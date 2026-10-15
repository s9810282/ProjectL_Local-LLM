using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed class ChapterContinuationPublicationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void PreparedContinuation_PublishesAfterAllValidation(bool final)
        {
            var b = Completed();
            b.MapProgressWriteSink.RegisterMap(2);
            var decision = final ? ChapterContinuationResult.FinishRun(1, new StageNodeId(2), new StageKey(10))
                : ChapterContinuationResult.SelectNext(1, new StageNodeId(2), new StageKey(10), 2);
            var proposal = new ChapterContinuationPreparation(decision, b.CurrentProgressReadSink.CurrentStage,
                b.CombatStateReadSink.Completion, b.StageProgressReadSink.GetStages(1),
                Array.Empty<StageProgressData>(), final ? Array.Empty<StageProgressData>() : Target(), true, false);
            b.ChapterContinuationWriteSink.PublishContinuation(proposal);
            Assert.That(b.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(final ? 1 : 2));
            Assert.That(b.CurrentProgressReadSink.HasSelection, Is.EqualTo(final));
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(final ? RunOutcome.Victory : RunOutcome.InProgress));
        }

        internal static IBufferQuery Completed()
        {
            var b = CharacterBattleSettlementTests.Create();
            var main = b.CharacterReadSink.Characters.Single().ToFacts();
            b.CharacterReadSink.TryGetMainState(out var persistent);
            var changes = new BattleCompletionCalculator(new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()))
                .Complete(main, persistent, new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                    ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(10), true));
            Assert.That(b.BattleWriteSink.CompleteBattle(changes), Is.True);
            b.BattleWriteSink.EndBattle();
            return b;
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReusedTarget_PreservesAllOwnedFactsAndOldClears(bool reuse)
        {
            var b = Completed(); b.MapProgressWriteSink.RegisterMap(2);
            b.CurrencyWriteSink.SetAmount(91);
            var target = Target();
            if (reuse) b.StageProgressWriteSink.RegisterStages(2, target);
            var before = new RecoveryBufferSnapshot(b);
            var proposal = Next(b, reuse ? target : Array.Empty<StageProgressData>(), reuse ? Array.Empty<StageProgressData>() : target);
            b.ChapterContinuationWriteSink.PublishContinuation(proposal);
            before.AssertUnchanged(b, selection: false, progress: false);
            Assert.That(b.MapProgressReadSink.GetMap(1).IsClear, Is.True);
            Assert.That(b.StageProgressReadSink.IsCleared(1, 2), Is.True);
            Assert.That(b.CurrentProgressReadSink.CurrentMapKey, Is.EqualTo(2));
            Assert.That(b.CurrentProgressReadSink.Shop, Is.Null);
            Assert.That(b.CurrentProgressReadSink.Recovery, Is.Null);
            Assert.That(b.StageProgressReadSink.GetStages(2), Is.EqualTo(target));
        }

        [TestCase("edge")]
        [TestCase("duplicate")]
        [TestCase("source")]
        [TestCase("target")]
        [TestCase("selected")]
        [TestCase("terminal")]
        [TestCase("retained")]
        [TestCase("map")]
        public void InvalidPreparedTarget_LeavesBothGraphAndSelectionUnchanged(string fault)
        {
            var b = Completed(); b.MapProgressWriteSink.RegisterMap(2);
            var graph = Target();
            if (fault == "edge") graph[0] = new StageProgressData(1, 0, 0, 0, new[] { 99 });
            if (fault == "duplicate") graph[1] = graph[0];
            var proposal = Next(b, Array.Empty<StageProgressData>(), graph);
            if (fault == "source") b.StageProgressWriteSink.SetClear(1, 2, false);
            if (fault == "target") b.StageProgressWriteSink.RegisterStages(2, Target());
            if (fault == "selected") b.CurrentProgressWriteSink.SelectStage(new StageNodeId(2), new StageKey(11), "Changed");
            if (fault == "terminal") RunTerminationAdmissionTests.Seed(b, RunOutcome.Defeat);
            if (fault == "map") b.MapProgressWriteSink.SetClear(2, true);
            if (fault == "retained")
            {
                b.CharacterReadSink.TryGetMainState(out var main);
                var owned = b.CharacterReadSink.Owned.Single();
                b.BattleWriteSink.PrepareBattle(new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.BattlePreparation(
                    new[] { CharacterBufferTestData.MainFacts(owned.Id, main) }, CharacterBufferTestData.Combat()));
            }
            var before = new RecoveryBufferSnapshot(b);
            var outcome = b.RunProgressReadSink.Outcome; var receipt = b.RunProgressReadSink.Completion;
            Assert.Catch(() => b.ChapterContinuationWriteSink.PublishContinuation(proposal));
            before.AssertUnchanged(b);
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(outcome));
            Assert.That(b.RunProgressReadSink.Completion, Is.SameAs(receipt));
        }

        [Test]
        public void FinalPublication_PreservesSelectionAndRejectsDuplicateReceipt()
        {
            var b = Completed();
            var proposal = new ChapterContinuationPreparation(ChapterContinuationResult.FinishRun(1, new StageNodeId(2), new StageKey(10)),
                b.CurrentProgressReadSink.CurrentStage, b.CombatStateReadSink.Completion, b.StageProgressReadSink.GetStages(1),
                Array.Empty<StageProgressData>(), Array.Empty<StageProgressData>(), true, false);
            var before = new RecoveryBufferSnapshot(b);
            b.ChapterContinuationWriteSink.PublishContinuation(proposal);
            before.AssertUnchanged(b);
            var receipt = b.RunProgressReadSink.Completion;
            Assert.That(receipt.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.Throws<InvalidOperationException>(() => b.ChapterContinuationWriteSink.PublishContinuation(proposal));
            Assert.That(b.RunProgressReadSink.Completion, Is.SameAs(receipt));
            before.AssertUnchanged(b);
        }

        [Test]
        public void CompletionIdentity_MustMatchClearedNodeKey()
        {
            var b = Completed();
            b.StageProgressWriteSink.Clear();
            b.StageProgressWriteSink.RegisterStages(1, new[] { new StageProgressData(2, 999, 1, 0, Array.Empty<int>(), true) });
            var proposal = new ChapterContinuationPreparation(ChapterContinuationResult.FinishRun(1, new StageNodeId(2), new StageKey(10)),
                b.CurrentProgressReadSink.CurrentStage, b.CombatStateReadSink.Completion, b.StageProgressReadSink.GetStages(1),
                Array.Empty<StageProgressData>(), Array.Empty<StageProgressData>(), true, false);
            var before = new RecoveryBufferSnapshot(b);
            Assert.Throws<InvalidOperationException>(() => b.ChapterContinuationWriteSink.PublishContinuation(proposal));
            before.AssertUnchanged(b);
            Assert.That(b.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
        }

        private static ChapterContinuationPreparation Next(IBufferQuery b, StageProgressData[] expected, StageProgressData[] generated)
            => new(ChapterContinuationResult.SelectNext(1, new StageNodeId(2), new StageKey(10), 2),
                b.CurrentProgressReadSink.CurrentStage, b.CombatStateReadSink.Completion, b.StageProgressReadSink.GetStages(1), expected, generated, true, false);

        internal static StageProgressData[] Target() => new[] {
            new StageProgressData(1, 0, 0, 0, new[] { 2 }),
            new StageProgressData(2, 201, 1, 0, Array.Empty<int>()) };
    }
}
