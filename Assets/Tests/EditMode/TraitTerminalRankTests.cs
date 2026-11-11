using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class TraitTerminalRankTests
    {
        [Test]
        public void Rank2OnlyStopsAtTwo()
        {
            var definition = Vanguard(4);
            Assert.That(definition.IsTrainable, Is.True);
            Assert.That(definition.MaxRank, Is.EqualTo(2));
            Assert.That(definition.CanAdvanceFrom(1), Is.True);
            Assert.That(definition.CanAdvanceFrom(2), Is.False);
            Assert.That(definition.CanAdvanceFrom(3), Is.False);
            Assert.That(AttackAt(definition, 1), Is.EqualTo(1));
            Assert.That(AttackAt(definition, 2), Is.EqualTo(4));
        }

        [Test]
        public void ExistingThreeRanksRemainThree()
        {
            var definition = Vanguard(4, 9);
            Assert.That(definition.MaxRank, Is.EqualTo(3));
            Assert.That(definition.CanAdvanceFrom(2), Is.True);
            Assert.That(definition.CanAdvanceFrom(3), Is.False);
            Assert.That(AttackAt(definition, 1), Is.EqualTo(1));
            Assert.That(AttackAt(definition, 2), Is.EqualTo(4));
            Assert.That(AttackAt(definition, 3), Is.EqualTo(9));
        }

        [Test]
        public void NoRanksRetainsLegacyEnhanced()
        {
            var definition = Vanguard(enhanced: new VanguardTraitSpec(4, 2));
            var profile = TraitGrowthFixtures.Profile(native: new[] { definition.Normal });
            var catalog = EnhanceCatalog(definition);
            var owned = TraitGrowthFixtures.Owned(profile);

            Assert.That(definition.IsTrainable, Is.False);
            Assert.That(definition.MaxRank, Is.EqualTo(1));
            Assert.That(definition.CanAdvanceFrom(1), Is.False);
            Assert.That(TraitGrowthRules.Candidates(owned, profile, catalog), Is.EqualTo(new[] { definition.Key }));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void CanAdvanceFromRejectsUnsupportedRank(int rank)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Vanguard(4).CanAdvanceFrom(rank));
        }

        [Test]
        public void Rank3WithoutRank2IsRejected()
        {
            Assert.Throws<ArgumentException>(() => Vanguard(rank3: 9));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MismatchedRankKeyIsRejected(bool third)
        {
            var wrong = new TraitRankValues(TraitKey.Hunter, 4);
            Assert.Throws<ArgumentException>(() => new TraitGrowthDefinition(new VanguardTraitSpec(1, 1), null,
                "", "", "", "", third ? new TraitRankValues(TraitKey.Vanguard, 4) : wrong,
                third ? wrong : new TraitRankValues(TraitKey.Vanguard, 9)));
        }

        [Test]
        public void DuplicateRank2IsRejected()
        {
            Assert.Throws<ArgumentException>(() => Vanguard(1, 9));
        }

        [Test]
        public void DuplicateRank3IsRejected()
        {
            Assert.Throws<ArgumentException>(() => Vanguard(4, 4));
        }

        [Test]
        public void UnavailableRankFailsWithTraitAndRank()
        {
            var definition = Vanguard(4);
            var error = Assert.Throws<InvalidOperationException>(() => AttackAt(definition, 3));
            Assert.That(error.Message, Does.Contain(nameof(TraitKey.Vanguard)).And.Contain("Rank=3"));
        }

        [Test]
        public void OverCapPurchaseHistoryIsRejected()
        {
            var error = Assert.Throws<InvalidOperationException>(() => AttackAt(Vanguard(4), 3));
            Assert.That(error.Message, Does.Contain("Vanguard").And.Contain("Rank=3").And.Contain("MaxRank=2"));
        }

        [Test]
        public void LegacyEnhancementPlusPurchaseRespectsCap()
        {
            var definition = Vanguard(4, enhanced: new VanguardTraitSpec(7, 1));
            var profile = TraitGrowthFixtures.Profile(native: new[] { definition.Normal });
            var catalog = EnhanceCatalog(definition);
            var fresh = TraitGrowthFixtures.Owned(profile);
            var paid = OwnedTraitGrowth.Empty.WithPurchase(definition.Key);
            var enhanced = OwnedTraitGrowth.Empty.Append(new TraitGrowthSelection(20, definition.Key));
            var owned = fresh.WithGrowth(enhanced);

            Assert.That(TraitGrowthRules.Candidates(fresh, profile, catalog), Is.EqualTo(new[] { definition.Key }));
            Assert.That(TraitGrowthRules.Candidates(fresh.WithGrowth(paid), profile, catalog), Is.Empty);
            Assert.That(TraitGrowthRules.Candidates(owned, profile, catalog), Is.Empty);
            Assert.That(((VanguardTraitSpec)OwnedUnitSpecResolver.Resolve(profile.Definition, owned, catalog)
                .Traits.Span[0]).AttackBonus, Is.EqualTo(4));
            var overCap = owned.WithGrowth(enhanced.WithPurchase(definition.Key));
            var error = Assert.Throws<InvalidOperationException>(() => OwnedUnitSpecResolver.Resolve(profile.Definition, overCap, catalog));
            Assert.That(error.Message, Does.Contain("Vanguard").And.Contain("Rank=3").And.Contain("MaxRank=2"));
        }

        [Test]
        public void UnsupportedLegacyEnhancementStillResolves()
        {
            var definition = new TraitGrowthDefinition(new KnockbackTraitSpec(1), new KnockbackTraitSpec(3), "", "", "", "");
            var profile = TraitGrowthFixtures.Profile(native: new[] { definition.Normal });
            var catalog = EnhanceCatalog(definition);
            var owned = TraitGrowthFixtures.Owned(profile);

            Assert.That(TraitGrowthRules.Candidates(owned, profile, catalog), Is.EqualTo(new[] { definition.Key }));
            var enhanced = owned.WithGrowth(owned.Growth.Append(new TraitGrowthSelection(20, definition.Key)));
            var resolved = OwnedUnitSpecResolver.Resolve(profile.Definition, enhanced, catalog);
            Assert.That(((KnockbackTraitSpec)resolved.Traits.Span[0]).DistanceCells, Is.EqualTo(3));
        }

        private static TraitGrowthDefinition Vanguard(int? rank2 = null, int? rank3 = null, TraitSpec enhanced = null) =>
            new(new VanguardTraitSpec(1, 1), enhanced, "", "", "", "",
                rank2.HasValue ? new TraitRankValues(TraitKey.Vanguard, rank2.Value) : null,
                rank3.HasValue ? new TraitRankValues(TraitKey.Vanguard, rank3.Value) : null);

        private static int AttackAt(TraitGrowthDefinition definition, int rank)
        {
            var profile = TraitGrowthFixtures.Profile(native: new[] { definition.Normal });
            var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories, new[] { definition });
            var growth = OwnedTraitGrowth.Empty;
            for (int i = 1; i < rank; i++)
                growth = growth.WithPurchase(definition.Key);
            var owned = TraitGrowthFixtures.Owned(profile, growth: growth);
            return ((VanguardTraitSpec)OwnedUnitSpecResolver.Resolve(profile.Definition, owned, catalog).Traits.Span[0]).AttackBonus;
        }

        private static TraitGrowthCatalog EnhanceCatalog(TraitGrowthDefinition definition) => TraitGrowthFixtures.Catalog(
            steps: new[] { new TraitGrowthStepSpec(20, 2, TraitGrowthKind.Enhance) },
            definitions: new[] { definition });
    }
}
