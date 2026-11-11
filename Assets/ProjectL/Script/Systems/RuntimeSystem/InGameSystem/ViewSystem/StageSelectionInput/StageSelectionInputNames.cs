using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput
{
    /// <summary>PlayerInputActions의 맵 선택 액션 이름입니다.</summary>
    internal static class StageSelectionInputNames
    {
        internal const string Map = "StageSelection";
        internal const string CommonMap = GlobalInputNames.CommonMap;
        internal const string PointerPosition = "PointerPosition";
        internal const string Select = "Select";
        internal const string Confirm = "Confirm";
        internal const string Cancel = "Cancel";
        // 기존 Input Actions의 휠 액션 이름입니다(이름은 CameraZoom이지만 맵 카메라 이동에 씁니다).
        internal const string CameraScroll = "CameraZoom";
    }
}
