using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 튜토리얼의 런타임 수명 실행 소유자입니다(튜토리얼 설계 §9.3·§10, 구현 계획 T-4). 선례는 <see cref="Recovery.RecoveryRequestLifetime"/>입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 맡는 것: 정의를 상태 Store에 한 번 설치, Tick 트리거(조건 없이 Next 등록), Combat 화면 연결·해제와 1-1 판정·Start 제출,
    /// 표시·정리가 끝났다는 사실 기록, 화면 해제·런타임 정지의 Job 없는 마감, 오류 때 자원 반환.
    /// </para>
    /// <para>
    /// 맡기는 것: 다음 할 일 판단과 설명 구간 시작 조건(TutorialPlan), 상태 전이 규칙(상태 Store), 세션 Snapshot 만들기(<see cref="TutorialSessionChanges"/>),
    /// 1-1 판정 규칙(<see cref="TutorialViewTarget"/>), 실제 표시(<see cref="ITutorialViewDisplay"/>), 입력 빌리기(<see cref="ITutorialViewInputLease"/>),
    /// "다음" 입력 해석(<see cref="TutorialViewInputAdapter"/>).
    /// </para>
    /// <para>
    /// 씬보다 오래 살아야 하므로 씬 Scope가 아니라 View 그룹 모듈입니다(설계 §10). 씬의 튜토리얼 화면은 Combat 화면 연결 동안만 잡습니다.
    /// </para>
    /// </remarks>
    internal sealed class TutorialViewSystem :
        ITutorialViewExecution,
        ISystemInitData<IHasViewQuery>,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemTick,
        ISystemShutdown,
        IExportSystemServices
    {
        private const string CleanupFailure = "[TutorialViewSystem] 튜토리얼 입력·표시 정리나 세션 마감에 실패했습니다.";

        private readonly RuntimeFaultBoundary _faults;
        private readonly Func<CombatInputWindow> _readInputWindow;
        private readonly TutorialViewPresentationRecord _presentation = new();
        private readonly Action<string> _log;
        private readonly ITutorialInputReceiverSlot _inputSlot;
        private readonly bool _autoStartTutorial;

        private ITutorialDefinitionTableQuery _definitions;
        private ITutorialBufferStore _store;
        private ITutorialStateBufferReadSink _state;
        private ITutorialRequestBufferWriteSink _requests;
        private ICombatStateBufferReadSink _combat;
        private ICurrentProgressBufferReadSink _current;
        private IStageProgressBufferReadSink _stages;
        private IJobRunner _runner;
        private IJobRegisterSystem _register;
        private ILodingRegisterService _loadingRegister;
        private ILodingSystemService _loading;
        private ITutorialViewInputLease _input;
        private ITutorialViewDisplay _display;
        private TutorialViewInputAdapter _inputAdapter;

        private bool _isInitialized;
        private bool _isShutdown;
        private int _contextId;
        private int _lastContextId;

        // 마지막 정리 결과(설계 §8.3). 정리 결과가 없는 세션의 마감은 자원 반환이 한 번이라도 실패했는지로 정한다.
        // 반환 실패는 오류 경계로 런타임을 멈추므로 그 뒤에 새 세션이 시작되지 않는다.
        private int _cleanupSession;
        private bool _cleanupSucceeded;
        private bool _releaseFailed;

        // 개발 로그에 마지막으로 남긴 세션 상태.
        private TutorialSessionSnapshot _loggedSession;

        /// <param name="log">세션 진행을 남기는 개발 로그입니다. 없으면 남기지 않습니다.</param>
        /// <param name="inputSlot">튜토리얼 "다음" 입력의 수신 자리입니다. 초기화 때 입력 어댑터를 연결합니다. 입력을 조립하지 않는 시험에서는 생략합니다.</param>
        /// <param name="autoStartTutorial">화면 연결 때 튜토리얼을 자동 시작할지입니다. 생성 시 고정하며 초기화·정리 수명에는 영향을 주지 않습니다.</param>
        internal TutorialViewSystem(RuntimeFaultBoundary faults, Func<CombatInputWindow> readInputWindow, Action<string> log = null,
            ITutorialInputReceiverSlot inputSlot = null, bool autoStartTutorial = true)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _readInputWindow = readInputWindow ?? throw new ArgumentNullException(nameof(readInputWindow));
            _log = log;
            _inputSlot = inputSlot;
            _autoStartTutorial = autoStartTutorial;
        }

        /// <summary>지금 화면 연결 번호입니다. 연결이 없으면 0입니다.</summary>
        internal int ContextId => _contextId;

        #region 시스템 모듈

        /// <summary>View DB의 튜토리얼 정의 표를 받아 둡니다. 정의 표가 없는 시험 조립의 InitData는 막지 않고 초기화에서 확인합니다.</summary>
        public void InitData(IHasViewQuery data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            _definitions = data.ViewQuery?.TutorialDefinitionTableQuery;
        }

        /// <summary>정의 표를 직접 받습니다(시험 입구, <see cref="BindServices"/>와 같은 쓰임).</summary>
        internal void BindDefinitions(ITutorialDefinitionTableQuery definitions)
        {
            _definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null)
                throw new ArgumentNullException(nameof(resolver));

            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            var global = resolver.GetService(SystemGroupsService.Global);
            BindServices(
                buffers.TutorialStore,
                buffers.TutorialStateReadSink,
                buffers.TutorialRequestWriteSink,
                buffers.CombatStateReadSink,
                buffers.CurrentProgressReadSink,
                buffers.StageProgressReadSink,
                global.JobServiceCommand.JobRunner,
                global.JobServiceCommand.JobRegister,
                global.LodingServiceCommand.LodingRegisterService,
                global.LodingServiceCommand.LodingService,
                new TutorialViewInputLease(global.InputServiceCommand.InputLayerServices,
                    global.InputServiceCommand.InputLayerOverrides, new TutorialViewEventSystemSelection()),
                new TutorialViewUIDisplay(buffers.CharacterReadSink, buffers.CombatStateReadSink,
                    buffers.TutorialStateReadSink, _readInputWindow));
        }

        /// <summary>필요한 Buffer·Global 서비스와 표시·입력 부품을 보관합니다(시험 입구이기도 합니다).</summary>
        internal void BindServices(
            ITutorialBufferStore store,
            ITutorialStateBufferReadSink state,
            ITutorialRequestBufferWriteSink requests,
            ICombatStateBufferReadSink combat,
            ICurrentProgressBufferReadSink current,
            IStageProgressBufferReadSink stages,
            IJobRunner runner,
            IJobRegisterSystem register,
            ILodingRegisterService loadingRegister,
            ILodingSystemService loading,
            ITutorialViewInputLease input,
            ITutorialViewDisplay display)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _register = register ?? throw new ArgumentNullException(nameof(register));
            _loadingRegister = loadingRegister ?? throw new ArgumentNullException(nameof(loadingRegister));
            _loading = loading ?? throw new ArgumentNullException(nameof(loading));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _display = display ?? throw new ArgumentNullException(nameof(display));
            _inputAdapter = new TutorialViewInputAdapter(state, requests, _faults);
        }

        /// <summary>
        /// 정의 표의 각 정의로 공개 정의와 단계 규칙을 만들어 상태 Store에 한 번 넘기고, Combat 씬 연결·해제를 등록합니다(구현 계획 §6).
        /// </summary>
        public void Initialize()
        {
            if (_store == null)
                throw new InvalidOperationException("[TutorialViewSystem] 서비스를 받기 전에 초기화할 수 없습니다.");
            if (_isInitialized)
                throw new InvalidOperationException("[TutorialViewSystem] 한 번만 초기화합니다.");

            if (_definitions == null)
                throw new InvalidOperationException("[TutorialViewSystem] 튜토리얼 정의 표(View DB)를 받기 전에 초기화할 수 없습니다.");

            var infos = new List<TutorialDefinitionInfo>();
            var rules = new List<TutorialDefinitionRules>();
            foreach (TutorialDefinitionData definition in _definitions.GetAll())
            {
                infos.Add(definition.CreateInfo());
                rules.Add(definition.CreateRules());
            }

            _store.Initialize(infos, rules);
            // 순서와 그 이유의 원본은 SceneNodeOrder다.
            _loadingRegister.RegisterSceneEnterLoaded(SceneIndices.Combat, new LodingNode(SceneNodeOrder.TutorialAttach, Attach));
            _loadingRegister.RegisterSceneExitLoaded(SceneIndices.Combat, new LodingNode(SceneNodeOrder.TutorialDetach, Detach));
            // "다음"은 튜토리얼 Layer를 빌린 동안에만 라우팅되므로 런타임 동안 연결해 둔다.
            _inputSlot?.Bind(_inputAdapter);
            _isInitialized = true;
        }

        /// <summary>
        /// 오류·로딩 중이 아니고 Runner가 비었으면 조건 없이 <c>Register(Tutorial, Next)</c>를 부릅니다(설계 §9.3).
        /// 할 일 판단은 TutorialPlan만 합니다. 할 일이 없으면 Runner가 빈 채로 남아 같은 프레임의 전투가 자기 Next를 겁니다.
        /// </summary>
        public void Tick(float deltaTime)
        {
            // 초기화하지 않은 시험 조립에서도 그룹 Tick은 돈다.
            if (!_isInitialized || _isShutdown || _faults.IsFaulted)
                return;

            LogSessionChange();
            if (_loading.IsLoading)
                return;

            if (_contextId != 0)
                _display.RefreshLayout();
            if (!_runner.IsIdle)
                return;

            _register.Register(JobRootType.Tutorial, JobType.Next);
        }

        /// <summary>
        /// 런타임 정지 마감입니다(설계 §10). Tick·Job을 기다리지 않습니다. 제출을 막고, 남은 요청은 Failed(RuntimeStopped)로,
        /// Starting/Running 세션은 Cancelled(RuntimeStopped)로, 이미 Ending인 세션은 고정된 종료 의도로 게시합니다.
        /// </summary>
        public void Shutdown()
        {
            if (_isShutdown)
                return;
            _isShutdown = true;
            if (!_isInitialized)
                return;

            _inputSlot?.Unbind(_inputAdapter);
            List<Exception> errors = ReleaseResources();
            CleanupErrors.Try(_display.Detach, ref errors);
            _contextId = 0;
            _store.StopRuntime();
            // 하나가 실패해도 나머지 마감을 시도한다.
            CleanupErrors.Try(FailPendingRequests, ref errors);
            CleanupErrors.Try(() => CloseActiveSession(TutorialEndReason.RuntimeStopped), ref errors);
            // 그룹 종료가 모듈의 실패를 모아 던진다. 정지 중이라 오류 경계에 다시 보고하지 않는다.
            CleanupErrors.ThrowIfAny(errors, CleanupFailure);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));

            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        #region 화면 연결

        /// <summary>
        /// Combat 화면에 연결합니다. 새 연결 번호를 게시하고, 1-1이면 그 번호로 Start를 제출합니다(설계 §10·§15.12).
        /// 등록은 Tick이 하고, 전투 기준점은 Start를 받아들이는 Job이 세션과 함께 설치합니다(설계 §8.2).
        /// </summary>
        internal void Attach()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("[TutorialViewSystem] 초기화 전에는 화면에 연결할 수 없습니다.");
            if (_isShutdown || _faults.IsFaulted)
                return;
            if (_contextId != 0)
                throw new InvalidOperationException($"[TutorialViewSystem] 이미 화면 연결 {_contextId}에 연결돼 있습니다.");
            if (_lastContextId == int.MaxValue)
                throw new InvalidOperationException("[TutorialViewSystem] 화면 연결 번호를 더 발급할 수 없습니다.");

            // 씬의 튜토리얼 화면을 잡는다(설계 §10). 없으면 설정 오류라 연결하지 않는다.
            _display.Attach();
            _contextId = ++_lastContextId;
            _store.SetContext(_contextId, TutorialUnavailableReason.None);

            // 개발 재생의 선택 기능이다. 정상적인 자동 시작 생략이며, 화면 연결·정리 계약은 유지한다.
            if (!_autoStartTutorial)
            {
                _log?.Invoke($"[Tutorial] 화면 연결 {_contextId}: 개발 설정으로 자동 시작을 건너뜁니다.");
                return;
            }

            TutorialViewTargetMiss miss = TutorialViewTarget.Find(_combat, _current, _stages, _definitions, _state,
                out int tutorialKey);
            if (miss != TutorialViewTargetMiss.None)
            {
                _log?.Invoke($"[Tutorial] 화면 연결 {_contextId}: 실행 대상이 아닙니다({miss}).");
                return;
            }

            if (!_requests.TrySubmit(TutorialCommandData.Start(tutorialKey, _contextId), out int requestNumber,
                    out TutorialSubmitFailure failure))
                throw new InvalidOperationException($"[TutorialViewSystem] Start를 제출하지 못했습니다: {failure}");

            _log?.Invoke($"[Tutorial] 화면 연결 {_contextId}: 정의 {tutorialKey} Start 요청 {requestNumber}.");
        }

        /// <summary>
        /// 화면 연결을 끊고 Job 없이 바로 마감합니다(설계 §8.2·§10). 빌린 입력·표시를 돌려주고, Starting/Running 세션은 Cancelled(ContextDetached)로,
        /// 이미 Ending인 세션은 고정된 종료 의도로 최종 게시합니다. 그래서 다음 화면의 Runner 검사와 재진입 Start의 Busy에 걸리지 않습니다.
        /// 오류로 멈춘 뒤에는 자원만 돌려주고 세션은 Shutdown이 마감합니다.
        /// 마감 실패는 연결 해제를 막지 않고 오류 경계에 보고합니다. 이미 오류로 멈췄으면 보고가 무시되므로
        /// 던져서 기록되게 합니다(<see cref="CombatViewSystem.Detach"/>와 같음).
        /// </summary>
        internal void Detach()
        {
            if (!_isInitialized || _isShutdown)
                return;

            List<Exception> errors = ReleaseResources();
            CleanupErrors.Try(_display.Detach, ref errors);
            if (!_faults.IsFaulted)
                CleanupErrors.Try(() => CloseActiveSession(TutorialEndReason.ContextDetached), ref errors);
            if (_contextId != 0)
            {
                _contextId = 0;
                _store.SetContext(0, TutorialUnavailableReason.NoContext);
            }

            if (errors == null)
                return;
            var failure = new AggregateException(CleanupFailure, errors);
            if (_faults.IsFaulted)
                throw failure;
            _faults.Report(failure);
        }

        /// <summary>
        /// 오류 정리 콜백입니다(설계 §10). 빌린 입력과 표시를 돌려줍니다. 세션 상태는 바꾸지 않고 Shutdown이 마감합니다.
        /// 실패는 모아 던지고, 오류 경계의 정리가 함께 보고합니다.
        /// </summary>
        internal void StopInput()
        {
            CleanupErrors.ThrowIfAny(ReleaseResources(), CleanupFailure);
        }

        #endregion

        #region 실행 창구

        public bool IsInputMonopolized => _input.IsMonopolized;

        public bool TryGetCompletedPresentation(int sessionId, int stepRevision, out TutorialViewPresentationKind kind) =>
            _presentation.TryGetCompleted(sessionId, stepRevision, out kind);

        public bool TryGetCleanupResult(int sessionId, out bool succeeded)
        {
            if (sessionId != 0 && _cleanupSession == sessionId)
            {
                succeeded = _cleanupSucceeded;
                return true;
            }

            succeeded = false;
            return false;
        }

        public bool TryBorrowInput()
        {
            if (!_input.TryBorrow())
                return false;

            // 빌리는 동안 다른 입력의 취소 처리에서 오류가 보고되면, 오류 정리는 아직 빌리기 전이라 이 Layer를 돌려주지 못했다.
            // 남기지 않도록 바로 돌려준다. 런타임은 오류로 멈춘다.
            if (_faults.IsFaulted)
                ReturnInput();
            return true;
        }

        /// <summary>돌려주지 못하면 정리 실패로 기록하고 그대로 던집니다. 그 세션은 마감 때 Faulted(CleanupFault)가 됩니다(설계 §8.3).</summary>
        public void ReturnInput()
        {
            if (!_input.IsBorrowed)
                return;

            try
            {
                _input.Return();
            }
            catch
            {
                _releaseFailed = true;
                throw;
            }
        }

        public void BeginPresentation(int sessionId, int stepRevision, int tutorialKey, int stepKey,
            TutorialViewPresentationKind kind)
        {
            var request = new TutorialViewDisplayRequest(_definitions.Get(tutorialKey), stepKey, kind);
            // 새 표시는 앞 표시의 완료 사실을 대신한다.
            int generation = _presentation.Begin(sessionId, stepRevision, kind);
            _display.Present(request, () => _presentation.Complete(generation));
        }

        public void ShowBlockedInputNotice(int sessionId, int stepRevision, int tutorialKey, int stepKey)
        {
            _display.ShowBlockedInputNotice(_definitions.Get(tutorialKey), stepKey);
        }

        /// <summary>
        /// 세션 자원을 돌려주고 결과를 기록합니다. 지금 표시·입력은 그 자리에서 끝나므로 결과도 바로 남깁니다.
        /// 돌려주지 못하면 정리 실패로 기록하고 오류 경계에 보고합니다(설계 §8.3).
        /// </summary>
        public void BeginCleanup(int sessionId)
        {
            List<Exception> errors = ReleaseResources();
            _cleanupSession = sessionId;
            _cleanupSucceeded = !_releaseFailed;
            if (errors != null)
                _faults.Report(new AggregateException(CleanupFailure, errors));
        }

        #endregion

        #region 내부

        /// <summary>빌린 입력·표시·표시 기록을 모두 놓습니다. 하나가 실패해도 나머지를 시도하고 실패 목록을 돌려줍니다.</summary>
        private List<Exception> ReleaseResources()
        {
            _presentation.Forget();
            if (_input == null)
                return null;

            List<Exception> errors = null;
            if (_input.IsBorrowed)
                CleanupErrors.Try(_input.Return, ref errors);
            CleanupErrors.Try(_display.Close, ref errors);
            if (errors != null)
                _releaseFailed = true;
            return errors;
        }

        /// <summary>
        /// 활성 세션을 Job 없이 마감합니다. Starting/Running은 주어진 이유로 끝내고, 이미 Ending이면 고정된 종료 의도를 유지합니다(설계 §8.2).
        /// 그 세션의 정리 결과가 있으면 그것으로, 없으면 자원 반환이 실패했는지로 정하며 실패면 Faulted(CleanupFault)입니다.
        /// 이미 정리를 끝낸 세션을 나중의 다른 실패로 덮지 않습니다.
        /// </summary>
        private void CloseActiveSession(TutorialEndReason reason)
        {
            int sessionId = _state.Context.ActiveSessionId;
            if (sessionId == 0 || !_state.TryGetSession(sessionId, out TutorialSessionSnapshot session))
                return;

            TutorialSessionSnapshot ending = session.Status == TutorialSessionStatus.Ending
                ? session
                : TutorialSessionChanges.Ending(session, reason);
            bool succeeded = _cleanupSession == sessionId ? _cleanupSucceeded : !_releaseFailed;
            _store.InstallSession(TutorialSessionChanges.Final(ending, succeeded));
        }

        /// <summary>남은 요청을 모두 Failed(RuntimeStopped)로 게시합니다(설계 §10).</summary>
        private void FailPendingRequests()
        {
            while (_store.TryGetNextRequest(out TutorialRequest request))
            {
                _store.Publish(TutorialRequestResult.Failed(request.Number, request.Data.Kind, request.Data.SessionId,
                    TutorialReason.RuntimeStopped));
            }
        }

        /// <summary>세션 상태가 바뀌면 개발 로그에 한 줄 남깁니다. 막 끝난 세션의 최종 상태도 한 번 남깁니다.</summary>
        private void LogSessionChange()
        {
            if (_log == null)
                return;

            int sessionId = _state.Context.ActiveSessionId;
            if (sessionId == 0)
                sessionId = _loggedSession?.SessionId ?? 0;
            if (sessionId == 0 || !_state.TryGetSession(sessionId, out TutorialSessionSnapshot session)
                || ReferenceEquals(session, _loggedSession))
                return;

            _loggedSession = session;
            _log($"[Tutorial] 세션 {session.SessionId}: {session.Status} 단계 {session.CurrentStepKey}({session.StepKind}) "
                + $"revision {session.StepRevision} 대기 {session.WaitReason} 종료 {session.EndReason}");
        }

        #endregion
    }
}
