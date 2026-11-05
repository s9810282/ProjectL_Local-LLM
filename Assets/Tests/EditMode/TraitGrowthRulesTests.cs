using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;

namespace ProjectL.Tests.Core
{
    public sealed class TraitGrowthRulesTests
    {
        [Test]
        public void NextStep_UsesConfiguredPrefixAndLevelWithoutSkipping()
        {
            var p = TraitGrowthFixtures.Profile();
            var c = TraitGrowthFixtures.Catalog(new[] { new TraitGrowthStepSpec(20, 3, TraitGrowthKind.Enhance), new TraitGrowthStepSpec(10, 3, TraitGrowthKind.Acquire) });
            Assert.That(TraitGrowthRules.NextStep(TraitGrowthFixtures.Owned(p, 2), c.GetCategory(CategoryKey.Heavy)), Is.Null);
            var next = TraitGrowthRules.NextStep(TraitGrowthFixtures.Owned(p, 3), c.GetCategory(CategoryKey.Heavy));
            Assert.That(next, Is.Not.Null);
            Assert.That(next.StepId, Is.EqualTo(20));
            var invalid = TraitGrowthFixtures.Owned(p, growth: new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.Knockback) }));
            Assert.Throws<InvalidOperationException>(() => TraitGrowthRules.NextStep(invalid, c.GetCategory(CategoryKey.Heavy)));
        }
        [Test]
        public void ValidatorAndRuntime_UseEquivalentEligibility()
        {
            var p = TraitGrowthFixtures.Profile(CategoryKey.Heavy, new KnockbackTraitSpec(1));
            var definitions = TraitGrowthFixtures.Definitions.Append(new TraitGrowthDefinition(new ThornFieldTraitSpec(5, 5), new ThornFieldTraitSpec(7, 5), "n", "d", "e", "ed")).ToArray();
            var c = TraitGrowthFixtures.Catalog(definitions: definitions);
            TraitGrowthCatalogValidator.Validate(c, new[] { p });
            var o = TraitGrowthFixtures.Owned(p);
            Assert.That(TraitGrowthRules.Candidates(o, p, c), Is.EqualTo(TraitGrowthRules.EligibleTraits(p,
                new[] { TraitKey.Knockback }, Array.Empty<TraitKey>(), TraitGrowthKind.Acquire, c.GetCategory(CategoryKey.Heavy), c)));
            Assert.That(TraitGrowthRules.Candidates(o, p, c), Is.EquivalentTo(new[] { TraitKey.SoloAction, TraitKey.HuntersMark,
                TraitKey.Vanguard, TraitKey.TrackingShot, TraitKey.GroundCompaction }));
            var growth = new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, TraitKey.SoloAction) });
            o = TraitGrowthFixtures.Owned(p, growth: growth);
            Assert.That(TraitGrowthRules.Candidates(o, p, c), Is.EqualTo(new[] { TraitKey.Knockback, TraitKey.SoloAction }.OrderBy(k => k)));
            var next = TraitGrowthFixtures.Owned(p, growth: growth.Append(new TraitGrowthSelection(20, TraitKey.Knockback)));
            Assert.That(TraitGrowthRules.Candidates(next, p, c), Is.Empty);
            Assert.That(TraitGrowthRules.EligibleTraits(p, new[] { TraitKey.Knockback }, new[] { TraitKey.Knockback }, TraitGrowthKind.Enhance,
                c.GetCategory(CategoryKey.Heavy), c), Is.Empty);
        }
        [Test]
        public void FrozenOffer_RejectsAValidKeyNotPresented()
        {
            var p = TraitGrowthFixtures.Profile();
            var c = TraitGrowthFixtures.Catalog();
            var o = TraitGrowthFixtures.Owned(p);
            var f = TraitGrowthFixtures.Offer(o, c.GetCategory(CategoryKey.Heavy).Steps[0], TraitKey.Knockback, TraitKey.SoloAction, TraitKey.HuntersMark);
            var rejected = TraitGrowthRules.PlanChoice(o, TraitGrowthFixtures.Request(f, TraitKey.Vanguard), f, p, c);
            Assert.That(rejected.FailureReason, Is.EqualTo(TraitGrowthFailureReason.NotCandidate));
            Assert.That(rejected.Change, Is.Null);
            var selected = TraitGrowthRules.PlanChoice(o, TraitGrowthFixtures.Request(f, TraitKey.Knockback), f, p, c);
            Assert.That(selected.Succeeded, Is.True);
            Assert.That(o.Growth.IsEmpty, Is.True);
            Assert.That(TraitGrowthRules.PlanChoice(selected.Change.Final, TraitGrowthFixtures.Request(f, TraitKey.Knockback), f, p, c).Succeeded, Is.False);
        }
    }
}
