using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class MainCharacterHealthCalculatorTests
    {
        [TestCase(18, 30, 2, 18, 32)]
        [TestCase(0, 30, 1, 0, 31)]
        [TestCase(30, 30, 0, 30, 30)]
        public void Calculate_KeepsPersistentCurrentAndAddsSoulsToMax(int current, int persistentMax, int souls,
            int expectedCurrent, int expectedMax)
        {
            var main = new MainCharacterPersistentState(current, persistentMax, 15);

            var health = MainCharacterHealthCalculator.Calculate(main, souls);

            Assert.That((health.CurrentHealth, health.MaxHealth), Is.EqualTo((expectedCurrent, expectedMax)));
            var rule = CharacterHealthRules.PersistentMainHealth(main, souls);
            Assert.That((health.CurrentHealth, health.MaxHealth), Is.EqualTo((rule.CurrentHealth, rule.MaxHealth)));
        }

        [Test]
        public void InvalidInputs_AreRejected()
        {
            Assert.Throws<ArgumentNullException>(() => MainCharacterHealthCalculator.Calculate(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MainCharacterHealthCalculator.Calculate(new MainCharacterPersistentState(10, 10, 5), -1));
            Assert.Throws<OverflowException>(() => MainCharacterHealthCalculator.Calculate(
                new MainCharacterPersistentState(10, int.MaxValue, 5), 1));
            Assert.Throws<InvalidOperationException>(() => MainCharacterHealthCalculator.Calculate(
                new MainCharacterPersistentState(12, 10, 5), 1));
        }
    }
}
