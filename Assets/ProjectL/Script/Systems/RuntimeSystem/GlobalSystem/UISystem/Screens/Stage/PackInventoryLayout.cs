using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>
    /// 재고 스냅숏을 가방 칸 순서로 바꿉니다(가방 설계 P2). 아이템 키 순으로 앞 칸부터 채우고, 칸보다 많은 종류는 넘친 수로 돌려줍니다.
    /// </summary>
    /// <remarks>Unity를 쓰지 않는 순수 계산입니다. 재고의 0개 항목은 스냅숏에 없으므로 따로 거르지 않습니다.</remarks>
    internal static class PackInventoryLayout
    {
        internal readonly struct Entry
        {
            internal Entry(int itemKey, int count)
            {
                ItemKey = itemKey;
                Count = count;
            }

            internal int ItemKey { get; }
            internal int Count { get; }
        }

        /// <summary>칸에 놓을 항목을 <paramref name="output"/>에 채우고, 칸이 모자라 놓지 못한 종류 수를 돌려줍니다.</summary>
        internal static int Build(ItemInventorySnapshot inventory, int capacity, List<Entry> output)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            if (output == null) throw new ArgumentNullException(nameof(output));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

            output.Clear();
            foreach (var pair in inventory.Counts)
                output.Add(new Entry(pair.Key, pair.Value));
            output.Sort((a, b) => a.ItemKey.CompareTo(b.ItemKey));

            int overflow = Math.Max(0, output.Count - capacity);
            if (overflow > 0)
                output.RemoveRange(capacity, overflow);
            return overflow;
        }
    }
}
