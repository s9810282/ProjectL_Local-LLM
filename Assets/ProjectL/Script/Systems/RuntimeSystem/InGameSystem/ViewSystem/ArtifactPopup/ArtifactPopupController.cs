using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// 아티팩트 팝업의 표시와 입력 수신을 소유합니다. 공유 장면(3D 보드·UI·입력 Buffer)은 "현재 호스트만" 감싼 판으로만 쥡니다.
    /// 호스트 검사는 감싸개 한 곳에서만 하므로, 교체된 연결의 정리가 새 연결의 장면을 건드리지 않습니다(로비 리팩토링 2차 설계 M-11).
    /// </summary>
    internal sealed class ArtifactPopupController : IPopupViewController, IDisposable
    {
        private readonly RuntimeFaultBoundary _faults;
        private readonly IInputLayerService _layers;
        private readonly IArtifactBufferReadSink _board;
        private readonly IArtifactViewBufferWriteSink _writer;
        private readonly IArtifactInputSlot _inputSlot;
        private readonly IArtifactPopupPresenter _presenter;
        private readonly IArtifactPopupLifetime _lifetime;
        private readonly IPopupViewHost _host;
        private readonly PopupViewScope _scope;
        private readonly ArtifactBoardRefresher _refresher;
        private readonly ArtifactPopupStatus _status;
        private readonly ArtifactDragController _drag;
        private readonly IArtifactPopupOutput _popupOutput;
        // 아티팩트 도메인 정지 사실입니다. 멈춘 동안 편집 입력을 막고 안내를 보입니다(로비 리팩토링 2차 설계 M-06).
        private readonly Func<bool> _isStopped;
        private IArtifactInputReceiver _inputReceiver;
        private IArtifactPopupCloseReceiver _closeReceiver;
        private bool _started;
        private bool _disposed;
        private bool _displayActive;

        /// <param name="pointer">화면 영역·보드 칸 판정입니다. Factory가 팝업 표시의 보드 변환기 하나로 만듭니다(N-02).</param>
        internal ArtifactPopupController(RuntimeFaultBoundary faults, ArtifactPopupServices services,
            IArtifactPopupPresenter presenter, IArtifactDragPresenter drag, IArtifactPopupDisplay display,
            IArtifactPopupLifetime lifetime, ArtifactPointerMapper pointer, PopupInputSession popupInput,
            IPopupViewHost host, IArtifactPopupOutput popupOutput)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (presenter == null) throw new ArgumentNullException(nameof(presenter));
            if (drag == null) throw new ArgumentNullException(nameof(drag));
            if (display == null) throw new ArgumentNullException(nameof(display));
            if (lifetime == null) throw new ArgumentNullException(nameof(lifetime));
            if (pointer == null) throw new ArgumentNullException(nameof(pointer));
            if (popupInput == null) throw new ArgumentNullException(nameof(popupInput));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _popupOutput = popupOutput ??
                throw new ArgumentNullException(nameof(popupOutput), "[ArtifactPopup] 씬 팝업은 출력 설정 수명이 필요합니다.");
            _layers = services.Layers;
            _board = services.Board;
            _inputSlot = services.Slot;
            _isStopped = services.IsStopped;
            // 공유 장면은 감싼 판만 필드로 둡니다. 원본은 여기서만 감싸고 다시 쓰지 않습니다(M-11).
            _writer = new OwnedWriter(services.Writer, host);
            var ownedPresenter = new OwnedPresenter(presenter, drag, host);
            _presenter = ownedPresenter;
            _lifetime = new OwnedLifetime(lifetime, host);
            var ownedDisplay = new OwnedDisplay(display, host);
            _status = new ArtifactPopupStatus(ownedDisplay, services.Delay);
            _refresher = new ArtifactBoardRefresher(_board, services.FindVisual, ownedPresenter, ownedDisplay, _status, CancelDrag);
            _drag = new ArtifactDragController(pointer, _board, services.Input, _writer,
                services.Results, _refresher, ownedPresenter, _status, AllowsInput);
            _scope = new PopupViewScope(services.UI, new ArtifactPopupViewScreen(_lifetime), popupInput,
                ArtifactInputLayers.Artifact, this, host, faults);
        }

        internal bool IsOpen => _scope.IsOpen;
        internal bool CanOpen => _started && CanWork;
        private bool CanWork => !_disposed && _host.IsCurrent && !_faults.IsFaulted;
        /// <summary>이 컨트롤러의 표시 연결이 지금 유효한지입니다. 입력·닫기 수신자와 입력 허용이 함께 씁니다(로비 리팩토링 2차 설계 N-43).</summary>
        private bool IsDisplayCurrent => CanWork && IsOpen && _displayActive;

        /// <summary>부모의 기본 Layer 연결 뒤에 화면 허용과 이미 열린 화면을 인수합니다.</summary>
        internal void Start()
        {
            if (_started || !CanWork) return;
            _started = true;
            try
            {
                _lifetime.BindClose(null);
                if (!CanWork) return;
                _presenter.Hide();
                if (CanWork) _scope.Start();
            }
            catch (Exception error)
            {
                var errors = new List<Exception> { error };
                CleanupErrors.Try(Dispose, ref errors);
                throw new AggregateException("[ArtifactPopup] 팝업 시작 중 실패했습니다.", errors);
            }
            if (CanWork) _lifetime.SetCanClose(CanOpen);
        }

        internal void Open()
        {
            if (!CanOpen || IsOpen) return;
            // 지난 프리뷰는 표시가 켜질 때(Show) 지웁니다(로비 리팩토링 2차 설계 N-25).
            _scope.Open();
        }

        internal void Close()
        {
            if (!CanWork) return;
            try
            {
                _scope.Close();
            }
            catch (Exception error)
            {
                // 닫는 도중 새 연결로 넘어갔으면 전파하지 않고 원인 로그만 남깁니다(로비 리팩토링 2차 설계 N-06).
                if (!_host.IsCurrent)
                {
                    UnityEngine.Debug.LogException(error);
                    return;
                }
                throw;
            }
        }

        internal void Tick()
        {
            _scope.Tick();
            if (!CanWork) return;
            if (!_displayActive)
            {
                _drag.Tick(false);
                return;
            }
            _status.SetStopped(_isStopped());
            _refresher.Refresh();
            if (CanWork && _displayActive) _drag.Tick(true);
        }

        /// <summary>바꾼 공유 출력 설정을 되돌립니다. 로비 연결 정리가 다른 정리 콜백보다 먼저 부릅니다(로비 리팩토링 2차 설계 M-14).</summary>
        internal void RestoreOutput() => _popupOutput.Exit();

        void IPopupViewController.Show()
        {
            if (!CanWork || _displayActive || !IsOpen) return;
            _displayActive = true;
            _writer.ClearPreview();
            if (!CanWork || !_displayActive) return;
            _refresher.Reset();
            _status.ResetDisplay(!_board.Current.IsInitialized);
            _status.SetStopped(_isStopped());
            _presenter.Show();
            if (!CanWork || !_displayActive) return;
            _popupOutput.Enter();
            if (!CanWork || !_displayActive) return;
            _inputReceiver = new InputReceiver(this);
            _inputSlot.Bind(_inputReceiver);
            if (!CanWork || !_displayActive) return;
            _closeReceiver = new CloseReceiver(this);
            _lifetime.BindClose(_closeReceiver);
            if (!CanWork || !_displayActive) return;
            _refresher.Refresh();
            if (CanWork && _displayActive) _drag.ConsumeEditResult(true);
            if (CanWork && _displayActive) _lifetime.SetCanClose(CanOpen);
        }

        void IPopupViewController.Hide(bool explicitClose) => HideDisplay();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            List<Exception> errors = null;
            // 화면 정리 콜백이 새 호스트를 연결하기 전에 출력 대여를 먼저 끝냅니다.
            CleanupErrors.Try(_popupOutput.Exit, ref errors);
            _status.Suspend();
            CleanupErrors.Try(BlockScreen, ref errors);
            CleanupErrors.Try(_scope.Dispose, ref errors);
            CleanupErrors.Try(HideDisplay, ref errors);
            CleanupErrors.Try(_drag.Stop, ref errors);
            CleanupErrors.Try(_writer.Clear, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[ArtifactPopup] 팝업 정리 중 실패했습니다.");
        }

        private void HideDisplay()
        {
            bool wasDisplayed = _displayActive;
            _displayActive = false;
            List<Exception> errors = null;
            CleanupErrors.Try(_popupOutput.Exit, ref errors);
            _status.Suspend();
            CleanupErrors.Try(BlockScreen, ref errors);
            CleanupErrors.Try(CancelDrag, ref errors);
            CleanupErrors.Try(_drag.ResetPointer, ref errors);
            CleanupErrors.Try(ReleaseInput, ref errors);
            _closeReceiver = null;
            CleanupErrors.Try(UnbindClose, ref errors);
            if (wasDisplayed) CleanupErrors.Try(_presenter.Hide, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[ArtifactPopup] 표시 정리 중 실패했습니다.");
        }

        private bool AllowsInput() => IsDisplayCurrent && !_isStopped() &&
            _layers.CurrentLayer.Equals(ArtifactInputLayers.Artifact);
        // 드래그 취소는 감싼 Buffer·표시에만 씁니다. 교체된 연결이면 그 쓰기를 감싸개가 막습니다(M-11).
        private void CancelDrag() => _drag.SelectCanceled();
        private void UnbindClose() => _lifetime.BindClose(null);
        private void BlockScreen() => _lifetime.SetCanClose(false);

        private void ReleaseInput()
        {
            IArtifactInputReceiver receiver = _inputReceiver;
            _inputReceiver = null;
            if (receiver != null) _inputSlot.Unbind(receiver);
        }

        /// <summary>
        /// 같은 컨트롤러의 현재 표시 연결만 입력을 받습니다. InputSystem 콜백(런타임 Tick 밖)에서 불리므로
        /// 예외는 오류 경계로 보냅니다(로비 리팩토링 2차 설계 M-01). 포인터 이동마다 불려 대리자를 만들지 않습니다.
        /// </summary>
        private sealed class InputReceiver : IArtifactInputReceiver
        {
            private readonly ArtifactPopupController _owner;
            internal InputReceiver(ArtifactPopupController owner)
            {
                _owner = owner;
            }
            private bool IsCurrent => ReferenceEquals(_owner._inputReceiver, this) &&
                _owner.IsDisplayCurrent;
            public void PointerMoved(float x, float y)
            {
                if (!IsCurrent) return;
                try { _owner._drag.PointerMoved(x, y); }
                catch (Exception error) { _owner._faults.Report(error); }
            }
            public void SelectPressed(float x, float y)
            {
                if (!IsCurrent) return;
                try { _owner._drag.SelectPressed(x, y); }
                catch (Exception error) { _owner._faults.Report(error); }
            }
            public void SelectReleased(float x, float y)
            {
                if (!IsCurrent) return;
                try { _owner._drag.SelectReleased(x, y); }
                catch (Exception error) { _owner._faults.Report(error); }
            }
            public void SelectCanceled()
            {
                if (!IsCurrent) return;
                try { _owner._drag.SelectCanceled(); }
                catch (Exception error) { _owner._faults.Report(error); }
            }
            public void RotateLeft()
            {
                if (!IsCurrent) return;
                try { _owner._drag.RotateLeft(); }
                catch (Exception error) { _owner._faults.Report(error); }
            }
            public void RotateRight()
            {
                if (!IsCurrent) return;
                try { _owner._drag.RotateRight(); }
                catch (Exception error) { _owner._faults.Report(error); }
            }
            public void Cancel()
            {
                if (!IsCurrent) return;
                try { _owner._drag.Cancel(); }
                catch (Exception error) { _owner._faults.Report(error); }
            }
        }

        /// <summary>
        /// 숨김과 폐기 때 이전 닫기 수신자를 무효화합니다. 닫기 버튼 콜백(EventSystem, 런타임 Tick 밖)의 예외는
        /// 오류 경계로 보냅니다(로비 리팩토링 2차 설계 M-01).
        /// </summary>
        private sealed class CloseReceiver : IArtifactPopupCloseReceiver
        {
            private readonly ArtifactPopupController _owner;
            internal CloseReceiver(ArtifactPopupController owner)
            {
                _owner = owner;
            }
            public void CloseClicked()
            {
                if (!ReferenceEquals(_owner._closeReceiver, this) || !_owner.IsDisplayCurrent) return;
                try { _owner.Close(); }
                catch (Exception error) { _owner._faults.Report(error); }
            }
        }

        /// <summary>정리 중 재진입한 새 연결의 공유 입력을 보존합니다.</summary>
        private sealed class OwnedWriter : IArtifactViewBufferWriteSink
        {
            private readonly IArtifactViewBufferWriteSink _writer;
            private readonly IPopupViewHost _host;
            internal OwnedWriter(IArtifactViewBufferWriteSink writer, IPopupViewHost host)
            {
                _writer = writer;
                _host = host;
            }
            public int Submit(ArtifactViewCommandData command) => _host.IsCurrent ? _writer.Submit(command) : 0;
            public int SetPreview(ArtifactViewPreviewData preview) => _host.IsCurrent ? _writer.SetPreview(preview) : 0;
            public void ClearPreview()
            {
                if (_host.IsCurrent) _writer.ClearPreview();
            }
            public void RemoveCommandsThrough(int number)
            {
                if (_host.IsCurrent) _writer.RemoveCommandsThrough(number);
            }
            public void Clear()
            {
                if (_host.IsCurrent) _writer.Clear();
            }
        }

        /// <summary>이전 연결의 드래그 정리가 새 표시를 변경하지 않도록 합니다.</summary>
        private sealed class OwnedPresenter : IArtifactPopupPresenter, IArtifactDragPresenter
        {
            private readonly IArtifactPopupPresenter _boardPresenter;
            private readonly IArtifactDragPresenter _presenter;
            private readonly IPopupViewHost _host;
            internal OwnedPresenter(IArtifactPopupPresenter boardPresenter, IArtifactDragPresenter presenter, IPopupViewHost host)
            {
                _boardPresenter = boardPresenter;
                _presenter = presenter;
                _host = host;
            }
            public void Show()
            {
                if (_host.IsCurrent) _boardPresenter.Show();
            }
            public void Hide()
            {
                if (_host.IsCurrent) _boardPresenter.Hide();
            }
            public void Render(ArtifactPopupBoard board)
            {
                if (_host.IsCurrent) _boardPresenter.Render(board);
            }
            public void ClearBoard()
            {
                if (_host.IsCurrent) _boardPresenter.ClearBoard();
            }
            public void BeginDrag(GameObject prefab, int instanceId, bool placed)
            {
                if (_host.IsCurrent) _presenter.BeginDrag(prefab, instanceId, placed);
            }
            public void ClearPreview()
            {
                if (_host.IsCurrent) _presenter.ClearPreview();
            }
            public void ShowPreview(ArtifactPlacementPreview preview, Rotation4 rotation, int width, int height)
            {
                if (_host.IsCurrent) _presenter.ShowPreview(preview, rotation, width, height);
            }
            public void EndDrag()
            {
                if (_host.IsCurrent) _presenter.EndDrag();
            }
        }

        /// <summary>공유 화면 콜백 뒤에도 현재 호스트만 표시 값을 갱신합니다.</summary>
        private sealed class OwnedDisplay : IArtifactPopupDisplay
        {
            private readonly IArtifactPopupDisplay _screen;
            private readonly IPopupViewHost _host;
            internal OwnedDisplay(IArtifactPopupDisplay screen, IPopupViewHost host)
            {
                _screen = screen;
                _host = host;
            }
            public void RenderPool(IReadOnlyList<ArtifactPoolItemContent> items)
            {
                if (_host.IsCurrent) _screen.RenderPool(items);
            }
            public void ShowStatus(string message)
            {
                if (_host.IsCurrent) _screen.ShowStatus(message);
            }
            public void ShowDetail(string name, string description)
            {
                if (_host.IsCurrent) _screen.ShowDetail(name, description);
            }
        }

        /// <summary>현재 호스트만 공유 화면의 닫기 연결·닫기 허용을 바꿉니다. 열림 상태와 알림은 그대로 전달합니다.</summary>
        private sealed class OwnedLifetime : IArtifactPopupLifetime
        {
            private readonly IArtifactPopupLifetime _screen;
            private readonly IPopupViewHost _host;
            internal OwnedLifetime(IArtifactPopupLifetime screen, IPopupViewHost host)
            {
                _screen = screen;
                _host = host;
            }
            public event Action<bool> OpenStateChanged
            {
                add => _screen.OpenStateChanged += value;
                remove => _screen.OpenStateChanged -= value;
            }
            public bool IsOpen => _screen.IsOpen;
            public void BindClose(IArtifactPopupCloseReceiver receiver)
            {
                if (_host.IsCurrent) _screen.BindClose(receiver);
            }
            public void SetCanClose(bool value)
            {
                if (_host.IsCurrent) _screen.SetCanClose(value);
            }
        }
    }
}
