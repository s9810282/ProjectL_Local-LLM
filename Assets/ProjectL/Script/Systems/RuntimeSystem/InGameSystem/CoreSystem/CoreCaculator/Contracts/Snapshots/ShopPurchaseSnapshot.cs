using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class ShopPurchaseSnapshot
    {
        public ShopOfferId OfferId { get; }
        public EntityKey CharacterKey { get; }
        public int Cost { get; }

        public ShopPurchaseSnapshot(ShopOfferId offerId, EntityKey characterKey, int cost)
        {
            if (offerId.IsEmpty || offerId.Value > ShopStageSnapshot.OfferCount) throw new ArgumentException(nameof(offerId));
            if (characterKey == EntityKey.None) throw new ArgumentException(nameof(characterKey));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            OfferId = offerId;
            CharacterKey = characterKey;
            Cost = cost;
        }
    }
}
