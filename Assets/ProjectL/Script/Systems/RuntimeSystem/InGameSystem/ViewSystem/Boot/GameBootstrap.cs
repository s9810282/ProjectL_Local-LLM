using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.Runtime;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev;
#endif
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.ArtifactPreparation;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Boot;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot
{
    /// <summary>
    /// Boot 씬의 런타임 구동자입니다(설계 W1 §10). DB 생성 → 런타임 조립 → 수명 단계 → 시작 구간 전환 →
    /// 매 프레임 Tick → 종료를 한 곳에서 맡고, 런타임당 하나인 오류 경계를 만들어 소유합니다.
    /// </summary>
    /// <remarks>
    /// Framework 가이드가 "연동 담당 코드"에 맡긴 구동 역할(FW-016, DR-001)을 채우는 구현입니다. 역할의 전체 목록은 설계 §10.0입니다.
    /// 맡는 것: 런타임 생성·소유, 수명 단계 1회 호출, 시작 구간 전환 1회, 매 프레임 Tick, 오류 경계 소유, 종료 1회.
    /// 맡지 않는 것: 규칙·상태(Core), Job 등록 판단(CombatPlan), 화면·입력(View), 구간 사이 전환(전투 Plan의 이탈 연쇄), 서비스 찾기 창구.
    /// 첫 오류가 보고되면 다음 Tick·전환을 호출하지 않습니다. 복구는 새 런타임(재생 재시작)으로만 합니다.
    /// 개발 설정(에디터·개발 빌드): 개발 진입이 켜지면 Core 공개 서비스로 새 Run·시작 아이템 지급·챕터를 준비하고, 전투 정의는 운영과 같은 DB 정의(Database)를 씁니다.
    /// 개발 고정 정의(Fixture) 모드는 2026-10-28 사용자 지시로 지웠습니다. 그 정의 클래스는 시험용으로만 남아 있습니다.
    /// 전투 대상은 CombatEntry에 전달하며 선택 확정·준비·활성화는 골격 진입 수명이 맡습니다(설계 §8.2·§22, W12·W23).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour, IGameStartReceiver
    {
        /// <summary>시작할 수 있는 구간입니다. 값은 Build Settings 순서(Lobby → StageSelect → Combat)와 같습니다(2026-10-15 변경, 옛 값 0 = Combat·2 = Lobby).</summary>
        private enum StartLifeCycle
        {
            Lobby = 0,
            Stage = 1,
            Combat = 2
        }

        /// <summary>
        /// 개발 진입 전투의 스테이지입니다. Node는 진입한 노드의 스테이지, Boss는 현재 맵의 보스 전투 스테이지 정의,
        /// Selected는 Dev Stage Key의 전투 스테이지 정의입니다.
        /// Core가 이어진 노드로만 진입을 허락해 보스 노드로 바로 갈 수 없으므로, Boss는 Dev Node Id로 진입하되
        /// 전투 정의만 보스 스테이지로 읽습니다(DevBossStageBattleSource).
        /// 일반 스테이지는 맵마다 노드에 무작위로 놓이므로, Selected도 같은 방식으로 지정 스테이지를 읽습니다(DevSelectedStageBattleSource).
        /// PlacedStage는 Dev Stage Data의 전투 스테이지를 Dev Node Id 노드에 실제로 놓고 진입합니다. 챕터는 그 스테이지의 맵이며,
        /// 노드·전투·클리어 기록이 모두 그 스테이지가 됩니다(개발 진입 지정 스테이지 배치 설계).
        /// </summary>
        private enum DevBattleStage
        {
            Node = 0,
            Boss = 1,
            Selected = 2,
            PlacedStage = 3
        }

        // 씬을 넘어 유지되는 부트스트랩의 중복 생성을 막는 용도로만 쓴다(서비스 찾기에 쓰지 않는다).
        private static GameBootstrap _activeInstance;

        [SerializeField] private DatabaseCatalogSO databaseCatalog;
        [Tooltip("Boot에서 한 번 생성하고 씬을 넘어 유지할 Global UI Canvas 프리팹입니다.")]
        [SerializeField] private GameObject globalUICanvasPrefab;
        [SerializeField] private StartLifeCycle startLifeCycle = StartLifeCycle.Lobby;
        [Tooltip("Lobby 시작에서 공통 준비 완료 후 게임 시작 입력을 기다릴 화면입니다.")]
        [SerializeField] private TitleUI titleUI;

        // 개발 설정(설계 §8.2, W12). 에디터·개발 빌드에서만 적용되며, 비개발 빌드에서는 읽지 않는다.
#pragma warning disable CS0414
        [Header("개발 설정 (에디터·개발 빌드 전용)")]
        [Tooltip("켜면 Combat 직접 시작에서 새 Run·챕터 준비 후 아래 노드로 진입을 요청한다. 전투 정의는 운영과 같은 DB 정의다(노드 그래프도 DB 맵 설정으로 만든다).")]
        [FormerlySerializedAs("useDevBattle")]
        [SerializeField] private bool useDevProgress = true;
        [Tooltip("StartNewRun에 넘길 PlayerLoadout 키")]
        [SerializeField] private int devLoadoutKey = 1;
        [Tooltip("EnterChapter에 넘길 StageMap 키. Dev Battle Stage가 PlacedStage면 쓰지 않는다(지정 스테이지의 맵)")]
        [SerializeField] private int devChapterKey = 1;
        [Tooltip("EnterBattle에 요청할 노드 번호. 시작 노드는 1, 깊이 1의 첫 노드는 2다. 선택은 Core 수락 때 확정된다")]
        [SerializeField] private int devNodeId = 2;
        [Tooltip("개발 전투 스테이지. Node = 진입 노드의 스테이지, Boss = 현재 맵의 보스 전투 스테이지 정의, Selected = 아래 Dev Stage Key의 전투 스테이지 정의, PlacedStage = 아래 Dev Stage Data를 Dev Node Id 노드에 놓고 진입")]
        [SerializeField] private DevBattleStage devBattleStage = DevBattleStage.Node;
        [Tooltip("Dev Battle Stage가 Selected일 때 읽을 전투 스테이지 키. 현재 챕터 맵의 전투 스테이지여야 하며, 1 미만이면 재생 시작에서 멈춘다")]
        [SerializeField] private int devStageKey;
        [Tooltip("Dev Battle Stage가 PlacedStage일 때, 이 전투 스테이지를 Dev Node Id 노드에 놓고 진입한다. 챕터는 그 스테이지의 맵이다. DB 스테이지 루트 표에 등록된 일반 전투 스테이지여야 한다")]
        [SerializeField] private CombatStageData devStageData;
        [Tooltip("새 Run 직후 지급할 아이템(키·수량). 개발 전투 또는 Play From Boot 챕터 직접 시작에서 쓴다. 이 PC에서만 바꾸려면 Play From Boot 설정을 쓴다")]
        [SerializeField] private DevStartingItem[] devStartingItems = Array.Empty<DevStartingItem>();
#pragma warning restore CS0414

        private RuntimeFaultBoundary _faults;
        private FrameworkRuntime _runtime;
        private bool _isRunning;
        private bool _shutdownStarted;
        private CombatEntryRequest _bootEntry;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 개발 보스·지정 스테이지 진입에서만 만든다(CreateDevBattleSource). 서비스 바인딩 뒤 한 번 연결한다.
        private DevBossStageBattleSource _devBossSource;
        private DevSelectedStageBattleSource _devSelectedSource;
        // 에디터 덮어쓰기의 챕터 직접 시작입니다. 0이면 기존 씬 시작 경로를 사용합니다.
        private int _devStageChapterKey = 0;
        // 개발 진입이 실제로 요청할 챕터입니다. 보통 Dev Chapter Key이고, 지정 스테이지 배치면 그 스테이지의 맵입니다(PrepareDevProgress).
        private int _devEntryChapterKey;
        // 개발 진입이 실제로 요청할 노드입니다. 챕터 진입 뒤 맵 그래프로 정합니다(ResolveDevEntryNode).
        private int _devEntryNodeId;
        // 개발 직행 진입을 아직 요청하지 못했는지입니다. 진입의 일시적 사전 조건이 맞을 때까지 Update가 다시 봅니다(TryRequestDevEntry).
        private bool _devEntryWaiting;
#endif
        private GameObject _globalUICanvas;
        private IUISystemService _ui;
        private IGlobalUIPresentation _globalUI;
        private IArtifactBoardPreparation _boardPreparation;
        private bool _startupPending;
        private bool _titleReady;
        private bool _gameStartRequested;

        // 도메인 리로드 없이 재생을 다시 시작해도 이전 재생의 인스턴스가 남지 않게 한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActiveInstance()
        {
            _activeInstance = null;
        }

        private void Awake()
        {
            if (_activeInstance != null)
            {
                Destroy(gameObject);
                return;
            }

            _activeInstance = this;
            DontDestroyOnLoad(gameObject);
            _faults = new RuntimeFaultBoundary(Debug.LogException);
        }

        private void Start()
        {
            if (_activeInstance != this)
                return;

            try
            {
#if UNITY_EDITOR
                ApplyEditorOverrides();
#endif
                ValidateScenes();
                if (databaseCatalog == null)
                    throw new InvalidOperationException("[GameBootstrap] DatabaseCatalogSO is not assigned.");
                if (startLifeCycle == StartLifeCycle.Lobby)
                {
                    if (titleUI == null)
                        throw new InvalidOperationException("[GameBootstrap] Lobby 시작에는 Boot 씬의 TitleUI 참조가 필요합니다.");
                    titleUI.BindStart(this);
                }

                IServiceModule database = DataBaseFactory.Create(databaseCatalog);
                CreateGlobalUI();
                // 이번 재생의 자동 시작 설정을 고정한다. 에디터 덮어쓰기 외에는 기존 동작을 유지한다.
                bool autoStartTutorial = true;
#if UNITY_EDITOR
                autoStartTutorial = !GameBootstrapEditorOverrides.Enabled || GameBootstrapEditorOverrides.UseTutorial;
#endif
                _runtime = FrameworkRuntimeFactory.Create(CreateDevBattleSource(), _faults, autoStartTutorial);
                _runtime.BindScene();
                _runtime.InitData(database);
                _runtime.PostBindService();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _devBossSource?.Bind(new RuntimeServices(_runtime));
                _devSelectedSource?.Bind(new RuntimeServices(_runtime));
#endif
                _runtime.Initialize();
                var audio = _runtime.GetService(SystemGroupsService.Global).AudioServiceCommand;
                audio.SetPreset(AmbgmEvent.BGM, 0);
                if (!audio.IsPlaying(AmbgmEvent.BGM)) audio.Play(AmbgmEvent.BGM);
                BindUILifeCycle();
                _globalUI.ShowTitle();

                if (_faults.IsFaulted || _shutdownStarted)
                    return;
                _boardPreparation = _runtime.GetService(SystemGroupsService.LifeCycle).ArtifactBoardPreparation;
                _boardPreparation.Request();
                _startupPending = true;
                _isRunning = !_faults.IsFaulted && !_shutdownStarted;
            }
            catch (Exception exception)
            {
                // 완료되지 않은 단계로 진행하지 않는다. Tick도 시작하지 않는다.
                _faults.Report(exception);
            }
        }

        private void Update()
        {
            if (!_isRunning || _shutdownStarted || _faults.IsFaulted)
                return;

            try
            {
                _runtime.Tick(Time.deltaTime);
                ContinueStartup();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                TryRequestDevEntry();
#endif
                if (_bootEntry != null && _bootEntry.IsRejected)
                {
                    // 비전투 노드는 거절이 아니라 준비 Job의 예외(F1)이고, 개발 진입은 ResolveDevEntryNode가 전투 노드로 보정한다.
                    // 그래서 여기 오는 거절은 Core가 준 사유 그대로 남긴다.
                    Debug.Log($"[GameBootstrap] 전투 진입 거절: {_bootEntry.Result.FailureReason}, {_bootEntry.Result.NavigationFailureReason}");
                    _bootEntry = null;
                }
                else if (_bootEntry != null && (_bootEntry.IsActive || _bootEntry.IsCancelled))
                {
                    _bootEntry = null;
                }
            }
            catch (Exception exception)
            {
                // Runner는 예외 Job을 유지하므로 같은 런타임을 다시 Tick하지 않는다(F1).
                _faults.Report(exception);
            }
        }

        private void CreateGlobalUI()
        {
            if (globalUICanvasPrefab == null || globalUICanvasPrefab.GetComponent<Canvas>() == null)
                throw new InvalidOperationException("[GameBootstrap] Global UI Canvas prefab is required.");
            _globalUICanvas = Instantiate(globalUICanvasPrefab);
            _globalUICanvas.name = globalUICanvasPrefab.name;
            DontDestroyOnLoad(_globalUICanvas);
        }

        /// <summary>공통 준비 뒤 Lobby 시작은 버튼 입력을 기다리고, 최초 구간 진입을 한 번만 요청합니다.</summary>
        private void ContinueStartup()
        {
            if (!_startupPending || _shutdownStarted || _faults.IsFaulted || !_boardPreparation.IsReady)
                return;
            if (startLifeCycle == StartLifeCycle.Lobby)
            {
                if (!_titleReady)
                {
                    _titleReady = true;
                    titleUI.SetCanStart(true);
                    _ui.OpenUI(UIId.Title);
                }
                if (!_gameStartRequested) return;
                titleUI.SetCanStart(false);
                _ui.CloseUI(UIId.Title);
            }
            _startupPending = false;
            PrepareDevProgress();
            if (_shutdownStarted || _faults.IsFaulted)
                return;
            RequestInitialLifeCycle();
        }

        /// <summary>UI 콜백에서는 의도만 받고 실제 전환은 다음 Bootstrap Update에서 수행합니다.</summary>
        bool IGameStartReceiver.TryRequestGameStart()
        {
            if (!_isRunning || !_startupPending || !_titleReady || _gameStartRequested ||
                _shutdownStarted || _faults.IsFaulted || !_boardPreparation.IsReady)
                return false;
            _gameStartRequested = true;
            return true;
        }

        private void BindUILifeCycle()
        {
            var global = _runtime.GetService(SystemGroupsService.Global);
            _ui = global.UIServiceCommand;
            _globalUI = global.GlobalUIPresentation;
            // Framework는 씬마다 다시 연결하는 수단(BindScene)을 서비스 계약으로 주지 않고 그룹 모듈에만 둔다.
            // 그래서 UI 서비스와 카메라 그룹을 그 모듈로 보는 캐스트 둘을 이 한 곳에만 둔다(기반 한계, 전투 개선 설계 §7.2 E4).
            var bindUI = (ISystemBindScene)_ui;
            var bindCameras = (ISystemBindScene)global.RenderServiceCommand.CoreGroup.CameraGroup;

            // 씬별 진입 노드 표입니다. 순서 값의 원본은 SceneNodeOrder 한 곳이다. 같은 순서 안에서는 이 표의 위에서부터 돈다.
            var nodes = new (int Scene, int Priority, Action Run)[]
            {
                (SceneIndices.Stage, SceneNodeOrder.BindSceneUI, bindUI.BindScene),
                (SceneIndices.Combat, SceneNodeOrder.BindSceneUI, bindUI.BindScene),
                (SceneIndices.Lobby, SceneNodeOrder.BindSceneUI, bindUI.BindScene),
                (SceneIndices.Stage, SceneNodeOrder.BindCameras, bindCameras.BindScene),
                (SceneIndices.Combat, SceneNodeOrder.BindCameras, bindCameras.BindScene),
                (SceneIndices.Lobby, SceneNodeOrder.BindCameras, bindCameras.BindScene),
                (SceneIndices.Combat, SceneNodeOrder.PrepareCombatDeck, PrepareCombatDeckUI),
                (SceneIndices.Combat, SceneNodeOrder.OpenSceneUI, OpenCombatUI),
                (SceneIndices.Stage, SceneNodeOrder.OpenSceneUI, OpenStageUI),
                (SceneIndices.Lobby, SceneNodeOrder.OpenSceneUI, ShowLobbyUI),
            };
            var loading = global.LodingServiceCommand.LodingRegisterService;
            foreach (var node in nodes)
                loading.RegisterSceneEnterLoaded(node.Scene, new LodingNode(node.Priority, node.Run));
        }

        private void OpenStageUI()
        {
            if (_faults.IsFaulted || _shutdownStarted) return;
            _globalUI.ShowStageSelect();
        }

        private void ShowLobbyUI()
        {
            if (_faults.IsFaulted || _shutdownStarted) return;
            _globalUI.ShowLobby();
        }

        private void PrepareCombatDeckUI()
        {
            if (_faults.IsFaulted || _shutdownStarted) return;
            // 씬 등록 뒤, 최초 View 연결 전에 한 번만 초기화합니다. 일반 Open은 선택된 자식을 유지합니다.
            // 이력 제외는 Combat 씬 안의 UI 루트 수명이며 GlobalUI의 씬 간 보존과는 별개입니다.
            _ui.OpenUI(UIId.Deck, true);
            if (_faults.IsFaulted || _shutdownStarted) return;
            _ui.CloseChildUI(UIId.Deck);
        }

        private void OpenCombatUI()
        {
            if (_faults.IsFaulted || _shutdownStarted) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 개발 전투 직행에서는 요청형 패널을 제외한 공통 요소를 모두 표시합니다.
            if (useDevProgress && startLifeCycle == StartLifeCycle.Combat)
                _globalUI.ShowDevelopmentCombat();
            else
#endif
            _globalUI.ShowCombat();
            if (_faults.IsFaulted || _shutdownStarted) return;
            _ui.OpenUI(UIId.NightChecker, true);
            // 덱 카드는 맵 등장→배치 안내를 마친 뒤 전투 진입 표시가 엽니다.
        }

        private void OnApplicationQuit()
        {
            ShutdownOnce();
        }

        private void OnDestroy()
        {
            ShutdownOnce();
        }

        /// <summary>종료 플래그 → 입력 정리 → 런타임 Shutdown 최대 1회 → 소유 참조 정리 순서로 끝냅니다.</summary>
        private void ShutdownOnce()
        {
            if (_activeInstance != this || _shutdownStarted)
                return;

            _shutdownStarted = true;
            _startupPending = false;
            _isRunning = false;
            if (titleUI != null) titleUI.UnbindStart(this);
            try
            {
                Exception inputFailure = _faults.StopInputForShutdown();
                if (inputFailure != null)
                    Debug.LogException(new InvalidOperationException(
                        "[GameBootstrap] Input cleanup failed during shutdown.", inputFailure));

                _runtime?.Shutdown();
            }
            catch (Exception exception)
            {
                // FrameworkRuntime은 하위 Shutdown 예외에서 순회를 멈추므로 전체 정리를 보장했다고 기록하지 않는다.
                Debug.LogException(new InvalidOperationException(
                    "[GameBootstrap] Runtime shutdown failed. Some Framework resources may not be released.", exception));
            }
            finally
            {
                _runtime = null;
                _bootEntry = null;
                _boardPreparation = null;
                _ui = null;
                _globalUI = null;
                if (_globalUICanvas != null) Destroy(_globalUICanvas);
                _globalUICanvas = null;
                _activeInstance = null;
            }
        }

        /// <summary>
        /// 개발 진입이 켜져 있고 전투로 시작할 때, 보스 전투 스테이지를 골랐으면 보스 스테이지 정의를, 지정 스테이지를 골랐으면
        /// Dev Stage Key의 정의를 읽는 개발 소스를 돌려줍니다. 지정 키가 1 미만이면 개발 소스 생성이 예외로 시작을 멈춥니다.
        /// 아니면 null(운영과 같은 기본 DB 소스)입니다. 지정 스테이지 배치(PlacedStage)도 노드에 스테이지를 놓으므로 기본 DB 소스입니다.
        /// </summary>
        private IBattleInitializationSource CreateDevBattleSource()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!useDevProgress || startLifeCycle != StartLifeCycle.Combat)
                return null;
            if (devBattleStage == DevBattleStage.Boss)
            {
                _devBossSource = new DevBossStageBattleSource();
                return _devBossSource;
            }
            if (devBattleStage == DevBattleStage.Selected)
            {
                _devSelectedSource = new DevSelectedStageBattleSource(devStageKey);
                return _devSelectedSource;
            }
#endif
            return null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 프레임워크가 외부 전투 정의 소스를 모듈로 묶지 않으므로, 개발 보스·지정 스테이지 소스는 서비스 바인딩 뒤 이 어댑터로 한 번 연결합니다.
        /// </summary>
        private sealed class RuntimeServices : ISystemServiceStoreReader
        {
            private readonly FrameworkRuntime _runtime;

            internal RuntimeServices(FrameworkRuntime runtime) => _runtime = runtime;

            public TService GetService<TService>(SystemGroupKey<TService> systemGroupKey)
                where TService : class, IServiceModule => _runtime.GetService(systemGroupKey);
        }
#endif

        /// <summary>
        /// 개발용 진행 준비를 Core 공개 서비스로 수행합니다(설계 §8.2, W12). Buffer에 직접 쓰지 않습니다.
        /// 로스터와 챕터를 준비합니다. Combat 선택 확정은 진입 Job의 Core 호출에 맡깁니다.
        /// 운영의 스테이지 맵 → 전투 경로를 대신하지 않는 개발 전용 단축 경로입니다. 거절은 예외로 보고해 진입하지 않습니다.
        /// </summary>
        private void PrepareDevProgress()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool chapterStart = startLifeCycle == StartLifeCycle.Stage && _devStageChapterKey > 0;
            if (!chapterStart && (!useDevProgress || startLifeCycle != StartLifeCycle.Combat))
                return;

            IInGameCoreSystemGroupService core = _runtime.GetService(SystemGroupsService.InGame).CoreGroup;

            // 지정 스테이지 배치는 Run을 만들기 전에 루트를 찾는다. 잘못된 입력이면 Run 없이 멈춘다(설계 DS7).
            int chapterKey = chapterStart ? _devStageChapterKey : devChapterKey;
            ChapterStagePin pin = null;
            if (!chapterStart && devBattleStage == DevBattleStage.PlacedStage)
            {
                if (devStageData == null)
                    throw new InvalidOperationException(
                        "[GameBootstrap] Dev Battle Stage가 PlacedStage인데 Dev Stage Data가 비었습니다. 인스펙터나 Play From Boot 설정에서 고르세요.");
                StageRootData root = DevStageRootLookup.Find(databaseCatalog, devStageData);
                chapterKey = root.MapKey;
                pin = new ChapterStagePin(new StageNodeId(devNodeId), new StageKey(root.StageKey));
                string chapterNote = chapterKey == devChapterKey ? "" : $" Dev Chapter Key {devChapterKey} 대신 맵 {chapterKey}로 진입합니다.";
                Debug.Log($"[GameBootstrap] 지정 스테이지 {devStageData.name}(키 {root.StageKey}, 맵 {root.MapKey})를 노드 {devNodeId}에 놓습니다.{chapterNote}");
            }

            // 운영 새 Run과 같은 경로다: 이전 Run 정리·시작 로스터·시작 Run 재화를 한 번에 설치한 뒤 챕터에 들어간다.
            // StartNewRun은 맵 선택까지 비우므로 EnterChapter는 반드시 그 뒤에 한다(Core 새 Run 계약).
            RosterAcquireResult roster = core.RunStart.StartNewRun(devLoadoutKey);
            if (!roster.Succeeded)
                throw new InvalidOperationException(
                    $"[GameBootstrap] Dev new run was rejected. Loadout: {devLoadoutKey}, reason: {roster.FailureReason}");

            // 새 Run이 재고를 비우므로 시작 아이템은 그 뒤에 지급한다. 챕터 진입은 재고를 건드리지 않는다(시작 아이템 설계 DI3).
            DevStartingItem[] startingItems = devStartingItems ?? Array.Empty<DevStartingItem>();
            ItemInventorySnapshot granted = DevStartingItemGrant.Apply(core.Items, startingItems);
            if (granted != null)
                Debug.Log($"[GameBootstrap] 개발 시작 아이템 지급: {DevStartingItemGrant.Describe(startingItems, granted)}");

            // 지정 스테이지 배치면 Core가 새 그래프의 노드 하나에 그 스테이지를 놓는다. 놓을 수 없으면 Core가 예외로 멈춘다.
            ChapterProgressionResult chapter = core.ChapterProgression.EnterChapter(chapterKey, pin);
            if (!chapter.Accepted)
                throw new InvalidOperationException(
                    $"[GameBootstrap] Dev chapter entry was rejected. Chapter: {chapterKey}, reason: {chapter.FailureReason}");
            // 챕터 직접 시작은 맵의 시작 지점에 머문다. 전투 직접 시작만 진입 노드를 고른다.
            if (!chapterStart)
            {
                _devEntryChapterKey = chapterKey;
                _devEntryNodeId = ResolveDevEntryNode(chapterKey);
            }

#else
            if (useDevProgress && startLifeCycle == StartLifeCycle.Combat)
                Debug.LogWarning("[GameBootstrap] Dev battle settings apply only in the editor or development builds.");
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 개발 진입 노드를 정합니다. Dev Node Id 노드가 전투 스테이지면 그대로 쓰고, 아니면 시작 노드에서 이어진 전투 노드 중 첫 번째로 바꿉니다.
        /// </summary>
        /// <remarks>
        /// 일반 노드의 스테이지는 Run마다 Regular 스테이지(상점·강화·이벤트 포함)에서 무작위로 뽑혀 고정 번호가 비전투일 수 있고,
        /// Core는 비전투 노드의 전투 진입을 거절하지 않고 예외로 멈춥니다(BattleEntryService.ValidateCombatTarget → 준비 Job → F1). 보스·지정 스테이지도 노드는 그대로 들어가고
        /// 전투 정의만 바꾸므로 같은 문제를 겪습니다. 맵 그래프는 Buffer에서 읽기만 하고, 진입 판단은 지금처럼 Core가 합니다.
        /// </remarks>
        private int ResolveDevEntryNode(int chapterKey)
        {
            IReadOnlyList<StageProgressData> nodes =
                _runtime.GetService(BufferSystemKey.Buffer).StageProgressReadSink.GetStages(chapterKey);
            bool found = TryFindNode(nodes, devNodeId, out StageProgressData requested);
            if (found && requested.StageType == StageType.Combat)
                return devNodeId;

            // 선택이 없을 때 Core는 시작 노드(깊이 0)에서 이어진 노드로만 진입을 허락한다.
            foreach (StageProgressData start in nodes)
            {
                if (start.Depth != 0) continue;
                foreach (int next in start.NextNodeIds)
                {
                    if (!TryFindNode(nodes, next, out StageProgressData candidate) || candidate.StageType != StageType.Combat)
                        continue;
                    string reason = found ? $"스테이지 {requested.StageKey}, {requested.StageType}" : "맵에 없음";
                    Debug.Log($"[GameBootstrap] 개발 진입 노드 {devNodeId}는 전투가 아니어서({reason}) " +
                        $"이어진 전투 노드 {next}(스테이지 {candidate.StageKey})로 진입합니다.");
                    return next;
                }
            }
            throw new InvalidOperationException(
                $"[GameBootstrap] 챕터 {chapterKey}에서 개발 진입 노드 {devNodeId}가 전투가 아니고, 시작 노드에서 이어진 전투 노드도 없습니다.");
        }

        private static bool TryFindNode(IReadOnlyList<StageProgressData> nodes, int nodeId, out StageProgressData node)
        {
            foreach (StageProgressData candidate in nodes)
            {
                if (candidate.NodeId != nodeId) continue;
                node = candidate;
                return true;
            }
            node = default;
            return false;
        }
#endif

        /// <summary>개발 첫 진입은 대상만 전달합니다. 실제 활성화는 LifeCycle의 결과 수신 수명이 수행합니다.</summary>
        private void RequestInitialLifeCycle()
        {
            if (_faults.IsFaulted || _shutdownStarted)
                return;
            var lifeCycles = _runtime.GetService(SystemGroupsService.LifeCycle);
            if (startLifeCycle != StartLifeCycle.Combat)
            {
                lifeCycles.LifeCycleManager.Change(ToLifeCycleKey(startLifeCycle));
                return;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (useDevProgress)
            {
                _devEntryWaiting = true;
                TryRequestDevEntry();
                return;
            }
#endif
            throw new InvalidOperationException("[GameBootstrap] Combat 직접 시작에는 개발 대상 설정이 필요합니다. 운영 진입은 챕터·로스터 준비 후 CombatEntry에 대상을 전달해야 합니다.");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 개발 직행 진입을 요청합니다. 진입의 일시적 사전 조건(공용 Job Runner가 비고 로딩 중이 아님, <see cref="ICombatEntryService"/> 설명)을
        /// 부르기 전에 확인하고, 맞지 않으면 다음 Update에 다시 봅니다(전투 개선 설계 §6.3, 일시적 조건을 오류로 다루지 않음).
        /// </summary>
        private void TryRequestDevEntry()
        {
            if (!_devEntryWaiting || _faults.IsFaulted || _shutdownStarted)
                return;
            var global = _runtime.GetService(SystemGroupsService.Global);
            if (!global.JobServiceCommand.JobRunner.IsIdle || global.LodingServiceCommand.LodingService.IsLoading)
                return;
            _devEntryWaiting = false;
            _bootEntry = _runtime.GetService(SystemGroupsService.LifeCycle).CombatEntry
                .RequestEntry(_devEntryChapterKey, new StageNodeId(_devEntryNodeId));
        }
#endif

#if UNITY_EDITOR
        /// <summary>
        /// 이 PC의 에디터 덮어쓰기(<see cref="GameBootstrapEditorOverrides"/>)가 켜져 있으면 이번 재생의 값만 바꿉니다.
        /// 씬에 저장된 값은 바뀌지 않습니다(재생 중 변경은 재생이 끝나면 버려짐). 정의되지 않은 값이면 예외로 시작을 멈춥니다.
        /// </summary>
        private void ApplyEditorOverrides()
        {
            if (!GameBootstrapEditorOverrides.Enabled)
                return;

            var start = (StartLifeCycle)GameBootstrapEditorOverrides.StartLifeCycle;
            var battleStage = (DevBattleStage)GameBootstrapEditorOverrides.DevBattleStage;
            if (!Enum.IsDefined(typeof(StartLifeCycle), start) || !Enum.IsDefined(typeof(DevBattleStage), battleStage))
                throw new InvalidOperationException(
                    "[GameBootstrap] Play From Boot 설정의 시작 구간·전투 스테이지 값이 올바르지 않습니다. 설정 창에서 다시 고르세요.");

            int stageChapterKey = start == StartLifeCycle.Stage ? GameBootstrapEditorOverrides.StageChapterKey : 0;
            if (start == StartLifeCycle.Stage && stageChapterKey < 1)
                throw new InvalidOperationException("[GameBootstrap] Play From Boot의 시작 챕터 키는 1 이상이어야 합니다.");

            startLifeCycle = start;
            _devStageChapterKey = stageChapterKey;
            useDevProgress = GameBootstrapEditorOverrides.UseDevProgress;
            devLoadoutKey = GameBootstrapEditorOverrides.DevLoadoutKey;
            devChapterKey = GameBootstrapEditorOverrides.DevChapterKey;
            devNodeId = GameBootstrapEditorOverrides.DevNodeId;
            devBattleStage = battleStage;
            devStageKey = GameBootstrapEditorOverrides.DevStageKey;
            // 지정 스테이지 배치를 실제로 쓸 때(Combat 시작·개발 진행)만 저장된 에셋을 못 찾은 것을 멈춤으로 본다.
            // 그 밖에는 null이 들어가고 쓰지 않는다.
            bool usesStageData = start == StartLifeCycle.Combat && useDevProgress &&
                battleStage == DevBattleStage.PlacedStage;
            if (!GameBootstrapEditorOverrides.TryReadDevStageData(out CombatStageData stageData) && usesStageData)
                throw new InvalidOperationException(
                    "[GameBootstrap] Play From Boot 설정의 Dev Stage Data 에셋을 찾지 못했습니다. 설정 창에서 다시 고르세요.");
            devStageData = stageData;
            // 저장값을 읽지 못하면 이 호출이 예외로 시작을 멈춘다(위 열거형 검사와 같다).
            devStartingItems = GameBootstrapEditorOverrides.DevStartingItems;

            // 씬 값과 다르게 시작한다는 사실을 한 번 남긴다(혼동 방지).
            if (start == StartLifeCycle.Stage)
                Debug.Log($"[GameBootstrap] 에디터 덮어쓰기 사용(ProjectL > Play From Boot 설정): 시작 Chapter {stageChapterKey}, " +
                    $"로드아웃 {devLoadoutKey}, 시작 아이템 {devStartingItems.Length}줄");
            else
                Debug.Log($"[GameBootstrap] 에디터 덮어쓰기 사용(ProjectL > Play From Boot 설정): 시작 {startLifeCycle}, " +
                    $"개발 진입 {(useDevProgress ? "켬" : "끔")}(로드아웃 {devLoadoutKey}, 챕터 {devChapterKey}, 노드 {devNodeId}, 전투 스테이지 {devBattleStage}{DescribeDevStage()}, 시작 아이템 {devStartingItems.Length}줄)");
        }

        // 덮어쓰기 로그의 전투 스테이지 뒤에 붙일 구절이다. 지정 스테이지는 키, 지정 스테이지 배치는 에셋 이름이다.
        private string DescribeDevStage()
        {
            return devBattleStage switch
            {
                DevBattleStage.Selected => $" {devStageKey}",
                DevBattleStage.PlacedStage => $" {(devStageData != null ? devStageData.name : "없음")}",
                _ => ""
            };
        }
#endif

        /// <summary>Boot 씬에서 시작했는지와 Build Settings의 실제 씬 경로·번호를 대조합니다(설계 §11).</summary>
        private static void ValidateScenes()
        {
            int activeIndex = SceneManager.GetActiveScene().buildIndex;
            if (activeIndex != SceneIndices.Boot)
                throw new InvalidOperationException(
                    $"[GameBootstrap] GameBootstrap must start in the Boot scene (build index {SceneIndices.Boot}), but the active scene index is {activeIndex}.");

            RequireBuildScene(SceneIndices.Boot, SceneIndices.BootPath);
            RequireBuildScene(SceneIndices.Stage, SceneIndices.StagePath);
            RequireBuildScene(SceneIndices.Combat, SceneIndices.CombatPath);
            RequireBuildScene(SceneIndices.Lobby, SceneIndices.LobbyPath);
        }

        private static void RequireBuildScene(int buildIndex, string expectedPath)
        {
            string actualPath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            if (!string.Equals(actualPath, expectedPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"[GameBootstrap] Build Settings scene {buildIndex} must be '{expectedPath}', but was '{actualPath}'.");
        }

        /// <summary>직접 전환할 시작 구간입니다. Combat은 진입 요청으로만 들어가므로 여기 오지 않습니다(오면 예외).</summary>
        private static LifeCycleKey ToLifeCycleKey(StartLifeCycle start)
        {
            switch (start)
            {
                case StartLifeCycle.Stage:
                    return LifeCycleKeys.Stage;
                case StartLifeCycle.Lobby:
                    return LifeCycleKeys.Lobby;
                default:
                    throw new ArgumentOutOfRangeException(nameof(start), start, null);
            }
        }
    }
}
