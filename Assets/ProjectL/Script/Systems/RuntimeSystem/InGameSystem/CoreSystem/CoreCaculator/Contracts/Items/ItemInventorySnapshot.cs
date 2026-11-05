using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items
{
    /// <summary>한 Run의 미장착 재고를 보관하는 불변 값입니다. 0개 항목은 생략합니다.</summary>
    public sealed class ItemInventorySnapshot
    {
        public static ItemInventorySnapshot Empty { get; } = new ItemInventorySnapshot(new Dictionary<int, int>());
        public IReadOnlyDictionary<int, int> Counts { get; }

        public ItemInventorySnapshot(IReadOnlyDictionary<int, int> counts)
        {
            if (counts == null) throw new ArgumentNullException(nameof(counts));
            var copy = new Dictionary<int, int>();
            foreach (var pair in counts)
            {
                if (pair.Key <= 0 || pair.Value < 0) throw new ArgumentOutOfRangeException(nameof(counts));
                if (pair.Value != 0) copy.Add(pair.Key, pair.Value);
            }
            Counts = new ReadOnlyDictionary<int, int>(copy);
        }

        public int GetCount(int itemKey)
        {
            if (itemKey <= 0) throw new ArgumentOutOfRangeException(nameof(itemKey));
            return Counts.TryGetValue(itemKey, out int count) ? count : 0;
        }

        public bool SameValues(ItemInventorySnapshot other)
        {
            if (other == null || Counts.Count != other.Counts.Count) return false;
            foreach (var pair in Counts)
                if (other.GetCount(pair.Key) != pair.Value) return false;
            return true;
        }
    }
}
