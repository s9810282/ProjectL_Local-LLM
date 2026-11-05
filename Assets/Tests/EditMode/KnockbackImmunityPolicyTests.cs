using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class KnockbackImmunityPolicyTests
    {
        [TestCase(false, false)]
        [TestCase(true, true)]
        public void HeavyRule_CanBeRestoredWhileExplicitImmunityAlwaysWins(bool enabled, bool expected)
        {
            Assert.That(KnockbackImmunityPolicy.IsImmune(CategoryKey.Heavy, false, enabled), Is.EqualTo(expected));
            foreach (CategoryKey category in System.Enum.GetValues(typeof(CategoryKey)))
            {
                Assert.That(KnockbackImmunityPolicy.IsImmune(category, true, enabled), Is.True);
                if (category != CategoryKey.Heavy)
                    Assert.That(KnockbackImmunityPolicy.IsImmune(category, false, enabled), Is.False);
            }
        }

        [TestCase(CategoryKey.Main, false, false)]
        [TestCase(CategoryKey.Main, true, true)]
        [TestCase(CategoryKey.Rider, false, false)]
        [TestCase(CategoryKey.Rider, true, true)]
        [TestCase(CategoryKey.Heavy, false, false)]
        [TestCase(CategoryKey.Heavy, true, true)]
        [TestCase(CategoryKey.Cannon, false, false)]
        [TestCase(CategoryKey.Cannon, true, true)]
        [TestCase(CategoryKey.Boss, false, false)]
        [TestCase(CategoryKey.Boss, true, true)]
        public void IsImmune_MatchesCategoryAndExplicitImmunity(
            CategoryKey category, bool explicitImmunity, bool expected)
        {
            Assert.That(KnockbackImmunityPolicy.IsImmune(category, explicitImmunity), Is.EqualTo(expected));
        }
    }
}
