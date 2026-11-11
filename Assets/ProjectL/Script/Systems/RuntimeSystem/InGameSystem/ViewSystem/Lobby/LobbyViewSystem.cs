using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby;
using Cinemachine;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비의 Framework 연결입니다. 서비스를 받아 묶음 하나로 모으고, 씬 노드를 등록하고, 씬 연결 Scope의 게시·Tick·폐기와
    /// 씬 배경음을 맡습니다. Scope 조립은 생성 계약(<see cref="ILobbySceneFactory"/>)이 합니다.
    /// </summary>
    internal sealed class LobbyViewSystem : ISystemInitData<IHasViewQuery>, ISystemPostBindService, ISystemInitialize,
        ISystemTick, ISystemShutdown, IExportSystemServices
    {
        internal const int AttachPriority = 100;
        /// <summary>
        /// 로비 씬 이탈 때 View 연결을 떼는 순서입니다. LifeCycle의 이탈 검사(<see cref="LobbyLifeCycle.ExitPreparationPriority"/>)가
        /// 먼저 돌아야 하므로 그 값에서 구합니다(로비 리팩토링 2차 설계 M-08). 값은 -100 그대로입니다.
        /// </summary>
        internal const int DetachPriority = LobbyLifeCycle.ExitPreparationPriority + 100;

        private readonly RuntimeFaultBoundary _faults;
        private readonly ILobbySceneFactory _sceneFactory;
        private IViewQuery _view;
        private LobbyViewServices _services;
        // 로비 배경음 프리셋 번호입니다(로비 리팩토링 2차 설계 N-72).
        private const int LobbyBgmPreset = 0;
        // 씬 노드 등록은 System만 쓰므로 묶음에 넣지 않고 직접 갖습니다. 배경음 서비스는 묶음(Audio)이 원본이라 따로 들지 않습니다(N-78).
        private ILodingRegisterService _loading;
        private LobbyViewScope _scope;
        // 마지막으로 게시한 연결입니다. 뗀 연결도 다음 연결이 게시될 때까지 "현재"로 남깁니다. 팝업 UI 호출 중에 떼어
        // 미뤄 둔 입력 Layer 반환은 그 연결이 현재여야 돌기 때문입니다(로비 리팩토링 2차 R2-c Codex 검수, N-14는 그대로 둠).
        private LobbyViewScope _lastPublishedScope;
        private bool _stopped;

        internal LobbyViewSystem(RuntimeFaultBoundary faults, ILobbySceneFactory sceneFactory)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _sceneFactory = sceneFactory ?? throw new ArgumentNullException(nameof(sceneFactory));
        }

        private LobbyViewServices Services => _services
            ?? throw new InvalidOperationException("[Lobby] 서비스 연결 뒤에 써야 합니다.");

        public void InitData(IHasViewQuery data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _view = data.ViewQuery ?? throw new InvalidOperationException("[Lobby] View DB 조회가 필요합니다.");
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            IViewQuery view = _view
                ?? throw new InvalidOperationException("[Lobby] InitData 뒤에 서비스를 받아야 합니다.");
            var global = resolver.GetService(SystemGroupsService.Global);
            var input = resolver.GetService(SystemGroupsService.InGame).ViewGroup.Input;
            var cameraGroup = global.RenderServiceCommand.CoreGroup.CameraGroup;
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            var lifeCycles = resolver.GetService(SystemGroupsService.LifeCycle);
            // Group Context의 속성은 만들 때 서비스 조회(없으면 예외)로 채워져 다시 검사하지 않습니다(N-23).
            var lobbyVisual = view.LobbyVisualTableQuery;
            _services = new LobbyViewServices(
                models: lobbyVisual.Models,
                board: lobbyVisual.Board,
                motion: lobbyVisual.Motion,
                characterPresentation: lobbyVisual.CharacterPresentation,
                facilityHover: lobbyVisual.FacilityHover,
                artifactVisuals: view.ArtifactViewTableQuery,
                artifactConfig: view.ArtifactPopupConfigTableQuery.Config,
                departure: buffers.LobbyViewWriteSink,
                artifactBoard: buffers.ArtifactReadSink,
                artifactInput: buffers.ArtifactViewReadSink,
                artifactWriter: buffers.ArtifactViewWriteSink,
                artifactResults: buffers.ArtifactCommandResults,
                slot: input.Lobby,
                artifactSlot: input.Artifact,
                ui: global.UIServiceCommand,
                text: global.UITextPresentation,
                layers: global.InputServiceCommand.InputLayerServices,
                delay: global.UtilityServiceCommand.delayService,
                outputs: cameraGroup.UnityCameraCatalogSystem,
                audio: global.AudioServiceCommand,
                progress: lifeCycles.LobbyProgress);
            _loading = global.LodingServiceCommand.LodingRegisterService;
            _view = null;
        }

        public void Initialize()
        {
            ILodingRegisterService loading = _loading
                ?? throw new InvalidOperationException("[Lobby] 서비스 연결 뒤에 초기화해야 합니다.");
            loading.RegisterSceneEnterLoaded(SceneIndices.Lobby, new LodingNode(AttachPriority, AttachFromScene));
            loading.RegisterSceneExitLoaded(SceneIndices.Lobby, new LodingNode(DetachPriority, Detach));
        }

        public void Tick(float deltaTime)
        {
            // 오류 정지는 Scope.Tick의 CanWork가 봅니다(N-24).
            if (!_stopped) _scope?.Tick();
        }

        public void Shutdown()
        {
            StopInput();
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddModule<ISystemModule>(this);
        }

        internal void Detach()
        {
            LobbyViewScope scope = _scope;
            try
            {
                scope?.Dispose();
            }
            finally
            {
                // 정리 콜백이 게시한 새 연결은 이전 Detach가 지우지 않습니다.
                if (scope != null && ReferenceEquals(_scope, scope))
                {
                    _scope = null;
                    IAudioSystemService audio = Services.Audio;
                    if (audio.IsPlaying(AmbgmEvent.Lobby)) audio.Stop(AmbgmEvent.Lobby);
                }
            }
        }

        internal void StopInput()
        {
            _stopped = true;
            try { Detach(); }
            finally { _services?.Artifact.Writer.Clear(); }
        }

        private void AttachFromScene()
        {
            // 흡수한 아티팩트 진입과 같이 오류 뒤의 늦은 로딩 콜백은 연결하지 않습니다.
            if (_stopped || _faults.IsFaulted) return;
            LobbySceneRoot[] roots = UnityEngine.Object.FindObjectsByType<LobbySceneRoot>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (roots.Length != 1)
                throw new InvalidOperationException("[Lobby] 로비 씬에 LobbySceneRoot가 하나 필요합니다.");
            LobbySceneRoot root = roots[0];
            // 새 참조가 올바른지 먼저 확인하여 기존 연결을 불완전한 저작값으로 교체하지 않습니다.
            // 팝업 루트 찾기도 각 팝업 Factory가 이전 연결을 떼기 전에 합니다(로비 리팩토링 2차 설계 M-14).
            root.Validate();
            LobbyViewPopups popups = _sceneFactory.FindPopups(root, Services, _faults);
            Attach(root, popups, ReadOutputCamera());
        }

        private void Attach(LobbySceneRoot root, LobbyViewPopups popups, Camera output)
        {
            LobbyViewScope previous = _lastPublishedScope;
            Detach();
            RequireRunning();
            // 이전 폐기 중 게시됐다가 다시 해제된 새 연결도 외곽 Attach보다 우선합니다.
            if (!ReferenceEquals(_lastPublishedScope, previous)) return;
            LobbyViewScope scope = _sceneFactory.Create(root, output, Services, popups, _faults);
            previous?.Supersede();
            _scope = scope;
            _lastPublishedScope = scope;
            try
            {
                // 시작이 끝까지 가지 못한 연결(도중 폐기·교체·오류)에서는 배경음을 바꾸지 않습니다(로비 리팩토링 2차 설계 N-12).
                if (scope.Start(RequireRunning))
                {
                    IAudioSystemService audio = Services.Audio;
                    audio.SetPreset(AmbgmEvent.BGM, LobbyBgmPreset);
                    if (!audio.IsPlaying(AmbgmEvent.BGM)) audio.Play(AmbgmEvent.BGM);
                    if (!audio.IsPlaying(AmbgmEvent.Lobby)) audio.Play(AmbgmEvent.Lobby);
                }
            }
            catch
            {
                if (ReferenceEquals(_scope, scope)) Detach();
                else scope.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 출력 카메라는 Boot의 Main Camera 하나입니다(카메라 이전 설계 Q1 A, 검사 후속 정리 TP-R6 뺌).
        /// 렌즈는 Brain이 가상 카메라에서 받아 쓰므로 Brain만 확인합니다.
        /// </summary>
        private static Camera ReadOutputCamera()
        {
            Camera output = Camera.main;
            if (output == null)
                throw new InvalidOperationException("[Lobby] 화면을 그릴 출력 Camera가 없습니다. Boot의 Main Camera가 켜져 있어야 합니다.");
            if (output.GetComponent<CinemachineBrain>() == null)
                throw new InvalidOperationException("[Lobby] 출력 Camera에 CinemachineBrain이 필요합니다.");
            return output;
        }

        private void RequireRunning()
        {
            if (_stopped || _faults.IsFaulted)
                throw new InvalidOperationException("[Lobby] 정지한 런타임에는 씬을 다시 연결할 수 없습니다.");
        }
    }
}
