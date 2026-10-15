using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterRecoveryRuleTests
    {
        [TestCase(RecoveryHealMode.Full, int.MinValue, 75, 13, 20, 7)]
        [TestCase(RecoveryHealMode.Flat, 3, 0, 13, 20, 3)]
        [TestCase(RecoveryHealMode.Flat, int.MaxValue, 10, 13, 20, 7)]
        [TestCase(RecoveryHealMode.PercentOfMax, 10, 10, 13, 25, 2)]
        [TestCase(RecoveryHealMode.PercentOfMax, 100, 10, 1, int.MaxValue, int.MaxValue - 1)]
        public void Recovery_PlansBoundedHealingAndPaymentWithoutMutatingInput(
            RecoveryHealMode mode, int amount, int cost, int hp, int max, int healed)
        {
            var before = new MainCharacterPersistentState(hp, max, 5, 10, 2, 1);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), cost, new RecoveryHealSpec(mode, amount));
            var plan = CharacterRecoveryCalculator.Plan(before, new CharacterInstanceId(1), 0, option, 75);
            Assert.That(plan.HealedAmount, Is.EqualTo(healed));
            Assert.That(plan.FinalBalance, Is.EqualTo(75 - cost));
            Assert.That(plan.Characters.Main.Final.CurrentHealth, Is.EqualTo(hp + healed));
            Assert.That(plan.Characters.Main.Expected, Is.SameAs(before));
            Assert.That(before.CurrentHealth, Is.EqualTo(hp));
        }

        [TestCase(13, 20, 2, 1, 3)]
        [TestCase(20, 20, 10, 1, 3)]
        [TestCase(13, 20, 10, 0, 0)]
        [TestCase(0, 20, 10, 1, 3)]
        public void Recovery_RejectsInsufficientFundsNoEffectAndDeath(int hp, int max, int balance, int amount, int cost)
        {
            var before = new MainCharacterPersistentState(hp, max, 5, 10, 2, 1);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), cost,
                new RecoveryHealSpec(RecoveryHealMode.Flat, amount));
            Assert.Throws<InvalidOperationException>(() => CharacterRecoveryCalculator.Plan(before, new CharacterInstanceId(1), 0, option, balance));
            Assert.That(before.CurrentHealth, Is.EqualTo(hp));
        }

        [Test]
        public void Recovery_PercentRoundingToZeroRejects_AndSoulsContributeToCurrentMaximum()
        {
            var before = new MainCharacterPersistentState(1, 9, 5, 10, 2, 1);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), 0,
                new RecoveryHealSpec(RecoveryHealMode.PercentOfMax, 10));
            Assert.Throws<InvalidOperationException>(() => CharacterRecoveryCalculator.Plan(before, new CharacterInstanceId(1), 0, option, 0));
            Assert.That(CharacterRecoveryCalculator.Plan(before, new CharacterInstanceId(1), 1, option, 0).HealedAmount, Is.EqualTo(1));
            Assert.That(before.CurrentHealth, Is.EqualTo(1));
        }
    }
}
