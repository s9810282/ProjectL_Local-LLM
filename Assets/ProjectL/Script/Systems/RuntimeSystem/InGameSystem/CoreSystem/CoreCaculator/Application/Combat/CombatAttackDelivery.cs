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
            if (!profile.UsesPiercingShot) return ResolveDirect(actor, profile, selection, commonDamage);
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
                return Piercing(new PiercingShotDelivery(initial.Contributions[0].Origin, origin,
                    aim, step, Array.Empty<CellCoord>(), Array.Empty<PiercingShotEncounter>(), origin, PiercingShotStopReason.TargetBudget),
                    actor, selection, commonDamage, null);

            // 사거리는 조준 칸(지정)만 제한한다. 관통 진행은 보드 끝까지다.
            var trace = PierceTargetCollector.CollectSingle(origin, aim, actor.OccupiedCells,
                input.State.BoardWidth, input.State.BoardHeight, selection.EligibleCharacters, selection.EligibleStructures, input.State.Structures,
                selection.Policy, walls, elevation, profile.Occlusion);
            var encounters = trace.Encounters.Take(budget).Select(hit => new PiercingShotEncounter(hit.Id,
                hit.Encounter, hit.Cell, engine.EvaluatePierceFalloff(bindings,
                    new PierceFalloffEffectContext(commonDamage, hit.Encounter)).RemainingPercent)).ToArray();
            var endpoint = trace.Endpoint;
            var reason = trace.StopReason;
            var traveled = trace.TraveledCells.ToArray();
            if (encounters.Length == budget && !(trace.StopReason == PiercingShotStopReason.Structure &&
                    encounters[encounters.Length - 1].Cell == trace.Endpoint))
            {
                endpoint = encounters[encounters.Length - 1].Cell;
                reason = PiercingShotStopReason.TargetBudget;
                int length = Array.IndexOf(traveled, endpoint) + 1;
                traveled = traveled.Take(length).ToArray();
            }
            return Piercing(new PiercingShotDelivery(initial.Contributions[0].Origin,
                origin, aim, trace.Step, traveled, encounters, endpoint, reason), actor, selection, commonDamage, trace.BlockedContacts);
        }

        /// <summary>현재 공격력으로 피해를 다시 계산하되 이미 확정한 관통 조우와 감쇠 비율은 보존합니다.</summary>
        internal static int CurrentBaseDamage(CommandAttackPlan plan, CharacterCombatFacts actor, int hitIndex)
            => CurrentDamage(plan, actor, hitIndex).Damage;

        internal static (int Damage, IReadOnlyList<Contracts.Snapshots.Traits.TraitActivationSnapshot> Traits) CurrentDamage(
            CommandAttackPlan plan, CharacterCombatFacts actor, int hitIndex)
        {
            int common = CombatAttackCalculator.CommonAttackDamage(plan.Input, actor, plan.Profile);
            int damage = common == plan.CommonDamage ? plan.Delivery.Hits[hitIndex].BaseDamage
                : plan.Delivery.PiercingShot == null ? common : PiercingFalloffTraitRule.ApplyFalloff(common,
                    plan.Delivery.PiercingShot.Encounters.Span[hitIndex].RemainingPercent);
            var traits = new List<Contracts.Snapshots.Traits.TraitActivationSnapshot>();
            if (plan.Delivery.PiercingShot != null)
                CombatDamageCalculator.AddEvidence(traits, plan.Delivery.PiercingShot.Origin,
                    TraitActivationKind.DamageModified, Math.Max(0, common - damage));
            return (damage, traits.AsReadOnly());
        }


        /// <summary>
        /// 일반 공격의 직접 전달이다. 지정 대상마다 역할에 맞는 적용 비율과 피해 출발 칸을 정한다.
        /// 맞는 대상은 선택 칸(주 칸 + 스플래시) 안에서만 나온다. 적 추적 조준 후보(<see cref="CombatAttackQueries.AimsThatCanHit"/>)가 이 성질을 쓰므로, 바꾸면 그쪽도 고친다.
        /// </summary>
        private static ResolvedAttackDelivery ResolveDirect(CharacterCombatFacts actor, CombatAttackProfile profile,
            ResolvedAttackSelection selection, int commonDamage)
        {
            var reachable = selection.ReachableCells;
            var hits = selection.Targets.Select(target => target.Value == AttackTargetRole.Additional
                ? new DeliveredAttackHit(target.Key, target.Value, commonDamage, profile.Method.SplashPercent, reachable.PrimaryCell)
                : new DeliveredAttackHit(target.Key, target.Value, commonDamage, 100, actor.Anchor));
            return new ResolvedAttackDelivery(hits, DirectAffectedCells(reachable));
        }

        /// <summary>
        /// 직접 전달이 덮는 칸이다: 가림을 적용한 지정·추가 칸, 중복 제거, 행·열 순서. 대상이 없는 지정의 표시 칸(포효 범위)도 이 식 하나를 쓴다.
        /// </summary>
        internal static CellCoord[] DirectAffectedCells(AttackCellSelection reachable) =>
            reachable.DesignatedCells.ToArray().Concat(reachable.AdditionalCells.ToArray())
                .Distinct().OrderBy(c => c.Y).ThenBy(c => c.X).ToArray();

        /// <summary>관통 조우마다 감쇠를 적용한 피격을 만든다. 선택한 개체만 Primary이고 나머지는 Effect다.</summary>
        private static ResolvedAttackDelivery Piercing(PiercingShotDelivery record, CharacterCombatFacts actor,
            ResolvedAttackSelection selection, int commonDamage, IEnumerable<AttackBlockedContact> blockedContacts)
        {
            var hits = record.Encounters.ToArray().Select(encounter => new DeliveredAttackHit(encounter.TargetId,
                encounter.TargetId == selection.SelectedTargetId ? AttackTargetRole.Primary : AttackTargetRole.Effect,
                PiercingFalloffTraitRule.ApplyFalloff(commonDamage, encounter.RemainingPercent), 100, actor.Anchor));
            return new ResolvedAttackDelivery(hits, record.TraveledCells.ToArray(), record, blockedContacts);
        }
    }
}
