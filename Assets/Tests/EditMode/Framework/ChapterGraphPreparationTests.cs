using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.Factory.DBFactory.Group;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed partial class ChapterGraphPreparationTests
    {
        private const string Root = "ProjectL.Systems.";
        private static readonly Assembly Systems = typeof(StageRootDataSO).Assembly;
        private readonly DataBaseTestAssets assets = new();
        private readonly List<FrameworkRuntime> runtimes = new();

        [TestCase("Nearest")]
        [TestCase("Random")]
        [TestCase("WeightedRandom")]
        public void Runtime_ChapterProgressionRecordsBossCompletionAndRejectsCompletedEntry(string algorithm)
        {
            var setup = Prepare(algorithm, true, 1, withJobRuntime: true);
            var jobs = setup.runtime.GetService(SystemGroupsService.Job);
            jobs.JobRegister.Register(JobRootType.Stage, JobType.Start);
            Assert.That(setup.current.HasMap, Is.False);
            setup.runtime.Tick(0);
            Assert.That(setup.current.CurrentMapKey, Is.EqualTo(1));
            Assert.That(setup.current.HasSelection, Is.False);

            var graph = setup.stages.GetStages(1);
            var node = graph.First(n => n.Depth == 0);
            while (node.NextNodeIds.Count > 0)
            {
                node = graph.Single(n => n.NodeId == node.NextNodeIds[0]);
                var target = new StageNodeId(node.NodeId);
                Assert.That(setup.core.ChapterProgression.PrepareStageEntry(1, target).Accepted, Is.True);
                // 실제 전투 준비 대신 성공 신호만 입력하여 진행 기록을 검증한다.
                Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(1, target).Accepted, Is.True);
                if (node.NextNodeIds.Count > 0)
                    Assert.That(setup.core.ChapterProgression.PrepareStageEntry(
                        1, new StageNodeId(node.NextNodeIds[0])).Accepted, Is.False);

                Assert.That(setup.core.ChapterProgression.ResolveStage(1, StageOutcome.Victory).Accepted, Is.True);
                Assert.That(setup.stages.IsCleared(1, node.NodeId), Is.True);
                Assert.That(setup.core.ChapterProgression.PrepareStageEntry(1, target).Accepted, Is.False);
                if (!setup.maps.GetMap(1).IsClear)
                {
                    jobs.JobRegister.Register(JobRootType.Stage, JobType.Start);
                    setup.runtime.Tick(0);
                }
                Assert.That(jobs.JobRunner.IsIdle, Is.True);
                Assert.That(setup.current.CurrentStage.NodeId, Is.EqualTo(node.NodeId));
                Assert.That(setup.stages.GetStages(1)[0].NextNodeIds, Is.SameAs(graph[0].NextNodeIds));
            }

            Assert.That(setup.maps.GetMap(1).IsClear, Is.True);
            Assert.That(setup.core.ChapterProgression.EnterChapter(1).Accepted, Is.False);
            Assert.That(setup.core.ChapterProgression.ResolveStage(1, StageOutcome.Victory).Accepted, Is.False);
            // 완료된 챕터의 재진입 거절을 Job이 정상 완료로 바꾸지 않는다.
            jobs.JobRegister.Register(JobRootType.Stage, JobType.Start);
            Assert.Throws<InvalidOperationException>(() => setup.runtime.Tick(0));
            Assert.That(setup.maps.GetMap(1).IsClear, Is.True);
            Assert.That(setup.current.CurrentStage.NodeId, Is.EqualTo(node.NodeId));
        }

        [Test]
        public void Runtime_DefeatThenExplicitNewRun_RebuildsGraphAndPreservesArtifact()
        {
            var setup = Prepare("Random", true, 1, withJobRuntime: true);
            var jobs = setup.runtime.GetService(SystemGroupsService.Job);
            jobs.JobRegister.Register(JobRootType.Stage, JobType.NewRun);
            setup.runtime.Tick(0);
            var oldGraph = setup.stages.GetStages(1);
            var node = oldGraph.First(n => n.Depth == 1);
            Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(1, new StageNodeId(node.NodeId)).Accepted, Is.True);
            Assert.That(setup.core.ChapterProgression.ResolveStage(1, StageOutcome.Victory).Accepted, Is.True);
            int next = node.NextNodeIds[0];
            Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(1, new StageNodeId(next)).Accepted, Is.True);
            var artifact = ArtifactSnapshot();
            Assert.That(Property<IArtifactInitializationWriteSink>(setup.buffers, "ArtifactInitializationWriteSink").TryInitialize(artifact.ToCalculationState()), Is.True);
            artifact = Property<IArtifactBufferReadSink>(setup.buffers, "ArtifactReadSink").Current;
            Property<ICurrencyBufferWriteSink>(setup.buffers, "CurrencyWriteSink").SetAmount(75);

            // 패배는 기존 완전한 정산 경로로 확정한 뒤 호출자가 전투를 정리한다. 로스터는 새 Run 시작이 이미 설치했다.
            var b = (IBufferQuery)setup.buffers;
            var owned = b.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);
            Assert.That(b.CharacterReadSink.TryGetMainState(out var persistent), Is.True);
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] {
                CharacterBufferTestData.MainFacts(owned.Id, persistent)
            }, CharacterBufferTestData.Combat()));
            Assert.That(setup.core.Combat.AbandonBattle(), Is.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned));
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            setup.core.Combat.EndBattle();
            Assert.That(setup.stages.IsCleared(1, node.NodeId), Is.True);
            Assert.That(setup.stages.IsCleared(1, next), Is.False);
            jobs.JobRegister.Register(JobRootType.Stage, JobType.NewRun);
            Assert.That(setup.current.HasSelection, Is.True);
            setup.runtime.Tick(0);

            Assert.That(jobs.JobRunner.IsIdle, Is.True);
            Assert.That(setup.current.CurrentMapKey, Is.EqualTo(1));
            Assert.That(setup.current.HasSelection, Is.False);
            Assert.That(setup.maps.GetMap(1).IsClear, Is.False);
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(b.RunProgressReadSink.Completion, Is.Null);
            Assert.That(setup.stages.GetStages(1)[0].NextNodeIds, Is.Not.SameAs(oldGraph[0].NextNodeIds));
            Assert.That(setup.stages.GetStages(1).All(n => !n.IsClear), Is.True);
            // 새 Run은 시작 재화와 새 로스터로 다시 시작한다(이전 메인 ID는 쓰지 않는다).
            Assert.That(Property<ICurrencyBufferReadSink>(setup.buffers, "CurrencyReadSink").Amount, Is.EqualTo(NewRunCurrency));
            Assert.That(b.CharacterReadSink.Owned.Count, Is.EqualTo(4));
            Assert.That(b.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id, Is.Not.EqualTo(owned.Id));
            Assert.That(Property<IArtifactBufferReadSink>(setup.buffers, "ArtifactReadSink").Current,
                Is.SameAs(artifact));
            var first = setup.stages.GetStages(1).First(n => n.Depth == 1);
            Assert.That(setup.core.ChapterProgression.PrepareStageEntry(1, new StageNodeId(first.NodeId)).Accepted, Is.True);
            Property<ICurrencyBufferWriteSink>(setup.buffers, "CurrencyWriteSink").SetAmount(9);
            setup.runtime.Tick(0);
            Assert.That(Property<ICurrencyBufferReadSink>(setup.buffers, "CurrencyReadSink").Amount, Is.EqualTo(9));
        }

        [TestCase(JobType.NewRun)]
        [TestCase(JobType.Start)]
        public void StageJobs_ResetOnlyForNewRun_AndPrepareMapWithoutSelectingNode(JobType type)
        {
            int chapterKey = type == JobType.NewRun ? 1 : 7;
            var setup = Prepare("Nearest", true, chapterKey);
            setup.core.ChapterProgression.EnterChapter(chapterKey);
            var oldGraph = setup.stages.GetStages(chapterKey).ToArray();
            var node = oldGraph.First(n => n.Depth == 1);
            setup.stageWrite.SetClear(chapterKey, node.NodeId, true);
            var artifact = ArtifactSnapshot();
            Assert.That(Property<IArtifactInitializationWriteSink>(setup.buffers, "ArtifactInitializationWriteSink").TryInitialize(artifact.ToCalculationState()), Is.True);
            artifact = Property<IArtifactBufferReadSink>(setup.buffers, "ArtifactReadSink").Current;
            Property<ICurrencyBufferWriteSink>(setup.buffers, "CurrencyWriteSink").SetAmount(75);
            var runner = new JobRunnerSystem();
            var registry = CreateStageRegistry(setup.core, setup.buffers, runner);

            registry.Register(JobRootType.Stage, type);
            Assert.That(setup.stages.IsCleared(chapterKey, node.NodeId), Is.True,
                "Registration must not mutate buffers before the runner executes.");
            runner.Tick(0);

            Assert.That(runner.IsIdle, Is.True);
            Assert.That(setup.current.CurrentMapKey, Is.EqualTo(chapterKey));
            Assert.That(setup.current.HasSelection, Is.False);
            Assert.That(setup.stages.GetStages(chapterKey), Is.Not.Empty);
            Assert.That(setup.stages.IsCleared(chapterKey, node.NodeId), Is.EqualTo(type == JobType.Start));
            Assert.That(Property<ICurrencyBufferReadSink>(setup.buffers, "CurrencyReadSink").Amount,
                Is.EqualTo(type == JobType.NewRun ? NewRunCurrency : 75));
            Assert.That(Property<IArtifactBufferReadSink>(setup.buffers, "ArtifactReadSink").Current,
                Is.SameAs(artifact));
            // 완료된 작업은 다음 Tick에 새로 얻은 재화를 다시 지우지 않는다.
            Property<ICurrencyBufferWriteSink>(setup.buffers, "CurrencyWriteSink").SetAmount(9);
            runner.Tick(0);
            Assert.That(Property<ICurrencyBufferReadSink>(setup.buffers, "CurrencyReadSink").Amount, Is.EqualTo(9));
        }

        [Test]
        public void NewRun_FromLaterChapter_ReturnsToChapterOne()
        {
            var setup = Prepare("Nearest", true, 1);
            Property<IMapProgressBufferWriteSink>(setup.buffers, "MapProgressWriteSink").RegisterMap(7);
            setup.currentWrite.SelectMap(7);
            var runner = new JobRunnerSystem();
            var registry = CreateStageRegistry(setup.core, setup.buffers, runner);
            registry.Register(JobRootType.Stage, JobType.NewRun);
            Assert.That(setup.current.CurrentMapKey, Is.EqualTo(7));
            runner.Tick(0);
            Assert.That(setup.current.CurrentMapKey, Is.EqualTo(1));
            Assert.That(setup.current.HasSelection, Is.False);
            Assert.That(setup.stages.GetStages(1), Is.Not.Empty);
            Assert.That(runner.IsIdle, Is.True);
        }

        [Test]
        public void StartNewRun_InstallsRosterAndRunCurrency_AndRepeatDoesNotDoubleGrant()
        {
            var setup = Prepare("Nearest", true, 1);
            var b = (IBufferQuery)setup.buffers;
            b.CurrencyWriteSink.SetAmount(75);

            var first = setup.core.RunStart.StartNewRun(1);

            Assert.That(first.Succeeded, Is.True, first.FailureReason.ToString());
            Assert.That(b.CharacterReadSink.Owned.Count(o => o.Role == CharacterOwnershipRole.Main), Is.EqualTo(1));
            Assert.That(b.CharacterReadSink.Owned.Count(o => o.Role == CharacterOwnershipRole.Sub), Is.EqualTo(3));
            Assert.That(b.CharacterReadSink.TryGetMainState(out _), Is.True);
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(NewRunCurrency));
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));

            // 같은 요청을 다시 해도 이전 Run을 비운 뒤 설치하므로 로스터·재화가 늘어나지 않는다.
            b.CurrencyWriteSink.SetAmount(5);
            Assert.That(setup.core.RunStart.StartNewRun(1).Succeeded, Is.True);
            Assert.That(b.CharacterReadSink.Owned.Count, Is.EqualTo(4));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(NewRunCurrency));

            // 챕터 진입은 새 Run 뒤 흐름이 이어서 한다.
            Assert.That(setup.current.HasMap, Is.False);
            Assert.That(setup.core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
        }

        [TestCase(4, 3, TestName = "Default_Main1Sub3")]
        [TestCase(3, 2, TestName = "Configured_Main1Sub2")]
        public void StartNewRun_RecordsStartingHandSize_AndFirstDrawUsesIt(int startingHandSize, int firstSubDraws)
        {
            var setup = Prepare("Nearest", true, 1, startingHandSize: startingHandSize);
            var b = (IBufferQuery)setup.buffers;

            Assert.That(setup.core.RunStart.StartNewRun(1).Succeeded, Is.True);

            // 로드아웃의 첫 손패 수가 Run에 기록되고, 매 스테이지 첫 드로우가 그 값 - 1장(메인 제외)의 서브를 뽑는다.
            Assert.That(b.RunProgressReadSink.StartingHandSize, Is.EqualTo(startingHandSize));
            Assert.That(setup.core.Characters.PlanInitialDraw(42).InitialCount, Is.EqualTo(firstSubDraws));
            Assert.That(setup.core.Characters.PlanInitialDraw(7).InitialCount, Is.EqualTo(firstSubDraws));
        }

        [Test]
        public void StartNewRun_UnknownLoadout_ChangesNothing()
        {
            var setup = Prepare("Nearest", true, 1);
            var b = (IBufferQuery)setup.buffers;
            Assert.That(setup.core.RunStart.StartNewRun(1).Succeeded, Is.True);
            setup.core.ChapterProgression.EnterChapter(1);
            b.CurrencyWriteSink.SetAmount(12);
            var owned = b.CharacterReadSink.Owned.ToArray();
            var graph = setup.stages.GetStages(1);

            var rejected = setup.core.RunStart.StartNewRun(99);

            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(rejected.FailureReason, Is.EqualTo(RosterAcquireFailureReason.InvalidDefinition));
            Assert.That(b.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(12));
            Assert.That(setup.current.CurrentMapKey, Is.EqualTo(1));
            Assert.That(setup.stages.GetStages(1), Is.EqualTo(graph));
        }

        [Test]
        public void PublishNewRun_FailedRosterPreparation_ChangesNothing()
        {
            var setup = Prepare("Nearest", true, 1);
            var b = (IBufferQuery)setup.buffers;
            Assert.That(setup.core.RunStart.StartNewRun(1).Succeeded, Is.True);
            b.CurrencyWriteSink.SetAmount(12);
            var owned = b.CharacterReadSink.Owned.ToArray();
            // 현재 체력이 최대 체력보다 큰 메인 상태는 저장 불변식 위반이라 준비 단계에서 거부된다.
            var invalid = new ProjectL.Systems.RuntimeSystem.BufferSystems.Run.NewRunPreparation(
                new[] { new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main) },
                new MainCharacterPersistentState(20, 10, 1, 3, 3, 1), 50, 4);

            var result = b.RunProgressWriteSink.PublishNewRun(invalid);

            Assert.That(result.FailureReason, Is.EqualTo(RosterAcquireFailureReason.InvalidMainState));
            Assert.That(b.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(b.CurrencyReadSink.Amount, Is.EqualTo(12));
        }

        [Test]
        public void EnterBattle_WithoutRoster_IsRejectedBeforeAnyChange()
        {
            var setup = Prepare("Nearest", true, 1);
            var b = (IBufferQuery)setup.buffers;
            Assert.That(setup.core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            var node = setup.stages.GetStages(1).First(n => n.Depth == 1);

            var result = setup.core.Combat.EnterBattle(1, new StageNodeId(node.NodeId));

            Assert.That(result.FailureReason, Is.EqualTo(ChapterProgressionFailureReason.RosterNotInitialized));
            Assert.That(setup.current.HasSelection, Is.False);
            Assert.That(b.CombatStateReadSink.HasBattle, Is.False);
        }

        [Test]
        public void StageNone_DoesNotEnqueueOrChangeCurrentProgress()
        {
            var setup = Prepare("Nearest", true);
            var runner = new JobRunnerSystem();
            var registry = CreateStageRegistry(setup.core, setup.buffers, runner);
            registry.Register(JobRootType.Stage, JobType.None);
            Assert.That(runner.IsIdle, Is.True);
            Assert.That(setup.current.HasMap, Is.False);
            Assert.That(setup.stages.GetStages(7), Is.Empty);
        }
        private static JobRegisterSystem CreateStageRegistry(
            IInGameCoreSystemGroupService core, object buffers, IJobRunner runner)
        {
            var builder = new SystemBuilderHelper();
            Export(buffers, builder);
            builder.AddService<IInGameSystemGroupService>(new JobInGameService(core));
            TestLifeCycleGroup.Add(builder);
            var registry = new JobRegisterSystem(runner);
            registry.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            registry.Initialize();
            return registry;
        }

        private sealed class JobInGameService : IInGameSystemGroupService
        {
            public IInGameCoreSystemGroupService CoreGroup { get; }
            public IInGameViewSystemGroupService ViewGroup => null;
            internal JobInGameService(IInGameCoreSystemGroupService core) => CoreGroup = core;
        }

        // 진행 런타임은 InGame 조회만 읽으므로 View·Render 조회는 비워 둔다.
        private sealed class QueryContext : IDataBaseQueryContext
        {
            public IInGameQuery InGameQuery { get; }
            public IRenderQuery RenderQuery => null;
            public IViewQuery ViewQuery => null;
            internal QueryContext(IInGameQuery inGameQuery) => InGameQuery = inGameQuery;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var runtime in runtimes) runtime.Shutdown();
            runtimes.Clear();
            assets.DestroyAll();
        }

        [TestCase("Nearest")]
        [TestCase("Random")]
        [TestCase("WeightedRandom")]
        public void Entry_GeneratesFromRealDbQueries_AndReusesBufferGraph(string algorithm)
        {
            var setup = Prepare(algorithm, true);
            Assert.That(setup.stages.GetStages(7), Is.Empty);
            Assert.That(setup.current.CurrentMapKey, Is.Zero);

            Assert.That(setup.core.ChapterProgression.EnterChapter(7).Accepted, Is.True);
            var graph = setup.stages.GetStages(7).ToArray();
            Assert.That(setup.current.CurrentMapKey, Is.EqualTo(7));
            Assert.That(graph.Count(n => n.Depth == 0 && n.StageKey == 0), Is.EqualTo(1));
            Assert.That(graph.Count(n => n.Depth == 4 && n.StageKey == 900), Is.EqualTo(1));
            foreach (var node in graph)
            {
                if (node.Depth > 0 && node.Depth < 4)
                    Assert.That(node.StageKey, Is.EqualTo(100));
                if (node.Depth < 4) Assert.That(node.NextNodeIds, Is.Not.Empty);
                foreach (int next in node.NextNodeIds)
                    Assert.That(graph.Single(n => n.NodeId == next).Depth, Is.EqualTo(node.Depth + 1));
                if (node.Depth > 0)
                    Assert.That(graph.Any(n => n.NextNodeIds.Contains(node.NodeId)), Is.True);
            }

            var selected = graph.First(n => n.Depth == 1);
            setup.stageWrite.SetClear(7, selected.NodeId, true);
            setup.currentWrite.SelectStage(new StageNodeId(selected.NodeId), new StageKey(selected.StageKey), "Test stage");
            Assert.That(setup.core.ChapterProgression.EnterChapter(7).Accepted, Is.True);
            Assert.That(setup.stages.IsCleared(7, selected.NodeId), Is.True);
            Assert.That(setup.current.CurrentStage.NodeId, Is.EqualTo(selected.NodeId));
            CollectionAssert.AreEqual(graph.Select(n => n.NodeId),
                setup.stages.GetStages(7).Select(n => n.NodeId));
        }

        [Test]
        public void Entry_MissingBossFailsBeforeGraphRegistrationAndCurrentSelection()
        {
            var setup = Prepare("Nearest", false);
            Assert.Throws<InvalidOperationException>(() => setup.core.ChapterProgression.EnterChapter(7));
            Assert.That(setup.stages.GetStages(7), Is.Empty);
            Assert.That(setup.current.CurrentMapKey, Is.Zero);
            Assert.That(setup.current.HasSelection, Is.False);
        }

        [Test]
        public void StageEntry_ExplicitConfirmationRecordsSelectionAndRejectsRepeat()
        {
            var setup = Prepare("Nearest", true);
            setup.core.ChapterProgression.EnterChapter(7);
            StageProgressData target = setup.stages.GetStages(7).Single(n => n.Depth == 1);

            ChapterProgressionResult accepted = setup.core.ChapterProgression.ConfirmStageEntry(
                7, new StageNodeId(target.NodeId));
            ChapterProgressionResult repeated = setup.core.ChapterProgression.ConfirmStageEntry(
                7, new StageNodeId(target.NodeId));

            Assert.That(accepted.Accepted && accepted.SelectStage, Is.True);
            Assert.That(setup.current.HasSelection, Is.True);
            Assert.That(setup.current.CurrentStage.NodeId, Is.EqualTo(target.NodeId));
            Assert.That(setup.current.CurrentStage.StageKey, Is.EqualTo(100));
            Assert.That(setup.current.CurrentStage.StageName, Is.EqualTo("Stage 100"));
            Assert.That(repeated.FailureReason,
                Is.EqualTo(ChapterProgressionFailureReason.NavigationRejected));
        }

        [Test]
        public void StagePreparation_InvalidDataDoesNotCommitSelection()
        {
            var setup = Prepare("Nearest", true);
            setup.stageWrite.RegisterStages(7, new[]
            {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 999, 1, 0, Array.Empty<int>())
            });
            setup.core.ChapterProgression.EnterChapter(7);

            Assert.Throws<KeyNotFoundException>(() => setup.core.ChapterProgression.PrepareStageEntry(
                7, new StageNodeId(2)));
            Assert.That(setup.current.HasSelection, Is.False);
        }

        [Test]
        public void StageVictory_ClearsStageAndBossAlsoClearsMap()
        {
            var victory = Prepare("Nearest", true);
            victory.core.ChapterProgression.EnterChapter(7);
            StageProgressData node = victory.stages.GetStages(7).Single(n => n.Depth == 1);
            victory.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(node.NodeId));
            ChapterProgressionResult regular = victory.core.ChapterProgression.ResolveStage(
                7, StageOutcome.Victory);
            Assert.That(regular.ClearStage && !regular.ClearMap, Is.True);
            Assert.That(victory.stages.IsCleared(7, node.NodeId), Is.True);

            while (node.NextNodeIds.Count > 0)
            {
                int nextId = node.NextNodeIds[0];
                node = victory.stages.GetStages(7).Single(n => n.NodeId == nextId);
                victory.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(nextId));
                regular = victory.core.ChapterProgression.ResolveStage(7, StageOutcome.Victory);
            }
            Assert.That(regular.ClearStage && regular.ClearMap, Is.True);
            Assert.That(victory.maps.GetMap(7).IsClear, Is.True);
        }

        [Test]
        public void StageDefeat_RequestsAttemptEndWithoutClearingProgress()
        {
            var defeat = Prepare("Nearest", true);
            defeat.core.ChapterProgression.EnterChapter(7);
            StageProgressData defeated = defeat.stages.GetStages(7).Single(n => n.Depth == 1);
            defeat.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(defeated.NodeId));
            ChapterProgressionResult lost = defeat.core.ChapterProgression.PlanStageResolution(
                7, StageOutcome.Defeat);
            Assert.That(lost.EndAttempt, Is.True);
            var b = (IBufferQuery)defeat.buffers;
            var selected = defeat.current.CurrentStage;
            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                defeat.core.ChapterProgression.ResolveStage(7, StageOutcome.Defeat)).Message,
                Does.Contain("settlement"));
            Assert.That(defeat.current.CurrentStage, Is.EqualTo(selected));
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(b.RunProgressReadSink.Completion, Is.Null);
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(defeat.stages.IsCleared(7, defeated.NodeId), Is.False);
            Assert.That(defeat.maps.GetMap(7).IsClear, Is.False);
        }

        [Test]
        public void CombatService_UsesBufferValuesAndRecordsAbandonmentBeforeCleanup()
        {
            var setup = Prepare("Nearest", true);
            var b = (IBufferQuery)setup.buffers;
            setup.core.ChapterProgression.EnterChapter(7);
            var node = setup.stages.GetStages(7).First(n => n.Depth == 1);
            setup.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(node.NodeId));
            var persistent = CharacterBufferTestData.MainState(7);
            var owned = b.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, persistent).Acquired.Single();
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[]
            {
                CharacterBufferTestData.MainFacts(owned.Id, persistent)
            }, CharacterBufferTestData.Combat()));
            var loadout = new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>());
            setup.core.Combat.ApplyHealth(new EntityId(1), 4);
            Assert.That(b.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(setup.core.Combat.ResolveBattle(loadout), Is.False);
            Assert.Throws<InvalidOperationException>(() => setup.core.Combat.EndBattle());
            Assert.That(setup.core.Combat.AbandonBattle(), Is.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned));
            Assert.That(setup.core.Combat.AbandonBattle(), Is.Not.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned));
            Assert.That(b.CombatStateReadSink.Completion.Outcome.Reason, Is.EqualTo(StageOutcomeReason.VoluntaryAbandonment));
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            Assert.That(b.CharacterReadSink.Characters, Is.Not.Empty);
            Assert.That(setup.stages.IsCleared(7, node.NodeId), Is.False);
            setup.core.Combat.EndBattle();
            Assert.That(b.CharacterReadSink.Characters, Is.Empty);
            b.CharacterReadSink.TryGetMainState(out var final);
            Assert.That(final.CurrentHealth, Is.EqualTo(4));
            Assert.That(b.CombatStateReadSink.Completion, Is.Not.Null);
        }

        [Test]
        public void CombatService_TurnDeathPublishesFinalHealthAndDefeatTogether()
        {
            var setup = Prepare("Nearest", true);
            var b = (IBufferQuery)setup.buffers;
            setup.core.ChapterProgression.EnterChapter(7);
            var node = setup.stages.GetStages(7).First(n => n.Depth == 1);
            setup.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(node.NodeId));
            var persistent = CharacterBufferTestData.MainState(7);
            var owned = b.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, persistent).Acquired.Single();
            var main = CharacterBufferTestData.MainFacts(owned.Id, persistent);
            b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { main }, CharacterBufferTestData.Combat()));
            var field = b.CombatStateReadSink.SoulField;
            var settled = new SoulActionChanges(CharacterSettlementCalculator.SetHealth(main, persistent, 0),
                new SoulFieldChange(field, field), Array.Empty<long>());
            setup.core.Combat.FinalizeTurn(main.EntityId, settled,
                new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()));
            Assert.That(b.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.Zero);
            Assert.That(b.CharacterReadSink.Characters.Single().Stats.CurrentActionCost, Is.Zero);
            Assert.That(b.CombatStateReadSink.Completion.Outcome.Reason, Is.EqualTo(StageOutcomeReason.PlayerMainCharacterDied));
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            Assert.That(b.StageProgressReadSink.IsCleared(7, node.NodeId), Is.False);
            setup.core.Combat.EndBattle();
            b.CharacterReadSink.TryGetMainState(out var final);
            Assert.That(final.CurrentHealth, Is.Zero);
        }

        [Test]
        public void BossCompletionAndChapterEntry_PreserveStageAndMapRecords()
        {
            var route = new ChapterContinuationTestSource { Definition =
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap.ChapterContinuationDefinition.Next(7, 8) };
            var setup = Prepare("Nearest", true, spyReset: true, continuationSource: route);
            var b = (IBufferQuery)setup.buffers;
            setup.core.ChapterProgression.EnterChapter(7);
            var sourceGraph = setup.stages.GetStages(7).Select(n => n.MapRole == StageMapRole.Boss
                ? new StageProgressData(n.NodeId, n.StageKey, n.Depth, n.WidthIndex, n.NextNodeIds, n.IsClear, StageType.Combat, StageMapRole.Boss)
                : n).ToArray();
            setup.stageWrite.Clear();
            setup.stageWrite.RegisterStages(7, sourceGraph);
            var continuationDatabase = new ChapterContinuationTestDatabase {
                RootOverride = key => key == 100 ? new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item.StageRootData(100, 7, StageMapRole.Regular,
                    new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event.EventStageDetail(new[] { 1 }))
                    : CoreStageCombatCycleTests.CycleDatabase.Stage(key, key == 900 ? 7 : 8, StageMapRole.Boss),
                MapOverride = key => new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item.StageMapData(key, 2, 1, 0,
                    ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item.StageConnectionAlgorithm.Nearest, 1, 1, 42, false) };
            ((ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem.StageServiceManager)setup.core.StageServiceManager).InitData(continuationDatabase);
            var persistent = CharacterBufferTestData.MainState(7);
            var owned = b.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, persistent).Acquired.Single();
            var node = setup.stages.GetStages(7).First(n => n.Depth == 0);
            while (node.NextNodeIds.Count > 0)
            {
                node = setup.stages.GetStages(7).Single(n => n.NodeId == node.NextNodeIds[0]);
                Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(7, new StageNodeId(node.NodeId)).Accepted, Is.True);
                b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[]
                {
                    CharacterBufferTestData.MainFacts(owned.Id, persistent)
                }, CharacterBufferTestData.Combat()));
                var progress = setup.core.ChapterProgression.PlanStageResolution(7, StageOutcome.Victory);
                Assert.That(b.StageProgressReadSink.IsCleared(7, node.NodeId), Is.False);
                var completion = new BattleCompletionCalculator(new ArtifactLoadoutSnapshot(0,
                    Array.Empty<ArtifactPlacedEffectSnapshot>())).Complete(b.CharacterReadSink.Characters.Single().ToFacts(),
                    persistent, new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed), progress);
                b.BattleWriteSink.CompleteBattle(completion);
                b.CombatViewWriteSink.AcknowledgePresented(Math.Max(1, b.CombatStateReadSink.LatestResultSequence));
                Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.GreaterThan(0));
                setup.core.Combat.EndBattle();
                Assert.That(b.StageProgressReadSink.IsCleared(7, node.NodeId), Is.True);
                if (node.NextNodeIds.Count > 0)
                    Assert.That(setup.core.ChapterProgression.EnterChapter(7).Accepted, Is.True);
            }
            Assert.That(b.MapProgressReadSink.GetMap(7).IsClear, Is.True);
            b.MapProgressWriteSink.RegisterMap(8);
            b.StageProgressWriteSink.RegisterStages(8, new[]
            {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 800, 1, 0, Array.Empty<int>(), stageType: StageType.Combat, mapRole: StageMapRole.Boss)
            });
            Assert.That(setup.core.ChapterProgression.EnterChapter(8).Accepted, Is.True);
            Assert.That(b.MapProgressReadSink.GetMap(7).IsClear, Is.True);
            Assert.That(b.StageProgressReadSink.IsCleared(7, node.NodeId), Is.True);
            b.CharacterReadSink.TryGetMainState(out var final);
            Assert.That(final.CurrentHealth, Is.EqualTo(7));
            Assert.That(b.CharacterReadSink.Owned.Count, Is.EqualTo(1));
            Assert.That(((ResetSpy)setup.buffers).ResetCalls, Is.Zero);
        }

        [Test]
        public void StagePreparation_ValidationAloneDoesNotRecordPlayableEntry()
        {
            var setup = Prepare("Nearest", true);
            setup.core.ChapterProgression.EnterChapter(7);
            var node = setup.stages.GetStages(7).First(n => n.Depth == 1);
            var decision = setup.core.ChapterProgression.PrepareStageEntry(7, new StageNodeId(node.NodeId));
            Assert.That(decision.Accepted, Is.True);
            Assert.That(setup.current.HasSelection, Is.False);
            Assert.That(setup.current.CurrentStage.NodeId, Is.Zero);
            Assert.That(setup.stages.IsCleared(7, node.NodeId), Is.False);
        }

        [Test]
        public void StageConfirmation_RechecksBufferAndRejectsAlreadyClearedTarget()
        {
            var setup = Prepare("Nearest", true);
            setup.core.ChapterProgression.EnterChapter(7);
            var node = setup.stages.GetStages(7).First(n => n.Depth == 1);
            Assert.That(setup.core.ChapterProgression.PrepareStageEntry(
                7, new StageNodeId(node.NodeId)).Accepted, Is.True);
            setup.stageWrite.SetClear(7, node.NodeId, true);
            Assert.That(setup.core.ChapterProgression.ConfirmStageEntry(
                7, new StageNodeId(node.NodeId)).Accepted, Is.False);
            Assert.That(setup.current.HasSelection, Is.False);
        }

        [Test]
        public void ChapterEntry_PreservesOtherChapterCompletionAndUnrelatedBuffers()
        {
            var setup = Prepare("Nearest", true);
            var mapWrite = Property<IMapProgressBufferWriteSink>(setup.buffers, "MapProgressWriteSink");
            var artifact = ArtifactSnapshot();
            var character = Property<ICharacterBufferWriteSink>(
                setup.buffers, "CharacterWriteSink").Acquire(new[]
                {
                    new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
                }).Acquired.Single();
            Assert.That(Property<IArtifactInitializationWriteSink>(setup.buffers, "ArtifactInitializationWriteSink").TryInitialize(artifact.ToCalculationState()), Is.True);
            artifact = Property<IArtifactBufferReadSink>(setup.buffers, "ArtifactReadSink").Current;
            Property<ICurrencyBufferWriteSink>(setup.buffers, "CurrencyWriteSink").SetAmount(75);
            mapWrite.RegisterMap(8);
            mapWrite.SetClear(8, true);
            setup.stageWrite.RegisterStages(8, new[] {
                new StageProgressData(201, 100, 1, 0, Array.Empty<int>(), true) });

            Assert.That(setup.core.ChapterProgression.EnterChapter(7).Accepted, Is.True);
            Assert.That(setup.core.ChapterProgression.EnterChapter(7).Accepted, Is.True);

            Assert.That(setup.maps.GetMap(8).IsClear, Is.True);
            Assert.That(setup.stages.IsCleared(8, 201), Is.True);
            Assert.That(Property<ICurrencyBufferReadSink>(setup.buffers, "CurrencyReadSink").Amount, Is.EqualTo(75));
            Assert.That(Property<ICharacterBufferReadSink>(setup.buffers,
                "CharacterReadSink").Owned.Single(), Is.SameAs(character));
            Assert.That(Property<IArtifactBufferReadSink>(setup.buffers, "ArtifactReadSink").Current, Is.SameAs(artifact));
            Assert.That(setup.current.CurrentMapKey, Is.EqualTo(7));
            Assert.That(setup.current.HasSelection, Is.False);
        }

        [Test]
        public void BufferReset_LeavesGraphEmptyUntilExplicitChapterPreparation()
        {
            var setup = Prepare("Random", true);
            setup.core.ChapterProgression.EnterChapter(7);
            var node = setup.stages.GetStages(7).First(n => n.Depth == 1);
            setup.currentWrite.SelectStage(new StageNodeId(node.NodeId), new StageKey(node.StageKey), "Old stage");
            setup.stageWrite.SetClear(7, node.NodeId, true);
            Property<IMapProgressBufferWriteSink>(setup.buffers, "MapProgressWriteSink").SetClear(7, true);

            Property<ProjectL.Systems.RuntimeSystem.BufferSystems.Core.IRunProgressBufferWriteSink>(
                setup.buffers, "RunProgressWriteSink").ResetRunProgress();

            Assert.That(setup.stages.GetStages(7), Is.Empty);
            Assert.That(setup.current.HasMap, Is.False);
            Assert.That(setup.maps.GetMap(7).IsClear, Is.False);

            Assert.That(setup.core.ChapterProgression.EnterChapter(7).Accepted, Is.True);
            Assert.That(setup.stages.GetStages(7).Any(n => n.Depth == 1), Is.True);
            Assert.That(setup.stages.GetStages(7).All(n => !n.IsClear), Is.True);
            Assert.That(setup.current.CurrentMapKey, Is.EqualTo(7));
            Assert.That(setup.current.HasSelection, Is.False);
        }

        private (IInGameCoreSystemGroupService core, IStageProgressBufferReadSink stages,
            IStageProgressBufferWriteSink stageWrite, ICurrentProgressBufferReadSink current,
            ICurrentProgressBufferWriteSink currentWrite, IMapProgressBufferReadSink maps, object buffers,
            FrameworkRuntime runtime)
            Prepare(string algorithm, bool includeBoss, int chapterKey = 7, bool withJobRuntime = false,
                int seed = 192, int depthCount = 5, int widthMax = 3,
                float additionalChance = 0.25f, bool spyReset = false, int startingHandSize = 4,
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem.IChapterContinuationSource continuationSource = null)
        {
            var map = assets.Create<StageMapDataSO>();
            // GetData는 매번 새 Data를 만들므로 값은 SO 직렬화 필드에 넣는다.
            Set(map, "mapKey", chapterKey);
            Set(map, "depthCount", depthCount);
            Set(map, "widthMax", widthMax);
            Set(map, "seed", seed);
            Set(map, "additionalConnectionChance", additionalChance);
            Set(map, "connectionAlgorithm", Enum.Parse(Get(map, "connectionAlgorithm").GetType(), algorithm));
            // 회복(Event) 스테이지 검증은 선택지를 하나 이상 요구하므로 모든 테스트 스테이지가 같은 선택지를 가리킨다.
            var recoveryOption = assets.Create<RecoveryOptionDataSO>();
            var stageItems = new List<DatabaseItem> { Stage(100, StageMapRole.Regular, chapterKey, recoveryOption) };
            if (includeBoss) stageItems.Add(Stage(900, StageMapRole.Boss, chapterKey, recoveryOption));

            // 새 Run 시작(NewRun)은 시작 로드아웃이 필요하므로 메인 1명과 서브 1종 3장을 둔다.
            var targeting = assets.Create<TargetingMethodDataSO>();
            var mainUnit = assets.Create<UnitDataSO>();
            Set(mainUnit, "key", EntityKey.Unit_MainCharacter);
            Set(mainUnit, "role", UnitRole.MainCharacter);
            Set(mainUnit, "targetingMethod", targeting);
            var subUnit = assets.Create<UnitDataSO>();
            Set(subUnit, "key", EntityKey.Unit_Akimbo);
            Set(subUnit, "targetingMethod", targeting);
            var loadout = assets.Create<PlayerLoadoutDataSO>();
            Set(loadout, "loadoutKey", 1);
            Set(loadout, "mainCharacter", mainUnit);
            Set(loadout, "entries", DataBaseTestAssets.Entries(typeof(PlayerLoadoutDataSO), "Entry",
                new[] { ("unitDefinition", (object)subUnit), ("count", (object)3) }));
            Set(loadout, "startingRunCurrency", NewRunCurrency);
            Set(loadout, "startingHandSize", startingHandSize);

            // 실제 InGame DB 팩토리로 조립하고 테스트 데이터만 메모리에서 공급한다.
            // CategoryConfig는 항목이 정확히 하나 있어야 하므로 기본값 하나를 넣는다.
            InGameGroup group = assets.Group<InGameGroup>(
                assets.Table<StageMapDataTableSO>(map),
                assets.Table<StageRootDataTableSO>(stageItems.ToArray()),
                assets.Table<FieldDataTableSO>(),
                assets.Table<UnitDataTableSO>(mainUnit, subUnit),
                assets.Table<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.TargetingMethodDataTableSO>(targeting),
                assets.Table<StructureDataTableSO>(),
                assets.Table<EnemyLoadoutDataTableSO>(),
                assets.Table<PlayerLoadoutDataTableSO>(loadout),
                assets.Table<UnitRosterDataTableSO>(),
                assets.Table<ArtifactDefinitionDataTableSO>(),
                assets.Table<CategoryConfigDataTableSO>(assets.Create<CategoryConfigDataSO>()),
                assets.Table<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.ArtifactBoardConfigDataTableSO>(
                    assets.Create<ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item.ArtifactBoardConfigDataSO>()),
                assets.Table<StageEconomyDataTableSO>(),
                assets.Table<RecoveryOptionDataTableSO>(recoveryOption));
            var context = new QueryContext((IInGameQuery)InGameDataBaseFactory.Create(group));

            object buffers;
            IInGameCoreSystemGroupService core;
            FrameworkRuntime runtime;
            if (withJobRuntime)
            {
                // 렌더링과 씬 없이 실제 진행 관련 Factory만 테스트 루트에 조립한다.
                var builder = new SystemBuilderHelper();
                buffers = Factory("Factory.Runtime.Buffer.BufferSystemFactory");
                if (spyReset) buffers = new ResetSpy((IBufferQuery)buffers);
                // 진행 테스트는 Global과 실제 View 없이 Core와 Job만 조립한다.
                core = (IInGameCoreSystemGroupService)ProjectL.Systems.Factory.Runtime.InGame.Core.InGameCoreSystemFactory.Create(null, continuationSource);
                Export(buffers, builder);
                Export(Factory("Factory.Runtime.Global.JobSystemFactory"), builder);
                Export(core, builder);
                builder.AddService<IInGameSystemGroupService>(new JobInGameService(core));
                runtime = new FrameworkRuntime(builder.BuildSystemReader());
                runtimes.Add(runtime);
            }
            else
            {
                buffers = Factory("Factory.Runtime.Buffer.BufferSystemFactory");
                if (spyReset) buffers = new ResetSpy((IBufferQuery)buffers);
                core = (IInGameCoreSystemGroupService)ProjectL.Systems.Factory.Runtime.InGame.Core.InGameCoreSystemFactory.Create(null, continuationSource);
                var builder = new SystemBuilderHelper();
                Export(buffers, builder);
                Export(core, builder);
                runtime = new FrameworkRuntime(builder.BuildSystemReader());
            }
            runtime.InitData(context);
            runtime.PostBindService();
            if (withJobRuntime) runtime.Initialize();
            return (core, Property<IStageProgressBufferReadSink>(buffers, "StageProgressReadSink"),
                Property<IStageProgressBufferWriteSink>(buffers, "StageProgressWriteSink"),
                Property<ICurrentProgressBufferReadSink>(buffers, "CurrentProgressReadSink"),
                Property<ICurrentProgressBufferWriteSink>(buffers, "CurrentProgressWriteSink"),
                Property<IMapProgressBufferReadSink>(buffers, "MapProgressReadSink"), buffers, runtime);
        }

        /// <summary>테스트 시작 로드아웃의 Run 시작 재화. 다른 테스트 금액(75, 9)과 겹치지 않게 고른다.</summary>
        private const int NewRunCurrency = 30;

        private StageRootDataSO Stage(int key, StageMapRole role, int chapterKey,
            RecoveryOptionDataSO recoveryOption)
        {
            var item = assets.Create<StageRootDataSO>();
            Set(item, "stageKey", key);
            Set(item, "mapKey", chapterKey);
            Set(item, "mapRole", role);
            var eventData = assets.Create<EventStageData>();
            Set(eventData, "recoveryOptions", new[] { recoveryOption });
            Set(item, "stageData", eventData);
            return item;
        }

        // Artifact 버퍼는 스냅샷 단위로 교체되므로 보존 여부는 같은 스냅샷인지로 확인한다.
        // 테스트 DB에는 Artifact 정의가 없으므로 배치하지 않은(보유만 한) 항목을 쓴다. 배치 항목은 전투 정산 때 정의를 읽는다.
        private static ArtifactBufferSnapshot ArtifactSnapshot() => new(new ArtifactState(6, 6,
            new[] { new ArtifactItemState(1001, 1, false, default, default, new[] { CellCoord.Zero }) },
            Array.Empty<ArtifactItemState>()));

        // 내부 타입의 공개 범위를 바꾸지 않고 기존 생성자와 팩토리를 호출한다.
        private static Type TypeOf(string name) => Systems.GetType(Root + name, true);
        private static object Factory(string name)
        {
            var method = TypeOf(name).GetMethod("Create", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return method.Invoke(null, method.GetParameters().Select(p => p.DefaultValue).ToArray());
        }
        private static void Export(object value, SystemBuilderHelper builder) => ((IExportSystemServices)value).ExportServices(builder);
        private static T Property<T>(object value, string name) => (T)value.GetType().GetProperty(name).GetValue(value);
        private static void Set(object value, string name, object data) => DataBaseTestAssets.Set(value, name, data);
        private static object Get(object value, string name) => DataBaseTestAssets.Get(value, name);
    }
}
