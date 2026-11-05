namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    /// <summary>특성 성장 선택을 반영하지 않은 이유다.</summary>
    public enum TraitGrowthFailureReason
    {
        None = 0,
        InvalidRequest = 1,
        NotUnlocked = 2,
        NotCandidate = 3,
        UnknownDefinition = 4,
        NotOwned = 5,
        InBattle = 6,
        RunEnded = 7,
        SourceChanged = 8,
        StaleOffer = 9,
        AwaitingAcknowledgement = 10
    }
}
