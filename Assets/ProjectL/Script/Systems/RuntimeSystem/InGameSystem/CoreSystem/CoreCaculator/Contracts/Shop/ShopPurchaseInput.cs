using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop
{
    public sealed class ShopPurchaseInput
    {
        public IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> Definitions { get; }
        public ShopPurchaseSnapshot Offer { get; }
        public bool HasPurchase { get; }
        public int Balance { get; }
        public IReadOnlyList<OwnedCharacterRecord> Owned { get; }
        public CharacterDefinitionFacts Unit { get; }
        public MainCharacterPersistentState Main { get; }
        public ItemInventorySnapshot Inventory { get; }
        public IReadOnlyList<ItemDefinition> Items { get; }

        public ShopPurchaseInput(ShopPurchaseSnapshot offer, bool hasPurchase, int balance,
            IReadOnlyList<OwnedCharacterRecord> owned, CharacterDefinitionFacts unit,
            MainCharacterPersistentState main, ItemInventorySnapshot inventory, IReadOnlyList<ItemDefinition> items, IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            if (items == null) throw new ArgumentNullException(nameof(items));
            // 유닛 상품의 소유 한도는 보유 정의 전체로 판정하므로 정의 표는 항상 받는다. 유닛 상품이 아니면 빈 표다.
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            var ownedCopy = new OwnedCharacterRecord[owned.Count];
            for (int i = 0; i < ownedCopy.Length; i++)
                ownedCopy[i] = owned[i] ?? throw new ArgumentException("보유 기록에 null을 넣을 수 없습니다.", nameof(owned));
            var itemCopy = new ItemDefinition[items.Count];
            for (int i = 0; i < itemCopy.Length; i++)
                itemCopy[i] = items[i] ?? throw new ArgumentException("아이템 정의에 null을 넣을 수 없습니다.", nameof(items));
            var copy = new Dictionary<EntityKey, CharacterDefinitionFacts>();
            foreach (var row in definitions)
            {
                if (row.Value == null || row.Value.Key != row.Key)
                    throw new ArgumentException("정의 키와 값이 일치해야 합니다.", nameof(definitions));
                copy.Add(row.Key, row.Value);
            }
            Definitions = new System.Collections.ObjectModel.ReadOnlyDictionary<EntityKey, CharacterDefinitionFacts>(copy);
            Offer = offer;
            HasPurchase = hasPurchase;
            Balance = balance;
            Owned = Array.AsReadOnly(ownedCopy);
            Items = Array.AsReadOnly(itemCopy);
            Unit = unit;
            Main = main;
        }
    }
}
