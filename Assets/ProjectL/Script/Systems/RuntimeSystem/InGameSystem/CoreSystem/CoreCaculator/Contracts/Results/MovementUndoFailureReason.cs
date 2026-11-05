namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum MovementUndoFailureReason
    {
        None, NoCheckpoint, WrongPhase, WrongController, StaleBattle, StaleTurn, StaleToken,
        ActorUnavailable, ActorChanged, DestinationOccupied, DestinationInvalid, ReturnWouldCauseEffect, PendingReaction
    }
}
