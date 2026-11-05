using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>피해 계산만 담당한다. 대상 선택과 HP·사망·원본 소모는 기존 호출자가 정산한다.</summary>
    internal static class CombatDamageCalculator
    {
        /// <summary>일반 피해의 보유 방어를 투영한다. 순수 피해 산식과 저장소 게시를 분리한다.</summary>
        internal static int ApplyHeldDefense(CommandAttackGroups groups,
            Dictionary<EntityId, CharacterCombatFacts> projected, EntityId target, int damage,
            CombatDamageOrigin origin, List<CombatEvent> events)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            if (!projected.TryGetValue(target, out var before)) return damage;
            int remaining = HeldEnvironmentEffects.Protect(groups, before, damage, origin.AttackGroup, out var after);
            if (!ReferenceEquals(before, after))
            {
                projected[target] = after;
                events.Add(new CombatHeldEnvironmentEvent(before, after, origin));
            }
            return remaining;
        }

        /// <summary>공격자와 대상 사이 체비셰프 거리의 배율을 대상 피해에 적용한다. 반올림은 상성 배율과 같은 half-up이다.</summary>
        internal static int ScaleByTargetDistance(CombatEffectEngine engine, EffectRuleBinding[] bindings,
            CharacterCombatFacts actor, EntityId targetId, CharacterCombatFacts victim, StructureCombatFacts[] structures, int damage)
            => ScaleByTargetDistanceDetailed(engine, bindings, actor, targetId, victim, structures, damage).Damage;

        internal static (int Damage, IReadOnlyList<TraitActivationSnapshot> Traits) ScaleByTargetDistanceDetailed(
            CombatEffectEngine engine, EffectRuleBinding[] bindings, CharacterCombatFacts actor, EntityId targetId,
            CharacterCombatFacts victim, StructureCombatFacts[] structures, int damage)
        {
            var cells = victim?.OccupiedCells ?? structures.Single(s => s.EntityId == targetId).OccupiedCells;
            int distance = Distance(actor, cells);
            var scale = engine.EvaluateTargetDistanceDamage(bindings, new TargetDistanceDamageEffectContext(distance));
            var traits = new List<TraitActivationSnapshot>();
            int numerator = 1, denominator = 1, prior = damage;
            foreach (var contribution in scale.Contributions)
            {
                numerator = checked(numerator * contribution.Percent);
                denominator = checked(denominator * 100);
                int next = CategoryMatchupDamageCalculator.Apply(damage, numerator, denominator);
                AddEvidence(traits, contribution.Origin, TraitActivationKind.DamageModified, Math.Abs(next - prior));
                prior = next;
            }
            return (scale.HasContribution ? CategoryMatchupDamageCalculator.Apply(damage, scale.Numerator, scale.Denominator) : damage,
                traits.AsReadOnly());
        }

        internal static int Distance(CharacterCombatFacts actor, IReadOnlyList<CellCoord> cells) =>
            cells.Min(c => checked((int)Math.Max(Math.Abs((long)c.X - actor.Anchor.Value.X), Math.Abs((long)c.Y - actor.Anchor.Value.Y))));

        internal static TargetDamageBreakdown Attack(CombatCommandInput input, CombatEffectEngine engine,
            TargetDamagePipeline pipeline, CharacterCombatFacts actor, CharacterCombatFacts victim, int damage,
            AttackTargetRole role, int percent, CellCoord? impactOrigin, Rotation4? ray,
            MovementEnvironmentFacts environment = null,
            int? targetDistance = null, int actualRecipientIndex = 0, IReadOnlyList<CharacterCombatFacts> currentCharacters = null,
            CombatDamageKind damageKind = CombatDamageKind.OrdinaryAttack, AttackTraitScope traitScope = null, HealthStateSnapshot? targetHealth = null,
            IReadOnlyList<TraitActivationSnapshot> deliveredContributions = null)
        {
            // 방어 단계는 캐릭터 대상의 실효 방어력(아이템 감소 반영)을 쓴다. 구조물 대상은 0이다.
            var target = engine.EvaluateTargetDamage(CombatCommandEffects.Capture(actor,
                CombatCommandCalculator.RequiredProfile(input, actor)), new TargetDamageEffectContext(
                    CombatEffectiveStatsCalculator.AttackPower(actor),
                    victim == null ? (int?)null : CombatEffectiveStatsCalculator.AttackPower(victim),
                    targetDistance ?? (victim == null ? 0 : Distance(actor, victim.OccupiedCells)), actualRecipientIndex, victim?.Health ?? targetHealth, traitScope?.CapturedEnemyCount));
            var contributions = new List<TraitActivationSnapshot>(deliveredContributions ?? Array.Empty<TraitActivationSnapshot>());
            int effectiveDefense = victim == null ? 0 : CombatEffectiveStatsCalculator.PhysicalDefense(input, victim, currentCharacters ?? input.Characters);
            int defense = target.BypassPhysicalDefense ? 0 : effectiveDefense;
            var result = pipeline.Calculate(damage, role, percent, actor.Stats.Category.Key, victim?.Stats.Category.Key, defense);
            if (target.BypassPhysicalDefense && effectiveDefense > 0 && result.DamageBeforeMatchup > 0)
            {
                var bypass = target.Contributions.First(contribution => contribution.BypassPhysicalDefense);
                AddEvidence(contributions, bypass.Origin, TraitActivationKind.DefenseBypassed, effectiveDefense);
            }
            IReadOnlyList<TraitTrailFact> usedDust = Array.Empty<TraitTrailFact>();
            if (victim != null)
            {
                var incoming = engine.EvaluateIncomingDamage(
                    CombatCommandEffects.CaptureIncoming(input, currentCharacters ?? input.Characters, victim, out var selectedDust, environment),
                    new IncomingDamageEffectContext(result.DamageAfterPhysicalDefense, damageKind, victim, currentCharacters ?? input.Characters, actor.EntityId));
                contributions.AddRange(IncomingEvidence(incoming, result.DamageAfterPhysicalDefense));
                if (result.DamageBeforeMatchup > 0 && selectedDust.Count > 0 &&
                    incoming.AllocatedReductions.Any(c => c.Origin == selectedDust[0].Origin && c.RemainingDamagePercent < 100))
                    usedDust = selectedDust;
                result = new TargetDamageBreakdown(result.DamageBeforeMatchup, result.MatchupNumerator,
                    result.MatchupDenominator, result.DamageAfterMatchup, result.DamageAfterPhysicalDefense,
                    incoming.EffectiveReduction, incoming.FinalDamage, incoming.DamageAfterPercentages);
                result = ApplyTerrain(input, result, ray.HasValue ? actor.Anchor : impactOrigin, victim.Anchor, environment);
            }

            // 양수 공격의 최소 피해는 모든 경감 뒤 여기서 한 번만 보정한다. 보호막 흡수는 이후 호출자가 처리한다.
            int minimum = result.DamageBeforeMatchup > 0 ? Math.Max(1, result.FinalDamage) : result.FinalDamage;
            int final = target.Scale(minimum, target.Contributions.Count);
            int prior = minimum;
            for (int index = 0; index < target.Contributions.Count; index++)
            {
                var contribution = target.Contributions[index];
                int next = target.Scale(minimum, index + 1);
                AddEvidence(contributions, contribution.Origin, TraitActivationKind.DamageModified, Math.Abs(next - prior));
                prior = next;
            }
            return new TargetDamageBreakdown(result.DamageBeforeMatchup, result.MatchupNumerator,
                result.MatchupDenominator, result.DamageAfterMatchup, result.DamageAfterPhysicalDefense,
                result.IncomingReduction, final, result.DamageAfterReceivedPercentages, contributions, usedDust);
        }

        internal static IReadOnlyList<TraitActivationSnapshot> IncomingEvidence(EffectIncomingDamageEvaluation incoming, int originalDamage)
        {
            var amounts = incoming.AllocatedReductions.ToDictionary(c => c.Origin, c => c.FlatReduction);
            decimal exact = originalDamage - incoming.EffectiveReduction;
            foreach (var contribution in incoming.AllocatedReductions)
            {
                decimal next = exact * contribution.RemainingDamagePercent / 100m;
                amounts[contribution.Origin] = checked(amounts[contribution.Origin] + (int)decimal.Truncate(exact) - (int)decimal.Truncate(next));
                exact = next;
            }
            var traits = new List<TraitActivationSnapshot>();
            foreach (var contribution in incoming.AllocatedReductions)
                AddEvidence(traits, contribution.Origin, TraitActivationKind.DamageModified, amounts[contribution.Origin]);
            return traits.AsReadOnly();
        }

        internal static void AddEvidence(List<TraitActivationSnapshot> traits, EffectOriginKey origin, TraitActivationKind kind, int magnitude)
        {
            if (origin.Kind != EffectOwnerKind.Trait || magnitude == 0) return;
            traits.Add(new TraitActivationSnapshot(new EntityId(origin.OwnerInstanceId), null, (TraitKey)origin.DefinitionKey, kind, magnitude));
        }

        /// <summary>지형 문맥이 없는 기존 환경 피해량을 통과시킨다. 공격 상성·Heavy나 뱃지 규칙을 추가하지 않는다.</summary>
        internal static TargetDamageBreakdown Environment(int damage, CombatDamageOrigin origin)
        {
            if (origin == null) throw new ArgumentNullException(nameof(origin));
            return TargetDamageBreakdown.CreatePassthrough(damage);
        }

        internal static TargetDamageBreakdown Environment(CombatCommandInput input, int damage,
            CombatDamageOrigin origin, CellCoord? receiver, MovementEnvironmentFacts environment = null) =>
            ApplyTerrain(input, Environment(damage, origin), origin.PhysicalCell, receiver, environment ?? input.State.Environment);

        private static TargetDamageBreakdown ApplyTerrain(CombatCommandInput input, TargetDamageBreakdown damage,
            CellCoord? physicalOrigin, CellCoord? receiver, MovementEnvironmentFacts environment)
        {
            if (!receiver.HasValue) return damage;
            int percent = EnvironmentTerrainRules.RemainingDamagePercent(environment ?? input.State.Environment,
                input.EnvironmentBindings, physicalOrigin, receiver.Value);
            return percent == 100 ? damage : TargetDamagePipeline.ApplyReceivedPercentages(damage, percent);
        }
    }
}
