using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectPierceFalloffContribution
    {
        public EffectOriginKey Origin { get; }
        public int RemainingPercent { get; }
        public int TargetBudget { get; }

        public EffectPierceFalloffContribution(
            EffectOriginKey origin,
            int remainingPercent,
            int targetBudget)
        {
            if (origin.IsValid == false)
                throw new ArgumentException(nameof(origin));
            if (remainingPercent < 0 || remainingPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(remainingPercent));
            if (targetBudget < 0)
                throw new ArgumentOutOfRangeException(nameof(targetBudget));

            Origin = origin;
            RemainingPercent = remainingPercent;
            TargetBudget = targetBudget;
        }
    }
}
