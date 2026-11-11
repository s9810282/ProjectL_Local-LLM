using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>준비 예산을 한 번 사용하고 끝난다. 결과 게시나 다음 Job 등록은 하지 않는다.</summary>
    internal struct CombatPrepareAdvanceJob : IJobService
    {
        private readonly ICombatAdvanceService _advance;
        private readonly ICombatProgressReader _progress;
        private readonly int _baseSequence;
        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatPrepareAdvanceJob(ICombatAdvanceService advance, int priority, ICombatProgressReader progress, int baseSequence)
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
            if (_progress.Read().LatestResultSequence != _baseSequence)
                throw new InvalidOperationException("[CombatPrepareAdvanceJob] 준비 전에 결과 순번이 바뀌었습니다.");
            switch (_advance.PrepareAdvanceSlice())
            {
                case AdvancePreparationState.Preparing:
                case AdvancePreparationState.Ready:
                case AdvancePreparationState.FrameDeferred:
                case AdvancePreparationState.NotRequired:
                    break;
                default:
                    throw new InvalidOperationException("[CombatPrepareAdvanceJob] 준비 호출이 올바른 진행 상태를 반환하지 않았습니다.");
            }
            if (_progress.Read().LatestResultSequence != _baseSequence)
                throw new InvalidOperationException("[CombatPrepareAdvanceJob] 준비 중 전투 결과가 게시됐습니다.");
            IsCompleted = true;
        }
    }
}
