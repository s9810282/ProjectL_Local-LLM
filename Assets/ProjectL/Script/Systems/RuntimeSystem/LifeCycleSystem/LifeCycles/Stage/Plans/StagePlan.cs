using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.RunStart;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Plans
{
    /// <summary>
    /// Stage의 다음 할 일을 정하고 그 Job 목록을 만듭니다. 판단의 유일한 원본은 <see cref="Decide"/>이고, 순서만 정합니다
    /// (검사 후속 정리 계획 TP-R9, 로비 꼴). 도메인마다의 Core 호출은 각 Job이, 처리 확인은 바로 뒤의 확인 Job이 합니다.
    /// </summary>
    internal static class StagePlan
    {
        // 할 일이 없을 때 돌려주는 목록. Stage 활성 동안 Runner가 빌 때마다 판단하므로 새로 만들지 않는다. JobRegister는 목록을 바꾸지 않는다.
        private static readonly List<IJobService> NoJobs = new(0);

        /// <param name="nextSeed">노드 처리에 넘길 임시 seed 공급원입니다.</param>
        /// <param name="lifeCycles">이번 Stage 방문의 진행(<see cref="IStageProgress"/>)을 꺼낼 LifeCycle 그룹입니다.</param>
        internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType, IBufferQuery buffers,
            Func<int> nextSeed, ILifeCycleSystemGroupService lifeCycles)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            if (buffers == null)
                throw new ArgumentNullException(nameof(buffers));

            switch (jobType)
            {
                case JobType.None:
                    return NoJobs;
                case JobType.Start:
                    return Start(service, buffers);
                case JobType.Next:
                    return Next(service, buffers, nextSeed, lifeCycles);
                default:
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown Stage job type.");
            }
        }

        /// <summary>
        /// 지금 사실로 Stage의 다음 할 일을 정합니다. 판단의 유일한 원본입니다. 위에서부터 처음 맞는 줄 하나만 고릅니다.
        /// 멈춘 도메인은 건너뜁니다. 정지는 공용 오류 경계(F1)에도 보고되므로 같은 Tick 안에서만 의미가 있습니다.
        /// </summary>
        internal static StageStep Decide(in StageProgressFacts facts)
        {
            // 0. 로딩 중에는 아무것도 하지 않는다.
            if (facts.IsLoading)
                return StageStep.Nothing;

            if (!facts.AbandonRunId.IsEmpty)
                return new StageStep(StageStepKind.AbandonRun);

            // 1. 전투 진입 시도. 끝날 때까지 다른 일을 걸지 않는다.
            if (!facts.IsEntryStopped)
            {
                switch (facts.CombatEntry)
                {
                    case StageCombatEntryState.Waiting:
                        return StageStep.Nothing;
                    case StageCombatEntryState.Done:
                        return new StageStep(StageStepKind.CloseCombatEntry);
                    case StageCombatEntryState.Broken:
                        return new StageStep(StageStepKind.StopCombatEntry);
                }
            }

            // 2. 증강을 맨 앞에 둔다. 처리가 준비보다 먼저다. 노드 진입은 화면이 막고 Core도 거절하며, 상점·회복 명령은 계속 받아 그 팝업을 닫게 한다.
            if (!facts.IsGrowthStopped)
            {
                if (facts.CanReserveGrowth)
                    return new StageStep(StageStepKind.GrowthCommand);
                if (facts.GrowthNeedsOffer)
                    return new StageStep(StageStepKind.GrowthOffer);
            }

            // 3. 노드 진입. 전투 노드는 전투 진입을 요청하고, 그 밖의 노드는 Core 노드 처리에 맡긴다(상점·미지원·없는 노드의 정상 거절·수락).
            if (!facts.IsEntryStopped && facts.HasMapCommand && facts.IsMapReady)
                return new StageStep(facts.IsCombatNode ? StageStepKind.CombatEntry : StageStepKind.MapCommand,
                    facts.MapCommand.Number);

            // 4. 상점, 5. 회복.
            if (!facts.IsShopStopped && facts.ShopCommandNumber > 0)
                return new StageStep(StageStepKind.ShopCommand, facts.ShopCommandNumber);
            if (!facts.IsRecoveryStopped && facts.CanReserveRecovery)
                return new StageStep(StageStepKind.RecoveryCommand);
            return StageStep.Nothing;
        }

        /// <summary>
        /// 맵을 준비합니다. 로스터가 없으면 아직 Run이 시작되지 않은 첫 진입이므로 새 Run(로스터·Run 재화)부터 시작합니다.
        /// 로스터가 있으면(이어하기, 전투 뒤 복귀, 로비 출발이 이미 새 Run을 시작한 경우) 맵 준비만 합니다.
        /// 끝난 Run(패배·최종 승리)을 자동으로 새로 시작하지 않습니다. 작은 우선순위가 먼저 실행됩니다.
        /// </summary>
        private static List<IJobService> Start(IInGameSystemGroupService service, IBufferQuery buffers)
        {
            var jobs = new List<IJobService>();
            if (buffers.CharacterReadSink.Owned.Count == 0)
                jobs.Add(new RunStartJob(service.CoreGroup.RunStart, StageRunStartOutcome.Instance, mainCharacterKey: null,
                    buffers.CombatStateReadSink, buffers.StageViewWriteSink, buffers.CharacterReadSink, 0));
            jobs.Add(new StageJob(service.CoreGroup.ChapterProgression, 1, buffers.CurrentProgressReadSink,
                buffers.StageViewWriteSink));
            return jobs;
        }

        /// <summary>사실을 한 번 읽어 다음 단계를 정하고 그 Job 목록을 만듭니다. 할 일이 없으면 공유 빈 목록입니다.</summary>
        private static List<IJobService> Next(IInGameSystemGroupService service, IBufferQuery buffers, Func<int> nextSeed,
            ILifeCycleSystemGroupService lifeCycles)
        {
            if (lifeCycles == null)
                throw new ArgumentNullException(nameof(lifeCycles));
            IStageProgress progress = lifeCycles.StageProgress ??
                throw new InvalidOperationException("StagePlan: Stage 진행 서비스가 없습니다.");
            StageProgressFacts facts = progress.Read();
            StageStep step = Decide(in facts);
            switch (step.Kind)
            {
                case StageStepKind.None:
                    return NoJobs;
                case StageStepKind.AbandonRun:
                    return new List<IJobService>
                    {
                        new StageRunAbandonJob(service.CoreGroup.RunAbandon, progress, facts.AbandonRunId)
                    };
                // 아래 셋은 Core를 부르지 않는 이번 방문의 상태 바꿈이라 판단 직후 바로 합니다.
                case StageStepKind.CloseCombatEntry:
                    progress.CloseCombatEntry();
                    return NoJobs;
                case StageStepKind.StopCombatEntry:
                    progress.Stop(StageDomain.Entry, facts.CombatEntryProblem);
                    return NoJobs;
                case StageStepKind.CombatEntry:
                    progress.BeginCombatEntry(facts.MapCommand);
                    return NoJobs;
                case StageStepKind.GrowthCommand:
                {
                    ITraitGrowthRequestExecution owner = service.ViewGroup.TraitGrowthExecution;
                    return new List<IJobService>
                    {
                        new TraitGrowthCommandJob(service.CoreGroup.TraitGrowthCommands, owner),
                        progress.CreateCheck(StageDomain.Growth, () => owner.HasReservedExecution,
                            "[StageGrowth] 예약된 증강 Job의 종료 통지가 없습니다.")
                    };
                }
                case StageStepKind.GrowthOffer:
                    return new List<IJobService>
                    {
                        new TraitGrowthPrepareJob(service.CoreGroup.Characters),
                        progress.CreateGrowthOfferCheck()
                    };
                case StageStepKind.MapCommand:
                {
                    int number = step.Number;
                    IStageViewBufferReadSink input = buffers.StageViewReadSink;
                    IStageCommandResultReadSink results = buffers.StageCommandResults;
                    return new List<IJobService>
                    {
                        new StageNodeEntryJob(number, service.CoreGroup.StageCommands,
                            nextSeed ?? throw new ArgumentNullException(nameof(nextSeed))),
                        progress.CreateCheck(StageDomain.Entry,
                            () => results.HandledMapCommandNumber < number && HasMapInput(input, number),
                            $"[StageEntry] 요청 {number}: 맵 진입의 Core 처리 번호가 기록되지 않았습니다.")
                    };
                }
                case StageStepKind.ShopCommand:
                {
                    int number = step.Number;
                    IShopViewBufferReadSink input = buffers.ShopViewReadSink;
                    IStageCommandResultReadSink results = buffers.StageCommandResults;
                    return new List<IJobService>
                    {
                        new ShopCommandJob(number, service.CoreGroup.StageCommands),
                        progress.CreateCheck(StageDomain.Shop,
                            () => results.HandledShopCommandNumber < number &&
                                input.TryGetNextCommand(number - 1, out ShopViewCommand next) && next.Number == number,
                            $"[StageShop] 요청 {number}의 Core 처리가 기록되지 않았습니다.")
                    };
                }
                case StageStepKind.RecoveryCommand:
                {
                    IRecoveryRequestExecution owner = service.ViewGroup.RecoveryExecution;
                    return new List<IJobService>
                    {
                        new RecoveryCommandJob(service.CoreGroup.RecoveryCommands, owner),
                        progress.CreateCheck(StageDomain.Recovery, () => owner.HasReservedExecution,
                            "[StageRecovery] 예약된 회복 Job의 종료 통지가 없습니다.")
                    };
                }
                default:
                    throw new InvalidOperationException($"StagePlan: 알 수 없는 단계입니다: {step}");
            }
        }

        private static bool HasMapInput(IStageViewBufferReadSink input, int number)
        {
            for (int i = 0; i < input.Commands.Count; i++)
                if (input.Commands[i].Number == number) return true;
            return false;
        }
    }
}
