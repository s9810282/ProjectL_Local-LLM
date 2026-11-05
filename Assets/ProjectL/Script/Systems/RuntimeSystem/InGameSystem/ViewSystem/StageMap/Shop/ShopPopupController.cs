using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ShopFailureReason = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.ShopFailureReason;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>
    /// 연결 한 번의 상점 표시·제출 허용·결과와 구매 문구를 관찰합니다. 화면의 입력 의도를 받는 유일한 제출자이고,
    /// "제출 중" 사실은 영수증(<see cref="_pending"/>) 하나만 갖습니다.
    /// 구매와 완료가 끝나도 플레이어가 떠난다를 누를 때까지 화면을 남깁니다(공통 레이아웃 설계 SO7, <see cref="_awaitingExit"/>).
    /// </summary>
    internal sealed class ShopPopupController : IPopupViewController, IStageOfferInput, IDisposable
    {
        private readonly ShopPopupModelBuilder _models;
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IStageProgressBufferReadSink _stages;
        private readonly IShopViewBufferReadSink _input;
        private readonly IShopViewBufferWriteSink _writer;
        private readonly IStageCommandResultReadSink _results;
        private readonly Action _mapChanged;
        private readonly Action<string> _report;
        private readonly IStageOfferDisplay _screen;
        private readonly IStageOfferTextTableQuery _texts;
        private readonly StagePopupViewLifetime _lifetime;
        // 훈련소 단계가 바뀐 직후의 입력을 버리는 시간입니다. 더블클릭의 두 번째 클릭이 바뀐 화면의 같은 자리를 누르지 않게 합니다.
        private const float StepInputLockSeconds = 0.2f;
        private float _stepInputOpensAt;
        private ShopViewCommand _pending;
        private CurrentShopData _displayContext;
        private int _stageKey;
        private bool _displayActive;
        private bool _skipNextRestore;
        // 구매와 완료가 모두 수락돼 떠난다만 남았습니다. 그린 화면을 유지하고 다시 그리지 않습니다.
        private bool _awaitingExit;

        internal ShopPopupController(ShopPopupModelBuilder models, ICurrentProgressBufferReadSink current,
            IStageProgressBufferReadSink stages, IShopViewBufferReadSink input, IShopViewBufferWriteSink writer,
            IStageCommandResultReadSink results, IUISystemService ui, IStageOfferDisplay screen,
            PopupInputSession popupInput, IPopupViewHost host,
            Action mapChanged, Action<string> report, RuntimeFaultBoundary faults, IStageOfferTextTableQuery texts,
            StageMapScreenPresentation presentation = null)
        {
            _models = models ?? throw new ArgumentNullException(nameof(models));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _screen = screen ?? throw new ArgumentNullException(nameof(screen));
            _mapChanged = mapChanged ?? throw new ArgumentNullException(nameof(mapChanged));
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _lifetime = new StagePopupViewLifetime("ShopPopup", ui, new StageOfferPopupViewScreen(screen), popupInput,
                this, host, faults, presentation);
        }

        internal bool IsOpen => _lifetime.IsOpen;
        internal bool IsWaiting => _pending.Number > 0;
        internal int ChapterKey { get; private set; }
        internal StageNodeId NodeId { get; private set; }
        private bool CanWork => _lifetime.CanWork;
        private bool CanPermit() => CanWork && MatchesDisplayContext() && !_stages.IsCleared(ChapterKey, NodeId.Value);
        // 화면에 내리는 제출 허용의 한 원본입니다. 영수증이 있는 동안에는 늘 잠급니다.
        private bool CanSubmitNow() => CanWork && _displayActive && !IsWaiting && (_awaitingExit || CanPermit());
        private bool IsStepInputLocked => UnityEngine.Time.unscaledTime < _stepInputOpensAt;

        /// <summary>기본 입력 연결 뒤 화면을 준비하고 이미 열린 UI는 즉시 인수합니다.</summary>
        internal void Start() => _lifetime.Start(() =>
        {
            _screen.SetCanSubmit(false);
            if (CanWork) _screen.BindInput(this);
        }, Dispose);

        public void Dispose()
        {
            if (_lifetime.IsDisposed) return;
            _pending = default;
            _displayContext = null;
            ChapterKey = 0;
            NodeId = StageNodeId.Empty;
            _stageKey = 0;
            _skipNextRestore = false;
            _awaitingExit = false;
            // 공유 화면의 수신자는 수명 부품이 현재 연결일 때만 끊습니다. 입력 Buffer도 현재 연결의 것만 비웁니다.
            _lifetime.Dispose(() => CleanupErrors.RunAll("[ShopPopup] 제출 정리 중 실패했습니다.",
                BlockSubmission, () => { if (_lifetime.IsCurrent) _writer.Clear(); }),
                () => _screen.BindInput(null));
        }

        internal bool TryRestore(int mapKey)
        {
            if (!_lifetime.CanOpen || IsWaiting || _input.Commands.Count != 0) return false;
            if (_skipNextRestore) { _skipNextRestore = false; return false; }
            if (!TryCaptureContext(mapKey) || !CanWork) return false;
            try
            {
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
            if (!CanWork || !IsOpen || _displayActive) return;
            _displayActive = true;
            // 완료된 노드라 아래 허용 조건을 넘지 못하므로, 다시 열려도 떠난다를 누를 수 있게 먼저 답합니다.
            if (_awaitingExit)
            {
                _screen.SetCanSubmit(CanSubmitNow());
                return;
            }
            if (!MatchesDisplayContext() && !IsWaiting && _input.Commands.Count == 0)
                TryCaptureContext(_current.CurrentMapKey, true);
            if (!CanWork || !_displayActive) return;
            if (!CanPermit()) { BlockSubmission(); return; }
            StageOfferPopupModel model = _models.Build();
            if (!CanWork || !_displayActive) return;
            _screen.Render(model);
            if (CanWork && _displayActive) _screen.SetCanSubmit(CanSubmitNow());
        }

        void IPopupViewController.Hide(bool explicitClose)
        {
            _displayActive = false;
            // 숨김 중 도착한 소유 요청 결과도 소비하므로 pending은 폐기 때만 지웁니다.
            BlockSubmission();
        }

        private void BlockSubmission()
        {
            if (_lifetime.IsCurrent) _screen.SetCanSubmit(false);
        }

        void IStageOfferInput.SelectCard(int index)
        {
            if (!CanSubmitNow() || IsStepInputLocked) return;
            if (_models.TrySelectUnit(index)) Redraw();
            else if (_models.TryGetCommand(index, out ShopViewCommandData command)) Submit(command);
        }

        void IStageOfferInput.Exit()
        {
            if (!CanSubmitNow() || IsStepInputLocked) return;
            if (_models.TryBack())
            {
                Redraw();
                return;
            }
            if (!_awaitingExit)
            {
                Submit(ShopViewCommandData.Complete(_models.Context));
                return;
            }
            // 완료는 구매 때 이미 수락됐으므로 명령 없이 닫습니다. 예외는 오류 경계에 보고합니다(EventSystem은 수신부 예외를 기록만 합니다).
            try
            {
                _screen.SetCanSubmit(false);
                if (CanWork) CloseAndRefresh(true);
            }
            catch (Exception error)
            {
                _lifetime.Report(error);
            }
        }

        // 훈련소의 단계 이동입니다. Core 요청이 없고, 예외는 오류 경계에 보고합니다(EventSystem은 수신부 예외를 기록만 합니다).
        private void Redraw()
        {
            try
            {
                _stepInputOpensAt = UnityEngine.Time.unscaledTime + StepInputLockSeconds;
                _screen.Render(_models.Build());
                if (CanWork) _screen.SetCanSubmit(CanSubmitNow());
            }
            catch (Exception error)
            {
                _lifetime.Report(error);
            }
        }

        /// <summary>
        /// 그린 방문의 의도만 받아 제출합니다. 제출 전에 먼저 잠가 같은 프레임의 두 번째 입력을 막고,
        /// 제출 예외는 오류 경계에 보고합니다(EventSystem은 수신부 예외를 기록만 합니다).
        /// </summary>
        private void Submit(ShopViewCommandData command)
        {
            try
            {
                if (!CanSubmitNow() || command.Context.EntryId != _displayContext.EntryId) return;
                _screen.SetCanSubmit(false);
                if (!CanWork) return;
                int number = _writer.Submit(command);
                _pending = new ShopViewCommand(number, command);
            }
            catch (Exception error)
            {
                _lifetime.Report(error);
            }
        }

        private bool TryCaptureContext(int mapKey, bool includeCleared = false)
        {
            if (mapKey <= 0 || _current.CurrentMapKey != mapKey || !_current.HasSelection || _current.Shop == null)
                return false;
            var selected = _current.CurrentStage;
            if (selected.NodeId <= 0 || selected.StageKey <= 0) return false;
            // 상점인지는 Buffer의 상점 상태(위 _current.Shop)가 말합니다. 노드 종류 이름(StageType)은 읽지 않습니다(설계 §12.6 Q-R9).
            bool found = false;
            foreach (StageProgressData node in _stages.GetStages(mapKey))
            {
                if (node.NodeId != selected.NodeId) continue;
                found = (includeCleared || !node.IsClear) && node.StageKey == selected.StageKey;
                break;
            }
            if (!found) return false;
            ChapterKey = mapKey;
            NodeId = new StageNodeId(selected.NodeId);
            _stageKey = selected.StageKey;
            _displayContext = _current.Shop;
            return true;
        }

        private bool MatchesDisplayContext()
        {
            if (_displayContext == null || !_current.HasSelection || _current.CurrentMapKey != ChapterKey ||
                _current.CurrentStage.NodeId != NodeId.Value || _current.CurrentStage.StageKey != _stageKey) return false;
            CurrentShopData currentShop = _current.Shop;
            if (currentShop == null || currentShop.EntryId != _displayContext.EntryId) return false;
            return true;
        }

        internal void Tick()
        {
            try
            {
                _lifetime.Tick();
                if (!CanWork) return;
                if (!IsWaiting)
                {
                    // 영수증이 없을 때만 Buffer에 남은 같은 방문의 명령(이전 연결의 제출)을 넘겨받습니다.
                    if (!_input.TryGetNextCommand(0, out var next))
                    {
                        ReconcileWithoutRequest();
                        return;
                    }
                    if (next.Data.ChapterKey != ChapterKey || next.Data.NodeId != NodeId ||
                        _displayContext == null || next.Data.Context.EntryId != _displayContext.EntryId ||
                        next.Data.Context.StageKey.Value != _stageKey) return;
                    _pending = next;
                }
                bool sameContext = MatchesDisplayContext();
                if (!sameContext) BlockSubmission();
                if (!CanWork || !_results.TryGetShopResult(out var result) || !MatchesResult(result)) return;
                if (!sameContext && !IsMismatch(result)) return;
                HandleResult(result);
            }
            catch (Exception error)
            {
                _lifetime.Report(error);
            }
        }

        private bool MatchesResult(ShopCommandResult result)
        {
            if (result.RequestNumber != _pending.Number || result.Context != _pending.Data.Context) return false;
            ShopViewCommandData pending = _pending.Data;
            switch (pending.Kind)
            {
                case ShopViewCommandKind.Purchase:
                    return result.Kind == ShopCommandKind.Purchase && result.OfferId == pending.OfferId && result.PurchaseResult != null;
                case ShopViewCommandKind.PurchaseTraining:
                    return result.Kind == ShopCommandKind.PurchaseTraining && result.OwnedId == pending.OwnedId &&
                        result.TraitKey == pending.TraitKey && result.ExpectedRank == pending.ExpectedRank && result.PurchaseResult != null;
                default:
                    return result.Kind == ShopCommandKind.Complete && result.CompletionResult != null;
            }
        }

        private static bool IsMismatch(ShopCommandResult result) => result.Kind != ShopCommandKind.Complete
            ? ShopResultPolicy.ClassifyPurchase(result.PurchaseResult.FailureReason) == ShopResultCategory.Mismatch
            : !result.CompletionResult.Accepted;

        private void HandleResult(ShopCommandResult result)
        {
            bool completed = result.CompletionResult != null && result.CompletionResult.Accepted;
            bool close = completed && result.Kind == ShopCommandKind.Complete;
            if (result.Kind != ShopCommandKind.Complete)
            {
                var failure = result.PurchaseResult.FailureReason;
                switch (ShopResultPolicy.ClassifyPurchase(failure))
                {
                    case ShopResultCategory.Success:
                        // 완료가 실패했으면 떠난다가 완료를 다시 냅니다.
                        _awaitingExit = completed;
                        if (!completed) _report("구매는 처리됐지만 상점 완료는 처리되지 않았습니다.");
                        break;
                    case ShopResultCategory.NormalRejection:
                        ShowResultError(ShopResultPolicy.ErrorOf(failure));
                        break;
                    default:
                        if (ShopResultPolicy.IsUnexpected(failure))
                            throw new InvalidOperationException($"상점 구매 이상 거절: {failure}");
                        close = true;
                        _report($"상점 구매 거절: {failure}");
                        break;
                }
            }
            else if (!result.CompletionResult.Accepted)
            {
                if (CanPermit())
                {
                    ShowResultError(StageOfferErrorKind.CompletionRetry);
                    if (CanWork) _report($"상점 완료 재시도 필요: {result.CompletionResult.FailureReason}");
                }
                else
                {
                    close = true;
                    _report($"상점 완료 거절: {result.CompletionResult.FailureReason}");
                }
            }

            if (!CanWork) return;
            if (!close)
            {
                CurrentShopData displayedShop = _current.Shop;
                StageOfferPopupModel model = _models.Build();
                if (!CanWork) return;
                _screen.Render(model);
                if (!CanWork) return;
                // 처리한 스냅샷을 기억해 다음 idle 갱신이 방금 표시한 거절 안내를 덮지 않습니다.
                _displayContext = displayedShop;
            }
            if (!CanWork || (!MatchesDisplayContext() && !IsMismatch(result))) return;
            _writer.RemoveCommandsThrough(_pending.Number);
            _pending = default;
            if (!CanWork) return;
            _screen.SetCanSubmit(!close && CanSubmitNow());
            if (CanWork && close) CloseAndRefresh(completed);
        }

        private void ReconcileWithoutRequest()
        {
            if (_displayContext == null) return;
            // 떠난다를 기다리는 동안 완료된 노드라는 이유로 닫지 않습니다. 화면이 밖에서 닫혔으면 아래에서 맵을 새로 그립니다.
            if (_awaitingExit && IsOpen) return;
            if (!MatchesDisplayContext() || _stages.IsCleared(ChapterKey, NodeId.Value))
            {
                BlockSubmission();
                if (CanWork) CloseAndRefresh(false);
                return;
            }
            // 구매 마크가 외부 처리 중 바뀌어도 현재 방문의 Buffer 값으로 다시 그립니다.
            if (!ReferenceEquals(_displayContext, _current.Shop))
            {
                CurrentShopData displayedShop = _current.Shop;
                StageOfferPopupModel model = _models.Build();
                if (!CanWork) return;
                _screen.Render(model);
                if (!CanWork || !MatchesDisplayContext()) return;
                _displayContext = displayedShop;
            }
        }

        private void CloseAndRefresh(bool returnToSelection)
        {
            _skipNextRestore = true;
            _awaitingExit = false;
            if (IsOpen) _lifetime.CloseThen(returnToSelection, FinishCloseAndRefresh);
            else FinishCloseAndRefresh();
        }

        private void FinishCloseAndRefresh()
        {
            if (!CanWork) return;
            _displayContext = null;
            _mapChanged();
        }

        private void ShowResultError(StageOfferErrorKind kind)
        {
            if (IsOpen && _displayActive) _screen.ShowError(_texts.GetForStage(_stageKey).ErrorOf(kind));
        }
    }
}
