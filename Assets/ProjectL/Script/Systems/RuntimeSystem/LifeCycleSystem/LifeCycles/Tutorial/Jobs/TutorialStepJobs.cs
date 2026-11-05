using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial.Plans;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial.Jobs
{
    /// <summary>
    /// 설명 구간의 첫 설명을 시작합니다(튜토리얼 설계 §8.1·§15.4.3). 입력을 빌리기 직전에 §8.1 조건을 같은 정의로 다시 확인하고,
    /// 맞지 않으면 시작 조건 대기를 그대로 둡니다. 다시 확인한 뒤에도 빌리지 못하면 Faulted(ExecutionFault)로 마감합니다(설계 §8.3).
    /// 입력을 빌린 뒤에 표시 단계를 설치하므로, 그 순간 이동 실습의 제한 유지가 풀립니다(설계 §5).
    /// </summary>
    internal sealed class TutorialStartExplanationJob : TutorialSessionJob
    {
        internal TutorialStartExplanationJob(TutorialPlanSources sources, int sessionId, int stepRevision, int priority)
            : base(sources, sessionId, stepRevision, priority)
        {
        }

        protected override void Run(TutorialSessionSnapshot session)
        {
            if (session.Status != TutorialSessionStatus.Running || session.StepKind != TutorialStepKind.Explanation
                || session.WaitReason != TutorialWaitReason.StartCondition)
                return;

            // 판단(실행 소유자 Tick)과 실행(다음 Runner Tick) 사이에 전투 명령·연출·입력 독점이 바뀔 수 있다.
            // 빌리기 직전에 같은 정의(§8.1)로 다시 확인하고, 맞지 않으면 시작 조건 대기를 그대로 둔다.
            TutorialFacts facts = TutorialFactsReader.Read(Sources);
            if (!TutorialPlan.CanStartExplanation(in facts))
                return;

            if (!Sources.Execution.TryBorrowInput())
            {
                TutorialProgression.BeginEnding(Sources, session, TutorialEndReason.ExecutionFault);
                return;
            }

            TutorialSessionSnapshot shown = TutorialSessionChanges.Wait(session, TutorialWaitReason.Presentation);
            Sources.Store.InstallSession(shown);
            TutorialProgression.Present(Sources, shown, TutorialViewPresentationKind.Explanation);
        }
    }

    /// <summary>
    /// 다음 대기로 넘어갑니다(설계 §8 WaitReason 표). Starting은 첫 단계에 들어가고,
    /// 표시가 끝난 설명은 확인·만료를, 표시가 끝난 실습은 통과 조건을 기다리며, 닫기가 끝난 설명은 다음 단계에 들어갑니다.
    /// </summary>
    internal sealed class TutorialAdvanceJob : TutorialSessionJob
    {
        internal TutorialAdvanceJob(TutorialPlanSources sources, int sessionId, int stepRevision, int priority)
            : base(sources, sessionId, stepRevision, priority)
        {
        }

        protected override void Run(TutorialSessionSnapshot session)
        {
            if (session.Status == TutorialSessionStatus.Starting)
            {
                TutorialProgression.EnterNextStep(Sources, session);
                return;
            }

            if (session.Status != TutorialSessionStatus.Running || session.WaitReason != TutorialWaitReason.Presentation)
                return;
            if (!Sources.Execution.TryGetCompletedPresentation(session.SessionId, session.StepRevision,
                    out TutorialViewPresentationKind completed))
                return;

            switch (completed)
            {
                case TutorialViewPresentationKind.Explanation when session.StepKind == TutorialStepKind.Explanation:
                    Sources.Store.InstallSession(TutorialSessionChanges.Wait(session, TutorialWaitReason.ContinueOrTimeout));
                    Sources.Execution.StartTimer(session.SessionId, session.StepRevision, session.TutorialKey);
                    break;
                case TutorialViewPresentationKind.Transition when session.StepKind == TutorialStepKind.Explanation:
                    TutorialProgression.EnterNextStep(Sources, session);
                    break;
                case TutorialViewPresentationKind.Practice when session.StepKind == TutorialStepKind.Practice:
                    Sources.Store.InstallSession(TutorialSessionChanges.Wait(session, TutorialWaitReason.PracticeGoal));
                    break;
            }
        }
    }

    /// <summary>
    /// 자동 진행 시간이 다 된 설명을 닫기 시작합니다(설계 §6.1·§15.4). 사용자 확인과 같은 전환이며 확인 명령으로 꾸미지 않습니다.
    /// 확인이 먼저 받아들여졌으면 이미 전환 중이므로 바꾸지 않습니다.
    /// </summary>
    internal sealed class TutorialExpireJob : TutorialSessionJob
    {
        internal TutorialExpireJob(TutorialPlanSources sources, int sessionId, int stepRevision, int priority)
            : base(sources, sessionId, stepRevision, priority)
        {
        }

        protected override void Run(TutorialSessionSnapshot session)
        {
            if (!session.CanContinue || !Sources.Execution.IsTimerExpired(session.SessionId, session.StepRevision))
                return;

            TutorialSessionSnapshot closing = TutorialSessionChanges.Wait(session, TutorialWaitReason.Presentation);
            Sources.Store.InstallSession(closing);
            TutorialProgression.Present(Sources, closing, TutorialViewPresentationKind.Transition);
        }
    }

    /// <summary>통과 조건을 채운 실습에서 다음 단계로 갑니다(설계 §8.2). 실습은 게임 결과로만 통과합니다(설계 §6).</summary>
    internal sealed class TutorialPassPracticeJob : TutorialSessionJob
    {
        internal TutorialPassPracticeJob(TutorialPlanSources sources, int sessionId, int stepRevision, int priority)
            : base(sources, sessionId, stepRevision, priority)
        {
        }

        protected override void Run(TutorialSessionSnapshot session)
        {
            if (session.Status != TutorialSessionStatus.Running || session.StepKind != TutorialStepKind.Practice
                || session.WaitReason != TutorialWaitReason.PracticeGoal)
                return;

            TutorialProgression.EnterNextStep(Sources, session);
        }
    }
}
