using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.ServiceKeys;
using UnityEditor;
using UnityEngine;
using static ProjectL.Tests.Framework.DataBaseTestAssets;

namespace ProjectL.Tests.Framework
{
    /// <summary>스테이지 맵 View 시스템의 씬 연결, 데이터 대기, 한 번 표시, 정리 순서를 확인합니다.</summary>
    public sealed class StageMapViewSystemTests
    {
        private readonly DataBaseTestAssets _assets = new();
        private readonly List<GameObject> _sceneObjects = new();
        private StageMapViewSystem _system;
        private LoadingRegisterSpy _loading;
        private ReceiverSlotSpy _slot;
        private LayerSpy _layers;
        private readonly NoNodePicker _picker = new();
        private IBufferQuery _buffers;

        [SetUp]
        public void SetUp()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            _buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            _loading = new LoadingRegisterSpy();
            _system = new StageMapViewSystem();
            _system.BindData(MapData(), new EmptyVisualTable());
            _system.BindServices(_loading, _buffers.CurrentProgressReadSink, _buffers.StageProgressReadSink);
            _slot = new ReceiverSlotSpy();
            _layers = new LayerSpy();
            _system.BindInput(_slot, _layers, _buffers.StageViewReadSink, _buffers.StageViewWriteSink, new SilentFeedback());
        }

        [TearDown]
        public void TearDown()
        {
            // EditMode에서는 생성한 계층을 먼저 즉시 제거한 뒤 시스템이 남은 참조를 놓게 합니다.
            foreach (GameObject sceneObject in _sceneObjects)
                if (sceneObject != null) UnityEngine.Object.DestroyImmediate(sceneObject);
            _sceneObjects.Clear();
            _system.Shutdown();
            _assets.DestroyAll();
        }

        [Test]
        public void Initialize_RegistersAttachAfterAndDetachBeforeLifeCycleOnStageScene()
        {
            _system.Initialize();

            Assert.That(_loading.Enter.Count, Is.EqualTo(1));
            Assert.That(_loading.Enter[0].SceneIndex, Is.EqualTo(SceneIndices.Stage));
            Assert.That(_loading.Enter[0].Node.Priority, Is.EqualTo(StageMapViewSystem.AttachPriority).And.GreaterThan(0));
            Assert.That(_loading.Exit.Count, Is.EqualTo(1));
            Assert.That(_loading.Exit[0].SceneIndex, Is.EqualTo(SceneIndices.Stage));
            Assert.That(_loading.Exit[0].Node.Priority, Is.EqualTo(StageMapViewSystem.DetachPriority).And.LessThan(0));
        }

        [Test]
        public void Initialize_WithoutBinding_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => new StageMapViewSystem().Initialize());
        }

        [Test]
        public void Tick_BeforeAttach_DoesNotShow()
        {
            Transform mapRoot = CreateSceneRoot().MapRoot;
            RegisterMap();

            _system.Tick(0f);

            Assert.That(mapRoot.childCount, Is.Zero);
        }

        [Test]
        public void Tick_WaitsForMap_ThenShowsOnlyOnce()
        {
            StageMapSceneRoot root = CreateSceneRoot();
            _system.Attach(root, _picker);

            _system.Tick(0f);
            Assert.That(root.MapRoot.childCount, Is.Zero, "맵이 준비되기 전에는 대기합니다.");

            RegisterMap();
            _system.Tick(0f);
            Assert.That(root.MapRoot.childCount, Is.EqualTo(3), "노드 2개와 연결선 1개를 표시합니다.");

            _system.Tick(0f);
            Assert.That(root.MapRoot.childCount, Is.EqualTo(3), "표시한 뒤에는 다시 만들지 않습니다.");
        }

        [Test]
        public void Detach_ReleasesRoot_AndNextAttachShowsAgain()
        {
            RegisterMap();
            StageMapSceneRoot first = CreateSceneRoot();
            _system.Attach(first, _picker);
            _system.Tick(0f);
            Assert.That(first.MapRoot.childCount, Is.EqualTo(3));

            // 실제 씬 이탈처럼 이전 씬 오브젝트가 사라진 뒤 정리합니다.
            UnityEngine.Object.DestroyImmediate(first.gameObject);
            _system.Detach();
            _system.Tick(0f);

            StageMapSceneRoot second = CreateSceneRoot();
            Assert.That(second.MapRoot.childCount, Is.Zero, "Detach 뒤에는 연결 전까지 표시하지 않습니다.");
            _system.Attach(second, _picker);
            _system.Tick(0f);
            Assert.That(second.MapRoot.childCount, Is.EqualTo(3));
        }

        [Test]
        public void EnterNode_FindsSceneRootOnce_AndExitNodeDetaches()
        {
            _system.Initialize();
            StageMapSceneRoot root = CreateSceneRoot();
            CreateMainCamera();
            RegisterMap();

            _loading.Enter[0].Node.Execute();
            _system.Tick(0f);
            Assert.That(root.MapRoot.childCount, Is.EqualTo(3));

            // EditMode에서는 Destroy를 쓸 수 없으므로 표시한 오브젝트를 먼저 즉시 제거합니다.
            for (int i = root.MapRoot.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.MapRoot.GetChild(i).gameObject);
            _loading.Exit[0].Node.Execute();
            _system.Tick(0f);

            Assert.That(root.MapRoot.childCount, Is.Zero, "Exit 뒤에는 같은 Root에 다시 표시하지 않습니다.");
        }

        [Test]
        public void EnterNode_WithoutSceneRoot_Throws()
        {
            _system.Initialize();

            Assert.Throws<InvalidOperationException>(() => _loading.Enter[0].Node.Execute());
        }

        [Test]
        public void Attach_WithoutMapRoot_Throws()
        {
            var sceneObject = new GameObject("StageMapSceneRoot");
            _sceneObjects.Add(sceneObject);
            var root = sceneObject.AddComponent<StageMapSceneRoot>();

            Assert.Throws<InvalidOperationException>(() => _system.Attach(root, _picker));
        }

        [Test]
        public void Shutdown_IsSafeToCallTwice()
        {
            _system.Attach(CreateSceneRoot(), _picker);

            Assert.DoesNotThrow(() =>
            {
                _system.Shutdown();
                _system.Shutdown();
            });
        }

        [Test]
        public void Attach_BindsInputAndChangesToSelectionLayer()
        {
            _system.Attach(CreateSceneRoot(), _picker);

            Assert.That(_slot.Bound, Is.Not.Null);
            Assert.That(_slot.BindCount, Is.EqualTo(1));
            Assert.That(_layers.CurrentLayer, Is.EqualTo(StageSelectionInputLayers.Selection));
        }

        [Test]
        public void Detach_UnbindsAndClearsSelectionLayer()
        {
            _system.Attach(CreateSceneRoot(), _picker);
            IStageSelectionInputReceiver bound = _slot.Bound;

            _system.Detach();
            _system.Detach();

            Assert.That(_slot.Bound, Is.Null);
            Assert.That(_slot.Unbound, Is.SameAs(bound));
            Assert.That(_slot.UnbindCount, Is.EqualTo(1), "두 번째 Detach는 입력을 다시 건드리지 않습니다.");
            Assert.That(_layers.ClearCount, Is.EqualTo(1));
            Assert.That(_layers.CurrentLayer, Is.EqualTo(default(InputLayer)));
        }

        [Test]
        public void Detach_KeepsLayerChangedByPopup()
        {
            _system.Attach(CreateSceneRoot(), _picker);
            var popupLayer = new InputLayer("Artifact", "Common", "Artifact");
            _layers.ChangeLayer(popupLayer);

            _system.Detach();

            Assert.That(_slot.Bound, Is.Null);
            Assert.That(_layers.ClearCount, Is.Zero);
            Assert.That(_layers.CurrentLayer, Is.EqualTo(popupLayer));
        }

        [Test]
        public void EnterNode_WithoutMainCamera_Throws()
        {
            _system.Initialize();
            CreateSceneRoot();
            Assume.That(Camera.main, Is.Null, "테스트 씬에 MainCamera가 없어야 합니다.");

            Assert.Throws<InvalidOperationException>(() => _loading.Enter[0].Node.Execute());
            Assert.That(_slot.Bound, Is.Null);
        }

        private void CreateMainCamera()
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            _sceneObjects.Add(cameraObject);
            cameraObject.AddComponent<Camera>();
        }

        private void RegisterMap()
        {
            _buffers.CurrentProgressWriteSink.SelectMap(1);
            _buffers.StageProgressWriteSink.RegisterStages(1, new[]
            {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 100, 1, 0, Array.Empty<int>(), false, StageType.Combat, StageMapRole.Regular)
            });
        }

        private StageMapSceneRoot CreateSceneRoot()
        {
            var sceneObject = new GameObject("StageMapSceneRoot");
            _sceneObjects.Add(sceneObject);
            var mapRoot = new GameObject("MapRoot");
            mapRoot.transform.SetParent(sceneObject.transform, false);
            var root = sceneObject.AddComponent<StageMapSceneRoot>();
            Set(root, "mapRoot", mapRoot.transform);
            return root;
        }

        private StageMapViewData MapData()
        {
            var data = new StageMapViewData();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/ProjectL/Prefabs/StageSelect/Infrastructure/StageMapNode.prefab");
            Assert.That(prefab, Is.Not.Null);
            Set(data, "nodePrefab", prefab);
            var connection = _assets.CreateGameObject("Connection");
            connection.AddComponent<LineRenderer>();
            Set(data, "connectionPrefab", connection);
            return data;
        }

        private sealed class EmptyVisualTable : IStageNodeVisualTableQuery
        {
            public StageNodeVisualData GetNodeVisual(StageType stageType, StageMapRole mapRole) =>
                throw new KeyNotFoundException();

            public bool TryGetNodeVisual(StageType stageType, StageMapRole mapRole, out StageNodeVisualData data)
            {
                data = null;
                return false;
            }

            public IReadOnlyCollection<StageNodeVisualData> GetAllNodeVisuals() => Array.Empty<StageNodeVisualData>();
        }

        private sealed class NoNodePicker : IStageNodePicker
        {
            public bool TryPickNode(float screenX, float screenY, out int nodeId)
            {
                nodeId = 0;
                return false;
            }
        }

        private sealed class SilentFeedback : IStageMapInteractionFeedback
        {
            public void HoverChanged(int previousNodeId, int currentNodeId) { }
            public void EntryRequested(int nodeId, int requestNumber) { }
        }

        private sealed class ReceiverSlotSpy : IStageSelectionReceiverSlot
        {
            public IStageSelectionInputReceiver Bound;
            public IStageSelectionInputReceiver Unbound;
            public int BindCount;
            public int UnbindCount;

            public void Bind(IStageSelectionInputReceiver receiver)
            {
                if (Bound != null) throw new InvalidOperationException("Already bound.");
                Bound = receiver;
                BindCount++;
            }

            public void Unbind(IStageSelectionInputReceiver receiver)
            {
                if (!ReferenceEquals(Bound, receiver)) return;
                Bound = null;
                Unbound = receiver;
                UnbindCount++;
            }
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
