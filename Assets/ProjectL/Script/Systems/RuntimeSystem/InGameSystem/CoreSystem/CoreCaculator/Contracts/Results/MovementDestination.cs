using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct MovementDestination
    {
        public CellCoord TargetAnchor { get; }
        public ReadOnlyMemory<CellCoord> DestinationOccupiedCells { get; }

        public int StepDistance { get; }
        public int DistanceFromCheckpoint { get; }
        public int TotalMoveActionCost { get; }
        public int ActionCostDelta { get; }

        /// 기존 호출부 호환용 alias
        public int Distance => StepDistance;
        public int ActionCost => TotalMoveActionCost;

        public MovementDestination(
            CellCoord targetAnchor,
            CellCoord[] destinationOccupiedCells,
            int stepDistance,
            int distanceFromCheckpoint,
            int totalMoveActionCost,
            int actionCostDelta)
        {
            if (destinationOccupiedCells == null)
                throw new ArgumentNullException(nameof(destinationOccupiedCells));

            if (stepDistance < 0)
                throw new ArgumentOutOfRangeException(nameof(stepDistance));
            if (distanceFromCheckpoint < 0)
                throw new ArgumentOutOfRangeException(nameof(distanceFromCheckpoint));
            if (totalMoveActionCost < 0)
                throw new ArgumentOutOfRangeException(nameof(totalMoveActionCost));

            CellCoord[] copiedCells = new CellCoord[destinationOccupiedCells.Length];
            Array.Copy(destinationOccupiedCells, copiedCells, destinationOccupiedCells.Length);

            TargetAnchor = targetAnchor;
            DestinationOccupiedCells = copiedCells;
            StepDistance = stepDistance;
            DistanceFromCheckpoint = distanceFromCheckpoint;
            TotalMoveActionCost = totalMoveActionCost;
            ActionCostDelta = actionCostDelta;
        }

        public MovementDestination(
            CellCoord targetAnchor,
            CellCoord[] destinationOccupiedCells,
            int distance,
            int actionCost)
            : this(
                targetAnchor,
                destinationOccupiedCells,
                distance,
                distance,
                actionCost,
                actionCost)
        {
        }
    }
}
