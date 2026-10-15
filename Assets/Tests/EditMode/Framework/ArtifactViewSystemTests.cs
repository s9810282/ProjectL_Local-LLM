using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ProjectL.Tests.Framework.DataBaseTestAssets;

namespace ProjectL.Tests.Framework
{
    /// <summary>아티팩트 팝업 View 시스템의 씬 연결, 열기·닫기·Layer, 갱신 규칙, 정리 규칙, 팝업 UI 연결(UI 이전 설계)을 확인합니다.</summary>
    public sealed class ArtifactViewSystemTests
    {
        private readonly List<GameObject> _sceneObjects = new();
        private RuntimeFaultBoundary _faults;
        private LoadingRegisterSpy _loading;
        private LayerSpy _layers;
        private BoardStub _board;
        private CoreFake _core;
        private RunnerSpy _runner;
        private ArtifactEditScheduler _scheduler;
        private PresenterSpy _presenter;
        private ScreenSpy _screen;
        private UISpy _ui;
        private ArtifactViewSystem _system;

        [SetUp]
        public void SetUp()
        {
            _faults = new RuntimeFaultBoundary(_ => { });
            _loading = new LoadingRegisterSpy();
            _layers = new LayerSpy();
            _layers.ChangeLayer(StageSelectionInputLayers.Selection);
            _board = new BoardStub(Initialized());
            _core = new CoreFake();
            _runner = new RunnerSpy();
            _scheduler = new ArtifactEditScheduler(_runner, _core, _board, _core, _core, _faults);
            _presenter = new PresenterSpy();
            _screen = new ScreenSpy();
            _ui = new UISpy();
            _system = new ArtifactViewSystem(_faults);
            _system.BindIcons(_ => null);
            _system.BindServices(_loading, _layers, _ui, _board, _scheduler);
        }

        [TearDown]
        public void TearDown()
        {
            _system.Shutdown();
            foreach (GameObject sceneObject in _sceneObjects)
                if (sceneObject != null) UnityEngine.Object.DestroyImmediate(sceneObject);
            _sceneObjects.Clear();
        }

        #region 씬 연결

        [Test]
        public void Initialize_RegistersAttachAfterAndDetachBeforeLifeCycleOnStageScene()
        {
            _system.Initialize();

            Assert.That(_loading.Enter.Count, Is.EqualTo(1));
            Assert.That(_loading.Enter[0].SceneIndex, Is.EqualTo(SceneIndices.Stage));
            Assert.That(_loading.Enter[0].Node.Priority, Is.EqualTo(ArtifactViewSystem.AttachPriority).And.GreaterThan(0));
            Assert.That(_loading.Exit.Count, Is.EqualTo(1));
            Assert.That(_loading.Exit[0].SceneIndex, Is.EqualTo(SceneIndices.Stage));
            Assert.That(_loading.Exit[0].Node.Priority, Is.EqualTo(ArtifactViewSystem.DetachPriority).And.LessThan(0));
        }

        [Test]
        public void Initialize_WithoutBinding_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new ArtifactViewSystem(_faults).Initialize());
        }

        [Test]
        public void EnterNode_WithoutSceneRoot_Throws()
        {
            _system.Initialize();

            Assert.Throws<InvalidOperationException>(() => _loading.Enter[0].Node.Execute());
        }

        [Test]
        public void EnterNode_WithMissingReferences_Throws()
        {
            _system.Initialize();
            var sceneObject = new GameObject("ArtifactSceneRoot");
            _sceneObjects.Add(sceneObject);
            sceneObject.AddComponent<ArtifactSceneRoot>();

            Assert.Throws<InvalidOperationException>(() => _loading.Enter[0].Node.Execute());
            Assert.That(_system.Root, Is.Null);
        }

        [Test]
        public void EnterNode_FindsSceneRoot_ConnectsPopupUI_AndExitReleases()
        {
            _system.Initialize();
            ArtifactSceneRoot root = CreateSceneRoot();

            _loading.Enter[0].Node.Execute();
            Assert.That(_system.Root, Is.SameAs(root));

            Click(root.PopupUI.OpenButton);
            Assert.That(_ui.Opened, Is.EqualTo(new[] { UIId.Artifact }), "열기는 UISystem을 거칩니다(FW-009).");
            Assert.That(_system.IsOpen, Is.True);
            Assert.That(root.PopupUI.Panel.activeSelf, Is.True);
            Assert.That(root.PopupView.WorldRoot.gameObject.activeSelf, Is.True);

            Click(root.PopupUI.CloseButton);
            Assert.That(_system.IsOpen, Is.False);
            Assert.That(root.PopupUI.Panel.activeSelf, Is.False);
            Assert.That(_layers.CurrentLayer, Is.EqualTo(StageSelectionInputLayers.Selection));

            _loading.Exit[0].Node.Execute();
            Assert.That(_system.Root, Is.Null);
            Click(root.PopupUI.OpenButton);
            Assert.That(_system.IsOpen, Is.False, "Detach 뒤에는 팝업 UI가 시스템을 부르지 않습니다.");
            Assert.That(_ui.Opened.Count, Is.EqualTo(1));
        }

        [Test]
        public void EnterNode_WhenFaulted_DoesNotAttach()
        {
            _system.Initialize();
            CreateSceneRoot();
            _faults.Report(new InvalidOperationException("test fault"));

            _loading.Enter[0].Node.Execute();

            Assert.That(_system.Root, Is.Null);
        }

        #endregion

        #region 열기·닫기

        [Test]
        public void Open_ShowsPanel_SwitchesToArtifactLayer_AndRendersOnce()
        {
            _system.AttachPresenter(_presenter, _screen);

            _system.Open();
            _system.Tick(0f);

            Assert.That(_presenter.ShowCount, Is.EqualTo(1));
            Assert.That(_layers.CurrentLayer, Is.EqualTo(ArtifactInputLayers.Artifact));
            Assert.That(_presenter.RenderCount, Is.EqualTo(1), "스냅샷이 그대로면 Tick에서 다시 그리지 않습니다.");
            Assert.That(_presenter.LastBoard.Width, Is.EqualTo(4));
            Assert.That(_presenter.LastBoard.PlacedItems.Count, Is.EqualTo(1));
        }

        [Test]
        public void Open_WhileOpen_OrFaulted_IsIgnored()
        {
            _system.AttachPresenter(_presenter, _screen);
            _system.Open();
            _system.Open();
            Assert.That(_presenter.ShowCount, Is.EqualTo(1));

            _system.Close();
            _faults.Report(new InvalidOperationException("test fault"));
            _system.Open();

            Assert.That(_presenter.ShowCount, Is.EqualTo(1));
            Assert.That(_system.IsOpen, Is.False);
        }

        [Test]
        public void Close_ReturnsToStageSelection_KeepsPendingEdit_AndDropsPreview()
        {
            _system.AttachPresenter(_presenter, _screen);
            _system.Open();
            var edit = ArtifactEditRequest.Place(3, new CellCoord(1, 1), Rotation4.R0);
            _scheduler.TryRequestEdit(edit);

            _system.Close();

            Assert.That(_layers.CurrentLayer, Is.EqualTo(StageSelectionInputLayers.Selection));
            Assert.That(edit.Status, Is.EqualTo(ArtifactRequestStatus.Queued), "닫기는 대기 편집을 취소하지 않습니다.");
            _runner.RunAll();
            Assert.That(edit.Status, Is.EqualTo(ArtifactRequestStatus.Completed));
        }

        [Test]
        public void Close_AfterOtherScreenTookLayer_KeepsThatLayer()
        {
            _system.AttachPresenter(_presenter, _screen);
            _system.Open();
            var other = new InputLayer("Other", "Common");
            _layers.ChangeLayer(other);

            _system.Close();

            Assert.That(_layers.CurrentLayer, Is.EqualTo(other));
        }

        [Test]
        public void RepeatedClose_HidesOnce()
        {
            _system.AttachPresenter(_presenter, _screen);
            int hidesAfterAttach = _presenter.HideCount;
            _system.Open();

            _system.Close();
            _system.Close();

            Assert.That(_presenter.HideCount, Is.EqualTo(hidesAfterAttach + 1));
        }

        [Test]
        public void Host_CanOpen_OnlyWhenAttachedAndNotFaulted()
        {
            Assert.That(_system.CanOpen, Is.False);
            _system.AttachPresenter(_presenter, _screen);
            Assert.That(_system.CanOpen, Is.True);
            Assert.That(_screen.Host, Is.SameAs(_system));

            _faults.Report(new InvalidOperationException("test fault"));
            Assert.That(_system.CanOpen, Is.False);
        }

        [Test]
        public void PopupNotifications_SwitchLayerBothWays()
        {
            _system.AttachPresenter(_presenter, _screen);

            _system.PopupOpened();
            Assert.That(_layers.CurrentLayer, Is.EqualTo(ArtifactInputLayers.Artifact));
            Assert.That(_presenter.ShowCount, Is.EqualTo(1));

            _system.PopupHidden();
            Assert.That(_layers.CurrentLayer, Is.EqualTo(StageSelectionInputLayers.Selection));
        }



        [Test]
        public void DetachFromScene_WhileOpen_DisconnectsBeforeClosingUI_AndOnlyClearsLayer()
        {
            _system.AttachPresenter(_presenter, _screen);
            _system.PopupOpened();
            IArtifactPopupHost hostAtClose = _system;
            _ui.OnClose = id => hostAtClose = _screen.Host;

            _system.DetachFromScene();

            Assert.That(_ui.Closed, Is.EqualTo(new[] { UIId.Artifact }));
            Assert.That(hostAtClose, Is.Null, "닫기 전에 연결을 끊어야 숨김 알림이 맵 Layer를 켜지 않습니다.");
            Assert.That(_layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
        }

        [Test]
        public void DetachFromScene_WhileClosed_DoesNotCallUISystem()
        {
            _system.AttachPresenter(_presenter, _screen);

            _system.DetachFromScene();

            Assert.That(_ui.Closed, Is.Empty);
            Assert.That(_screen.Host, Is.Null);
        }

        [Test]
        public void StatusAndDetail_GoToPopupUI()
        {
            _board.Current = ArtifactBufferSnapshot.Empty;
            _system.AttachPresenter(_presenter, _screen);

            _system.PopupOpened();

            Assert.That(_screen.Status, Is.EqualTo(ArtifactPopupText.Waiting));
        }

        #endregion

        #region 갱신

        [Test]
        public void Uninitialized_ShowsWaiting_ThenRendersWhenBoardArrives()
        {
            _board.Current = ArtifactBufferSnapshot.Empty;
            _system.AttachPresenter(_presenter, _screen);

            _system.Open();
            Assert.That(_presenter.RenderCount, Is.Zero);
            Assert.That(_screen.Status, Is.EqualTo(ArtifactPopupText.Waiting));

            _board.Current = Initialized();
            _system.Tick(0f);

            Assert.That(_presenter.RenderCount, Is.EqualTo(1));
            Assert.That(_screen.Status, Is.Empty);
        }

        [Test]
        public void RejectedEdit_KeepsSnapshot_ButUpdatesStatus()
        {
            _system.AttachPresenter(_presenter, _screen);
            _system.Open();
            _core.NextEditResult = ArtifactActionResult.Failure(ArtifactActionFailureReason.CellOccupied);
            _scheduler.TryRequestEdit(ArtifactEditRequest.Place(3, CellCoord.Zero, Rotation4.R0));
            _runner.RunAll();

            _system.Tick(0f);

            Assert.That(_presenter.RenderCount, Is.EqualTo(1), "거절된 편집은 보드를 다시 그리지 않습니다.");
            Assert.That(_screen.Status, Is.EqualTo(ArtifactPopupText.ForFailure(ArtifactActionFailureReason.CellOccupied)));
        }

        [Test]
        public void ChangedSnapshot_RendersAgain()
        {
            _system.AttachPresenter(_presenter, _screen);
            _system.Open();

            _board.Current = Initialized();
            _system.Tick(0f);

            Assert.That(_presenter.RenderCount, Is.EqualTo(2));
        }

        [Test]
        public void EditFinishedWhileClosed_IsShownOnReopen()
        {
            _system.AttachPresenter(_presenter, _screen);
            _system.Open();
            _core.NextEditResult = ArtifactActionResult.Failure(ArtifactActionFailureReason.OutOfBounds);
            _scheduler.TryRequestEdit(ArtifactEditRequest.Place(3, new CellCoord(9, 9), Rotation4.R0));
            _system.Close();
            _runner.RunAll();

            _system.Open();

            Assert.That(_screen.Status, Is.EqualTo(ArtifactPopupText.ForFailure(ArtifactActionFailureReason.OutOfBounds)));
        }

        [Test]
        public void ClosedPopup_DoesNotRefreshOnTick()
        {
            _system.AttachPresenter(_presenter, _screen);

            _board.Current = Initialized();
            _system.Tick(0f);

            Assert.That(_presenter.RenderCount, Is.Zero);
        }

        #endregion

        #region 정리

        [Test]
        public void Detach_CancelsPendingEdit_HidesOnce_AndClearsOnlyArtifactLayer()
        {
            _system.AttachPresenter(_presenter, _screen);
            _system.Open();
            var edit = ArtifactEditRequest.Place(3, CellCoord.Zero, Rotation4.R0);
            _scheduler.TryRequestEdit(edit);
            int hides = _presenter.HideCount;

            _system.Detach();
            _system.Detach();
            _system.Shutdown();

            Assert.That(edit.Status, Is.EqualTo(ArtifactRequestStatus.Cancelled));
            Assert.That(_presenter.HideCount, Is.EqualTo(hides + 1));
            Assert.That(_layers.CurrentLayer, Is.EqualTo(default(InputLayer)), "Detach는 StageSelection을 다시 켜지 않습니다.");
        }

        [Test]
        public void Detach_AfterOtherScreenTookLayer_KeepsThatLayer()
        {
            _system.AttachPresenter(_presenter, _screen);
            _system.Open();
            var other = new InputLayer("Other", "Common");
            _layers.ChangeLayer(other);

            _system.Detach();

            Assert.That(_layers.CurrentLayer, Is.EqualTo(other));
            Assert.That(_layers.ClearCount, Is.Zero);
        }

        #endregion

        private static ArtifactBufferSnapshot Initialized() => new(new ArtifactState(4, 4,
            new[] { new ArtifactItemState(5, 1, false, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }) },
            new[] { new ArtifactItemState(3, 7, true, new CellCoord(1, 1), Rotation4.R0, new[] { new CellCoord(1, 1), new CellCoord(2, 1) }) }));

        private ArtifactSceneRoot CreateSceneRoot()
        {
            var rootObject = new GameObject("ArtifactSceneRoot", typeof(RectTransform));
            _sceneObjects.Add(rootObject);
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(rootObject.transform, false);
            var board = new GameObject("Board", typeof(RectTransform));
            board.transform.SetParent(rootObject.transform, false);
            var pool = new GameObject("Pool", typeof(RectTransform));
            pool.transform.SetParent(panel.transform, false);
            var status = new GameObject("Status", typeof(RectTransform));
            status.transform.SetParent(panel.transform, false);
            var prefabs = new GameObject("Prefabs");
            _sceneObjects.Add(prefabs);
            var cell = new GameObject("Cell", typeof(RectTransform)).AddComponent<Image>();
            cell.transform.SetParent(prefabs.transform, false);
            var item = new GameObject("Item", typeof(RectTransform)).AddComponent<Image>();
            item.transform.SetParent(prefabs.transform, false);

            var view = rootObject.AddComponent<ArtifactPopupView>();
            Set(view, "worldRoot", board.transform);
            var background = new GameObject("Background");
            background.transform.SetParent(rootObject.transform, false);
            Set(view, "backgroundRoot", background);
            view.Configure(new ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact.Item.ArtifactPopupConfigData());
            Set(view, "cellPrefab", cell.gameObject);

            var openButton = new GameObject("Open", typeof(RectTransform)).AddComponent<Button>();
            openButton.transform.SetParent(rootObject.transform, false);
            var closeButton = new GameObject("Close", typeof(RectTransform)).AddComponent<Button>();
            closeButton.transform.SetParent(panel.transform, false);
            var name = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            name.transform.SetParent(panel.transform, false);
            var description = new GameObject("Description", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            description.transform.SetParent(panel.transform, false);
            var slot = item.gameObject.AddComponent<Button>();

            rootObject.SetActive(false);
            var popupUI = rootObject.AddComponent<ArtifactPopupUI>();
            Set(popupUI, "openButton", openButton);
            Set(popupUI, "panel", panel);
            Set(popupUI, "closeButton", closeButton);
            Set(popupUI, "poolArea", (RectTransform)pool.transform);
            Set(popupUI, "slotPrefab", slot);
            Set(popupUI, "statusText", status.AddComponent<TextMeshProUGUI>());
            Set(popupUI, "artifactName", name);
            Set(popupUI, "effectDescription", description);
            Set(popupUI, "_ui", _ui);
            Set(popupUI, "_board", _board);
            rootObject.SetActive(true);
            ArtifactPopupUITests.InvokeAwake(popupUI);
            _ui.Target = popupUI;

            var root = rootObject.AddComponent<ArtifactSceneRoot>();
            Set(root, "popupView", view);
            Set(root, "popupUI", popupUI);
            var cameraObject = new GameObject("OutputCamera", typeof(Camera));
            cameraObject.transform.SetParent(rootObject.transform, false);
            Set(root, "outputCamera", cameraObject.GetComponent<Camera>());
            return root;
        }

        private static void Click(Button button)
        {
            ExecuteEvents.Execute(button.gameObject,
                new PointerEventData(null) { button = PointerEventData.InputButton.Left },
                ExecuteEvents.pointerClickHandler);
        }

        private sealed class PresenterSpy : IArtifactPopupPresenter
        {
            public int ShowCount;
            public int HideCount;
            public int RenderCount;
            public ArtifactPopupBoard LastBoard;

            public void Show() => ShowCount++;
            public void Hide() => HideCount++;
            public void Render(ArtifactPopupBoard board) { RenderCount++; LastBoard = board; }
            public void ClearBoard() { }
        }

        private sealed class ScreenSpy : IArtifactPopupScreen
        {
            public IArtifactPopupHost Host;
            public string Status = string.Empty;
            public int DetailId = -1;

            public void Connect(IArtifactPopupHost host) => Host = host;
            public void Disconnect() => Host = null;
            public void ShowStatus(string message) => Status = message ?? string.Empty;
            public void ShowDetail(int artifactId) => DetailId = artifactId;
            public bool TryPickPool(Vector2 screen, out int artifactId) { artifactId = 0; return false; }
            public bool IsReturnArea(Vector2 screen) => false;
            public bool IsBlockingArea(Vector2 screen) => false;
        }

        /// <summary>호출만 기록합니다. Target이 있으면 실제 UISystem처럼 Open/Hide를 부릅니다.</summary>
        private sealed class UISpy : IUISystemService
        {
            public IUIService Target;
            public readonly List<UIId> Opened = new();
            public readonly List<UIId> Closed = new();
            public int BackCount;
            public Action OnBack;
            public Action<UIId> OnClose;

            public void OpenUI(UIId id) { Opened.Add(id); Target?.Open(); }
            public void OpenUI(UIId id, bool isFixable) => OpenUI(id);
            public void CloseUI(UIId id) { Closed.Add(id); OnClose?.Invoke(id); Target?.Hide(); }
            public void BackCloseUI() { BackCount++; OnBack?.Invoke(); }
            public void OpenGlobalUI() { }
            public void OpenChildUI(UIId parentId, UIId childId) { }
            public void CloseChildUI(UIId parentId) { }
            public void HideAllUI() => Target?.Hide();
        }

        private sealed class LayerSpy : IInputLayerService
        {
            public InputLayer CurrentLayer { get; private set; }
            public int ClearCount;

            public void ChangeLayer(InputLayer nextLayer) => CurrentLayer = nextLayer;

            public void ClearLayer()
            {
                CurrentLayer = default;
                ClearCount++;
            }
        }

        private sealed class BoardStub : IArtifactBufferReadSink
        {
            public BoardStub(ArtifactBufferSnapshot current) => Current = current;
            public ArtifactBufferSnapshot Current { get; set; }
        }

        private sealed class RunnerSpy : IJobRunner
        {
            private readonly List<IJobService> _queue = new();
            public bool IsIdle => _queue.Count == 0;
            public void Register(IJobService job) => _queue.Add(job);
            public void RegisterRange(IReadOnlyList<IJobService> jobs) => _queue.AddRange(jobs);
            public void Clear() => throw new NotSupportedException("공용 Runner를 비우면 안 됩니다.");

            public void RunAll()
            {
                foreach (IJobService job in _queue.ToArray())
                {
                    job.Execute();
                    if (job.IsCompleted) _queue.Remove(job);
                }
            }
        }

        private sealed class CoreFake : IArtifactEditingService, IArtifactEditResultReadSink, IArtifactPreviewReadSink
        {
            public ArtifactActionResult NextEditResult = ArtifactActionResult.Success();
            public ArtifactActionResult? LastEditResult { get; private set; }
            public ArtifactPlacementPreview LastPreview { get; private set; }

            public bool InitializeBoard(int width, int height) => throw new NotSupportedException();
            public void ExpandBoard(int width, int height) => throw new NotSupportedException();
            public void Place(int artifactId, CellCoord anchor, Rotation4 rotation) => LastEditResult = NextEditResult;
            public void Move(int instanceId, CellCoord anchor, Rotation4 rotation) => LastEditResult = NextEditResult;
            public void ReturnToPool(int instanceId) => LastEditResult = NextEditResult;
            public void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
            public void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation) => throw new NotSupportedException();
        }

        private sealed class LoadingRegisterSpy : ILodingRegisterService
        {
            public readonly List<(int SceneIndex, LodingNode Node)> Enter = new();
            public readonly List<(int SceneIndex, LodingNode Node)> Exit = new();

            public void RegisterBeforeLoad(LodingNode node) => throw new NotSupportedException();
            public void RegisterSceneExitLoaded(int sceneIndex, LodingNode node) => Exit.Add((sceneIndex, node));
            public void RegisterSceneEnterLoaded(int sceneIndex, LodingNode node) => Enter.Add((sceneIndex, node));
            public void RegisterAfterLoad(LodingNode node) => throw new NotSupportedException();
        }
    }
}
