using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial
{
    /// <summary>
    /// 다음 세션 Snapshot을 만드는 도우미입니다. 값이 맞는지와 이전 상태와의 관계는 Snapshot과 상태 Store가 검사하므로 여기서 다시 검사하지 않습니다(구현 계획 §5).
    /// </summary>
    internal static class TutorialSessionChanges
    {
        /// <summary>받아들인 Start의 새 세션입니다. 전투 기준점은 여기서 정하고 바꾸지 않습니다(설계 §8.2).</summary>
        internal static TutorialSessionSnapshot Starting(int sessionId, int contextId, int tutorialKey,
            long battleInstanceId, int startResultSequence)
        {
            return new TutorialSessionSnapshot(sessionId, contextId, tutorialKey, TutorialSessionStatus.Starting,
                0, 0, TutorialStepKind.None, TutorialWaitReason.None, TutorialEndReason.None,
                battleInstanceId, startResultSequence, startResultSequence);
        }

        /// <summary>다음 단계에 들어갑니다. revision은 1 늘어납니다. 실습이면 실습 기준점을 지금 결과 순번으로 정합니다.</summary>
        internal static TutorialSessionSnapshot Enter(TutorialSessionSnapshot session, TutorialStepRule step,
            TutorialWaitReason wait, int latestResultSequence)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (step == null)
                throw new ArgumentNullException(nameof(step));

            int practiceSequence = step.Kind == TutorialStepKind.Practice
                ? latestResultSequence
                : session.PracticeResultSequence;
            return new TutorialSessionSnapshot(session.SessionId, session.ContextId, session.TutorialKey,
                TutorialSessionStatus.Running, step.StepKey, session.StepRevision + 1, step.Kind, wait,
                TutorialEndReason.None, session.BattleInstanceId, session.StartResultSequence, practiceSequence);
        }

        /// <summary>같은 단계에서 기다리는 것만 바꿉니다.</summary>
        internal static TutorialSessionSnapshot Wait(TutorialSessionSnapshot session, TutorialWaitReason wait)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            return new TutorialSessionSnapshot(session.SessionId, session.ContextId, session.TutorialKey,
                session.Status, session.CurrentStepKey, session.StepRevision, session.StepKind, wait,
                session.EndReason, session.BattleInstanceId, session.StartResultSequence, session.PracticeResultSequence);
        }

        /// <summary>단계를 떠나 종료 정리를 기다립니다. 종료 의도는 여기서 고정합니다(설계 §8.2).</summary>
        internal static TutorialSessionSnapshot Ending(TutorialSessionSnapshot session, TutorialEndReason reason)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            return new TutorialSessionSnapshot(session.SessionId, session.ContextId, session.TutorialKey,
                TutorialSessionStatus.Ending, session.CurrentStepKey, session.StepRevision, TutorialStepKind.None,
                TutorialWaitReason.Cleanup, reason, session.BattleInstanceId, session.StartResultSequence,
                session.PracticeResultSequence);
        }

        /// <summary>정리가 끝난 세션의 최종 상태입니다. 정리에 실패하면 Faulted(CleanupFault)입니다(설계 §8.3).</summary>
        internal static TutorialSessionSnapshot Final(TutorialSessionSnapshot session, bool cleanupSucceeded)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            TutorialEndReason reason = cleanupSucceeded ? session.EndReason : TutorialEndReason.CleanupFault;
            TutorialSessionStatus status = reason == TutorialEndReason.Finished
                ? TutorialSessionStatus.Completed
                : TutorialSessionSnapshot.IsFaultReason(reason)
                    ? TutorialSessionStatus.Faulted
                    : TutorialSessionStatus.Cancelled;
            return new TutorialSessionSnapshot(session.SessionId, session.ContextId, session.TutorialKey, status,
                session.CurrentStepKey, session.StepRevision, TutorialStepKind.None, TutorialWaitReason.None, reason,
                session.BattleInstanceId, session.StartResultSequence, session.PracticeResultSequence);
        }
    }
}
