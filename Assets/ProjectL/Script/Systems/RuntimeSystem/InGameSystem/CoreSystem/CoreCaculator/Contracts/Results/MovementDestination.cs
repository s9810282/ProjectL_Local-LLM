using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct MovementDestination
    {
        public CellCoord TargetAnchor { get; }
        public ReadOnlyMemory<CellCoord> DestinationOccupiedCells { get; }

        public int StepDistance { get; }

        public MovementDestination(CellCoord targetAnchor,CellCoord[] destinationOccupiedCells,int distance)
        {
            if (destinationOccupiedCells == null) throw new ArgumentNullException(nameof(destinationOccupiedCells));
            if (distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
            TargetAnchor = targetAnchor;
            DestinationOccupiedCells = (CellCoord[])destinationOccupiedCells.Clone();
            StepDistance = distance;
        }
    }
}
