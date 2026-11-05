using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby.Deck
{
    /// <summary>덱 팝업의 생성 입구입니다. 화면은 로비 씬 루트의 UI가 갖습니다(로비 리팩토링 2차 설계 M-14).</summary>
    internal sealed class LobbyDeckPopupFactory : ILobbyPopupFactory
    {
        private readonly IUISystemService _ui;
        private readonly ILobbyDeckDisplay _screen;
        private readonly RuntimeFaultBoundary _faults;

        internal LobbyDeckPopupFactory(IUISystemService ui, ILobbyDeckDisplay screen, RuntimeFaultBoundary faults)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        public ILobbyPopup Create(IPopupViewHost host, PopupInputSession input, Camera output, Transform lobbyRoot)
        {
            // 덱 화면의 닫기 입력은 컨트롤러보다 먼저 만드는 Presenter가 받으므로, 닫을 때 컨트롤러를 찾습니다.
            LobbyDeckPopupController controller = null;
            var presenter = new LobbyDeckPresenter(_screen, () => controller?.Close());
            controller = new LobbyDeckPopupController(_ui, _screen, presenter, input, host, _faults);
            return controller;
        }
    }
}
