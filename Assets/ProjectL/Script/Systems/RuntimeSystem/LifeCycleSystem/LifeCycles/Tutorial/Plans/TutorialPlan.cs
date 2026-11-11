using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial.Jobs;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Tutorial.Plans
{
    /// <summary>
    /// 튜토리얼의 다음 한 걸음을 정하고 그 Job 목록을 만듭니다. 판단과 설명 구간 시작 조건의 유일한 원본은 <see cref="Decide"/>입니다(튜토리얼 설계 §8.1·§9.1).
    /// </summary>
    /// <remarks>
    /// <see cref="Combat.Plans.CombatPlan"/>과 같은 형태입니다. 상태를 갖지 않고, <see cref="JobType.Next"/>는 사실 묶음을 한 번 읽어 판단하며,
    /// 할 일이 없으면 Job을 만들지 않습니다. 기다림은 Job이 아니라 판단이 "없음"을 돌려주는 것으로 표현합니다.
    /// 화면 연결 해제와 런타임 정지는 판단을 거치지 않고 실행 소유자가 바로 마감합니다(설계 §10).
    /// </remarks>
    internal static class TutorialPlan
    {
        // 할 일이 없을 때 돌려주는 목록. 실행 소유자가 Runner가 빌 때마다 등록하므로 새로 만들지 않는다.
        // 공유 목록이라 받는 쪽(JobRegister → Runner.RegisterRange)은 읽기만 하고 바꾸지 않는다. Plan 서명(Global)이
        // List<IJobService>라 읽기 전용 형식으로 바꿀 수 없어 이 약속으로 지킨다(기반 한계, 전투 개선 설계 §7.3 F3).
        private static readonly List<IJobService> NoJobs = new(0);

        // 같은 우선순위는 등록 순서를 지키므로 다른 루트의 연쇄 사이에 끼어들지 않는다(설계 §9.1).
        private const int JobPriority = 0;

        /// <summary>
        /// 다른 루트와 같은 등록 입구입니다(<c>JobRegisterSystem.Initialize</c>). 상태·전투 사실은 Buffer에서,
        /// 실행 소유자의 사실과 실행 창구는 <c>service.ViewGroup.TutorialExecution</c>에서 읽습니다(설계 §9.1, <c>RecoveryPlan</c>과 같은 방식).
        /// </summary>
        internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType, IBufferQuery buffers)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            if (buffers == null)
                throw new ArgumentNullException(nameof(buffers));

            switch (jobType)
            {
                case JobType.None:
                    return NoJobs;
                case JobType.Next:
                    return Next(TutorialPlanSources.From(buffers, RequireExecution(service), RequireProgress(service)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Tutorial은 Next만 받습니다.");
            }
        }

        /// <summary>이미 꺼낸 재료로 판단합니다. 시험과 위 입구가 같이 씁니다.</summary>
        internal static List<IJobService> Register(JobType jobType, in TutorialPlanSources sources)
        {
            switch (jobType)
            {
                case JobType.None:
                    return NoJobs;
                case JobType.Next:
                    return Next(sources);
                default:
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Tutorial은 Next만 받습니다.");
            }
        }

        /// <summary>
        /// 지금 사실로 다음 한 걸음을 정합니다(설계 §9.1). 위에서부터 처음 맞는 줄 하나만 고릅니다.
        /// 승패 > 정리 끝 > 요청 > 단계 진행 순서이며, 같은 기회에 실습 통과와 승패가 함께 보이면 승패가 이깁니다(설계 §8.2).
        /// </summary>
        internal static TutorialPlanStep Decide(in TutorialFacts facts)
        {
            TutorialSessionSnapshot session = facts.ActiveSession;

            // 0. 활성 세션도 미처리 요청도 없으면 할 일이 없다.
            if (session == null && !facts.HasPendingRequest)
                return TutorialPlanStep.Nothing;

            // 1. 세션을 시작한 전투에 승패가 기록됐다. 이미 Ending이면 고정된 종료 의도를 유지한다.
            if (session != null && session.Status != TutorialSessionStatus.Ending && facts.IsSessionBattleEnded)
                return SessionStep(TutorialPlanStepKind.End, session);

            // 2. 정리가 끝났으면 최종 상태를 게시한다.
            if (session != null && session.Status == TutorialSessionStatus.Ending && facts.HasCleanupResult)
                return SessionStep(TutorialPlanStepKind.Finalize, session);

            // 3. 미처리 요청은 번호순으로 하나씩 처리한다. 설명 진행은 이 경로의 Continue 요청만 받는다.
            if (facts.HasPendingRequest)
                return new TutorialPlanStep(TutorialPlanStepKind.Request, facts.NextRequestNumber, 0);

            if (session == null)
                return TutorialPlanStep.Nothing;

            // Starting은 다음 기회에 첫 단계에 들어간다(설계 §8 Starting).
            if (session.Status == TutorialSessionStatus.Starting)
                return SessionStep(TutorialPlanStepKind.Advance, session);
            if (session.Status != TutorialSessionStatus.Running)
                return TutorialPlanStep.Nothing;

            // 4. 설명 단계: 표시·전환 끝 → 다음 대기, 시작 조건 충족 → 설명 시작. 확인 대기는 요청이 올 때까지 유지한다.
            if (session.StepKind == TutorialStepKind.Explanation)
            {
                switch (session.WaitReason)
                {
                    case TutorialWaitReason.Presentation:
                        if (facts.CompletedPresentation == TutorialViewPresentationKind.Explanation
                            || facts.CompletedPresentation == TutorialViewPresentationKind.Transition)
                            return SessionStep(TutorialPlanStepKind.Advance, session);
                        break;
                    case TutorialWaitReason.StartCondition:
                        if (CanStartExplanation(in facts))
                            return SessionStep(TutorialPlanStepKind.StartExplanation, session);
                        break;
                }

                return TutorialPlanStep.Nothing;
            }

            // 5. 실습 단계: 안내 표시 끝 → 통과 조건 대기, 통과 조건 충족 → 다음 단계.
            if (session.StepKind == TutorialStepKind.Practice)
            {
                if (session.WaitReason == TutorialWaitReason.Presentation
                    && facts.CompletedPresentation == TutorialViewPresentationKind.Practice)
                    return SessionStep(TutorialPlanStepKind.Advance, session);
                if (session.WaitReason == TutorialWaitReason.PracticeGoal && IsPracticeGoalMet(in facts))
                    return SessionStep(TutorialPlanStepKind.PassPractice, session);
            }

            // 6. 그 밖에는 기다린다.
            return TutorialPlanStep.Nothing;
        }

        /// <summary>
        /// 설명 구간 시작 조건의 유일한 정의입니다. 진입 연출·제출 수명·입력 독점을 포함해 시작 Job이 빌리기 직전에 다시 확인합니다.
        /// </summary>
        internal static bool CanStartExplanation(in TutorialFacts facts)
        {
            return Combat.Plans.CombatPlan.IsAwaitingPlayerCommand(facts.Phase, facts.PresentedResultSequence,
                       facts.LatestResultSequence, facts.HasUnhandledCommand)
                && !facts.IsSessionBattleEnded
                && !facts.IsInputMonopolized
                && facts.IsCombatViewAttached
                && !facts.IsEntryPresentationPending
                && !facts.IsSubmissionSuspended
                && !facts.IsAbandonRequested;
        }

        /// <summary>실습 통과 조건을 정의의 종류에 따라 사실에 맞춥니다(설계 §15.5).</summary>
        internal static bool IsPracticeGoalMet(in TutorialFacts facts)
        {
            switch (facts.PracticeGoal)
            {
                case TutorialPracticeGoal.EnterCombat:
                    return facts.HasEnteredCombat;
                case TutorialPracticeGoal.MoveAtLeastOneCell:
                    return facts.HasPresentedMove;
                case TutorialPracticeGoal.AttackEnemyOnce:
                    return facts.HasPresentedEnemyAttack;
                case TutorialPracticeGoal.EndPlayerTurnOnce:
                    return facts.HasPresentedPlayerTurnEnd;
                default:
                    return false;
            }
        }

        /// <summary>사실을 한 번 읽어 다음 한 걸음을 정하고 그 Job 목록을 만듭니다. 할 일이 없으면 공유 빈 목록입니다.</summary>
        private static List<IJobService> Next(in TutorialPlanSources sources)
        {
            TutorialFacts facts = TutorialFactsReader.Read(in sources);
            TutorialPlanStep step = Decide(in facts);
            switch (step.Kind)
            {
                case TutorialPlanStepKind.None:
                    return NoJobs;
                case TutorialPlanStepKind.Request:
                    return One(new TutorialRequestJob(sources, step.Number, JobPriority));
                case TutorialPlanStepKind.End:
                    return One(new TutorialEndJob(sources, step.Number, step.StepRevision, TutorialEndReason.BattleEnded,
                        JobPriority));
                case TutorialPlanStepKind.Finalize:
                    return One(new TutorialFinalizeJob(sources, step.Number, step.StepRevision, JobPriority));
                case TutorialPlanStepKind.StartExplanation:
                    return One(new TutorialStartExplanationJob(sources, step.Number, step.StepRevision, JobPriority));
                case TutorialPlanStepKind.Advance:
                    return One(new TutorialAdvanceJob(sources, step.Number, step.StepRevision, JobPriority));
                case TutorialPlanStepKind.PassPractice:
                    return One(new TutorialPassPracticeJob(sources, step.Number, step.StepRevision, JobPriority));
                default:
                    throw new InvalidOperationException($"TutorialPlan: 알 수 없는 걸음입니다: {step}");
            }
        }

        private static ITutorialViewExecution RequireExecution(IInGameSystemGroupService service)
        {
            return service.ViewGroup?.TutorialExecution
                ?? throw new InvalidOperationException("TutorialPlan: 튜토리얼 실행 소유자가 조립되지 않았습니다.");
        }

        private static ICombatProgressView RequireProgress(IInGameSystemGroupService service)
        {
            return service.ViewGroup?.Combat?.Progress
                ?? throw new InvalidOperationException("TutorialPlan: 전투 View 진행 사실이 조립되지 않았습니다.");
        }

        private static TutorialPlanStep SessionStep(TutorialPlanStepKind kind, TutorialSessionSnapshot session)
        {
            return new TutorialPlanStep(kind, session.SessionId, session.StepRevision);
        }

        private static List<IJobService> One(IJobService job) => new(1) { job };
    }
}
