using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 전투 Plan의 Job 구성과 Core 호출, 정상 반환의 완료, 예외 전파를 확인한다.
    /// </summary>
    public sealed class CombatPlanTests
    {
        [Test]
        public void EntryRegistration_CapturesTarget_RejectsDuplicate_AndCompletedJobDoesNotCallAgain()
        {
            var runner = new CaptureRunner();
            var core = new CombatProbe { EntryResult = ChapterProgressionResult.EnterStage(3, new StageNodeId(7), new StageKey(101)) };
            var register = CreateRegistry(runner, new FakeInGameService(core));
            var request = new CombatEntryRequest(3, new StageNodeId(7));
            register.RegisterCombatEntry(request);
            IJobService job = runner.Jobs[0];
            Assert.That(job.Priority, Is.Zero);
            Assert.That(core.CallCount, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => register.RegisterCombatEntry(request));
            Assert.That(runner.Jobs[0], Is.SameAs(job));
            core.DuringCall = () =>
            {
                Assert.That(job.IsCompleted, Is.False);
                Assert.That(request.IsJobCompleted, Is.False);
                Assert.That(request.Result, Is.Null);
            };
            job.Execute();
            job.Execute();
            Assert.That(core.EntryChapter, Is.EqualTo(3));
            Assert.That(core.EntryNode, Is.EqualTo(new StageNodeId(7)));
            Assert.That(core.CallCount, Is.EqualTo(1));
            Assert.That(request.Result, Is.SameAs(core.EntryResult));
            Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Prepared));
            Assert.That(job.IsCompleted, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EntryJob_MissingOrMismatchedAcceptance_FaultsWithoutCompletion(bool mismatched)
        {
            var core = new CombatProbe
            {
                EntryResult = mismatched ? ChapterProgressionResult.EnterStage(1, new StageNodeId(3), new StageKey(101)) : null
            };
            var runner = new CaptureRunner();
            var register = CreateRegistry(runner, new FakeInGameService(core));
            var request = new CombatEntryRequest(1, new StageNodeId(2));
            register.RegisterCombatEntry(request);
            Assert.Throws<InvalidOperationException>(runner.Jobs[0].Execute);
            Assert.That(runner.Jobs[0].IsCompleted, Is.False);
            Assert.That(request.IsJobCompleted, Is.False);
            Assert.That(request.Result, Is.Null);
            Assert.That(request.Status, Is.EqualTo(CombatEntryStatus.Faulted));
        }

        [TestCase(JobType.Start, typeof(CombatPrepareJob))]
        [TestCase(JobType.ProcessCommand, typeof(CombatCommandJob))]
        [TestCase(JobType.ProcessPreview, typeof(CombatPreviewJob))]
        [TestCase(JobType.AdvanceCombat, typeof(CombatAdvanceJob))]
        public void Register_BuildsOneIncompleteJobWithPriorityZeroAndInjectedService(JobType jobType, Type expected)
        {
            var service = new FakeInGameService();

            List<IJobService> jobs = CombatPlan.Register(service, jobType, null);

            Assert.That(jobs.Count, Is.EqualTo(1));
            Assert.That(jobs[0], Is.TypeOf(expected));
            Assert.That(jobs[0].Priority, Is.EqualTo(0));
            Assert.That(jobs[0].IsCompleted, Is.False);
            var field = expected.GetField("_service", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            Assert.That(field.GetValue(jobs[0]), Is.SameAs(service));
        }

        [Test]
        public void Register_None_BuildsNoJob()
        {
            Assert.That(CombatPlan.Register(new FakeInGameService(), JobType.None, null), Is.Empty);
        }

        [Test]
        public void Register_CreatesFreshJobEachTime()
        {
            var service = new FakeInGameService();

            IJobService first = CombatPlan.Register(service, JobType.ProcessCommand, null)[0];
            IJobService second = CombatPlan.Register(service, JobType.ProcessCommand, null)[0];

            Assert.That(second, Is.Not.SameAs(first));
        }

        [Test]
        public void Register_RejectsNullServiceAndNonCombatTypes()
        {
            Assert.Throws<ArgumentNullException>(() => CombatPlan.Register(null, JobType.Start, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CombatPlan.Register(new FakeInGameService(), (JobType)99, null));
        }

        [Test]
        public void NewRun_StartsRunWithLoadoutOne_ThroughRegistry()
        {
            // 이전 Run 정리·로스터·Run 재화는 Core RunStart 한 번이 맡는다(Core 5f2d0381, StagePlan과 같은 NewRunJob).
            var runStart = new RunStartProbe();
            var service = new FakeInGameService(runStart: runStart);
            var runner = new JobRunnerSystem();
            var registry = CreateRegistry(runner, service);
            try
            {
                List<IJobService> jobs = CombatPlan.Register(service, JobType.NewRun, null);
                Assert.That(jobs.Count, Is.EqualTo(1));
                Assert.That(jobs[0], Is.TypeOf<NewRunJob>());

                registry.Register(JobRootType.Combat, JobType.NewRun);
                Assert.That(runStart.LoadoutKeys, Is.Empty);
                runner.Tick(0);

                Assert.That(runner.IsIdle, Is.True);
                Assert.That(runStart.LoadoutKeys, Is.EqualTo(new[] { CombatPlan.NewRunLoadoutKey }));
                Assert.That(CombatPlan.NewRunLoadoutKey, Is.EqualTo(1));
            }
            finally
            {
                runner.Clear();
            }
        }

        [Test]
        public void NewRun_RejectedStart_ThrowsAndStaysIncomplete()
        {
            var runStart = new RunStartProbe { Failure = RosterAcquireFailureReason.InvalidRequest };
            IJobService job = CombatPlan.Register(new FakeInGameService(runStart: runStart), JobType.NewRun, null)[0];

            Assert.Throws<InvalidOperationException>(job.Execute);
            Assert.That(job.IsCompleted, Is.False);
            Assert.That(runStart.LoadoutKeys, Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void JobConstructors_RejectNullService()
        {
            Assert.Throws<ArgumentNullException>(() => new CombatPrepareJob(null, 0));
            Assert.Throws<ArgumentNullException>(() => new CombatCommandJob(null, 0));
            Assert.Throws<ArgumentNullException>(() => new CombatPreviewJob(null, 0));
            Assert.Throws<ArgumentNullException>(() => new CombatAdvanceJob(null, 0));
        }

        [TestCase(JobType.ProcessCommand, true)]
        [TestCase(JobType.ProcessCommand, false)]
        [TestCase(JobType.ProcessPreview, true)]
        [TestCase(JobType.ProcessPreview, false)]
        [TestCase(JobType.AdvanceCombat, true)]
        [TestCase(JobType.AdvanceCombat, false)]
        public void Execute_NormalTrueOrFalse_CallsSelectedServiceOnceAndCompletes(JobType jobType, bool returned)
        {
            var processing = new CombatProbe { Returned = returned };
            IJobService job = CombatPlan.Register(new FakeInGameService(processing), jobType, null)[0];
            processing.DuringCall = () => Assert.That(job.IsCompleted, Is.False);

            job.Execute();
            job.Execute();

            Assert.That(processing.CallCount, Is.EqualTo(1));
            Assert.That(processing.CallsTo(jobType), Is.EqualTo(1));
            Assert.That(job.IsCompleted, Is.True);
        }

        [TestCase(JobType.ProcessCommand)]
        [TestCase(JobType.ProcessPreview)]
        [TestCase(JobType.AdvanceCombat)]
        public void Execute_PreservesCoreException_AndStaysIncomplete(JobType jobType)
        {
            var failure = new InvalidOperationException("Core 처리 실패");
            var processing = new CombatProbe { Failure = failure };
            IJobService job = CombatPlan.Register(new FakeInGameService(processing), jobType, null)[0];

            Assert.That(Assert.Throws<InvalidOperationException>(job.Execute), Is.SameAs(failure));
            Assert.That(processing.CallsTo(jobType), Is.EqualTo(1));
            Assert.That(processing.CallCount, Is.EqualTo(1));
            Assert.That(job.IsCompleted, Is.False);
        }

        [Test]
        public void Registry_RoutesCombatRootToCombatPlan_WithoutExecuting()
        {
            var runner = new CaptureRunner();
            var preparation = new CombatProbe();
            var registry = CreateRegistry(runner, new FakeInGameService(preparation));

            // 등록 단계에서는 어떤 Core 처리도 호출하지 않는다.
            Assert.DoesNotThrow(() => registry.Register(JobRootType.Combat, JobType.ProcessCommand));
            Assert.That(runner.Jobs[0], Is.TypeOf<CombatCommandJob>());

            Assert.DoesNotThrow(() => registry.Register(JobRootType.Combat, JobType.Start));
            Assert.That(runner.Jobs[0], Is.TypeOf<CombatPrepareJob>());
            Assert.That(preparation.CallCount, Is.Zero);
        }

        [Test]
        public void PrepareJob_CallsCoreOnce_AndCompletesAfterNormalReturn()
        {
            var preparation = new CombatProbe();
            IJobService job = CombatPlan.Register(new FakeInGameService(preparation), JobType.Start, null)[0];

            job.Execute();
            job.Execute();

            Assert.That(preparation.CallCount, Is.EqualTo(1));
            Assert.That(job.IsCompleted, Is.True);
        }

        [Test]
        public void PrepareJob_PreservesCoreException_AndStaysIncomplete()
        {
            var failure = new InvalidOperationException("전투 준비 실패");
            var preparation = new CombatProbe { Failure = failure };
            IJobService job = CombatPlan.Register(new FakeInGameService(preparation), JobType.Start, null)[0];

            var exception = Assert.Throws<InvalidOperationException>(job.Execute);

            Assert.That(exception, Is.SameAs(failure));
            Assert.That(preparation.CallCount, Is.EqualTo(1));
            Assert.That(job.IsCompleted, Is.False);
        }

        [Test]
        public void RealRunner_PreparesBattleOnce_AndReturnsToIdle()
        {
            var preparation = new CombatProbe();
            var runner = new JobRunnerSystem();
            var registry = CreateRegistry(runner, new FakeInGameService(preparation));
            try
            {
                registry.Register(JobRootType.Combat, JobType.Start);

                runner.Tick(0);
                runner.Tick(0);

                Assert.That(preparation.CallCount, Is.EqualTo(1));
                Assert.That(runner.IsIdle, Is.True);
            }
            finally
            {
                runner.Clear();
            }
        }

        [TestCase(JobType.ProcessCommand, true)]
        [TestCase(JobType.ProcessCommand, false)]
        [TestCase(JobType.ProcessPreview, true)]
        [TestCase(JobType.ProcessPreview, false)]
        [TestCase(JobType.AdvanceCombat, true)]
        [TestCase(JobType.AdvanceCombat, false)]
        public void RealRunner_NormalTrueOrFalse_ReturnsToIdleWithoutRetry(JobType jobType, bool returned)
        {
            var processing = new CombatProbe { Returned = returned };
            var runner = new JobRunnerSystem();
            var registry = CreateRegistry(runner, new FakeInGameService(processing));
            try
            {
                registry.Register(JobRootType.Combat, jobType);
                Assert.That(processing.CallCount, Is.Zero);
                runner.Tick(0);
                Assert.That(runner.IsIdle, Is.True);
                runner.Tick(0);
                Assert.That(processing.CallsTo(jobType), Is.EqualTo(1));
                Assert.That(processing.CallCount, Is.EqualTo(1));
            }
            finally
            {
                runner.Clear();
            }
        }

        [TestCase(JobType.Start)]
        [TestCase(JobType.ProcessCommand)]
        [TestCase(JobType.ProcessPreview)]
        [TestCase(JobType.AdvanceCombat)]
        public void RealRunner_PropagatesCoreException_AndKeepsTheJob(JobType jobType)
        {
            var failure = new InvalidOperationException("Core 예외 보존");
            var processing = new CombatProbe { Failure = failure };
            var runner = new JobRunnerSystem();
            var registry = CreateRegistry(runner, new FakeInGameService(processing));
            try
            {
                registry.Register(JobRootType.Combat, jobType);

                var exception = Assert.Throws<InvalidOperationException>(() => runner.Tick(0));

                Assert.That(exception, Is.SameAs(failure));
                Assert.That(processing.CallsTo(jobType), Is.EqualTo(1));
                // 현재 Runner는 예외가 난 Job을 붙잡는다. 재실행 차단은 이 테스트의 범위가 아니다(F1).
                Assert.That(runner.IsIdle, Is.False);
            }
            finally
            {
                runner.Clear();
            }
        }

        internal static JobRegisterSystem CreateRegistry(IJobRunner runner, IInGameSystemGroupService service, IBufferQuery buffers = null)
        {
            var registry = new JobRegisterSystem(runner);
            registry.PostBindService(CreateResolver(service, buffers));
            registry.Initialize();
            return registry;
        }

        private static ISystemServiceStoreReader CreateResolver(IInGameSystemGroupService service, IBufferQuery buffers = null)
        {
            var builder = new SystemBuilderHelper();
            builder.AddService<IInGameSystemGroupService>(service);
            TestLifeCycleGroup.Add(builder);
            if (buffers == null) BufferSystemFactory.Create().ExportServices(builder);
            else builder.AddService<IBufferQuery>(buffers);
            return builder.BuildSystemReader().ServiceStoreReader;
        }

        internal sealed class FakeInGameService : IInGameSystemGroupService
        {
            public IInGameCoreSystemGroupService CoreGroup { get; }
            public IInGameViewSystemGroupService ViewGroup => null;

            internal FakeInGameService(ICombatProcessingService preparation = null,
                IRunStartService runStart = null)
            {
                CoreGroup = new FakeCoreService(preparation, runStart);
            }
        }

        private sealed class FakeCoreService : IInGameCoreSystemGroupService
        {
            public IStageGeneratorSystem StageGeneratorSystem => null;
            public IStageServiceManager StageServiceManager => null;
            public IChapterProgressionCoordinator ChapterProgression => null;
            public ICharacterRosterService Characters => null;
            public ICharacterShopService Shop => null;
            public ICombatProcessingService Combat { get; }
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem.IRecoveryStageService Recovery => null;
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem.IArtifactEditingService Artifacts => null;
            public IRunStartService RunStart { get; }

            internal FakeCoreService(ICombatProcessingService preparation,
                IRunStartService runStart = null)
            {
                Combat = preparation ?? new CombatProbe();
                RunStart = runStart;
            }
        }

        /// <summary>새 Run 시작 요청의 로드아웃 키를 기록한다.</summary>
        private sealed class RunStartProbe : IRunStartService
        {
            internal readonly List<int> LoadoutKeys = new();
            internal RosterAcquireFailureReason Failure;

            public RosterAcquireResult StartNewRun(int loadoutKey)
            {
                LoadoutKeys.Add(loadoutKey);
                return Failure == RosterAcquireFailureReason.None
                    ? RosterAcquireResult.Success(Array.Empty<OwnedCharacterRecord>())
                    : RosterAcquireResult.Failure(Failure);
            }
        }

        private sealed class CombatProbe : ICombatProcessingService
        {
            private readonly Dictionary<JobType, int> _calls = new();

            internal ChapterProgressionResult EntryResult;
            internal int EntryChapter;
            internal StageNodeId EntryNode;
            public ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId)
            {
                EntryChapter = chapterKey;
                EntryNode = targetNodeId;
                Call(JobType.Start);
                return EntryResult;
            }
            public bool ProcessNextCommand() => Call(JobType.ProcessCommand);
            public bool ProcessLatestPreview() => Call(JobType.ProcessPreview);
            public bool AdvanceCombat() => Call(JobType.AdvanceCombat);
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatDrawAvailability GetDrawAvailability() => default;
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.CombatTimelineEntrySnapshot[] GetTimeline(int maxEntries = 10) => System.Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.CombatTimelineEntrySnapshot>();
            public void ApplyHealth(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId id, int hp) => throw new NotSupportedException();
            public void ApplyPersistentMain(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.MainCharacterPersistentState final) => throw new NotSupportedException();
            public void FinalizeTurn(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId id, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.SoulActionChanges settled, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactLoadoutSnapshot loadout) => throw new NotSupportedException();
            public bool ResolveBattle(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactLoadoutSnapshot loadout) => throw new NotSupportedException();
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult AbandonBattle() => throw new NotSupportedException();
            public bool Recover(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.RecoveryOptionId id) => throw new NotSupportedException();
            public void EndBattle() => throw new NotSupportedException();
            internal int CallCount { get; private set; }
            internal Exception Failure { get; set; }
            internal bool Returned { get; set; } = true;
            internal Action DuringCall { get; set; }

            internal int CallsTo(JobType jobType) => _calls.TryGetValue(jobType, out int count) ? count : 0;

            public void PrepareBattle() => Call(JobType.Start);

            private bool Call(JobType jobType)
            {
                CallCount++;
                _calls[jobType] = CallsTo(jobType) + 1;
                DuringCall?.Invoke();
                if (Failure != null)
                    throw Failure;
                return Returned;
            }
        }

        internal sealed class PreparationProbe : ICombatProcessingService
        {
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.ChapterProgressionResult EnterBattle(int chapterKey,
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.StageNodeId targetNodeId) => throw new NotSupportedException();
            public bool ProcessNextCommand() => Process(JobType.ProcessCommand);
            public bool ProcessLatestPreview() => Process(JobType.ProcessPreview);
            public bool AdvanceCombat() => Process(JobType.AdvanceCombat);
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatDrawAvailability GetDrawAvailability() => default;
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.CombatTimelineEntrySnapshot[] GetTimeline(int maxEntries = 10) => System.Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.CombatTimelineEntrySnapshot>();
            public void ApplyHealth(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId id, int hp) => throw new NotSupportedException();
            public void ApplyPersistentMain(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.MainCharacterPersistentState final) => throw new NotSupportedException();
            public void FinalizeTurn(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId id, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.SoulActionChanges settled, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactLoadoutSnapshot loadout) => throw new NotSupportedException();
            public bool ResolveBattle(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact.ArtifactLoadoutSnapshot loadout) => throw new NotSupportedException();
            public ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult AbandonBattle() => throw new NotSupportedException();
            public bool Recover(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.RecoveryOptionId id) => throw new NotSupportedException();
            public void EndBattle() => throw new NotSupportedException();
            internal int CallCount { get; private set; }
            internal Exception Failure { get; set; }
            internal readonly List<JobType> ProcessingCalls = new();
            internal bool HasEligibleWork { get; set; } = true;

            private bool Process(JobType kind)
            {
                ProcessingCalls.Add(kind);
                if (Failure != null) throw Failure;
                return HasEligibleWork;
            }

            public void PrepareBattle()
            {
                CallCount++;
                if (Failure != null)
                    throw Failure;
            }
        }

        [TestCase(JobType.AdvanceCombat)]
        public void Execute_CallsMatchingServiceOnce(JobType jobType)
        {
            var probe = new PreparationProbe();
            IJobService job = CombatPlan.Register(new FakeInGameService(probe), jobType, null)[0];

            job.Execute();
            job.Execute();

            Assert.That(probe.ProcessingCalls, Is.EqualTo(new[] { jobType }));
            Assert.That(probe.CallCount, Is.Zero);
            Assert.That(job.IsCompleted, Is.True);
        }

        [TestCase(JobType.AdvanceCombat)]
        public void Execute_ExceptionRemainsIncomplete(JobType jobType)
        {
            var failure = new InvalidOperationException("Core 처리 실패");
            var probe = new PreparationProbe { Failure = failure };
            IJobService job = CombatPlan.Register(new FakeInGameService(probe), jobType, null)[0];

            var exception = Assert.Throws<InvalidOperationException>(job.Execute);

            Assert.That(exception, Is.SameAs(failure));
            Assert.That(probe.ProcessingCalls, Is.EqualTo(new[] { jobType }));
            Assert.That(job.IsCompleted, Is.False);
        }

        private sealed class CaptureRunner : IJobRunner
        {
            internal IReadOnlyList<IJobService> Jobs;
            public bool IsIdle => Jobs == null || Jobs.Count == 0;
            public void Register(IJobService job) => Jobs = new[] { job };
            public void RegisterRange(IReadOnlyList<IJobService> jobs) => Jobs = jobs;
            public void Clear() => Jobs = null;
        }
    }
}
