using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class ImpactBoltTraitSpec : TraitSpec
    {
        public int DistanceCells { get; }

        public ImpactBoltTraitSpec(int distanceCells)
            : base(TraitKey.ImpactBolt)
        {
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));

            DistanceCells = distanceCells;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (ImpactBoltTraitSpec)other;
            return DistanceCells == otherSpec.DistanceCells;
        }
    }
}
