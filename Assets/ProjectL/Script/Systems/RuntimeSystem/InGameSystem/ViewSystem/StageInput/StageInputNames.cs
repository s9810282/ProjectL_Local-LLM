using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;

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
            internal const string Map = GlobalInputNames.CommonMap;
            internal const string OpenSettings = GlobalInputNames.OpenSettings;
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
            internal const string AttackMode = "AttackMode";
            /// <summary>이동 되돌리기입니다(Q, 옛 복귀 이름을 그대로 씀, 스킬 View 설계 Q-SK1).</summary>
            internal const string Return = "Return";
            internal const string CycleUnitCategory = "CycleUnitCategory";
            /// <summary>플레이어 액티브 스킬 모드 진입·끝입니다(R, 스킬 View 설계 Q-AS1·SK6).</summary>
            internal const string Skill = "Skill";
        }

        /// <summary>배치·전투 두 Layer가 함께 켜는 전투 카메라 조작 맵입니다(카메라 조작 설계 CC2).</summary>
        internal static class CombatCamera
        {
            internal const string Map = "CombatCamera";
            /// <summary>이동 방향입니다(Value Vector2, WASD·화살표 2D 묶음). 키를 떼면 (0, 0)입니다.</summary>
            internal const string Pan = "Pan";
            /// <summary>휠입니다(PassThrough Axis, <c>&lt;Mouse&gt;/scroll/y</c>). 위로 굴리면 양수입니다.</summary>
            internal const string Zoom = "Zoom";
            /// <summary>회전입니다(Value Axis, Z = −1·C = +1 1D 묶음, 카메라 회전 설계 CR5). 키를 떼면 0, 둘 다 누르면 0입니다.</summary>
            internal const string Rotate = "Rotate";
            /// <summary>가운데 버튼 끌기 회전의 누름입니다(Button, <c>&lt;Mouse&gt;/middleButton</c>, 카메라 회전 설계 CR8). 끈 양은 포인터 위치로 셉니다.</summary>
            internal const string RotateDrag = "RotateDrag";
        }
    }
}
