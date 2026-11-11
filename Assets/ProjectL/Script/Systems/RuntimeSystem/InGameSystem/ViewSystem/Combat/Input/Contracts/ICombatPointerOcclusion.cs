using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 화면 좌표가 UI(Canvas)에 가려졌는지 알려 주는 좁은 계약입니다(행동 버튼 설계 B5).
    /// Router(버튼을 누른 마우스 떼기가 버튼 뒤 칸의 공격으로 기록되지 않게), 유닛 호버·환경 호버(UI 위에서는 호버를 내려놓음), 카메라 입력(UI 위에서는 가장자리 이동 안 함)이 읽습니다.
    /// </summary>
    /// <remarks>구현: <see cref="CombatPointerMapper"/>(씬 표시 나누기 설계 DS2). 연결 전·해제 뒤는 false입니다.</remarks>
    internal interface ICombatPointerOcclusion
    {
        /// <summary>화면 좌표 아래에 UI Graphic이 있는지입니다. 연결 전·해제 뒤·EventSystem이 없으면 false입니다.</summary>
        bool IsPointerOverUi(float screenX, float screenY);
    }
}
