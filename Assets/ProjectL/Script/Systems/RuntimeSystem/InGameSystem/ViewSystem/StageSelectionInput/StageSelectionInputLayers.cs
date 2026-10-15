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
    }
}
