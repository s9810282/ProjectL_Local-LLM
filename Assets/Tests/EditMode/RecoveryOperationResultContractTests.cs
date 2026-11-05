using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class RecoveryOperationResultContractTests
    {
        [TestCase("first")]
        [TestCase("retry")]
        [TestCase("null")]
        [TestCase("unapplied")]
        [TestCase("completed")]
        public void Selected_RequiresAppliedOpenState(string scenario)
        {
            RecoveryStateSnapshot state = scenario == "null" ? null :
                scenario == "unapplied" ? RecoveryResultTestData.State(RecoveryFailureReason.None) :
                RecoveryResultTestData.State(scenario == "completed" ? RecoveryFailureReason.StaleEntry :
                    RecoveryFailureReason.AlreadyResolved, scenario == "completed", 1);
            bool retry = scenario == "retry";
            if (scenario != "first" && !retry)
            {
                Assert.That(() => RecoveryOperationResult.Selected(state), Throws.InstanceOf<ArgumentException>());
                return;
            }

            RecoveryOperationResult result = RecoveryOperationResult.Selected(state, retry);
            Assert.That(result.Kind, Is.EqualTo(RecoveryResultKind.Accepted));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(RecoveryFailureReason.None));
            Assert.That(result.AlreadyApplied, Is.EqualTo(retry));
            Assert.That(result.StateAfter, Is.SameAs(state));
            Assert.That(result.Progression, Is.Null);
        }

        [TestCase("skipped")]
        [TestCase("applied")]
        [TestCase("null-state")]
        [TestCase("null-progression")]
        [TestCase("open")]
        [TestCase("rejected")]
        [TestCase("enter-stage")]
        [TestCase("enter-chapter")]
        [TestCase("defeat")]
        [TestCase("boss")]
        [TestCase("chapter")]
        [TestCase("node")]
        [TestCase("stage")]
        public void Completed_RequiresMatchingNodeOnlyProgression(string scenario)
        {
            RecoveryStateSnapshot state = scenario == "null-state" ? null :
                RecoveryResultTestData.State(scenario == "open" ? RecoveryFailureReason.None : RecoveryFailureReason.StaleEntry,
                    scenario != "open", scenario == "applied" ? 1 : (int?)null);
            ChapterProgressionResult progression = scenario switch
            {
                "null-progression" => null,
                "rejected" => ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageAlreadyCleared),
                "enter-stage" => ChapterProgressionResult.EnterStage(1, new StageNodeId(2), new StageKey(20)),
                "enter-chapter" => ChapterProgressionResult.EnterChapter(1),
                "defeat" => ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(20)),
                _ => ChapterProgressionResult.FinishStage(scenario == "chapter" ? 2 : 1,
                    new StageNodeId(scenario == "node" ? 3 : 2), new StageKey(scenario == "stage" ? 21 : 20), scenario == "boss")
            };
            if (scenario != "skipped" && scenario != "applied")
            {
                Assert.That(() => RecoveryOperationResult.Completed(state, progression), Throws.InstanceOf<ArgumentException>());
                return;
            }

            RecoveryOperationResult result = RecoveryOperationResult.Completed(state, progression);
            Assert.That(result.Kind, Is.EqualTo(RecoveryResultKind.Accepted));
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(RecoveryFailureReason.None));
            Assert.That(result.AlreadyApplied, Is.False);
            Assert.That(result.StateAfter, Is.SameAs(state));
            Assert.That(result.Progression, Is.SameAs(progression));
        }

        [TestCase("funds")]
        [TestCase("no-effect")]
        [TestCase("resolved")]
        [TestCase("null")]
        [TestCase("completed")]
        [TestCase("funds-applied")]
        [TestCase("no-effect-applied")]
        [TestCase("resolved-unapplied")]
        [TestCase("funds-missing-reason")]
        [TestCase("no-effect-missing-reason")]
        public void TradeRejected_RequiresSupportedReasonAndCurrentState(string scenario)
        {
            RecoveryFailureReason reason = scenario.StartsWith("no-effect") ? RecoveryFailureReason.NoEffect :
                scenario.StartsWith("resolved") ? RecoveryFailureReason.AlreadyResolved : RecoveryFailureReason.InsufficientFunds;
            bool applied = scenario == "resolved" || scenario == "funds-applied" || scenario == "no-effect-applied";
            RecoveryFailureReason stateReason = scenario == "completed" ? RecoveryFailureReason.StaleEntry :
                applied ? RecoveryFailureReason.AlreadyResolved :
                scenario == "resolved-unapplied" || scenario.EndsWith("missing-reason") ? RecoveryFailureReason.None : reason;
            RecoveryStateSnapshot state = scenario == "null" ? null :
                RecoveryResultTestData.State(stateReason, scenario == "completed", applied ? 1 : (int?)null);
            if (scenario != "funds" && scenario != "no-effect" && scenario != "resolved")
            {
                Assert.That(() => RecoveryOperationResult.TradeRejected(reason, state), Throws.InstanceOf<ArgumentException>());
                return;
            }

            RecoveryOperationResult result = RecoveryOperationResult.TradeRejected(reason, state);
            Assert.That(result.Kind, Is.EqualTo(RecoveryResultKind.TradeRejected));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.AlreadyApplied, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(reason));
            Assert.That(result.StateAfter, Is.SameAs(state));
            Assert.That(result.Progression, Is.Null);
        }

        [TestCase(RecoveryFailureReason.None)]
        [TestCase(RecoveryFailureReason.WrongPhase)]
        [TestCase(RecoveryFailureReason.RunMismatch)]
        [TestCase(RecoveryFailureReason.EntryMismatch)]
        [TestCase(RecoveryFailureReason.StaleEntry)]
        [TestCase(RecoveryFailureReason.UnknownOption)]
        [TestCase(RecoveryFailureReason.RevisionOverflow)]
        [TestCase(RecoveryFailureReason.Disposed)]
        [TestCase((RecoveryFailureReason)999)]
        public void TradeRejected_RejectsUnsupportedReason(RecoveryFailureReason reason)
        {
            RecoveryStateSnapshot state = RecoveryResultTestData.State(RecoveryFailureReason.None);
            Assert.That(() => RecoveryOperationResult.TradeRejected(reason, state), Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(RecoveryFailureReason.WrongPhase, true)]
        [TestCase(RecoveryFailureReason.EntryMismatch, true)]
        [TestCase(RecoveryFailureReason.StaleEntry, true)]
        [TestCase(RecoveryFailureReason.UnknownOption, true)]
        [TestCase(RecoveryFailureReason.None, false)]
        [TestCase(RecoveryFailureReason.RunMismatch, false)]
        [TestCase(RecoveryFailureReason.InsufficientFunds, false)]
        [TestCase(RecoveryFailureReason.NoEffect, false)]
        [TestCase(RecoveryFailureReason.AlreadyResolved, false)]
        [TestCase(RecoveryFailureReason.RevisionOverflow, false)]
        [TestCase(RecoveryFailureReason.Disposed, false)]
        [TestCase((RecoveryFailureReason)999, false)]
        public void StateMismatch_HasNoStateOrProgression(RecoveryFailureReason reason, bool supported)
        {
            if (!supported)
            {
                Assert.That(() => RecoveryOperationResult.StateMismatch(reason), Throws.InstanceOf<ArgumentException>());
                return;
            }

            RecoveryOperationResult result = RecoveryOperationResult.StateMismatch(reason);
            Assert.That(result.Kind, Is.EqualTo(RecoveryResultKind.StateMismatch));
            Assert.That(result.FailureReason, Is.EqualTo(reason));
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.AlreadyApplied, Is.False);
            Assert.That(result.StateAfter, Is.Null);
            Assert.That(result.Progression, Is.Null);
        }
    }

    internal static class RecoveryResultTestData
    {
        internal static RecoveryEntryContext Context(int chapter = 1, int node = 2, int stage = 20, Guid? entry = null)
        {
            return new RecoveryEntryContext(new StageEntryId(entry ?? new Guid("11111111-1111-1111-1111-111111111111")),
                chapter, new StageNodeId(node), new StageKey(stage));
        }

        internal static RecoveryStateSnapshot State(RecoveryFailureReason reason, bool completed = false,
            int? applied = null, RecoveryEntryContext? context = null)
        {
            var options = new[] { Option(1, reason), Option(2, reason) };
            var receipt = new RecoveryReceiptSnapshot(applied.HasValue ? new RecoveryOptionId(applied.Value) : (RecoveryOptionId?)null,
                applied.HasValue ? 3 : 0, applied.HasValue ? 1 : 0);
            return new RecoveryStateSnapshot(context ?? Context(), new HealthStateSnapshot(5, 10),
                reason == RecoveryFailureReason.InsufficientFunds ? 2 : 3, options, receipt, completed);
        }

        internal static RecoveryStateSnapshot MixedTradeState()
        {
            return new RecoveryStateSnapshot(Context(), new HealthStateSnapshot(5, 10), 2,
                new[] { Option(1, RecoveryFailureReason.NoEffect), Option(2, RecoveryFailureReason.InsufficientFunds) },
                new RecoveryReceiptSnapshot(null, 0, 0), false);
        }

        private static RecoveryOptionState Option(int id, RecoveryFailureReason reason)
        {
            return new RecoveryOptionState(new RecoveryOptionSpec(new RecoveryOptionId(id), 3,
                new RecoveryHealSpec(RecoveryHealMode.Flat, 3)), reason == RecoveryFailureReason.NoEffect ? 0 : 1, reason);
        }
    }
}
