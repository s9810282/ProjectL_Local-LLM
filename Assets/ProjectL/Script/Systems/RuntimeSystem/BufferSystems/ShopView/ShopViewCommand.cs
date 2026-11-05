using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView
{
    public enum ShopViewCommandKind
    {
        None = 0,
        Purchase = 1,
        Complete = 2,
        PurchaseTraining = 3
    }

    /// <summary>구매·나가기 의도만 담습니다. 상품 번호는 실제 Core 발급 행동 키입니다.</summary>
    public readonly struct ShopViewCommandData
    {
        public ShopViewCommandKind Kind { get; }
        public ShopOfferId OfferId { get; }
        public ShopVisitContext Context { get; }
        public CharacterInstanceId OwnedId { get; }
        public TraitKey TraitKey { get; }
        public int ExpectedRank { get; }
        public int ChapterKey => Context.ChapterKey;
        public StageNodeId NodeId => Context.NodeId;
        private ShopViewCommandData(ShopViewCommandKind kind, ShopOfferId offerId, ShopVisitContext context,
            CharacterInstanceId ownedId = default, TraitKey traitKey = TraitKey.None, int expectedRank = 0)
        {
            Kind = kind;
            OfferId = offerId;
            Context = context;
            OwnedId = ownedId;
            TraitKey = traitKey;
            ExpectedRank = expectedRank;
        }

        public static ShopViewCommandData Purchase(ShopVisitContext context, ShopOfferId offerId)
        {
            ValidateContext(context);
            if (offerId.IsEmpty) throw new ArgumentException("구매할 상품 키가 필요합니다.", nameof(offerId));
            return new ShopViewCommandData(ShopViewCommandKind.Purchase, offerId, context);
        }
        public static ShopViewCommandData Complete(ShopVisitContext context)
        {
            ValidateContext(context);
            return new ShopViewCommandData(ShopViewCommandKind.Complete, ShopOfferId.Empty, context);
        }
        public static ShopViewCommandData PurchaseTraining(ShopVisitContext context,
            CharacterInstanceId ownedId, TraitKey traitKey, int expectedRank)
        {
            var data = new ShopViewCommandData(ShopViewCommandKind.PurchaseTraining, ShopOfferId.Empty,
                context, ownedId, traitKey, expectedRank);
            data.Validate();
            return data;
        }
        private static void ValidateContext(ShopVisitContext context)
        {
            _ = new ShopVisitContext(context.ChapterKey, context.NodeId, context.StageKey, context.EntryId);
        }

        internal void Validate()
        {
            if (!((Kind == ShopViewCommandKind.Purchase && !OfferId.IsEmpty) || Kind == ShopViewCommandKind.Complete ||
                  (Kind == ShopViewCommandKind.PurchaseTraining && OfferId.IsEmpty)))
                throw new ArgumentException("유효한 상점 구매·나가기 요청이 필요합니다.");
            ValidateContext(Context);
            if (Kind == ShopViewCommandKind.PurchaseTraining)
            {
                if (OwnedId.IsEmpty) throw new ArgumentException("훈련할 보유 캐릭터가 필요합니다.", nameof(OwnedId));
                if (TraitKey == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), TraitKey))
                    throw new ArgumentException("훈련할 특성 키가 필요합니다.", nameof(TraitKey));
                if (ExpectedRank < 1 || ExpectedRank > 2) throw new ArgumentOutOfRangeException(nameof(ExpectedRank));
            }
        }
    }

    public readonly struct ShopViewCommand
    {
        public int Number { get; }
        public ShopViewCommandData Data { get; }
        internal ShopViewCommand(int number, ShopViewCommandData data)
        {
            if (number <= 0) throw new ArgumentOutOfRangeException(nameof(number));
            data.Validate();
            Number = number;
            Data = data;
        }
    }
}
