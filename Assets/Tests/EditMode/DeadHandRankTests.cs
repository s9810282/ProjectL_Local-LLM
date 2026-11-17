using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;

namespace ProjectL.Tests.Core
{
    public sealed class DeadHandRankTests
    {
        [Test]
        public void EqualRanksRequireExplicitPolicyAndContiguousSlots()
        {
            Assert.Throws<ArgumentException>(() => Definition(100, 100, false));
            Assert.That(Definition(100, 100, true).MaxRank, Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => new TraitGrowthDefinition(new DeadHandTraitSpec(100), null, "", "", "", "",
                rank3: new TraitRankValues(TraitKey.DeadHand, 100), allowUnchangedRankValues: true));
        }

        [TestCase(100, 100)]
        [TestCase(125, 150)]
        public void RestoredPurchasesResolveRankEvenWhenPercentIsUnchanged(int rank2, int rank3)
        {
            var definition = Definition(rank2, rank3, true);
            var profile = TraitGrowthFixtures.Profile(CategoryKey.Heavy, definition.Normal);
            var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories, new[] { definition });
            var expected = new[] { 100, rank2, rank3 };
            for (int count = 0; count < expected.Length; count++)
            {
                var purchases = count == 0 ? null : new Dictionary<TraitKey, int> { [definition.Key] = count };
                var record = TraitGrowthFixtures.Owned(profile, growth: new OwnedTraitGrowth(Array.Empty<TraitGrowthSelection>(), purchases));
                var ranks = new Dictionary<TraitKey, int>();
                var effective = OwnedUnitSpecResolver.Resolve(profile.Definition, record, catalog, ranks);
                Assert.That(ranks[definition.Key], Is.EqualTo(count + 1));
                Assert.That(((DeadHandTraitSpec)effective.Traits.Span[0]).DamagePercent, Is.EqualTo(expected[count]));
            }
        }

        [Test]
        public void TrainingAdvancesAnOptedInEqualRankIncludingActiveDeadHand()
        {
            var deadHand = Definition(100, 100, true);
            var vanguard = new TraitGrowthDefinition(new VanguardTraitSpec(1, 1), null, "", "", "", "",
                rank2: new TraitRankValues(TraitKey.Vanguard, 1), allowUnchangedRankValues: true);
            var profile = TraitGrowthFixtures.Profile(CategoryKey.Heavy, deadHand.Normal, vanguard.Normal);
            var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories, new[] { deadHand, vanguard });
            var visit = new TrainingVisitDefinition(new TrainingPrices(10, 20), catalog, new[] { profile.Definition });
            var record = TraitGrowthFixtures.Owned(profile);

            var purchased = TrainingRules.Plan(visit, record, vanguard.Key, 1, 20);
            Assert.That(purchased.Succeeded, Is.True);
            Assert.That(purchased.Final.Growth.Purchases[vanguard.Key], Is.EqualTo(1));
            Assert.That(purchased.FinalBalance, Is.EqualTo(10));
            var trained = TrainingRules.Plan(visit, record, deadHand.Key, 1, 20);
            Assert.That(trained.Succeeded, Is.True);
            Assert.That(trained.Final.Growth.Purchases[deadHand.Key], Is.EqualTo(1));
            Assert.That(trained.FinalBalance, Is.EqualTo(10));
        }

        private static TraitGrowthDefinition Definition(int rank2, int rank3, bool allowUnchanged) =>
            new TraitGrowthDefinition(new DeadHandTraitSpec(100), null, "", "", "", "",
                new TraitRankValues(TraitKey.DeadHand, rank2), new TraitRankValues(TraitKey.DeadHand, rank3), allowUnchanged);
    }
}
