using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class OwnedTraitGrowthTests
    {
        [Test]
        public void Purchases_AreIndependentOfLevelChoices()
        {
            var paid = OwnedTraitGrowth.Empty.WithPurchase(TraitKey.Vanguard);
            Assert.That(paid.IsEmpty, Is.False);
            Assert.That(paid.Selections, Is.Empty);
            Assert.That(paid.SameValues(OwnedTraitGrowth.Empty), Is.False);
            var selected = paid.Append(new TraitGrowthSelection(10, TraitKey.SoloAction));
            Assert.That(selected.Purchases[TraitKey.Vanguard], Is.EqualTo(1));
            Assert.That(selected.WithPurchase(TraitKey.Vanguard).Selections.Single().StepId, Is.EqualTo(10));
            Assert.Throws<ArgumentException>(() => selected.WithPurchase(TraitKey.Vanguard).WithPurchase(TraitKey.Vanguard));
        }

        [Test]
        public void OldEnhancementAndOnePurchaseReachRankThreeOnOnlyOneCopy()
        {
            var key = TraitKey.Vanguard;
            var definition = new TraitGrowthDefinition(new VanguardTraitSpec(1, 1), new VanguardTraitSpec(2, 2),
                "", "", "", "", new TraitRankValues(key, 4), new TraitRankValues(key, 9));
            var catalog = TraitGrowthFixtures.Catalog(definitions: new[] { definition });
            var profile = TraitGrowthFixtures.Profile();
            var acquired = new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, key) });
            var legacy = acquired.Append(new TraitGrowthSelection(20, key));
            var first = TraitGrowthFixtures.Owned(profile, growth: legacy);
            var second = TraitGrowthFixtures.Owned(profile, growth: acquired, id: 3);
            int Bonus(OwnedCharacterRecord record) => ((VanguardTraitSpec)OwnedUnitSpecResolver.Resolve(
                profile.Definition, record, catalog).Traits.Span[0]).AttackBonus;
            var capped = first.WithGrowth(acquired.WithPurchase(key).WithPurchase(key));
            Assert.That(TraitGrowthRules.Candidates(capped, profile, catalog), Has.No.Member(key));
            var neverEnhanced = new TraitGrowthDefinition(new VanguardTraitSpec(1, 1), null, "", "", "", "",
                new TraitRankValues(key, 4), new TraitRankValues(key, 9));
            Assert.Throws<InvalidOperationException>(() => OwnedUnitSpecResolver.Resolve(profile.Definition, first,
                TraitGrowthFixtures.Catalog(definitions: new[] { neverEnhanced })));
            Assert.That(Bonus(first), Is.EqualTo(4));
            var trained = first.WithGrowth(legacy.WithPurchase(key));
            Assert.That(Bonus(trained), Is.EqualTo(9));
            Assert.That(Bonus(second), Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => Bonus(trained.WithGrowth(trained.Growth.WithPurchase(key))));
            Assert.Throws<InvalidOperationException>(() => Bonus(second.WithGrowth(OwnedTraitGrowth.Empty.WithPurchase(key))));
        }

        [Test]
        public void History_IsDefensiveOrderedAndRejectsDuplicateSteps()
        {
            var items = new[] { new TraitGrowthSelection(20, TraitKey.Knockback), new TraitGrowthSelection(10, TraitKey.SoloAction) };
            var history = new OwnedTraitGrowth(items);
            items[0] = new TraitGrowthSelection(30, TraitKey.TrackingShot);
            Assert.That(history.Selections.Select(s => s.StepId), Is.EqualTo(new[] { 20, 10 }));
            Assert.Throws<NotSupportedException>(() => ((IList<TraitGrowthSelection>)history.Selections)[0] = items[0]);
            Assert.Throws<ArgumentException>(() => history.Append(new TraitGrowthSelection(20, TraitKey.Knockback)));
            Assert.Throws<ArgumentException>(() => new OwnedTraitGrowth(new TraitGrowthSelection[] { null }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TraitGrowthSelection(0, TraitKey.Knockback));
            Assert.Throws<ArgumentOutOfRangeException>(() => new TraitGrowthSelection(1, TraitKey.None));
            Assert.That(history.SameValues(new OwnedTraitGrowth(history.Selections.ToArray())), Is.True);
            Assert.That(history.SameValues(new OwnedTraitGrowth(history.Selections.Reverse().ToArray())), Is.False);
        }
        [Test]
        public void PurchaseCopy_DefendsCountsAndPreservesSpecializationAndHunterProgress()
        {
            var source = new Dictionary<TraitKey, int> { [TraitKey.Vanguard] = 1 };
            var paid = new OwnedTraitGrowth(Array.Empty<TraitGrowthSelection>(), source);
            source.Clear();
            var before = new OwnedCharacterRecord(new CharacterInstanceId(8), EntityKey.Unit_Akimbo,
                CharacterOwnershipRole.Main, 5, 12, hunterAttackBonus: 6, specializationCategory: CategoryKey.Heavy);
            var after = before.WithGrowth(paid);
            Assert.That((after.Id, after.Level, after.Experience, after.HunterAttackBonus, after.SpecializationCategory),
                Is.EqualTo((before.Id, 5, 12, 6, (CategoryKey?)CategoryKey.Heavy)));
            Assert.That(after.Growth.Purchases[TraitKey.Vanguard], Is.EqualTo(1));
            var gain = ExperienceRules.Gain(after, 1000, new LevelProgressionSpec(100, 100, 3, 1, 1));
            Assert.That(gain.Final.Level, Is.GreaterThan(after.Level));
            Assert.That(gain.Final.SameGrowth(after), Is.True);
        }

        [Test]
        public void MainRecord_AcceptsHistoryAndPreservesOwnershipInvariants()
        {
            var p = TraitGrowthFixtures.Profile(CategoryKey.Main);
            var history = new OwnedTraitGrowth(new[] { new TraitGrowthSelection(1, TraitKey.Knockback) });
            Assert.That(TraitGrowthFixtures.Owned(p, growth: history).Growth, Is.SameAs(history));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OwnedCharacterRecord(new CharacterInstanceId(1), p.Definition.Key, (CharacterOwnershipRole)99));
            Assert.Throws<ArgumentException>(() => new OwnedCharacterRecord(default, p.Definition.Key, CharacterOwnershipRole.Main));
        }
    }
}
