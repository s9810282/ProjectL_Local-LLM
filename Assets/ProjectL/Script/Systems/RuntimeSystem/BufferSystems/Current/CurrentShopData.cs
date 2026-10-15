using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    /// <summary>현재 선택한 상점의 고정 상품과 구매 표시다.</summary>
    public sealed class CurrentShopData
    {
        public IReadOnlyList<ShopPurchaseSnapshot> Offers { get; }
        public int PurchasedMask { get; }

        internal CurrentShopData(IReadOnlyList<ShopPurchaseSnapshot> offers, int purchasedMask = 0)
        {
            // 슬롯 수는 Core 계약(ShopStageSnapshot.OfferCount) 한 곳에서 정하고, 구매 표시 범위는 슬롯 수에서 계산한다.
            if (offers == null || offers.Count != ShopStageSnapshot.OfferCount)
                throw new ArgumentException("상점 상품 슬롯 수가 계약과 다릅니다.", nameof(offers));
            if (purchasedMask < 0 || purchasedMask >= 1 << offers.Count)
                throw new ArgumentOutOfRangeException(nameof(purchasedMask));

            var copy = new ShopPurchaseSnapshot[offers.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                if (offers[i] == null || offers[i].OfferId.Value != i + 1)
                    throw new ArgumentException("상점 상품은 슬롯 번호 순서로 지정해야 합니다.");
                copy[i] = offers[i];
            }

            Offers = Array.AsReadOnly(copy);
            PurchasedMask = purchasedMask;
        }

        public bool IsPurchased(ShopOfferId id)
        {
            return id.Value >= 1 && id.Value <= Offers.Count &&
                   (PurchasedMask & (1 << (id.Value - 1))) != 0;
        }

        public bool TryGetOffer(ShopOfferId id, out ShopPurchaseSnapshot offer)
        {
            offer = id.Value >= 1 && id.Value <= Offers.Count ? Offers[id.Value - 1] : null;
            return offer != null;
        }

        internal CurrentShopData WithPurchase(ShopOfferId id)
        {
            if (TryGetOffer(id, out _) == false || IsPurchased(id))
                throw new InvalidOperationException("구매할 상품 슬롯의 현재 상태가 맞지 않습니다.");
            return new CurrentShopData(Offers, PurchasedMask | (1 << (id.Value - 1)));
        }
    }
}
