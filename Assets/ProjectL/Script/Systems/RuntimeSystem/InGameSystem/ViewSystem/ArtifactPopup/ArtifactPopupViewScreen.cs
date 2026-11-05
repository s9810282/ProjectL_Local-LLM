using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>아티팩트 화면을 공통 팝업 수명에 연결합니다.</summary>
    internal sealed class ArtifactPopupViewScreen : IPopupViewScreen
    {
        private readonly IArtifactPopupLifetime _screen;

        internal ArtifactPopupViewScreen(IArtifactPopupLifetime screen)
        {
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        }

        public UIId Id => UIId.Artifact;
        public bool IsOpen => _screen.IsOpen;

        public void Subscribe(Action<bool> openStateChanged)
        {
            _screen.OpenStateChanged += openStateChanged;
        }

        public void Unsubscribe(Action<bool> openStateChanged)
        {
            _screen.OpenStateChanged -= openStateChanged;
        }
    }
}
