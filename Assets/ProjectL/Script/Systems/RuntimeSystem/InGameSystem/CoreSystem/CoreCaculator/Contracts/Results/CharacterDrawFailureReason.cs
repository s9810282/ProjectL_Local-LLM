namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum CharacterDrawFailureReason
    {
        None = 0,
        StageEnded,
        WrongPhase,
        FirstPreparation,
        DeckEmpty,
        LivingSubLimitReached,
        ReserveFull,
        InsufficientGold,
    }
}
