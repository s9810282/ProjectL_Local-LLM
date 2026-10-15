using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// View 시스템이 아티팩트 팝업 UI에 요구하는 좁은 계약입니다. 3D·입력 Layer·Core 요청은 View가 맡습니다.
    /// </summary>
    /// <remarks>구현: <c>ArtifactPopupUI</c>(현강 UI 구조의 <c>UIBase</c>), 테스트 가짜.</remarks>
    internal interface IArtifactPopupScreen
    {
        /// <summary>열림·숨김을 알릴 View를 연결합니다. 씬 연결 때 한 번 호출합니다.</summary>
        void Connect(IArtifactPopupHost host);

        /// <summary>연결을 끊습니다. 이후 숨겨져도 View를 부르지 않습니다. 여러 번 불러도 안전합니다.</summary>
        void Disconnect();

        /// <summary>상태·결과 문구를 표시합니다. 빈 문자열이면 지웁니다. 문구 내용은 View가 정합니다.</summary>
        void ShowStatus(string message);

        /// <summary>아티팩트 이름·효과 설명을 표시합니다. 문구 조회는 UI가 View DB에서 합니다.</summary>
        void ShowDetail(int artifactId);

        /// <summary>화면 좌표가 하단 보유 목록의 슬롯 위면 그 아티팩트 종류 ID를 돌려줍니다.</summary>
        bool TryPickPool(Vector2 screen, out int artifactId);

        /// <summary>화면 좌표가 하단 반환 영역(보유 목록 영역) 안인지 확인합니다.</summary>
        bool IsReturnArea(Vector2 screen);

        /// <summary>화면 좌표가 보드 조작을 막아야 하는 UI(닫기 버튼·상세·상태 문구) 위인지 확인합니다.</summary>
        bool IsBlockingArea(Vector2 screen);
    }
}
