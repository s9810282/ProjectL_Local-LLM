using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    public sealed class TrainingPurchasePreparation
    {
        public ShopVisitContext Context { get; }
        public CurrentShopData ExpectedShop { get; }
        public int ExpectedBalance { get; }
        public long ExpectedRosterRevision { get; }
        public CharacterInstanceId OwnedId { get; }
        public TraitKey TraitKey { get; }
        public int ExpectedRank { get; }
        public TrainingPurchasePreparation(ShopVisitContext context, CurrentShopData expectedShop, int expectedBalance,
            long expectedRosterRevision, CharacterInstanceId ownedId, TraitKey traitKey, int expectedRank)
        {
            Context = new ShopVisitContext(context.ChapterKey, context.NodeId, context.StageKey, context.EntryId);
            ExpectedShop = expectedShop ?? throw new ArgumentNullException(nameof(expectedShop));
            if (expectedBalance < 0 || expectedRosterRevision < 0 || ownedId.IsEmpty ||
                traitKey == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), traitKey) || expectedRank < 1 || expectedRank > 3)
                throw new ArgumentException("훈련 구매의 원본과 대상이 유효해야 합니다.");
            ExpectedBalance = expectedBalance;
            ExpectedRosterRevision = expectedRosterRevision;
            OwnedId = ownedId;
            TraitKey = traitKey;
            ExpectedRank = expectedRank;
        }
    }
}
