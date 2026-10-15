using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class CombatEffectOriginTests
    {
        [Test]
        public void Origin_DistinguishesEveryIdentityField()
        {
            var baseline = new EffectOriginKey(EffectOwnerKind.Trait, 0, 5, "entry-A");
            var same = new EffectOriginKey(EffectOwnerKind.Trait, 0, 5, "entry-A");

            Assert.That(baseline, Is.EqualTo(same));
            Assert.That(baseline.GetHashCode(), Is.EqualTo(same.GetHashCode()));
            Assert.That(baseline == same, Is.True);
            Assert.That(baseline, Is.Not.EqualTo(
                new EffectOriginKey(EffectOwnerKind.Category, 0, 5, "entry-A")));
            Assert.That(baseline, Is.Not.EqualTo(
                new EffectOriginKey(EffectOwnerKind.Trait, 1, 5, "entry-A")));
            Assert.That(baseline, Is.Not.EqualTo(
                new EffectOriginKey(EffectOwnerKind.Trait, 0, 6, "entry-A")));
            Assert.That(baseline, Is.Not.EqualTo(
                new EffectOriginKey(EffectOwnerKind.Trait, 0, 5, "entry-B")));
            Assert.That(baseline, Is.Not.EqualTo(
                new EffectOriginKey(EffectOwnerKind.Trait, 0, 5, "ENTRY-A")));
            Assert.That(baseline, Is.Not.EqualTo(
                new EffectOriginKey(EffectOwnerKind.Trait, 0, 5, " entry-A ")));
        }

        [Test]
        public void CompareTo_OrdersByKindOwnerDefinitionThenOrdinalRuleKey()
        {
            var expected = new[]
            {
                new EffectOriginKey(EffectOwnerKind.Trait, 0, 5, "A"),
                new EffectOriginKey(EffectOwnerKind.Trait, 0, 5, "a"),
                new EffectOriginKey(EffectOwnerKind.Trait, 0, 6, "A"),
                new EffectOriginKey(EffectOwnerKind.Trait, 1, 0, "A"),
                new EffectOriginKey(EffectOwnerKind.Category, 0, 0, "A"),
                new EffectOriginKey(EffectOwnerKind.Artifact, 0, 0, "A"),
            };
            var shuffled = new List<EffectOriginKey>
            {
                expected[5],
                expected[3],
                expected[1],
                expected[4],
                expected[2],
                expected[0],
            };

            shuffled.Sort();

            Assert.That(shuffled, Is.EqualTo(expected));
        }

        [TestCase(EffectOwnerKind.None)]
        [TestCase((EffectOwnerKind)999)]
        public void Constructor_RejectsInvalidOwnerKind(EffectOwnerKind kind)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EffectOriginKey(kind, 0, 0, "entry-A"));
        }

        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        public void Constructor_RejectsNegativeNumericIdentity(int ownerInstanceId, int definitionKey)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EffectOriginKey(EffectOwnerKind.Artifact, ownerInstanceId, definitionKey, "entry-A"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Constructor_RejectsMissingRuleKey(string ruleKey)
        {
            Assert.Throws<ArgumentException>(() =>
                new EffectOriginKey(EffectOwnerKind.Artifact, 0, 0, ruleKey));
        }

        [Test]
        public void DefaultValue_IsInvalidButComparisonOperationsRemainSafe()
        {
            EffectOriginKey empty = default;
            var valid = new EffectOriginKey(EffectOwnerKind.Artifact, 0, 0, "entry-A");

            Assert.That(empty.IsValid, Is.False);
            Assert.DoesNotThrow(() => empty.Equals(default(EffectOriginKey)));
            Assert.DoesNotThrow(() => empty.GetHashCode());
            Assert.DoesNotThrow(() => empty.CompareTo(valid));
            Assert.DoesNotThrow(() => valid.CompareTo(empty));
            Assert.DoesNotThrow(() => empty.CompareTo(default(EffectOriginKey)));
        }
    }
}
