namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum MovementFailureReason
    {
        None = 0,
        EntityNotFound,
        NotUnit,
        WrongPhase,
        NotInCombat,
        Dead,
        NotActiveUnit,
        UnsupportedMoveMethod,
        SameAnchor,
        OutOfBounds,
        DestinationOccupied,
        MoveRangeExceeded,
        ActionCostInsufficient,
        StateInconsistent,
        StageEnded, // 스테이지 종료
        PathOriginMismatch,
        InvalidPath,
        RepeatedCell,
        NoPendingMovement,
        StaleContext,
    }
}
