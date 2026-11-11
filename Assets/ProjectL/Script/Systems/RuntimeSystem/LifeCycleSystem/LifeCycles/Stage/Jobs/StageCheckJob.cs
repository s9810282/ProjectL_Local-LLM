using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>
    /// 처리 Job 바로 뒤에서 그 처리가 끝났는지 확인합니다(검사 후속 정리 계획 TP-R9, 로비 아티팩트 확인과 같은 꼴).
    /// 아직 남았으면 기록 누락이므로 그 도메인을 멈추고 공용 오류 경계(F1)에 보고합니다. 같은 Runner Tick에 끝나 다음 Tick까지 기다리는 상태가 없습니다.
    /// </summary>
    internal sealed class StageCheckJob : IJobService
    {
        private readonly IStageProgress _progress;
        private readonly StageDomain _domain;
        private readonly Func<bool> _isStillPending;
        private readonly string _reason;

        /// <summary>처리 Job과 같은 우선순위입니다. 같은 우선순위는 등록 순서를 지키므로 처리 바로 뒤에 돕니다.</summary>
        public int Priority => 3;
        public bool IsCompleted { get; private set; }

        internal StageCheckJob(IStageProgress progress, StageDomain domain, Func<bool> isStillPending, string reason)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _domain = domain;
            _isStillPending = isStillPending ?? throw new ArgumentNullException(nameof(isStillPending));
            _reason = reason ?? throw new ArgumentNullException(nameof(reason));
        }

        public void Execute()
        {
            if (IsCompleted)
                return;
            if (_isStillPending())
                _progress.Stop(_domain, _reason);
            IsCompleted = true;
        }
    }
}
