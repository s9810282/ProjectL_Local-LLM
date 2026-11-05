using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 연결 한 번의 수명입니다. 부품 넷(씬 표시·노드 진입·맵 입력·팝업)과 선택 화면을 갖고,
    /// 시작·Tick·정리 순서, 맵을 언제 그리고 언제 팝업을 되살릴지, 입력 판정 셋을 정합니다.
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
        // 부품에 넘기는 연결 확인입니다. 한 번만 만들어 Tick마다 대리자를 새로 만들지 않습니다.
        private readonly Func<bool> _canWork;
        private bool _started;
        private bool _readyForTick;
        private bool _isShown;
        private StageCombatDeparture _departure;
        private bool IsDepartureBusy => _departure?.IsBusy == true;

        internal StageMapViewScope(StageMapSceneRoot root, Camera output, StageMapViewServices services,
            StageMapViewPopups popups, RuntimeFaultBoundary faults)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _popups = popups ?? throw new ArgumentNullException(nameof(popups));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _canWork = () => CanWork;
            _scene = new StageMapViewScene(root, output, services);
            _reader = new StageMapViewReader(services.Current, services.Stages);
            _selection = new StageMapSelectionPresentation(services.UI, root.UI.Screens,
                () => IsCurrent, _canWork, CanShowSelection);
            _entry = new StageMapViewEntry(services, _reader, _selection, _canWork);
            _input = new StageMapViewInput(services, () => IsCurrent);
        }

        internal bool IsDisposed { get; private set; }
        internal bool IsSuperseded { get; private set; }
        public bool IsCurrent => !IsSuperseded;
        internal bool CanWork => !IsDisposed && !IsSuperseded && !_faults.IsFaulted;
        internal bool IsNoncombatMoving => _entry.IsNoncombatPresenting;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>개발 도구(맵 다시 그리기 등)를 써도 되는지입니다.</summary>
        internal bool CanUseDevelopmentTools => IsReady && _isShown && !_entry.IsPresenting && !IsAnyPopupOpen &&
            !HasPendingRequest && !IsDepartureBusy;
#endif

        /// <summary>연결이 일하고 팝업까지 준비됐습니다. 아래 판정은 모두 이것부터 봅니다.</summary>
        private bool IsReady => CanWork && _readyForTick;
        private bool HasPendingRequest => _services.Requests.TryGetNextCommand(0, out _);
        private bool IsAnyPopupOpen => _popups.Shop.IsOpen || _popups.Recovery.IsOpen || _popups.DeckEdit.IsOpen ||
            _popups.TraitGrowth.IsOpen;

        /// <summary>
        /// 선택 화면 → 노드 진입(표시 소유권·이동) → 맵 카메라 → 맵 입력·진입 관찰자 → 팝업 순으로 시작합니다.
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
            _input.Create(_scene, BlocksEntryForGrowth, InterceptEntry);
            _entry.StartObserver(RestoreNoncombat, Report);
            if (!_input.Connect(_canWork)) return;
            var popupInput = new PopupInputSession(new ViewAttachmentInputLayers(_services.Layers, () => IsCurrent),
                StageSelectionInputLayers.Selection, () => CanWork && _input.Interaction != null);
            if (!_popups.Start(this, popupInput, RequestMapRedraw, _selection, CanOpenDeckEdit, _canWork)) return;
            _departure = new StageCombatDeparture(_services.Current, _services.Stages, _services.Requests,
                _services.RequestWriter, _reader, _scene.Display, _scene.Root.MapRoot, _services.MapData,
                _services.Delay, _popups.DeckEdit, _canWork, _services.Audio);
            _entry.Departure = _departure;
            // 준비 뒤의 Tick·판정은 세 팝업과 진입 관찰자가 모두 있다고 보고 직접 부릅니다.
            _readyForTick = true;
        }

        internal void Tick()
        {
            if (!CanWork || !_readyForTick) return;
            CancelReturnWhileBusy();
            _departure.Tick();
            if (!CanWork) return;
            _entry.Tick();
            if (!CanWork) return;
            RefreshDisplay();
            if (!CanWork) return;
#if UNITY_EDITOR
            _input.TickCameraFraming();
            _scene.PreviewCameraFraming();
#endif
            _popups.Tick();
            if (!CanWork) return;
            OpenTraitGrowthWhenIdle();
            if (!CanWork) return;
            CancelReturnWhileBusy();
            _selection.Tick();
            if (!CanWork) return;
            _input.ReleaseHoverOutsideSelection();
        }

        internal void RequestMapRedraw()
        {
            if (CanWork) _isShown = false;
        }

        /// <summary>Catalog가 씬 로드 뒤 마지막으로 카메라를 수집한 다음(AfterLoad) 맵 카메라 핸들을 빌립니다.</summary>
        internal void BindCameraHandle()
        {
            if (CanWork) _scene.BindCameraHandle();
        }

        internal void Supersede()
        {
            IsSuperseded = true;
            _departure?.Dispose();
            _entry.Release();
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
            _isShown = false;
            List<Exception> errors = null;
            // 오류 상태를 먼저 고정해야 정리 콜백이 새 Scope로 이동을 건너뛰지 못합니다.
            if (lostPresentation)
            {
                var loss = new InvalidOperationException("[StageMap] 수락된 비전투 노드의 착지 전에 표시 소유권을 잃었습니다.");
                CleanupErrors.Try(() => _faults.Report(loss), ref errors);
            }
            // 표시 소유권은 교체(Supersede) 때도 먼저 돌려주는 자원이라 맨 앞입니다. 나머지는 시작의 역순입니다.
            CleanupErrors.Try(_entry.Release, ref errors);
            CleanupErrors.Try(() => _departure?.Dispose(), ref errors);
            CleanupErrors.Try(() => { if (IsCurrent) _services.RequestWriter.Clear(); }, ref errors);
            CleanupErrors.Try(_popups.Dispose, ref errors);
            CleanupErrors.Try(_selection.Dispose, ref errors);
            CleanupErrors.Try(_input.Dispose, ref errors);
            CleanupErrors.Try(_scene.Dispose, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 씬 정리 중 실패했습니다.");
        }

        private void CancelReturnWhileBusy()
        {
            if (_services.Requests.TryGetNextCommand(0, out _) || _entry.IsPresenting || IsDepartureBusy) _selection.CancelPendingReturn();
        }

        // 입력 판정 셋은 쓰는 순간 원본(팝업 컨트롤러·Buffer·입력 Layer)을 읽고 저장하지 않습니다(설계 §12.2 B4).

        /// <summary>선택 화면(StageSelect)을 다시 보여도 되는지입니다. 이동·처리 전 요청·팝업·팝업 결과 대기 중이면 보이지 않습니다.</summary>
        private bool CanShowSelection() => IsReady && _isShown && !_entry.IsPresenting && !HasPendingRequest && !IsDepartureBusy &&
            !IsAnyPopupOpen && !_popups.Shop.IsWaiting && !_popups.Recovery.IsWaiting && _input.IsSelectionLayer;

        /// <summary>
        /// 일반 덱 편집의 열기 조건입니다. 출전용 창은 표시 이동 완료 뒤 별도 경로로 열고,
        /// 이동·출전 준비 중에는 일반 열기 버튼으로 중복 팝업을 만들지 않습니다.
        /// </summary>
        private bool CanOpenDeckEdit() => IsReady && !_entry.IsPresenting && !IsDepartureBusy &&
            !_popups.Shop.IsOpen && !_popups.Recovery.IsOpen && !_popups.TraitGrowth.IsOpen;

        /// <summary>
        /// 증강 선택 화면을 여는 맵 쪽 조건입니다(강화 View 설계 §14 TG2, StageSelect 임시 자리). 맵을 그렸고, 진입 표시·처리 전 노드 요청이 없고,
        /// 상점·회복·덱 편집이 닫혀 있고 그 결과를 기다리지 않고, 입력 Layer가 선택 화면일 때만 엽니다. 상점 안에서 생긴 제안도 상점이 닫힌 뒤 열립니다.
        /// Layer 조건은 설정 창이 입력을 덮어쓴 동안 팝업 입력 세션이 Layer를 잡지 못해 멈추는 것을 막습니다(검토 H1).
        /// 보일 것은 증강 Controller가 다시 봅니다.
        /// </summary>
        private void OpenTraitGrowthWhenIdle()
        {
            if (!IsReady || !_isShown || _entry.IsPresenting || IsDepartureBusy || HasPendingRequest || _popups.Shop.IsOpen ||
                _popups.Recovery.IsOpen || _popups.DeckEdit.IsOpen || _popups.Shop.IsWaiting || _popups.Recovery.IsWaiting ||
                !_input.IsSelectionLayer)
                return;
            _popups.TraitGrowth.TryOpen();
        }

        /// <summary>
        /// 노드를 눌렀을 때 맵 진입을 막을지입니다(계획 P-TG1, 공통 레이아웃 설계 SO8). 증강이 남아 있으면 참입니다.
        /// 맵 입력은 팝업보다 먼저 만들어지므로 팝업이 아직 없으면 거짓이고, 그때 들어간 요청은 Core가 거절합니다.
        /// </summary>
        private bool BlocksEntryForGrowth()
        {
            TraitGrowthPopupController growth = _popups.TraitGrowth;
            if (!CanWork || growth == null) return false;
            try
            {
                // 진행 읽기는 Core까지 닿아 예외를 던질 수 있습니다. 입력 경로로 새지 않게 오류 경계에 보고하고 진입을 막습니다(검토 L5).
                return growth.IsPending;
            }
            catch (Exception error)
            {
                _faults.Report(error);
                return true;
            }
        }

        private bool InterceptEntry(int chapter, int node)
        {
            if (!IsReady || !_isShown || _entry.IsPresenting || IsAnyPopupOpen) return true;
            try
            {
                return _departure.TryBegin(chapter, node);
            }
            catch (Exception error)
            {
                _faults.Report(error);
                return true;
            }
        }

        /// <summary>처음 표시면 배경·노드·카메라 범위를 그리고, 이미 표시 중이면 현재 위치만 맞춥니다.</summary>
        private void RefreshDisplay()
        {
            if (_isShown)
            {
                if (!IsDepartureBusy && _reader.TryReadCurrentNode(out int currentNodeId)) _entry.SynchronizeCurrentNode(currentNodeId);
                if (!CanWork) _scene.Display.Clear();
                return;
            }
            if (!_reader.TryRead(out int mapKey, out IReadOnlyList<StageProgressData> nodes)) return;
            _reader.TryReadCurrentNode(out int initialNodeId);
            _scene.ShowBackground(mapKey);
            if (!CanWork)
            {
                _scene.ClearBackground();
                return;
            }
            _scene.ShowNodes(mapKey, nodes, initialNodeId);
            if (!CanWork)
            {
                _scene.Display.Clear();
                return;
            }
            _entry.SynchronizeCurrentNode(initialNodeId);
            if (!CanWork) return;
            _input.Interaction.SetDisplayedChapter(mapKey);
            if (!CanWork) return;
            _scene.ConfigureCameraBounds(nodes);
            if (!CanWork) return;
            _isShown = true;
            RestoreNoncombat(mapKey);
        }

        /// <summary>
        /// 선택된 비전투 노드의 팝업을 되살립니다. 노드 종류 이름(<c>StageType</c>)은 읽지 않고, Buffer에 있는 그 방문의 상태로 고릅니다 —
        /// 상점 상태가 있으면 상점, 끝나지 않은 회복 상태가 있으면 회복입니다(검사 후속 정리 설계 §12.6 Q-R9).
        /// 상태가 선택한 노드의 것인지는 각 팝업이 자기 문맥을 잡을 때 확인합니다. 진행 중 요청이 있거나 선택 화면 부모가 없으면 하지 않습니다.
        /// </summary>
        private void RestoreNoncombat(int chapter)
        {
            var current = _services.Current;
            if (!CanWork || !_selection.CanOpenScreens || current.CurrentMapKey != chapter || !current.HasSelection ||
                HasPendingRequest) return;
            try
            {
                bool hasShop = current.Shop != null;
                bool hasRecovery = _services.RecoveryState.TryGetCurrent(out RecoveryStateSnapshot state) &&
                    !state.IsCompleted;
                if (hasShop && hasRecovery)
                    throw new InvalidOperationException("[StageMap] 상점과 회복 상태가 동시에 활성입니다.");
                if (hasShop) _popups.Shop.TryRestore(chapter);
                else if (hasRecovery) _popups.Recovery.TryRestore(chapter);
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }

        private static void Report(string message) => Debug.Log($"[StageMap] {message}");
    }
}
