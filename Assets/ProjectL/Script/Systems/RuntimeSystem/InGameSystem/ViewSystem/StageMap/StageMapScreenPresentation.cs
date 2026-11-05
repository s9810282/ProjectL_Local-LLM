using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>팝업 한 번의 표시와 종료 후 복귀 권한을 실제 부모 수명에 귀속합니다.</summary>
    internal sealed class StageMapScreenPresentation : IDisposable
    {
        private readonly StageMapSelectionPresentation _owner;
        private readonly IPopupViewScreen _screen;
        private object _display;
        private object _displayLifetime;
        private object _closedDisplay;
        private object _closedLifetime;
        private int _closeDepth;
        private bool _started;
        private bool _disposed;

        internal StageMapScreenPresentation(StageMapSelectionPresentation owner, IPopupViewScreen screen)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
        }

        internal bool CanOpen => !_disposed && _owner.CanOpenScreens;

        internal void Start()
        {
            if (_started || _disposed) return;
            _started = true;
            _screen.Subscribe(OnOpenStateChanged);
            if (!_disposed) OnOpenStateChanged(_screen.IsOpen);
        }

        internal void Close(Action close, bool returnToSelection)
        {
            if (close == null) throw new ArgumentNullException(nameof(close));
            object display = _display;
            object lifetime = _displayLifetime;
            _closeDepth++;
            try
            {
                close();
            }
            finally
            {
                _closeDepth--;
            }
            if (returnToSelection && !_disposed && CanReturn(display, lifetime))
                _owner.RequestReturn(this, display, lifetime);
        }

        internal bool CanReturn(object display, object lifetime) => !_disposed && display != null &&
            ReferenceEquals(_closedDisplay, display) && ReferenceEquals(_closedLifetime, lifetime) &&
            _display == null && !_screen.IsOpen;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _display = _displayLifetime = _closedDisplay = _closedLifetime = null;
            if (_started) _screen.Unsubscribe(OnOpenStateChanged);
        }

        private void OnOpenStateChanged(bool _)
        {
            if (_disposed) return;
            // 재진입 Open 뒤에 전달된 옛 false 알림도 현재 화면 상태로 해석합니다.
            if (_screen.IsOpen)
            {
                if (_display != null) return;
                object lifetime = _owner.Lifetime;
                if (lifetime == null) return;
                _display = new object();
                _displayLifetime = lifetime;
                _closedDisplay = _closedLifetime = null;
                _owner.ScreenOpened(lifetime);
                return;
            }
            if (_display == null) return;
            object closed = _display;
            object closedLifetime = _displayLifetime;
            _display = _displayLifetime = null;
            _closedDisplay = closed;
            _closedLifetime = closedLifetime;
            // 내부 닫힘은 호출자가 정상 복귀 여부를 결정하고, 외부 닫힘은 Tick 뒤에만 소비합니다.
            if (_closeDepth == 0) _owner.RequestReturn(this, closed, closedLifetime);
        }
    }
}
