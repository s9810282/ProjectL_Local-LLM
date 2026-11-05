using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class RecoveryStateContractTests
    {
        [TestCase("null-option")]
        [TestCase("negative-projection")]
        [TestCase("zero-available")]
        [TestCase("undefined")]
        [TestCase("wrong-phase")]
        [TestCase("run-mismatch")]
        [TestCase("entry-mismatch")]
        [TestCase("unknown-option")]
        [TestCase("revision-overflow")]
        [TestCase("disposed")]
        public void OptionState_RejectsInvalidValuesAndReasons(string invalid)
        {
            RecoveryFailureReason reason = invalid switch
            {
                "undefined" => (RecoveryFailureReason)999,
                "wrong-phase" => RecoveryFailureReason.WrongPhase,
                "run-mismatch" => RecoveryFailureReason.RunMismatch,
                "entry-mismatch" => RecoveryFailureReason.EntryMismatch,
                "unknown-option" => RecoveryFailureReason.UnknownOption,
                "revision-overflow" => RecoveryFailureReason.RevisionOverflow,
                "disposed" => RecoveryFailureReason.Disposed,
                _ => RecoveryFailureReason.None
            };

            Assert.That(() => new RecoveryOptionState(invalid == "null-option" ? null : Option(1),
                invalid == "negative-projection" ? -1 : invalid == "zero-available" ? 0 : 1, reason),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase("context")]
        [TestCase("default-health")]
        [TestCase("dead-health")]
        [TestCase("gold")]
        [TestCase("null-options")]
        [TestCase("empty-options")]
        [TestCase("duplicate-options")]
        [TestCase("null-element")]
        [TestCase("receipt")]
        [TestCase("projection")]
        public void State_RejectsMalformedOrInconsistentFacts(string invalid)
        {
            var option = new RecoveryOptionState(Option(1), invalid == "projection" ? 6 : 1,
                RecoveryFailureReason.None);
            IReadOnlyList<RecoveryOptionState> options = invalid switch
            {
                "null-options" => null,
                "empty-options" => Array.Empty<RecoveryOptionState>(),
                "duplicate-options" => new[] { option, new RecoveryOptionState(Option(1), 1, RecoveryFailureReason.None) },
                "null-element" => new RecoveryOptionState[] { null },
                _ => new[] { option }
            };
            HealthStateSnapshot health = invalid == "default-health" ? default :
                invalid == "dead-health" ? new HealthStateSnapshot(0, 10) : new HealthStateSnapshot(5, 10);

            Assert.That(() => new RecoveryStateSnapshot(invalid == "context" ? default : Context(),
                health, invalid == "gold" ? -1 : 3, options,
                invalid == "receipt" ? null : new RecoveryReceiptSnapshot(null, 0, 0), false),
                Throws.InstanceOf<ArgumentException>());
        }

        [TestCase(false, false, 3, 3, 1, RecoveryFailureReason.None)]
        [TestCase(false, false, 3, 2, 1, RecoveryFailureReason.InsufficientFunds)]
        [TestCase(false, false, 3, 2, 0, RecoveryFailureReason.NoEffect)]
        [TestCase(false, false, 0, 0, 1, RecoveryFailureReason.None)]
        [TestCase(false, true, 3, 0, 1, RecoveryFailureReason.AlreadyResolved)]
        [TestCase(true, false, 3, 0, 1, RecoveryFailureReason.StaleEntry)]
        [TestCase(true, true, 3, 0, 0, RecoveryFailureReason.StaleEntry)]
        public void State_AvailabilityMatchesReceiptCompletionAndFunds(bool completed, bool applied,
            int cost, int gold, int projection, RecoveryFailureReason reason)
        {
            var option = new RecoveryOptionState(Option(1, cost), projection, reason);
            var receipt = new RecoveryReceiptSnapshot(applied ? new RecoveryOptionId(1) : (RecoveryOptionId?)null,
                applied ? cost : 0, applied ? 1 : 0);
            var state = new RecoveryStateSnapshot(Context(), new HealthStateSnapshot(5, 10), gold,
                new[] { option }, receipt, completed);

            Assert.That(state.Options[0].UnavailableReason, Is.EqualTo(reason));
            Assert.That(state.Options[0].CanSelect, Is.EqualTo(reason == RecoveryFailureReason.None));
            Assert.That(state.IsCompleted, Is.EqualTo(completed));
            Assert.That(state.CanComplete, Is.EqualTo(!completed));
            Assert.That(state.Gold, Is.EqualTo(gold));
            Assert.That(state.Context, Is.EqualTo(Context()));
            Assert.That(state.MainCharacterHealth.CurrentHealth, Is.EqualTo(5));
            Assert.That(state.Receipt, Is.SameAs(receipt));
        }

        [TestCaseSource(nameof(InconsistentAvailabilityCases))]
        public void State_RejectsInconsistentAvailability(bool completed, bool applied, int gold,
            int projection, RecoveryFailureReason wrongReason)
        {
            var option = new RecoveryOptionState(Option(1), projection, wrongReason);
            var receipt = new RecoveryReceiptSnapshot(applied ? new RecoveryOptionId(1) : (RecoveryOptionId?)null,
                applied ? 3 : 0, applied ? 1 : 0);

            Assert.That(() => new RecoveryStateSnapshot(Context(), new HealthStateSnapshot(5, 10), gold,
                new[] { option }, receipt, completed), Throws.InstanceOf<ArgumentException>());
        }

        [TestCase("missing-option")]
        [TestCase("wrong-cost")]
        [TestCase("zero-heal")]
        [TestCase("free-applied")]
        public void State_ReceiptMatchesInstalledOption(string scenario)
        {
            int cost = scenario == "free-applied" ? 0 : 3;
            var option = new RecoveryOptionState(Option(1, cost), 1, RecoveryFailureReason.AlreadyResolved);
            var receipt = new RecoveryReceiptSnapshot(new RecoveryOptionId(scenario == "missing-option" ? 2 : 1),
                scenario == "wrong-cost" ? 2 : cost, scenario == "zero-heal" ? 0 : 1);
            TestDelegate create = () => new RecoveryStateSnapshot(Context(), new HealthStateSnapshot(5, 10),
                0, new[] { option }, receipt, false);

            if (scenario != "free-applied")
            {
                Assert.That(create, Throws.InstanceOf<ArgumentException>());
                return;
            }

            var state = new RecoveryStateSnapshot(Context(), new HealthStateSnapshot(5, 10),
                0, new[] { option }, receipt, false);
            Assert.That(state.Receipt.SpentAmount, Is.EqualTo(0));
            Assert.That(state.Receipt.HealedAmount, Is.EqualTo(1));
            Assert.That(state.Receipt.AppliedOptionId, Is.EqualTo(new RecoveryOptionId(1)));
            Assert.That(state.CanComplete, Is.True);
            Assert.That(state.Options[0].CanSelect, Is.False);
        }

        [Test]
        public void State_RetainsAuthoredOrderAndDetachedList()
        {
            var authored = new RecoveryOptionSpec(new RecoveryOptionId(2), 3,
                new RecoveryHealSpec(RecoveryHealMode.PercentOfMax, 50));
            var first = new RecoveryOptionState(authored, 1, RecoveryFailureReason.None);
            var second = new RecoveryOptionState(Option(1), 1, RecoveryFailureReason.None);
            var source = new[] { first, second };
            var state = new RecoveryStateSnapshot(Context(), new HealthStateSnapshot(5, 10), 3,
                source, new RecoveryReceiptSnapshot(null, 0, 0), false);
            source[0] = second;
            source[1] = first;

            Assert.That(state.Options[0], Is.SameAs(first));
            Assert.That(state.Options[1], Is.SameAs(second));
            Assert.That(state.Options[0].Option.Id, Is.EqualTo(new RecoveryOptionId(2)));
            Assert.That(state.Options[1].Option.Id, Is.EqualTo(new RecoveryOptionId(1)));
            Assert.That(state.Options[0].Option, Is.SameAs(authored));
            Assert.That(state.Options[0].Option.Heal.Mode, Is.EqualTo(RecoveryHealMode.PercentOfMax));
            Assert.That(state.Options[0].Option.Heal.Value, Is.EqualTo(50));
            Assert.That(state.Options[0].ProjectedHealAmount, Is.EqualTo(1));
            Assert.That(state.Receipt.AppliedOptionId, Is.Null);
            Assert.That(state.Receipt.SpentAmount, Is.EqualTo(0));
            Assert.That(state.Receipt.HealedAmount, Is.EqualTo(0));

            var mutable = state.Options as IList<RecoveryOptionState>;
            if (mutable != null)
            {
                Assert.That(mutable.IsReadOnly, Is.True);
                Assert.Throws<NotSupportedException>(() => mutable[0] = second);
                Assert.Throws<NotSupportedException>(() => mutable.Add(first));
                Assert.Throws<NotSupportedException>(() => mutable.Insert(0, first));
                Assert.Throws<NotSupportedException>(() => mutable.Remove(first));
                Assert.Throws<NotSupportedException>(() => mutable.RemoveAt(0));
                Assert.Throws<NotSupportedException>(() => mutable.Clear());
            }
            var nongeneric = state.Options as IList;
            if (nongeneric != null)
            {
                Assert.Throws<NotSupportedException>(() => nongeneric[0] = second);
            }
            Assert.That(state.Options.Count, Is.EqualTo(2));
            Assert.That(state.Options[0], Is.SameAs(first));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void State_HistoricalReceiptSurvivesFullHealth(bool completed)
        {
            var receipt = new RecoveryReceiptSnapshot(new RecoveryOptionId(2), 3, 5);
            var option = new RecoveryOptionState(Option(2), 0,
                completed ? RecoveryFailureReason.StaleEntry : RecoveryFailureReason.AlreadyResolved);
            var state = new RecoveryStateSnapshot(Context(), new HealthStateSnapshot(10, 10), 0,
                new[] { option }, receipt, completed);

            Assert.That(state.Receipt.HealedAmount, Is.EqualTo(5));
            Assert.That(state.Receipt.SpentAmount, Is.EqualTo(3));
            Assert.That(state.Receipt.AppliedOptionId, Is.EqualTo(new RecoveryOptionId(2)));
            Assert.That(state.Options[0].ProjectedHealAmount, Is.EqualTo(0));
            Assert.That(state.Options[0].CanSelect, Is.False);
            Assert.That(state.CanComplete, Is.EqualTo(!completed));
        }

        private static IEnumerable<TestCaseData> InconsistentAvailabilityCases()
        {
            var cases = new[]
            {
                (Completed: false, Applied: false, Gold: 3, Projection: 1, Reason: RecoveryFailureReason.None),
                (Completed: false, Applied: false, Gold: 2, Projection: 1, Reason: RecoveryFailureReason.InsufficientFunds),
                (Completed: false, Applied: false, Gold: 2, Projection: 0, Reason: RecoveryFailureReason.NoEffect),
                (Completed: false, Applied: true, Gold: 0, Projection: 0, Reason: RecoveryFailureReason.AlreadyResolved),
                (Completed: true, Applied: true, Gold: 0, Projection: 0, Reason: RecoveryFailureReason.StaleEntry)
            };
            var reasons = new[] { RecoveryFailureReason.None, RecoveryFailureReason.InsufficientFunds,
                RecoveryFailureReason.NoEffect, RecoveryFailureReason.AlreadyResolved, RecoveryFailureReason.StaleEntry };
            foreach (var scenario in cases)
            {
                foreach (RecoveryFailureReason reason in reasons)
                {
                    if (reason == scenario.Reason || (scenario.Projection == 0 && reason == RecoveryFailureReason.None))
                    {
                        continue;
                    }
                    yield return new TestCaseData(scenario.Completed, scenario.Applied, scenario.Gold,
                        scenario.Projection, reason);
                }
            }
        }

        private static RecoveryOptionSpec Option(int id, int cost = 3)
        {
            return new RecoveryOptionSpec(new RecoveryOptionId(id), cost,
                new RecoveryHealSpec(RecoveryHealMode.Flat, 3));
        }

        private static RecoveryEntryContext Context()
        {
            return new RecoveryEntryContext(new StageEntryId(new Guid("11111111-1111-1111-1111-111111111111")),
                1, new StageNodeId(2), new StageKey(20));
        }
    }
}
