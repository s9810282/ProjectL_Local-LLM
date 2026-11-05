using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class ShopCandidateSpec
    {
        public ShopProductKind Kind { get; }
        public EntityKey CharacterKey { get; }
        public int ItemKey { get; }
        public int Cost { get; }
        public int InitialLevel { get; }
        public string ItemName { get; } = string.Empty;
        public string ItemDescription { get; } = string.Empty;

        // 기존 유닛 전용 호출자는 후속 단계에서 옮긴다.
        public ShopCandidateSpec(EntityKey characterKey, int cost, int initialLevel = 1)
        {
            if (characterKey == EntityKey.None) throw new ArgumentException(nameof(characterKey));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (initialLevel < 1) throw new ArgumentOutOfRangeException(nameof(initialLevel));
            Kind = ShopProductKind.Unit;
            CharacterKey = characterKey;
            Cost = cost;
            InitialLevel = initialLevel;
        }

        private ShopCandidateSpec(int itemKey, int cost, string name, string description)
        {
            if (itemKey < 1) throw new ArgumentOutOfRangeException(nameof(itemKey));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("아이템 이름이 필요합니다.", nameof(name));
            Kind = ShopProductKind.ConsumableItem;
            ItemKey = itemKey;
            Cost = cost;
            ItemName = name;
            ItemDescription = description ?? throw new ArgumentNullException(nameof(description));
        }

        public static ShopCandidateSpec ForUnit(EntityKey key, int cost) => new ShopCandidateSpec(key, cost);
        public static ShopCandidateSpec ForItem(int itemKey, int cost, string name, string description)
            => new ShopCandidateSpec(itemKey, cost, name, description);
    }
}
