using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class KnockbackTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int DistanceCells { get; }

        public KnockbackTraitStatusSnapshot(int distanceCells)
            : base(TraitKey.Knockback)
        {
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));

            DistanceCells = distanceCells;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is KnockbackTraitStatusSnapshot b && DistanceCells == b.DistanceCells;
    }
}
