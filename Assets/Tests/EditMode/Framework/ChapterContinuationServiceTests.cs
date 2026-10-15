using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Tests.Framework
{
    public sealed class ChapterContinuationServiceTests
    {
        private FrameworkRuntime _runtime;
        private IInGameCoreSystemGroupService _core;
        private IBufferQuery _b;
        private ChapterContinuationTestDatabase _db;
        private ChapterContinuationTestSource _routes;
        private CoreStageCombatCycleTests.CycleSource _battle;

        [SetUp]
        public void SetUp() => Compose();
        private void Compose(bool defaultSource = false, bool bindFirst = false)
        {
            _runtime?.Shutdown();
            _db = new ChapterContinuationTestDatabase();
            _routes = new ChapterContinuationTestSource();
            _battle = new CoreStageCombatCycleTests.CycleSource(_db);
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create(_battle, defaultSource ? null : _routes).ExportServices(builder);
            _runtime = new FrameworkRuntime(builder.BuildSystemReader());
            if (bindFirst) _runtime.PostBindService();
            _runtime.InitData(_db);
            if (!bindFirst) _runtime.PostBindService();
            _b = _runtime.GetService(BufferSystemKey.Buffer);
            _core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
            Assert.That(_core.Characters.InitializeRoster(1).Succeeded, Is.True);
            _b.CurrencyWriteSink.SetAmount(91);
            Assert.That(_core.Artifacts.InitializeBoard(4, 4), Is.True);
            _core.Artifacts.Place(100, CellCoord.Zero, Rotation4.R0);
            _b.StageProgressWriteSink.RegisterStages(1, Graph(1));
            Assert.That(_core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
        }
        [TearDown] public void TearDown() => _runtime?.Shutdown();

        internal static StageProgressData[] Graph(int chapter) => new[] {
            new StageProgressData(1, 0, 0, 0, new[] { 2 }),
            new StageProgressData(2, chapter * 100 + 1, 1, 0, new[] { 3 }, stageType: StageType.Combat),
            new StageProgressData(3, chapter * 100 + 2, 2, 0, Array.Empty<int>(), stageType: StageType.Combat, mapRole: StageMapRole.Boss) };

        private void Win(int node)
        {
            Assert.That(_core.Combat.EnterBattle(1, new StageNodeId(node)).Accepted, Is.True);
            Command(CombatViewCommandData.EnterCombat());
            for (int i = 0; i < 8 && _b.CombatStateReadSink.Phase == CombatFlowPhase.Automatic; i++)
                Assert.That(_core.Combat.AdvanceCombat(), Is.True);
            var main = _b.CharacterReadSink.Characters.Single(x => x.OwnershipRole == CharacterOwnershipRole.Main);
            Command(MovementPublicationTests.PreviewedMove(_b, _core.Combat, main.EntityId, _b.CharacterReadSink.Characters.Single(c => c.EntityId == main.EntityId).Anchor.Value, new CellCoord(1, 2)));
            Command(CombatViewCommandData.Attack(main.EntityId, AttackAim.ForCell(new CellCoord(1, 3))));
            Assert.That(_b.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
        }
        private void Command(CombatViewCommandData command)
        {
            _b.CombatViewWriteSink.Submit(command);
            Assert.That(_core.Combat.ProcessNextCommand(), Is.True);
        }
        private void Boss(bool cleanup = true)
        {
            Win(2); _core.Combat.EndBattle(); Win(3);
            if (cleanup) _core.Combat.EndBattle();
        }
        private ChapterContinuationResult Complete() => _core.ChapterProgression.CompleteChapter(1, new StageNodeId(3));

        [TestCase(false)]
        [TestCase(true)]
        public void BossCleanup_CompatibilityPreparationRejected_ContinuationStillSucceeds(bool final)
        {
            Boss();
            _routes.Definition = final ? ChapterContinuationDefinition.Final(1) : ChapterContinuationDefinition.Next(1, 2);
            var before = new RecoveryBufferSnapshot(_b);
            int definitions = _battle.Reads.Count;
            var cache = _core.Combat.GetType().GetField("_definitions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(cache.GetValue(_core.Combat), Is.Null);
            Assert.Throws<InvalidOperationException>(() => _core.Combat.PrepareBattle());
            before.AssertUnchanged(_b);
            Assert.That(_battle.Reads.Count, Is.EqualTo(definitions));
            Assert.That(cache.GetValue(_core.Combat), Is.Null);
            var completion = _b.CombatStateReadSink.Completion;
            var roster = _b.CharacterReadSink.Owned.ToArray();
            _b.CharacterReadSink.TryGetMainState(out var main);
            var artifact = _b.ArtifactReadSink.Current;
            int gold = _b.CurrencyReadSink.Amount, counter = _b.CombatStateReadSink.LatestResultSequence;
            var result = Complete();
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Kind, Is.EqualTo(final ? ChapterContinuationKind.RunCompleted : ChapterContinuationKind.NextChapterSelected));
            Assert.That(_b.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(final ? 1 : 2));
            Assert.That(_b.CurrentProgressReadSink.HasSelection, Is.EqualTo(final));
            Assert.That(_b.RunProgressReadSink.Outcome, Is.EqualTo(final ? RunOutcome.Victory : RunOutcome.InProgress));
            Assert.That(_b.CombatStateReadSink.Completion, Is.SameAs(completion));
            Assert.That(_b.CharacterReadSink.Owned, Is.EqualTo(roster));
            _b.CharacterReadSink.TryGetMainState(out var afterMain);
            Assert.That(afterMain, Is.SameAs(main));
            Assert.That(_b.CurrencyReadSink.Amount, Is.EqualTo(gold));
            Assert.That(_b.ArtifactReadSink.Current, Is.SameAs(artifact));
            Assert.That(_b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(counter));
            Assert.That(Complete().FailureReason, Is.EqualTo(final ? ChapterContinuationFailureReason.RunEnded : ChapterContinuationFailureReason.ChapterMismatch));
            if (final)
            {
                Assert.That(_core.ChapterProgression.Completion.NodeId, Is.EqualTo(new StageNodeId(3)));
                Assert.That(_core.ChapterProgression.Completion.StageKey, Is.EqualTo(new StageKey(102)));
                _core.Combat.EndBattle();
                Assert.That(_core.Combat.ProcessNextCommand(), Is.False);
            }
            else Assert.That(_b.StageProgressReadSink.GetStages(2).Count, Is.EqualTo(3));
        }

        [Test]
        public void RetainedBattle_BlocksContinuationUntilCallerEndBattle()
        {
            Boss(false);
            var before = new RecoveryBufferSnapshot(_b);
            Assert.That(Complete().FailureReason, Is.EqualTo(ChapterContinuationFailureReason.BattleRetained));
            before.AssertUnchanged(_b);
            Assert.That(_routes.Reads, Is.Zero);
            _core.Combat.EndBattle();
            Assert.That(Complete().Accepted, Is.True);
        }

        [Test]
        public void ClearFlagsWithoutMatchingBossCompletion_AreRejected()
        {
            _b.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(102), "Forged");
            _b.StageProgressWriteSink.SetClear(1, 3, true);
            _b.MapProgressWriteSink.SetClear(1, true);
            var before = new RecoveryBufferSnapshot(_b);
            Assert.That(Complete().FailureReason, Is.EqualTo(ChapterContinuationFailureReason.CompletionMismatch));
            before.AssertUnchanged(_b);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingDefinition_DoesNotMeanFinal(bool defaults)
        {
            if (defaults) Compose(true);
            Boss(); _routes.Definition = null;
            var before = new RecoveryBufferSnapshot(_b);
            Assert.That(Complete().FailureReason, Is.EqualTo(ChapterContinuationFailureReason.DefinitionUnavailable));
            before.AssertUnchanged(_b);
            Assert.That(_b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
        }

        [TestCase("route")]
        [TestCase("map:2")]
        [TestCase("stages:2")]
        [TestCase("root:202")]
        public void ContinuationFault_AfterSettlement_RetryOnlyCompleteChapter(string query)
        {
            Boss(); var before = new RecoveryBufferSnapshot(_b);
            if (query == "route") _routes.Failure = new InvalidOperationException("Fixture route fault");
            else _db.OnQuery = step => { if (step == query) throw new InvalidOperationException("Fixture lookup fault"); };
            Assert.Throws<InvalidOperationException>(() => Complete());
            before.AssertUnchanged(_b);
            Assert.That(_b.StageProgressReadSink.GetStages(2), Is.Empty);
            _routes.Failure = null; _db.OnQuery = null;
            Assert.That(Complete().Accepted, Is.True);
            Assert.That(_b.StageProgressReadSink.GetStages(2).Count, Is.EqualTo(3));
        }

        [TestCase("route", "selection")]
        [TestCase("root:102", "source")]
        [TestCase("root:202", "source")]
        [TestCase("root:202", "target")]
        [TestCase("stages:2", "terminal")]
        [TestCase("root:202", "battle")]
        [TestCase("route", "target")]
        [TestCase("root:202", "map")]
        [TestCase("root:102", "terminal")]
        [TestCase("root:202", "selection")]
        public void LateLookupMutation_AddsNoContinuationWrites(string query, string mutation)
        {
            Boss(); RecoveryBufferSnapshot afterMutation = null;
            Action mutate = () => {
                if (afterMutation != null) return;
                switch (mutation)
                {
                    case "selection": _b.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "Changed"); break;
                    case "source": _b.StageProgressWriteSink.SetClear(1, 2, false); break;
                    case "target": _b.StageProgressWriteSink.RegisterStages(2, Graph(2)); break;
                    case "terminal": RunTerminationAdmissionTests.Seed(_b, RunOutcome.Defeat); break;
                    case "battle":
                        _b.CharacterReadSink.TryGetMainState(out var main);
                        var owned = _b.CharacterReadSink.Owned.Single(x => x.Role == CharacterOwnershipRole.Main);
                        _b.BattleWriteSink.PrepareBattle(new BattlePreparation(new[] { CharacterBufferTestData.MainFacts(owned.Id, main) }, CharacterBufferTestData.Combat())); break;
                    case "map": _b.MapProgressWriteSink.SetClear(2, true); break;
                }
                afterMutation = new RecoveryBufferSnapshot(_b);
            };
            if (query == "route") _routes.BeforeRead = mutate;
            else _db.OnQuery = step => { if (step == query) mutate(); };
            Assert.That(Complete().Accepted, Is.False);
            Assert.That(afterMutation, Is.Not.Null);
            afterMutation.AssertUnchanged(_b);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConfiguredTarget_IsTheOnlyAllowedSuccessor(bool final)
        {
            Boss();
            if (final) _routes.Definition = ChapterContinuationDefinition.Final(1);
            var before = new RecoveryBufferSnapshot(_b);
            Assert.That(_core.ChapterProgression.EnterChapter(3).Accepted, Is.False);
            before.AssertUnchanged(_b);
            Assert.That(_core.ChapterProgression.EnterChapter(2).Accepted, Is.EqualTo(!final));
            Assert.That(_b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RegisteredTarget_IsValidatedAndReused(bool invalid)
        {
            Boss(); var graph = Graph(2);
            if (invalid) graph[2] = new StageProgressData(3, 202, 2, 0, Array.Empty<int>(), stageType: StageType.Event);
            _b.StageProgressWriteSink.RegisterStages(2, graph);
            var before = new RecoveryBufferSnapshot(_b);
            var result = Complete();
            Assert.That(result.Accepted, Is.EqualTo(!invalid));
            if (invalid) before.AssertUnchanged(_b);
            else Assert.That(_b.StageProgressReadSink.GetStages(2), Is.EqualTo(graph));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Composition_InitAndBindOrdersUseSameCoordinator(bool bindFirst)
        {
            Compose(bindFirst: bindFirst); Boss();
            Assert.That(Complete().Accepted, Is.True);
            Assert.That(_core.ChapterProgression.Outcome, Is.EqualTo(_b.RunProgressReadSink.Outcome));
        }

        [TestCase("source-key")]
        [TestCase("source-map")]
        [TestCase("source-role")]
        [TestCase("target-key")]
        [TestCase("target-map")]
        [TestCase("target-role")]
        [TestCase("map-key")]
        [TestCase("route-key")]
        public void InvalidDefinitionIdentity_RejectsBeforePublication(string fault)
        {
            Boss();
            if (fault == "route-key") _routes.Definition = ChapterContinuationDefinition.Final(3);
            else if (fault == "map-key") _db.MapOverride = key => new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item.StageMapData(3, 3, 1, 0,
                ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item.StageConnectionAlgorithm.Nearest, 1, 1, 42, false);
            else _db.RootOverride = key => {
                bool source = key == 102 && fault.StartsWith("source");
                bool target = key == 202 && fault.StartsWith("target");
                return CoreStageCombatCycleTests.CycleDatabase.Stage((source || target) && fault.EndsWith("key") ? 999 : key,
                    (source || target) && fault.EndsWith("map") ? 3 : key < 200 ? 1 : 2,
                    key == 102 || key == 202 ? (source || target) && fault.EndsWith("role") ? StageMapRole.Regular : StageMapRole.Boss : StageMapRole.Regular);
            };
            var before = new RecoveryBufferSnapshot(_b);
            Assert.That(Complete().Accepted, Is.False);
            before.AssertUnchanged(_b);
            Assert.That(_b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FinalContinuationFault_RetriesWithoutSettlementReplay(bool rootFailure)
        {
            Boss(); _routes.Definition = ChapterContinuationDefinition.Final(1);
            if (rootFailure) _db.OnQuery = step => { if (step == "root:102") throw new InvalidOperationException("Fixture root fault"); };
            else _routes.Failure = new InvalidOperationException("Fixture route fault");
            var before = new RecoveryBufferSnapshot(_b);
            Assert.Throws<InvalidOperationException>(() => Complete());
            before.AssertUnchanged(_b);
            _db.OnQuery = null; _routes.Failure = null;
            Assert.That(Complete().Kind, Is.EqualTo(ChapterContinuationKind.RunCompleted));
            before.AssertUnchanged(_b);
            Assert.That(_b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Victory));
        }

        [Test]
        public void NamedFactory_PreservesUniqueCreateAndInjectsOneCoordinator()
        {
            var factory = typeof(ProjectL.Systems.Factory.Runtime.InGame.InGameSystemFactory);
            Assert.That(factory.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic).Count(m => m.Name == "Create"), Is.EqualTo(1));
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            ProjectL.Systems.Factory.Runtime.InGame.InGameSystemFactory.CreateWithSources(_battle, _routes).ExportServices(builder);
            var reader = builder.BuildSystemReader();
            var runtime = new FrameworkRuntime(reader);
            try
            {
                var group = runtime.GetService(SystemGroupsService.InGame);
                var coreModule = (ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.InGameCoreSystemGroup)group.CoreGroup;
                coreModule.InitData(_db);
                coreModule.PostBindService(reader.ServiceStoreReader);
                var coordinator = group.CoreGroup.ChapterProgression;
                Assert.That(coordinator, Is.SameAs(runtime.GetService(SystemGroupsService.InGame).CoreGroup.ChapterProgression));
                Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() => runtime.GetService(InGameSystemGroupServiceKey.Core));
                Assert.That(coordinator.CompleteChapter(1, new StageNodeId(3)).FailureReason, Is.EqualTo(ChapterContinuationFailureReason.ChapterMismatch));
            }
            finally { runtime.Shutdown(); }
        }
    }
}
