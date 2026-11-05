using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class SoloActionTraitSpec : TraitSpec
    {
        public int ShieldAmount { get; }
        public int RadiusCells { get; }
        public int RequiredOccupiedNeighborCount { get; }
        public int ResetEveryControllerTurnEnds { get; }

        public SoloActionTraitSpec(int shieldAmount, int radiusCells, int requiredOccupiedNeighborCount, int resetEveryControllerTurnEnds) : base(TraitKey.SoloAction)
        {
            if (shieldAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(shieldAmount));
            if (radiusCells < 1)
                throw new ArgumentOutOfRangeException(nameof(radiusCells));
            if (requiredOccupiedNeighborCount < 0)
                throw new ArgumentOutOfRangeException(nameof(requiredOccupiedNeighborCount));
            if (resetEveryControllerTurnEnds < 1)
                throw new ArgumentOutOfRangeException(nameof(resetEveryControllerTurnEnds));
            if (requiredOccupiedNeighborCount > 4m * radiusCells * (radiusCells + 1m))
                throw new ArgumentOutOfRangeException(nameof(requiredOccupiedNeighborCount));
            ShieldAmount = shieldAmount;
            RadiusCells = radiusCells;
            RequiredOccupiedNeighborCount = requiredOccupiedNeighborCount;
            ResetEveryControllerTurnEnds = resetEveryControllerTurnEnds;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (SoloActionTraitSpec)other;
            return ShieldAmount == otherSpec.ShieldAmount &&
                RadiusCells == otherSpec.RadiusCells &&
                RequiredOccupiedNeighborCount == otherSpec.RequiredOccupiedNeighborCount &&
                ResetEveryControllerTurnEnds == otherSpec.ResetEveryControllerTurnEnds;
        }
    }
}
