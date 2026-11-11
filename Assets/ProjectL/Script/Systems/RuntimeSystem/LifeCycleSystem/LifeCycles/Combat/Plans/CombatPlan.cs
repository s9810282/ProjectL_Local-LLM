using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans
{
    /// <summary>
    /// 전투 진행의 다음 작업을 정하고 그 Job 목록을 만듭니다. 판단의 유일한 원본은 <see cref="Decide"/>입니다(재구성 설계 J2).
    /// </summary>
    /// <remarks>
    /// <see cref="JobType.Next"/>는 등록 때 할 일이 있는지만 보고, 있으면 <see cref="CombatNextJob"/> 하나를 만듭니다.
    /// 무엇을 할지는 그 Job이 실행 순간 <see cref="BuildStep"/>으로 정하고, 그 단계도 그 Job 안에서 실행합니다
    /// (개선 설계 §5.1, 골격 점검 설계 K-1). 상태는 갖지 않습니다.
    /// 진입은 진입 서비스가 <see cref="JobType.Entry"/>로 걸고 [진입] → [진입 전환]이며(골격 점검 설계 K-2),
    /// 맵 진입이 착지 전이면 착지 게시 때 <see cref="JobType.EntryTransition"/>이 전환을 다시 겁니다.
    /// Combat 씬 로딩 준비 단계(가림 중)에서는 진입 서비스의 준비가 <see cref="JobType.FirstUsePrepare"/>로 첫 동작 Core 준비를 겁니다(첫 동작 Core 준비 계획).
    /// 이탈은 [정리·표시] → [떠나기]입니다. 전투가 남은 채 나가면 <see cref="JobType.ExitCheck"/>가 확인합니다.
    /// 이탈 목적지와 챕터 완료가 필요한 결과의 판정도 여기 한 곳에 둡니다(설계 J7, 개선 설계 §4.4).
    /// </remarks>
    internal static class CombatPlan
    {
        // 할 일이 없을 때 돌려주는 목록. 전투 중 매 Tick 판단하므로 새로 만들지 않는다.
        // 공유 목록이라 받는 쪽(JobRegister → Runner.RegisterRange, CombatNextJob의 차례 실행)은 읽기만 하고 바꾸지 않는다. Plan 서명(Global)이
        // List<IJobService>라 읽기 전용 형식으로 바꿀 수 없어 이 약속으로 지킨다(기반 한계, 개선 설계 §7.3 F3).
        private static readonly List<IJobService> NoJobs = new(0);

        /// <summary>
        /// 진입 연쇄입니다(설계 J5). 이번 요청의 대상으로 전투를 준비하고, 수락됐을 때만 같은 Tick에 Combat으로 전환합니다.
        /// 맵 진입이 아직 착지 전이면 전환 시도는 착지 대기를 남기고 끝납니다(개선 설계 §6.1).
        /// </summary>
        private static List<IJobService> RegisterEntry(IInGameSystemGroupService service, CombatEntryRequest request,
            ILifeCycleSystemGroupService lifeCycles)
        {
            IInGameCoreSystemGroupService core = service.CoreGroup;
            return new List<IJobService>
            {
                new CombatPrepareJob(core?.BattleEntry, core?.StageCommands, 0, request),
                new CombatEntryTransitionJob(request, RequireManager(lifeCycles), 0)
            };
        }

        /// <param name="lifeCycles">전환·진입 요청·진행 사실 읽기에 쓸 LifeCycle 그룹입니다(재구성 설계 J14).</param>
        internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType,
            ILifeCycleSystemGroupService lifeCycles)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            switch (jobType)
            {
                case JobType.None:
                    return NoJobs;
                case JobType.Next:
                    return Next(service, lifeCycles);
                case JobType.ExitCheck:
                    return new List<IJobService>
                    {
                        new CombatExitCheckJob(RequireProgress(lifeCycles), RequireEntry(lifeCycles).Current, 0)
                    };
                case JobType.Entry:
                    return Entry(service, lifeCycles);
                case JobType.EntryTransition:
                    return EntryTransition(lifeCycles);
                case JobType.FirstUsePrepare:
                    return FirstUsePrepare(service, lifeCycles);
                default:
                    // 처리·정리·전환은 Next의 Job이 실행 순간 판단해 만든다(BuildStep). 따로 등록하는 경로는 없다(재구성 R3).
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown Combat job type.");
            }
        }

        /// <summary>
        /// 진입 서비스가 막 받은 현재 요청으로 준비 → 전환을 만듭니다(골격 점검 설계 K-2). 등록을 기다리는 요청만 받으므로
        /// 같은 요청을 두 번 등록하면 여기서 거절합니다. 등록 때의 단계 표시(등록됨·등록 실패)는 요청의 주인인 진입 서비스가 합니다.
        /// </summary>
        private static List<IJobService> Entry(IInGameSystemGroupService service, ILifeCycleSystemGroupService lifeCycles)
        {
            CombatEntryRequest request = RequireEntry(lifeCycles).Current;
            if (request == null || !request.IsAwaitingRegistration)
                throw new InvalidOperationException("CombatPlan: 등록을 기다리는 전투 진입 요청이 없습니다.");
            return RegisterEntry(service, request, lifeCycles);
        }

        /// <summary>착지를 기다리던 현재 진입 요청의 전환을 한 번 다시 시도합니다. 진입 서비스가 착지 게시 때 겁니다.</summary>
        private static List<IJobService> EntryTransition(ILifeCycleSystemGroupService lifeCycles)
        {
            CombatEntryRequest request = RequireEntry(lifeCycles).Current;
            if (request == null || !request.IsAwaitingLanding)
                throw new InvalidOperationException("CombatPlan: 착지를 기다리는 전투 진입 요청이 없습니다.");
            return new List<IJobService> { new CombatEntryTransitionJob(request, RequireManager(lifeCycles), 0) };
        }

        /// <summary>
        /// 현재 진입 요청에 붙은 첫 동작 준비로 Core 준비 Job 하나를 만듭니다(첫 동작 Core 준비 계획 FU-b).
        /// 진입 서비스가 만든 Combat 씬 로딩 준비가 시작할 때 겁니다. 시작된 준비가 없으면 거절합니다(진입·착지 전환과 같은 꼴).
        /// </summary>
        private static List<IJobService> FirstUsePrepare(IInGameSystemGroupService service, ILifeCycleSystemGroupService lifeCycles)
        {
            CombatFirstUseLoadingPreparation preparation = RequireEntry(lifeCycles).Current?.FirstUsePreparation;
            if (preparation == null || !preparation.IsRunning)
                throw new InvalidOperationException("CombatPlan: 시작된 첫 동작 준비가 없습니다.");
            ICombatFirstUsePreparation core = service.CoreGroup?.CombatFirstUse
                ?? throw new InvalidOperationException("CombatPlan: 첫 동작 Core 준비 서비스가 없습니다.");
            return new List<IJobService> { new CombatFirstUsePrepareJob(core, preparation, 0) };
        }

        /// <summary>
        /// 지금 사실로 다음 단계를 정합니다(재구성 설계 J2). 위에서부터 처음 맞는 줄 하나만 고릅니다.
        /// 승패 > 포기 > 처리 순서이며, 처리 안에서는 연출이 끝난 명령 > 미리보기 > 자동 진행 순서입니다(K5).
        /// 연출 대기는 Job이 아니라 이 조건이 맡습니다(K1).
        /// </summary>
        internal static CombatStep Decide(in CombatProgressFacts facts)
        {
            // 1~5. 승패가 기록됐다: 제출 정지 → 남은 연출 → 정리·표시 → 사용자 확인 → 떠나기. 모든 결과가 같은 순서다.
            if (facts.HasCompletion)
            {
                // 제출 정지는 Plan만 세운다(이 줄과 아래 포기 블록). View는 같은 사실("승패 기록됨", CombatInputPolicy.IsTerminal)로
                // 새 결과를 처음 본 Tick에 입력 연결만 놓고, 정지 자체는 세우지 않는다(C 단계 설계 C-4).
                if (!facts.IsSubmissionSuspended)
                    return CombatStep.Of(CombatStepKind.SuspendSubmissions);
                if (facts.IsPresenting || facts.IsEntryPresentationPending || !facts.IsViewAttached)
                    return CombatStep.Nothing;
                switch (facts.CompletionDisplay)
                {
                    case CombatCompletionDisplay.None:
                        // 정리는 전투가 있을 때 한 번만. 표시 없이 전투가 사라진 상태는 판단하지 않는다(보스 챕터 완료 두 번 방지).
                        return facts.HasBattle ? CombatStep.Of(CombatStepKind.Settle) : CombatStep.Nothing;
                    case CombatCompletionDisplay.Confirmed:
                        return CombatStep.Of(CombatStepKind.Leave);
                    default:
                        return CombatStep.Nothing;
                }
            }

            // 6. 승패 없이 전투가 없으면 할 일이 없다.
            if (!facts.HasBattle)
                return CombatStep.Nothing;

            // 7~9. 포기 요청: 새 제출을 막고, 남은 연출이 끝나면 Core에 포기를 한 번 요청한다.
            // 아직 처리하지 않은 명령·자동 진행은 처리하지 않는다(K22 A5). 판단은 실행 순간이라 등록 뒤 실행 전에 들어온 포기도 이긴다(개선 설계 §5.1, 10-22 사용자 결정).
            if (facts.IsAbandonRequested)
            {
                if (!facts.IsSubmissionSuspended)
                    return CombatStep.Of(CombatStepKind.SuspendSubmissions);
                if (facts.IsPresenting || facts.IsEntryPresentationPending)
                    return CombatStep.Nothing;
                return CombatStep.Of(CombatStepKind.Abandon);
            }

            // 진입 연출은 결과 순번을 만들지 않습니다. 보드·초기 적 등장 전에는 명령·미리보기·자동 처리를 모두 기다립니다.
            if (facts.IsEntryPresentationPending)
                return CombatStep.Nothing;

            // 10~11. 입력 국면: 연출이 끝났으면 명령을 먼저, 미리보기는 연출 중에도 받는다.
            if (facts.Phase == CombatFlowPhase.PlayerInput)
            {
                if (!facts.HasUnpresentedResult && facts.HasNextCommand)
                    return new CombatStep(CombatStepKind.Command, facts.NextCommandNumber);
                if (facts.PreviewNumber > facts.AnsweredPreviewNumber)
                    return new CombatStep(CombatStepKind.Preview, facts.PreviewNumber);
            }

            // 12. 연출이 남았으면 상태를 바꾸는 처리를 등록하지 않는다. 최신 결과가 확인 전에 덮이지 않게 한다.
            if (facts.HasUnpresentedResult)
                return CombatStep.Nothing;

            // 13. 자동 국면은 연출이 끝난 뒤 한 걸음씩 진행한다.
            if (facts.Phase == CombatFlowPhase.Automatic)
            {
                switch (facts.Preparation)
                {
                    case AdvancePreparationState.NotRequired:
                        return new CombatStep(CombatStepKind.Advance, facts.LatestResultSequence);
                    case AdvancePreparationState.Required:
                    case AdvancePreparationState.Preparing:
                        return new CombatStep(CombatStepKind.PrepareAdvance, facts.LatestResultSequence);
                    case AdvancePreparationState.Ready:
                        return new CombatStep(CombatStepKind.PreparedAdvance, facts.LatestResultSequence);
                    case AdvancePreparationState.FrameDeferred:
                        return CombatStep.Nothing;
                    default:
                        throw new InvalidOperationException($"CombatPlan: 알 수 없는 준비 상태입니다: {facts.Preparation}");
                }
            }

            // 14. PlayerInput 대기, Finished, None.
            return CombatStep.Nothing;
        }

        /// <summary>
        /// 입력 국면에서 남은 연출도 처리할 명령도 없어 사용자 명령을 기다리는 상태입니다. 이때 <see cref="Decide"/>는 명령 처리·자동 진행을
        /// 고르지 않습니다(미리보기 응답과, 승패·포기 요청이 있으면 그 단계는 고를 수 있습니다).
        /// 튜토리얼 설명 시작 조건이 이 함수 하나를 부릅니다(규칙 원본, 개선 설계 §7.1 D2).
        /// </summary>
        internal static bool IsAwaitingPlayerCommand(CombatFlowPhase phase, int presentedResultSequence,
            int latestResultSequence, bool hasUnhandledCommand) =>
            phase == CombatFlowPhase.PlayerInput
            && !CombatProgressFacts.IsUnpresented(presentedResultSequence, latestResultSequence)
            && !hasUnhandledCommand;

        /// <summary>
        /// 할 일이 있는지만 보고, 있으면 실행 순간 판단할 <see cref="CombatNextJob"/> 하나를 만듭니다. 없으면 공유 빈 목록입니다.
        /// 등록(LifeCycle Tick)과 실행(다음 프레임 Job Tick) 사이에 다른 모듈이 돌아 사실이 바뀔 수 있으므로 여기서는 고르지 않습니다.
        /// </summary>
        private static List<IJobService> Next(IInGameSystemGroupService service, ILifeCycleSystemGroupService lifeCycles)
        {
            CombatProgressFacts facts = RequireProgress(lifeCycles).Read();
            if (Decide(in facts).Kind == CombatStepKind.None)
                return NoJobs;
            return new List<IJobService> { new CombatNextJob(CombatStepContext.From(service, lifeCycles), 0) };
        }

        /// <summary>
        /// 지금 사실을 한 번 읽어 다음 단계를 정하고 그 Job 목록을 만듭니다. <see cref="CombatNextJob"/>이 실행 순간 부릅니다.
        /// 할 일이 없어졌으면 공유 빈 목록입니다.
        /// </summary>
        internal static List<IJobService> BuildStep(CombatStepContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            ICombatProgressReader progress = context.Progress;
            CombatProgressFacts facts = progress.Read();
            if (facts.HasCompletion || facts.IsAbandonRequested || !facts.HasBattle)
                context.Advance.CancelPreparation();
            CombatStep step = Decide(in facts);
            switch (step.Kind)
            {
                case CombatStepKind.None:
                    return NoJobs;
                case CombatStepKind.SuspendSubmissions:
                    return new List<IJobService> { new CombatSuspendJob(context.View, 0) };
                case CombatStepKind.Settle:
                    return Settle(context, in facts);
                case CombatStepKind.Leave:
                    return new List<IJobService>
                    {
                        new CombatTransitionJob(context.Manager, DestinationOf(facts.RunOutcome), 0)
                    };
                case CombatStepKind.Abandon:
                    return new List<IJobService> { new CombatAbandonJob(context.BattleCompletion, progress, 0) };
                case CombatStepKind.Command:
                    return new List<IJobService> { new CombatCommandJob(context.Commands, 0, progress, step.Number) };
                case CombatStepKind.Preview:
                    return new List<IJobService> { new CombatPreviewJob(context.Preview, 0, progress, step.Number) };
                case CombatStepKind.Advance:
                    return new List<IJobService> { new CombatAdvanceJob(context.Advance, 0, progress, step.Number) };
                case CombatStepKind.PrepareAdvance:
                    return new List<IJobService> { new CombatPrepareAdvanceJob(context.Advance, 0, progress, step.Number) };
                case CombatStepKind.PreparedAdvance:
                    return new List<IJobService> { new CombatPreparedAdvanceJob(context.Advance, 0, progress, step.Number) };
                default:
                    throw new InvalidOperationException($"CombatPlan: 알 수 없는 단계입니다: {step}");
            }
        }

        /// <summary>
        /// 정리 → (보스 승리면) 챕터 완료 → 완료 표시입니다(개선 설계 §4.4). 모든 결과가 같은 순서이므로 결과 화면은
        /// Core가 Run 결과를 확정한 뒤 열립니다. 기다림이 없어 한 Job(<see cref="CombatNextJob"/>) 안에서 이어 실행되고, 그 사이 View Tick이 끼지 않습니다.
        /// </summary>
        private static List<IJobService> Settle(CombatStepContext context, in CombatProgressFacts facts)
        {
            var jobs = new List<IJobService>
            {
                new CombatEndBattleJob(context.BattleCompletion, context.Progress, facts.LatestResultSequence, 0)
            };
            if (RequiresChapterCompletion(facts.Completion))
                jobs.Add(new CombatChapterCompletionJob(context.Chapters, facts.Completion, 0));
            jobs.Add(new CombatCompletionJob(context.Progress, context.View, context.Entry.Current, 0));
            return jobs;
        }

        /// <summary>
        /// Core가 보스 승리로 기록한 완료입니다. 이 결과만 정리 뒤 챕터 완료를 요청합니다. 최종 챕터인지는 Core가 판정합니다.
        /// </summary>
        internal static bool RequiresChapterCompletion(BattleCompletionChanges completion)
            => completion != null && completion.Outcome.Outcome == StageOutcome.Victory && completion.Progression.ClearMap;

        /// <summary>
        /// 자발적 포기로 끝난 완료입니다(패배 ∧ 자발적 포기 사유). 골격의 포기 확인과 결과 계약 확인이 이 한 곳을 부릅니다(골격 점검 설계 K-5 a).
        /// 결과 화면(<c>CombatEndUI</c>)의 사유 판정 사본은 View 단계에서 다룹니다.
        /// </summary>
        internal static bool IsAbandonment(BattleCompletionChanges completion)
            => completion != null && completion.Outcome.Outcome == StageOutcome.Defeat
                && completion.Outcome.Reason == StageOutcomeReason.VoluntaryAbandonment;

        /// <summary>
        /// 떠날 목적지입니다. Run이 이어지면 Stage, 끝났으면(일반 패배·자발적 포기·최종 승리) 로비입니다(설계 J7). 원본은 이 한 곳입니다.
        /// </summary>
        internal static LifeCycleKey DestinationOf(RunOutcome outcome)
            => outcome == RunOutcome.InProgress ? LifeCycleKeys.Stage : LifeCycleKeys.Lobby;

        private static ICombatProgressReader RequireProgress(ILifeCycleSystemGroupService lifeCycles)
        {
            return lifeCycles?.CombatProgress
                ?? throw new InvalidOperationException("CombatPlan: 전투 진행 사실을 읽을 LifeCycle 그룹이 없습니다.");
        }

        private static ILifeCycleManager RequireManager(ILifeCycleSystemGroupService lifeCycles)
        {
            return lifeCycles?.LifeCycleManager
                ?? throw new InvalidOperationException("CombatPlan: LifeCycle 전환 서비스가 없습니다.");
        }

        private static ICombatEntryService RequireEntry(ILifeCycleSystemGroupService lifeCycles)
        {
            return lifeCycles?.CombatEntry
                ?? throw new InvalidOperationException("CombatPlan: 전투 진입 서비스가 없습니다.");
        }
    }
}
