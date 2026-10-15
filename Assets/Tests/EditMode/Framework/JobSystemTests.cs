using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Jobs;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class JobSystemTests
    {
        [Test]
        public void Group_ExportsOnlyContext_AndForwardsBindingInitializeTickAndShutdown()
        {
            var builder = new SystemBuilderHelper();
            JobSystemFactory.Create().ExportServices(builder);
            var reader = builder.BuildSystemReader();
            var context = reader.ServiceStoreReader.GetService(
                new SystemGroupKey<IJobSystemServiceContext>("Job"));
            Assert.Throws<KeyNotFoundException>(() =>
                reader.ServiceStoreReader.GetService(JobSystemServiceKey.JobRunner));
            Assert.Throws<KeyNotFoundException>(() =>
                reader.ServiceStoreReader.GetService(JobSystemServiceKey.JobRegister));

            var modules = reader.SystemModuleStoreReader;
            Assert.That(modules.TickModules.Count, Is.EqualTo(1));
            modules.PostBindServiceModules[0].PostBindService(CreateResolver(new FakeInGameService()));
            modules.InitializeModules[0].Initialize();
            context.JobRegister.Register(JobRootType.Lobby, JobType.Start);
            Assert.That(context.JobRunner.IsIdle, Is.False);
            modules.TickModules[0].Tick(0);
            Assert.That(context.JobRunner.IsIdle, Is.True);

            context.JobRegister.Register(JobRootType.Lobby, JobType.Start);
            modules.ShutdownModules[0].Shutdown();
            Assert.That(context.JobRunner.IsIdle, Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                context.JobRegister.Register(JobRootType.Lobby, JobType.Start));
        }

        [Test]
        public void Register_SelectsStaticPlan_AndInjectsBoundServiceIntoJob()
        {
            var runner = new CaptureRunner();
            var service = new FakeInGameService();
            var registry = CreateRegistry(runner, service);
            registry.Register(JobRootType.Lobby, JobType.Start);
            Assert.That(runner.Jobs[0], Is.TypeOf<LobbyJob>());
            var field = typeof(LobbyJob).GetField("_service", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field.GetValue(runner.Jobs[0]), Is.SameAs(service));

            registry.Register(JobRootType.Lobby, JobType.Start);
            Assert.That(runner.Jobs[0], Is.TypeOf<LobbyJob>());
            registry.Register(JobRootType.Stage, JobType.None);
            Assert.That(runner.Jobs, Is.Empty);
        }

        [Test]
        public void RepeatedRegistrations_CreateFreshJobs()
        {
            var runner = new CaptureRunner();
            var registry = CreateRegistry(runner, new FakeInGameService());
            registry.Register(JobRootType.Lobby, JobType.Start);
            var first = runner.Jobs[0];
            first.Execute();
            Assert.That(first.IsCompleted, Is.True);
            registry.Register(JobRootType.Lobby, JobType.Start);
            Assert.That(runner.Jobs[0], Is.Not.SameAs(first));
            Assert.That(runner.Jobs[0].IsCompleted, Is.False);
        }

        [Test]
        public void Register_RequiresBindingAndInitialization_AndRejectsUnknownTypes()
        {
            var registry = new JobRegisterSystem(new JobRunnerSystem());
            Assert.Throws<InvalidOperationException>(() => registry.Register(JobRootType.Lobby, JobType.Start));
            registry.PostBindService(CreateResolver(new FakeInGameService()));
            Assert.Throws<KeyNotFoundException>(() => registry.Register(JobRootType.Lobby, JobType.Start));
            registry.Initialize();
            Assert.Throws<KeyNotFoundException>(() => registry.Register((JobRootType)99, JobType.Start));
            Assert.Throws<ArgumentOutOfRangeException>(() => registry.Register(JobRootType.Stage, (JobType)99));
        }

        [Test]
        public void Runner_RespectsPriority_AndWaitsForCurrentJob()
        {
            var runner = new JobRunnerSystem();
            var log = new List<string>();
            runner.RegisterRange(new IJobService[]
            {
                new ProbeJob(log, "late", 20),
                new ProbeJob(log, "first", 10, 2)
            });
            runner.Tick(0);
            Assert.That(log, Is.EqualTo(new[] { "first:1" }));
            runner.Tick(0);
            Assert.That(log, Is.EqualTo(new[] { "first:1", "first:2", "late:1" }));
        }

        [Test]
        public void NonStagePlans_CanBeRegisteredAndExecutedWithoutChapter()
        {
            var runner = new JobRunnerSystem();
            var registry = CreateRegistry(runner, new FakeInGameService());
            // Combat Job은 Core 서비스 연결 전까지 미연결 예외를 던지므로 CombatPlanTests에서 따로 검증한다.
            foreach (JobRootType root in new[] { JobRootType.Lobby, JobRootType.Artifact })
                registry.Register(root, JobType.Start);
            runner.Tick(0);
            Assert.That(runner.IsIdle, Is.True);
        }

        [Test]
        public void Runner_YieldsAfterTickBudget_AndClearDropsPendingJobs()
        {
            var runner = new JobRunnerSystem();
            var log = new List<string>();
            for (int i = 0; i < 33; i++) runner.Register(new ProbeJob(log, "job", 0));
            runner.Tick(0);
            Assert.That(log.Count, Is.EqualTo(32));
            Assert.That(runner.IsIdle, Is.False);
            runner.Tick(0);
            Assert.That(log.Count, Is.EqualTo(33));
            Assert.That(runner.IsIdle, Is.True);
            runner.Register(new ProbeJob(log, "job", 0));
            runner.Clear();
            runner.Tick(0);
            Assert.That(log.Count, Is.EqualTo(33));
        }

        [Test]
        public void InvalidJobList_DoesNotPartiallyEnqueue()
        {
            var runner = new JobRunnerSystem();
            Assert.Throws<ArgumentException>(() => runner.RegisterRange(new IJobService[]
            {
                new ProbeJob(new List<string>(), "job", 0), null
            }));
            Assert.That(runner.IsIdle, Is.True);
        }

        private static JobRegisterSystem CreateRegistry(IJobRunner runner, IInGameSystemGroupService service)
        {
            var registry = new JobRegisterSystem(runner);
            registry.PostBindService(CreateResolver(service));
            registry.Initialize();
            return registry;
        }

        private static ISystemServiceStoreReader CreateResolver(IInGameSystemGroupService service)
        {
            var builder = new SystemBuilderHelper();
            builder.AddService<IInGameSystemGroupService>(service);
            TestLifeCycleGroup.Add(builder);
            ProjectL.Systems.Factory.Runtime.Buffer.BufferSystemFactory.Create().ExportServices(builder);
            return builder.BuildSystemReader().ServiceStoreReader;
        }

        private sealed class FakeInGameService : IInGameSystemGroupService
        {
            // 표본 Job(Lobby·Artifact)은 CoreGroup을 쓰지 않는다. 전투 준비 호출 검증은 CombatPlanTests가 맡는다.
            public IInGameCoreSystemGroupService CoreGroup => null;
            public IInGameViewSystemGroupService ViewGroup => null;
        }

        private sealed class CaptureRunner : IJobRunner
        {
            internal IReadOnlyList<IJobService> Jobs;
            public bool IsIdle => Jobs == null || Jobs.Count == 0;
            public void Register(IJobService job) => Jobs = new[] { job };
            public void RegisterRange(IReadOnlyList<IJobService> jobs) => Jobs = jobs;
            public void Clear() => Jobs = null;
        }

        private struct ProbeJob : IJobService
        {
            private readonly List<string> _log;
            private readonly string _name;
            private readonly int _requiredTicks;
            private int _ticks;
            public int Priority { get; }
            public bool IsCompleted => _ticks >= _requiredTicks;

            internal ProbeJob(List<string> log, string name, int priority, int requiredTicks = 1)
            {
                _log = log;
                _name = name;
                Priority = priority;
                _requiredTicks = requiredTicks;
                _ticks = 0;
            }

            public void Execute()
            {
                _ticks++;
                _log.Add($"{_name}:{_ticks}");
            }
        }
    }
}
