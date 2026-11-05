using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common.ScrollPanel;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 팝업(상점·회복·덱 편집) 컨트롤러가 함께 갖는 수명 부품입니다. 열기·닫기 공통 Scope, 연결 확인,
    /// 시작 실패 정리, 닫고 선택 화면으로 돌아가기, 오류 보고를 한 곳에 둡니다. 컨트롤러는 자기 내용만 다룹니다
    /// (상속 대신 조합, 검사 후속 정리 설계 §12.2 B5).
    /// </summary>
    internal sealed class StagePopupViewLifetime
    {
        private readonly string _name;
        private readonly PopupViewScope _scope;
        private readonly IPopupViewHost _host;
        private readonly RuntimeFaultBoundary _faults;
        private readonly StageMapScreenPresentation _presentation;
        private readonly ScrollPanelPresentation _scroll;
        private CloseRequest _pendingClose;

        private sealed class CloseRequest
        {
            private readonly StagePopupViewLifetime _owner;
            internal readonly bool ReturnToSelection;
            internal readonly Action AfterClose;

            internal CloseRequest(StagePopupViewLifetime owner, bool returnToSelection, Action afterClose)
            {
                _owner = owner;
                ReturnToSelection = returnToSelection;
                AfterClose = afterClose;
            }

            internal void Complete() => _owner.FinishClose(this);
        }

        /// <param name="name">예외 문구 머리 이름(예: "ShopPopup")입니다.</param>
        /// <param name="presentation">선택 화면 복귀를 맡는 표시 관찰자입니다. 없으면 닫을 때 복귀하지 않습니다.</param>
        internal StagePopupViewLifetime(string name, IUISystemService ui, IPopupViewScreen screen, PopupInputSession input,
            IPopupViewController controller, IPopupViewHost host, RuntimeFaultBoundary faults,
            StageMapScreenPresentation presentation = null)
        {
            _name = string.IsNullOrEmpty(name) ? throw new ArgumentException("이름이 필요합니다.", nameof(name)) : name;
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _presentation = presentation;
            _scroll = (screen as IScrollPopupDisplay)?.ScrollPresentation;
            _scope = new PopupViewScope(ui, screen, input, StageSelectionInputLayers.Popup, controller, host, faults);
        }

        internal bool IsStarted { get; private set; }
        internal bool IsDisposed { get; private set; }
        /// <summary>부모 연결이 현재입니다. 폐기 뒤에도 공유 화면을 건드려도 되는지 볼 때 씁니다.</summary>
        internal bool IsCurrent => _host.IsCurrent;
        /// <summary>폐기 전이고 부모 연결이 현재입니다. 오류 정지 여부는 보지 않습니다.</summary>
        internal bool IsActive => !IsDisposed && _host.IsCurrent;
        internal bool CanWork => IsActive && !_faults.IsFaulted && _pendingClose == null;
        internal bool IsOpen => !IsDisposed && _scope.IsOpen;
        internal bool CanOpen => CanWork && (_presentation == null || _presentation.CanOpen) && _scope.CanOpen;

        /// <summary>
        /// 화면을 연결(<paramref name="connectScreen"/>)한 뒤 표시 관찰과 공통 Scope를 시작합니다. 이미 열린 UI는 Scope가 즉시 인수합니다.
        /// 실패하면 컨트롤러 폐기(<paramref name="disposeOwner"/>)까지 시도하고 모아서 던집니다.
        /// </summary>
        internal void Start(Action connectScreen, Action disposeOwner)
        {
            if (connectScreen == null) throw new ArgumentNullException(nameof(connectScreen));
            if (disposeOwner == null) throw new ArgumentNullException(nameof(disposeOwner));
            if (IsStarted || !CanWork) return;
            IsStarted = true;
            try
            {
                connectScreen();
                if (!CanWork) return;
                _presentation?.Start();
                if (CanWork) _scope.Start();
            }
            catch (Exception error)
            {
                var failures = new List<Exception> { error };
                CleanupErrors.Try(disposeOwner, ref failures);
                throw new AggregateException($"[{_name}] 화면 연결 중 실패했습니다.", failures);
            }
        }

        internal void Open()
        {
            if (CanWork) _scope.Open();
        }

        internal void Tick()
        {
            _scope.Tick();
            // 외부 동기 Hide는 정상 완료 후속을 실행하지 않습니다.
            if (_pendingClose != null && (!_scope.IsOpen || !IsActive || _faults.IsFaulted))
            {
                CancelPendingClose();
            }
            else if (_pendingClose != null && (_scroll == null || !_scroll.isActiveAndEnabled ||
                (!_scroll.IsVisible && !_scroll.IsAnimating)))
            {
                // 부모 비활성화로 완료 알림이 취소돼도 논리 화면·입력 세션을 남기지 않습니다.
                CancelPendingClose();
                CloseImmediately(false, null);
            }
        }

        /// <summary>팝업을 닫습니다. 표시 관찰자가 있으면 <paramref name="returnToSelection"/>일 때 선택 화면 복귀를 요청합니다.</summary>
        internal void Close(bool returnToSelection) => CloseThen(returnToSelection, null);

        /// <summary>정상 닫기가 끝난 뒤 현재 연결에서 후속 동작을 한 번 실행합니다.</summary>
        internal void CloseThen(bool returnToSelection, Action afterClose)
        {
            if (!CanWork) return;
            // 두루마리 계약이 없는 기존 화면은 원래 동기 닫기를 유지합니다.
            if (_scroll == null || !_scope.IsOpen)
            {
                CloseImmediately(returnToSelection, afterClose);
                return;
            }
            var request = new CloseRequest(this, returnToSelection, afterClose);
            _pendingClose = request;
            _scroll.Closed += request.Complete;
            try
            {
                if (!_scroll.PlayClose()) FinishClose(request);
            }
            catch
            {
                CancelPendingClose();
                throw;
            }
        }

        private void CloseImmediately(bool returnToSelection, Action afterClose)
        {
            if (_presentation == null) _scope.Close();
            else _presentation.Close(_scope.Close, returnToSelection);
            if (CanWork && !_scope.IsOpen) afterClose?.Invoke();
        }

        private void FinishClose(CloseRequest request)
        {
            if (!ReferenceEquals(_pendingClose, request)) return;
            CancelPendingClose();
            try
            {
                if (CanWork) CloseImmediately(request.ReturnToSelection, request.AfterClose);
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }

        private void CancelPendingClose()
        {
            CloseRequest pending = _pendingClose;
            _pendingClose = null;
            if (pending != null && _scroll != null) _scroll.Closed -= pending.Complete;
        }

        internal void Report(Exception error) => _faults.Report(error);

        /// <summary>
        /// 표시 관찰 → 컨트롤러 정리(<paramref name="ownerCleanup"/>) → 화면 연결 끊기(<paramref name="unbindScreen"/>) → 공통 Scope 순으로 정리합니다.
        /// 공유 화면은 다음 연결이 이미 걸었을 수 있으므로 끊기는 시작한 현재 연결일 때만 합니다.
        /// </summary>
        internal void Dispose(Action ownerCleanup, Action unbindScreen)
        {
            if (IsDisposed) return;
            IsDisposed = true;
            CancelPendingClose();
            List<Exception> errors = null;
            if (_presentation != null) CleanupErrors.Try(_presentation.Dispose, ref errors);
            if (ownerCleanup != null) CleanupErrors.Try(ownerCleanup, ref errors);
            if (IsStarted && unbindScreen != null)
                CleanupErrors.Try(() => { if (_host.IsCurrent) unbindScreen(); }, ref errors);
            CleanupErrors.Try(_scope.Dispose, ref errors);
            CleanupErrors.ThrowIfAny(errors, $"[{_name}] 화면 정리 중 실패했습니다.");
        }
    }
}
