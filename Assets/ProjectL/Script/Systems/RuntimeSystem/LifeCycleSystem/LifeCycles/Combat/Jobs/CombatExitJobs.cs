using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    // 전투 이탈 연쇄의 Job들입니다(재구성 설계 J7·J9). 각 Job은 호출 한 번과 그 결과 확인만 하고,
    // 다음에 무엇을 할지는 CombatPlan이 정합니다. 확인이 어긋나면 예외로 런타임 오류 경계(F1)에 맡깁니다.

    /// <summary>새 입력 제출만 닫습니다. 진행 중인 재생과 제출된 명령은 그대로 둡니다.</summary>
    internal struct CombatSuspendJob : IJobService
    {
        private readonly ICombatProgressView _view;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatSuspendJob(ICombatProgressView view, int priority)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            _view.SuspendSubmissions();
            IsCompleted = true;
        }
    }

    /// <summary>
    /// 완료가 이번 진입의 대상인지, 일반 승패라면 최종 결과와 맞는지 확인한 뒤 완료를 한 번 표시합니다.
    /// 포기 완료는 일반 결과를 게시하지 않으므로 최종 결과 대조를 하지 않습니다(골격 설계 §6.5).
    /// </summary>
    internal struct CombatCompletionJob : IJobService
    {
        private readonly ICombatProgressReader _progress;
        private readonly ICombatProgressView _view;
        private readonly CombatEntryRequest _entry;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatCompletionJob(ICombatProgressReader progress, ICombatProgressView view, CombatEntryRequest entry,
            int priority)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _entry = entry;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            CombatProgressFacts facts = _progress.Read();
            BattleCompletionChanges completion = RequireCompletion(in facts, _entry);
            _view.PresentCompletion(completion);
            IsCompleted = true;
        }

        /// <summary>표시 전에 같은 완료 대상·최종 결과의 계약을 확인합니다. 정리 뒤에도 Core가 완료·최신 결과·순번을 남기므로 그대로 성립합니다.</summary>
        internal static BattleCompletionChanges RequireCompletion(in CombatProgressFacts facts, CombatEntryRequest entry)
        {
            BattleCompletionChanges completion = facts.Completion
                ?? throw new InvalidOperationException("[CombatCompletionJob] 표시할 전투 완료가 없습니다.");
            RequireTarget(completion, entry);
            if (!CombatPlan.IsAbandonment(completion))
                RequireFinalResult(completion, facts);
            return completion;
        }

        private static void RequireTarget(BattleCompletionChanges completion, CombatEntryRequest entry)
        {
            var progress = completion.Progression;
            if (entry == null || !entry.IsTargetOf(progress.ChapterKey, progress.NodeId.Value, progress.StageKey.Value))
                throw new InvalidOperationException("[CombatCompletionJob] 현재 진입과 완료의 대상이 다릅니다.");
        }

        private static void RequireFinalResult(BattleCompletionChanges completion, in CombatProgressFacts facts)
        {
            CombatResult result = facts.LatestResult;
            if (result == null || !result.Accepted || result.Sequence != facts.LatestResultSequence ||
                !result.Outcome.HasValue || !result.Outcome.Value.Equals(completion.Outcome))
                throw new InvalidOperationException("[CombatCompletionJob] 확인한 최종 결과와 완료가 다릅니다.");
        }
    }

    /// <summary>
    /// Core에 EndBattle을 한 번 요청한 뒤 다음 판단이 기대는 사실만 확인합니다(개선 설계 §5.2).
    /// 전투는 사라지고, 결과 순번 S는 판단 때 그대로여야 합니다(다음 판단의 "남은 연출" 조건이 S·P에 기댑니다).
    /// 판단한 Job(<see cref="CombatNextJob"/>) 안에서 바로 이어 실행되므로 정리 전 대상은 다시 확인하지 않습니다.
    /// </summary>
    internal struct CombatEndBattleJob : IJobService
    {
        private readonly IBattleCompletionService _completion;
        private readonly ICombatProgressReader _progress;
        private readonly int _expectedSequence;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        /// <param name="expectedSequence">정리를 판단할 때의 결과 순번 S입니다.</param>
        internal CombatEndBattleJob(IBattleCompletionService completion, ICombatProgressReader progress,
            int expectedSequence, int priority)
        {
            _completion = completion ?? throw new ArgumentNullException(nameof(completion));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _expectedSequence = expectedSequence;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            _completion.EndBattle();

            CombatProgressFacts after = _progress.Read();
            if (after.HasBattle || after.LatestResultSequence != _expectedSequence)
                throw new InvalidOperationException(
                    $"[CombatEndBattleJob] EndBattle 뒤 전투가 남았거나 결과 순번이 바뀌었습니다(판단 때 {_expectedSequence}, 지금 {after.LatestResultSequence}).");
            IsCompleted = true;
        }
    }

    /// <summary>
    /// 보스 승리를 정리한 뒤 챕터 완료를 한 번 요청합니다. Core는 이 뒤에 Run 종료(최종 챕터)와 요약을 확정하므로,
    /// 결과 화면보다 먼저 실행됩니다(개선 설계 §4.1). 보스 승리일 때만 등록되며, 거절은 연결 정의 없음까지 모두 데이터 오류입니다.
    /// </summary>
    internal struct CombatChapterCompletionJob : IJobService
    {
        private readonly IChapterProgressionCoordinator _chapters;
        private readonly BattleCompletionChanges _completion;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatChapterCompletionJob(IChapterProgressionCoordinator chapters, BattleCompletionChanges completion,
            int priority)
        {
            _chapters = chapters ?? throw new ArgumentNullException(nameof(chapters));
            _completion = completion ?? throw new ArgumentNullException(nameof(completion));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            ChapterProgressionResult progress = _completion.Progression;
            // 마지막 챕터 여부는 Core가 챕터(맵) 데이터로 판정한다. 결과는 Run 진행 Buffer로 게시되고, 떠날 때 그 값으로 목적지를 정한다.
            ChapterContinuationResult chapter = _chapters.CompleteChapter(progress.ChapterKey, progress.NodeId);
            if (!chapter.Accepted)
                throw new InvalidOperationException(
                    $"[CombatChapterCompletionJob] 챕터 완료가 거절됐습니다({chapter.FailureReason}).");
            IsCompleted = true;
        }
    }

    /// <summary>이탈 연쇄의 마지막입니다. Plan이 Run 결과로 정한 목적지로 전환을 요청합니다.</summary>
    internal struct CombatTransitionJob : IJobService
    {
        private readonly ILifeCycleManager _manager;
        private readonly LifeCycleKey _destination;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatTransitionJob(ILifeCycleManager manager, LifeCycleKey destination, int priority)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _destination = destination;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            _manager.Change(_destination);
            IsCompleted = true;
        }
    }

    /// <summary>
    /// Combat 구간을 나간 뒤의 사후 확인입니다(설계 J9, D1-a). 전투가 남은 채 나갔을 때만 등록됩니다(정상 이탈은 나가는 순간 요청을 닫음).
    /// 전투가 그대로면 정리 순서를 건너뛴 전환이므로 예외이고, 그 사이 정리됐으면 현재 진입 요청을 닫습니다.
    /// </summary>
    internal struct CombatExitCheckJob : IJobService
    {
        private readonly ICombatProgressReader _progress;
        private readonly CombatEntryRequest _entry;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatExitCheckJob(ICombatProgressReader progress, CombatEntryRequest entry, int priority)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _entry = entry;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            if (_progress.Read().HasBattle)
                throw new InvalidOperationException(
                    "[CombatExitCheckJob] 전투를 정리하지 않고 Combat을 나갔습니다. 이탈은 전투 진행 연쇄를 거쳐야 합니다.");
            _entry?.Close();
            IsCompleted = true;
        }
    }
}
