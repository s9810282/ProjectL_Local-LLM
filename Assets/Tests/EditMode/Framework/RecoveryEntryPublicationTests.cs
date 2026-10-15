using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
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
    public sealed class RecoveryEntryPublicationTests
    {
        private IBufferQuery _buffers;

        [SetUp]
        public void SetUp()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            _buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            _buffers.MapProgressWriteSink.RegisterMap(1);
            _buffers.CurrentProgressWriteSink.SelectMap(1);
            Register();
            _buffers.CharacterWriteSink.Acquire(new[] {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, CharacterBufferTestData.MainState(7));
            _buffers.CurrencyWriteSink.SetAmount(8);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Publication_InstallsSelectionAndDetachedOptions(bool fromShop)
        {
            if (fromShop) SelectShop();
            var options = Options();
            var preparation = Preparation(options: options);
            options[0] = new RecoveryOptionSpec(new RecoveryOptionId(9), 99, new RecoveryHealSpec(RecoveryHealMode.Full, 0));
            var before = new RecoveryBufferSnapshot(_buffers);
            RecoveryEntryTestAccess.Publish(_buffers, preparation);
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.NodeId, Is.EqualTo(3));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(20));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageName, Is.EqualTo("Recovery"));
            Assert.That(_buffers.CurrentProgressReadSink.Shop, Is.Null);
            var recovery = _buffers.CurrentProgressReadSink.Recovery;
            Assert.That(recovery.Options.Single().Id.Value, Is.EqualTo(1));
            Assert.That(recovery.AppliedOptionId, Is.Null);
            before.AssertUnchanged(_buffers, selection: false);
        }

        [Test]
        public void Publication_NullPreparationRejectsWithoutWrites()
        {
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<ArgumentNullException>(() => _buffers.RecoveryStageWriteSink.PrepareRecovery(null));
            before.AssertUnchanged(_buffers);
        }

        [TestCase("chapter")]
        [TestCase("presence")]
        [TestCase("node")]
        [TestCase("key")]
        [TestCase("name")]
        [TestCase("binding")]
        [TestCase("missing-target")]
        [TestCase("cleared-target")]
        [TestCase("cleared-map")]
        public void Publication_RejectsStaleOrInvalidStorageWithoutWrites(string fault)
        {
            SelectShop();
            var preparation = Preparation();
            switch (fault)
            {
                case "chapter": _buffers.CurrentProgressWriteSink.SelectMap(2); break;
                case "presence": _buffers.CurrentProgressWriteSink.Clear(); _buffers.CurrentProgressWriteSink.SelectMap(1); break;
                case "node": _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(4), new StageKey(100), "Shop"); break;
                case "key": _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(21), "Shop"); break;
                case "name": _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Renamed"); break;
                case "binding": Register(21); break;
                case "missing-target": _buffers.StageProgressWriteSink.Clear(); break;
                case "cleared-target": _buffers.StageProgressWriteSink.SetClear(1, 3, true); break;
                case "cleared-map": _buffers.MapProgressWriteSink.SetClear(1, true); break;
            }
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => RecoveryEntryTestAccess.Publish(_buffers, preparation));
            before.AssertUnchanged(_buffers);
        }

        [Test]
        public void Publication_SameSelectedTargetDoesNotResetConsumedReceipt()
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(3), new StageKey(20), "Recovery");
            _buffers.CurrentProgressWriteSink.SetRecoveryOptions(Options());
            Consume();
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => RecoveryEntryTestAccess.Publish(_buffers, Preparation()));
            before.AssertUnchanged(_buffers);
        }

        [Test]
        public void Publication_DifferentNodeWithSameKeyGetsFreshOptions()
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(20), "Previous recovery");
            _buffers.CurrentProgressWriteSink.SetRecoveryOptions(Options());
            Consume();
            var previous = _buffers.CurrentProgressReadSink.Recovery;
            var before = new RecoveryBufferSnapshot(_buffers);
            RecoveryEntryTestAccess.Publish(_buffers, Preparation());
            Assert.That(_buffers.CurrentProgressReadSink.Recovery, Is.Not.SameAs(previous));
            Assert.That(_buffers.CurrentProgressReadSink.Recovery.AppliedOptionId, Is.Null);
            before.AssertUnchanged(_buffers, selection: false);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Publication_RetainedActiveOrTerminalBattleRejectsWithoutCleanup(bool terminal)
        {
            _buffers.StageProgressWriteSink.Clear();
            _buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 20, 1, 0, new[] { 3 }),
                new StageProgressData(3, 20, 2, 0, Array.Empty<int>(), false, StageType.Event) });
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(20), "Previous");
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            var preparation = BattlePreparationCalculator.Prepare(CharacterBattlePreparationTests.Initialization(),
                _buffers.CharacterReadSink.Owned, main,
                _buffers.CharacterReadSink.Owned.ToDictionary(x => x.Id, x => 0),
                new InitialDrawPlan(Array.Empty<CharacterInstanceId>()), Array.Empty<CharacterCombatFacts>());
            _buffers.BattleWriteSink.PrepareBattle(preparation);
            if (terminal)
            {
                var facts = _buffers.CharacterReadSink.Characters.Single().ToFacts();
                _buffers.BattleWriteSink.CompleteBattle(new BattleCompletionCalculator(
                    new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>())).Complete(facts, main,
                    new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                    ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(20), false)));
            }
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => RecoveryEntryTestAccess.Publish(_buffers, Preparation()));
            before.AssertUnchanged(_buffers);
        }

        [TestCase("null-options")]
        [TestCase("empty-options")]
        [TestCase("null-option")]
        [TestCase("duplicate-options")]
        [TestCase("chapter")]
        [TestCase("node")]
        [TestCase("key")]
        [TestCase("name")]
        [TestCase("invalid-origin")]
        [TestCase("unexpected-origin")]
        public void Preparation_RejectsMalformedValuesBeforePublication(string fault)
        {
            var before = new RecoveryBufferSnapshot(_buffers);
            var options = Options();
            int chapter = 1, node = 3, key = 20;
            string name = "Recovery";
            bool selected = false;
            StageSelectionData origin = default;
            switch (fault)
            {
                case "null-options": options = null; break;
                case "empty-options": options = Array.Empty<RecoveryOptionSpec>(); break;
                case "null-option": options = new RecoveryOptionSpec[] { null }; break;
                case "duplicate-options": options = new[] { options[0], options[0] }; break;
                case "chapter": chapter = 0; break;
                case "node": node = 0; break;
                case "key": key = 0; break;
                case "name": name = " "; break;
                case "invalid-origin": selected = true; break;
                case "unexpected-origin": origin = new StageSelectionData(2, 100, "Shop"); break;
            }
            Assert.Catch<ArgumentException>(() => RecoveryEntryTestAccess.Preparation(chapter, selected, origin,
                new StageNodeId(node), new StageKey(key), name, options));
            before.AssertUnchanged(_buffers);
        }

        private RecoveryEntryPreparation Preparation(RecoveryOptionSpec[] options = null) => RecoveryEntryTestAccess.Preparation(
            _buffers.CurrentProgressReadSink.CurrentMapKey, _buffers.CurrentProgressReadSink.HasSelection,
            _buffers.CurrentProgressReadSink.CurrentStage, new StageNodeId(3), new StageKey(20), "Recovery", options ?? Options());
        private static RecoveryOptionSpec[] Options() => new[] {
            new RecoveryOptionSpec(new RecoveryOptionId(1), 3, new RecoveryHealSpec(RecoveryHealMode.Full, 0)) };
        private void Consume()
        {
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            Assert.That(_buffers.RecoveryStageWriteSink.PublishRecovery(
                new StageNodeId(_buffers.CurrentProgressReadSink.CurrentStage.NodeId), new StageKey(20),
                CharacterRecoveryCalculator.Plan(main,
                    _buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main).Id, 0, Options()[0], _buffers.CurrencyReadSink.Amount)), Is.True);
        }
        [TestCase(false)]
        [TestCase(true)]
        public void Publication_InvalidGraphMetadataRejectsWithoutWrites(bool boss)
        {
            var nodes = _buffers.StageProgressReadSink.GetStages(1).Select(n => n.NodeId == 3
                ? new StageProgressData(n.NodeId, n.StageKey, n.Depth, n.WidthIndex, n.NextNodeIds, n.IsClear,
                    boss ? StageType.Event : StageType.Combat, boss ? StageMapRole.Boss : StageMapRole.Regular) : n).ToArray();
            _buffers.StageProgressWriteSink.Clear();
            _buffers.StageProgressWriteSink.RegisterStages(1, nodes);
            var before = new RecoveryBufferSnapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => _buffers.RecoveryStageWriteSink.PrepareRecovery(Preparation()));
            before.AssertUnchanged(_buffers);
            Register();
            Assert.DoesNotThrow(() => _buffers.RecoveryStageWriteSink.PrepareRecovery(Preparation()));
        }

        private void Register(int key = 20)
        {
            _buffers.StageProgressWriteSink.Clear();
            _buffers.StageProgressWriteSink.RegisterStages(1, new[] {
            new StageProgressData(1, 0, 0, 0, new[] { 2 }),
            new StageProgressData(2, 100, 1, 0, new[] { 3 }, false, StageType.Shop),
            new StageProgressData(3, key, 2, 0, Array.Empty<int>(), false, StageType.Event) });
        }
        private void SelectShop()
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(100), "Shop");
            _buffers.CurrentProgressWriteSink.SetShopOffers(Enumerable.Range(1, 3).Select(i =>
                new ShopPurchaseSnapshot(new ShopOfferId(i), EntityKey.Unit_Akimbo, 5)).ToArray());
        }
    }

    internal sealed class RecoveryBufferSnapshot
    {
        private readonly Action<IBufferQuery, bool, bool> _assert;
        internal RecoveryBufferSnapshot(IBufferQuery buffers)
        {
            var current = buffers.CurrentProgressReadSink;
            int chapter = current.CurrentMapKey;
            bool selected = current.HasSelection;
            var stage = current.CurrentStage;
            var shop = current.Shop;
            var recovery = current.Recovery;
            buffers.CharacterReadSink.TryGetMainState(out var main);
            var owned = buffers.CharacterReadSink.Owned.ToArray();
            var souls = owned.Select(x => buffers.CharacterReadSink.GetSoulCount(x.Id)).ToArray();
            var characters = buffers.CharacterReadSink.Characters.ToArray();
            int currency = buffers.CurrencyReadSink.Amount;
            var maps = buffers.MapProgressReadSink.Maps.ToArray();
            var nodes = buffers.StageProgressReadSink.GetStages(1).ToArray();
            var combat = buffers.CombatStateReadSink;
            bool battle = combat.HasBattle;
            object state = battle ? combat.Current : null;
            var completion = combat.Completion;
            var field = combat.SoulField;
            combat.TryGetLatestPreview(out var preview);
            buffers.CombatResultReadSink.TryGetLatest(out var result);
            int sequence = combat.LatestResultSequence, handled = combat.HandledCommandNumber, answered = combat.AnsweredPreviewNumber;
            var commands = buffers.CombatViewReadSink.Commands.ToArray();
            var viewPreview = buffers.CombatViewReadSink.Preview;
            int presented = buffers.CombatViewReadSink.PresentedResultSequence;
            var artifact = buffers.ArtifactReadSink.Current;
            _assert = (after, selection, progress) => {
                if (selection)
                {
                    Assert.That(after.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(chapter));
                    Assert.That(after.CurrentProgressReadSink.HasSelection, Is.EqualTo(selected));
                    Assert.That(after.CurrentProgressReadSink.CurrentStage, Is.EqualTo(stage));
                    Assert.That(after.CurrentProgressReadSink.Shop, Is.SameAs(shop));
                    Assert.That(after.CurrentProgressReadSink.Recovery, Is.SameAs(recovery));
                }
                after.CharacterReadSink.TryGetMainState(out var finalMain);
                Assert.That(finalMain, Is.SameAs(main));
                Assert.That(after.CharacterReadSink.Owned, Is.EqualTo(owned));
                Assert.That(owned.Select(x => after.CharacterReadSink.GetSoulCount(x.Id)), Is.EqualTo(souls));
                Assert.That(after.CharacterReadSink.Characters, Is.EqualTo(characters));
                Assert.That(after.CurrencyReadSink.Amount, Is.EqualTo(currency));
                Assert.That(after.MapProgressReadSink.Maps, Is.EqualTo(maps));
                if (progress) Assert.That(after.StageProgressReadSink.GetStages(1), Is.EqualTo(nodes));
                var final = after.CombatStateReadSink;
                Assert.That(final.HasBattle, Is.EqualTo(battle));
                if (battle) Assert.That(final.Current, Is.SameAs(state));
                Assert.That(final.Completion, Is.SameAs(completion));
                Assert.That(final.SoulField, Is.SameAs(field));
                final.TryGetLatestPreview(out var finalPreview);
                after.CombatResultReadSink.TryGetLatest(out var finalResult);
                Assert.That(finalPreview, Is.SameAs(preview));
                Assert.That(finalResult, Is.SameAs(result));
                Assert.That(final.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(final.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(final.AnsweredPreviewNumber, Is.EqualTo(answered));
                Assert.That(after.CombatViewReadSink.Commands, Is.EqualTo(commands));
                Assert.That(after.CombatViewReadSink.Preview, Is.EqualTo(viewPreview));
                Assert.That(after.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(presented));
                Assert.That(after.ArtifactReadSink.Current, Is.SameAs(artifact));
            };
        }
        internal void AssertUnchanged(IBufferQuery buffers, bool selection = true, bool progress = true)
            => _assert(buffers, selection, progress);
    }

    internal static class RecoveryEntryTestAccess
    {
        internal static RecoveryEntryPreparation Preparation(int chapter, bool selected, StageSelectionData origin,
            StageNodeId node, StageKey key, string name, RecoveryOptionSpec[] options)
            => new RecoveryEntryPreparation(chapter, selected, origin, node, key, name, options, StageType.Event, StageMapRole.Regular);
        internal static void Publish(IBufferQuery buffers, RecoveryEntryPreparation preparation)
            => buffers.RecoveryStageWriteSink.PrepareRecovery(preparation);
    }
}
