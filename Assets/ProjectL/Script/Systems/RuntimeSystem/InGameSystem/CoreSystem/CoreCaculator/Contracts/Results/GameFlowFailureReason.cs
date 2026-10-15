namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum GameFlowFailureReason
    {
        None = 0,
        WrongFlowPhase = 1,
        NodeNotFound = 2,
        NotConnected = 3,
        FinalNodeReached = 4,
        StageConfigurationMissing = 5,
        StageCreationFailed = 6,
        NoCurrentStage = 7,
        StageEntryMismatch = 8,
        StageNotEnded = 9,
    }
}
