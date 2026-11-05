using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial.Jobs
{
    /// <summary>
    /// 활성 세션 하나에 한 걸음을 하는 Job의 공통 틀입니다(튜토리얼 설계 §9.1).
    /// 실행할 때 활성 세션·revision을 다시 확인하고, 등록 뒤 바뀌었으면 낡은 등록이므로 아무것도 바꾸지 않고 끝냅니다.
    /// </summary>
    internal abstract class TutorialSessionJob : IJobService
    {
        protected TutorialSessionJob(TutorialPlanSources sources, int sessionId, int stepRevision, int priority)
        {
            Sources = sources;
            if (sessionId <= 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId), sessionId, "0보다 커야 합니다.");
            if (stepRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(stepRevision), stepRevision, "0 이상이어야 합니다.");
            SessionId = sessionId;
            StepRevision = stepRevision;
            Priority = priority;
        }

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        protected TutorialPlanSources Sources { get; }
        protected int SessionId { get; }
        protected int StepRevision { get; }

        public void Execute()
        {
            if (IsCompleted)
                return;

            TutorialSessionSnapshot active = ReadActive();
            if (active != null && active.SessionId == SessionId && active.StepRevision == StepRevision)
                Run(active);
            IsCompleted = true;
        }

        /// <summary>등록 때와 같은 세션·revision일 때만 부릅니다. 상태가 이 걸음의 조건과 다르면 바꾸지 않고 돌아갑니다.</summary>
        protected abstract void Run(TutorialSessionSnapshot session);

        private TutorialSessionSnapshot ReadActive() => TutorialFactsReader.ReadActiveSession(Sources.State);
    }
}
