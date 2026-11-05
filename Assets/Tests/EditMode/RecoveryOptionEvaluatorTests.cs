using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class RecoveryOptionEvaluatorTests
    {
        [TestCase(int.MaxValue)]
        [TestCase(int.MaxValue - 5)]
        public void GrowthBeyondCapacity_IsUnavailableWithoutBreakingTheVisitProjection(int maximum)
        {
            var option = new RecoveryOptionSpec(new RecoveryOptionId(4), 10, 10);
            var state = RecoveryOptionEvaluator.Evaluate(option, new HealthStateSnapshot(40, maximum), 20, false, false);
            Assert.That(state.CanSelect, Is.False);
            Assert.That(state.UnavailableReason, Is.EqualTo(RecoveryFailureReason.NoEffect));
            Assert.That(state.ProjectedMaxHealthIncrease, Is.Zero);
        }

        [TestCase(true, false, RecoveryFailureReason.AlreadyResolved)]
        [TestCase(false, true, RecoveryFailureReason.StaleEntry)]
        public void ResolvedMaxHealthOption_CanBeProjectedAtTheIntegerMaximum(bool applied, bool completed, RecoveryFailureReason reason)
        {
            var option = new RecoveryOptionSpec(new RecoveryOptionId(4), 10, 10);
            var state = RecoveryOptionEvaluator.Evaluate(option, new HealthStateSnapshot(40, int.MaxValue), 10, applied, completed);
            Assert.That(state.UnavailableReason, Is.EqualTo(reason));
            Assert.That(state.ProjectedMaxHealthIncrease, Is.EqualTo(10));
        }

        [TestCase(40, 10, false, false, RecoveryFailureReason.None)]
        [TestCase(60, 10, false, false, RecoveryFailureReason.None)]
        [TestCase(60, 9, false, false, RecoveryFailureReason.InsufficientFunds)]
        [TestCase(60, 10, true, false, RecoveryFailureReason.AlreadyResolved)]
        [TestCase(60, 10, true, true, RecoveryFailureReason.StaleEntry)]
        public void MaxHealthOption_RemainsEffectiveAtFullHealthAndUsesExistingVisitPriority(int hp, int gold,
            bool applied, bool completed, RecoveryFailureReason expected)
        {
            var option = new RecoveryOptionSpec(new RecoveryOptionId(4), 10, 10);
            var result = RecoveryOptionEvaluator.Evaluate(option, new HealthStateSnapshot(hp, 60), gold, applied, completed);
            Assert.That(result.UnavailableReason, Is.EqualTo(expected));
            Assert.That(result.ProjectedHealAmount, Is.Zero);
            Assert.That(result.ProjectedMaxHealthIncrease, Is.EqualTo(10));
            Assert.That(result.CanSelect, Is.EqualTo(expected == RecoveryFailureReason.None));
        }

        [TestCase(5, 2, false, false, 3, RecoveryFailureReason.InsufficientFunds)]
        [TestCase(5, 3, false, false, 3, RecoveryFailureReason.None)]
        [TestCase(10, 0, false, false, 0, RecoveryFailureReason.NoEffect)]
        [TestCase(5, 0, true, false, 3, RecoveryFailureReason.AlreadyResolved)]
        [TestCase(10, 0, true, false, 0, RecoveryFailureReason.AlreadyResolved)]
        [TestCase(5, 3, false, true, 3, RecoveryFailureReason.StaleEntry)]
        [TestCase(10, 0, true, true, 0, RecoveryFailureReason.StaleEntry)]
        public void Evaluate_ProjectsHealAndUsesAvailabilityPriority(int hp, int gold,
            bool applied, bool completed, int expectedHeal, RecoveryFailureReason expectedReason)
        {
            var option = Option(RecoveryHealMode.Flat, 3, 3);
            var state = RecoveryOptionEvaluator.Evaluate(option, new HealthStateSnapshot(hp, 10), gold, applied, completed);
            Assert.That(state.Option, Is.SameAs(option));
            Assert.That(state.ProjectedHealAmount, Is.EqualTo(expectedHeal));
            Assert.That(state.UnavailableReason, Is.EqualTo(expectedReason));
            Assert.That(state.CanSelect, Is.EqualTo(expectedReason == RecoveryFailureReason.None));
        }

        [TestCase(RecoveryHealMode.Flat, 3, 0, 5, 10, 3, RecoveryFailureReason.None)]
        [TestCase(RecoveryHealMode.Flat, int.MaxValue, 0, 8, 10, 2, RecoveryFailureReason.None)]
        [TestCase(RecoveryHealMode.PercentOfMax, 25, 0, 1, 11, 2, RecoveryFailureReason.None)]
        [TestCase(RecoveryHealMode.PercentOfMax, 10, 0, 1, 9, 0, RecoveryFailureReason.NoEffect)]
        [TestCase(RecoveryHealMode.Full, 0, 0, 8, 10, 2, RecoveryFailureReason.None)]
        public void Evaluate_ReusesRoundingCapAndFreeSelection(RecoveryHealMode mode, int amount, int cost,
            int hp, int max, int expectedHeal, RecoveryFailureReason expectedReason)
        {
            var state = RecoveryOptionEvaluator.Evaluate(Option(mode, amount, cost),
                new HealthStateSnapshot(hp, max), 0, false, false);
            Assert.That(state.ProjectedHealAmount, Is.EqualTo(expectedHeal));
            Assert.That(state.UnavailableReason, Is.EqualTo(expectedReason));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Evaluate_DefaultOrDeadHealthIsFaultEvenAfterCompletion(bool useDefault)
        {
            var health = useDefault ? default : new HealthStateSnapshot(0, 10);
            Assert.Throws<InvalidOperationException>(() => RecoveryOptionEvaluator.Evaluate(
                Option(RecoveryHealMode.Full, 0, 0), health, 0, true, true));
        }

        [Test]
        public void Evaluate_NullOptionAndNegativeGoldAreFaults()
        {
            var health = new HealthStateSnapshot(5, 10);
            Assert.Throws<ArgumentNullException>(() => RecoveryOptionEvaluator.Evaluate(null, health, 0, false, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => RecoveryOptionEvaluator.Evaluate(
                Option(RecoveryHealMode.Flat, 3, 0), health, -1, true, true));
        }

        [Test]
        public void Plan_SoulAdjustedMaximumChangesRoundingWithoutChangingSource()
        {
            var main = new MainCharacterPersistentState(1, 9, 5);
            var option = Option(RecoveryHealMode.PercentOfMax, 10, 0);
            var source = new CharacterInstanceId(7);
            Assert.Throws<InvalidOperationException>(() => CharacterRecoveryCalculator.Plan(main, source, 0, option, 0));
            var plan = CharacterRecoveryCalculator.Plan(main, source, 1, option, 0);
            Assert.That(plan.HealedAmount, Is.EqualTo(1));
            Assert.That(plan.Characters.Main.Final.CurrentHealth, Is.EqualTo(2));
            Assert.That(plan.FinalBalance, Is.Zero);
            Assert.That(plan.Characters.Main.Expected, Is.SameAs(main));
            Assert.That(main.CurrentHealth, Is.EqualTo(1));
        }

        [Test]
        public void Plan_EffectiveMaximumOverflowRemainsFault()
        {
            var main = new MainCharacterPersistentState(1, int.MaxValue, 5);
            Assert.Throws<OverflowException>(() => CharacterRecoveryCalculator.Plan(main, new CharacterInstanceId(7),
                1, Option(RecoveryHealMode.Full, 0, 0), 0));
        }

        private static RecoveryOptionSpec Option(RecoveryHealMode mode, int amount, int cost)
        {
            return new RecoveryOptionSpec(new RecoveryOptionId(1), cost, new RecoveryHealSpec(mode, amount));
        }
    }
}
