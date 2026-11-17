using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵의 Framework 연결입니다. 서비스를 받아 쓰는 쪽별 묶음으로 나누고(설계 §2.9), 씬 노드를 등록하고, 씬 연결 Scope의 게시·Tick·폐기와
    /// 씬 배경음을 맡습니다. Scope 조립은 생성 계약(<see cref="IStageMapSceneFactory"/>)이 합니다.
    /// </summary>
    internal sealed class StageMapViewSystem :
        ISystemInitData<IDataBaseQueryContext>,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices
    {
        internal const int AttachPriority = 100;
        internal const int DetachPriority = -100;
        /// <summary>CameraCatalogSystem의 AfterLoad 재수집(10)보다 뒤입니다. 재수집은 그 전에 빌린 핸들을 무효로 만듭니다.</summary>
        internal const int CameraHandlePriority = 100;

        private readonly RuntimeFaultBoundary _faults;
        private readonly IStageMapSceneFactory _sceneFactory;
        private IViewQuery _view;
        private StageMapViewServices _services;
        // 배경음·씬 노드 등록·입력 Buffer 비우기는 System만 하므로 묶음에서 꺼내지 않고 직접 갖습니다.
        private IAudioSystemService _audio;
        private ILodingRegisterService _loading;
        private IStageViewBufferWriteSink _requestWriter;
        private IShopViewBufferWriteSink _shopWriter;
        private StageMapViewScope _scope;
        private StageMapViewScope _lastPublishedScope;
        private bool _stopped;
        private bool _ambientAttached;

        internal StageMapViewSystem(RuntimeFaultBoundary faults, IStageMapSceneFactory sceneFactory)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _sceneFactory = sceneFactory ?? throw new ArgumentNullException(nameof(sceneFactory));
        }

        private StageMapViewServices Services => _services
            ?? throw new InvalidOperationException("[StageMapViewSystem] 서비스 연결 뒤에 써야 합니다.");

        public void InitData(IDataBaseQueryContext data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _view = data.ViewQuery
                ?? throw new InvalidOperationException("[StageMapViewSystem] View database query is required.");
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            IViewQuery view = _view
                ?? throw new InvalidOperationException("[StageMapViewSystem] InitData 뒤에 서비스를 받아야 합니다.");
            var global = resolver.GetService(SystemGroupsService.Global);
            var inGame = resolver.GetService(SystemGroupsService.InGame);
            var lifeCycles = resolver.GetService(SystemGroupsService.LifeCycle);
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            StageMapViewData mapData = view.StageMapViewTableQuery.Config;
            IAudioSystemService audio = global.AudioServiceCommand;
            IInputLayerService layers = global.InputServiceCommand.InputLayerServices;
            IUISystemService ui = global.UIServiceCommand;
            // 공용 안내도 Global Context로 받습니다(로비 출발 거절과 같은 경로).
            var notice = new StageMapViewNotice(global.UITextPresentation);
            IRunDeckReadSink runDeck = buffers.RunDeckReadSink;
            ICharacterBufferReadSink characters = buffers.CharacterReadSink;
            Func<EntityKey, UnitVisualData> unitVisuals = view.UnitVisualTableQuery.GetUnitVisual;
            // Run 덱은 맵을 그릴 때 읽습니다. 전체 Context나 나중에 비우는 _view는 캡처하지 않습니다.
            Func<GameObject> readMainModel = () =>
                MainModelPrefab.Require(MainModelPrefab.FindRunMain(runDeck, characters), unitVisuals);
            var scene = new StageMapViewSceneServices(mapData, view.StageNodeVisualTableQuery,
                view.StageMapBackgroundTableQuery, global.RenderServiceCommand.CoreGroup.ColorSystemServices,
                global.RenderServiceCommand.ViewGroup.SelectOutlineSystemServices,
                global.RenderServiceCommand.CoreGroup.CameraGroup.CameraCatalogSystem, readMainModel);
            var clearHistory = new StageNodeClearHistory(buffers.RunProgressReadSink);
            var completion = new StageMapViewCompletionServices(clearHistory,
                new StageNodeBreakEffects(global.RenderServiceCommand.CoreGroup.EffectSystemServices),
                buffers.CurrentProgressReadSink, buffers.StageProgressReadSink, buffers.RunProgressReadSink,
                global.UtilityServiceCommand.delayService, audio, mapData);
            var entry = new StageMapViewEntryServices(lifeCycles.CombatEntry, buffers.CurrentProgressReadSink,
                buffers.StageProgressReadSink, buffers.StageViewReadSink, buffers.StageViewWriteSink,
                buffers.StageCommandResults, lifeCycles.StageEntryAttempts, mapData,
                global.UtilityServiceCommand.delayService, audio, clearHistory);
            var input = new StageMapViewInputServices(mapData,
                // 호버 패널은 Global Context로 받습니다(루트에서 따로 조회하지 않음, 설계 §5.7 NH5).
                new StageNodeHoverInfo(buffers.StageProgressReadSink, view.StageOfferTextTableQuery,
                    global.UITextPresentation, _faults.Report),
                new StageNodeAvailabilityReader(inGame.CoreGroup.ChapterProgression,
                    buffers.CurrentProgressReadSink, buffers.StageProgressReadSink, lifeCycles.StageTraitGrowthFacts),
                buffers.StageViewReadSink, buffers.StageViewWriteSink, inGame.ViewGroup.Input.StageSelection, layers);
            var popups = new StageMapViewPopupServices(view.UnitVisualTableQuery, view.ConsumableItemVisualTableQuery,
                view.RecoveryOptionVisualTableQuery, view.StageOfferTextTableQuery, view.TraitVisualTableQuery,
                buffers.CurrentProgressReadSink, buffers.StageProgressReadSink, buffers.CharacterReadSink,
                buffers.CurrencyReadSink, buffers.ShopViewReadSink, buffers.ShopViewWriteSink, buffers.StageCommandResults,
                inGame.CoreGroup.RecoveryState, inGame.ViewGroup.RecoveryStatus, inGame.ViewGroup.RecoveryRequests,
                inGame.CoreGroup.Deck, buffers.TraitGrowthStateReadSink, buffers.TraitGrowthResultReadSink,
                buffers.TraitGrowthViewWriteSink, inGame.ViewGroup.TraitGrowthRequests,
                lifeCycles.StageTraitGrowthFacts, ui, notice, audio);
            _services = new StageMapViewServices(scene, entry, input, popups, mapData,
                buffers.CurrentProgressReadSink, buffers.StageProgressReadSink, buffers.StageViewReadSink,
                buffers.RunProgressReadSink, inGame.CoreGroup.RecoveryState, ui, layers,
                global.LodingServiceCommand.LodingService, notice,
                new StageMapIntroHistory(buffers.RunProgressReadSink),
                new StageNodeDropEffects(global.RenderServiceCommand.CoreGroup.EffectSystemServices), audio, completion);
            _audio = audio ?? throw new InvalidOperationException("[StageMapViewSystem] 오디오 서비스가 필요합니다.");
            // 맵·상점 입력 Buffer 비우기의 주인은 System 하나입니다(평가 N-06). 연결이 바뀌어도 같은 쓰기 창구입니다.
            _requestWriter = buffers.StageViewWriteSink
                ?? throw new InvalidOperationException("[StageMapViewSystem] 맵 입력 쓰기 창구가 필요합니다.");
            _shopWriter = buffers.ShopViewWriteSink
                ?? throw new InvalidOperationException("[StageMapViewSystem] 상점 입력 쓰기 창구가 필요합니다.");
            _loading = global.LodingServiceCommand.LodingRegisterService
                ?? throw new InvalidOperationException("[StageMapViewSystem] 로딩 등록 서비스가 필요합니다.");
            _view = null;
        }

        public void Initialize()
        {
            ILodingRegisterService loading = _loading
                ?? throw new InvalidOperationException("[StageMapViewSystem] 서비스 연결 뒤에 초기화해야 합니다.");
            loading.RegisterSceneEnterLoaded(SceneIndices.Stage, new LodingNode(AttachPriority, AttachFromScene));
            loading.RegisterSceneExitLoaded(SceneIndices.Stage, new LodingNode(DetachPriority, Detach));
            // AfterLoad는 씬 번호가 없으므로 모든 씬에서 불리며, Stage 연결이 없으면 아무것도 하지 않습니다.
            loading.RegisterAfterLoad(new LodingNode(CameraHandlePriority, BindCameraHandle));
            loading.RegisterScenePreparation(SceneIndices.Stage, new LodingPreparationNode(GetScenePreparation));
        }

        private ILodingPreparation GetScenePreparation()
        {
            RequireRunning();
            StageMapViewScope scope = _scope;
            if (scope == null || !scope.CanWork)
                throw new InvalidOperationException("[StageMapViewSystem] 로딩 준비에 유효한 맵 연결이 필요합니다.");
            return _sceneFactory.CreatePreparation(scope, _faults);
        }

        public void Tick(float deltaTime)
        {
            if (!_stopped && !_faults.IsFaulted) _scope?.Tick();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        internal bool CanUseDevelopmentTools => !_stopped && _scope?.CanUseDevelopmentTools == true;
#endif

        internal void RequestMapRedraw() => _scope?.RequestMapRedraw();

        public void Shutdown()
        {
            _stopped = true;
            Detach(true);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddModule<ISystemModule>(this);
        }

        /// <summary>정상 이탈은 AMB를 먼저 정지하고 자기 Scope와 입력을 정리합니다.</summary>
        internal void Detach() => Detach(false);

        /// <summary>오류 정지는 표시까지 폐기하며 AMB 종료 의무는 정상 이탈까지 보존합니다.</summary>
        internal void StopInput()
        {
            _stopped = true;
            StageMapViewScope scope = _scope;
            List<Exception> errors = null;
            CleanupErrors.Try(() => scope?.Dispose(true), ref errors);
            if (ReferenceEquals(_scope, scope)) _scope = null;
            CleanupErrors.Try(() => _requestWriter?.Clear(), ref errors);
            CleanupErrors.Try(() => _shopWriter?.Clear(), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 입력 정리 중 실패했습니다.");
        }

        private void AttachFromScene()
        {
            RequireRunning();
            StageMapSceneRoot root = UnityEngine.Object.FindFirstObjectByType<StageMapSceneRoot>();
            if (root == null)
                throw new InvalidOperationException("[StageMapViewSystem] StageMapSceneRoot is missing in the stage map scene.");
            // 출력 카메라는 Boot의 Main Camera 하나입니다(카메라 이전 설계 Q1 A, 검사 후속 정리 TP-R6 뺌).
            Camera output = Camera.main;
            if (output == null)
                throw new InvalidOperationException("[StageMapViewSystem] A camera tagged MainCamera is required in the stage map scene.");
            ValidateRoot(root);
            Attach(root, output);
        }

        private void Attach(StageMapSceneRoot root, Camera output)
        {
            StageMapViewScope previous = _lastPublishedScope;
            Detach();
            RequireRunning();
            if (!ReferenceEquals(_lastPublishedScope, previous)) return;
            StageMapViewScope scope = _sceneFactory.Create(root, output, Services, _faults);
            previous?.Supersede();
            _scope = scope;
            _lastPublishedScope = scope;
            try
            {
                StartAmbient(scope);
                if (scope.CanWork) scope.Start(RequireRunning);
            }
            catch
            {
                if (ReferenceEquals(_scope, scope)) Detach();
                else scope.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 씬 배경음을 맞춥니다. 배경음은 Scope보다 오래 남는 씬 수명이라 System이 갖습니다 —
        /// 오류 정지(<see cref="StopInput"/>)로 Scope가 없어져도 정상 이탈 때 AMB를 멈춰야 합니다.
        /// </summary>
        private void StartAmbient(StageMapViewScope scope)
        {
            IAudioSystemService audio = _audio;
            _ambientAttached = true;
            audio.SetPreset(AmbgmEvent.BGM, 1);
            if (!scope.CanWork) return;
            bool bgmPlaying = audio.IsPlaying(AmbgmEvent.BGM);
            if (!scope.CanWork) return;
            if (!bgmPlaying) audio.Play(AmbgmEvent.BGM);
            if (!scope.CanWork) return;
            bool ambPlaying = audio.IsPlaying(AmbgmEvent.AMB);
            if (!scope.CanWork) return;
            if (!ambPlaying) audio.Play(AmbgmEvent.AMB);
        }

        /// <summary>Catalog의 마지막 씬 수집 뒤 현재 연결의 맵 카메라 핸들을 빌립니다. 실패하면 연결 실패와 같이 정리합니다.</summary>
        private void BindCameraHandle()
        {
            StageMapViewScope scope = _scope;
            if (_stopped || _faults.IsFaulted || scope == null) return;
            try
            {
                scope.BindCameraHandle();
            }
            catch
            {
                if (ReferenceEquals(_scope, scope)) Detach();
                throw;
            }
        }

        private void Detach(bool suppressMovementLoss)
        {
            StageMapViewScope scope = _scope;
            StageMapViewScope owner = _lastPublishedScope;
            if (!suppressMovementLoss && scope?.IsNoncombatMoving == true) _stopped = true;
            List<Exception> errors = null;
            CleanupErrors.Try(StopAmbient, ref errors);
            CleanupErrors.Try(() => scope?.Dispose(suppressMovementLoss), ref errors);
            if (ReferenceEquals(_scope, scope)) _scope = null;
            // 맵·상점 입력 Buffer 비우기의 주인은 여기 한 곳입니다(평가 N-06). Scope·상점 컨트롤러는 새 제출만 막습니다.
            // 정리 콜백이 다음 연결을 게시했으면 그 연결의 입력은 지우지 않습니다. Scope가 없거나 정리가 실패해도 발급·처리 번호는 유지하며 비웁니다.
            CleanupErrors.Try(() => { if (ReferenceEquals(_lastPublishedScope, owner)) _requestWriter?.Clear(); }, ref errors);
            CleanupErrors.Try(() => { if (ReferenceEquals(_lastPublishedScope, owner)) _shopWriter?.Clear(); }, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[StageMap] 씬 정리 중 실패했습니다.");
        }

        private void StopAmbient()
        {
            if (!_ambientAttached) return;
            // Stop 콜백이 만든 새 연결의 종료 의무를 외곽 정리가 지우지 않습니다.
            _ambientAttached = false;
            _audio.Stop(AmbgmEvent.AMB);
        }

        private static void ValidateRoot(StageMapSceneRoot root)
        {
            if (root.MapRoot == null)
                throw new InvalidOperationException("[StageMapViewSystem] StageMapSceneRoot requires a map root.");
            if (root.BackgroundRoot == null)
                throw new InvalidOperationException("[StageMapViewSystem] StageMapSceneRoot.backgroundRoot 연결이 필요합니다.");
            if (root.BackgroundRoot == root.MapRoot || root.BackgroundRoot.parent != root.MapRoot.parent)
                throw new InvalidOperationException("[StageMapViewSystem] BackgroundRoot는 MapRoot와 서로 다른 형제 Transform이어야 합니다.");
            if (root.UI == null || root.UI.gameObject.scene != root.gameObject.scene)
                throw new InvalidOperationException(
                    "[StageMapViewSystem] StageMapSceneRoot.ui에 같은 씬의 Canvas 구성 진입점이 필요합니다.");
            root.UI.ValidateComposition();
            if (root.MapCamera == null)
                throw new InvalidOperationException("[StageMapViewSystem] 종류 아이콘 방향을 위한 MapCamera가 필요합니다.");
        }

        private void RequireRunning()
        {
            if (_stopped || _faults.IsFaulted)
                throw new InvalidOperationException("[StageMapViewSystem] 정지한 런타임에는 씬을 다시 연결할 수 없습니다.");
        }
    }
}
