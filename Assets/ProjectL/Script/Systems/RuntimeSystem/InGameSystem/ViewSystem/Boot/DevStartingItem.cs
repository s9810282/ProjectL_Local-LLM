using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot
{
    /// <summary>
    /// 개발 진입이 새 Run 직후 지급할 아이템 한 줄(키·수량)입니다(시작 아이템 설계 DI5).
    /// Boot 씬 값과 이 PC의 Play From Boot 덮어쓰기가 같은 형식을 씁니다.
    /// </summary>
    /// <remarks>
    /// 값을 검사하지 않습니다. 키가 있는지·수량이 맞는지는 Core 지급이 판정하고, 설정 창은 1 미만만 안내합니다.
    /// 직렬화 필드는 모든 빌드에 있어야 하므로 조건부 컴파일을 걸지 않습니다.
    /// </remarks>
    [Serializable]
    public struct DevStartingItem
    {
        [SerializeField] private int itemKey;
        [SerializeField, Min(1)] private int quantity;

        public DevStartingItem(int itemKey, int quantity)
        {
            this.itemKey = itemKey;
            this.quantity = quantity;
        }

        /// <summary>DB 소모 아이템 키입니다.</summary>
        public int ItemKey => itemKey;

        /// <summary>지급할 수량입니다.</summary>
        public int Quantity => quantity;
    }
}
