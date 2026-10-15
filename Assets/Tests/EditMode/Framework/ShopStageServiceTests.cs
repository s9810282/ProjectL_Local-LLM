using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed partial class ShopStageServiceTests
    {
        private FrameworkRuntime _runtime;
        private IBufferQuery _buffers;
        private IInGameCoreSystemGroupService _core;
        private ShopStageTestDatabase _db;

        [SetUp]
        public void SetUp() => CreateRuntime();

        private void CreateRuntime(bool bindFirst = false)
        {
            _db = new ShopStageTestDatabase();
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var inGameBuilder = new SystemBuilderHelper();
            ProjectL.Systems.Factory.Runtime.InGame.Core.InGameCoreSystemFactory.Create().ExportServices(inGameBuilder);
            new ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.InGameViewSystemGroup(
                new SystemBuilderHelper().BuildSystemReader(), null).ExportServices(inGameBuilder);
            new ProjectL.Systems.RuntimeSystem.InGameSystem.InGameSystemGroup(inGameBuilder.BuildSystemReader()).ExportServices(builder);
            _runtime = new FrameworkRuntime(builder.BuildSystemReader());
            if (bindFirst) { _runtime.PostBindService(); _runtime.InitData(_db); }
            else { _runtime.InitData(_db); _runtime.PostBindService(); }
            _runtime.Initialize();
            _buffers = _runtime.GetService(BufferSystemKey.Buffer);
            _core = _runtime.GetService(SystemGroupsService.InGame).CoreGroup;
            Assert.That(_core.Characters.InitializeRoster(1).Succeeded, Is.True);
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            var sub = _buffers.CharacterReadSink.Owned.First(x => x.Role == CharacterOwnershipRole.Sub);
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(sub.Id, 0, 2) },
                new MainCharacterPersistentChange(main, CharacterBufferTestData.MainState(7))));
            _buffers.CurrencyWriteSink.SetAmount(50);
            Register();
            Assert.That(_core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            _db.UnitReads.Clear();
        }

        [TearDown]
        public void TearDown() => _runtime?.Shutdown();

        [TestCase("InitializeRoster")]
        [TestCase("AcquireSub")]
        [TestCase("Purchase")]
        [TestCase("LegacyPurchase")]
        public void FinalUnitLookupEndsRun_PreventsRosterOrPurchasePublication(string operation)
        {
            Assert.That(Enter().Accepted, Is.True);
            var snapshot = new RecoveryBufferSnapshot(_buffers);
            _db.OnUnitRead = _ => { if (_buffers.RunProgressReadSink.Completion == null)
                RunTerminationAdmissionTests.Seed(_buffers, RunOutcome.Defeat); };
            string reason = operation switch {
                "InitializeRoster" => _core.Characters.InitializeRoster(1).FailureReason.ToString(),
                "AcquireSub" => _core.Characters.AcquireSub(EntityKey.Unit_Akimbo).FailureReason.ToString(),
                "Purchase" => _core.Shop.Purchase(1, new StageNodeId(2), new ShopOfferId(1)).FailureReason.ToString(),
                _ => _core.Shop.Purchase(new ShopOfferId(1)).FailureReason.ToString()
            };
            Assert.That(reason, Is.EqualTo("RunEnded"));
            snapshot.AssertUnchanged(_buffers);
            Assert.That(_buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
        }

        [Test]
        public void EnterShop_PreparesSelectionAndThreeOffersTogether()
        {
            var before = new RecoveryBufferSnapshot(_buffers);
            var result = Enter();
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ChapterKey, Is.EqualTo(1));
            Assert.That(result.NodeId.Value, Is.EqualTo(2));
            Assert.That(result.StageKey.Value, Is.EqualTo(100));
            var current = _buffers.CurrentProgressReadSink;
            Assert.That(current.CurrentStage.NodeId, Is.EqualTo(2));
            Assert.That(current.CurrentStage.StageKey, Is.EqualTo(100));
            Assert.That(current.Shop.Offers.Select(x => x.OfferId.Value), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(current.Shop.Offers.Select(x => x.CharacterKey), Is.All.EqualTo(EntityKey.Unit_Akimbo));
            Assert.That(current.Shop.Offers.Select(x => x.Cost), Is.EqualTo(new[] { 5, 5, 5 }));
            Assert.That(current.Shop.PurchasedMask, Is.Zero);
            Assert.That(current.Recovery, Is.Null);
            before.AssertUnchanged(_buffers, selection: false);
        }

        [TestCase("unreachable")]
        [TestCase("cleared-target")]
        [TestCase("cleared-map")]
        [TestCase("wrong-chapter")]
        [TestCase("missing-target")]
        public void RejectedNavigation_DoesNotReadDefinitionsOrWrite(string fault)
        {
            int chapter = 1, node = 2;
            if (fault == "unreachable") node = 3;
            if (fault == "missing-target") node = 99;
            if (fault == "cleared-target") _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            if (fault == "cleared-map") _buffers.MapProgressWriteSink.SetClear(1, true);
            if (fault == "wrong-chapter") { chapter = 2; _buffers.MapProgressWriteSink.RegisterMap(2); }
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Enter(chapter, node).Accepted, Is.False);
            Assert.That(_db.StageReads, Is.Zero);
            Assert.That(_db.UnitReads, Is.Empty);
            before.AssertUnchanged(_buffers);
        }

        [TestCase("later-lookup")]
        [TestCase("invalid-unselected-candidate")]
        [TestCase("wrong-key")]
        [TestCase("wrong-map")]
        [TestCase("boss-root")]
        [TestCase("nonshop-root")]
        [TestCase("missing-root")]
        public void EntryFailure_PreservesPreviousReceiptsAndCanRetry(string fault)
        {
            SelectPreviousShop();
            var root = _db.Stages[100];
            if (fault == "later-lookup")
            {
                _db.Stages[100] = ShopStageTestDatabase.Shop(new ShopCandidate(EntityKey.Unit_Akimbo, 5),
                    new ShopCandidate(EntityKey.Unit_Sniper, 7));
                _db.OnUnitRead = key => { if (key == EntityKey.Unit_Sniper) throw new InvalidOperationException("조회 실패"); };
            }
            if (fault == "invalid-unselected-candidate") _db.Stages[100] = ShopStageTestDatabase.Shop(
                new ShopCandidate(EntityKey.Unit_Akimbo, 5), new ShopCandidate(EntityKey.Unit_Demon, 7));
            if (fault == "wrong-key" || fault == "wrong-map" || fault == "boss-root")
                _db.Stages[100] = new StageRootData(fault == "wrong-key" ? 101 : 100, fault == "wrong-map" ? 2 : 1,
                    fault == "boss-root" ? StageMapRole.Boss : StageMapRole.Regular, root.StageData);
            if (fault == "nonshop-root") _db.Stages[100] = new StageRootData(100, 1, StageMapRole.Regular, new EventStageDetail(new[] { 1 }));
            if (fault == "missing-root") _db.OnStageRead = _ => { if (_db.StageReads == 2) _db.Stages[100] = null; };
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => Enter(node: 3));
            before.AssertUnchanged(_buffers);
            _db.Stages[100] = root; _db.OnUnitRead = null; _db.OnStageRead = null;
            Assert.That(Enter(node: 3).Accepted, Is.True);
            Assert.That(_buffers.CurrentProgressReadSink.Shop.PurchasedMask, Is.Zero);
        }

        [TestCase("origin-name")]
        [TestCase("target-key")]
        [TestCase("navigation")]
        [TestCase("target-cleared")]
        [TestCase("map-cleared")]
        [TestCase("graph-type")]
        [TestCase("graph-boss")]
        public void Entry_StateChangesDuringFinalLookupAddsNoWrites(string fault)
        {
            SelectPreviousShop();
            RecoveryBufferSnapshot afterMutation = null;
            _db.OnUnitRead = _ => {
                if (fault == "origin-name") _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Changed");
                if (fault == "target-key") Register(targetKey: 101, firstClear: true);
                if (fault == "navigation") Register(disconnected: true, firstClear: true);
                if (fault == "target-cleared") _buffers.StageProgressWriteSink.SetClear(1, 3, true);
                if (fault == "map-cleared") _buffers.MapProgressWriteSink.SetClear(1, true);
                if (fault == "graph-type" || fault == "graph-boss") Register(firstClear: true,
                    targetType: fault == "graph-type" ? StageType.Event : StageType.Shop,
                    targetRole: fault == "graph-boss" ? StageMapRole.Boss : StageMapRole.Regular);
                afterMutation = new RecoveryBufferSnapshot(_buffers);
            };
            if (fault.StartsWith("graph-")) Assert.Throws<InvalidOperationException>(() => Enter(node: 3));
            else Assert.That(Enter(node: 3).Accepted, Is.False);
            Assert.That(afterMutation, Is.Not.Null);
            afterMutation.AssertUnchanged(_buffers);
        }

        [Test]
        public void EnterShop_SameSelectedNodeCannotRefreshOffers()
        {
            Assert.That(Enter().Accepted, Is.True);
            Assert.That(_core.Shop.Purchase(new ShopOfferId(1)).Succeeded, Is.True);
            int reads = _db.StageReads;
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Enter().Accepted, Is.False);
            Assert.That(_db.StageReads, Is.EqualTo(reads));
            Assert.That(_core.Shop.PrepareShop(999), Is.EqualTo(ShopFailureReason.None));
            before.AssertUnchanged(_buffers);
        }

        [Test]
        public void EnterShop_DifferentNodesWithSameStageKeyHaveIndependentOffers()
        {
            SelectPreviousShop();
            var previous = _buffers.CurrentProgressReadSink.Shop;
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Enter(node: 3).Accepted, Is.True);
            Assert.That(_buffers.CurrentProgressReadSink.Shop, Is.Not.SameAs(previous));
            Assert.That(_buffers.CurrentProgressReadSink.Shop.PurchasedMask, Is.Zero);
            before.AssertUnchanged(_buffers, selection: false);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnterShop_FromRecoveryPreservesReceiptOnFailureAndClearsItOnSuccess(bool fail)
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(20), "Recovery");
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), 0, new RecoveryHealSpec(RecoveryHealMode.Full, 0));
            _buffers.CurrentProgressWriteSink.SetRecoveryOptions(new[] { option });
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            Assert.That(_buffers.RecoveryStageWriteSink.PublishRecovery(new StageNodeId(2), new StageKey(20),
                CharacterRecoveryCalculator.Plan(main,
                    _buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id, 0, option, _buffers.CurrencyReadSink.Amount)), Is.True);
            _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            if (fail) _db.OnUnitRead = _ => throw new InvalidOperationException("조회 실패");
            var before = new RecoveryBufferSnapshot(_buffers);
            if (fail) { Assert.Throws<InvalidOperationException>(() => Enter(node: 3)); before.AssertUnchanged(_buffers); }
            else { Assert.That(Enter(node: 3).Accepted, Is.True); Assert.That(_buffers.CurrentProgressReadSink.Recovery, Is.Null); before.AssertUnchanged(_buffers, selection: false); }
        }

        [TestCase(0, 2)]
        [TestCase(1, 0)]
        public void Entry_InvalidArgumentsDoNotReadOrWrite(int chapter, int node)
        {
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Catch<ArgumentException>(() => Enter(chapter, node));
            Assert.That(_db.StageReads, Is.Zero);
            before.AssertUnchanged(_buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FrameworkLifecycle_EitherOrderExportsShopOnlyThroughGroup(bool bindFirst)
        {
            _runtime.Shutdown(); CreateRuntime(bindFirst);
            Assert.Throws<KeyNotFoundException>(() => _runtime.GetService(InGameSystemServiceKey.Shop));
            Assert.Throws<KeyNotFoundException>(() => _runtime.GetService(InGameSystemGroupServiceKey.Core));
            Assert.That(Enter().Accepted, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Entry_RetainedBattleBlocksWithoutCleanup(bool terminal)
        {
            InstallBattle(terminal);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => Enter(node: 3));
            Assert.That(_db.StageReads, Is.Zero);
            Assert.That(_db.UnitReads, Is.Empty);
            before.AssertUnchanged(_buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Entry_BattleInstalledDuringLookupCannotPublish(bool terminal)
        {
            SelectPreviousShop();
            RecoveryBufferSnapshot afterMutation = null;
            _db.OnUnitRead = _ => { InstallBattle(terminal); afterMutation = new RecoveryBufferSnapshot(_buffers); };
            Assert.Throws<InvalidOperationException>(() => Enter(node: 3));
            Assert.That(afterMutation, Is.Not.Null);
            afterMutation.AssertUnchanged(_buffers);
        }

        [TestCase("navigation")]
        [TestCase("target-key")]
        [TestCase("target-cleared")]
        [TestCase("map-cleared")]
        public void Entry_LastPreflightRootLookupAddsNoWritesAfterGraphMutation(string fault)
        {
            SelectPreviousShop();
            RecoveryBufferSnapshot afterMutation = null;
            _db.OnStageRead = _ => {
                if (_db.StageReads != 3) return;
                if (fault == "navigation") Register(disconnected: true, firstClear: true);
                if (fault == "target-key") Register(targetKey: 101, firstClear: true);
                if (fault == "target-cleared") _buffers.StageProgressWriteSink.SetClear(1, 3, true);
                if (fault == "map-cleared") _buffers.MapProgressWriteSink.SetClear(1, true);
                afterMutation = new RecoveryBufferSnapshot(_buffers);
            };
            Assert.That(Enter(node: 3).Accepted, Is.False);
            Assert.That(afterMutation, Is.Not.Null);
            afterMutation.AssertUnchanged(_buffers);
        }

        private void InstallBattle(bool terminal)
        {
            if (!_buffers.CurrentProgressReadSink.HasSelection)
                _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Previous");
            if (terminal) _buffers.StageProgressWriteSink.SetClear(1, 2, false);
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            _buffers.BattleWriteSink.PrepareBattle(BattlePreparationCalculator.Prepare(
                CharacterBattlePreparationTests.Initialization(),
                _buffers.CharacterReadSink.Owned.Where(x => x.Role == CharacterOwnershipRole.Main).ToArray(), main,
                _buffers.CharacterReadSink.Owned.ToDictionary(x => x.Id, x => 0),
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>()));
            if (terminal) _buffers.BattleWriteSink.CompleteBattle(new BattleCompletionCalculator(
                new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>())).Complete(
                _buffers.CharacterReadSink.Characters.Single().ToFacts(), main,
                new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(100), false)));
        }

        private void SelectPreviousShop()
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Previous shop");
            Assert.That(_core.Shop.PrepareShop(42), Is.EqualTo(ShopFailureReason.None));
            Assert.That(_core.Shop.Purchase(new ShopOfferId(1)).Succeeded, Is.True);
            _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            _db.StageReads = 0; _db.UnitReads.Clear();
        }

        private void Register(int targetKey = 100, bool disconnected = false, bool firstClear = false,
            StageType targetType = StageType.Shop, StageMapRole targetRole = StageMapRole.Regular)
        {
            _buffers.StageProgressWriteSink.Clear();
            _buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 100, 1, 0, disconnected ? Array.Empty<int>() : new[] { 3 }, firstClear, StageType.Shop),
                new StageProgressData(3, targetKey, 2, 0, Array.Empty<int>(), false, targetType, targetRole) });
        }

        private ChapterProgressionResult Enter(int chapter = 1, int node = 2)
            => _core.Shop.EnterShop(chapter, new StageNodeId(node), 42);
    }
}
