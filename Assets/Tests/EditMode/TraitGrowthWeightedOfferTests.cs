using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class TraitGrowthWeightedOfferTests
    {
        private static readonly TraitKey[] CommonKeys =
        {
            TraitKey.Knockback, TraitKey.Vanguard, TraitKey.CleanSweep, TraitKey.SoloAction,
            TraitKey.CallOfBlood, TraitKey.MoraleBoost, TraitKey.Disarm, TraitKey.AnkleBreaker
        };

        private static readonly TraitKey[] CategoryKeys =
        {
            TraitKey.Leap, TraitKey.SteadfastWill, TraitKey.TrackingShot, TraitKey.Cohesion
        };

        [Test]
        public void WeightedSample_IsDistinctSortedAndInputOrderIndependent()
        {
            var groups = Groups(4);
            var candidates = groups.Keys.Reverse().ToArray();
            groups.Add(TraitKey.Focus, TraitGrowthGroup.Common);
            var originalKeys = candidates.ToArray();
            var originalGroups = groups.ToArray();

            var sample = TraitGrowthOfferSampler.Sample(candidates, TraitGrowthKind.Acquire, 42, groups, 1, 2);
            var reversedGroups = groups.Reverse().ToDictionary(pair => pair.Key, pair => pair.Value);
            var reordered = TraitGrowthOfferSampler.Sample(candidates.Reverse().ToArray(), TraitGrowthKind.Acquire,
                42, reversedGroups, 1, 2);

            Assert.That(sample.Count, Is.EqualTo(TraitGrowthOffer.MaximumChoices));
            Assert.That(sample, Is.Unique.And.Ordered);
            Assert.That(sample, Is.SubsetOf(originalKeys));
            Assert.That(reordered, Is.EqualTo(sample));
            Assert.That(candidates, Is.EqualTo(originalKeys));
            Assert.That(groups, Is.EquivalentTo(originalGroups));
        }

        [Test]
        public void WeightedSample_RejectsInvalidInputs()
        {
            var groups = Groups(4);
            var keys = groups.Keys.ToArray();
            Assert.Throws<ArgumentNullException>(() => TraitGrowthOfferSampler.Sample(null, TraitGrowthKind.Acquire, 1, groups, 1, 2));
            Assert.Throws<InvalidOperationException>(() => TraitGrowthOfferSampler.Sample(Array.Empty<TraitKey>(), TraitGrowthKind.Acquire, 1, groups, 1, 2));
            foreach (var invalid in new[] { new[] { keys[0], keys[0] }, new[] { TraitKey.None }, new[] { (TraitKey)999 } })
                Assert.That(() => TraitGrowthOfferSampler.Sample(invalid, TraitGrowthKind.Acquire, 1, groups, 1, 2), Throws.InstanceOf<ArgumentException>());
            Assert.Throws<ArgumentNullException>(() => TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, 1, null, 1, 2));
            Assert.That(() => TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.None, 1, groups, 1, 2), Throws.InstanceOf<ArgumentException>());
            foreach (var (commonWeight, categoryWeight) in new[] { (0, 2), (1, 0), (-1, 2), (1, -1), (int.MaxValue, 1) })
                Assert.That(() => TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, 1, groups, commonWeight, categoryWeight),
                    Throws.InstanceOf<ArgumentException>());

            groups.Remove(keys[0]);
            Assert.That(() => TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, 1, groups, 1, 2), Throws.InstanceOf<ArgumentException>());
            foreach (var group in new[] { TraitGrowthGroup.Unspecified, (TraitGrowthGroup)999 })
            {
                groups[keys[0]] = group;
                Assert.That(() => TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, 1, groups, 1, 2), Throws.InstanceOf<ArgumentException>());
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void OfferMaximum_IsShared(int count)
        {
            var owned = TraitGrowthFixtures.Owned(TraitGrowthFixtures.Profile());
            var step = new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire);
            var keys = CommonKeys.Take(count).ToArray();

            Assert.That(TraitGrowthOffer.MaximumChoices, Is.EqualTo(3));
            if (count <= TraitGrowthOffer.MaximumChoices)
                Assert.That(TraitGrowthFixtures.Offer(owned, step, keys).Choices.Count, Is.EqualTo(count));
            else
                Assert.Throws<ArgumentException>(() => TraitGrowthFixtures.Offer(owned, step, keys));
        }

        [TestCase(4)]
        [TestCase(8)]
        public void WeightedSample_GroupFrequencyDoesNotDependOnPoolSize(int commonCount)
        {
            var groups = Groups(commonCount);
            var draws = DrawAcrossSeeds(groups, 1, 2);
            Assert.That(draws.Count(key => groups[key] == TraitGrowthGroup.CategorySpecific) / (double)draws.Length,
                Is.EqualTo(2.0 / 3).Within(0.04));
        }

        [Test]
        public void WeightedSample_EqualWeightsDrawUniformlyWithinGroups()
        {
            var groups = Groups(4);
            var draws = DrawAcrossSeeds(groups, 1, 1);
            double expectedCount = draws.Length / (double)groups.Count;
            foreach (var key in groups.Keys)
                Assert.That(draws.Count(drawn => drawn == key), Is.EqualTo(expectedCount).Within(expectedCount * 0.12), key.ToString());
        }

        [TestCase(4, 0)]
        [TestCase(0, 4)]
        [TestCase(1, 2)]
        [TestCase(1, 0)]
        [TestCase(0, 2)]
        [TestCase(1, 1)]
        public void WeightedSample_FallsBackAndReturnsAllWhenAtMostThreeRemain(int commonCount, int categoryCount)
        {
            var groups = Groups(commonCount, categoryCount);
            var keys = groups.Keys.ToArray();
            foreach (int seed in Enumerable.Range(0, 32))
            {
                var drawn = TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, seed, groups, 1, 2);
                Assert.That(drawn, Is.Unique.And.SubsetOf(keys));
                Assert.That(drawn.Count, Is.EqualTo(Math.Min(TraitGrowthOffer.MaximumChoices, keys.Length)));
                if (keys.Length <= TraitGrowthOffer.MaximumChoices)
                    Assert.That(drawn, Is.EqualTo(keys.OrderBy(key => key)));
            }
        }

        [TestCase(int.MaxValue - 1, 1)]
        [TestCase(1, int.MaxValue - 1)]
        public void WeightedSample_HandlesMaximumWeightSum(int commonWeight, int categoryWeight)
        {
            var groups = Groups(4);
            var keys = groups.Keys.ToArray();
            foreach (int seed in Enumerable.Range(0, 32))
            {
                var drawn = TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, seed, groups, commonWeight, categoryWeight);
                Assert.That(drawn, Is.Unique.And.SubsetOf(keys));
                Assert.That(drawn.Count, Is.EqualTo(TraitGrowthOffer.MaximumChoices));
            }
        }

        [Test]
        public void Enhance_IgnoresAcquisitionWeights()
        {
            var groups = Groups(4);
            var keys = groups.Keys.ToArray();
            foreach (int seed in new[] { 0, 1, 42, -17 })
            {
                var legacy = TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Enhance, seed);
                Assert.That(TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Enhance, seed, groups, 1, 9), Is.EqualTo(legacy));
                Assert.That(TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Enhance, seed, null, 0, 0), Is.EqualTo(legacy));
            }
        }

        [TestCase(TraitGrowthKind.Acquire)]
        [TestCase(TraitGrowthKind.Enhance)]
        public void LegacySample_PreservesSeededOutput(TraitGrowthKind kind)
        {
            var keys = TraitGrowthFixtures.Definitions.Select(definition => definition.Key).ToArray();
            Assert.That(TraitGrowthOfferSampler.Sample(keys, kind, 42),
                Is.EqualTo(new[] { TraitKey.HuntersMark, TraitKey.Vanguard, TraitKey.GroundCompaction }));
            Assert.That(TraitGrowthOfferSampler.Sample(keys, kind, -17),
                Is.EqualTo(new[] { TraitKey.SoloAction, TraitKey.GroundCompaction, TraitKey.TrackingShot }));
        }

        private static Dictionary<TraitKey, TraitGrowthGroup> Groups(int commonCount, int categoryCount = 4) =>
            CommonKeys.Take(commonCount).Concat(CategoryKeys.Take(categoryCount)).ToDictionary(key => key,
                key => CommonKeys.Contains(key) ? TraitGrowthGroup.Common : TraitGrowthGroup.CategorySpecific);

        private static TraitKey[] DrawAcrossSeeds(Dictionary<TraitKey, TraitGrowthGroup> groups, int commonWeight, int categoryWeight)
        {
            var keys = groups.Keys.ToArray();
            return Enumerable.Range(0, 4096).SelectMany(seed =>
                TraitGrowthOfferSampler.Sample(keys, TraitGrowthKind.Acquire, seed, groups, commonWeight, categoryWeight)).ToArray();
        }

        [TestCase(0)]
        [TestCase(999)]
        public void ExplicitGroup_RejectsUnspecifiedAndUnknown(int group)
        {
            Assert.That(() => new TraitGrowthDefinition((TraitGrowthGroup)group, new KnockbackTraitSpec(1),
                null, "knockback", "", "", ""), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void LegacyConstructors_PreserveExistingCalls()
        {
            var normal = new KnockbackTraitSpec(1);
            var definition = new TraitGrowthDefinition(normal, null, "knockback", "", "", "");
            var row = new CategoryTraitProgressionSpec(CategoryKey.Heavy, Array.Empty<TraitGrowthStepSpec>(),
                new[] { TraitKey.Knockback });

            Assert.That(definition.Normal, Is.SameAs(normal));
            Assert.That(definition.Group, Is.EqualTo(TraitGrowthGroup.Common));
            Assert.That((row.CommonWeight, row.CategoryWeight), Is.EqualTo((1, 2)));
        }

        [TestCase(0, 2)]
        [TestCase(1, 0)]
        [TestCase(-1, 2)]
        [TestCase(1, -2)]
        [TestCase(int.MaxValue, 1)]
        public void Weights_RejectInvalidValues(int commonWeight, int categoryWeight)
        {
            Assert.That(() => new CategoryTraitProgressionSpec(CategoryKey.Heavy, Array.Empty<TraitGrowthStepSpec>(),
                Array.Empty<TraitKey>(), commonWeight, categoryWeight), Throws.InstanceOf<ArgumentException>());
        }

        [Test]
        public void Weights_AcceptMaximumSum()
        {
            var row = new CategoryTraitProgressionSpec(CategoryKey.Heavy, Array.Empty<TraitGrowthStepSpec>(),
                Array.Empty<TraitKey>(), 1, int.MaxValue - 1);
            Assert.That((row.CommonWeight, row.CategoryWeight), Is.EqualTo((1, int.MaxValue - 1)));
        }

        [TestCase(CategoryKey.Rider, 3, 7)]
        [TestCase(CategoryKey.Heavy, 5, 2)]
        [TestCase(CategoryKey.Cannon, 8, 1)]
        public void MainSpecialization_UsesSelectedWeightsAndMainSteps(CategoryKey selected, int commonWeight,
            int categoryWeight)
        {
            var main = new CategoryTraitProgressionSpec(CategoryKey.Main,
                new[] { new TraitGrowthStepSpec(10, 2, TraitGrowthKind.Acquire), new TraitGrowthStepSpec(20, 3, TraitGrowthKind.Acquire) },
                new[] { TraitKey.Knockback, TraitKey.SoloAction }, 2, 3);
            var specialized = new CategoryTraitProgressionSpec(selected,
                new[] { new TraitGrowthStepSpec(99, 2, TraitGrowthKind.Acquire) },
                new[] { TraitKey.Knockback, TraitKey.TrackingShot }, commonWeight, categoryWeight);
            var rows = TraitGrowthCatalog.Disabled.Categories.Select(row =>
                row.Category == CategoryKey.Main ? main : row.Category == selected ? specialized : row).ToArray();
            var catalog = new TraitGrowthCatalog(rows, TraitGrowthFixtures.Definitions);
            var basis = TraitGrowthFixtures.Profile(CategoryKey.Main).Definition;
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), basis.Key, CharacterOwnershipRole.Main,
                3, 0, specializationCategory: selected);

            var effective = EffectiveTraitProgression.Resolve(basis, owned, catalog);
            Assert.That(effective.Category, Is.EqualTo(CategoryKey.Main));
            Assert.That(effective.Steps.Select(step => step.StepId), Is.EqualTo(new[] { 10, 20 }));
            Assert.That(effective.Pool, Is.EquivalentTo(new[] { TraitKey.Knockback, TraitKey.SoloAction, TraitKey.TrackingShot }));
            Assert.That((effective.CommonWeight, effective.CategoryWeight), Is.EqualTo((commonWeight, categoryWeight)));

            var unspecialized = EffectiveTraitProgression.Resolve(basis,
                new OwnedCharacterRecord(owned.Id, basis.Key, CharacterOwnershipRole.Main, 3, 0), catalog);
            Assert.That(unspecialized, Is.SameAs(main));
        }
    }
}
