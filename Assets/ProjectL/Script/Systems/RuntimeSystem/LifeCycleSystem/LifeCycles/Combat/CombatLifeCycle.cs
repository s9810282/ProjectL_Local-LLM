using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>
    /// 스테이지 내부(배치·전투) 구간의 수명입니다. 들어오고 나갈 때의 순서만 맡고,
    /// 무엇을 언제 처리할지는 <see cref="CombatFlow"/>가 정합니다.
    /// </summary>
    /// <remarks>
    /// 진입 서비스가 대상과 수락 결과를 보관한 뒤 시작 전환을 요청한다. 실행 모드·오류 경계는 조립 때 받아
    /// Flow에 그대로 전달한다. 정상 종료의 F2 순서는 CombatExitSequence에 위임한다.
    /// </remarks>
    internal sealed class CombatLifeCycle : LifeCycleBase, ICombatEntryService, ICombatExitService, ISystemTick, ILifeCycleTransitionGuard
    {
        private ICombatViewBufferWriteSink _combatViewWrite;
        private ICombatStateBufferReadSink _combatStateRead;
        private CombatFlow _flow;
        private ICurrentProgressBufferReadSink _current;
        private IJobRunner _runner;
        private IJobRegisterSystem _register;
        private ILifeCycleManager _manager;
        private ILodingSystemService _loading;
        private CombatEntryRequest _entry;
        private CombatExitSequence _exit;
        private bool _ready;

        public bool IsActive => _entry?.Status == CombatEntryStatus.Active && !FaultBoundary.IsFaulted && _ready;

        internal CombatLifeCycle(CombatExecutionMode executionMode, RuntimeFaultBoundary faultBoundary)
        {
            if (!System.Enum.IsDefined(typeof(CombatExecutionMode), executionMode))
                throw new ArgumentOutOfRangeException(nameof(executionMode), executionMode, null);

            ExecutionMode = executionMode;
            FaultBoundary = faultBoundary ?? throw new ArgumentNullException(nameof(faultBoundary));
        }

        /// <summary>조립 때 고정한 실행 모드입니다. 런타임 안에서 바꾸지 않습니다.</summary>
        internal CombatExecutionMode ExecutionMode { get; }

        /// <summary>런타임 공통 오류 경계입니다. Bootstrap·View와 같은 인스턴스입니다.</summary>
        internal RuntimeFaultBoundary FaultBoundary { get; }

        public override LifeCycleKey Key => LifeCycleKeys.Combat;

        protected override int SceneIndex => SceneIndices.Combat;

        protected override void OnBindServices(ISystemServiceStoreReader resolver)
        {
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            IJobSystemServiceContext jobs = resolver.GetService(SystemGroupsService.Global).JobServiceCommand;

            // IBufferQuery는 모든 싱크를 노출하므로 이 구간에 필요한 것만 꺼내 보관한다.
            _combatViewWrite = buffers.CombatViewWriteSink;
            _combatStateRead = buffers.CombatStateReadSink;
            _current = buffers.CurrentProgressReadSink;
            _runner = jobs.JobRunner;
            _register = jobs.JobRegister;
            _manager = resolver.GetService(SystemGroupsService.LifeCycle).LifeCycleManager;
            _loading = resolver.GetService(SystemGroupsService.Global).LodingServiceCommand.LodingService;
            _flow = new CombatFlow(
                buffers.CombatStateReadSink,
                buffers.CombatResultReadSink,
                buffers.CombatViewReadSink,
                jobs.JobRunner,
                jobs.JobRegister,
                ExecutionMode,
                FaultBoundary);
            _flow.Stop();
            var inGame = resolver.GetService(SystemGroupsService.InGame);
            _exit = new CombatExitSequence(buffers.CombatStateReadSink, buffers.CombatResultReadSink,
                buffers.CharacterReadSink, buffers.CombatViewReadSink, _runner, inGame.CoreGroup.Combat,
                inGame.CoreGroup.ChapterProgression, FaultBoundary, () => _entry, () => inGame.ViewGroup?.Combat,
                _flow.Stop, () => _register.Register(JobRootType.Combat, JobType.NewRun), _manager.Change);
        }

        protected override void OnInitialize() => _ready = true;

        protected override void ExportLifeCycleServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatEntryService>(this);
            sink.AddService<ICombatExitService>(this);
        }

        public CombatExitStatus ExitStatus => _exit?.Status ?? CombatExitStatus.Idle;

        public bool RequestNormalExit(LifeCycleKey destination) => _ready && _exit.Request(destination);

        /// <summary>
        /// 포기는 항상 Run 종료라 목적지는 Run 종료 목적지 한 곳에서 정합니다(설계 §6.5 K22).
        /// 기록 모드는 처리·결과가 없는 진단 경로라 받지 않습니다.
        /// </summary>
        public bool RequestAbandon() =>
            _ready && ExecutionMode == CombatExecutionMode.Live && _exit.RequestAbandon(RunEndDestination);

        /// <summary>Run 종료(패배·최종 승리·포기) 뒤 목적지입니다. Title 화면이 생기면 바꿉니다(설계 K21).</summary>
        private static LifeCycleKey RunEndDestination => LifeCycleKeys.Stage;

        void ILifeCycleTransitionGuard.ValidateTransition(LifeCycleKey? destination)
        {
            if (_exit == null)
                throw new InvalidOperationException("Combat 이탈 경계가 초기화되지 않았습니다.");
            _exit.ValidateTransition(destination);
        }

        public CombatEntryRequest RequestEntry(int chapterKey, StageNodeId nodeId)
        {
            var request = new CombatEntryRequest(chapterKey, nodeId);
            if (!_ready)
                throw new InvalidOperationException("CombatLifeCycle: 초기화 이후에 진입을 요청해야 합니다.");
            if (FaultBoundary.IsFaulted)
            {
                request.Fail();
                return request;
            }
            if (_entry != null)
            {
                if (_entry.Status == CombatEntryStatus.Queued && _entry.ChapterKey == chapterKey && _entry.NodeId == nodeId)
                    return _entry;
                throw new InvalidOperationException("CombatLifeCycle: 이전 요청을 취소·배출하거나 전투 이탈을 완료해야 합니다.");
            }
            if (!_runner.IsIdle || _loading.IsLoading || _flow.HasPendingCheck)
                throw new InvalidOperationException("CombatLifeCycle: 진행 중인 작업이나 로딩이 있습니다.");

            _flow.Reset();
            _flow.ValidateEntryRegistration();
            _exit.Reset();
            _entry = request;
            try
            {
                _register.RegisterCombatEntry(request);
            }
            catch
            {
                request.Fail();
                throw;
            }
            return request;
        }

        public bool CancelEntry(CombatEntryRequest request)
        {
            return request != null && ReferenceEquals(_entry, request) && request.TryCancel();
        }

        /// <summary>모듈 Tick은 비활성 Combat에서도 진입 결과만 확인합니다. 전투 Flow의 Tick은 Manager가 맡습니다.</summary>
        void ISystemTick.Tick(float deltaTime)
        {
            if (!_ready || _entry == null)
                return;
            if (FaultBoundary.IsFaulted)
            {
                _entry.Fail();
                return;
            }
            if (_entry.Status == CombatEntryStatus.Activating || _entry.Status == CombatEntryStatus.Active || !_runner.IsIdle)
                return;

            try
            {
                if (!_entry.IsJobCompleted)
                    throw new InvalidOperationException("CombatLifeCycle: Runner가 비었지만 진입 Job의 결과가 없습니다.");
                if (_entry.Status == CombatEntryStatus.Rejected || _entry.Status == CombatEntryStatus.Cancelled)
                {
                    _entry = null;
                    return;
                }
                _entry.BeginActivation();
                _manager.Change(LifeCycleKeys.Combat);
            }
            catch
            {
                _entry?.Fail();
                throw;
            }
        }

        protected override void OnEnter()
        {
            try
            {
                if (_entry == null || _entry.Result == null || !_entry.Result.Accepted ||
                    _entry.Status != CombatEntryStatus.Activating || !_entry.IsJobCompleted ||
                    _current.CurrentMapKey != _entry.ChapterKey || !_current.HasSelection ||
                    _current.CurrentStage.NodeId != _entry.NodeId.Value || _current.CurrentStage.StageKey != _entry.Result.StageKey.Value)
                    throw new InvalidOperationException("CombatLifeCycle: 이번 요청의 수락 없이 Combat을 활성화할 수 없습니다.");
                // 지난 전투의 명령·미리보기를 버린다. 번호 카운터와 연출 확인 값은 Buffer가 유지한다.
                _combatViewWrite.Clear();
                _flow.ActivatePreparedBattle(_entry);
                _entry.Activate();
            }
            catch
            {
                _entry?.Fail();
                throw;
            }
        }

        protected override void OnTick(float deltaTime)
        {
            _flow.Tick();
            RequestStageReturnAfterCompletion();
            _exit.Tick();
        }

        /// <summary>
        /// Core가 기록한 승패의 결과 표시와 StageSelect 복귀를 시작합니다(설계 §6.4 K21).
        /// Run 종료 여부와 새 Run 준비는 이탈 순서가 EndBattle 뒤 Core 값으로 정합니다.
        /// </summary>
        private void RequestStageReturnAfterCompletion()
        {
            // Flow의 처리 확인을 먼저 마친다. 오류·기록 모드·이미 시작한 이탈은 새 요청을 만들지 않는다.
            if (ExecutionMode != CombatExecutionMode.Live || FaultBoundary.IsFaulted ||
                _flow.IsStopped || _flow.HasPendingCheck || ExitStatus != CombatExitStatus.Idle)
                return;

            var completion = _combatStateRead.Completion;
            // 포기 Completion은 RequestAbandon 경로(K22)가 이미 처리 중이라 여기서 제외한다. Title 화면이 생기면 Run 종료 목적지만 바꾼다.
            if (completion != null && completion.Outcome.Reason != StageOutcomeReason.VoluntaryAbandonment)
                RequestNormalExit(LifeCycleKeys.Stage);
        }

        protected override void OnExit()
        {
            // 정상 전환은 F2에서 이미 정리했다. Runtime 전체 종료에서도 최종 참조만 놓는다.
            // Global Runner는 다른 구간과 함께 쓰므로 여기서 비우지 않는다(D7).
            _flow.Stop();
            _combatViewWrite.Clear();
            _entry?.Close();
            _entry = null;
        }

        protected override void OnShutdown()
        {
            _ready = false;
            _exit?.Reset();
            _flow?.Stop();
            if (_entry != null && !_entry.TryCancel())
                _entry.Fail();
            _entry = null;
        }
    }
}
