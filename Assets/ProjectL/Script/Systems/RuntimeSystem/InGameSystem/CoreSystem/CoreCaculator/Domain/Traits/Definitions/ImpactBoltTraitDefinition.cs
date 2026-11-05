using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    public sealed class ImpactBoltTraitDefinition : TraitDefinition
    {
        public int DistanceCells { get; }

        public ImpactBoltTraitDefinition(int distanceCells)
            : base(TraitKey.ImpactBolt)
        {
            if (distanceCells < 1)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));

            DistanceCells = distanceCells;
        }
    }
}
