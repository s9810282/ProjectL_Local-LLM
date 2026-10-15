using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.Factory.Runtime.Development;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Tests.Framework
{
    public sealed class TemporaryCoreChapterContinuationTests
    {
        private FrameworkRuntime _runtime;
        private IBufferQuery _buffers;
        private IInGameCoreSystemGroupService _core;

        [SetUp]
        public void SetUp()
        {
            _runtime = TemporaryCoreRuntimeFactory.CreateWithChapterContinuation();
            _buffers = _runtime.GetService(BufferSystemKey.Buffer);
            _buffers.CurrencyWriteSink.SetAmount(TemporaryCoreRuntimeTests.StartingRunCurrency);
            _core = _runtime.GetService(SystemGroupsService.InGame).CoreGroup;
        }

        [TearDown]
        public void TearDown() => _runtime?.Shutdown();

        [Test]
        public void TwoChapters_KeepOwnedFacts_ThenFinishRun()
        {
            Assert.That(_buffers.MapProgressReadSink.GetMap(2).IsClear, Is.False);
            Assert.That(_buffers.StageProgressReadSink.GetStages(2), Is.Empty);
            Assert.That(_buffers.ArtifactReadSink.Current.PoolItems, Is.Empty);
            Assert.That(_buffers.ArtifactReadSink.Current.PlacedItems, Is.Empty);
            var original = _buffers.CharacterReadSink.Owned.ToArray();
            var mainOwned = original.Single(x => x.Role == CharacterOwnershipRole.Main).Id;
            var subOwned = original.First(x => x.Role == CharacterOwnershipRole.Sub).Id;
            // 테스트 준비 포트로 영혼과 미배치 아이템을 넣는다. 승리와 진행 사실은 실제 명령으로만 만든다.
            _buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(mainOwned, 0, 2), new SoulCountChange(subOwned, 0, 1) }));
            _buffers.ArtifactEditWriteSink.PublishEdit(new ArtifactEditProposal(ArtifactActionResult.Success(100, 0),
                new ArtifactState(4, 4, new[] {
                    new ArtifactItemState(100, 0, false, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }) }, null)));
            var artifact = _buffers.ArtifactReadSink.Current;

            Enter(1, 2);
            _core.Combat.ApplyHealth(MainId(), 4);
            WinBattle();
            _core.Combat.EndBattle();
            Assert.That(_core.Shop.EnterShop(1, new StageNodeId(5), 42).Accepted, Is.True);
            var offer = _buffers.CurrentProgressReadSink.Shop.Offers[0];
            var purchase = _core.Shop.Purchase(1, new StageNodeId(5), offer.OfferId);
            Assert.That(purchase.Succeeded, Is.True);
            var acquired = purchase.Acquisition.Acquired.Single().Id;
            Assert.That(_core.Shop.CompleteShop(1, new StageNodeId(5)).Accepted, Is.True);
            Enter(1, 4);
            WinBattle();
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.True);
            var beforeBlocked = new RecoveryBufferSnapshot(_buffers);
            Assert.That(_core.ChapterProgression.CompleteChapter(1, new StageNodeId(4)).FailureReason,
                Is.EqualTo(ChapterContinuationFailureReason.BattleRetained));
            beforeBlocked.AssertUnchanged(_buffers);
            _core.Combat.EndBattle();
            QueueOldInput();
            var cleanup = new RecoveryBufferSnapshot(_buffers);
            var completion = _buffers.CombatStateReadSink.Completion;
            var next = _core.ChapterProgression.CompleteChapter(1, new StageNodeId(4));
            Assert.That(next.Accepted, Is.True);
            Assert.That(next.Kind, Is.EqualTo(ChapterContinuationKind.NextChapterSelected));
            Assert.That(next.NextChapterKey, Is.EqualTo(2));
            cleanup.AssertUnchanged(_buffers, selection: false, progress: false);
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(completion));
            Assert.That(_buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(_buffers.RunProgressReadSink.Completion, Is.Null);
            Assert.That(_buffers.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(2));
            Assert.That(_buffers.CurrentProgressReadSink.HasSelection, Is.False);
            var graph = _buffers.StageProgressReadSink.GetStages(2);
            Assert.That(graph.Select(x => x.NodeId), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(graph.Select(x => x.StageKey), Is.EqualTo(new[] { 0, 201, 202 }));
            Assert.That(graph.Select(x => x.Depth), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(graph.Select(x => x.NextNodeIds.ToArray()),
                Is.EqualTo(new[] { new[] { 2 }, new[] { 3 }, Array.Empty<int>() }));
            AssertPersistent();

            _buffers.CombatResultReadSink.TryGetLatest(out var priorResult);
            var priorCommands = _buffers.CombatViewReadSink.Commands.ToArray();
            var priorViewPreview = _buffers.CombatViewReadSink.Preview;
            int priorSequence = _buffers.CombatStateReadSink.LatestResultSequence;
            int priorHandled = _buffers.CombatStateReadSink.HandledCommandNumber;
            int priorAnswered = _buffers.CombatStateReadSink.AnsweredPreviewNumber;
            Enter(2, 2);
            _buffers.CombatResultReadSink.TryGetLatest(out var retainedResult);
            Assert.That(retainedResult, Is.SameAs(priorResult));
            Assert.That(_buffers.CombatViewReadSink.Commands, Is.EqualTo(priorCommands));
            Assert.That(_buffers.CombatViewReadSink.Preview, Is.EqualTo(priorViewPreview));
            Assert.That(_buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(priorSequence));
            Assert.That(_buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(priorSequence));
            Assert.That(_buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(priorHandled));
            Assert.That(_buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.EqualTo(priorAnswered));
            AssertPersistent();
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.Null);
            var draw = _buffers.CombatStateReadSink.Current.DrawOrder;
            Assert.That(draw, Is.EquivalentTo(_buffers.CharacterReadSink.Owned
                .Where(x => x.Role == CharacterOwnershipRole.Sub).Select(x => x.Id)));
            Assert.That(draw.Count(x => x == acquired), Is.EqualTo(1));
            var battleMain = _buffers.CharacterReadSink.Characters.Single(x => x.EntityId == MainId()).ToFacts();
            Assert.That(battleMain.Health.CurrentHealth, Is.EqualTo(4));
            Assert.That(battleMain.Health.MaxHealth, Is.EqualTo(12));
            // 보존 확인을 마친 뒤 View 소유자가 이전 입력을 비운다.
            _buffers.CombatViewWriteSink.Clear();
            WinBattle();
            _core.Combat.EndBattle();
            Enter(2, 3);
            WinBattle();
            _core.Combat.EndBattle();
            QueueOldInput();
            var finalSnapshot = new RecoveryBufferSnapshot(_buffers);
            var finalCompletion = _buffers.CombatStateReadSink.Completion;
            var final = _core.ChapterProgression.CompleteChapter(2, new StageNodeId(3));
            Assert.That(final.Accepted, Is.True);
            Assert.That(final.Kind, Is.EqualTo(ChapterContinuationKind.RunCompleted));
            finalSnapshot.AssertUnchanged(_buffers);
            Assert.That(_buffers.CombatStateReadSink.Completion, Is.SameAs(finalCompletion));
            var receipt = _buffers.RunProgressReadSink.Completion;
            Assert.That(receipt.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(receipt.ChapterKey, Is.EqualTo(2));
            Assert.That(receipt.NodeId, Is.EqualTo(new StageNodeId(3)));
            Assert.That(receipt.StageKey, Is.EqualTo(new StageKey(202)));
            Assert.That(_buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(_buffers.CurrentProgressReadSink.CurrentStage.NodeId, Is.EqualTo(3));
            Assert.That(_buffers.MapProgressReadSink.GetMap(2).IsClear, Is.True);
            Assert.That(_buffers.StageProgressReadSink.GetStages(2).Where(x => x.StageKey != 0).All(x => x.IsClear), Is.True);
            AssertPersistent();
            Assert.That(_core.ChapterProgression.CompleteChapter(2, new StageNodeId(3)).FailureReason,
                Is.EqualTo(ChapterContinuationFailureReason.RunEnded));
            finalSnapshot.AssertUnchanged(_buffers);
            Assert.That(_buffers.RunProgressReadSink.Completion, Is.SameAs(receipt));
            Assert.That(_core.Combat.ResolveBattle(EmptyLoadout()), Is.False);
            Assert.Throws<InvalidOperationException>(() => _core.Combat.PrepareBattle());
            finalSnapshot.AssertUnchanged(_buffers);
            _core.Combat.EndBattle();
            _buffers.CombatViewWriteSink.Clear();
            Assert.That(_buffers.RunProgressReadSink.Completion, Is.SameAs(receipt));

            void AssertPersistent()
            {
                Assert.That(_buffers.CurrencyReadSink.Amount, Is.EqualTo(45));
                Assert.That(_buffers.CharacterReadSink.Owned.Count, Is.EqualTo(5));
                Assert.That(_buffers.CharacterReadSink.Owned.Take(4), Is.EqualTo(original));
                Assert.That(_buffers.CharacterReadSink.GetSoulCount(mainOwned), Is.EqualTo(2));
                Assert.That(_buffers.CharacterReadSink.GetSoulCount(subOwned), Is.EqualTo(1));
                Assert.That(_buffers.CharacterReadSink.GetSoulCount(acquired), Is.Zero);
                _buffers.CharacterReadSink.TryGetMainState(out var main);
                Assert.That(main.CurrentHealth, Is.EqualTo(4));
                Assert.That(main.PersistentMaxHealth, Is.EqualTo(10));
                Assert.That(_buffers.ArtifactReadSink.Current, Is.SameAs(artifact));
                Assert.That(artifact.PoolItems.Single().ArtifactId, Is.EqualTo(100));
                Assert.That(_buffers.StageProgressReadSink.GetStages(1).Where(x => x.StageKey != 0).All(x => x.IsClear), Is.True);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DefeatInSecondChapter_TerminatesWithoutClearingChapter(bool boss)
        {
            ReachSecondChapter();
            Enter(2, 2);
            if (boss)
            {
                WinBattle();
                _core.Combat.EndBattle();
                Enter(2, 3);
            }
            _core.Combat.ApplyHealth(MainId(), 0);
            Assert.That(_core.Combat.ResolveBattle(EmptyLoadout()), Is.True);
            Assert.That(_buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));
            var receipt = _buffers.RunProgressReadSink.Completion;
            Assert.That(receipt.ChapterKey, Is.EqualTo(2));
            Assert.That(receipt.NodeId, Is.EqualTo(new StageNodeId(boss ? 3 : 2)));
            Assert.That(receipt.StageKey, Is.EqualTo(new StageKey(boss ? 202 : 201)));
            Assert.That(_buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.True);
            Assert.That(_buffers.MapProgressReadSink.GetMap(2).IsClear, Is.False);
            Assert.That(_buffers.StageProgressReadSink.IsCleared(2, boss ? 3 : 2), Is.False);
            var ended = new RecoveryBufferSnapshot(_buffers);
            Assert.That(_core.Combat.ResolveBattle(EmptyLoadout()), Is.False);
            Assert.That(_core.Combat.AbandonBattle(), Is.Not.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.BattleAbandonResult.Abandoned));
            Assert.That(_core.ChapterProgression.CompleteChapter(2, new StageNodeId(3)).FailureReason,
                Is.EqualTo(ChapterContinuationFailureReason.RunEnded));
            Assert.That(_core.Combat.EnterBattle(2, new StageNodeId(3)).FailureReason,
                Is.EqualTo(ChapterProgressionFailureReason.RunEnded));
            ended.AssertUnchanged(_buffers);
            _core.Combat.EndBattle();
            _buffers.CombatViewWriteSink.Clear();
            Assert.That(_buffers.CombatStateReadSink.HasBattle, Is.False);
            Assert.That(_buffers.RunProgressReadSink.Completion, Is.SameAs(receipt));
            Assert.That(_buffers.MapProgressReadSink.GetMap(1).IsClear, Is.True);
            Assert.That(_buffers.MapProgressReadSink.GetMap(2).IsClear, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NewScenario_DoesNotMutateAnotherRuntime(bool withShop)
        {
            var other = withShop ? TemporaryCoreRuntimeFactory.CreateWithShop() : TemporaryCoreRuntimeFactory.Create();
            try
            {
                var otherBuffers = other.GetService(BufferSystemKey.Buffer);
                otherBuffers.CurrencyWriteSink.SetAmount(TemporaryCoreRuntimeTests.StartingRunCurrency);
                var original = new RecoveryBufferSnapshot(otherBuffers);
                Assert.That(otherBuffers.MapProgressReadSink.Maps.Count, Is.EqualTo(1));
                Assert.That(_buffers.MapProgressReadSink.Maps.Count, Is.EqualTo(2));
                Assert.That(otherBuffers.StageProgressReadSink.GetStages(1).Single(x => x.NodeId == 2).StageType,
                    Is.EqualTo(StageType.Event));
                Assert.That(_buffers.StageProgressReadSink.GetStages(1).Single(x => x.NodeId == 2).StageType,
                    Is.EqualTo(StageType.Combat));
                ReachSecondChapter();
                Assert.That(_buffers.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(2));
                original.AssertUnchanged(otherBuffers);
                Assert.That(otherBuffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
                Assert.That(otherBuffers.CharacterReadSink.Owned.Count, Is.EqualTo(4));
                Assert.That(otherBuffers.CurrencyReadSink.Amount, Is.EqualTo(50));
                Assert.That(otherBuffers.StageProgressReadSink.GetStages(2), Is.Empty);
            }
            finally { other.Shutdown(); }
        }

        private void ReachSecondChapter()
        {
            Enter(1, 2);
            WinBattle();
            _core.Combat.EndBattle();
            Assert.That(_core.Shop.EnterShop(1, new StageNodeId(5), 42).Accepted, Is.True);
            Assert.That(_core.Shop.CompleteShop(1, new StageNodeId(5)).Accepted, Is.True);
            Enter(1, 4);
            WinBattle();
            _core.Combat.EndBattle();
            Assert.That(_core.ChapterProgression.CompleteChapter(1, new StageNodeId(4)).Accepted, Is.True);
        }

        private void Enter(int chapter, int node)
            => Assert.That(_core.Combat.EnterBattle(chapter, new StageNodeId(node)).Accepted, Is.True);

        private EntityId MainId() => _buffers.CharacterReadSink.Characters
            .Single(x => x.OwnershipRole == CharacterOwnershipRole.Main).EntityId;

        private static ArtifactLoadoutSnapshot EmptyLoadout() => new(0, Array.Empty<ArtifactPlacedEffectSnapshot>());

        private void QueueOldInput()
        {
            int sequence = _buffers.CombatStateReadSink.LatestResultSequence;
            Assert.That(sequence, Is.GreaterThan(0));
            Assert.That(_buffers.CombatStateReadSink.HandledCommandNumber, Is.GreaterThan(0));
            Assert.That(_buffers.CombatStateReadSink.AnsweredPreviewNumber, Is.GreaterThan(0));
            _buffers.CombatViewWriteSink.AcknowledgePresented(sequence);
            _buffers.CombatViewWriteSink.Submit(CombatViewCommandData.DrawCharacter());
        }

        private void WinBattle()
        {
            Command(CombatViewCommandData.EnterCombat());
            for (int i = 0; i < 8 && _buffers.CombatStateReadSink.Phase == CombatFlowPhase.Automatic; i++)
                Assert.That(_core.Combat.AdvanceCombat(), Is.True);
            Assert.That(_buffers.CombatStateReadSink.Current.CommandState.ActiveActorId, Is.EqualTo(MainId()));
            Command(MovementPublicationTests.PreviewedMove(_buffers, _core.Combat, MainId(), _buffers.CharacterReadSink.Characters.Single(c => c.EntityId == MainId()).Anchor.Value, new CellCoord(1, 2)));
            _buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(MainId()));
            Assert.That(_core.Combat.ProcessLatestPreview(), Is.True);
            Assert.That(_buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Range.Value.Succeeded, Is.True);
            Command(CombatViewCommandData.Attack(MainId(), AttackAim.ForCell(new CellCoord(1, 3))));
            Assert.That(_buffers.CombatStateReadSink.Completion.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
        }

        private void Command(CombatViewCommandData command)
        {
            int number = _buffers.CombatViewWriteSink.Submit(command);
            Assert.That(_core.Combat.ProcessNextCommand(), Is.True);
            Assert.That(_buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Accepted, Is.True, result.Reason);
        }
    }
}
