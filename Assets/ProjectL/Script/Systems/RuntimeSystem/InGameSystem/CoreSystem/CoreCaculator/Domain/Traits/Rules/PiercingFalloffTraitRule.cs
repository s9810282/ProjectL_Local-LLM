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

        /// <summary>조우 순번의 남은 비율을 피해에 적용한다(half-up). 예산 계산과 실제 피해가 이 공식 하나를 쓴다.</summary>
        internal static int ApplyFalloff(int damage, int remainingPercent) =>
            CategoryMatchupDamageCalculator.Apply(damage, remainingPercent, 100);

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
                int damage = ApplyFalloff(damageAfterBonuses, remainingPercent);

                if (damage == 0)
                    return Math.Min(definition.MaxTargetsPerRay, encounterIndex);
            }

            throw new InvalidOperationException("A zero-damage encounter budget was not found.");
        }
    }
}
