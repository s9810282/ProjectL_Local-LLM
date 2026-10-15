namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum RunFailureReason
    {
        None = 0,
        WrongPhase,
        RunMismatch,
        StaleEntry,
        EntryMismatch,
        StageNotEnded,
        InvalidMainCharacter,
        InvalidStage,
        NodeNotFound,
        NotConnected,
        FinalNodeReached,
        RevisionOverflow,
        Disposed,
        StageKindMismatch,
        InvalidRecoverySpec,
        InvalidShopSpec,
        ArtifactLoadoutUnavailable,
    }
}
