using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class MovementUndoAvailability
    {
        public static MovementUndoAvailability Unavailable { get; } = new MovementUndoAvailability(MovementUndoFailureReason.NoCheckpoint);
        public bool CanUndo => Failure == MovementUndoFailureReason.None;
        public MovementUndoFailureReason Failure { get; }
        public int MoveCommandNumber { get; }
        public CellCoord? Destination { get; }

        public MovementUndoAvailability(MovementUndoFailureReason failure, int moveCommandNumber = 0, CellCoord? destination = null)
        {
            if (!Enum.IsDefined(typeof(MovementUndoFailureReason), failure) ||
                (failure == MovementUndoFailureReason.None ? moveCommandNumber < 1 || !destination.HasValue : moveCommandNumber != 0 || destination.HasValue))
                throw new ArgumentException("취소 가능 응답에만 원래 명령 번호와 목적지가 필요합니다.");
            Failure = failure; MoveCommandNumber = moveCommandNumber; Destination = destination;
        }
    }
}
