using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// 자동 진행(적 턴·타임라인·기믹) 한 걸음을 Core 처리 서비스에 요청합니다.
    /// 수락·거절 판단은 Core 결과로 남으며 Job은 판단하지 않습니다.
    /// </summary>
    internal struct CombatAdvanceJob : IJobService
    {
        private readonly ICombatAdvanceService _advance;
        private readonly ICombatProgressReader _progress;
        private readonly int _baseSequence;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        /// <summary>
        /// 자동 진행을 한 번 요청하고 같은 실행 안에서 결과를 확인합니다(재구성 설계 J3, K8).
        /// 결과 순번이 판단 때의 S보다 커졌고, 그 최신 결과가 자동 진행의 수락이어야 합니다. 거절도 멈춥니다.
        /// </summary>
        internal CombatAdvanceJob(ICombatAdvanceService advance, int priority, ICombatProgressReader progress,
            int baseSequence)
        {
            _advance = advance ?? throw new ArgumentNullException(nameof(advance));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            if (baseSequence < 0)
                throw new ArgumentOutOfRangeException(nameof(baseSequence));
            _baseSequence = baseSequence;
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            // 반환값으로 재시도하지 않는다. 결과의 종류·순번·수락 여부로 확인한다.
            _advance.AdvanceCombat();
            string failure = FindFailure(_progress.Read());
            if (failure != null)
                throw new InvalidOperationException($"[CombatAdvanceJob] {failure}");
            IsCompleted = true;
        }

        /// <summary>골격 설계 §4.3.1의 판정 순서입니다. 순번 증가만으로 성공을 판단하지 않습니다.</summary>
        private string FindFailure(in CombatProgressFacts facts)
        {
            int latest = facts.LatestResultSequence;
            if (latest <= _baseSequence)
                return $"자동 진행의 결과 순번이 오르지 않았습니다(판단 때 {_baseSequence}, 현재 {latest}).";

            CombatResult result = facts.LatestResult;
            if (result == null)
                return $"자동 진행의 결과가 기록되지 않았습니다(결과 순번 {latest}).";

            if (result.Sequence != latest)
                return $"자동 진행의 결과 순번이 상태 순번과 다릅니다(결과 {result.Sequence}, 상태 {latest}).";

            if (result.Kind != CombatProcessingKind.Advance)
                return $"결과 {result.Sequence}는 자동 진행이 아닌 {result.Kind} 처리의 결과입니다.";

            if (!result.Accepted)
                return $"자동 진행이 거절됐습니다(결과 {result.Sequence}, 사유 {result.Reason}).";

            return null;
        }
    }
}
