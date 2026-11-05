using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비 팝업 하나의 생성 입구입니다. 씬 루트 찾기는 이 Factory를 만들 때 하고, 출력 복원은 만든 항목이 맡습니다
    /// (로비 리팩토링 2차 설계 M-14).
    /// </summary>
    internal interface ILobbyPopupFactory
    {
        /// <param name="output">화면을 그리는 출력 카메라입니다(Boot의 Main Camera).</param>
        /// <param name="lobbyRoot">팝업 동안 숨길 로비 공간 루트(로비 씬 루트의 월드 루트)입니다.</param>
        ILobbyPopup Create(IPopupViewHost host, PopupInputSession input, Camera output, Transform lobbyRoot);
    }
}
