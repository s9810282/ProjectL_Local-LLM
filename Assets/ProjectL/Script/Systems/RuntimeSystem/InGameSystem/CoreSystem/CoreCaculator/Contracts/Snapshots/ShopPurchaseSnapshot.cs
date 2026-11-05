using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class ShopPurchaseSnapshot
    {
        public CharacterInstanceId TargetOwnedId { get; }
        public OwnedEnhancement Enhancement { get; }
        public string EnhancementName { get; } = "";
        public string EnhancementDescription { get; } = "";
        public ShopOfferId OfferId { get; }
        public ShopProductKind Kind { get; }
        public EntityKey CharacterKey { get; }
        public int ItemKey { get; }
        public int Cost { get; }
        public int InitialLevel { get; }
        public string ItemName { get; }
        public string ItemDescription { get; }

        // 기존 유닛 전용 호출자의 가격과 레벨을 그대로 보존한다.
        public ShopPurchaseSnapshot(ShopOfferId offerId, EntityKey characterKey, int cost, int initialLevel = 1)
            : this(offerId, new ShopCandidateSpec(characterKey, cost, initialLevel), initialLevel)
        {
        }

        private ShopPurchaseSnapshot(ShopOfferId id, ShopCandidateSpec candidate, int level)
        {
            if (id.IsEmpty || id.Value > ShopOfferLimits.Maximum) throw new ArgumentException(nameof(id));
            OfferId = id;
            Kind = candidate.Kind;
            CharacterKey = candidate.CharacterKey;
            ItemKey = candidate.ItemKey;
            Cost = candidate.Cost;
            InitialLevel = level;
            ItemName = candidate.ItemName;
            ItemDescription = candidate.ItemDescription;
        }

        private ShopPurchaseSnapshot(ShopOfferId id, CharacterInstanceId targetId, EntityKey key,
            OwnedEnhancement enhancement, int cost, string name, string description)
        {
            if (id.IsEmpty || id.Value > ShopOfferLimits.Maximum) throw new ArgumentException(nameof(id));
            if (targetId.IsEmpty) throw new ArgumentException(nameof(targetId));
            if (key == EntityKey.None) throw new ArgumentException(nameof(key));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            Enhancement = enhancement ?? throw new ArgumentNullException(nameof(enhancement));
            if (enhancement.Level != 1) throw new ArgumentOutOfRangeException(nameof(enhancement));
            OfferId = id;
            TargetOwnedId = targetId;
            CharacterKey = key;
            Kind = ShopProductKind.Enhancement;
            Cost = cost;
            EnhancementName = name ?? throw new ArgumentNullException(nameof(name));
            EnhancementDescription = description ?? throw new ArgumentNullException(nameof(description));
            ItemName = "";
            ItemDescription = "";
        }

        public static ShopPurchaseSnapshot ForEnhancement(ShopOfferId id, CharacterInstanceId targetId,
            EntityKey characterKey, OwnedEnhancement enhancement, int cost, string name, string description)
            => new ShopPurchaseSnapshot(id, targetId, characterKey, enhancement, cost, name, description);

        public static ShopPurchaseSnapshot ForUnit(ShopOfferId id, EntityKey key, int cost, int level)
            => new ShopPurchaseSnapshot(id, key, cost, level);
        public static ShopPurchaseSnapshot ForItem(ShopOfferId id, int itemKey, int cost, string name, string description)
            => new ShopPurchaseSnapshot(id, ShopCandidateSpec.ForItem(itemKey, cost, name, description), 0);
    }
}
