using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class PiercingFalloffTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.PiercingFalloff;

        internal int EvaluateRemainingPercent(
            PiercingFalloffTraitDefinition definition,
            int encounterIndex)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (encounterIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(encounterIndex));

            long remaining = 100L -
                ((long)definition.ReductionPercentPerTarget * encounterIndex);
            return remaining > 0L ? (int)remaining : 0;
        }

        internal int CalculateTargetBudget(
            PiercingFalloffTraitDefinition definition,
            int damageAfterBonuses)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (damageAfterBonuses < 0)
                throw new ArgumentOutOfRangeException(nameof(damageAfterBonuses));

            int reduction = definition.ReductionPercentPerTarget;
            int firstCertainZero = (100 + reduction - 1) / reduction;

            for (int encounterIndex = 0;
                 encounterIndex <= firstCertainZero;
                 encounterIndex++)
            {
                int remainingPercent = EvaluateRemainingPercent(
                    definition,
                    encounterIndex);
                int damage = CategoryMatchupDamageCalculator.Apply(
                    damageAfterBonuses,
                    remainingPercent,
                    100);

                if (damage == 0)
                    return Math.Min(definition.MaxTargetsPerRay, encounterIndex);
            }

            throw new InvalidOperationException("A zero-damage encounter budget was not found.");
        }
    }
}
