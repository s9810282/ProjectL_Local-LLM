using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class RecoveryProcessedResultContractTests
    {
        [TestCase("one")]
        [TestCase("maximum")]
        [TestCase("zero")]
        [TestCase("negative")]
        [TestCase("command")]
        [TestCase("result")]
        public void Processed_RequiresPositiveNumberAndNonNullParts(string scenario)
        {
            int number = scenario == "maximum" ? int.MaxValue : scenario == "zero" ? 0 : scenario == "negative" ? -1 : 1;
            RecoveryCommandData command = scenario == "command" ? null : RecoveryCommandData.Complete(RecoveryResultTestData.Context());
            RecoveryOperationResult result = scenario == "result" ? null : RecoveryOperationResult.StateMismatch(RecoveryFailureReason.StaleEntry);
            if (scenario != "one" && scenario != "maximum")
            {
                Assert.That(() => new RecoveryProcessedResult(number, command, result), Throws.InstanceOf<ArgumentException>());
                return;
            }

            var processed = new RecoveryProcessedResult(number, command, result);
            Assert.That(processed.Number, Is.EqualTo(number));
            Assert.That(processed.Command, Is.SameAs(command));
            Assert.That(processed.Result, Is.SameAs(result));
        }

        [TestCase("entry", "select")]
        [TestCase("chapter", "select")]
        [TestCase("node", "select")]
        [TestCase("stage", "select")]
        [TestCase("entry", "trade")]
        [TestCase("chapter", "trade")]
        [TestCase("node", "trade")]
        [TestCase("stage", "trade")]
        [TestCase("entry", "complete")]
        [TestCase("chapter", "complete")]
        [TestCase("node", "complete")]
        [TestCase("stage", "complete")]
        public void Processed_RejectsDifferentContextIncludingSameKeysNewEntry(string changed, string operation)
        {
            RecoveryEntryContext original = RecoveryResultTestData.Context();
            RecoveryEntryContext other = RecoveryResultTestData.Context(changed == "chapter" ? 2 : 1,
                changed == "node" ? 3 : 2, changed == "stage" ? 21 : 20,
                changed == "entry" ? new Guid("22222222-2222-2222-2222-222222222222") : (Guid?)null);
            RecoveryCommandData command = operation == "complete" ? RecoveryCommandData.Complete(original) :
                RecoveryCommandData.Select(original, new RecoveryOptionId(1));
            RecoveryOperationResult result;
            if (operation == "complete")
            {
                result = Completion(RecoveryResultTestData.State(RecoveryFailureReason.StaleEntry, true, context: other));
            }
            else if (operation == "trade")
            {
                result = RecoveryOperationResult.TradeRejected(RecoveryFailureReason.InsufficientFunds,
                    RecoveryResultTestData.State(RecoveryFailureReason.InsufficientFunds, context: other));
            }
            else
            {
                result = RecoveryOperationResult.Selected(RecoveryResultTestData.State(RecoveryFailureReason.AlreadyResolved,
                    applied: 1, context: other));
            }

            Assert.That(() => new RecoveryProcessedResult(1, command, result), Throws.InstanceOf<ArgumentException>());
        }

        [TestCase("accepted", true)]
        [TestCase("retry", true)]
        [TestCase("wrong-applied", false)]
        [TestCase("unknown-accepted", false)]
        [TestCase("funds", true)]
        [TestCase("no-effect", true)]
        [TestCase("wrong-funds", false)]
        [TestCase("wrong-no-effect", false)]
        [TestCase("unknown-trade", false)]
        [TestCase("resolved-other", true)]
        [TestCase("resolved-same", false)]
        [TestCase("resolved-unknown", false)]
        [TestCase("completed", false)]
        public void Processed_SelectMatchesAppliedOrRejectedOption(string scenario, bool valid)
        {
            int optionId = scenario.StartsWith("unknown") || scenario == "resolved-unknown" ? 3 :
                scenario == "wrong-applied" || scenario == "funds" || scenario == "wrong-no-effect" ||
                scenario == "resolved-other" ? 2 : 1;
            RecoveryCommandData command = RecoveryCommandData.Select(RecoveryResultTestData.Context(), new RecoveryOptionId(optionId));
            RecoveryOperationResult result;
            switch (scenario)
            {
                case "funds":
                case "wrong-funds":
                case "unknown-trade":
                    result = RecoveryOperationResult.TradeRejected(RecoveryFailureReason.InsufficientFunds,
                        RecoveryResultTestData.MixedTradeState());
                    break;
                case "no-effect":
                case "wrong-no-effect":
                    result = RecoveryOperationResult.TradeRejected(RecoveryFailureReason.NoEffect,
                        RecoveryResultTestData.MixedTradeState());
                    break;
                case "resolved-other":
                case "resolved-same":
                case "resolved-unknown":
                    result = RecoveryOperationResult.TradeRejected(RecoveryFailureReason.AlreadyResolved,
                        RecoveryResultTestData.State(RecoveryFailureReason.AlreadyResolved, applied: 1));
                    break;
                case "completed":
                    result = Completion(RecoveryResultTestData.State(RecoveryFailureReason.StaleEntry, true, 1));
                    break;
                default:
                    result = RecoveryOperationResult.Selected(
                        RecoveryResultTestData.State(RecoveryFailureReason.AlreadyResolved, applied: 1), scenario == "retry");
                    break;
            }
            if (!valid)
            {
                Assert.That(() => new RecoveryProcessedResult(7, command, result), Throws.InstanceOf<ArgumentException>());
                return;
            }

            var processed = new RecoveryProcessedResult(7, command, result);
            Assert.That(processed.Number, Is.EqualTo(7));
            Assert.That(processed.Command, Is.SameAs(command));
            Assert.That(processed.Result, Is.SameAs(result));
            Assert.That(processed.Command.OptionId, Is.EqualTo(new RecoveryOptionId(optionId)));
            Assert.That(processed.Result.AlreadyApplied, Is.EqualTo(scenario == "retry"));
        }

        [TestCase("skipped", true)]
        [TestCase("applied", true)]
        [TestCase("wrong-phase", true)]
        [TestCase("entry-mismatch", true)]
        [TestCase("stale", true)]
        [TestCase("unknown-option", false)]
        [TestCase("selected", false)]
        [TestCase("selected-retry", false)]
        [TestCase("trade-funds", false)]
        [TestCase("trade-no-effect", false)]
        [TestCase("trade-resolved", false)]
        public void Processed_CompleteMatchesCompletionOnly(string scenario, bool valid)
        {
            RecoveryCommandData command = RecoveryCommandData.Complete(RecoveryResultTestData.Context());
            RecoveryOperationResult result = scenario switch
            {
                "wrong-phase" => RecoveryOperationResult.StateMismatch(RecoveryFailureReason.WrongPhase),
                "entry-mismatch" => RecoveryOperationResult.StateMismatch(RecoveryFailureReason.EntryMismatch),
                "stale" => RecoveryOperationResult.StateMismatch(RecoveryFailureReason.StaleEntry),
                "unknown-option" => RecoveryOperationResult.StateMismatch(RecoveryFailureReason.UnknownOption),
                "selected" => RecoveryOperationResult.Selected(RecoveryResultTestData.State(RecoveryFailureReason.AlreadyResolved, applied: 1)),
                "selected-retry" => RecoveryOperationResult.Selected(RecoveryResultTestData.State(RecoveryFailureReason.AlreadyResolved, applied: 1), true),
                "trade-funds" => RecoveryOperationResult.TradeRejected(RecoveryFailureReason.InsufficientFunds, RecoveryResultTestData.MixedTradeState()),
                "trade-no-effect" => RecoveryOperationResult.TradeRejected(RecoveryFailureReason.NoEffect, RecoveryResultTestData.MixedTradeState()),
                "trade-resolved" => RecoveryOperationResult.TradeRejected(RecoveryFailureReason.AlreadyResolved,
                    RecoveryResultTestData.State(RecoveryFailureReason.AlreadyResolved, applied: 1)),
                _ => Completion(RecoveryResultTestData.State(RecoveryFailureReason.StaleEntry, true, scenario == "applied" ? 1 : (int?)null))
            };
            if (!valid)
            {
                Assert.That(() => new RecoveryProcessedResult(8, command, result), Throws.InstanceOf<ArgumentException>());
                return;
            }

            var processed = new RecoveryProcessedResult(8, command, result);
            Assert.That(processed.Command, Is.SameAs(command));
            Assert.That(processed.Result, Is.SameAs(result));
            Assert.That(processed.Command.OptionId, Is.Null);
            Assert.That(processed.Result.AlreadyApplied, Is.False);
        }

        [TestCase(RecoveryFailureReason.WrongPhase)]
        [TestCase(RecoveryFailureReason.EntryMismatch)]
        [TestCase(RecoveryFailureReason.StaleEntry)]
        [TestCase(RecoveryFailureReason.UnknownOption)]
        public void StateMismatch_RetainsOriginalSelectCommand(RecoveryFailureReason reason)
        {
            RecoveryCommandData command = RecoveryCommandData.Select(RecoveryResultTestData.Context(), new RecoveryOptionId(3));
            RecoveryOperationResult result = RecoveryOperationResult.StateMismatch(reason);
            var processed = new RecoveryProcessedResult(17, command, result);

            Assert.That(processed.Number, Is.EqualTo(17));
            Assert.That(processed.Command, Is.SameAs(command));
            Assert.That(processed.Result, Is.SameAs(result));
            Assert.That(processed.Result.FailureReason, Is.EqualTo(reason));
            Assert.That(processed.Result.StateAfter, Is.Null);
            Assert.That(processed.Result.Progression, Is.Null);
        }

        private static RecoveryOperationResult Completion(RecoveryStateSnapshot state)
        {
            return RecoveryOperationResult.Completed(state, ChapterProgressionResult.FinishStage(
                state.Context.ChapterKey, state.Context.NodeId, state.Context.StageKey, false));
        }
    }
}
