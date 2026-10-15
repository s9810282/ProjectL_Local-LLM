namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum RecoveryFailureReason
    {
        None = 0,
        WrongPhase,
        RunMismatch,
        EntryMismatch,
        StaleEntry,
        UnknownOption,
        InsufficientFunds,
        NoEffect,
        AlreadyResolved,
        RevisionOverflow,
        Disposed
    }
}
