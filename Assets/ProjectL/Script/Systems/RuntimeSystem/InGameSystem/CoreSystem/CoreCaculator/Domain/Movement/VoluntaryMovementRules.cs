using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
{
    internal static class VoluntaryMovementRules
    {
        /// <summary>자발 이동이 불가능하면 null을 반환합니다. 환경은 호출자가 적용할 관찰 범위로 전달합니다.</summary>
        internal static int? Allowance(CharacterCombatFacts actor, MovementEnvironmentFacts environment = null)
        {
            int allowance = actor.Stats.MoveRange;
            if (allowance == 0) return null;
            allowance = checked(allowance + RampageTraitRule.MovementBonus(actor));
            if (environment != null && actor.Anchor.HasValue)
                allowance = checked(allowance + GroundCompactionTraitRule.MovementBonusAt(actor.Anchor.Value, environment));
            int slow = actor.StatusEffects.Entries.OfType<MovementRangeDecreaseStatus>().SingleOrDefault()?.Magnitude ?? 0;
            return Math.Max(0, allowance - slow);
        }
    }
}
