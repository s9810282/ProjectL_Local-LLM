using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items
{
    public static class ItemInventoryCalculator
    {
        public static ItemGrantResult Grant(ItemInventorySnapshot inventory, IReadOnlyList<ItemDefinition> definitions,
            int itemKey, int quantity)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (definitions.Any(d => d == null) || definitions.Select(d => d.Key).Distinct().Count() != definitions.Count)
                throw new InvalidOperationException("아이템 정의에는 null 또는 중복 키를 넣을 수 없습니다.");
            if (!definitions.Any(d => d.Key == itemKey)) return new ItemGrantResult(false, "UnknownItem", null);
            if (quantity <= 0) return new ItemGrantResult(false, "InvalidQuantity", null);
            int final;
            try { final = checked(inventory.GetCount(itemKey) + quantity); }
            catch (OverflowException) { return new ItemGrantResult(false, "QuantityOverflow", null); }
            var counts = inventory.Counts.ToDictionary(p => p.Key, p => p.Value);
            counts[itemKey] = final;
            return new ItemGrantResult(true, "", new ItemInventoryChange(inventory, new ItemInventorySnapshot(counts)));
        }
    }
}
