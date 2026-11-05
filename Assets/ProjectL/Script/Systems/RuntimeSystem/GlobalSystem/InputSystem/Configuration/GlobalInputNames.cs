using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration
{
    /// <summary>공용 설정 액션과 옵션 전용 Layer의 이름을 한곳에서 관리합니다.</summary>
    internal static class GlobalInputNames
    {
        internal const string CommonMap = "Common";
        internal const string OpenSettings = "OpenSettings";
        internal const string SettingsMap = "Settings";
        internal const string CloseSettings = "Close";
        internal static readonly InputLayer Settings = new(SettingsMap, SettingsMap);
    }
}
