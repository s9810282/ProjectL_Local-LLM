using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules
{
    internal sealed class SoulAltarRule : ICombatEffectRule
    {
        internal SoulGrowthAmounts Amounts { get; }
        internal int DurationUnitTurns { get; }

        internal SoulAltarRule(SoulGrowthAmounts amounts, int durationUnitTurns)
        {
            if (durationUnitTurns <= 0)
                throw new ArgumentOutOfRangeException(nameof(durationUnitTurns));

            Amounts = new SoulGrowthAmounts(
                amounts.AttackBonus,
                amounts.MaxHealthBonus,
                amounts.HealAmount);
            DurationUnitTurns = durationUnitTurns;
        }

        internal bool CanSpawn(bool isNight, bool isEnemyUnitDeath)
        {
            return isNight && isEnemyUnitDeath;
        }
    }
}
