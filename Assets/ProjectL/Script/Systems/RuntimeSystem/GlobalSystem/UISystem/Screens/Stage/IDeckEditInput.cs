using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>덱 편집 화면이 넘기는 입력 의도입니다. 판단은 받는 쪽(초안·Core)이 합니다.</summary>
    public interface IDeckEditInput
    {
        /// <summary>미편성 유닛을 Main·Heavy·Rider·Cannon 순서의 고정 슬롯에 배치·교체합니다.</summary>
        void PlaceOwned(CharacterInstanceId ownedId, int slotIndex);

        /// <summary>덱 칸 하나를 비웁니다.</summary>
        void RemoveSlot(int index);

        void Confirm();

        /// <summary>최신 정보를 다시 읽어 지금 초안을 다시 평가합니다.</summary>
        void Reevaluate();

        void Close();
    }
}
