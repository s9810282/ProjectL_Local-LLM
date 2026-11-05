using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>덱 슬롯·미편성 목록·공용 상세창의 입력을 소유 화면에 직접 전달합니다.</summary>
    internal interface IDeckEditInteractionReceiver
    {
        void ShowSlotInfo(int index);
        void ShowOwnedInfo(CharacterInstanceId ownedId);
        void BeginSlotChange(int index);
        void DeleteSlot(int index);
        void CancelSlotChange();
        bool ClickOwned(OwnedUnitSlot slot);
        void CloseSlotInfo();
        void SlotInfoHidden();
    }
}
