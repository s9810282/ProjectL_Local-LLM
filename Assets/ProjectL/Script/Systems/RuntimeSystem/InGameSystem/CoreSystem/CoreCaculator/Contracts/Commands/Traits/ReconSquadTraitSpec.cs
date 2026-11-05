using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class ReconSquadTraitSpec : TraitSpec
    {
        public int IncreasePercent { get; }
        public int Radius { get; }

        public ReconSquadTraitSpec(int increasePercent = 10, int radius = 1) : base(TraitKey.ReconSquad)
        {
            if (increasePercent < 0)
                throw new ArgumentOutOfRangeException(nameof(increasePercent));
            if (radius < 1)
                throw new ArgumentOutOfRangeException(nameof(radius));
            IncreasePercent = increasePercent;
            Radius = radius;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (ReconSquadTraitSpec)other;
            return IncreasePercent == otherSpec.IncreasePercent &&
                Radius == otherSpec.Radius;
        }
    }
}
