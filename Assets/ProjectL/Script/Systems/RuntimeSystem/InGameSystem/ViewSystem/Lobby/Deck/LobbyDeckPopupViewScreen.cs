using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Lobby;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby.Deck
{
    /// <summary>덱 UI의 열림 상태와 알림을 공통 팝업 수명에 연결합니다.</summary>
    internal sealed class LobbyDeckPopupViewScreen : IPopupViewScreen
    {
        private readonly ILobbyDeckDisplay _view;

        internal LobbyDeckPopupViewScreen(ILobbyDeckDisplay view) =>
            _view = view ?? throw new ArgumentNullException(nameof(view));

        // 화면 ID의 원본은 UI 자신(UIBase.ID)입니다(로비 리팩토링 2차 설계 N-42).
        public UIId Id => _view.ID;
        public bool IsOpen => _view.IsOpen;
        public void Subscribe(Action<bool> openStateChanged) => _view.OpenStateChanged += openStateChanged;
        public void Unsubscribe(Action<bool> openStateChanged) => _view.OpenStateChanged -= openStateChanged;
    }
}
