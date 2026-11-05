using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;

namespace ProjectL.Tests.Core
{
    public sealed class TraitGrowthOfferTests
    {
        [Test]
        public void SelectionHistory_AllowsFourAcquisitionsAndTwoEnhancements()
        {
            var p = TraitGrowthFixtures.Profile(CategoryKey.Heavy, new FocusTraitSpec(1, 3));
            var steps = Enumerable.Range(1, 6).Select(i => new TraitGrowthStepSpec(i, 2, i < 5 ? TraitGrowthKind.Acquire : TraitGrowthKind.Enhance)).ToArray();
            var c = TraitGrowthFixtures.Catalog(steps);
            TraitGrowthCatalogValidator.Validate(c, new[] { p });
            var o = TraitGrowthFixtures.Owned(p);
            var initial = o;
            var keys = new[] { TraitKey.Knockback, TraitKey.SoloAction, TraitKey.HuntersMark, TraitKey.Vanguard, TraitKey.Knockback, TraitKey.SoloAction };
            foreach (var k in keys)
            {
                var step = TraitGrowthRules.NextStep(o, c.GetCategory(CategoryKey.Heavy));
                Assert.That(step, Is.Not.Null, "Reached configured step must be admitted.");
                var choices = TraitGrowthRules.Candidates(o, p, c);
                var shown = step.Kind == TraitGrowthKind.Acquire ? choices.Where(v => v != k).Take(2).Prepend(k).ToArray() : choices.Take(3).ToArray();
                if (step.Kind == TraitGrowthKind.Acquire)
                {
                    Assert.That(shown.Length, Is.EqualTo(3));
                    Assert.That(shown.Distinct().Count(), Is.EqualTo(3));
                }
                var f = TraitGrowthFixtures.Offer(o, step, shown);
                var decision = TraitGrowthRules.PlanChoice(o, TraitGrowthFixtures.Request(f, k), f, p, c);
                Assert.That(decision.Succeeded, Is.True);
                o = decision.Change.Final;
            }
            var traits = OwnedUnitSpecResolver.Resolve(p.Definition, o, c).Traits.ToArray();
            Assert.That(o.Growth.Selections.Count, Is.EqualTo(6));
            Assert.That(traits.Length, Is.EqualTo(5));
            Assert.That(((KnockbackTraitSpec)traits.Single(t => t.TraitKey == TraitKey.Knockback)).DistanceCells, Is.EqualTo(3));
            Assert.That(((SoloActionTraitSpec)traits.Single(t => t.TraitKey == TraitKey.SoloAction)).ShieldAmount, Is.EqualTo(3));
            Assert.That(traits[0], Is.SameAs(p.Definition.Traits.Span[0]));
            Assert.That((o.Id, o.Level, o.Experience), Is.EqualTo((initial.Id, 8, 7)));
            Assert.That(initial.Growth.IsEmpty, Is.True);
        }
        [Test]
        public void Sampler_IsDeterministicDistinctSortedAndUsesKindCounts()
        {
            var keys = TraitGrowthFixtures.Definitions.Select(d => d.Key).ToArray();
            var sample = TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, 42);
            Assert.That(sample.Count, Is.EqualTo(3));
            Assert.That(sample.Distinct().Count(), Is.EqualTo(3));
            Assert.That(sample, Is.Ordered);
            Assert.That(TraitGrowthOfferSampler.Sample(keys.Reverse().ToArray(), TraitGrowthKind.Acquire, 42), Is.EqualTo(sample));
            Assert.That(TraitGrowthOfferSampler.Sample(keys.Take(2).ToArray(), TraitGrowthKind.Enhance, 4).Count, Is.EqualTo(2));
            Assert.That(TraitGrowthOfferSampler.Sample(keys.Take(2).ToArray(), TraitGrowthKind.Acquire, 4).Count, Is.EqualTo(2));
            var offered = Enumerable.Range(1, 200).SelectMany(seed => TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, seed)).Distinct();
            Assert.That(offered, Is.EquivalentTo(keys));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 3)]
        public void AcquireOffer_PresentsAllAvailableUpToThreeAndRejectsZero(int available, int expected)
        {
            var keys = TraitGrowthFixtures.Definitions.Take(available).Select(d => d.Key).ToArray();
            if (available == 0)
            {
                Assert.Throws<InvalidOperationException>(() => TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, 42));
                return;
            }
            var sample = TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, 42);
            Assert.That(sample.Count, Is.EqualTo(expected));
            Assert.That(sample.Distinct().Count(), Is.EqualTo(expected));
            Assert.That(sample, Is.Ordered);
            Assert.That(TraitGrowthOfferSampler.Sample(keys.Reverse().ToArray(), TraitGrowthKind.Acquire, 42), Is.EqualTo(sample));
            var profile = TraitGrowthFixtures.Profile();
            var owned = TraitGrowthFixtures.Owned(profile);
            var offer = TraitGrowthFixtures.Offer(owned, new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire), sample.ToArray());
            Assert.That(offer.Choices.Select(c => c.TraitKey), Is.EqualTo(sample));
            Assert.That(owned.Growth.IsEmpty, Is.True);
        }
    }
}
