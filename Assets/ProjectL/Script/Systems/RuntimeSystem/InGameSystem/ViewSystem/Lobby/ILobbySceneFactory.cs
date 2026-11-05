using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비 씬 연결 한 번의 Scope를 만드는 유일한 입구입니다. 팝업 Factory 조립(씬의 팝업 루트 찾기 포함)은 구현(Factory 폴더)이 맡고,
    /// System은 씬을 찾고 서비스 묶음을 넘기기만 합니다(검사 후속 정리 계획 TP-R1, 로비 리팩토링 2차 설계 M-14).
    /// </summary>
    internal interface ILobbySceneFactory
    {
        /// <summary>
        /// 씬의 팝업 루트를 찾아 팝업 부품을 만듭니다. System이 이전 연결을 떼기 전에 불러,
        /// 씬 구성이 틀리면 기존 연결을 건드리지 않고 멈춥니다.
        /// </summary>
        LobbyViewPopups FindPopups(LobbySceneRoot root, LobbyViewServices services, RuntimeFaultBoundary faults);

        /// <param name="output">화면을 그리는 출력 카메라입니다(Boot의 Main Camera).</param>
        LobbyViewScope Create(LobbySceneRoot root, Camera output, LobbyViewServices services, LobbyViewPopups popups,
            RuntimeFaultBoundary faults);
    }
}
