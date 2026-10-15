using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 씬에 붙었다 떨어지는 View 시스템입니다. 런타임에 한 번 등록되고,
    /// 씬이 로드될 때마다 SceneRoot를 한 번 찾아 연결한 뒤 Core가 준비한 맵을 한 번 표시합니다.
    /// </summary>
    /// <remarks>
    /// 맵 데이터 준비(Flow·Job)는 이 시스템의 책임이 아닙니다. 데이터가 없으면 연결된 동안 매 Tick 다시 확인합니다.
    /// 씬에 연결된 동안 맵 선택 입력을 받습니다. 입력 해석은 <see cref="StageMapInteraction"/>이 맡고,
    /// 수신 슬롯과 Layer를 관리하고 StageMapEntryFlow에 기록된 요청의 실행 연결을 맡깁니다.
    /// </remarks>
    internal sealed class StageMapViewSystem :
        ISystemInitData<IDataBaseQueryContext>,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices
    {
        // LifeCycle의 Enter(0) 뒤에 연결하고, LifeCycle의 Exit(0) 전에 정리합니다.
        internal const int AttachPriority = 100;
        internal const int DetachPriority = -100;

        private readonly StageMapViewDisplay _display = new();
        private StageMapViewData _mapData;
        private Func<StageProgressData, GameObject> _findVisualPrefab;
        private ILodingRegisterService _loadingRegister;
        private StageMapViewReader _reader;
        private IStageSelectionReceiverSlot _slot;
        private IInputLayerService _layers;
        private IStageViewBufferReadSink _requests;
        private IStageViewBufferWriteSink _requestWriter;
        private IStageMapInteractionFeedback _feedback;
        private StageMapSceneRoot _root;
        private StageMapInteraction _interaction;
        private bool _isShown;
        private StageMapEntryFlow _entryFlow;
        private Func<bool> _canSubmitEntry;
        private readonly RuntimeFaultBoundary _faults;

        internal StageMapViewSystem(RuntimeFaultBoundary faults = null)
        {
            _faults = faults;
        }

        public void InitData(IDataBaseQueryContext data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            IViewQuery view = data.ViewQuery
                ?? throw new InvalidOperationException("[StageMapViewSystem] View database query is required.");
            BindData(view.StageMapViewTableQuery.Config, view.StageNodeVisualTableQuery);
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            var global = resolver.GetService(SystemGroupsService.Global);
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            BindServices(global.LodingServiceCommand.LodingRegisterService,
                buffers.CurrentProgressReadSink, buffers.StageProgressReadSink);
            BindInput(resolver.GetService(StageSelectionInputKey.ReceiverSlot),
                global.InputServiceCommand.InputLayerServices,
                buffers.StageViewReadSink, buffers.StageViewWriteSink, new StageMapInteractionLog());
            _entryFlow = new StageMapEntryFlow(_reader, _requests, _requestWriter,
                resolver.GetService(SystemGroupsService.LifeCycle).CombatEntry,
                message => Debug.Log($"[StageMap] {message}"));
            _canSubmitEntry = () => global.JobServiceCommand.JobRunner.IsIdle &&
                !global.LodingServiceCommand.LodingService.IsLoading &&
                _layers.CurrentLayer.Equals(StageSelectionInputLayers.Selection);
        }

        public void Initialize()
        {
            if (_mapData == null || _loadingRegister == null || _slot == null)
                throw new InvalidOperationException(
                    "[StageMapViewSystem] InitData and PostBindService must complete before initialization.");

            _loadingRegister.RegisterSceneEnterLoaded(SceneIndices.Stage, new LodingNode(AttachPriority, AttachFromScene));
            _loadingRegister.RegisterSceneExitLoaded(SceneIndices.Stage, new LodingNode(DetachPriority, Detach));
        }

        public void Tick(float deltaTime)
        {
            if (_root == null || (_faults != null && _faults.IsFaulted))
                return;

            _entryFlow?.Tick(_canSubmitEntry != null && _canSubmitEntry());
            if (_isShown) return;

            if (!_reader.TryRead(out _, out IReadOnlyList<StageProgressData> nodes))
                return;

            // 실패하면 Display가 만든 것을 정리한 뒤 예외를 전파합니다. 복구는 런타임 오류 경계가 담당합니다.
            _display.Show(nodes, _mapData, _findVisualPrefab, _root.MapRoot);
            _isShown = true;
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

        /// <summary>DB에서 받은 설정을 보관합니다. 외형 조회 함수는 여기서 한 번만 만듭니다.</summary>
        internal void BindData(StageMapViewData mapData, IStageNodeVisualTableQuery visuals)
        {
            if (visuals == null)
                throw new ArgumentNullException(nameof(visuals));

            _mapData = mapData ?? throw new ArgumentNullException(nameof(mapData));
            _findVisualPrefab = node => visuals.TryGetNodeVisual(node.StageType, node.MapRole,
                out StageNodeVisualData visual) ? visual.VisualPrefab : null;
        }

        /// <summary>필요한 서비스만 보관합니다. Buffer는 읽기 권한만 받습니다.</summary>
        internal void BindServices(ILodingRegisterService loadingRegister,
            ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages)
        {
            _loadingRegister = loadingRegister ?? throw new ArgumentNullException(nameof(loadingRegister));
            _reader = new StageMapViewReader(current, stages);
        }

        /// <summary>맵 선택 입력에 필요한 서비스만 보관합니다. StageView Buffer는 View가 쓰는 요청 Buffer입니다.</summary>
        internal void BindInput(IStageSelectionReceiverSlot slot, IInputLayerService layers,
            IStageViewBufferReadSink requests, IStageViewBufferWriteSink requestWriter,
            IStageMapInteractionFeedback feedback)
        {
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            _layers = layers ?? throw new ArgumentNullException(nameof(layers));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _requestWriter = requestWriter ?? throw new ArgumentNullException(nameof(requestWriter));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
        }

        /// <summary>
        /// 씬 오브젝트와 입력을 연결합니다. 이전 연결이 남아 있으면 먼저 정리합니다.
        /// 입력 해석기를 수신 슬롯에 붙이고 StageSelection Layer로 바꿉니다.
        /// </summary>
        internal void Attach(StageMapSceneRoot root, IStageNodePicker picker)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (picker == null)
                throw new ArgumentNullException(nameof(picker));
            if (root.MapRoot == null)
                throw new InvalidOperationException("[StageMapViewSystem] StageMapSceneRoot requires a map root.");
            if (_slot == null)
                throw new InvalidOperationException("[StageMapViewSystem] Input services must be bound before attaching.");

            Detach();
            _root = root;
            _interaction = new StageMapInteraction(picker, _requests, _requestWriter, _feedback);
            _slot.Bind(_interaction);
            _layers.ChangeLayer(StageSelectionInputLayers.Selection);
        }

        /// <summary>입력을 떼고 표시를 정리한 뒤 씬 참조를 놓습니다. 여러 번 호출해도 안전합니다.</summary>
        internal void Detach()
        {
            _entryFlow?.Detach();
            if (_interaction != null)
            {
                StageMapInteraction interaction = _interaction;
                _interaction = null;
                _slot.Unbind(interaction);

                // 팝업 등이 이미 다른 Layer로 바꿨다면 그 Layer는 건드리지 않습니다.
                if (_layers.CurrentLayer.Equals(StageSelectionInputLayers.Selection))
                    _layers.ClearLayer();
            }

            _display.Clear();
            _root = null;
            _isShown = false;
        }

        // 씬 로드가 끝난 직후 한 번: 씬 오브젝트와 카메라를 찾는 유일한 자리입니다.
        private void AttachFromScene()
        {
            StageMapSceneRoot root = UnityEngine.Object.FindFirstObjectByType<StageMapSceneRoot>();
            if (root == null)
                throw new InvalidOperationException("[StageMapViewSystem] StageMapSceneRoot is missing in the stage map scene.");

            // 카메라 제공 방식이 정해지기 전까지 MainCamera 태그의 카메라를 씁니다(계획 §8.1 D5).
            Camera camera = Camera.main;
            if (camera == null)
                throw new InvalidOperationException("[StageMapViewSystem] A camera tagged MainCamera is required in the stage map scene.");

            Attach(root, new StageNodePicker(camera, _display, _mapData.NodeMask, _mapData.PickDistance));
        }
    }
}
