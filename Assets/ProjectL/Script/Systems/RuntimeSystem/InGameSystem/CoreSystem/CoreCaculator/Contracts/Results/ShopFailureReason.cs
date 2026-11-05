namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum ShopFailureReason
    {
        None = 0, WrongPhase, RunMismatch, EntryMismatch, StaleEntry, UnknownOffer,
        InsufficientFunds, SoldOut, DeckCountOverflow, DeckRevisionOverflow, RunRevisionOverflow, Disposed,
        InvalidCharacter, IdentityOverflow, RunEnded,
        AlreadyOwned, PurchaseAlreadyUsed, InvalidItem, ItemQuantityOverflow,
        TargetNotOwned,
        /// <summary>판매 정의에 해당하는 특성 성장 칸이 이미 차 있어 구매할 수 없다.</summary>
        AlreadyEnhanced,
        InvalidEnhancement, UnsupportedEnhancementLevel, OwnershipLimitReached,
        SubTraitPurchaseUnavailable,
        TraitNotTrainable, RankLimitReached, StaleRank, StaleRoster
    }
}
