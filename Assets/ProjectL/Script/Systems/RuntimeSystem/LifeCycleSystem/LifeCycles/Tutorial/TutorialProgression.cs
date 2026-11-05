using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial
{
    /// <summary>
    /// 여러 Job이 같이 쓰는 한 걸음 동작입니다. 어느 걸음을 할지는 TutorialPlan이 정하고, 여기는 그 걸음의 순서만 지킵니다.
    /// </summary>
    internal static class TutorialProgression
    {
        /// <summary>
        /// 다음 단계에 들어갑니다. 마지막 단계였으면 정상 완료(Finished) 정리를 시작합니다(설계 §8.2).
        /// 설명 구간의 첫 설명은 시작 조건을 기다리고, 같은 구간의 설명은 빌린 입력을 그대로 둔 채 바로 표시합니다.
        /// 설명 구간에서 실습으로 갈 때는 실습 단계(제한)를 먼저 설치한 뒤 입력을 돌려줍니다(설계 §15.4.3 전환 순서).
        /// </summary>
        internal static void EnterNextStep(TutorialPlanSources sources, TutorialSessionSnapshot session)
        {
            TutorialDefinitionRules rules = sources.Store.GetRules(session.TutorialKey);
            TutorialStepRule next = rules.NextAfter(session.CurrentStepKey);

            // 실습을 떠나는 것은 통과했다는 뜻이므로 실습 안내를 닫는다(설계 §15.4 "통과할 때 닫는다"). 다음 설명은 시작 조건을 기다릴 수 있어
            // 그때까지 안내가 남지 않게 한다. 닫기 완료는 기다리지 않는다(다음 단계가 revision을 바꿈).
            if (session.Status == TutorialSessionStatus.Running && session.StepKind == TutorialStepKind.Practice)
                Present(sources, session, TutorialViewPresentationKind.Transition);
            if (next == null)
            {
                BeginEnding(sources, session, TutorialEndReason.Finished);
                return;
            }

            // 설명을 보여 주고 있던 단계에서만 입력을 빌린 상태다(설계 §15.4.3).
            bool holdsInput = session.Status == TutorialSessionStatus.Running
                && session.StepKind == TutorialStepKind.Explanation;
            int latestResult = sources.CombatState.LatestResultSequence;

            if (next.Kind == TutorialStepKind.Explanation)
            {
                if (rules.StartsExplanationSegment(next.StepKey))
                {
                    sources.Store.InstallSession(TutorialSessionChanges.Enter(session, next,
                        TutorialWaitReason.StartCondition, latestResult));
                    return;
                }

                TutorialSessionSnapshot shown = TutorialSessionChanges.Enter(session, next,
                    TutorialWaitReason.Presentation, latestResult);
                sources.Store.InstallSession(shown);
                Present(sources, shown, TutorialViewPresentationKind.Explanation);
                return;
            }

            TutorialSessionSnapshot practice = TutorialSessionChanges.Enter(session, next,
                TutorialWaitReason.Presentation, latestResult);
            sources.Store.InstallSession(practice);
            if (holdsInput)
                sources.Execution.ReturnInput();
            Present(sources, practice, TutorialViewPresentationKind.Practice);
        }

        /// <summary>종료 의도를 고정하고 정리를 시작합니다. 최종 상태는 정리가 끝난 뒤 게시합니다(설계 §8.2).</summary>
        internal static void BeginEnding(TutorialPlanSources sources, TutorialSessionSnapshot session,
            TutorialEndReason reason)
        {
            sources.Store.InstallSession(TutorialSessionChanges.Ending(session, reason));
            sources.Execution.BeginCleanup(session.SessionId);
        }

        internal static void Present(TutorialPlanSources sources, TutorialSessionSnapshot session,
            TutorialViewPresentationKind kind)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            sources.Execution.BeginPresentation(session.SessionId, session.StepRevision, session.TutorialKey,
                session.CurrentStepKey, kind);
        }
    }
}
