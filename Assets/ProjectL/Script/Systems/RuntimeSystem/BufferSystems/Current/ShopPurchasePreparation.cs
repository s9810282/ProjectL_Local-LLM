using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    /// <summary>구매 계산이 읽은 원본과 승인된 변경을 게시 경계까지 고정한다.</summary>
    public sealed class ShopPurchasePreparation
    {
        public ShopVisitContext Context { get; }
        public CurrentShopData ExpectedShop { get; }
        public int ExpectedBalance { get; }
        public IReadOnlyList<OwnedCharacterRecord> ExpectedOwned { get; }
        public MainCharacterPersistentState ExpectedMain { get; }
        public ShopOfferId OfferId { get; }
        public ShopPurchaseDecision Decision { get; }

        public ShopPurchasePreparation(ShopVisitContext context, CurrentShopData expectedShop,
            int expectedBalance, IReadOnlyList<OwnedCharacterRecord> expectedOwned,
            MainCharacterPersistentState expectedMain, ShopOfferId offerId, ShopPurchaseDecision decision)
        {
            Context = new ShopVisitContext(context.ChapterKey, context.NodeId, context.StageKey, context.EntryId);
            ExpectedShop = expectedShop ?? throw new ArgumentNullException(nameof(expectedShop));
            if (expectedShop.EntryId != context.EntryId)
                throw new ArgumentException("구매 원본과 방문 식별자가 다릅니다.", nameof(expectedShop));
            if (expectedBalance < 0) throw new ArgumentOutOfRangeException(nameof(expectedBalance));
            if (expectedOwned == null) throw new ArgumentNullException(nameof(expectedOwned));
            if (expectedOwned.Any(x => x == null)) throw new ArgumentException("보유 원본에 null이 있습니다.", nameof(expectedOwned));
            if (offerId.IsEmpty) throw new ArgumentException("구매 상품이 필요합니다.", nameof(offerId));
            if (decision == null || !decision.Succeeded)
                throw new ArgumentException("승인된 구매 제안이 필요합니다.", nameof(decision));
            ExpectedBalance = expectedBalance;
            ExpectedOwned = Array.AsReadOnly(expectedOwned.ToArray());
            ExpectedMain = expectedMain ?? throw new ArgumentNullException(nameof(expectedMain));
            OfferId = offerId;
            Decision = decision;
        }
    }
}
