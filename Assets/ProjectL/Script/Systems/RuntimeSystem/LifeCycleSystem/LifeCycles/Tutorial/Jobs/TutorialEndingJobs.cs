using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial.Jobs
{
    /// <summary>
    /// 세션을 시작한 전투에 승패가 기록돼 종료 정리를 시작합니다(튜토리얼 설계 §8.2 전투 승패).
    /// 이미 Ending인 세션은 고정된 종료 의도를 유지하므로 바꾸지 않습니다.
    /// </summary>
    internal sealed class TutorialEndJob : TutorialSessionJob
    {
        private readonly TutorialEndReason _reason;

        internal TutorialEndJob(TutorialPlanSources sources, int sessionId, int stepRevision, TutorialEndReason reason,
            int priority) : base(sources, sessionId, stepRevision, priority)
        {
            _reason = reason;
        }

        protected override void Run(TutorialSessionSnapshot session)
        {
            if (session.Status == TutorialSessionStatus.Starting || session.Status == TutorialSessionStatus.Running)
                TutorialProgression.BeginEnding(Sources, session, _reason);
        }
    }

    /// <summary>정리가 끝난 세션의 최종 상태를 게시합니다(설계 §8.2). 정리에 실패했으면 Faulted(CleanupFault)입니다(설계 §8.3).</summary>
    internal sealed class TutorialFinalizeJob : TutorialSessionJob
    {
        internal TutorialFinalizeJob(TutorialPlanSources sources, int sessionId, int stepRevision, int priority)
            : base(sources, sessionId, stepRevision, priority)
        {
        }

        protected override void Run(TutorialSessionSnapshot session)
        {
            if (session.Status != TutorialSessionStatus.Ending)
                return;
            if (!Sources.Execution.TryGetCleanupResult(session.SessionId, out bool succeeded))
                return;

            Sources.Store.InstallSession(TutorialSessionChanges.Final(session, succeeded));
        }
    }
}
