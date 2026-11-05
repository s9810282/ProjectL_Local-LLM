using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 전투 가방의 표시 계약입니다(가방 설계 P1·P2). 전투 표시가 그리기 때 재고 스냅숏을 넘기고, 가방은 받은 값을 그리기만 합니다.
    /// </summary>
    /// <remarks>
    /// 가방은 Buffer를 읽지 않고 재고를 계산하지 않습니다. 보이는지(배치 국면)는 조정자가 따로 정합니다.
    /// </remarks>
    internal interface ICombatPackDisplay
    {
        /// <summary>칸이 쓸 아이템 외형 표를 받습니다. 표시 Factory가 씬 연결 때 한 번 부릅니다.</summary>
        void ConfigureItems(IConsumableItemVisualTableQuery visuals);

        /// <summary>재고를 그립니다. 같은 스냅숏 객체면 다시 그리지 않습니다.</summary>
        void ShowInventory(ItemInventorySnapshot inventory);

        /// <summary>끄는 아이템 그림을 보입니다. 위치는 이어지는 <see cref="MoveItemDrag"/>가 정합니다(가방 설계 P4).</summary>
        void ShowItemDrag(int itemKey);

        void MoveItemDrag(float screenX, float screenY);

        void HideItemDrag();
    }
}
