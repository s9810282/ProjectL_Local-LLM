using System;
using System.Collections.Generic;
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

        internal static TargetDamageBreakdown Attack(CombatCommandInput input, CombatEffectEngine engine,
            TargetDamagePipeline pipeline, CharacterCombatFacts actor, CharacterCombatFacts victim, int damage,
            AttackTargetRole role, int percent, CellCoord? impactOrigin, Rotation4? ray,
            MovementEnvironmentFacts environment = null)
        {
            var before = pipeline.Calculate(damage, role, percent, actor.Stats.Category.Key, victim?.Stats.Category.Key, 0);
            int reduction = victim == null ? 0 : engine.EvaluateIncomingDamage(CombatCommandEffects.Capture(victim,
                CombatCommandCalculator.RequiredProfile(input, victim)), new IncomingDamageEffectContext(
                    before.DamageAfterPhysicalDefense, victim.Anchor.Value,
                    victim.Stats.Category.DefenseFacing ?? Rotation4.R0, impactOrigin, ray)).EffectiveReduction;
            var result = pipeline.Calculate(damage, role, percent, actor.Stats.Category.Key, victim?.Stats.Category.Key, reduction);
            return victim == null ? result : ApplyTerrain(input, result, ray.HasValue ? actor.Anchor : impactOrigin,
                victim.Anchor, environment);
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
