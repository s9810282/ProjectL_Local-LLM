using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck
{
    /// <summary>덱 편집 화면 알림을 기존 팝업 수명에 연결합니다.</summary>
    internal sealed class DeckEditPopupViewScreen : IPopupViewScreen, IScrollPopupDisplay
    {
        private readonly IDeckEditDisplay _screen;

        internal DeckEditPopupViewScreen(IDeckEditDisplay screen)
        {
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        }

        public UIId Id => UIId.DeckEdit;
        public bool IsOpen => _screen.IsOpen;
        public ScrollPanelPresentation ScrollPresentation => (_screen as IScrollPopupDisplay)?.ScrollPresentation;
        public void Subscribe(Action<bool> changed) => _screen.OpenStateChanged += changed;
        public void Unsubscribe(Action<bool> changed) => _screen.OpenStateChanged -= changed;
    }
}
