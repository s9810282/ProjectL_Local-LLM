using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class KnockbackTraitDefinition : TraitDefinition
    {
        public int DistanceCells { get; }

        public KnockbackTraitDefinition(int distanceCells)
            : base(TraitKey.Knockback)
        {
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));

            DistanceCells = distanceCells;
        }
    }
}
