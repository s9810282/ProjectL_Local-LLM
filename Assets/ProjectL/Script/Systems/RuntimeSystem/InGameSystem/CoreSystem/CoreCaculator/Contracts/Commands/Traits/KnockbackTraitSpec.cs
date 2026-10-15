using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class KnockbackTraitSpec : TraitSpec
    {
        public int DistanceCells { get; }

        public KnockbackTraitSpec(int distanceCells)
            : base(TraitKey.Knockback)
        {
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));

            DistanceCells = distanceCells;
        }
    }
}
