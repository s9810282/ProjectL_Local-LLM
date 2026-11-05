#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ItemSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev
{
    /// <summary>
    /// 개발 진입의 시작 아이템을 Core 공개 지급으로 넣습니다(시작 아이템 설계 DI2·DI4·DI6).
    /// 지급 가능 판정과 수량 계산은 Core가 하고, 여기서는 순서대로 부르고 거절을 예외로 바꿉니다.
    /// </summary>
    /// <remarks>
    /// 거절 앞에서 이미 지급된 줄은 되돌리지 않습니다. 예외는 오류 경계가 받아 런타임을 멈추고, 다시 시작은 새 런타임이기 때문입니다.
    /// 부르는 시점(새 Run 직후, 챕터 진입 전)은 GameBootstrap이 정합니다.
    /// </remarks>
    internal static class DevStartingItemGrant
    {
        /// <summary>
        /// 목록 순서대로 지급하고 마지막 지급 뒤의 재고를 돌려줍니다. 목록이 비면 아무것도 부르지 않고 null입니다.
        /// 거절이면 줄·키·수량·사유를 담은 예외를 던지고 남은 줄은 부르지 않습니다.
        /// </summary>
        internal static ItemInventorySnapshot Apply(IItemInventoryService items, IReadOnlyList<DevStartingItem> entries)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (entries == null) throw new ArgumentNullException(nameof(entries));

            ItemInventorySnapshot last = null;
            for (int i = 0; i < entries.Count; i++)
            {
                DevStartingItem entry = entries[i];
                ItemGrantResult result = items.Grant(entry.ItemKey, entry.Quantity)
                    ?? throw new InvalidOperationException(
                        $"[DevStartingItems] 아이템 지급이 결과를 돌려주지 않았습니다. 줄 {i + 1}, 키 {entry.ItemKey}");
                if (!result.Accepted)
                    throw new InvalidOperationException(
                        $"[DevStartingItems] 개발 시작 아이템 지급이 거절됐습니다. 줄 {i + 1}, 키 {entry.ItemKey}, 수량 {entry.Quantity}, 사유 {result.Reason}");
                last = result.Change.Final;
            }
            return last;
        }

        /// <summary>지급 로그 한 줄을 만듭니다. 예: <c>1×2, 4×1 → 재고 {1:2, 4:1}</c>. 재고는 키 순입니다.</summary>
        internal static string Describe(IReadOnlyList<DevStartingItem> entries, ItemInventorySnapshot final)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (final == null) throw new ArgumentNullException(nameof(final));

            var text = new StringBuilder();
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0) text.Append(", ");
                text.Append(entries[i].ItemKey).Append('×').Append(entries[i].Quantity);
            }
            text.Append(" → 재고 {");
            bool first = true;
            foreach (var pair in final.Counts.OrderBy(p => p.Key))
            {
                if (!first) text.Append(", ");
                text.Append(pair.Key).Append(':').Append(pair.Value);
                first = false;
            }
            return text.Append('}').ToString();
        }
    }
}
#endif
