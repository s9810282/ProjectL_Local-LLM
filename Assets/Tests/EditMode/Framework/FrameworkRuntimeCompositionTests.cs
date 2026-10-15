using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Enum;
using ProjectL.Systems.Factory.Runtime;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.Factory.Runtime.LifeCycle;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed partial class FrameworkRuntimeCompositionTests
    {
        [Test]
        public void LifeCycle_CanBeComposedAtRoot_WithoutGlobalOwnership()
        {
            var builder = new SystemBuilderHelper();
            LifeCycleSystemFactory.Create().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());

            var lifeCycle = runtime.GetService(SystemGroupsService.LifeCycle);
            Assert.That(lifeCycle.LifeCycleManager, Is.Not.Null);
            Assert.Throws<KeyNotFoundException>(
                () => runtime.GetService(SystemGroupsService.Global));
            Assert.That(typeof(IGlobalSystemGroupService).GetProperty("LifeCycleServiceCommand"),
                Is.Null, "Global must not own the sibling LifeCycle service.");
        }

        [Test]
        public void LifeCycleGroup_ExposesCombatProgressReader_ThatCannotReadBeforeBinding()
        {
            var builder = new SystemBuilderHelper();
            LifeCycleSystemFactory.Create().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());

            var progress = runtime.GetService(SystemGroupsService.LifeCycle).CombatProgress;
            Assert.That(progress, Is.Not.Null);
            Assert.Throws<InvalidOperationException>(() => progress.Read(), "서비스 바인딩 전에는 읽지 않는다.");
        }

        [Test]
        public void Create_RegistersGlobalInGameAndLifeCycleGroups_WithoutPublishingChildContextsAtRoot()
        {
            FrameworkRuntime runtime = CreateRuntime();

            IGlobalSystemGroupService global = runtime.GetService(SystemGroupsService.Global);
            IInGameSystemGroupService inGame = runtime.GetService(SystemGroupsService.InGame);

            Assert.That(global, Is.Not.Null);
            var lifeCycle = runtime.GetService(SystemGroupsService.LifeCycle);
            Assert.That(lifeCycle, Is.Not.Null);
            Assert.That(lifeCycle.LifeCycleManager, Is.Not.Null);
            Assert.That(global.JobServiceCommand, Is.Not.Null);
            Assert.That(global.JobServiceCommand.JobRunner, Is.Not.Null);
            Assert.That(global.JobServiceCommand.JobRegister, Is.Not.Null);
            Assert.Throws<KeyNotFoundException>(
                () => runtime.GetService(SystemGroupsService.Job));
            Assert.That(inGame, Is.Not.Null);
            Assert.That(inGame.CoreGroup, Is.Not.Null);
            Assert.That(inGame.ViewGroup, Is.Not.Null);
            Assert.That(global.RenderServiceCommand.CoreGroup, Is.Not.Null);
            Assert.That(global.RenderServiceCommand.CoreGroup.MeshSystemServices,
                Is.Not.Null);
            Assert.That(global.RenderServiceCommand.ViewGroup, Is.Not.Null);
            Assert.That(global.RenderServiceCommand.ViewGroup.OutlineSystemServices,
                Is.Not.Null);
            Assert.That(global.RenderServiceCommand.ViewGroup.AreaOutlineSystemServices,
                Is.Not.Null);
            Assert.That(global.LodingServiceCommand, Is.Not.Null);
            Assert.Throws<KeyNotFoundException>(
                () => runtime.GetService(SystemGroupsService.Render));
            Assert.Throws<KeyNotFoundException>(
                () => runtime.GetService(SystemGroupsService.Utility));
            Assert.Throws<KeyNotFoundException>(
                () => runtime.GetService(SystemGroupsService.Input));
            Assert.Throws<KeyNotFoundException>(
                () => runtime.GetService(SystemGroupsService.UI));
            Assert.Throws<KeyNotFoundException>(
                () => runtime.GetService(SystemGroupsService.Loding));
        }

        [Test]
        public void ChapterCoordinator_BindsRealBufferServicesThroughCoreGroup()
        {
            FrameworkRuntime runtime = CreateRuntime();
            IInGameSystemGroupService inGame = runtime.GetService(SystemGroupsService.InGame);

            Assert.That(inGame.CoreGroup.ChapterProgression, Is.Not.Null);
            runtime.PostBindService();
            Assert.Throws<KeyNotFoundException>(() =>
                inGame.CoreGroup.ChapterProgression.EnterChapter(7));
        }

        [Test]
        public void ChapterCoordinator_UsesRegisteredGraphAndSelectsCurrentMap()
        {
            FrameworkRuntime runtime = CreateRuntime();
            object buffers = GetBufferGroup(runtime);
            var mapWrite = (IMapProgressBufferWriteSink)buffers.GetType()
                .GetProperty("MapProgressWriteSink").GetValue(buffers);
            var stageWrite = (IStageProgressBufferWriteSink)buffers.GetType()
                .GetProperty("StageProgressWriteSink").GetValue(buffers);
            var stageRead = (IStageProgressBufferReadSink)buffers.GetType()
                .GetProperty("StageProgressReadSink").GetValue(buffers);
            var currentRead = (ICurrentProgressBufferReadSink)buffers.GetType()
                .GetProperty("CurrentProgressReadSink").GetValue(buffers);
            mapWrite.RegisterMap(7);
            stageWrite.RegisterStages(7, new[]
            {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 100, 1, 0, Array.Empty<int>())
            });
            runtime.PostBindService();
            var progression = runtime.GetService(SystemGroupsService.InGame)
                .CoreGroup.ChapterProgression;

            var entry = progression.EnterChapter(7);
            stageWrite.SetClear(7, 2, true);
            var reentry = progression.EnterChapter(7);

            Assert.That(entry.Accepted && entry.SelectMap, Is.True);
            Assert.That(currentRead.CurrentMapKey, Is.EqualTo(7));
            Assert.That(reentry.Accepted, Is.True);
            Assert.That(stageRead.IsCleared(7, 2), Is.True);
            Assert.That(currentRead.HasSelection, Is.False);
        }

        [Test]
        public void LodingRegisterSystem_RegistersCommonAndStateNodes()
        {
            ILodingSystemServiceContext context =
                (ILodingSystemServiceContext)LodingFactory.Create();

            Assert.DoesNotThrow(() =>
            {
                context.LodingRegisterService.RegisterBeforeLoad(new LodingNode(0, () => { }));
                context.LodingRegisterService.RegisterSceneExitLoaded(0, new LodingNode(0, () => { }));
                context.LodingRegisterService.RegisterSceneEnterLoaded(0, new LodingNode(0, () => { }));
                context.LodingRegisterService.RegisterAfterLoad(new LodingNode(0, () => { }));
            });

            Assert.That(context.LodingRegisterReader.GetBeforeLoadNodes(),
                Has.Count.EqualTo(1));
            Assert.That(context.LodingService.CurrentSceneIndex, Is.EqualTo(-1));
            Assert.That(context.LodingService.LoadingProgress, Is.EqualTo(1f));
        }

        [Test]
        public void LodingNodeQueue_ExecutesPriorityOrderAndPreservesRegistrations()
        {
            var queue = new LodingNodeQueue();
            var execution = new List<int>();

            queue.Register(new LodingNode(20, () => execution.Add(20)));
            queue.Register(new LodingNode(10, () => execution.Add(10)));
            queue.Register(new LodingNode(10, () => execution.Add(11)));

            foreach (LodingNode node in queue.GetSortedSnapshot())
                node.Execute();
            foreach (LodingNode node in queue.GetSortedSnapshot())
                node.Execute();

            Assert.That(execution, Is.EqualTo(new[] { 10, 11, 20, 10, 11, 20 }));
        }

        [Test]
        public void StageViewPropQuery_ProvidesStageKeyLookupAndAllDataRead()
        {
            Type queryType = typeof(StageEnum).Assembly.GetType(
                "ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable.IStageViewPropTableQuery",
                throwOnError: true);

            Assert.That(queryType.GetMethod("GetStageViewProp",
                new[] { typeof(int) }), Is.Not.Null);
            Assert.That(queryType.GetMethod("GetAllStageViewProps"), Is.Not.Null);
        }

        [Test]
        public void Create_ConnectsCombatViewStopInputToSharedFaultBoundary_AndBindsOutsideCombat()
        {
            var faults = new RuntimeFaultBoundary(_ => { });

            FrameworkRuntime runtime = FrameworkRuntimeFactory.Create(null, CombatExecutionMode.Live, faults);

            // 경계의 입력 정리 콜백은 하나만 연결된다. View Factory가 이미 CombatViewSystem.StopInput을 연결했다(설계 §10.1).
            Assert.Throws<InvalidOperationException>(() => faults.ConnectStopInput(() => { }));
            // 전투 씬 밖에서도 CombatViewSystem의 서비스 바인딩이 성공해야 한다(계획 C 검증표).
            Assert.DoesNotThrow(runtime.PostBindService);
            var inGame = runtime.GetService(SystemGroupsService.InGame);
            Assert.Throws<KeyNotFoundException>(() => runtime.GetService(InGameSystemServiceKey.Combat));
            var timelineQueries = inGame.ViewGroup.Combat.GetType()
                .GetField("_combatQueries", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(timelineQueries.GetValue(inGame.ViewGroup.Combat), Is.SameAs(inGame.CoreGroup.Combat),
                "Timeline 조회는 최상위가 아니라 InGame.CoreGroup의 Combat 서비스를 사용해야 한다.");
            Assert.That(faults.StopInputForShutdown(), Is.Null);
        }

        [Test]
        public void Create_PassesOneFaultBoundaryAndModeToCombatLifeCycle()
        {
            var faults = new RuntimeFaultBoundary(_ => { });

            FrameworkRuntime runtime = FrameworkRuntimeFactory.Create(
                null, CombatExecutionMode.RecordInputOnly, faults);
            CombatLifeCycle combat = GetCombatLifeCycle(runtime);

            Assert.That(combat.FaultBoundary, Is.SameAs(faults));
            Assert.That(combat.ExecutionMode, Is.EqualTo(CombatExecutionMode.RecordInputOnly));
        }

        [Test]
        public void Create_WithoutArguments_UsesLiveAndCreatesDefaultFaultBoundary()
        {
            CombatLifeCycle combat = GetCombatLifeCycle(CreateRuntime());

            Assert.That(combat.ExecutionMode, Is.EqualTo(CombatExecutionMode.Live));
            Assert.That(combat.FaultBoundary, Is.Not.Null);
            Assert.That(combat.FaultBoundary.IsFaulted, Is.False);
        }

        [Test]
        public void LifeCycles_UseSceneIndices_WithoutDuplicatesOrBoot()
        {
            var faults = new RuntimeFaultBoundary(_ => { });
            var indices = new Dictionary<string, int>
            {
                ["Lobby"] = SceneIndexOf(new LobbyLifeCycle()),
                ["Stage"] = SceneIndexOf(new StageLifeCycle()),
                ["Artifact"] = SceneIndexOf(new ArtifactLifeCycle()),
                ["Combat"] = SceneIndexOf(new CombatLifeCycle(CombatExecutionMode.Live, faults))
            };

            Assert.That(indices["Lobby"], Is.EqualTo(SceneIndices.Lobby));
            Assert.That(indices["Stage"], Is.EqualTo(SceneIndices.Stage));
            Assert.That(indices["Artifact"], Is.EqualTo(SceneIndices.Artifact));
            Assert.That(indices["Combat"], Is.EqualTo(SceneIndices.Combat));
            Assert.That(indices.Values, Is.Unique, "LifeCycle끼리 씬 번호가 겹치면 로딩 Node가 섞인다.");
            Assert.That(indices.Values, Has.No.Member(SceneIndices.Boot), "Boot 씬에는 LifeCycle을 배정하지 않는다.");
        }

        private static FrameworkRuntime CreateRuntime()
        {
            Type factoryType = typeof(IGlobalSystemGroupService).Assembly.GetType(
                "ProjectL.Systems.Factory.Runtime.FrameworkRuntimeFactory",
                throwOnError: true);

            // 리플렉션은 선택 인자를 채우지 않으므로 정확한 서명과 인자 배열을 넘긴다(설계 §8.3).
            MethodInfo create = factoryType.GetMethod(
                "Create",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(IBattleInitializationSource), typeof(CombatExecutionMode), typeof(RuntimeFaultBoundary) },
                null);

            Assert.That(create, Is.Not.Null);

            return (FrameworkRuntime)create.Invoke(null, new object[] { null, CombatExecutionMode.Live, null });
        }

        private static CombatLifeCycle GetCombatLifeCycle(FrameworkRuntime runtime)
        {
            var manager = (LifeCycleManager)runtime.GetService(SystemGroupsService.LifeCycle).LifeCycleManager;
            // 등록 목록은 공개하지 않으므로 테스트에서만 읽는다. 공용 LifeCycleManager에 조회 API를 더하지 않기 위함이다.
            var modules = (Dictionary<LifeCycleKey, ILifeCycleModule>)typeof(LifeCycleManager)
                .GetField("_modules", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(manager);
            return (CombatLifeCycle)modules[LifeCycleKeys.Combat];
        }

        private static int SceneIndexOf(LifeCycleBase lifeCycle)
        {
            return (int)typeof(LifeCycleBase)
                .GetProperty("SceneIndex", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(lifeCycle);
        }

        private static object GetBufferGroup(FrameworkRuntime runtime)
        {
            Type keyType = typeof(IInGameSystemGroupService).Assembly.GetType(
                "ProjectL.Systems.ServiceKeys.BufferSystemKey", throwOnError: true);
            object key = keyType.GetField("Buffer", BindingFlags.Static | BindingFlags.Public)
                .GetValue(null);
            Type serviceType = key.GetType().GetGenericArguments()[0];
            MethodInfo resolve = typeof(FrameworkRuntime).GetMethod("GetService")
                .MakeGenericMethod(serviceType);
            return resolve.Invoke(runtime, new[] { key });
        }
    }
}
