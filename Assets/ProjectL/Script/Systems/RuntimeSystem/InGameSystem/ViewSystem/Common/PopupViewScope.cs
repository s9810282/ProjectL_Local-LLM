using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>팝업 화면과 입력 소유의 공통 수명입니다.</summary>
    internal sealed class PopupViewScope : IDisposable
    {
        private readonly IUISystemService _ui;
        private readonly IPopupViewScreen _screen;
        private readonly PopupInputSession _input;
        private readonly InputLayer? _layer;
        private readonly IPopupViewController _controller;
        private readonly IPopupViewHost _host;
        private readonly RuntimeFaultBoundary _faults;
        private object _token;
        private State _state;
        private bool _started;
        private bool _controllerShown;
        private bool _insideNotification;
        private int _uiCallDepth;

        internal PopupViewScope(IUISystemService ui, IPopupViewScreen screen,
            PopupInputSession input, InputLayer? layer, IPopupViewController controller,
            IPopupViewHost host, RuntimeFaultBoundary faults)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _layer = layer;
            // 호출자(로비 덱·아티팩트, 스테이지 맵 팝업)는 모두 오류 경계를 넘깁니다(로비 리팩토링 2차 설계 N-17).
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        internal bool CanOpen => _started && _state == State.Closed && CanContinue && !_screen.IsOpen;
        internal bool IsOpen => _screen.IsOpen;
        internal bool IsDisposed => _state == State.Disposed;

        private bool IsCurrent => !IsDisposed && _host.IsCurrent;
        private bool CanContinue => IsCurrent && !_faults.IsFaulted;
        private bool IsUiBusy => _insideNotification || _uiCallDepth > 0;

        internal void Start()
        {
            if (_started || !CanContinue) return;
            _started = true;
            _screen.Subscribe(OnOpenStateChanged);
            if (!CanContinue) return;
            if (_screen.IsOpen) AdoptOpenScreen();
        }

        internal void Open()
        {
            if (!CanOpen || IsUiBusy) return;
            _state = State.Opening;
            try
            {
                if (!BeginInput()) return;
                OpenScreen();
                if (!CanContinue) return;
                if (!_screen.IsOpen)
                {
                    throw new InvalidOperationException("[PopupViewScope] 팝업 화면이 UI 서비스에서 열리지 않았습니다.");
                }
                _state = State.Open;
            }
            catch (Exception error)
            {
                // Begin의 동기 취소 중 새 연결로 넘어간 이전 열기는 예외를 전파하지 않습니다. 정상 교체에서도 날 수 있어
                // 오류 경계로 보내지는 않고, 원인을 잃지 않도록 로그만 남깁니다(로비 리팩토링 2차 설계 N-06).
                if (!IsCurrent)
                {
                    UnityEngine.Debug.LogException(error);
                    return;
                }
                AbortOpen();
                throw;
            }
            finally
            {
                EndDisposedInput();
            }
        }

        internal void Close()
        {
            if (_state != State.Open || !CanContinue || IsUiBusy) return;
            _state = State.Closing;
            try
            {
                CloseScreen();
                if (!CanContinue) return;
                if (_screen.IsOpen)
                {
                    throw new InvalidOperationException("[PopupViewScope] 팝업 화면이 UI 서비스에서 닫히지 않았습니다.");
                }
                FinishClose();
            }
            catch
            {
                AbortOpen();
                throw;
            }
            finally
            {
                EndDisposedInput();
            }
        }

        internal void Tick()
        {
            if (IsUiBusy) return;
            if (IsDisposed)
            {
                EndDisposedInput();
                return;
            }
            if (!_host.IsCurrent) return;
            if (_state == State.HiddenExternally)
            {
                FinishClose();
            }
            else if (_state == State.OpenedExternally)
            {
                if (_screen.IsOpen && CanContinue)
                {
                    AdoptOpenScreen();
                }
                else
                {
                    _state = State.Closed;
                }
            }
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            // 외부 정리가 재진입해도 새 작업과 중복 정리를 시작하지 않습니다.
            _state = State.Disposed;
            List<Exception> errors = null;
            if (_started)
            {
                CleanupErrors.Try(Unsubscribe, ref errors);
            }
            CleanupErrors.Try(CloseDisposedScreen, ref errors);
            CleanupErrors.Try(HideDisposedController, ref errors);
            CleanupErrors.Try(EndDisposedInput, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[PopupViewScope] 팝업 정리 중 실패했습니다.");
        }

        private bool BeginInput()
        {
            _host.SetPopupBlocked(true);
            if (!CanContinue) return false;
            object token = _input.Begin(_layer);
            // Begin의 동기 취소 콜백이 Dispose를 불러도 반환된 토큰을 놓치지 않습니다.
            _token = token;
            if (!CanContinue)
            {
                if (_host.IsCurrent) _input.End(TakeToken(), false);
                return false;
            }
            if (token == null)
            {
                // 입력 수명을 쥐지 못했으면(예: 출발 처리 중이라 복귀할 수 없음) 입력 없는 팝업을 열지 않고 조용히 멈춥니다(N-05).
                AbortOpen();
                return false;
            }
            return true;
        }

        private void AdoptOpenScreen()
        {
            _state = State.Opening;
            try
            {
                // 외부 숨김 뒤 같은 프레임의 재열림은 이전 입력 소유를 먼저 반환합니다.
                if (_token != null)
                {
                    _input.End(TakeToken(), false);
                    if (!CanContinue) return;
                }
                if (!BeginInput()) return;
                if (!CanFinishAdoption()) return;
                ShowController();
                if (!CanFinishAdoption()) return;
                _state = State.Open;
            }
            catch (Exception error)
            {
                // 새 연결로 넘어간 이전 채택은 전파하지 않고 원인 로그만 남깁니다(N-06).
                if (!IsCurrent)
                {
                    UnityEngine.Debug.LogException(error);
                    return;
                }
                AbortOpen();
                throw;
            }
            finally
            {
                EndDisposedInput();
            }
        }

        private bool CanFinishAdoption()
        {
            // Begin과 Show의 동기 콜백이 이미 닫힘을 알렸다면 그 상태를 덮어쓰지 않습니다.
            if (!CanContinue || _state != State.Opening) return false;
            if (_screen.IsOpen) return true;
            _state = State.HiddenExternally;
            HideController(false);
            return false;
        }

        private void FinishClose()
        {
            _input.End(TakeToken(), true);
            if (!IsCurrent) return;
            _host.SetPopupBlocked(false);
            if (!IsCurrent) return;
            // 복귀·차단 해제의 동기 콜백에서 재열렸다면 다음 Tick의 채택 대기를 보존합니다.
            _state = _screen.IsOpen ? State.OpenedExternally : State.Closed;
        }

        private void OnOpenStateChanged(bool open)
        {
            if (!CanContinue) return;
            bool previous = _insideNotification;
            _insideNotification = true;
            try
            {
                if (open)
                {
                    if (_state == State.Opening)
                    {
                        ShowController();
                    }
                    else if (_state == State.Closed || _state == State.HiddenExternally ||
                        _state == State.Closing)
                    {
                        _state = State.OpenedExternally;
                    }
                }
                else
                {
                    bool explicitClose = _state == State.Closing;
                    HideController(explicitClose);
                    if (!CanContinue) return;
                    if (!explicitClose)
                    {
                        _state = State.HiddenExternally;
                    }
                }
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
            finally
            {
                _insideNotification = previous;
            }
        }

        private void ShowController()
        {
            if (_controllerShown || !CanContinue) return;
            // Show의 부분 실패나 동기 폐기도 Hide로 정리할 수 있게 먼저 표시 수명을 엽니다.
            _controllerShown = true;
            _controller.Show();
        }

        private void HideController(bool explicitClose)
        {
            if (!_controllerShown) return;
            _controllerShown = false;
            _controller.Hide(explicitClose);
        }

        private void AbortOpen()
        {
            if (!IsCurrent || IsUiBusy) return;
            List<Exception> errors = null;
            _state = State.Closing;
            CleanupErrors.Try(CloseCurrentScreen, ref errors);
            CleanupErrors.Try(HideDisposedController, ref errors);
            CleanupErrors.Try(EndAbortedInput, ref errors);
            CleanupErrors.Try(UnblockCurrentHost, ref errors);
            if (IsCurrent) _state = State.Closed;
            CleanupErrors.ThrowIfAny(errors, "[PopupViewScope] 팝업 열기 또는 닫기 실패 정리 중 오류가 발생했습니다.");
        }

        private void Unsubscribe() => _screen.Unsubscribe(OnOpenStateChanged);
        private void HideDisposedController() => HideController(false);

        private void CloseDisposedScreen()
        {
            if (_host.IsCurrent && !IsUiBusy && _screen.IsOpen)
            {
                CloseScreen();
            }
        }

        private void CloseCurrentScreen()
        {
            if (IsCurrent && !IsUiBusy && _screen.IsOpen) CloseScreen();
        }

        private void EndAbortedInput()
        {
            if (IsCurrent) _input.End(TakeToken(), false);
        }

        private void UnblockCurrentHost()
        {
            if (IsCurrent) _host.SetPopupBlocked(false);
        }

        private void EndDisposedInput()
        {
            // 다른 구독자가 Dispose해도 소유한 UI 서비스 호출 전체가 반환된 뒤에만 정리합니다.
            if (IsDisposed && _host.IsCurrent && !IsUiBusy)
            {
                _input.End(TakeToken(), false);
            }
        }

        private void OpenScreen()
        {
            // 자기 observer 뒤에 실행되는 구독자의 콜백도 UI 호출 수명에 포함합니다.
            _uiCallDepth++;
            try
            {
                _ui.OpenUI(_screen.Id);
            }
            finally
            {
                _uiCallDepth--;
            }
        }

        private void CloseScreen()
        {
            _uiCallDepth++;
            try
            {
                _ui.CloseUI(_screen.Id);
            }
            finally
            {
                _uiCallDepth--;
            }
        }

        private object TakeToken()
        {
            object token = _token;
            _token = null;
            return token;
        }

        private enum State
        {
            Closed,
            Opening,
            Open,
            Closing,
            HiddenExternally,
            OpenedExternally,
            Disposed
        }
    }
}
