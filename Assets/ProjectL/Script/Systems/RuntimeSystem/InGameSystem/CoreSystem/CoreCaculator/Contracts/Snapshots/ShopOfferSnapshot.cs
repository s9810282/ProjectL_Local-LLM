using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class ShopOfferSnapshot
    {
        public ShopOfferId Id { get; }
        public EntityKey CharacterKey { get; }
        public int Cost { get; }
        public bool IsPurchased { get; }
        public bool CanPurchase { get; }
        public ShopFailureReason UnavailableReason { get; }

        public ShopOfferSnapshot(ShopOfferId id, EntityKey characterKey, int cost, bool isPurchased,
            bool canPurchase, ShopFailureReason unavailableReason)
        {
            if (id.IsEmpty || id.Value > ShopStageSnapshot.OfferCount) throw new ArgumentException(nameof(id));
            if (characterKey == EntityKey.None) throw new ArgumentException(nameof(characterKey));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (!Enum.IsDefined(typeof(ShopFailureReason), unavailableReason) ||
                canPurchase != (unavailableReason == ShopFailureReason.None) ||
                isPurchased != (unavailableReason == ShopFailureReason.SoldOut))
                throw new ArgumentException("Purchase status and availability must agree.");
            Id = id;
            CharacterKey = characterKey;
            Cost = cost;
            IsPurchased = isPurchased;
            CanPurchase = canPurchase;
            UnavailableReason = unavailableReason;
        }
    }
}
