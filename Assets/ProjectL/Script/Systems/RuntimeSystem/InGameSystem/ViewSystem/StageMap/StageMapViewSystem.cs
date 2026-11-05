using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵의 Framework 연결입니다. 서비스를 받아 묶음 하나로 모으고, 씬 노드를 등록하고, 씬 연결 Scope의 게시·Tick·폐기와
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
        // 배경음과 씬 노드 등록은 System만 쓰므로 묶음에 넣지 않고 직접 갖습니다.
        private IAudioSystemService _audio;
        private ILodingRegisterService _loading;
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
            _services = new StageMapViewServices(
                mapData: view.StageMapViewTableQuery.Config,
                nodeVisuals: view.StageNodeVisualTableQuery,
                backgrounds: view.StageMapBackgroundTableQuery,
                unitVisuals: view.UnitVisualTableQuery,
                itemVisuals: view.ConsumableItemVisualTableQuery,
                recoveryVisuals: view.RecoveryOptionVisualTableQuery,
                offerTexts: view.StageOfferTextTableQuery,
                current: buffers.CurrentProgressReadSink,
                stages: buffers.StageProgressReadSink,
                characters: buffers.CharacterReadSink,
                currency: buffers.CurrencyReadSink,
                shopInput: buffers.ShopViewReadSink,
                shopWriter: buffers.ShopViewWriteSink,
                requests: buffers.StageViewReadSink,
                requestWriter: buffers.StageViewWriteSink,
                results: buffers.StageCommandResults,
                attempts: lifeCycles.StageEntryAttempts,
                presentationOwner: lifeCycles.CombatEntry,
                recoveryState: inGame.CoreGroup.RecoveryState,
                recoveryStatus: inGame.ViewGroup.RecoveryStatus,
                recoveryRequests: inGame.ViewGroup.RecoveryRequests,
                deck: inGame.CoreGroup.Deck,
                slot: inGame.ViewGroup.Input.StageSelection,
                // 호버 패널은 Global Context로 받습니다(루트에서 따로 조회하지 않음, 설계 §5.7 NH5).
                feedback: new StageNodeHoverInfo(buffers.StageProgressReadSink, view.StageOfferTextTableQuery,
                    global.UITextPresentation, _faults.Report),
                ui: global.UIServiceCommand,
                layers: global.InputServiceCommand.InputLayerServices,
                delay: global.UtilityServiceCommand.delayService,
                cameras: global.RenderServiceCommand.CoreGroup.CameraGroup.CameraCatalogSystem,
                traitVisuals: view.TraitVisualTableQuery,
                growthOffers: buffers.TraitGrowthStateReadSink,
                growthResults: buffers.TraitGrowthResultReadSink,
                growthInputs: buffers.TraitGrowthViewReadSink,
                growthWriter: buffers.TraitGrowthViewWriteSink,
                growthRequests: inGame.ViewGroup.TraitGrowthRequests,
                growthFacts: lifeCycles.StageTraitGrowthFacts,
                colors: global.RenderServiceCommand.CoreGroup.ColorSystemServices,
                audio: global.AudioServiceCommand);
            _audio = _services.Audio;
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
            CleanupErrors.Try(() => _services?.RequestWriter.Clear(), ref errors);
            CleanupErrors.Try(() => _services?.ShopWriter.Clear(), ref errors);
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
            // Scope가 없거나 정리가 실패해도 자기 입력의 발급·처리 번호는 유지하며 비웁니다.
            CleanupErrors.Try(() => { if (ReferenceEquals(_lastPublishedScope, owner)) _services?.RequestWriter.Clear(); }, ref errors);
            CleanupErrors.Try(() => { if (ReferenceEquals(_lastPublishedScope, owner)) _services?.ShopWriter.Clear(); }, ref errors);
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
