using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using System;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Recovery
{
    /// <summary>
    /// 현재 회복 표시의 문맥과 요청 결과를 연결합니다. 화면의 입력 의도를 받는 유일한 제출자이고,
    /// "이 표시가 낸 요청"은 영수증(번호·명령·표시 토큰) 하나만 갖습니다.
    /// </summary>
    internal sealed class RecoveryPopupController : IPopupViewController, IStageOfferInput, IDisposable
    {
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IRecoveryStateReadSink _state;
        private readonly IRecoveryRequestStatus _status;
        private readonly IRecoveryRequestSubmitter _requests;
        private readonly IStageOfferDisplay _screen;
        private readonly IStageOfferTextTableQuery _texts;
        private readonly Action _mapChanged;
        private readonly Action<string> _report;
        private readonly StagePopupViewLifetime _lifetime;
        private readonly RecoveryPopupModelBuilder _models;
        private RecoveryEntryContext _context;
        private RecoveryEntryContext? _hiddenContext;
        private RecoveryCommandData _pending;
        private int _pendingNumber;
        private object _displayToken;
        private object _pendingDisplay;

        internal RecoveryPopupController(ICurrentProgressBufferReadSink current, IRecoveryStateReadSink state,
            IRecoveryRequestStatus status, IRecoveryRequestSubmitter requests, IUISystemService ui,
            IStageOfferDisplay screen, PopupInputSession input, IPopupViewHost host, Action mapChanged,
            Action<string> report, RuntimeFaultBoundary faults, RecoveryPopupModelBuilder models,
            IStageOfferTextTableQuery texts, StageMapScreenPresentation presentation = null)
        {
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _status = status ?? throw new ArgumentNullException(nameof(status));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _mapChanged = mapChanged ?? throw new ArgumentNullException(nameof(mapChanged));
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _models = models ?? throw new ArgumentNullException(nameof(models));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _lifetime = new StagePopupViewLifetime("RecoveryPopup", ui, new StageOfferPopupViewScreen(screen), input,
                this, host, faults, presentation);
        }

        internal bool IsOpen => _lifetime.IsOpen;
        internal bool IsWaiting => _pendingNumber > 0;
        private bool CanWork => _lifetime.CanWork;
        private bool IsDisplay(object token) => token != null && ReferenceEquals(_displayToken, token) && CanWork && IsOpen;

        internal void Start() => _lifetime.Start(() =>
        {
            _screen.SetCanSubmit(false);
            if (CanWork) _screen.BindInput(this);
        }, Dispose);

        internal bool TryRestore(int chapter)
        {
            if (!_lifetime.CanOpen || _status.HasPending) return false;
            try
            {
                if (_current.CurrentMapKey != chapter || !_state.TryGetCurrent(out var state) ||
                    state.IsCompleted || !MatchesSelection(state.Context) ||
                    (_hiddenContext.HasValue && _hiddenContext.Value == state.Context) || !CanWork) return false;
                _context = state.Context;
                _lifetime.Open();
                return CanWork && IsOpen;
            }
            catch (Exception error)
            {
                _lifetime.Report(error);
                return false;
            }
        }

        void IPopupViewController.Show()
        {
            if (!CanWork || !IsOpen || _displayToken != null) return;
            var token = new object();
            _displayToken = token;
            _hiddenContext = null;
            ClearPendingDisplay();
            if (!_state.TryGetCurrent(out var state) || state.IsCompleted || !MatchesSelection(state.Context)) return;
            _context = state.Context;
            Render(state, token);
        }

        void IPopupViewController.Hide(bool explicitClose)
        {
            if (_displayToken != null) _hiddenContext = _context;
            _displayToken = null;
            ClearPendingDisplay();
            BlockSubmission();
        }

        internal void Tick()
        {
            try
            {
                _lifetime.Tick();
                object token = _displayToken;
                if (!IsDisplay(token)) return;
                // 완료도 현재 진입 identity는 유지합니다. 재열기 허용 조건과 섞지 않습니다.
                if (!TryReadDisplay(out var state))
                {
                    BlockSubmission();
                    if (!IsDisplay(token)) return;
                    _report("회복 상태가 바뀌어 팝업을 닫습니다.");
                    if (IsDisplay(token)) Close(false);
                    return;
                }
                if (!IsDisplay(token) || _pendingNumber == 0) return;
                if (!_status.TryGetResult(_pendingNumber, _pending, out var processed) ||
                    !IsDisplay(token) || !ReferenceEquals(_pendingDisplay, token) || !TryReadDisplay(out state)) return;
                HandleResult(processed, state, token);
            }
            catch (Exception error)
            {
                _lifetime.Report(error);
            }
        }

        void IStageOfferInput.SelectCard(int index)
        {
            if (index >= 0 && index < _models.OptionIds.Count && IsDisplay(_displayToken))
                Submit(RecoveryCommandData.Select(_context, _models.OptionIds[index]));
        }

        void IStageOfferInput.Exit()
        {
            if (IsDisplay(_displayToken)) Submit(RecoveryCommandData.Complete(_context));
        }

        /// <summary>
        /// 지금 표시가 그린 문맥의 의도만 받아 제출합니다. 제출 전에 먼저 잠가 같은 프레임의 두 번째 입력을 막고,
        /// 제출 중 표시가 바뀌었으면 영수증을 새 표시에 묶지 않습니다. 제출 예외는 오류 경계에 보고합니다.
        /// </summary>
        private void Submit(RecoveryCommandData command)
        {
            try
            {
                object token = _displayToken;
                if (!IsDisplay(token) || !TryReadDisplay(out var state) || !CanSubmitNow(state)) return;
                _screen.SetCanSubmit(false);
                if (!IsDisplay(token)) return;
                int number = _requests.Submit(command);
                if (!IsDisplay(token)) return;
                _pending = command;
                _pendingNumber = number;
                _pendingDisplay = token;
            }
            catch (Exception error)
            {
                _lifetime.Report(error);
            }
        }

        private void HandleResult(RecoveryProcessedResult processed, RecoveryStateSnapshot state, object token)
        {
            if (!IsDisplay(token)) return;
            RecoveryOperationResult result = processed.Result;
            bool completed = result.Kind == RecoveryResultKind.Accepted && _pending.Kind == RecoveryCommandKind.Complete;
            bool mismatch = result.Kind == RecoveryResultKind.StateMismatch;
            ClearPendingDisplay();
            if (completed || mismatch)
            {
                _screen.SetCanSubmit(false);
                if (!IsDisplay(token)) return;
                if (mismatch) _report($"회복 상태 불일치: {result.FailureReason}");
                if (!IsDisplay(token)) return;
                _lifetime.CloseThen(completed, completed ? _mapChanged : null);
                return;
            }
            if (result.Kind == RecoveryResultKind.TradeRejected)
            {
                _screen.ShowError(_texts.GetForStage(_context.StageKey.Value)
                    .ErrorOf(RecoveryPopupModelBuilder.ErrorOf(result.FailureReason)));
                if (!IsDisplay(token)) return;
            }
            // 재사용 모델을 그리는 동안에도 제출 때 내린 잠금을 유지하고, 다 그린 뒤 허용을 다시 계산합니다.
            Render(state, token);
        }

        private void Render(RecoveryStateSnapshot state, object token)
        {
            StageOfferPopupModel model = _models.Build(state);
            if (!IsDisplay(token)) return;
            _screen.Render(model);
            if (IsDisplay(token)) _screen.SetCanSubmit(CanSubmitNow(state));
        }

        // 화면에 내리는 제출 허용의 한 원본입니다. 영수증이나 런타임 요청이 있는 동안에는 늘 잠급니다.
        private bool CanSubmitNow(RecoveryStateSnapshot state) => !state.IsCompleted && !IsWaiting && !_status.HasPending;

        private bool MatchesSelection(RecoveryEntryContext context) => _current.HasSelection &&
            _current.CurrentMapKey == context.ChapterKey && _current.CurrentStage.NodeId == context.NodeId.Value &&
            _current.CurrentStage.StageKey == context.StageKey.Value;

        private bool TryReadDisplay(out RecoveryStateSnapshot state) =>
            _state.TryGetCurrent(out state) && state.Context == _context && MatchesSelection(_context);

        private void ClearPendingDisplay()
        {
            _pending = null;
            _pendingNumber = 0;
            _pendingDisplay = null;
        }

        private void BlockSubmission()
        {
            if (_lifetime.IsCurrent) _screen.SetCanSubmit(false);
        }

        private void Close(bool returnToSelection) => _lifetime.Close(returnToSelection);

        public void Dispose()
        {
            if (_lifetime.IsDisposed) return;
            _displayToken = null;
            ClearPendingDisplay();
            // 공유 화면의 수신자는 수명 부품이 현재 연결일 때만 끊습니다.
            _lifetime.Dispose(BlockSubmission, () => _screen.BindInput(null));
        }
    }
}
