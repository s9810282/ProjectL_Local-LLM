using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 연결 한 번의 수명입니다. 부품(씬 표시·노드 진입·맵 입력·팝업·진입 준비·비전투 복원 대기)과 선택 화면을 갖고,
    /// 시작·Tick·정리 순서와 판정 둘(맵 한가함·노드 진입 가능)만 정합니다(스테이지 선택 리팩토링 설계 §2.6 C-01·§3.2).
    /// </summary>
    /// <remarks>
    /// 연결 확인(<see cref="CanWork"/>)은 재진입할 수 있는 호출(UI 열고 닫기·Instantiate·외부 콜백) 뒤에만 다시 합니다.
    /// 순수 Buffer 읽기 뒤에는 하지 않습니다(검사 후속 정리 설계 §12.2 B7).
    /// </remarks>
    internal sealed class StageMapViewScope : IPopupViewHost, IDisposable
    {
        private readonly StageMapViewServices _services;
        private readonly RuntimeFaultBoundary _faults;
        private readonly StageMapViewReader _reader;
        private readonly StageMapSelectionPresentation _selection;
        private readonly StageMapViewScene _scene;
        private readonly StageMapViewEntry _entry;
        private readonly StageMapViewInput _input;
        private readonly StageMapViewPopups _popups;
        private readonly StageMapViewNoncombatRestore _restore;
        private readonly StageMapViewEntryPreparation _preparation;
        private readonly StageNodeClearPresentation _completion;
        private readonly StageMapIntroPresentation _intro;
        private readonly StageMapIntroInteractionPolicy _introPolicy;
        private readonly StageMapCameraFocus _cameraFocus;
        // 부품에 넘기는 연결 확인입니다. 한 번만 만들어 Tick마다 대리자를 새로 만들지 않습니다.
        private readonly Func<bool> _canWork;
        private bool _started;
        private bool _readyForTick;
        private StageCombatDeparture _departure;
        private bool IsDepartureBusy => _departure?.IsBusy == true;

        internal StageMapViewScope(StageMapSceneRoot root, Camera output, StageMapViewServices services,
            StageMapViewPopups popups, StageMapIntroPresentation intro,
            StageMapIntroInteractionPolicy introPolicy, RuntimeFaultBoundary faults, StageMapCameraFocus cameraFocus,
            IStageMapSceneFactory factory)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _popups = popups ?? throw new ArgumentNullException(nameof(popups));
            _intro = intro ?? throw new ArgumentNullException(nameof(intro));
            _introPolicy = introPolicy ?? throw new ArgumentNullException(nameof(introPolicy));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _cameraFocus = cameraFocus ?? throw new ArgumentNullException(nameof(cameraFocus));
            _canWork = () => CanWork;
            _scene = new StageMapViewScene(root, output, services.Scene);
            _reader = new StageMapViewReader(services.Current, services.Stages);
            _selection = new StageMapSelectionPresentation(services.UI, root.UI.Screens, root.UI.Selection,
                () => IsCurrent, _canWork, IsMapIdle);
            _restore = new StageMapViewNoncombatRestore(services.Current, services.RunProgress, services.RecoveryState,
                popups, faults, _canWork, () => IsMapIdle() && _selection.CanOpenScreens);
            _entry = new StageMapViewEntry(services.Entry, _reader, _selection, _canWork, cameraFocus,
                CaptureMovementContext, IsMovementContextCurrent);
            _input = new StageMapViewInput(services.Input, () => IsCurrent, faults);
            _completion = factory.CreateCompletion(_scene, services.Completion, cameraFocus, _canWork,
                CaptureMovementContext, IsMovementContextCurrent, _input.ClearPointer);
            _preparation = new StageMapViewEntryPreparation(services, _reader, _scene, _input, _entry, intro, cameraFocus,
                _restore, faults, _canWork, () => IsCurrent, () => _readyForTick, CancelInvalidMovements, _completion);
        }

        internal bool IsDisposed { get; private set; }
        internal bool IsSuperseded { get; private set; }
        public bool IsCurrent => !IsSuperseded;
        internal bool CanWork => !IsDisposed && !IsSuperseded && !_faults.IsFaulted;
        internal bool IsNoncombatMoving => _entry.IsNoncombatPresenting;
        internal long EntryPreparationVersion => _preparation.Version;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>개발 도구(맵 다시 그리기 등)를 써도 되는지입니다. 맵이 한가할 때만입니다.</summary>
        internal bool CanUseDevelopmentTools => IsMapIdle();
#endif

        /// <summary>연결이 일하고 팝업까지 준비됐습니다. 아래 판정은 모두 이것부터 봅니다.</summary>
        private bool IsReady => CanWork && _readyForTick;
        private bool HasPendingRequest => _services.Requests.TryGetNextCommand(0, out _);
        private bool IsPreparedContext => _preparation.IsPreparedContext;

        /// <summary>
        /// 선택 화면 → 노드 진입(표시 소유권·이동) → 맵 카메라 → 맵 입력 → 팝업 → 출전·진입 관찰자 순으로 시작합니다.
        /// 카메라 핸들은 <see cref="BindCameraHandle"/>(AfterLoad)까지 미룹니다.
        /// </summary>
        internal void Start(Action requireRunning)
        {
            if (_started || !CanWork) return;
            _started = true;
            _selection.Start();
            if (!CanWork) return;
            if (!_entry.StartMovement(_scene)) return;
            StageMapCameraDrag drag = _scene.CreateCameraDrag();
            try
            {
                requireRunning?.Invoke();
            }
            finally
            {
                // 확인 콜백에서 끝난 연결의 드래그도 정리합니다.
                if (!CanWork) drag.Dispose();
            }
            if (!CanWork) return;
            _scene.AdoptCameraDrag(drag);
            _cameraFocus.Bind(drag);
            _input.Create(_scene, BlocksEntryForGrowth, CanEnterNode, IsMapIdle, InterceptEntry);
            _input.ClearPointer();
            if (!_input.Connect(_canWork)) return;
            var popupInput = new PopupInputSession(new ViewAttachmentInputLayers(_services.Layers, () => IsCurrent),
                StageSelectionInputLayers.Selection, () => CanWork && _input.IsCreated);
            if (!_popups.Start(this, popupInput, RequestMapRedraw, _selection, IsMapIdle, _canWork)) return;
            _departure = new StageCombatDeparture(_services.Entry, _reader, _scene.Display,
                new StageMapViewMarkerJump(_scene.Display, _scene.Root.MapRoot, _services.MapData,
                    _services.Entry.Delay, _services.Entry.Audio, _cameraFocus, "StageCombatDeparture"),
                _popups.DeckEdit, _canWork, CaptureMovementContext, IsMovementContextCurrent);
            // 진입 관찰자는 출전 부품을 받아야 하므로 그 뒤에 시작합니다. Tick은 아래 준비 표시 뒤에만 돌아 그 사이 결과를 놓치지 않습니다.
            _entry.StartObserver(_departure, _restore.Request, Report, ShowEntryRejected);
            // 준비 뒤의 Tick·판정은 팝업 넷과 출전·진입 관찰자가 모두 있다고 보고 직접 부릅니다.
            _readyForTick = true;
        }

        internal void Tick()
        {
            if (!CanWork || !_readyForTick) return;
            if (_preparation.IsShown && !IsPreparedContext)
            {
                _preparation.Cancel(false);
                return;
            }
            if (!_preparation.IsShown)
            {
                // 재준비 중에는 이전 수락 결과가 새 이동을 시작하지 않게 준비만 진행합니다.
                // 정상 표시 중의 관찰→이동→갱신 순서는 그대로 유지합니다.
                RefreshDisplay();
                return;
            }
            CancelReturnWhileBusy();
            _departure.Tick();
            if (!CanWork || !IsPreparedContext) return;
            _entry.Tick();
            if (!CanWork || !IsPreparedContext) return;
            RefreshDisplay();
            if (!CanWork) return;
            if (!_preparation.IsShown) return;
            try
            {
                _intro.Tick(Time.deltaTime, _services.Loading.IsLoading, Time.timeScale == 0f);
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
            if (!CanWork) return;
            if (!IsPreparedContext)
            {
                _preparation.Cancel();
                return;
            }
            _popups.Tick();
            if (!CanWork || !IsPreparedContext) return;
            TickCompletion();
            if (!CanWork || !IsPreparedContext) return;
            _restore.Tick();
            if (!CanWork) return;
            OpenTraitGrowthWhenIdle();
            if (!CanWork) return;
            CancelReturnWhileBusy();
            _selection.Tick();
            if (!CanWork) return;
            _input.ReleaseHoverOutsideSelection();
            _input.TickHover(IsMapIdle(), Time.unscaledDeltaTime, IsHeldByIntro);
        }

        internal void RequestMapRedraw()
        {
            if (CanWork) _preparation.BeginVisibleRedraw();
        }

        /// <summary>Catalog가 씬 로드 뒤 마지막으로 카메라를 수집한 다음(AfterLoad) 맵 카메라 핸들을 빌립니다.</summary>
        internal void BindCameraHandle()
        {
            if (CanWork) _scene.BindCameraHandle();
        }

        internal void Supersede()
        {
            IsSuperseded = true;
            List<Exception> errors = null;
            CleanupErrors.Try(_preparation.Cancel, ref errors);
            CleanupErrors.Try(() => _departure?.Dispose(), ref errors);
            CleanupErrors.Try(_entry.Release, ref errors);
            CleanupErrors.Try(() => _cameraFocus.Release(false), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 이전 연결 정리 중 실패했습니다.");
        }

        public void SetPopupBlocked(bool blocked)
        {
            // 팝업의 입력 세션이 맵 입력을 막으므로 별도 차단 상태를 만들지 않습니다.
        }

        public void Dispose() => Dispose(false);

        internal void Dispose(bool suppressMovementLoss)
        {
            if (IsDisposed) return;
            bool lostPresentation = !suppressMovementLoss && IsNoncombatMoving;
            IsDisposed = true;
            _readyForTick = false;
            List<Exception> errors = null;
            // 오류 상태를 먼저 고정해야 정리 콜백이 새 Scope로 이동을 건너뛰지 못합니다.
            if (lostPresentation)
            {
                var loss = new InvalidOperationException("[StageMap] 수락된 비전투 노드의 착지 전에 표시 소유권을 잃었습니다.");
                CleanupErrors.Try(() => _faults.Report(loss), ref errors);
            }
            // 표시 소유권은 교체(Supersede) 때도 먼저 돌려주는 자원이라 맨 앞입니다. 나머지는 시작의 역순입니다.
            // 맵·상점 입력 Buffer는 System이 이 정리 뒤에 비웁니다(평가 N-06).
            CleanupErrors.Try(_preparation.Cancel, ref errors);
            CleanupErrors.Try(_entry.Release, ref errors);
            CleanupErrors.Try(() => _departure?.Dispose(), ref errors);
            CleanupErrors.Try(() => _cameraFocus.Release(false), ref errors);
            CleanupErrors.Try(_popups.Dispose, ref errors);
            CleanupErrors.Try(_selection.Dispose, ref errors);
            CleanupErrors.Try(_input.Dispose, ref errors);
            CleanupErrors.Try(_scene.Dispose, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 씬 정리 중 실패했습니다.");
        }

        // 로딩 준비(StageMapLoadingPreparation)가 쓰는 입구입니다. 진입 준비 부품에 그대로 넘깁니다.
        internal void BeginScenePreparation(long version, bool isCovered) =>
            _preparation.BeginScenePreparation(version, isCovered);

        internal LodingPreparationState TryPrepareEntry() => _preparation.TryPrepare();

        internal void CancelEntryPreparation() => _preparation.Cancel();

        private StageMapMovementContext CaptureMovementContext() => _preparation.CaptureMovementContext();

        private bool IsMovementContextCurrent(StageMapMovementContext context) =>
            _preparation.IsMovementContextCurrent(context);

        // 판정은 쓰는 순간 원본(팝업 목록·Buffer·입력 Layer)을 읽고 저장하지 않습니다(설계 §12.2 B4).

        /// <summary>
        /// 아무것도 움직이거나 기다리지 않고 맵 선택 입력이 살아 있는지입니다. 준비가 끝난 맵이 보이고, 노드 이동·출전·처리 전 노드 요청이 없고,
        /// 팝업이 모두 닫혀 결과를 기다리지 않고, 입력 Layer가 맵 선택입니다. 인트로·로딩은 보지 않습니다.
        /// </summary>
        private bool IsMapQuiet => IsReady && IsPreparedContext && !_entry.IsPresenting && !IsDepartureBusy &&
            !HasPendingRequest && !_popups.IsAnyOpen && !_popups.IsAnyWaiting && _input.IsSelectionLayer;

        /// <summary>인트로 준비가 끝났고 인트로 차단 정책이 맵을 붙잡지 않습니다. 차단 정책은 인트로 설계대로 바꿔 끼울 수 있는 임시 정책입니다.</summary>
        private bool IsIntroInteractive => _intro.IsPrepared && !_introPolicy.BlocksInteraction;

        /// <summary>
        /// 맵 한가함입니다. 선택 화면 표시·노드 호버·끌기·휠·덱 열기·증강 열기·개발 도구·비전투 복원이 모두 이것만 봅니다(설계 §2.6).
        /// </summary>
        private bool IsMapIdle() => IsMapQuiet && IsIntroInteractive && !_services.Loading.IsLoading &&
            !_completion.BlocksMapInput;

        /// <summary>완료 연출 자신의 입력 차단을 제외한 시작/진행 조건입니다. 인트로는 완전히 끝나야 합니다.</summary>
        private bool CanPresentCompletion() => IsMapQuiet && _intro.IsPrepared && !_intro.IsPending &&
            !_services.Loading.IsLoading && Time.timeScale > 0f;

        private void TickCompletion()
        {
            try
            {
                if (!_reader.TryReadCurrentNode(out int nodeId))
                    throw new InvalidOperationException("[StageMap] 완료 연출을 관찰할 현재 노드가 없습니다.");
                int mapKey = _services.Current.CurrentMapKey;
                var key = new StageNodeClearKey(_services.RunProgress.RunId, mapKey, nodeId);
                _completion.Tick(new StageNodeClearFrame(key, _services.Stages.IsCleared(mapKey, nodeId),
                    _preparation.Version, IsPreparedContext, CanPresentCompletion()));
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }

        /// <summary>
        /// 노드를 눌러 진입을 요청해도 되는지입니다. 맵이 한가하고 선택 화면이 실제로 보여야 합니다. 누름·뗌 제출·출전 가로채기만 봅니다.
        /// 전투 노드는 착지 뒤 덱 편집을 선택 화면의 자식으로 열므로, 선택 화면이 닫힌 채 받으면 부모 미열림 예외(F1)가 납니다(평가 C-15).
        /// 선택 화면은 맵이 한가해지면 다음 Tick에 열리므로, 첫 표시·팝업에서 돌아온 직후 잠깐은 클릭을 받지 않습니다(의도한 입력 제한).
        /// </summary>
        private bool CanEnterNode() => IsMapIdle() && _selection.IsSelectionShown;

        /// <summary>
        /// 인트로가 맵을 붙잡은 동안입니다. 호버만 비우고 이미 막힌 누름은 떼기·취소까지 남깁니다(맵이 다른 이유로 막히면 누름도 내려놓음).
        /// </summary>
        private bool IsHeldByIntro => IsMapQuiet && _introPolicy.BlocksInteraction;

        /// <summary>노드 진입이 진행 중(처리 전 요청·이동·출전)이면 선택 화면으로 돌아가는 예약을 거둡니다.</summary>
        private void CancelReturnWhileBusy()
        {
            if (HasPendingRequest || _entry.IsPresenting || IsDepartureBusy) _selection.CancelPendingReturn();
        }

        /// <summary>
        /// 맵이 한가하면 증강 선택 화면을 엽니다(강화 View 설계 §14 TG2, StageSelect 임시 자리). 보일 것이 있는지는 증강 컨트롤러가 봅니다.
        /// 상점 안에서 생긴 제안도 상점이 닫힌 뒤 열립니다. 맵 한가함의 Layer 조건은 설정 창이 입력을 덮어쓴 동안
        /// 팝업 입력 세션이 Layer를 잡지 못해 멈추는 것을 막습니다(검토 H1).
        /// </summary>
        private void OpenTraitGrowthWhenIdle()
        {
            if (IsMapIdle()) _popups.TraitGrowth.TryOpen();
        }

        /// <summary>
        /// 노드를 눌렀을 때 맵 진입을 막을지입니다(계획 P-TG1, 공통 레이아웃 설계 SO8). 증강이 남아 있으면 참입니다.
        /// 맵 입력은 팝업보다 먼저 만들어지므로 팝업이 아직 없으면 거짓이고, 그때 들어간 요청은 Core가 거절합니다.
        /// </summary>
        private bool BlocksEntryForGrowth()
        {
            TraitGrowthPopupController growth = _popups.TraitGrowth;
            if (!CanWork || growth == null) return false;
            // 진행 읽기는 Core까지 닿아 예외를 던질 수 있습니다. 입력 콜백 경로에서만 불리고, 그 경로는 입력 감싸개가 오류 경계로 보냅니다.
            return growth.IsPending;
        }

        private bool InterceptEntry(int chapter, int node)
        {
            if (!CanEnterNode()) return true;
            // 입력 콜백 경로에서만 불립니다. 예외는 입력 감싸개가 오류 경계로 보냅니다.
            StageCombatDeparture.StartResult result = _departure.TryBegin(chapter, node);
            // 연결 밖 전투 노드는 출전이 말을 움직이지 않고 삼킵니다. Core 거절과 같은 문구로 알립니다(평가 C-16).
            if (result == StageCombatDeparture.StartResult.NotConnected) _services.Notice.Show(StageMapEntryText.NotConnected);
            return result != StageCombatDeparture.StartResult.NotIntercepted;
        }

        /// <summary>처음 표시면 배경·노드·카메라 범위를 그리고, 이미 표시 중이면 현재 위치만 맞춥니다.</summary>
        private void RefreshDisplay()
        {
            if (_preparation.TryPrepare() != LodingPreparationState.Complete || _intro.IsPending) return;
            if (!IsDepartureBusy && _reader.TryReadCurrentNode(out int currentNodeId))
                _entry.SynchronizeCurrentNode(currentNodeId);
            if (!CanWork) _scene.Display.Clear();
        }

        /// <summary>준비를 취소할 때 출전 → 노드 이동 순으로 무효가 된 이동 문맥을 거둡니다.</summary>
        private void CancelInvalidMovements()
        {
            List<Exception> errors = null;
            CleanupErrors.Try(() => _departure?.CancelInvalidMovement(), ref errors);
            CleanupErrors.Try(_entry.CancelInvalidMovement, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 이전 이동 정리 실패.");
        }

        private static void Report(string message) => Debug.Log($"[StageMap] {message}");

        private void ShowEntryRejected(ChapterProgressionResult result)
        {
            if (CanWork) _services.Notice.Show(StageMapEntryText.Rejected(result));
        }
    }
}
