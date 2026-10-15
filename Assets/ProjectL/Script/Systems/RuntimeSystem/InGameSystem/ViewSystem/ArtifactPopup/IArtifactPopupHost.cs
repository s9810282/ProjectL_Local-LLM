namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// 아티팩트 팝업 UI가 열림·숨김을 View 시스템에 알리는 좁은 계약입니다. 이벤트 구독 대신 명시적으로 연결합니다.
    /// </summary>
    /// <remarks>구현: <see cref="ArtifactViewSystem"/>, 테스트 가짜.</remarks>
    internal interface IArtifactPopupHost
    {
        /// <summary>지금 팝업을 열 수 있는지입니다. 씬 연결 전이거나 런타임 오류 상태면 false입니다.</summary>
        bool CanOpen { get; }

        /// <summary>UISystem이 팝업 UI를 연 직후 호출합니다. 3D 표시·입력 Layer 전환을 시작합니다.</summary>
        void PopupOpened();

        /// <summary>UISystem이 열려 있던 팝업 UI를 숨긴 직후 호출합니다(닫기 버튼·ESC·뒤로가기·전체 숨김).</summary>
        void PopupHidden();
    }
}
