using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>준비된 행동을 한 번만 시도한다. 무효화나 거절 뒤 재준비는 다음 프레임의 Plan이 고른다.</summary>
    internal struct CombatPreparedAdvanceJob : IJobService
    {
        private readonly ICombatAdvanceService _advance;
        private readonly ICombatProgressReader _progress;
        private readonly int _baseSequence;
        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatPreparedAdvanceJob(ICombatAdvanceService advance, int priority, ICombatProgressReader progress, int baseSequence)
        {
            _advance = advance ?? throw new ArgumentNullException(nameof(advance));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            if (baseSequence < 0) throw new ArgumentOutOfRangeException(nameof(baseSequence));
            _baseSequence = baseSequence;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted) return;
            var before = _progress.Read();
            if (before.LatestResultSequence != _baseSequence)
                throw new InvalidOperationException("[CombatPreparedAdvanceJob] 실행 전에 결과 순번이 바뀌었습니다.");
            if (before.Preparation == AdvancePreparationState.FrameDeferred)
            {
                IsCompleted = true;
                return;
            }
            var disposition = _advance.AdvancePreparedCombat();
            var after = _progress.Read();
            switch (disposition)
            {
                case PreparedAdvanceResult.Published:
                    var result = after.LatestResult;
                    if (after.LatestResultSequence != checked(_baseSequence + 1) || result == null ||
                        result.Sequence != after.LatestResultSequence || result.Kind != CombatProcessingKind.Advance || !result.Accepted)
                        throw new InvalidOperationException("[CombatPreparedAdvanceJob] 수락된 자동 진행 결과가 정확히 한 번 게시되지 않았습니다.");
                    break;
                case PreparedAdvanceResult.Stale:
                case PreparedAdvanceResult.RetryPreparation:
                case PreparedAdvanceResult.Cancelled:
                    if (after.LatestResultSequence != _baseSequence)
                        throw new InvalidOperationException("[CombatPreparedAdvanceJob] 미게시 실행에서 결과 순번이 바뀌었습니다.");
                    break;
                default:
                    throw new InvalidOperationException("[CombatPreparedAdvanceJob] 알 수 없는 준비 실행 결과입니다.");
            }
            IsCompleted = true;
        }
    }
}
