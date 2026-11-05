using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal enum EffectKnockbackMode
    {
        AttackerOrigin = 0,
        ImpactCenter = 1,
    }

    internal readonly struct EffectKnockbackContribution
    {
        public EffectOriginKey Origin { get; }
        public int DistanceCells { get; }
        public EffectKnockbackMode Mode { get; }

        public EffectKnockbackContribution(EffectOriginKey origin, int distanceCells,
            EffectKnockbackMode mode = EffectKnockbackMode.AttackerOrigin)
        {
            if (origin.IsValid == false)
                throw new ArgumentException(nameof(origin));
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));
            if (!Enum.IsDefined(typeof(EffectKnockbackMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));

            Origin = origin;
            DistanceCells = distanceCells;
            Mode = mode;
        }
    }
}
