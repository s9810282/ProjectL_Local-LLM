using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby
{
    /// <summary>
    /// 덱 팝업 화면의 계약입니다. 덱 컨트롤러·Presenter·팝업 수명 연결은 이 계약에만 기대고 구체 화면을 알지 않습니다
    /// (로비 리팩토링 2차 설계 N-56). 게임 규칙이나 View 타입을 참조하지 않습니다.
    /// </summary>
    public interface ILobbyDeckDisplay
    {
        UIId ID { get; }
        bool IsOpen { get; }
        event Action<bool> OpenStateChanged;

        /// <summary>
        /// 화면이 열려 있고 켜져 있어 입력을 받을 수 있는 상태인지입니다. 화면의 입력 수신 판정과 Presenter가 이 답 하나를 함께 씁니다
        /// (로비 리팩토링 2차 설계 N-45).
        /// </summary>
        bool IsShown { get; }

        void SetModel(LobbyDeckDisplayModel model);
        void ShowDetails(LobbyDeckCardContent card);
        void BindInput(IUIInputReceiver receiver);
    }
}
