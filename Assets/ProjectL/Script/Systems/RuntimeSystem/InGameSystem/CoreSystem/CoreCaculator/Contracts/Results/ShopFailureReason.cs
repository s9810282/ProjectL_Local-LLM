namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum ShopFailureReason
    {
        None = 0, WrongPhase, RunMismatch, EntryMismatch, StaleEntry, UnknownOffer,
        InsufficientFunds, SoldOut, DeckCountOverflow, DeckRevisionOverflow, RunRevisionOverflow, Disposed,
        InvalidCharacter, IdentityOverflow, RunEnded
    }
}
