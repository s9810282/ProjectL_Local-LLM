namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item
{
    /// <summary>ErrorText에 넣는 글의 사유입니다. Core 실패 사유를 컨트롤러가 이 값으로 바꿉니다.</summary>
    internal enum StageOfferErrorKind
    {
        None = 0,
        InsufficientFunds = 1,
        AlreadyOwned = 2,
        QuantityOverflow = 3,
        SoldOut = 4,
        DeckFull = 5,
        PurchaseUsed = 6,
        CompletionRetry = 7,
        StateChanged = 8,
        NoEffect = 9,
        AlreadyResolved = 10,
        SelectionChanged = 11,
        CannotChooseNow = 12
    }
}
