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
#if UNITY_EDITOR
        internal const string CameraModifier = "CameraModifier";
        internal const string CameraDolly = "CameraDolly";
        internal const string CameraZoom = "CameraZoom";
#endif
    }
}
