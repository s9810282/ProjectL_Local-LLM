using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed partial class RecoveryStageServiceTests
    {
        private FrameworkRuntime _runtime;
        private IBufferQuery _buffers;
        private IInGameCoreSystemGroupService _core;
        private RecoveryStageTestDatabase _db;

        [SetUp]
        public void SetUp()
            => CreateRuntime();

        private void CreateRuntime(bool bindFirst = false)
        {
            _db = new RecoveryStageTestDatabase();
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create().ExportServices(builder);
            _runtime = new FrameworkRuntime(builder.BuildSystemReader());
            if (bindFirst) { _runtime.PostBindService(); _runtime.InitData(_db); }
            else { _runtime.InitData(_db); _runtime.PostBindService(); }
            _runtime.Initialize();
            _buffers = _runtime.GetService(BufferSystemKey.Buffer);
            _core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
            Assert.That(_core.Characters.InitializeRoster(1).Succeeded, Is.True);
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                Array.Empty<SoulCountChange>(), new MainCharacterPersistentChange(main, CharacterBufferTestData.MainState(7))));
            _buffers.CurrencyWriteSink.SetAmount(8);
            Register();
            Assert.That(_core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
        }
        [TearDown]
        public void TearDown() => _runtime?.Shutdown();

        [TestCase("Enter")]
        [TestCase("Recover")]
        [TestCase("Complete")]
        public void FinalRecoveryLookupEndsRun_PreventsPublication(string operation)
        {
            Assert.That(Enter().Accepted, Is.True);
            if (operation == "Enter") _buffers.CurrentProgressWriteSink.Clear();
            if (operation == "Enter") _buffers.CurrentProgressWriteSink.SelectMap(1);
            var snapshot = new RecoveryBufferSnapshot(_buffers);
            if (operation == "Enter")
                _db.OnOptionRead = _ => { if (_buffers.RunProgressReadSink.Completion == null)
                    SeedFromRecoveryNode(); };
            else _db.OnStageRead = (_, _) => { if (_buffers.RunProgressReadSink.Completion == null)
                RunTerminationAdmissionTests.Seed(_buffers, RunOutcome.Defeat); };
            if (operation == "Recover")
                Assert.That(Assert.Throws<InvalidOperationException>(() => _core.Recovery.Recover(1,
                    new StageNodeId(2), new RecoveryOptionId(1))).Message, Does.Contain("RunEnded"));
            else Assert.That((operation == "Enter" ? Enter() : _core.Recovery.CompleteRecovery(1,
                new StageNodeId(2))).FailureReason.ToString(), Is.EqualTo("RunEnded"));
            snapshot.AssertUnchanged(_buffers);
        }

        private void SeedFromRecoveryNode()
        {
            var store = (ProjectL.Systems.RuntimeSystem.BufferSystems.Run.IRunProgressBufferStore)_buffers.RunProgressReadSink;
            store.InstallCompletion(store.PrepareCompletion(new RunCompletionReceipt(
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.RunOutcome.Defeat,
                1, new StageNodeId(2), new StageKey(20))));
        }

        [Test]
        public void EnterRecovery_UnconfirmedTargetPublishesOptions()
        {
            var before = new RecoveryBufferSnapshot(_buffers);
            var result = Enter();
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.ChapterKey, Is.EqualTo(1));
            Assert.That(result.NodeId.Value, Is.EqualTo(2));
            Assert.That(result.StageKey.Value, Is.EqualTo(20));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.NodeId, Is.EqualTo(2));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(20));
            var options = _buffers.CurrentProgressReadSink.Recovery.Options;
            Assert.That(options.Select(x => x.Id.Value), Is.EqualTo(new[] { 2, 1 }));
            Assert.That(options.Select(x => x.Cost), Is.EqualTo(new[] { 3, 0 }));
            before.AssertUnchanged(_buffers, selection: false);
        }

        [TestCase("unreachable")]
        [TestCase("cleared-target")]
        [TestCase("cleared-map")]
        [TestCase("wrong-chapter")]
        public void RejectedNavigation_DoesNotReadOptions(string fault)
        {
            int node = 2, chapter = 1;
            if (fault == "unreachable") node = 3;
            if (fault == "cleared-target") _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            if (fault == "cleared-map") _buffers.MapProgressWriteSink.SetClear(1, true);
            if (fault == "wrong-chapter") { chapter = 2; _buffers.MapProgressWriteSink.RegisterMap(2); }
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Enter(chapter, node).Accepted, Is.False);
            Assert.That(_db.OptionReads, Is.Empty);
            before.AssertUnchanged(_buffers);
        }

        [TestCase("lookup")]
        [TestCase("invalid-option")]
        [TestCase("wrong-key")]
        [TestCase("wrong-chapter")]
        [TestCase("boss")]
        [TestCase("shop")]
        public void EntryFault_PreservesAllFactsAndRetries(string fault)
        {
            Action correct = () => { };
            if (fault == "lookup")
            {
                _db.OnOptionRead = key => { if (key == 1) throw new InvalidOperationException("선택지 조회 실패"); };
                correct = () => _db.OnOptionRead = null;
            }
            else if (fault == "invalid-option")
            {
                var previous = _db.Options[1]; _db.Options[1] = null; correct = () => _db.Options[1] = previous;
            }
            else
            {
                var previous = _db.Stages[20];
                _db.Stages[20] = fault == "shop" ? _db.Stages[100] : new StageRootData(
                    fault == "wrong-key" ? 21 : 20, fault == "wrong-chapter" ? 2 : 1,
                    fault == "boss" ? StageMapRole.Boss : StageMapRole.Regular, new EventStageDetail(new[] { 1 }));
                correct = () => _db.Stages[20] = previous;
            }
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => Enter());
            before.AssertUnchanged(_buffers);
            correct();
            Assert.That(Enter().Accepted, Is.True);
            Assert.That(_buffers.CurrentProgressReadSink.Recovery.Options.Count, Is.EqualTo(2));
        }

        [TestCase("origin-name")]
        [TestCase("target-key")]
        [TestCase("navigation")]
        public void ChangedOriginOrTarget_DoesNotPublish(string change)
        {
            if (change == "origin-name")
            {
                _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(20), "Origin");
                _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            }
            RecoveryBufferSnapshot afterMutation = null;
            _db.OnOptionRead = key => {
                if (key != 1) return;
                if (change == "origin-name")
                    _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(20), "Changed");
                else Register(change == "target-key" ? 21 : 20, change == "navigation");
                afterMutation = new RecoveryBufferSnapshot(_buffers);
            };
            var result = Enter(1, change == "origin-name" ? 3 : 2);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(change == "navigation"
                ? ChapterProgressionFailureReason.NavigationRejected : ChapterProgressionFailureReason.StageMismatch));
            Assert.That(afterMutation, Is.Not.Null);
            afterMutation.AssertUnchanged(_buffers);
        }

        [Test]
        public void RepeatEntry_DoesNotResetReceiptAndDifferentNodeSameKeyGetsFreshOptions()
        {
            Assert.That(Enter().Accepted, Is.True);
            Assert.That(_core.Combat.Recover(new RecoveryOptionId(2)), Is.True);
            var before = new RecoveryBufferSnapshot(_buffers);
            int reads = _db.OptionReads.Count;
            Assert.That(Enter().Accepted, Is.False);
            Assert.That(_db.OptionReads.Count, Is.EqualTo(reads));
            before.AssertUnchanged(_buffers);
            _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            var afterClear = new RecoveryBufferSnapshot(_buffers);
            Assert.That(Enter(1, 3).Accepted, Is.True);
            Assert.That(_buffers.CurrentProgressReadSink.Recovery.AppliedOptionId, Is.Null);
            afterClear.AssertUnchanged(_buffers, selection: false);
        }

        [Test]
        public void FailedEntryFromShop_PreservesOffers()
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Shop");
            _buffers.StageProgressWriteSink.SetClear(1, 2, true);
            Assert.That(_core.Shop.PrepareShop(42), Is.EqualTo(ShopFailureReason.None));
            _db.OnOptionRead = _ => throw new InvalidOperationException("회복 조회 실패");
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => Enter(1, 3));
            before.AssertUnchanged(_buffers);
            _db.OnOptionRead = null;
            Assert.That(Enter(1, 3).Accepted, Is.True);
            Assert.That(_buffers.CurrentProgressReadSink.Shop, Is.Null);
        }

        [TestCase(0, 2)]
        [TestCase(1, 0)]
        public void InvalidArguments_DoNotReadOrWrite(int chapter, int node)
        {
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Catch<ArgumentException>(() => Enter(chapter, node));
            Assert.That(_db.StageReads, Is.Zero);
            Assert.That(_db.OptionReads, Is.Empty);
            before.AssertUnchanged(_buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FrameworkLifecycle_EitherStageOrderSupportsEntry(bool bindFirst)
        {
            _runtime.Shutdown();
            CreateRuntime(bindFirst);
            Assert.That(_core.Recovery, Is.Not.Null);
            Assert.That(Enter().Accepted, Is.True);
        }

        [TestCase("none")]
        [TestCase("init-only")]
        [TestCase("bind-only")]
        public void Entry_RequiresDatabaseAndBindingBeforeLookup(string lifecycle)
        {
            var service = new RecoveryStageService(_core.Combat, (ChapterProgressionCoordinator)_core.ChapterProgression);
            if (lifecycle == "init-only") service.InitData(_db);
            if (lifecycle == "bind-only")
            {
                var builder = new SystemBuilderHelper();
                builder.AddService<IBufferQuery>(_buffers);
                service.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            }
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => service.EnterRecovery(1, new StageNodeId(2)));
            Assert.That(_db.StageReads, Is.Zero);
            Assert.That(_db.OptionReads, Is.Empty);
            before.AssertUnchanged(_buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Entry_RetainedBattleBlocksBeforeLookupAndLegitimateCleanupAllowsEntry(bool terminal)
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(20), "Previous");
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            _buffers.BattleWriteSink.PrepareBattle(BattlePreparationCalculator.Prepare(
                CharacterBattlePreparationTests.Initialization(),
                _buffers.CharacterReadSink.Owned.Where(x => x.Role == CharacterOwnershipRole.Main).ToArray(), main,
                _buffers.CharacterReadSink.Owned.ToDictionary(x => x.Id, x => 0),
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>()));
            var loadout = new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>());
            if (terminal)
                _buffers.BattleWriteSink.CompleteBattle(new BattleCompletionCalculator(loadout).Complete(
                    _buffers.CharacterReadSink.Characters.Single().ToFacts(), main,
                    new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                    ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(20), false)));
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => Enter(1, 3));
            Assert.That(_db.StageReads, Is.Zero);
            Assert.That(_db.OptionReads, Is.Empty);
            before.AssertUnchanged(_buffers);

            // 활성 전투는 호출자가 기존 포기 계약으로 완료한 뒤에만 정리한다.
            if (!terminal) Assert.That(_core.Combat.AbandonBattle(), Is.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned));
            _core.Combat.EndBattle();
            var cleaned = new RecoveryBufferSnapshot(_buffers);
            if (terminal)
            {
                Assert.That(Enter(1, 3).Accepted, Is.True);
                cleaned.AssertUnchanged(_buffers, selection: false);
            }
            else
            {
                Assert.That(Enter(1, 3).FailureReason, Is.EqualTo(ChapterProgressionFailureReason.RunEnded));
                cleaned.AssertUnchanged(_buffers);
            }
        }

        private void Register(int firstKey = 20, bool disconnected = false)
        {
            _buffers.StageProgressWriteSink.Clear();
            _buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, disconnected ? new[] { 4 } : new[] { 2, 4 }),
                new StageProgressData(2, firstKey, 1, 0, new[] { 3 }, false, StageType.Event),
                new StageProgressData(3, 20, 2, 0, Array.Empty<int>(), false, StageType.Event),
                new StageProgressData(4, 21, 1, 1, Array.Empty<int>(), false, StageType.Event) });
        }

        private ChapterProgressionResult Enter(int chapter = 1, int node = 2)
            => _core.Recovery.EnterRecovery(chapter, new StageNodeId(node));
    }
}
