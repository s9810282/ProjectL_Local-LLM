using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CombatTransitionPublisherTests
    {
        [Test]
        public void TerminalTransition_AttachesCompletionWithoutResettling()
        {
            using var battle = TerminalBattle();
            var progression = new Progression(ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(1101), true));
            var parts = battle.Publication(progression);
            var proposal = Calculate(parts.Attack(battle.Actor, new CellCoord(1, 3)));
            Assert.That(proposal.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(proposal.Completion, Is.Null);
            var b = battle.Buffers;
            int sequence = b.CombatStateReadSink.LatestResultSequence;

            Assert.That(parts.Publisher.Publish(proposal), Is.True);

            Assert.That(progression.Calls, Is.EqualTo(1));
            Assert.That(progression.LastChapter, Is.EqualTo(1));
            Assert.That(progression.LastOutcome, Is.EqualTo(StageOutcome.Victory));
            var completion = b.CombatStateReadSink.Completion;
            Assert.That(completion, Is.Not.Null);
            Assert.That(completion.Outcome, Is.EqualTo(proposal.Outcome));
            Assert.That(completion.Progression, Is.EqualTo(progression.Result));
            // 이미 계산한 정산을 그대로 붙이고 추가 회복·영혼 계산을 만들지 않는다.
            Assert.That(completion.Characters, Is.SameAs(proposal.Characters));
            Assert.That(completion.Settlement.Characters, Is.SameAs(proposal.Characters));
            Assert.That(completion.Settlement.Field, Is.SameAs(proposal.Field));
            Assert.That(b.CombatStateReadSink.Current.CommandState.SameValues(proposal.FinalState), Is.True);
            Assert.That(b.CombatStateReadSink.SoulField.SameValues(proposal.Field.Final), Is.True);
            foreach (var change in proposal.Characters.Characters)
                Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == change.Final.EntityId)
                    .ToFacts().SameValues(change.Final), Is.True);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(proposal.Input.Number));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events, Is.EqualTo(proposal.Events));
        }

        [Test]
        public void RunEndedDuringResolution_DoesNotPublish()
        {
            using var battle = TerminalBattle();
            var progression = new Progression(ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded));
            var parts = battle.Publication(progression);
            var proposal = Calculate(parts.Attack(battle.Actor, new CellCoord(1, 3)));
            Assert.That(proposal.Outcome.IsStageEnded, Is.True);
            var before = new CoreIntegrationSnapshot(battle.Buffers);

            Assert.That(parts.Publisher.Publish(proposal), Is.False);

            Assert.That(progression.Calls, Is.EqualTo(1));
            Assert.That(progression.LastOutcome, Is.EqualTo(StageOutcome.Victory));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void RejectedProgression_DoesNotPublish()
        {
            using var battle = TerminalBattle();
            var progression = new Progression(ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch));
            var parts = battle.Publication(progression);
            var proposal = Calculate(parts.Attack(battle.Actor, new CellCoord(1, 3)));
            Assert.That(proposal.Outcome.IsStageEnded, Is.True);
            var before = new CoreIntegrationSnapshot(battle.Buffers);

            Assert.That(Assert.Throws<InvalidOperationException>(() => parts.Publisher.Publish(proposal)).Message,
                Does.Contain("StageMismatch"));

            Assert.That(progression.Calls, Is.EqualTo(1));
            before.AssertUnchanged(battle.Buffers);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NonterminalAndRejectedCommand_PreservePublicationContract(bool accepted)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));
            var progression = new Progression(ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(1101), true));
            var parts = battle.Publication(progression);
            var input = parts.Attack(battle.Actor, accepted ? new CellCoord(0, 3) : new CellCoord(99, 99));
            var proposal = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(proposal.Accepted, Is.EqualTo(accepted), proposal.Reason);
            Assert.That(proposal.Outcome.IsStageEnded, Is.False);
            var b = battle.Buffers;
            int sequence = b.CombatStateReadSink.LatestResultSequence;

            Assert.That(parts.Publisher.Publish(proposal), Is.True);

            Assert.That(progression.Calls, Is.Zero);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(input.Number));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.EqualTo(accepted));
            Assert.That(result.Reason, Is.EqualTo(proposal.Reason));
            Assert.That(b.CombatStateReadSink.Current.CommandState.SameValues(proposal.FinalState), Is.True);
        }

        private static ConfiguredMovementBattle TerminalBattle() => new(ConfiguredMovementBattle.Config,
            new ConfiguredMovementBattle.EncounterSource(terminalHeadquarters: true));

        private static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.ActionUseSettlement, Is.Not.Null);
            return result;
        }

        private sealed class Progression : IChapterProgressionCoordinator
        {
            internal readonly ChapterProgressionResult Result;
            internal int Calls;
            internal int LastChapter;
            internal StageOutcome LastOutcome;
            internal Progression(ChapterProgressionResult result) => Result = result;
            public RunOutcome Outcome => RunOutcome.InProgress;
            public RunCompletionReceipt Completion => null;
            public ChapterProgressionResult PlanStageResolution(int chapterKey, StageOutcome outcome)
            {
                Calls++;
                LastChapter = chapterKey;
                LastOutcome = outcome;
                return Result;
            }
            public ChapterContinuationResult CompleteChapter(int key, StageNodeId id) => throw new NotSupportedException();
            public ChapterProgressionResult EnterChapter(int key) => throw new NotSupportedException();
            public ChapterProgressionResult EnterChapter(int key, ChapterStagePin pin) => throw new NotSupportedException();
            public ChapterProgressionResult PrepareStageEntry(int key, StageNodeId id) => throw new NotSupportedException();
            public ChapterProgressionResult ConfirmStageEntry(int key, StageNodeId id) => throw new NotSupportedException();
            public ChapterProgressionResult ResolveStage(int key, StageOutcome outcome) => throw new NotSupportedException();
        }
    }
}
