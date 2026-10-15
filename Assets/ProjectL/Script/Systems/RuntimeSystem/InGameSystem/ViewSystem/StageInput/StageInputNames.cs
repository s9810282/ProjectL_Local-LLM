namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>
    /// 스테이지 내부 입력이 쓰는 ActionMap·Action 이름입니다.
    /// Assets/ProjectL/Input/PlayerInputActions.inputactions의 이름과 글자까지 같아야 합니다.
    /// 에셋과 맞는지는 입력 연결 계층이 시작할 때 검증합니다.
    /// </summary>
    internal static class StageInputNames
    {
        /// <summary>모든 스테이지 내부 Layer가 함께 켜는 공통 맵입니다.</summary>
        internal static class Common
        {
            internal const string Map = "Common";
            internal const string OpenSettings = "OpenSettings";
        }

        /// <summary>전투 진입 전 배치 단계의 맵입니다.</summary>
        internal static class Deployment
        {
            internal const string Map = "Deployment";
            internal const string PointerPosition = "PointerPosition";
            /// <summary>선택 버튼입니다. 에셋에서 Press(behavior=1)(ReleaseOnly)로 설정되어 있습니다.</summary>
            internal const string Press = "Press";
            internal const string Cancel = "Cancel";
            internal const string EnterCombat = "EnterCombat";
            internal const string DrawCharacter = "DrawCharacter";
        }

        /// <summary>전투 단계의 맵입니다.</summary>
        internal static class Combat
        {
            internal const string Map = "Combat";
            internal const string PointerPosition = "PointerPosition";
            /// <summary>선택 버튼입니다. 에셋에서 Press(behavior=1)(ReleaseOnly)로 설정되어 있습니다.</summary>
            internal const string Select = "Select";
            internal const string Cancel = "Cancel";
            internal const string EndTurn = "EndTurn";
            internal const string MoveMode = "MoveMode";
            internal const string AttackMode = "AttackMode";
            internal const string Return = "Return";
        }
    }
}
