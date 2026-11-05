using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class ImpactBoltTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int DistanceCells { get; }

        public ImpactBoltTraitStatusSnapshot(int distanceCells)
            : base(TraitKey.ImpactBolt)
        {
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));

            DistanceCells = distanceCells;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is ImpactBoltTraitStatusSnapshot b && DistanceCells == b.DistanceCells;
    }
}
