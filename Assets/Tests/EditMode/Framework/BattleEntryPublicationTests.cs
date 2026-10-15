using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
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
    public sealed class BattleEntryPublicationTests
    {
        private IBufferQuery _buffers;
        private OwnedCharacterRecord _main;

        [SetUp]
        public void SetUp()
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            _buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            _main = _buffers.CharacterWriteSink.Acquire(new[] {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, CharacterBufferTestData.MainState(7)).Acquired.Single();
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(_main.Id, 0, 2) }));
            _buffers.CurrentProgressWriteSink.SelectMap(1);
            RegisterStages();
            _buffers.CurrencyWriteSink.SetAmount(91);
            _buffers.ArtifactInitializationWriteSink.TryInitialize(new ArtifactState(4, 4,
                new[] { new ArtifactItemState(100, 0, false, default, default, new[] { CellCoord.Zero }) },
                Array.Empty<ArtifactItemState>()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EntryPublication_SelectsTargetAndInstallsBattle(bool previousSelection)
        {
            if (previousSelection) SelectPrevious(false);
            var artifact = _buffers.ArtifactReadSink.Current;
            _buffers.BattleWriteSink.PrepareBattle(Preparation(), Selection());

            Assert.That(_buffers.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(1));
            Assert.That(_buffers.CurrentProgressReadSink.HasSelection, Is.True);
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.NodeId, Is.EqualTo(3));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(102));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageName, Is.EqualTo("Target"));
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.True);
            Assert.That(_buffers.CharacterReadSink.Characters.Single().OwnedId, Is.EqualTo(_main.Id));
            Assert.That(_buffers.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(2));
            AssertPersistent(artifact);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void EntryPublication_PreservesOrClearsNoncombatDataBySelectionIdentity(bool recovery, bool sameTarget)
        {
            SelectPrevious(recovery);
            var current = _buffers.CurrentProgressReadSink;
            var shop = current.Shop;
            var options = current.Recovery;
            _buffers.BattleWriteSink.PrepareBattle(Preparation(), Selection(sameTarget ? 2 : 3,
                sameTarget ? 101 : 102, "Renamed"));

            Assert.That(current.Shop, sameTarget ? Is.SameAs(shop) : Is.Null);
            Assert.That(current.Recovery, sameTarget ? Is.SameAs(options) : Is.Null);
            Assert.That(current.CurrentStage.StageName, Is.EqualTo("Renamed"));
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.True);
        }

        [TestCase("Current", false)]
        [TestCase("Current", true)]
        [TestCase("Character", false)]
        [TestCase("Character", true)]
        [TestCase("Combat", false)]
        [TestCase("Combat", true)]
        public void EntryPublication_InvalidProposalPreservesAllStores(string boundary, bool recovery)
        {
            SeedHistory();
            SelectPrevious(recovery);
            var before = new Snapshot(_buffers);
            int answered = _buffers.CombatStateReadSink.AnsweredPreviewNumber;
            Assert.That(answered, Is.GreaterThan(0));
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out var retainedPreview), Is.True);
            Assert.That(retainedPreview.RequestNumber, Is.EqualTo(answered));
            var selection = boundary == "Current"
                ? new BattleEntrySelection(1, true, new StageSelectionData(2, 101, "Wrong name"),
                    new StageNodeId(3), new StageKey(102), "Target")
                : Selection();
            var preparation = Preparation(boundary == "Character", boundary == "Combat");

            var error = Assert.Throws<InvalidOperationException>(() =>
                _buffers.BattleWriteSink.PrepareBattle(preparation, selection));
            Assert.That(error.Message, Does.Contain(boundary == "Character" ? "로스터" :
                boundary == "Combat" ? "타임라인" : "선택"));
            before.AssertUnchanged(_buffers);

            _buffers.BattleWriteSink.PrepareBattle(Preparation(), Selection());
            Assert.That(_buffers.CombatStateReadSink.Current.NextEntityId, Is.EqualTo(2));
            Assert.That(_buffers.CombatStateReadSink.Current.DrawCursor, Is.Zero);
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.Null);
            Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(1));
            Assert.That(_buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(7));
            Assert.That(_buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(answered));
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
            Assert.That(_buffers.CombatViewReadSink.Preview.Number, Is.EqualTo(answered));
            var acquired = _buffers.CharacterWriteSink.Acquire(new[] {
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub) });
            Assert.That(acquired.Acquired.Single().Id.Value, Is.EqualTo(2));
        }

        [TestCase("Chapter")]
        [TestCase("Flag")]
        [TestCase("Node")]
        [TestCase("Stage")]
        [TestCase("Name")]
        [TestCase("Binding")]
        [TestCase("MissingNode")]
        public void EntryPublication_ChangedOriginOrNodeBindingRejects(string change)
        {
            SelectPrevious(false);
            var selection = Selection();
            switch (change)
            {
                case "Chapter": _buffers.CurrentProgressWriteSink.SelectMap(2); break;
                case "Flag": _buffers.CurrentProgressWriteSink.Clear(); _buffers.CurrentProgressWriteSink.SelectMap(1); break;
                case "Node": _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(4), new StageKey(101), "Previous"); break;
                case "Stage": _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(103), "Previous"); break;
                case "Name": _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "Changed"); break;
                case "Binding": RegisterStages(103); break;
                case "MissingNode": _buffers.StageProgressWriteSink.Clear(); break;
            }
            var before = new Snapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => _buffers.BattleWriteSink.PrepareBattle(Preparation(), selection));
            before.AssertUnchanged(_buffers);
        }

        [Test]
        public void EntryPublication_UnexpectedExistingSelectionRejectsFirstEntryProposal()
        {
            var selection = Selection();
            SelectPrevious(false);
            var before = new Snapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => _buffers.BattleWriteSink.PrepareBattle(Preparation(), selection));
            before.AssertUnchanged(_buffers);
        }

        [Test]
        public void EntryPublication_RetainedBattleDoesNotReplaceSelectionOrCharacters()
        {
            SelectPrevious(true);
            _buffers.BattleWriteSink.PrepareBattle(Preparation());
            SeedPreview();
            var before = new Snapshot(_buffers);
            Assert.Throws<InvalidOperationException>(() => _buffers.BattleWriteSink.PrepareBattle(Preparation(), Selection()));
            before.AssertUnchanged(_buffers);
        }

        [Test]
        public void EntryPublication_SuccessClearsRetainedPreviewButPreservesAnsweredNumberAndViewRequest()
        {
            SeedHistory();
            SelectPrevious(false);
            int answered = _buffers.CombatStateReadSink.AnsweredPreviewNumber;
            var request = _buffers.CombatViewReadSink.Preview;
            Assert.That(answered, Is.GreaterThan(0));
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.RequestNumber, Is.EqualTo(answered));

            _buffers.BattleWriteSink.PrepareBattle(Preparation(), Selection());

            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out _), Is.False);
            Assert.That(_buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(answered));
            Assert.That(_buffers.CombatViewReadSink.HasPreview, Is.True);
            Assert.That(_buffers.CombatViewReadSink.Preview, Is.EqualTo(request));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.StageKey, Is.EqualTo(102));
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.True);
        }

        [Test]
        public void EntryPublication_StorageBindingDoesNotDecideNavigationEligibility()
        {
            SelectPrevious(false);
            _buffers.StageProgressWriteSink.SetClear(1, 3, true);
            _buffers.BattleWriteSink.PrepareBattle(Preparation(), Selection());
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.NodeId, Is.EqualTo(3));
            Assert.That(_buffers.StageProgressReadSink.IsCleared(1, 3), Is.True);
        }

        [TestCase("Chapter")]
        [TestCase("Node")]
        [TestCase("Stage")]
        [TestCase("Name")]
        [TestCase("EmptyExpected")]
        [TestCase("UnexpectedExpected")]
        public void SelectionProposal_RejectsMalformedFacts(string field)
        {
            Assert.Catch<ArgumentException>(() => new BattleEntrySelection(field == "Chapter" ? 0 : 1,
                field == "EmptyExpected", field == "UnexpectedExpected" ? new StageSelectionData(2, 101, "Previous") : default,
                field == "Node" ? StageNodeId.Empty : new StageNodeId(3),
                field == "Stage" ? StageKey.Empty : new StageKey(102), field == "Name" ? " " : "Target"));
        }

        [Test]
        public void EntryPublication_NullInputsPreserveStores()
        {
            SelectPrevious(false);
            var before = new Snapshot(_buffers);
            Assert.Throws<ArgumentNullException>(() => _buffers.BattleWriteSink.PrepareBattle(null, Selection()));
            Assert.Throws<ArgumentNullException>(() => _buffers.BattleWriteSink.PrepareBattle(Preparation(), null));
            before.AssertUnchanged(_buffers);
        }

        private BattleEntrySelection Selection(int node = 3, int stage = 102, string name = "Target")
        {
            var current = _buffers.CurrentProgressReadSink;
            return new BattleEntrySelection(current.CurrentMapKey, current.HasSelection, current.CurrentStage,
                new StageNodeId(node), new StageKey(stage), name);
        }

        private BattlePreparation Preparation(bool invalidOwned = false, bool invalidTimeline = false)
        {
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            var facts = CharacterBufferTestData.MainFacts(invalidOwned ? new CharacterInstanceId(99) : _main.Id, main, 2);
            var combat = CharacterBufferTestData.Combat();
            var state = new CombatCommandState(8, 6, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(),
                invalidTimeline ? new[] { CombatTimelineEntrySnapshot.ForUnit(new EntityId(99), 1, 0, false) }
                    : Array.Empty<CombatTimelineEntrySnapshot>());
            return new BattlePreparation(new[] { facts }, combat, commandState: state);
        }

        private void RegisterStages(int targetStage = 102)
        {
            _buffers.StageProgressWriteSink.Clear();
            _buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                new StageProgressData(2, 101, 1, 0, new[] { 3 }),
                new StageProgressData(3, targetStage, 2, 0, Array.Empty<int>()) });
        }

        private void SelectPrevious(bool recovery)
        {
            _buffers.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(101), "Previous");
            if (recovery)
                _buffers.CurrentProgressWriteSink.SetRecoveryOptions(new[] {
                    new RecoveryOptionSpec(new RecoveryOptionId(1), 5, new RecoveryHealSpec(RecoveryHealMode.Flat, 3)) });
            else
                _buffers.CurrentProgressWriteSink.SetShopOffers(Enumerable.Range(1, 3).Select(i =>
                    new ShopPurchaseSnapshot(new ShopOfferId(i), EntityKey.Unit_Akimbo, 10)).ToArray());
        }

        private void SeedHistory()
        {
            _buffers.BattleWriteSink.PrepareBattle(Preparation());
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            var input = new CombatCommandInput(7, CombatCommandKind.Attack, new EntityId(1), default,
                _buffers.CombatStateReadSink.Current.CommandState,
                _buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray(), Array.Empty<CombatAttackProfile>(),
                main, _buffers.CharacterReadSink.Owned,
                _buffers.CharacterReadSink.Owned.ToDictionary(o => o.Id, o => _buffers.CharacterReadSink.GetSoulCount(o.Id)),
                _buffers.CombatStateReadSink.SoulField, default);
            _buffers.BattleWriteSink.Apply(new CombatCommandTransition(input, false, "InvalidAim",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), input.State,
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None)));
            var preview = SeedPreview();
            // 이전 완료 기록의 보존을 검증하는 저장소 입력이다. 승패 정산은 기존 C4에서 검증한다.
            ((ICombatStateBufferStore)_buffers.CombatStateReadSink).InstallCompletion(new BattleCompletionChanges(
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.VoluntaryAbandonment),
                ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(101))));
            _buffers.BattleWriteSink.EndBattle();
            // 종료는 응답을 지우므로 저장소 경계의 준비 검증을 위해 잔존 응답만 다시 입력한다.
            // 실제 종료 후 입력 수명은 등록된 Core 전투 순환 테스트에서 별도로 검증한다.
            ((ICombatStateBufferStore)_buffers.CombatStateReadSink).InstallPreview(preview);
        }

        private CombatPreviewResult SeedPreview()
        {
            var data = CombatViewPreviewData.AttackRange(new EntityId(1));
            _buffers.CombatViewWriteSink.SetPreview(data);
            int number = _buffers.CombatViewWriteSink.SetPreview(data);
            var preview = CombatPreviewResult.ForAttackRange(number,
                AttackRangeResult.Failure(AttackFailureReason.InvalidAttackAim, new EntityId(1)));
            _buffers.BattleWriteSink.PublishPreview(preview);
            Assert.That(_buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(number));
            Assert.That(number, Is.GreaterThan(1));
            return preview;
        }

        private void AssertPersistent(ArtifactBufferSnapshot artifact)
        {
            Assert.That(_buffers.CharacterReadSink.Owned.Single().Id, Is.EqualTo(_main.Id));
            Assert.That(_buffers.CharacterReadSink.GetSoulCount(_main.Id), Is.EqualTo(2));
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            Assert.That(main.CurrentHealth, Is.EqualTo(7));
            Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(91));
            Assert.That(_buffers.ArtifactReadSink.Current, Is.SameAs(artifact));
        }

        private sealed class Snapshot
        {
            private readonly StageSelectionData _stage;
            private readonly int _chapter, _sequence, _handled, _answered, _currency;
            private readonly bool _selected;
            private readonly CurrentShopData _shop;
            private readonly CurrentRecoveryData _recovery;
            private readonly object _battle, _completion, _result, _preview, _field, _main, _artifact;
            private readonly OwnedCharacterRecord[] _owned;
            private readonly object[] _characters;
            private readonly int[] _souls;
            private readonly bool _hasViewPreview;
            private readonly CombatViewPreviewRequest _viewPreview;
            private readonly CombatViewCommand[] _commands;
            private readonly int _presented;

            internal Snapshot(IBufferQuery buffers)
            {
                var current = buffers.CurrentProgressReadSink;
                _stage = current.CurrentStage; _chapter = current.CurrentMapKey; _selected = current.HasSelection;
                _shop = current.Shop; _recovery = current.Recovery;
                var combat = buffers.CombatStateReadSink;
                _battle = combat.Current; _completion = combat.Completion; _field = combat.SoulField;
                _sequence = combat.LatestResultSequence; _handled = combat.HandledCommandNumber; _answered = combat.AnsweredPreviewNumber;
                buffers.CombatResultReadSink.TryGetLatest(out var result); _result = result;
                combat.TryGetLatestPreview(out var preview); _preview = preview;
                buffers.CharacterReadSink.TryGetMainState(out var main); _main = main;
                _owned = buffers.CharacterReadSink.Owned.ToArray();
                _characters = buffers.CharacterReadSink.Characters.Cast<object>().ToArray();
                _souls = _owned.Select(o => buffers.CharacterReadSink.GetSoulCount(o.Id)).ToArray();
                _currency = buffers.CurrencyReadSink.Amount; _artifact = buffers.ArtifactReadSink.Current;
                _hasViewPreview = buffers.CombatViewReadSink.HasPreview;
                _viewPreview = buffers.CombatViewReadSink.Preview;
                _commands = buffers.CombatViewReadSink.Commands.ToArray();
                _presented = buffers.CombatViewReadSink.PresentedResultSequence;
            }

            internal void AssertUnchanged(IBufferQuery buffers)
            {
                var current = buffers.CurrentProgressReadSink;
                Assert.That(current.CurrentMapKey, Is.EqualTo(_chapter));
                Assert.That(current.HasSelection, Is.EqualTo(_selected));
                Assert.That(current.CurrentStage, Is.EqualTo(_stage));
                Assert.That(current.Shop, Is.SameAs(_shop)); Assert.That(current.Recovery, Is.SameAs(_recovery));
                var combat = buffers.CombatStateReadSink;
                Assert.That(combat.Current, Is.SameAs(_battle)); Assert.That(combat.Completion, Is.SameAs(_completion));
                Assert.That(combat.SoulField, Is.SameAs(_field));
                Assert.That(combat.LatestResultSequence, Is.EqualTo(_sequence));
                Assert.That(combat.HandledCommandNumber, Is.EqualTo(_handled));
                Assert.That(combat.AnsweredPreviewNumber, Is.EqualTo(_answered));
                buffers.CombatResultReadSink.TryGetLatest(out var result); Assert.That(result, Is.SameAs(_result));
                combat.TryGetLatestPreview(out var preview); Assert.That(preview, Is.SameAs(_preview));
                buffers.CharacterReadSink.TryGetMainState(out var main); Assert.That(main, Is.SameAs(_main));
                Assert.That(buffers.CharacterReadSink.Owned, Is.EqualTo(_owned));
                Assert.That(buffers.CharacterReadSink.Characters, Is.EqualTo(_characters));
                Assert.That(_owned.Select(o => buffers.CharacterReadSink.GetSoulCount(o.Id)), Is.EqualTo(_souls));
                Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(_currency));
                Assert.That(buffers.ArtifactReadSink.Current, Is.SameAs(_artifact));
                Assert.That(buffers.CombatViewReadSink.HasPreview, Is.EqualTo(_hasViewPreview));
                Assert.That(buffers.CombatViewReadSink.Preview, Is.EqualTo(_viewPreview));
                Assert.That(buffers.CombatViewReadSink.Commands, Is.EqualTo(_commands));
                Assert.That(buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(_presented));
            }
        }
    }
}
