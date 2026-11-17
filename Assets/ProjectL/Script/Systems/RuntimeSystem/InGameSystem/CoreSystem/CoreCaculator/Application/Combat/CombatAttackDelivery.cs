using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>지정 결과를 바꾸지 않고 전달(일반 직접 전달 또는 관통)의 피격 목록과 영향 칸을 확정한다.</summary>
    internal static class CombatAttackDelivery
    {
        /// <summary>
        /// 일반 지정이 받아들인 칸 조준이 관통 경로로 정렬되는지만 검사한다.
        /// 지정 방식과의 결합(단일 지정·스플래시 없음)은 프로필이 이미 보장한다.
        /// </summary>
        internal static AttackFailureReason ValidateAim(CombatAttackProfile profile, CellCoord origin, CellCoord aimCell)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (!profile.UsesPiercingShot) return AttackFailureReason.None;
            return PierceTargetCollector.TryResolveStep(origin, aimCell, out _)
                ? AttackFailureReason.None : AttackFailureReason.UnsupportedAttackDelivery;
        }

        internal static ResolvedAttackDelivery Resolve(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, ResolvedAttackSelection selection, int commonDamage,
            WallGeometry walls, AttackElevation elevation)
        {
            ResolvedAttackDelivery delivery = null;
            foreach (var _ in ResolveSteps(input, actor, profile, selection, commonDamage, walls, elevation,
                value => delivery = value)) { }
            return delivery;
        }

        internal static IEnumerable<string> ResolveSteps(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, ResolvedAttackSelection selection, int commonDamage, WallGeometry walls,
            AttackElevation elevation, Action<ResolvedAttackDelivery> completed)
        {
            if (!profile.UsesPiercingShot)
            {
                foreach (var phase in ResolveDirectSteps(actor, profile, selection, commonDamage, completed)) yield return phase;
                yield break;
            }
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(actor, profile);
            var initial = engine.EvaluatePierceFalloff(bindings, new PierceFalloffEffectContext(commonDamage, 0));
            // 프로필과 효과 바인딩은 같은 특성 목록에서 나온다. 둘이 어긋나면 조립 오류다.
            if (!initial.HasContribution)
                throw new InvalidOperationException("관통 프로필에 관통 효과가 없습니다.");
            if (initial.Contributions.Count != 1)
                throw new InvalidOperationException("관통 전달에는 하나의 효과 출처만 사용할 수 있습니다.");
            if (!selection.Cells.PrimaryCell.HasValue ||
                !PierceTargetCollector.TryResolveStep(actor.Anchor.Value, selection.Cells.PrimaryCell.Value, out var step))
                throw new InvalidOperationException("지원하지 않는 관통 조준입니다.");

            var origin = actor.Anchor.Value;
            var aim = selection.Cells.PrimaryCell.Value;
            int budget = initial.TargetBudget.Value;
            if (budget == 0)
            {
                foreach (var phase in PiercingSteps(new PiercingShotDelivery(initial.Contributions[0].Origin, origin,
                    aim, step, Array.Empty<CellCoord>(), Array.Empty<PiercingShotEncounter>(), origin, PiercingShotStopReason.TargetBudget),
                    actor, selection, commonDamage, null, completed)) yield return phase;
                yield break;
            }

            // 사거리는 조준 칸(지정)만 제한한다. 관통 진행은 보드 끝까지다.
            var victims = new List<CharacterCombatFacts>();
            var victimPolicy = selection.Policy.Combine(new AttackTargetingPolicy(false, false, false, false,
                allowsFriendlyCharacters: true));
            foreach (var candidate in input.Characters)
            {
                if (AttackTargetEligibility.CanTargetCharacter(actor, candidate, victimPolicy))
                    victims.Add(candidate);
                yield return "DeliveryPierceVictim";
            }
            PiercingShotTrace trace = null;
            foreach (var phase in PierceTargetCollector.CollectSingleSteps(origin, aim, actor.OccupiedCells,
                input.State.BoardWidth, input.State.BoardHeight, victims, selection.EligibleStructures, input.State.Structures,
                selection.Policy, walls, elevation, profile.Occlusion, value => trace = value)) yield return phase;
            var encounters = new List<PiercingShotEncounter>();
            foreach (var hit in trace.Encounters.Take(budget))
            {
                encounters.Add(new PiercingShotEncounter(hit.Id, hit.Encounter, hit.Cell, engine.EvaluatePierceFalloff(bindings,
                    new PierceFalloffEffectContext(commonDamage, hit.Encounter)).RemainingPercent));
                yield return "DeliveryPierceFalloff";
            }
            var endpoint = trace.Endpoint;
            var reason = trace.StopReason;
            bool trim = encounters.Count == budget && !(trace.StopReason == PiercingShotStopReason.Structure &&
                encounters[encounters.Count - 1].Cell == trace.Endpoint);
            if (trim)
            {
                endpoint = encounters[encounters.Count - 1].Cell;
                reason = PiercingShotStopReason.TargetBudget;
            }
            var traveled = new List<CellCoord>();
            foreach (var cell in trace.TraveledCells)
            {
                traveled.Add(cell);
                yield return "DeliveryPierceRoute";
                if (trim && cell == endpoint) break;
            }
            if (trim && (traveled.Count == 0 || traveled[traveled.Count - 1] != endpoint)) traveled.Clear();
            foreach (var phase in PiercingSteps(new PiercingShotDelivery(initial.Contributions[0].Origin,
                origin, aim, trace.Step, traveled.ToArray(), encounters.ToArray(), endpoint, reason), actor, selection, commonDamage,
                trace.BlockedContacts, completed)) yield return phase;
        }

        /// <summary>현재 공격력으로 피해를 다시 계산하되 이미 확정한 관통 조우와 감쇠 비율은 보존합니다.</summary>
        internal static int CurrentBaseDamage(CommandAttackPlan plan, CharacterCombatFacts actor, int hitIndex)
            => CurrentDamage(plan, actor, hitIndex).Damage;

        internal static (int Damage, IReadOnlyList<Contracts.Snapshots.Traits.TraitActivationSnapshot> Traits) CurrentDamage(
            CommandAttackPlan plan, CharacterCombatFacts actor, int hitIndex, int? projectileEncounter = null)
        {
            int common = CombatAttackCalculator.CommonAttackDamage(plan.DamageInput, actor, plan.Profile, plan.DamageKind);
            int damage = common == plan.CommonDamage ? plan.Delivery.Hits[hitIndex].BaseDamage
                : plan.Delivery.PiercingShot == null ? common : PiercingFalloffTraitRule.ApplyFalloff(common,
                    plan.Delivery.PiercingShot.Encounters.Span[hitIndex].RemainingPercent);
            var traits = new List<Contracts.Snapshots.Traits.TraitActivationSnapshot>();
            if (projectileEncounter.HasValue)
            {
                var falloff = plan.Engine.EvaluatePierceFalloff(plan.Bindings,
                    new PierceFalloffEffectContext(plan.CommonDamage, projectileEncounter.Value));
                damage = PiercingFalloffTraitRule.ApplyFalloff(common, falloff.RemainingPercent);
                CombatDamageCalculator.AddEvidence(traits, falloff.Contributions.Single().Origin,
                    TraitActivationKind.DamageModified, Math.Max(0, common - damage));
            }
            if (plan.Delivery.PiercingShot != null)
                CombatDamageCalculator.AddEvidence(traits, plan.Delivery.PiercingShot.Origin,
                    TraitActivationKind.DamageModified, Math.Max(0, common - damage));
            return (plan.DamagePercent == 100 ? damage : CategoryMatchupDamageCalculator.Apply(damage, plan.DamagePercent, 100),
                traits.AsReadOnly());
        }


        /// <summary>
        /// 일반 공격의 직접 전달이다. 지정 대상마다 역할에 맞는 적용 비율과 피해 출발 칸을 정한다.
        /// 맞는 대상은 선택 칸(주 칸 + 스플래시) 안에서만 나온다. 적 추적 조준 후보(<see cref="CombatAttackQueries.AimsThatCanHit"/>)가 이 성질을 쓰므로, 바꾸면 그쪽도 고친다.
        /// </summary>
        private static IEnumerable<string> ResolveDirectSteps(CharacterCombatFacts actor, CombatAttackProfile profile,
            ResolvedAttackSelection selection, int commonDamage, Action<ResolvedAttackDelivery> completed)
        {
            var reachable = selection.ReachableCells;
            var hits = new List<DeliveredAttackHit>();
            foreach (var target in selection.Targets)
            {
                hits.Add(target.Value == AttackTargetRole.Additional
                    ? new DeliveredAttackHit(target.Key, target.Value, commonDamage, profile.Method.SplashPercent,
                        reachable.PrimaryCell, landed: profile.Method.SplashPercent > 0)
                    : new DeliveredAttackHit(target.Key, target.Value, commonDamage, 100, actor.Anchor));
                yield return "DeliveryDirectHit";
            }
            CellCoord[] cells = null;
            foreach (var phase in DirectAffectedCellSteps(reachable, value => cells = value)) yield return phase;
            completed(new ResolvedAttackDelivery(hits, cells));
            yield return "DeliveryDirectComplete";
        }

        /// <summary>
        /// 직접 전달이 덮는 칸이다: 가림을 적용한 지정·추가 칸, 중복 제거, 행·열 순서. 대상이 없는 지정의 표시 칸(포효 범위)도 이 식 하나를 쓴다.
        /// </summary>
        internal static IEnumerable<string> DirectAffectedCellSteps(AttackCellSelection reachable, Action<CellCoord[]> completed)
        {
            var cells = new SortedSet<(int Y, int X)>();
            foreach (var mask in new[] { reachable.DesignatedCells, reachable.AdditionalCells })
                for (int i = 0; i < mask.Length; i++)
                {
                    var cell = mask.Span[i];
                    cells.Add((cell.Y, cell.X));
                    yield return "DeliveryDirectCell";
                }
            var result = new CellCoord[cells.Count];
            int index = 0;
            foreach (var cell in cells)
            {
                result[index++] = new CellCoord(cell.X, cell.Y);
                yield return "DeliveryDirectCellOutput";
            }
            completed(result);
        }

        /// <summary>관통 조우마다 감쇠를 적용한 피격을 만든다. 선택한 개체만 Primary이고 나머지는 Effect다.</summary>
        private static IEnumerable<string> PiercingSteps(PiercingShotDelivery record, CharacterCombatFacts actor,
            ResolvedAttackSelection selection, int commonDamage, IEnumerable<AttackBlockedContact> blockedContacts,
            Action<ResolvedAttackDelivery> completed)
        {
            var hits = new List<DeliveredAttackHit>();
            for (int i = 0; i < record.Encounters.Length; i++)
            {
                var encounter = record.Encounters.Span[i];
                hits.Add(new DeliveredAttackHit(encounter.TargetId,
                    encounter.TargetId == selection.SelectedTargetId ? AttackTargetRole.Primary : AttackTargetRole.Effect,
                    PiercingFalloffTraitRule.ApplyFalloff(commonDamage, encounter.RemainingPercent), 100, actor.Anchor));
                yield return "DeliveryPierceHit";
            }
            completed(new ResolvedAttackDelivery(hits, record.TraveledCells.ToArray(), record, blockedContacts));
            yield return "DeliveryPierceComplete";
        }
    }
}
