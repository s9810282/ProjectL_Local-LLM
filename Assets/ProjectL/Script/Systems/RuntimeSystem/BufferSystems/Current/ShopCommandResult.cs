using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    public enum ShopCommandKind { Purchase = 0, Complete = 1, PurchaseTraining = 2 }

    /// <summary>요청한 상점 문맥과 Core의 구매 또는 완료 결과를 함께 보관한다.</summary>
    public readonly struct ShopCommandResult
    {
        public int RequestNumber { get; }
        public ShopVisitContext Context { get; }
        public int ChapterKey => Context.ChapterKey;
        public StageNodeId NodeId => Context.NodeId;
        public ShopCommandKind Kind { get; }
        public ShopOfferId OfferId { get; }
        public CharacterInstanceId OwnedId { get; }
        public TraitKey TraitKey { get; }
        public int ExpectedRank { get; }
        public ShopPurchaseResult PurchaseResult { get; }
        public ChapterProgressionResult CompletionResult { get; }
        public bool Accepted => Kind == ShopCommandKind.Purchase || Kind == ShopCommandKind.PurchaseTraining
            ? PurchaseResult != null && PurchaseResult.Succeeded
            : CompletionResult != null && CompletionResult.Accepted;

        internal ShopCommandResult(int requestNumber, ShopVisitContext context,
            ShopCommandKind kind, ShopOfferId offerId, ShopPurchaseResult purchaseResult,
            ChapterProgressionResult completionResult, CharacterInstanceId ownedId = default,
            TraitKey traitKey = TraitKey.None, int expectedRank = 0)
        {
            RequestNumber = requestNumber;
            Context = context;
            Kind = kind;
            OfferId = offerId;
            OwnedId = ownedId;
            TraitKey = traitKey;
            ExpectedRank = expectedRank;
            PurchaseResult = purchaseResult;
            CompletionResult = completionResult;
        }
    }
}
