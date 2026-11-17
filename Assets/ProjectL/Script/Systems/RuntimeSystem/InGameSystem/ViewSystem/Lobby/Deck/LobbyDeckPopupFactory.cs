using System;
using System.Collections.Generic;
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
        private readonly LobbyMainSelection _selection;
        // 메인 후보 카드(이름·아이콘)입니다. 연결마다 생성자에서 한 번 만들고 팝업을 열 때마다 다시 만들지 않습니다.
        private readonly IReadOnlyList<LobbyDeckCardContent> _cards;
        private readonly RuntimeFaultBoundary _faults;

        /// <remarks>
        /// System이 이전 연결을 떼기 전에 부릅니다. 메인 후보의 외형 행·아이콘이 없으면 여기서 예외를 던져
        /// 불완전한 새 연결로 바꿔 끼우지 않습니다. 예외가 오류 경계에 닿으면 기존 연결도 정리됩니다(로비 메인 선택 설계 LM2).
        /// </remarks>
        internal LobbyDeckPopupFactory(LobbyDeckServices deck, ILobbyDeckDisplay screen, LobbyMainSelection selection,
            RuntimeFaultBoundary faults)
        {
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            _ui = deck.UI;
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _cards = LobbyMainCards.Build(selection.Candidates, deck.UnitVisuals);
        }

        public ILobbyPopup Create(IPopupViewHost host, PopupInputSession input, Camera output, Transform lobbyRoot)
        {
            // 덱 화면의 닫기 입력은 컨트롤러보다 먼저 만드는 Presenter가 받으므로, 닫을 때 컨트롤러를 찾습니다.
            LobbyDeckPopupController controller = null;
            var presenter = new LobbyDeckPresenter(_screen, _selection, _cards, () => controller?.Close());
            controller = new LobbyDeckPopupController(_ui, _screen, presenter, input, host, _faults);
            return controller;
        }
    }
}
