using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
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
        [TestCase("health")]
        [TestCase("maximum")]
        [TestCase("increase")]
        [TestCase("attack")]
        [TestCase("defense")]
        [TestCase("owned")]
        public void GrowthDecision_RejectsAnUnrelatedOrIncorrectDelta(string invalid)
        {
            var before = new MainCharacterPersistentState(40, 60, 7, 9);
            var after = before.With(currentHealth: invalid == "health" ? 41 : 40,
                persistentMaxHealth: invalid == "maximum" ? 69 : 70,
                persistentAttack: invalid == "attack" ? 8 : 7, persistentDefense: invalid == "defense" ? 10 : 9);
            var unrelated = new OwnedCharacterRecord(new CharacterInstanceId(2), EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub);
            var changes = new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(new CharacterInstanceId(1), before, after),
                owned: invalid == "owned" ? new[] { new OwnedCharacterChange(unrelated,
                    new OwnedCharacterRecord(unrelated.Id, unrelated.CharacterKey, unrelated.Role, 2)) } : null);
            Assert.Throws<ArgumentException>(() => new CharacterRecoveryDecision(new RecoveryOptionSpec(new RecoveryOptionId(4), 10, 10),
                new CharacterInstanceId(1), 0, 20, 10, invalid == "health" ? 1 : 0, changes, invalid == "increase" ? 9 : 10));
        }

        [TestCase(40, 60, 0, 40, 70)]
        [TestCase(60, 60, 0, 60, 70)]
        [TestCase(70, 60, 10, 70, 70)]
        public void MaxHealthPurchase_PreservesCurrentHealthSoulBonusesAndOtherStats(int hp, int maximum,
            int souls, int finalHp, int finalPersistentMaximum)
        {
            var before = new MainCharacterPersistentState(hp, maximum, 7, 9);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(4), 10, 10);
            var plan = CharacterRecoveryCalculator.Plan(before, new CharacterInstanceId(1), souls, option, 20);
            Assert.That(plan.HealedAmount, Is.Zero);
            Assert.That(plan.FinalBalance, Is.EqualTo(10));
            Assert.That(plan.Characters.Main.Final.CurrentHealth, Is.EqualTo(finalHp));
            Assert.That(plan.Characters.Main.Final.PersistentMaxHealth, Is.EqualTo(finalPersistentMaximum));
            Assert.That(plan.Characters.Main.Final.PersistentAttack, Is.EqualTo(7));
            Assert.That(plan.Characters.Main.Final.PersistentDefense, Is.EqualTo(9));
            Assert.That(plan.SoulCount, Is.EqualTo(souls));
            Assert.That(plan.Characters.Main.Expected, Is.SameAs(before));
            Assert.That(before.PersistentMaxHealth, Is.EqualTo(maximum));
        }

        [Test]
        public void RestHeal_UsesExistingFloorAndMissingHealthCap()
        {
            var option = new RecoveryOptionSpec(new RecoveryOptionId(2), 5, new RecoveryHealSpec(RecoveryHealMode.PercentOfMax, 30));
            var plan = CharacterRecoveryCalculator.Plan(new MainCharacterPersistentState(40, 60, 7, 9), new CharacterInstanceId(1), 0, option, 20);
            Assert.That(plan.Characters.Main.Final.CurrentHealth, Is.EqualTo(58));
            Assert.That(plan.Characters.Main.Final.PersistentMaxHealth, Is.EqualTo(60));
            Assert.That(plan.FinalBalance, Is.EqualTo(15));
            Assert.That(CharacterRecoveryCalculator.Plan(new MainCharacterPersistentState(59, 60, 7), new CharacterInstanceId(1), 0, option, 20).HealedAmount, Is.EqualTo(1));
        }

        [Test]
        public void MaxHealthPurchase_RejectsUnaffordableOrOverflowingGrowthWithoutChangingSource()
        {
            var option = new RecoveryOptionSpec(new RecoveryOptionId(4), 10, 10);
            var main = new MainCharacterPersistentState(40, 60, 7);
            Assert.Throws<InvalidOperationException>(() => CharacterRecoveryCalculator.Plan(main, new CharacterInstanceId(1), 0, option, 9));
            Assert.That(main.PersistentMaxHealth, Is.EqualTo(60));
            var capped = new MainCharacterPersistentState(40, int.MaxValue - 5, 7);
            Assert.Throws<InvalidOperationException>(() => CharacterRecoveryCalculator.Plan(capped, new CharacterInstanceId(1), 0, option, 20));
            Assert.That(capped.PersistentMaxHealth, Is.EqualTo(int.MaxValue - 5));
        }

        [TestCase(RecoveryHealMode.Full, int.MinValue, 75, 13, 20, 7)]
        [TestCase(RecoveryHealMode.Flat, 3, 0, 13, 20, 3)]
        [TestCase(RecoveryHealMode.Flat, int.MaxValue, 10, 13, 20, 7)]
        [TestCase(RecoveryHealMode.PercentOfMax, 10, 10, 13, 25, 2)]
        [TestCase(RecoveryHealMode.PercentOfMax, 100, 10, 1, int.MaxValue, int.MaxValue - 1)]
        public void Recovery_PlansBoundedHealingAndPaymentWithoutMutatingInput(
            RecoveryHealMode mode, int amount, int cost, int hp, int max, int healed)
        {
            var before = new MainCharacterPersistentState(hp, max, 5);
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
            var before = new MainCharacterPersistentState(hp, max, 5);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), cost,
                new RecoveryHealSpec(RecoveryHealMode.Flat, amount));
            Assert.Throws<InvalidOperationException>(() => CharacterRecoveryCalculator.Plan(before, new CharacterInstanceId(1), 0, option, balance));
            Assert.That(before.CurrentHealth, Is.EqualTo(hp));
        }

        [Test]
        public void Recovery_PercentRoundingToZeroRejects_AndSoulsContributeToCurrentMaximum()
        {
            var before = new MainCharacterPersistentState(1, 9, 5);
            var option = new RecoveryOptionSpec(new RecoveryOptionId(1), 0,
                new RecoveryHealSpec(RecoveryHealMode.PercentOfMax, 10));
            Assert.Throws<InvalidOperationException>(() => CharacterRecoveryCalculator.Plan(before, new CharacterInstanceId(1), 0, option, 0));
            Assert.That(CharacterRecoveryCalculator.Plan(before, new CharacterInstanceId(1), 1, option, 0).HealedAmount, Is.EqualTo(1));
            Assert.That(before.CurrentHealth, Is.EqualTo(1));
        }
    }
}
