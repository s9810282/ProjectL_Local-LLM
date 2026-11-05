using UnityEngine.EventSystems;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>슬롯의 Unity 드래그 입력을 화면 소유자에게 직접 전달합니다.</summary>
    internal interface IDeckEditDragReceiver
    {
        bool BeginOwnedDrag(OwnedUnitSlot source, PointerEventData eventData);
        void MoveOwnedDrag(OwnedUnitSlot source, PointerEventData eventData);
        void EndOwnedDrag(OwnedUnitSlot source);
        void DropOwned(int slotIndex, PointerEventData eventData);
    }
}
