namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum WalletCommandFailureReason
    {
        None = 0,
        InvalidAmount = 1,
        InsufficientFunds = 2,
        Overflow = 3,
        RunMismatch = 4,
        EntryMismatch = 5,
        WrongPhase = 6,
        RunEnded = 7,
        CompletionPending = 8,
        InvalidActiveStage = 9,
        Disposed = 10
    }
}
