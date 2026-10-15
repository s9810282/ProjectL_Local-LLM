using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class ShopReceiptSnapshot
    {
        public ReadOnlyMemory<ShopPurchaseSnapshot> Purchases { get; }
        public int PurchaseCount => Purchases.Length;
        public int TotalSpent { get; }

        public ShopReceiptSnapshot(IReadOnlyList<ShopPurchaseSnapshot> purchases)
        {
            if (purchases == null) throw new ArgumentNullException(nameof(purchases));
            if (purchases.Count > ShopStageSnapshot.OfferCount) throw new ArgumentException(nameof(purchases));
            var copy = new ShopPurchaseSnapshot[purchases.Count];
            int previousId = 0;
            int total = 0;
            for (int i = 0; i < copy.Length; i++)
            {
                var purchase = purchases[i];
                if (purchase == null || purchase.OfferId.Value <= previousId)
                    throw new ArgumentException("Purchases must have unique IDs in slot order.", nameof(purchases));
                previousId = purchase.OfferId.Value;
                total = checked(total + purchase.Cost);
                copy[i] = purchase;
            }
            Purchases = copy;
            TotalSpent = total;
        }
    }
}
