using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectKnockbackContribution
    {
        public EffectOriginKey Origin { get; }
        public int DistanceCells { get; }

        public EffectKnockbackContribution(EffectOriginKey origin, int distanceCells)
        {
            if (origin.IsValid == false)
                throw new ArgumentException(nameof(origin));
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));

            Origin = origin;
            DistanceCells = distanceCells;
        }
    }
}
