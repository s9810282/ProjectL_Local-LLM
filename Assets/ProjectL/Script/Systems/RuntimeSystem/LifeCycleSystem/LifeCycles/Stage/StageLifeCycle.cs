using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>
    /// 스테이지 맵 구간의 수명입니다. Stage Job을 거는 트리거만 맡고, 무엇을 할지는 <c>StagePlan</c>이 정합니다(검사 후속 정리 계획 TP-R9, 로비 꼴).
    /// </summary>
    /// <remarks>
    /// 진입 때 <see cref="JobType.Start"/>를 한 번 걸고(로스터가 없는 첫 진입이면 Plan이 새 Run 시작을 앞에 붙입니다), 활성 동안 Runner가 비면
    /// <see cref="JobType.Next"/>를 겁니다. 같은 챕터에 다시 들어오면 Core가 그래프를 다시 만들지 않고 진입만 수락합니다.
    /// 이번 방문의 진행 상태는 <see cref="StageProgress"/>가 갖습니다.
    /// </remarks>
    internal sealed class StageLifeCycle : LifeCycleBase, IStageEntryAttemptRead, IStageDeckEditFacts, IStageTraitGrowthFacts
    {
        // 맵 View Detach(-100)보다 먼저 등록을 멈추고 실행 전 취소를 확인합니다.
        internal const int ExitPreparationPriority = -200;
        private readonly RuntimeFaultBoundary _faults;
        private readonly StageDeckEditWindow _deckEditWindow;
        private readonly StageProgress _progress = new(ReportStoppedDomain);
        private ILodingSystemService _loading;
        private IJobRegisterSystem _jobRegister;
        private IJobRunner _jobRunner;
        private ICharacterBufferReadSink _roster;
        private ICurrentProgressBufferReadSink _current;
        private IStageProgressBufferReadSink _stages;
        private ICombatEntryService _combatEntry;
        private ILifeCycleManager _manager;
        private ILodingRegisterService _loadingRegister;
        private bool _isShutdown;

        /// <param name="deckEditWindow">덱 편집 창입니다. 이 수명이 바인딩 때 사실을 넘기고 종료 때 풉니다.</param>
        internal StageLifeCycle(RuntimeFaultBoundary faults, StageDeckEditWindow deckEditWindow)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _deckEditWindow = deckEditWindow ?? throw new ArgumentNullException(nameof(deckEditWindow));
        }

        public bool TryGetCombatEntry(out int mapCommandNumber, out CombatEntryRequest request) =>
            _progress.Attempts.TryGetCombatEntry(out mapCommandNumber, out request);

        protected override void ExportLifeCycleServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageEntryAttemptRead>(this);
            sink.AddService<IStageTraitGrowthFacts>(this);
            sink.AddService<IStageProgress>(_progress);
        }

        public override LifeCycleKey Key => LifeCycleKeys.Stage;

        protected override int SceneIndex => SceneIndices.Stage;

        protected override void OnBindServices(ISystemServiceStoreReader resolver)
        {
            var global = resolver.GetService(SystemGroupsService.Global);
            var jobs = global.JobServiceCommand;
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            BindServices(jobs.JobRegister, jobs.JobRunner, buffers.CharacterReadSink);

            _current = buffers.CurrentProgressReadSink;
            _stages = buffers.StageProgressReadSink;
            var lifeCycles = resolver.GetService(SystemGroupsService.LifeCycle);
            _combatEntry = lifeCycles.CombatEntry;
            _manager = lifeCycles.LifeCycleManager;
            _loadingRegister = global.LodingServiceCommand.LodingRegisterService;
            _loading = global.LodingServiceCommand.LodingService;
            var inGame = resolver.GetService(SystemGroupsService.InGame);
            // 증강 진행은 Core 로스터 서비스에서 읽고(부작용 없음), 처리·준비는 Job이 부릅니다(강화 View 설계 §14 TG6).
            _progress.Bind(_loading, buffers, _combatEntry, inGame.ViewGroup.RecoveryExecution,
                inGame.ViewGroup.TraitGrowthExecution, inGame.CoreGroup.Characters);
            _progress.Halt();
            _deckEditWindow.Bind(this);
        }

        protected override void OnInitialize()
        {
            _isShutdown = false;
            _loadingRegister.RegisterSceneExitLoaded(SceneIndex,
                new LodingNode(ExitPreparationPriority, PrepareForSceneExit));
        }

        protected override void OnEnter()
        {
            if (_isShutdown || _faults.IsFaulted)
                throw new InvalidOperationException("[StageLifeCycle] 종료되거나 오류가 난 런타임은 다시 진입할 수 없습니다.");
            RequireDrainedRunner();
            RegisterEntryJobs();
            _progress.Reset(_faults.IsFaulted);
        }

        /// <summary>Runner가 바빠도 예외가 아닙니다. 빌 때까지 다음 판단을 걸지 않을 뿐입니다(로비와 같음).</summary>
        protected override void OnTick(float deltaTime)
        {
            // 이탈·종료로 멈췄거나 같은 Tick 앞쪽에서 오류가 보고됐으면 새 등록을 하지 않습니다.
            if (_progress.IsHalted || _faults.IsFaulted || !_jobRunner.IsIdle)
                return;
            _jobRegister.Register(JobRootType.Stage, JobType.Next);
        }

        protected override void OnExit() => StopAndCancelEntry();

        protected override void OnShutdown()
        {
            // Runtime 종료는 재Tick하지 않는 정리입니다. 대기 Job 배출이나 공용 Runner.Clear를 요구하지 않습니다.
            _isShutdown = true;
            _deckEditWindow.Unbind();
            StopAndCancelEntry();
            // 종료 뒤에는 전투 진입 시도를 답하지 않습니다(옛 동작과 같음).
            _progress.CloseCombatEntry();
            _loading = null;
            _combatEntry = null;
            _manager = null;
            _current = null;
            _stages = null;
            _loadingRegister = null;
            _jobRegister = null;
            _jobRunner = null;
            _roster = null;
        }

        private void PrepareForSceneExit()
        {
            // 직접 로딩 경로도 View(-100)가 입력을 지우기 전에 확인합니다. 동기 콜백 안에서는 기다리지 않습니다.
            ValidateSceneExit();
            StopAndCancelEntry();
        }

        private void ValidateSceneExit()
        {
            // Shutdown은 등록을 다시 실행하지 않는 정리이며 대기 Job의 배출을 기다리지 않습니다.
            if (_isShutdown)
            {
                return;
            }
            if (_faults.IsFaulted)
            {
                throw new InvalidOperationException("[StageLifeCycle] 오류가 난 런타임은 정상 전환할 수 없습니다.");
            }
            if (IsAcceptedCombatTransition())
            {
                return;
            }
            RequireDrainedRunner();
            if (!TryGetCombatEntry(out _, out var request))
            {
                return;
            }
            if (request.IsAwaitingRegistration || request.IsCancelled || request.IsRejected)
            {
                return;
            }
            throw new InvalidOperationException("[StageLifeCycle] 이번 전투 요청의 취소·배출 또는 수락된 Combat 전환이 필요합니다.");
        }

        private bool IsAcceptedCombatTransition()
        {
            // Transition Job은 Manager의 목적을 먼저 Combat으로 정하고 Load 안에서 Stage를 동기로 나갑니다.
            // 이때 Runner는 busy여도 이번 방문이 건 수락·완료·활성화 요청만 이탈을 허용합니다.
            return TryGetCombatEntry(out int number, out var request) && number > 0 &&
                request.MapCommandNumber == number && ReferenceEquals(request, _combatEntry?.Current) &&
                request.IsActivating &&
                request.Result != null && request.Result.Accepted && _manager != null &&
                _manager.HasActiveLifeCycle && _manager.CurrentKey.Equals(LifeCycleKeys.Combat);
        }

        private void StopAndCancelEntry()
        {
            _progress.Halt();
            if (TryGetCombatEntry(out _, out var request) && (request.IsAwaitingRegistration || request.IsQueued))
                _combatEntry?.CancelEntry(request);
            // 입력 삭제는 View가 맡습니다. 그때까지 같은 요청 참조를 유지합니다.
        }

        private void RequireDrainedRunner()
        {
            if (_jobRunner == null || !_jobRunner.IsIdle)
                throw new InvalidOperationException("[StageLifeCycle] 등록된 Job을 배출한 뒤 씬을 전환해야 합니다.");
        }

        private bool IsStageActive =>
            _manager != null && _manager.HasActiveLifeCycle && _manager.CurrentKey.Equals(LifeCycleKeys.Stage);

        // 증강 선택 화면이 읽는 사실입니다(강화 View 설계 §14 TG2). Stage가 활성일 때만 진행을 답합니다.
        bool IStageTraitGrowthFacts.IsPending => IsStageActive && _progress.IsGrowthPending;

        // 덱 편집 창이 읽는 사실입니다. 창은 Bind 뒤에만 부르며, 판단 순서는 StageDeckEditWindow가 가집니다.
        bool IStageDeckEditFacts.IsFaulted => _faults.IsFaulted;
        bool IStageDeckEditFacts.IsStageActive => IsStageActive;
        bool IStageDeckEditFacts.IsLoading => _loading != null && _loading.IsLoading;
        bool IStageDeckEditFacts.IsRunnerIdle => _jobRunner != null && _jobRunner.IsIdle;
        bool IStageDeckEditFacts.IsFlowStopped => _progress.IsStopped;
        bool IStageDeckEditFacts.IsMapPrepared => _current != null && _progress.IsMapReady;
        bool IStageDeckEditFacts.HasRoster => _roster != null && _roster.Owned.Count > 0;
        bool IStageDeckEditFacts.HasPendingFlowCheck => _progress.HasPendingCheck;
        bool IStageDeckEditFacts.HasUnregisteredRequest => _current != null && _progress.HasUnregisteredRequest;
        // 노드는 들어갈 때 선택되고 ResolveStage에서 클리어됩니다. 완료된 상점·회복 기록은 잠금이 아닙니다(RD-0 F1).
        bool IStageDeckEditFacts.HasOpenInteraction =>
            _current != null && _stages != null && _current.HasSelection &&
            !_stages.IsCleared(_current.CurrentMapKey, _current.CurrentStage.NodeId);

        public bool IsGrowthStopped => _progress.IsGrowthStopped;

        // 처리 기록 누락은 해당 도메인의 정지와 진단으로 끝냅니다. Job·서비스 예외는 기존 공용 오류 경계를 따릅니다.
        private static void ReportStoppedDomain(string message) => Debug.LogError(message);

        /// <summary>맵 진입에 필요한 실행 경계와 로스터 조회만 보관합니다.</summary>
        internal void BindServices(IJobRegisterSystem jobRegister, IJobRunner jobRunner,
            ICharacterBufferReadSink roster)
        {
            _jobRegister = jobRegister ?? throw new ArgumentNullException(nameof(jobRegister));
            _jobRunner = jobRunner ?? throw new ArgumentNullException(nameof(jobRunner));
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        /// <summary>맵 준비를 등록합니다. 보드 준비는 최초 구간 진입 전에 완료됩니다.</summary>
        internal void RegisterEntryJobs()
        {
            if (_jobRegister == null || _jobRunner == null || _roster == null)
                throw new InvalidOperationException("[StageLifeCycle] Job services must be bound before entering.");

            // 새 Run이 필요한지(로스터 없음)는 StagePlan이 등록 순간에 판단합니다(검사 후속 정리 계획 TP-R8).
            _jobRegister.Register(JobRootType.Stage, JobType.Start);
        }
    }
}
