using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput
{
    /// <summary>맵 선택 화면의 입력 Layer입니다. 비활성화에는 ClearLayer를 사용합니다.</summary>
    internal static class StageSelectionInputLayers
    {
        internal static readonly InputLayer Selection = new(
            StageSelectionInputNames.Map,
            StageSelectionInputNames.CommonMap,
            StageSelectionInputNames.Map);

        /// <summary>
        /// 상점·회복·덱 편집 공통 모달의 입력 Layer입니다. 맵 선택 액션 맵을 끄고 공용 맵(설정 열기)만 켭니다.
        /// <see cref="InputLayer"/>는 이름으로 같음을 판정하므로 이 이름은 화면 안에서 하나여야 합니다.
        /// </summary>
        /// <remarks>버튼은 EventSystem으로 들어오므로 별도 액션 맵이 없습니다. 닫기(ESC)도 두지 않습니다(닫기 = 노드 완료이므로 나가기 버튼만).</remarks>
        internal static readonly InputLayer Popup = new("StageMapPopup", StageSelectionInputNames.CommonMap);
    }
}
