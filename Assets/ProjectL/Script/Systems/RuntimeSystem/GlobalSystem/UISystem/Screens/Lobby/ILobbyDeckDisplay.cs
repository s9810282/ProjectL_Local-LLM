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

        /// <summary>
        /// 다음 Run 메인으로 고른 목록 칸을 표시합니다. 지금 목록의 그 칸만 켜고 나머지는 끕니다. -1이면 모두 끕니다.
        /// 무엇이 골라졌는지는 화면이 판단하지 않고 칸 번호만 받습니다.
        /// </summary>
        void ShowSelected(int slot);

        /// <summary>
        /// "다음 Run 메인으로 선택" 버튼을 누를 수 있는지 받습니다. 화면은 이 값과 자기 입력 수신 상태가 둘 다 참일 때만 버튼을 켭니다.
        /// 목록을 바꾸거나 수신자를 바꾸거나 닫으면 화면이 이 값을 거짓으로 되돌리므로, 부르는 쪽이 다시 판정해 넘깁니다.
        /// </summary>
        void SetSelectEnabled(bool enabled);
    }
}
