using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.Runtime;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev;
#endif
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot
{
    /// <summary>
    /// Boot 씬의 런타임 구동자입니다(설계 W1 §10). DB 생성 → 런타임 조립 → 수명 단계 → 시작 구간 전환 →
    /// 매 프레임 Tick → 종료를 한 곳에서 맡고, 런타임당 하나인 오류 경계를 만들어 소유합니다.
    /// </summary>
    /// <remarks>
    /// Framework 가이드가 "연동 담당 코드"에 맡긴 구동 역할(FW-016, DR-001)을 채우는 구현입니다. 역할의 전체 목록은 설계 §10.0입니다.
    /// 맡는 것: 런타임 생성·소유, 수명 단계 1회 호출, 시작 구간 전환 1회, 매 프레임 Tick, 오류 경계 소유, 종료 1회.
    /// 맡지 않는 것: 규칙·상태(Core), Job 등록 판단(Flow), 화면·입력(View), 구간 사이 전환(F2), 서비스 찾기 창구.
    /// 첫 오류가 보고되면 다음 Tick·전환을 호출하지 않습니다. 복구는 새 런타임(재생 재시작)으로만 합니다.
    /// 개발 설정(에디터·개발 빌드): 개발 진입이 켜지면 Core 공개 서비스로 새 Run·챕터를 준비하고, 전투 정의는 Fixture(개발 고정) 또는 Database(운영 DB) 중 고릅니다.
    /// 전투 대상은 CombatEntry에 전달하며 선택 확정·준비·활성화는 골격 진입 수명이 맡습니다(설계 §8.2·§22, W12·W23).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        /// <summary>시작할 수 있는 구간입니다. 씬이 없는 예약 번호(Lobby·Artifact)는 고를 수 없습니다.</summary>
        private enum StartLifeCycle
        {
            Combat = 0,
            Stage = 1
        }

        /// <summary>개발 진입에서 쓸 전투 정의 소스입니다. Database는 운영과 같은 기본 DB 소스입니다.</summary>
        private enum DevBattleDefinition
        {
            Fixture = 0,
            Database = 1
        }

        // 씬을 넘어 유지되는 부트스트랩의 중복 생성을 막는 용도로만 쓴다(서비스 찾기에 쓰지 않는다).
        private static GameBootstrap _activeInstance;

        [SerializeField] private DatabaseCatalogSO databaseCatalog;
        [Tooltip("Boot에서 한 번 생성하고 씬을 넘어 유지할 Global UI Canvas 프리팹입니다.")]
        [SerializeField] private GameObject globalUICanvasPrefab;
        [SerializeField] private StartLifeCycle startLifeCycle = StartLifeCycle.Combat;
        [SerializeField] private CombatExecutionMode executionMode = CombatExecutionMode.Live;

        // 개발 설정(설계 §8.2, W12). 에디터·개발 빌드에서만 적용되며, 비개발 빌드에서는 읽지 않는다.
#pragma warning disable CS0414
        [Header("개발 설정 (에디터·개발 빌드 전용)")]
        [Tooltip("켜면 Combat 직접 시작에서 새 Run·챕터 준비 후 아래 노드로 진입을 요청한다. 두 실행 모드에 같다.")]
        [FormerlySerializedAs("useDevBattle")]
        [SerializeField] private bool useDevProgress = true;
        [Tooltip("개발 진입의 전투 정의. Fixture = 개발용 고정 정의, Database = 운영과 같은 DB 정의(노드 그래프도 DB 맵 설정으로 만든다)")]
        [SerializeField] private DevBattleDefinition devBattleDefinition = DevBattleDefinition.Fixture;
        [Tooltip("StartNewRun에 넘길 PlayerLoadout 키")]
        [SerializeField] private int devLoadoutKey = 1;
        [Tooltip("두 실행 모드에서 EnterChapter에 넘길 StageMap 키")]
        [SerializeField] private int devChapterKey = 1;
        [Tooltip("EnterBattle에 요청할 노드 번호. 시작 노드는 1, 깊이 1의 첫 노드는 2다. 선택은 Core 수락 때 확정된다")]
        [SerializeField] private int devNodeId = 2;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("개발 전투 필드")]
        [SerializeField, Min(1)] private int devMapWidth = DevBattleInitializationSource.DefaultMapWidth;
        [SerializeField, Min(1)] private int devMapHeight = DevBattleInitializationSource.DefaultMapHeight;
        [SerializeField, Min(1)] private int devPlayerDeploymentDepth = DevBattleInitializationSource.DefaultPlayerDeploymentDepth;
        [SerializeField, Min(1)] private int devEnemyDeploymentDepth = DevBattleInitializationSource.DefaultEnemyDeploymentDepth;
#endif
#pragma warning restore CS0414

        private RuntimeFaultBoundary _faults;
        private FrameworkRuntime _runtime;
        private bool _isRunning;
        private bool _shutdownStarted;
        private CombatEntryRequest _bootEntry;
        private GameObject _globalUICanvas;
        private IUISystemService _ui;

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

                IServiceModule database = DataBaseFactory.Create(databaseCatalog);
                CreateGlobalUI();
                _runtime = FrameworkRuntimeFactory.Create(CreateDevBattleSource(), executionMode, _faults);
                _runtime.BindScene();
                _runtime.InitData(database);
                _runtime.PostBindService();
                _runtime.Initialize();
                BindUILifeCycle();

                if (_faults.IsFaulted || _shutdownStarted)
                    return;
                PrepareDevProgress();
                _ui.OpenGlobalUI();
                RequestInitialLifeCycle();
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
                if (_bootEntry?.Status == CombatEntryStatus.Rejected)
                {
                    // DB 정의는 노드 그래프도 DB 맵 설정으로 만들므로 지정한 노드가 전투가 아닐 수 있다.
                    string hint = devBattleDefinition == DevBattleDefinition.Database
                        ? " (DB 정의: 개발 노드가 이 맵 그래프에서 전투 노드인지 확인하세요)" : "";
                    Debug.Log($"[GameBootstrap] 전투 진입 거절: {_bootEntry.Result.FailureReason}, {_bootEntry.Result.NavigationFailureReason}{hint}");
                    _bootEntry = null;
                }
                else if (_bootEntry?.Status == CombatEntryStatus.Active || _bootEntry?.Status == CombatEntryStatus.Cancelled)
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

        private void BindUILifeCycle()
        {
            var global = _runtime.GetService(SystemGroupsService.Global);
            _ui = global.UIServiceCommand;
            var loading = global.LodingServiceCommand.LodingRegisterService;
            // UI 참조는 LifeCycle 진입(0)과 View 연결(100) 전에 갱신한다.
            loading.RegisterSceneEnterLoaded(SceneIndices.Stage, new LodingNode(-200, BindLoadedUI));
            loading.RegisterSceneEnterLoaded(SceneIndices.Combat, new LodingNode(-200, BindLoadedUI));
            loading.RegisterSceneEnterLoaded(SceneIndices.Combat, new LodingNode(200, OpenCombatUI));
        }

        private void BindLoadedUI() => ((ISystemBindScene)_ui).BindScene();

        private void OpenCombatUI() => _ui.OpenUI(UIId.CombatLog, true);

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
            _isRunning = false;
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
                _ui = null;
                if (_globalUICanvas != null) Destroy(_globalUICanvas);
                _globalUICanvas = null;
                _activeInstance = null;
            }
        }

        /// <summary>
        /// 개발 진입이 켜져 있고 전투로 시작하며 정의가 Fixture면 개발용 준비 입력을 돌려줍니다.
        /// 아니면 null(운영과 같은 기본 DB 소스)입니다.
        /// </summary>
        private IBattleInitializationSource CreateDevBattleSource()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (useDevProgress && devBattleDefinition == DevBattleDefinition.Fixture &&
                startLifeCycle == StartLifeCycle.Combat)
                return new DevBattleInitializationSource(devMapWidth, devMapHeight,
                    devPlayerDeploymentDepth, devEnemyDeploymentDepth);
#endif
            return null;
        }

        /// <summary>
        /// 개발용 진행 준비를 Core 공개 서비스로 수행합니다(설계 §8.2, W12). Buffer에 직접 쓰지 않습니다.
        /// 두 실행 모드 모두 로스터와 챕터를 준비합니다. Combat 선택 확정은 진입 Job의 Core 호출에 맡깁니다.
        /// 운영의 스테이지 맵 → 전투 경로를 대신하지 않는 개발 전용 단축 경로입니다. 거절은 예외로 보고해 진입하지 않습니다.
        /// </summary>
        private void PrepareDevProgress()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!useDevProgress || startLifeCycle != StartLifeCycle.Combat)
                return;

            IInGameCoreSystemGroupService core = _runtime.GetService(SystemGroupsService.InGame).CoreGroup;

            // 운영 새 Run과 같은 경로다: 이전 Run 정리·시작 로스터·시작 Run 재화를 한 번에 설치한 뒤 챕터에 들어간다.
            // StartNewRun은 맵 선택까지 비우므로 EnterChapter는 반드시 그 뒤에 한다(Core 새 Run 계약).
            RosterAcquireResult roster = core.RunStart.StartNewRun(devLoadoutKey);
            if (!roster.Succeeded)
                throw new InvalidOperationException(
                    $"[GameBootstrap] Dev new run was rejected. Loadout: {devLoadoutKey}, reason: {roster.FailureReason}");

            ChapterProgressionResult chapter = core.ChapterProgression.EnterChapter(devChapterKey);
            if (!chapter.Accepted)
                throw new InvalidOperationException(
                    $"[GameBootstrap] Dev chapter entry was rejected. Chapter: {devChapterKey}, reason: {chapter.FailureReason}");

#else
            if (useDevProgress)
                Debug.LogWarning("[GameBootstrap] Dev battle settings apply only in the editor or development builds.");
#endif
        }

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
                _bootEntry = lifeCycles.CombatEntry.RequestEntry(devChapterKey, new StageNodeId(devNodeId));
                return;
            }
#endif
            throw new InvalidOperationException("[GameBootstrap] Combat 직접 시작에는 개발 대상 설정이 필요합니다. 운영 진입은 챕터·로스터 준비 후 CombatEntry에 대상을 전달해야 합니다.");
        }

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
            var mode = (CombatExecutionMode)GameBootstrapEditorOverrides.ExecutionMode;
            var definition = (DevBattleDefinition)GameBootstrapEditorOverrides.DevBattleDefinition;
            if (!Enum.IsDefined(typeof(StartLifeCycle), start) || !Enum.IsDefined(typeof(CombatExecutionMode), mode) ||
                !Enum.IsDefined(typeof(DevBattleDefinition), definition))
                throw new InvalidOperationException(
                    "[GameBootstrap] Play From Boot 설정의 시작 구간·실행 모드·전투 정의 값이 올바르지 않습니다. 설정 창에서 다시 고르세요.");

            startLifeCycle = start;
            executionMode = mode;
            useDevProgress = GameBootstrapEditorOverrides.UseDevProgress;
            devBattleDefinition = definition;
            devLoadoutKey = GameBootstrapEditorOverrides.DevLoadoutKey;
            devChapterKey = GameBootstrapEditorOverrides.DevChapterKey;
            devNodeId = GameBootstrapEditorOverrides.DevNodeId;
            devMapWidth = GameBootstrapEditorOverrides.DevMapWidth;
            devMapHeight = GameBootstrapEditorOverrides.DevMapHeight;
            devPlayerDeploymentDepth = GameBootstrapEditorOverrides.DevPlayerDeploymentDepth;
            devEnemyDeploymentDepth = GameBootstrapEditorOverrides.DevEnemyDeploymentDepth;

            // 씬 값과 다르게 시작한다는 사실을 한 번 남긴다(혼동 방지).
            Debug.Log($"[GameBootstrap] 에디터 덮어쓰기 사용(ProjectL > Play From Boot 설정): 시작 {startLifeCycle}, 모드 {executionMode}, " +
                $"개발 진입 {(useDevProgress ? "켬" : "끔")}(전투 정의 {devBattleDefinition}, 로드아웃 {devLoadoutKey}, 챕터 {devChapterKey}, 노드 {devNodeId}), " +
                $"개발 필드 {devMapWidth}×{devMapHeight}, 배치 깊이 아군 {devPlayerDeploymentDepth}/적군 {devEnemyDeploymentDepth}");
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
        }

        private static void RequireBuildScene(int buildIndex, string expectedPath)
        {
            string actualPath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            if (!string.Equals(actualPath, expectedPath, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"[GameBootstrap] Build Settings scene {buildIndex} must be '{expectedPath}', but was '{actualPath}'.");
        }

        private static LifeCycleKey ToLifeCycleKey(StartLifeCycle start)
        {
            switch (start)
            {
                case StartLifeCycle.Combat:
                    return LifeCycleKeys.Combat;
                case StartLifeCycle.Stage:
                    return LifeCycleKeys.Stage;
                default:
                    throw new ArgumentOutOfRangeException(nameof(start), start, null);
            }
        }
    }
}
