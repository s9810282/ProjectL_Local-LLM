using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterHealthRulesTests
    {
        [Test]
        public void Health_WithoutPersistentCurrent_StartsAtSoulAdjustedMax()
        {
            var health = CharacterHealthRules.Health(20, 3, null);

            Assert.That((health.CurrentHealth, health.MaxHealth), Is.EqualTo((23, 23)));
        }

        [Test]
        public void PersistentMainHealth_KeepsCurrentAndAddsSoulsToPersistentMax()
        {
            var main = new MainCharacterPersistentState(18, 30, 15);

            var health = CharacterHealthRules.PersistentMainHealth(main, 2);

            Assert.That((health.CurrentHealth, health.MaxHealth), Is.EqualTo((18, 32)));
        }

        [Test]
        public void Health_AllowsZeroCurrentHealth()
        {
            var health = CharacterHealthRules.Health(30, 0, 0);

            Assert.That((health.CurrentHealth, health.MaxHealth), Is.EqualTo((0, 30)));
        }

        [Test]
        public void SoulMaxHealthBonus_UsesSoulGrowthRule()
        {
            Assert.That(CharacterHealthRules.SoulMaxHealthBonus(4),
                Is.EqualTo(4 * SoulGrowthRules.MaxHealthBonusPerSoul));
        }

        [Test]
        public void InvalidInputs_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CharacterHealthRules.SoulMaxHealthBonus(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => CharacterHealthRules.Health(0, 0, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => CharacterHealthRules.Health(10, -1, null));
            Assert.Throws<InvalidOperationException>(() => CharacterHealthRules.Health(10, 0, 11));
            Assert.Throws<InvalidOperationException>(() => CharacterHealthRules.Health(10, 0, -1));
            Assert.Throws<OverflowException>(() => CharacterHealthRules.Health(int.MaxValue, 1, null));
            Assert.Throws<ArgumentNullException>(() => CharacterHealthRules.PersistentMainHealth(null, 0));
        }
    }
}
