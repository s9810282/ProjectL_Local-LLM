using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine.Rendering;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// StageSelect 씬에 붙었다 떨어지는 아티팩트 팝업 View 시스템입니다. 런타임에 한 번 등록되고,
    /// 씬이 로드될 때마다 <see cref="ArtifactSceneRoot"/>를 한 번 찾아 연결합니다.
    /// </summary>
    /// <remarks>
    /// 3D 보드와 입력 Layer 전환, 요청 스케줄러 소유, 언제 다시 그릴지를 맡습니다. 3D 그리기는 Presenter가,
    /// 버튼·목록·문구는 현강 UI 구조의 팝업 UI(<see cref="IArtifactPopupScreen"/>)가, 요청 실행은 스케줄러·Job이 맡습니다.
    /// 열기·닫기는 UISystem이 하고, UI가 열림·숨김을 <see cref="IArtifactPopupHost"/>로 알려 줍니다. 보드 초기화는 실행 계층(StageLifeCycle)이 맡고 이 시스템은 InGame DB를 읽지 않습니다(D7).
    /// 오류 경계는 읽기만 하며 입력 정리 콜백을 연결하지 않습니다(D11, §3).
    /// </remarks>
    internal sealed class ArtifactViewSystem :
        ISystemInitData<IHasViewQuery>,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices,
        IArtifactInputReceiver,
        IArtifactPopupHost
    {
        // StageLifeCycle의 Enter(0) 뒤에 연결하고, Exit(0) 전에 정리합니다.
        internal const int AttachPriority = 100;
        internal const int DetachPriority = -100;

        private readonly RuntimeFaultBoundary _faults;
        private readonly Action<ScriptableRenderContext, Camera> _cameraRenderingAction;
        private readonly Camera.CameraCallback _cameraPreCullAction;
        private ILodingRegisterService _loadingRegister;
        private IInputLayerService _layers;
        private IUISystemService _ui;
        private IArtifactBufferReadSink _board;
        private ArtifactEditScheduler _scheduler;
        private Func<int, Sprite> _findIcon;
        private Func<int, ArtifactViewData> _findVisual;
        private ArtifactPopupConfigData _layout;
        private IArtifactInputSlot _inputSlot;
        private ArtifactBoardMapper _mapper;
        private readonly ArtifactInteraction _interaction = new();
        private Vector2 _pointer;
        private ArtifactPreviewRequest _previewRequest;
        private ArtifactPreviewRequest _shownPreview;
        private bool _inputBound;
        private bool _previewStatusShown;

        private ArtifactSceneRoot _root;
        private IArtifactPopupPresenter _presenter;
        private IArtifactPopupScreen _screen;
        private bool _isOpen;
        private ArtifactBufferSnapshot _drawnSnapshot;
        private bool _isShowingWaiting;
        private ArtifactEditRequest _reportedEdit;

        internal ArtifactViewSystem(RuntimeFaultBoundary faults)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _cameraRenderingAction = OnCameraRendering;
            _cameraPreCullAction = OnCameraPreCull;
        }

        /// <summary>씬에 연결된 SceneRoot입니다. 연결 전이나 Detach 뒤에는 null입니다.</summary>
        internal ArtifactSceneRoot Root => _root;

        internal bool IsOpen => _isOpen;

        /// <summary>팝업이 쓰는 요청 스케줄러입니다. 입력 연결(Phase D)이 요청을 넣습니다.</summary>
        internal ArtifactEditScheduler Scheduler => _scheduler;

        public void InitData(IHasViewQuery data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            IViewQuery view = data.ViewQuery
                ?? throw new InvalidOperationException("[ArtifactViewSystem] View database query is required.");
            IArtifactViewTableQuery icons = view.ArtifactViewTableQuery;
            BindIcons(artifactId => icons.TryGetArtifactView(artifactId, out ArtifactViewData viewData) ? viewData.Icon : null);
            _findVisual = artifactId => icons.TryGetArtifactView(artifactId, out ArtifactViewData visual) ? visual : null;
            _layout = (view as IHasArtifactPopupConfig)?.ArtifactPopupConfig.Config
                ?? throw new InvalidOperationException("[ArtifactViewSystem] Artifact popup View DB configuration is required.");
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            var global = resolver.GetService(SystemGroupsService.Global);
            _inputSlot = resolver.GetService(ArtifactInputKey.ReceiverSlot);
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            var scheduler = new ArtifactEditScheduler(
                global.JobServiceCommand.JobRunner,
                resolver.GetService(SystemGroupsService.InGame).CoreGroup.Artifacts,
                buffers.ArtifactReadSink,
                buffers.ArtifactEditResultReadSink,
                buffers.ArtifactPreviewReadSink,
                _faults);
            BindServices(global.LodingServiceCommand.LodingRegisterService,
                global.InputServiceCommand.InputLayerServices, global.UIServiceCommand, buffers.ArtifactReadSink, scheduler);
        }

        public void Initialize()
        {
            if (_loadingRegister == null || _findIcon == null)
                throw new InvalidOperationException(
                    "[ArtifactViewSystem] InitData and PostBindService must complete before initialization.");

            _loadingRegister.RegisterSceneEnterLoaded(SceneIndices.Stage, new LodingNode(AttachPriority, AttachFromScene));
            _loadingRegister.RegisterSceneExitLoaded(SceneIndices.Stage, new LodingNode(DetachPriority, DetachFromScene));
        }

        public void Tick(float deltaTime)
        {
            if (_isOpen)
            {
                Refresh();
                if (_root != null) _root.PopupView.UpdateBackground();
                if (_interaction.IsDragging)
                {
                    if (!AllowsInteraction()) CancelDrag();
                    else
                    {
                        UpdateCandidate();
                        ShowCompletedPreview();
                    }
                }
            }
        }

        public void Shutdown()
        {
            Detach();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddModule<ISystemModule>(this);
        }

        /// <summary>View DB 아이콘 조회 함수를 보관합니다. 외형이 없으면 null을 돌려줘야 합니다(D17).</summary>
        internal void BindIcons(Func<int, Sprite> findIcon)
        {
            _findIcon = findIcon ?? throw new ArgumentNullException(nameof(findIcon));
        }

        /// <summary>필요한 서비스만 보관합니다.</summary>
        internal void BindServices(ILodingRegisterService loadingRegister, IInputLayerService layers, IUISystemService ui,
            IArtifactBufferReadSink board, ArtifactEditScheduler scheduler)
        {
            _loadingRegister = loadingRegister ?? throw new ArgumentNullException(nameof(loadingRegister));
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        /// <summary>씬의 SceneRoot를 연결하고 팝업 UI에 이 시스템을 열림·숨김 수신자로 한 번 연결합니다.</summary>
        internal void AttachScene(ArtifactSceneRoot root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            root.Validate();
            if (_layout != null) root.PopupView.Configure(_layout);
            AttachPresenter(root.PopupView, root.PopupUI);
            _root = root;
            root.PopupView.ConnectCamera(root.OutputCamera);
            _mapper = new ArtifactBoardMapper(root.PopupView.WorldRoot, root.PopupView.Configuration);
            RenderPipelineManager.beginCameraRendering += _cameraRenderingAction;
            Camera.onPreCull += _cameraPreCullAction;
        }

        /// <summary>3D 그리기 대상과 팝업 UI를 연결합니다. 이전 연결이 남아 있으면 먼저 정리합니다. 팝업은 닫힌 상태로 시작합니다.</summary>
        internal void AttachPresenter(IArtifactPopupPresenter presenter, IArtifactPopupScreen screen)
        {
            if (presenter == null)
                throw new ArgumentNullException(nameof(presenter));
            if (screen == null)
                throw new ArgumentNullException(nameof(screen));
            if (_scheduler == null || _findIcon == null)
                throw new InvalidOperationException("[ArtifactViewSystem] Services must be bound before attaching.");

            Detach();
            _presenter = presenter;
            _screen = screen;
            screen.Connect(this);
            presenter.Hide();
        }

        /// <summary>연결됐고 오류 상태가 아니면 팝업을 열 수 있습니다. 팝업 UI가 UISystem에 열기를 요청하기 전에 확인합니다.</summary>
        public bool CanOpen => _presenter != null && _screen != null && !_faults.IsFaulted;

        /// <summary>UISystem이 팝업 UI를 연 직후입니다.</summary>
        public void PopupOpened() => Open();

        /// <summary>UISystem이 팝업 UI를 숨긴 직후입니다(닫기 버튼·ESC·뒤로가기·전체 숨김).</summary>
        public void PopupHidden() => Close();

        /// <summary>
        /// 3D 보드를 열고 입력을 연결합니다. 연결 전·이미 열림·오류 상태면 무시합니다(D11·D12). 팝업 UI의 열림 알림으로 불립니다.
        /// 열면 Artifact Layer로 바꾸고 이전 프리뷰를 버린 뒤 Buffer 기준으로 그립니다.
        /// </summary>
        internal void Open()
        {
            if (_presenter == null || _isOpen || _faults.IsFaulted)
                return;

            _isOpen = true;
            _drawnSnapshot = null;
            _isShowingWaiting = false;
            _scheduler.InvalidatePreview();
            _presenter.Show();
            if (_inputSlot != null)
            {
                _inputSlot.Bind(this);
                _inputBound = true;
            }
            _layers.ChangeLayer(ArtifactInputLayers.Artifact);
            Refresh();
        }

        /// <summary>
        /// 3D 보드를 닫고 입력을 해제합니다. 대기 프리뷰만 취소하고 대기 편집은 계속 실행합니다(D15). 팝업 UI의 숨김 알림으로 불립니다.
        /// 현재 Layer가 Artifact일 때만 StageSelection으로 되돌립니다(D2). 닫혀 있으면 아무것도 하지 않습니다.
        /// </summary>
        internal void Close()
        {
            if (!_isOpen)
                return;

            _isOpen = false;
            _drawnSnapshot = null;
            CancelDrag();
            ReleaseInput();
            _scheduler.InvalidatePreview();
            try
            {
                _presenter.Hide();
            }
            finally
            {
                if (_layers.CurrentLayer.Equals(ArtifactInputLayers.Artifact))
                    _layers.ChangeLayer(StageSelectionInputLayers.Selection);
            }
        }

        /// <summary>
        /// 씬 이탈 정리입니다. 팝업 UI와의 연결을 먼저 끊고, 열려 있으면 UISystem으로 닫아 이력을 정리한 뒤 <see cref="Detach"/>합니다.
        /// 연결을 먼저 끊어야 숨김 알림이 맵 Layer를 다시 켜지 않습니다(D2).
        /// </summary>
        internal void DetachFromScene()
        {
            if (_isOpen && _screen != null)
            {
                _screen.Disconnect();
                _ui?.CloseUI(UIId.Artifact);
            }

            Detach();
        }

        /// <summary>
        /// 씬 이탈·종료 정리입니다. 대기 프리뷰와 실행 전 편집을 취소하고(D15), 팝업 UI 연결을 끊고, 3D 보드를 숨깁니다.
        /// 현재 Layer가 Artifact일 때만 끄고 StageSelection은 다시 켜지 않습니다(D2). 여러 번 불러도 안전합니다.
        /// </summary>
        internal void Detach()
        {
            RenderPipelineManager.beginCameraRendering -= _cameraRenderingAction;
            Camera.onPreCull -= _cameraPreCullAction;
            CancelDrag();
            ReleaseInput();
            _scheduler?.CancelPending();

            _screen?.Disconnect();

            bool wasOpen = _isOpen;
            IArtifactPopupPresenter presenter = _presenter;
            _isOpen = false;
            _presenter = null;
            _screen = null;
            _root = null;
            _mapper = null;
            _drawnSnapshot = null;

            if (!wasOpen)
                return;

            try
            {
                presenter.Hide();
            }
            finally
            {
                if (_layers.CurrentLayer.Equals(ArtifactInputLayers.Artifact))
                    _layers.ClearLayer();
            }
        }

        // 보드와 결과 문구를 따로 검사합니다(D14). 거절된 편집은 스냅샷을 바꾸지 않고 결과만 게시합니다.
        private void Refresh()
        {
            ArtifactBufferSnapshot current = _board.Current;
            if (!ReferenceEquals(current, _drawnSnapshot))
            {
                if (_interaction.IsDragging) CancelDrag();
                _drawnSnapshot = current;
                if (!current.IsInitialized)
                {
                    _presenter.ClearBoard();
                    _screen.ShowStatus(ArtifactPopupText.Waiting);
                    _isShowingWaiting = true;
                }
                else
                {
                    _presenter.Render(BuildBoard(current));
                    if (_isShowingWaiting)
                    {
                        _screen.ShowStatus(string.Empty);
                        _isShowingWaiting = false;
                    }
                }
            }

            ArtifactEditRequest edit = _scheduler.CurrentEdit;
            if (edit != null && !ReferenceEquals(edit, _reportedEdit) && IsFinished(edit))
            {
                _reportedEdit = edit;
                _screen.ShowStatus(ArtifactPopupText.ForEdit(edit));
            }
        }

        private ArtifactPopupBoard BuildBoard(ArtifactBufferSnapshot snapshot)
        {
            return new ArtifactPopupBoard(snapshot.BoardWidth, snapshot.BoardHeight,
                ToItems(snapshot.PlacedItems), ToItems(snapshot.PoolItems));
        }

        private ArtifactPopupItem[] ToItems(IReadOnlyList<ArtifactData> source)
        {
            var items = new ArtifactPopupItem[source.Count];
            for (int i = 0; i < source.Count; i++)
            {
                ArtifactData data = source[i];
                ArtifactViewData visual = _findVisual?.Invoke(data.ArtifactId);
                items[i] = new ArtifactPopupItem(data.ArtifactId, data.InstanceId, data.Cells, _findIcon(data.ArtifactId),
                    visual?.Prefab, data.Rotation, visual?.DisplayName, visual?.Description);
            }

            return items;
        }

        private static bool IsFinished(ArtifactEditRequest request)
        {
            return request.Status == ArtifactRequestStatus.Completed || request.Status == ArtifactRequestStatus.Faulted;
        }

        private void ShowDetail(int artifactId)
        {
            if (!_isOpen || _faults.IsFaulted || _screen == null) return;
            _screen.ShowDetail(artifactId);
        }

        public void PointerMoved(float screenX, float screenY)
        {
            _pointer = new Vector2(screenX, screenY);
            if (!AllowsInteraction()) { CancelDrag(); return; }
            if (_interaction.IsDragging) UpdateCandidate();
        }

        public void SelectPressed(float screenX, float screenY)
        {
            _pointer = new Vector2(screenX, screenY);
            if (!AllowsInteraction()) { CancelDrag(); return; }
            CancelDrag();
            var view = _root.PopupView;
            if (IsButtonOrDetail(_pointer)) return;
            if (_screen.TryPickPool(_pointer, out int artifactId))
                _interaction.BeginPool(artifactId);
            else if (!_screen.IsReturnArea(_pointer) && TryPointerCell(out var hit) &&
                ArtifactBoardMapper.Contains(hit, _board.Current.BoardWidth, _board.Current.BoardHeight))
            {
                foreach (ArtifactData item in _board.Current.PlacedItems)
                {
                    bool found = false;
                    foreach (var cell in item.Cells) if (cell == hit) { found = true; break; }
                    if (!found) continue;
                    _interaction.BeginPlaced(item.ArtifactId, item.InstanceId, item.Anchor, item.Rotation, hit);
                    break;
                }
            }
            if (!_interaction.IsDragging) return;
            ShowDetail(_interaction.ArtifactId);
            view.BeginDrag(_findVisual?.Invoke(_interaction.ArtifactId)?.Prefab, _interaction.InstanceId, _interaction.IsPlaced);
            UpdateCandidate();
        }

        public void SelectReleased(float screenX, float screenY)
        {
            _pointer = new Vector2(screenX, screenY);
            if (!AllowsInteraction() || !_interaction.IsDragging) { CancelDrag(); return; }
            UpdateCandidate();
            bool blocked = IsButtonOrDetail(_pointer);
            bool returning = !blocked && _screen.IsReturnArea(_pointer);
            bool onBoard = !blocked && !returning && TryPointerCell(out var cell) &&
                ArtifactBoardMapper.Contains(cell, _board.Current.BoardWidth, _board.Current.BoardHeight);
            ArtifactEditRequest request = _interaction.Release(onBoard, returning);
            CancelDrag();
            if (request != null) _scheduler.TryRequestEdit(request);
        }

        public void RotateLeft() => Rotate(-1);
        public void RotateRight() => Rotate(1);
        public void SelectCanceled() => CancelDrag();

        /// <summary>우클릭으로 드래그 중이거나 포인터 아래에 있는 배치물을 회수합니다.</summary>
        public void Cancel()
        {
            if (!AllowsInteraction()) { CancelDrag(); return; }

            ArtifactEditRequest request = null;
            if (_interaction.IsDragging)
            {
                if (_interaction.IsPlaced)
                    request = ArtifactEditRequest.Return(_interaction.InstanceId);
            }
            else if (!IsButtonOrDetail(_pointer) && !_screen.IsReturnArea(_pointer) &&
                TryPointerCell(out var hit) &&
                ArtifactBoardMapper.Contains(hit, _board.Current.BoardWidth, _board.Current.BoardHeight))
            {
                foreach (ArtifactData item in _board.Current.PlacedItems)
                {
                    foreach (CellCoord cell in item.Cells)
                    {
                        if (cell != hit) continue;
                        request = ArtifactEditRequest.Return(item.InstanceId);
                        break;
                    }
                    if (request != null) break;
                }
            }

            CancelDrag();
            if (request != null) _scheduler.TryRequestEdit(request);
        }

        private void Rotate(int steps)
        {
            if (!AllowsInteraction()) { CancelDrag(); return; }
            if (!_interaction.IsDragging) return;
            _interaction.Rotate(steps);
            InvalidateDisplayedPreview();
            UpdateCandidate();
        }

        private void UpdateCandidate()
        {
            uint revision = _interaction.Revision;
            CellCoord pointerCell = default;
            bool valid = !IsButtonOrDetail(_pointer) && !_screen.IsReturnArea(_pointer) && TryPointerCell(out pointerCell);
            _interaction.SetCandidate(pointerCell, valid);
            if (_interaction.Revision != revision) InvalidateDisplayedPreview();
            if (_interaction.TryTakePreview(out var request) && _scheduler.TryRequestPreview(request))
                _previewRequest = request;
        }

        private bool TryPointerCell(out CellCoord cell)
        {
            cell = default;
            Camera camera = _root.OutputCamera;
            return camera.pixelRect.Contains(_pointer) && _mapper.TryMap(camera.ScreenPointToRay(_pointer),
                _board.Current.BoardWidth, _board.Current.BoardHeight, out cell);
        }

        // 닫기 버튼·상세·상태 문구 위는 보드 조작 대상이 아닙니다. 판정은 팝업 UI가 합니다.
        private bool IsButtonOrDetail(Vector2 point) => _screen.IsBlockingArea(point);

        private bool AllowsInteraction()
        {
            return _isOpen && _root != null && _screen != null && _mapper != null && !_faults.IsFaulted &&
                _layers.CurrentLayer.Equals(ArtifactInputLayers.Artifact) && _scheduler.CanAcceptRequests && !_scheduler.IsEditPending;
        }

        private void ShowCompletedPreview()
        {
            var latest = _scheduler.LatestPreview;
            if (latest == null || !ReferenceEquals(latest, _previewRequest) || ReferenceEquals(latest, _shownPreview)) return;
            _shownPreview = latest;
            _root.PopupView.ShowPreview(latest.Result, _interaction.Rotation, _board.Current.BoardWidth, _board.Current.BoardHeight);
            _screen.ShowStatus(latest.Result.FailureReason == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.ArtifactActionFailureReason.None
                ? string.Empty : ArtifactPopupText.ForFailure(latest.Result.FailureReason));
            _previewStatusShown = true;
        }

        private void InvalidateDisplayedPreview()
        {
            ClearPreviewStatus();
            _scheduler.InvalidatePreview();
            _previewRequest = _shownPreview = null;
            if (_root != null) _root.PopupView.ClearPreview();
        }

        private void CancelDrag()
        {
            ClearPreviewStatus();
            _interaction.Cancel();
            _scheduler?.InvalidatePreview();
            _previewRequest = _shownPreview = null;
            if (_root != null) _root.PopupView.EndDrag();
        }

        private void ClearPreviewStatus()
        {
            if (!_previewStatusShown) return;
            _previewStatusShown = false;
            _screen?.ShowStatus(_reportedEdit != null && IsFinished(_reportedEdit) ? ArtifactPopupText.ForEdit(_reportedEdit) : string.Empty);
        }

        private void ReleaseInput()
        {
            if (!_inputBound) return;
            _inputBound = false;
            _inputSlot.Unbind(this);
        }

        /// <summary>아티팩트가 소유한 입력·임시 표현·대기 요청만 정리합니다. 공용 오류 콜백 연결은 협의 후 조립 계층에서 합니다.</summary>
        internal void StopInput()
        {
            CancelDrag();
            ReleaseInput();
            _scheduler?.CancelPending();
            if (_layers != null && _layers.CurrentLayer.Equals(ArtifactInputLayers.Artifact)) _layers.ClearLayer();
        }

        private void OnCameraRendering(ScriptableRenderContext context, Camera camera) => OnCameraPreCull(camera);

        private void OnCameraPreCull(Camera camera)
        {
            // Cinemachine 등 LateUpdate 뒤의 최종 카메라 시야를 사용합니다. 출력 카메라만 처리합니다.
            if (_isOpen && !_faults.IsFaulted && _root != null && camera == _root.OutputCamera)
                _root.PopupView.UpdateBackground();
        }

        // 씬 로드가 끝난 직후 한 번: 씬 오브젝트를 찾는 유일한 자리입니다.
        private void AttachFromScene()
        {
            if (_faults.IsFaulted)
                return;

            ArtifactSceneRoot root = UnityEngine.Object.FindFirstObjectByType<ArtifactSceneRoot>();
            if (root == null)
                throw new InvalidOperationException("[ArtifactViewSystem] ArtifactSceneRoot is missing in the stage map scene.");

            AttachScene(root);
        }
    }
}
