using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class RoarTraitDefinition : TraitDefinition
    {
        public int HitThreshold { get; }

        public RoarTraitDefinition(int hitThreshold)
            : base(TraitKey.Roar)
        {
            if (hitThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(hitThreshold));
            HitThreshold = hitThreshold;
        }
    }
}
