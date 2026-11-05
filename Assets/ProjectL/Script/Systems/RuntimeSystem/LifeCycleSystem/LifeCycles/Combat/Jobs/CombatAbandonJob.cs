using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// 남은 처리·표시가 끝난 뒤 Core에 자발적 포기를 한 번 요청합니다(골격 설계 §6.5 K22, 재구성 설계 J8).
    /// 호출 뒤에는 다음 판단이 기대는 포기 완료 기록만 확인합니다(개선 설계 §5.2).
    /// Decide는 완료가 없을 때만 포기를 고르고 <see cref="CombatNextJob"/>이 그 판단과 같은 Job 안에서 이 Job을 실행하므로(골격 점검 설계 K-1),
    /// 이미 끝난 전투(AlreadyCompleted)는 받을 수 없는 결과로 예외입니다.
    /// </summary>
    internal struct CombatAbandonJob : IJobService
    {
        private readonly IBattleCompletionService _completion;
        private readonly ICombatProgressReader _progress;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatAbandonJob(IBattleCompletionService completion, ICombatProgressReader progress, int priority)
        {
            _completion = completion ?? throw new ArgumentNullException(nameof(completion));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            BattleAbandonResult result = _completion.AbandonBattle();
            CombatProgressFacts after = _progress.Read();
            switch (result)
            {
                case BattleAbandonResult.Abandoned:
                    if (!CombatPlan.IsAbandonment(after.Completion))
                        throw new InvalidOperationException("[CombatAbandonJob] 포기 완료가 기록되지 않았습니다.");
                    break;
                default:
                    throw new InvalidOperationException($"[CombatAbandonJob] 활성 전투에서 받을 수 없는 결과입니다({result}).");
            }
            IsCompleted = true;
        }
    }
}
