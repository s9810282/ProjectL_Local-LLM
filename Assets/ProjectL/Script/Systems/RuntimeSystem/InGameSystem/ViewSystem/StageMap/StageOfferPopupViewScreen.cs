using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>공통 레이아웃 화면을 공통 팝업 수명에 연결합니다.</summary>
    internal sealed class StageOfferPopupViewScreen : IPopupViewScreen, IScrollPopupDisplay
    {
        private readonly IStageOfferDisplay _screen;

        internal StageOfferPopupViewScreen(IStageOfferDisplay screen)
        {
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        }

        public UIId Id => _screen.ID;
        public bool IsOpen => _screen.IsOpen;
        public ScrollPanelPresentation ScrollPresentation => (_screen as IScrollPopupDisplay)?.ScrollPresentation;
        public void Subscribe(Action<bool> openStateChanged) => _screen.OpenStateChanged += openStateChanged;
        public void Unsubscribe(Action<bool> openStateChanged) => _screen.OpenStateChanged -= openStateChanged;
    }
}
