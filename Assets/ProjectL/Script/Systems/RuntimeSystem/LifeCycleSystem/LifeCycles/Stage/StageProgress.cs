using System;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>
    /// 이번 Stage 방문의 진행 상태(전투 진입 시도·도메인 멈춤·전체 정지)와 그 사실 읽기입니다(검사 후속 정리 계획 TP-R9).
    /// 무엇을 할지는 <c>StagePlan.Decide</c>가, Core 호출은 Job이 합니다. 옛 Stage Flow 다섯이 나눠 들던 상태를 한 곳에 둡니다.
    /// </summary>
    internal sealed class StageProgress : IStageProgress
    {
        private readonly StageEntryAttempts _attempts = new();
        private readonly Action<string> _report;
        private ILodingSystemService _loading;
        private IStageViewBufferReadSink _mapInput;
        private IShopViewBufferReadSink _shopInput;
        private IStageCommandResultReadSink _results;
        private ICurrentProgressBufferReadSink _current;
        private IStageProgressBufferReadSink _stages;
        private ICombatEntryService _combatEntry;
        private IRecoveryRequestExecution _recovery;
        private ITraitGrowthRequestExecution _growthRequests;
        private StageTraitGrowthProgress _growth;
        private bool _halted = true;
        private bool _entryStopped;
        private bool _shopStopped;
        private bool _recoveryStopped;
        private bool _growthStopped;

        /// <param name="report">도메인을 멈출 때 남길 진단입니다.</param>
        internal StageProgress(Action<string> report)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        /// <summary>전투 진입 시도의 읽기 입구입니다(맵 View·씬 이탈 검사).</summary>
        internal IStageEntryAttemptRead Attempts => _attempts;

        /// <summary>LifeCycle이 이탈·종료로 등록을 멈췄는지입니다. 방문을 새로 시작할 때 풀립니다.</summary>
        internal bool IsHalted => _halted;

        /// <summary>전체가 멈췄거나 도메인 하나가 멈췄는지입니다(덱 편집 창).</summary>
        internal bool IsStopped => _halted || _entryStopped || _shopStopped || _recoveryStopped || _growthStopped;

        /// <summary>증강 도메인이 멈췄는지입니다(증강 화면).</summary>
        internal bool IsGrowthStopped => _growthStopped;

        /// <summary>여러 Tick을 기다리는 확인(전투 진입 시도)이 남았는지입니다(덱 편집 창).</summary>
        internal bool HasPendingCheck => _attempts.HasAttempt;

        /// <summary>
        /// 제출됐지만 아직 걸지 않은 맵·상점·회복·증강 요청이 있는지입니다(덱 편집 창). 증강은 제출된 선택만 넣고 "선택 필요" 상태는 넣지 않습니다.
        /// </summary>
        internal bool HasUnregisteredRequest =>
            _mapInput.TryGetNextCommand(_results.HandledMapCommandNumber, out _) || NextShopNumber() > 0 ||
            _recovery.CanReserveExecution || _growthRequests.CanReserveExecution;

        /// <summary>맵 그래프가 준비됐는지입니다.</summary>
        internal bool IsMapReady => _current.HasMap && _stages.GetStages(_current.CurrentMapKey).Count > 0;

        /// <summary>증강 선택이 남았는지입니다(증강 화면의 Stage 사실).</summary>
        internal bool IsGrowthPending => _growth.IsPending;

        internal void Bind(ILodingSystemService loading, IBufferQuery buffers, ICombatEntryService combatEntry,
            IRecoveryRequestExecution recovery, ITraitGrowthRequestExecution growthRequests, ITraitGrowthAvailabilityRead growthProgress)
        {
            if (buffers == null) throw new ArgumentNullException(nameof(buffers));
            _loading = loading ?? throw new ArgumentNullException(nameof(loading));
            _combatEntry = combatEntry ?? throw new ArgumentNullException(nameof(combatEntry));
            _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
            _growthRequests = growthRequests ?? throw new ArgumentNullException(nameof(growthRequests));
            _growth = new StageTraitGrowthProgress(growthProgress, buffers);
            _mapInput = buffers.StageViewReadSink;
            _shopInput = buffers.ShopViewReadSink;
            _results = buffers.StageCommandResults;
            _current = buffers.CurrentProgressReadSink;
            _stages = buffers.StageProgressReadSink;
        }

        /// <summary>방문을 새로 시작합니다. 시도·멈춤·증강 읽기를 비우고, 런타임이 오류 상태면 정지한 채 둡니다.</summary>
        internal void Reset(bool faulted)
        {
            _attempts.Clear();
            _growth.Reset();
            _entryStopped = false;
            _shopStopped = false;
            _recoveryStopped = false;
            _growthStopped = false;
            _halted = faulted;
        }

        /// <summary>이탈·종료 때 등록을 멈춥니다. 예약 전 회복 요청은 취소하고, 증강 입력은 취소하지 않습니다(취소 API 없음).</summary>
        internal void Halt()
        {
            _halted = true;
            _recovery?.CancelPending();
        }

        public StageProgressFacts Read()
        {
            if (_loading == null)
                throw new InvalidOperationException("[StageProgress] 서비스를 받기 전에 읽을 수 없습니다.");
            if (_loading.IsLoading)
                return new StageProgressFacts(isLoading: true);

            string problem = null;
            StageCombatEntryState combatEntry = _entryStopped ? StageCombatEntryState.None
                : _attempts.Check(_results.HandledMapCommandNumber, _mapInput, out problem);
            bool hasMapCommand = _mapInput.TryGetNextCommand(_results.HandledMapCommandNumber, out StageViewCommand command);
            // 미처리 입력이 없으면 맵 목록 조회와 읽기 전용 래퍼 생성을 피합니다.
            bool isMapReady = hasMapCommand && IsMapReady;
            bool canReserveGrowth = _growthRequests.CanReserveExecution;
            return new StageProgressFacts(
                combatEntry: combatEntry,
                combatEntryProblem: combatEntry == StageCombatEntryState.Broken ? problem : null,
                isEntryStopped: _entryStopped,
                isShopStopped: _shopStopped,
                isRecoveryStopped: _recoveryStopped,
                isGrowthStopped: _growthStopped,
                hasMapCommand: hasMapCommand,
                mapCommand: command,
                isMapReady: isMapReady,
                isCombatNode: isMapReady && IsCombatNode(command),
                shopCommandNumber: NextShopNumber(),
                canReserveRecovery: _recovery.CanReserveExecution,
                canReserveGrowth: canReserveGrowth,
                // 전투 진입 시도가 다른 일을 막는 동안에는 Core 진행을 읽지 않습니다(옛 Flow와 같음).
                growthNeedsOffer: combatEntry == StageCombatEntryState.None && !_growthStopped && !canReserveGrowth &&
                    _growth.NeedsOffer);
        }

        public void BeginCombatEntry(StageViewCommand command)
        {
            CombatEntryRequest request = _combatEntry.RequestEntry(command.Data.ChapterKey, command.Data.NodeId, command.Number);
            _attempts.Begin(command.Number, request);
        }

        public void CloseCombatEntry() => _attempts.Clear();

        public void Stop(StageDomain domain, string reason)
        {
            switch (domain)
            {
                case StageDomain.Entry: _entryStopped = true; break;
                case StageDomain.Shop: _shopStopped = true; break;
                case StageDomain.Recovery: _recoveryStopped = true; break;
                case StageDomain.Growth: _growthStopped = true; break;
                default: throw new ArgumentOutOfRangeException(nameof(domain), domain, null);
            }
            _report(reason);
        }

        public IJobService CreateCheck(StageDomain domain, Func<bool> isStillPending, string reason) =>
            new StageCheckJob(this, domain, isStillPending, reason);

        public IJobService CreateGrowthOfferCheck()
        {
            StageTraitGrowthProgress.ProgressKey before = _growth.ReadKey();
            // 준비 뒤에도 원본이 그대로면 같은 준비를 되풀이하게 되므로 멈춥니다.
            return new StageCheckJob(this, StageDomain.Growth, () => !_growth.HasChangedSince(before),
                "[StageGrowth] 준비 Job 뒤에도 증강 제안이 생기지 않았습니다.");
        }

        /// <summary>Core가 처리하지 않은 첫 상점 명령 번호입니다. 없으면 0입니다.</summary>
        private int NextShopNumber() =>
            _shopInput.TryGetNextCommand(0, out ShopViewCommand command) && command.Number > _results.HandledShopCommandNumber
                ? command.Number : 0;

        /// <summary>맵 명령의 노드가 전투 노드인지입니다. 상점·미지원·없는 노드는 노드 처리 Job으로 Core의 정상 거절·수락에 닿습니다.</summary>
        private bool IsCombatNode(StageViewCommand command)
        {
            var nodes = _stages.GetStages(command.Data.ChapterKey);
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node.NodeId == command.Data.NodeId.Value && node.StageKey != 0 && node.StageType == StageType.Combat)
                    return true;
            }
            return false;
        }
    }
}
