using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class ShopOfferPlannerTests
    {
        [Test]
        public void SubAverage_IsFlooredAndExcludesMain()
        {
            var owned = new[]
            {
                Owned(1, EntityKey.Unit_Akimbo, 99, CharacterOwnershipRole.Main),
                Owned(2, EntityKey.Unit_Cowboy, 2), Owned(3, EntityKey.Unit_DynamiteHammer, 5)
            };
            var offer = Plan(new[] { ShopCandidateSpec.ForUnit(EntityKey.Unit_Sniper, 600) }, owned,
                new Random(1), 2, 1).Single();
            Assert.That(offer.InitialLevel, Is.EqualTo(3));
            Assert.That(offer.Cost, Is.EqualTo(600));
            Assert.That(owned[1].Level, Is.EqualTo(2));
        }

        [Test]
        public void NoSubs_UsesLevelOne_AndLargeLevelsDoNotOverflow()
        {
            var candidate = new[] { ShopCandidateSpec.ForUnit(EntityKey.Unit_Sniper, 600) };
            var noSubs = Plan(candidate,
                new[] { Owned(1, EntityKey.Unit_Akimbo, 99, CharacterOwnershipRole.Main) }, new Random(1), 2, 1);
            Assert.That(noSubs.Single().InitialLevel, Is.EqualTo(1));
            var large = Plan(candidate, new[]
            {
                Owned(1, EntityKey.Unit_Cowboy, int.MaxValue), Owned(2, EntityKey.Unit_DynamiteHammer, int.MaxValue)
            }, new Random(1), 2, 1);
            Assert.That(large.Single().InitialLevel, Is.EqualTo(int.MaxValue));
        }

        /// <summary>중복 서브 금지 설계 U5: 이미 가진 서브와 메인은 카테고리 칸이 남아 있어도 후보에서 빠진다.</summary>
        [Test]
        public void OwnedSubAndMain_AreExcluded_WhileOtherSubInSameCategoryIsOffered()
        {
            var candidates = new[]
            {
                ShopCandidateSpec.ForUnit(EntityKey.Unit_Cowboy, 600),
                ShopCandidateSpec.ForUnit(EntityKey.Unit_DynamiteHammer, 600),
                ShopCandidateSpec.ForUnit(EntityKey.Unit_Akimbo, 600), Item(1)
            };
            var owned = new[]
            {
                Owned(8, EntityKey.Unit_Cowboy, 7), Owned(9, EntityKey.Unit_Akimbo, 1, CharacterOwnershipRole.Main)
            };
            var offers = Plan(candidates, owned, new Random(42), 2, 1);
            Assert.That(offers.Count, Is.EqualTo(2));
            Assert.That(offers.Any(x => x.CharacterKey == EntityKey.Unit_Cowboy), Is.False);
            Assert.That(offers.Any(x => x.CharacterKey == EntityKey.Unit_Akimbo), Is.False);
            Assert.That(offers.Any(x => x.CharacterKey == EntityKey.Unit_DynamiteHammer), Is.True);
        }

        [TestCase(0, ShopProductKind.ConsumableItem)]
        [TestCase(1, ShopProductKind.ConsumableItem)]
        [TestCase(2, ShopProductKind.Unit)]
        public void CategoryWeight_IsIndependentOfPoolSizes(int categoryRoll, ShopProductKind firstKind)
        {
            var random = new DrawsRandom(categoryRoll, 0);
            var offers = Plan(new[]
            {
                Item(1), Item(2), Item(3), Item(4), ShopCandidateSpec.ForUnit(EntityKey.Unit_Sniper, 600)
            }, Array.Empty<OwnedCharacterRecord>(), random, 2, 1);
            Assert.That(random.Bounds[0], Is.EqualTo(3));
            Assert.That(offers[0].Kind, Is.EqualTo(firstKind));
            Assert.That(random.Bounds[1], Is.EqualTo(firstKind == ShopProductKind.Unit ? 1 : 4));
        }

        [Test]
        public void CandidateDraw_UsesEveryRemainingItemWithEqualRange()
        {
            var random = new DrawsRandom(0, 1);
            var offers = Plan(new[]
            {
                Item(1), Item(2), ShopCandidateSpec.ForUnit(EntityKey.Unit_Sniper, 600)
            }, Array.Empty<OwnedCharacterRecord>(), random, 2, 1);
            Assert.That(random.Bounds[1], Is.EqualTo(2));
            Assert.That(offers[0].ItemKey, Is.EqualTo(2));
            Assert.That(offers.Select(x => (x.Kind, x.CharacterKey, x.ItemKey)).Distinct().Count(), Is.EqualTo(3));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(4, 3)]
        public void ItemOnlyPool_PublishesAvailableDistinctOffers(int count, int expected)
        {
            var offers = Plan(Enumerable.Range(1, count).Select(Item).ToArray(),
                Array.Empty<OwnedCharacterRecord>(), new Random(9), 2, 1);
            Assert.That(offers.Count, Is.EqualTo(expected));
            Assert.That(offers.Select(x => x.ItemKey).Distinct().Count(), Is.EqualTo(expected));
            Assert.That(offers.Select(x => x.OfferId.Value), Is.EqualTo(Enumerable.Range(1, expected)));
            Assert.That(offers.All(x => x.Kind == ShopProductKind.ConsumableItem), Is.True);
        }

        [Test]
        public void UnitOnlyPool_DoesNotInventItemsOrRepeatUnits()
        {
            var offers = Plan(new[]
            {
                ShopCandidateSpec.ForUnit(EntityKey.Unit_Cowboy, 600),
                ShopCandidateSpec.ForUnit(EntityKey.Unit_Sniper, 600)
            }, Array.Empty<OwnedCharacterRecord>(), new Random(2), 2, 1);
            Assert.That(offers.Count, Is.EqualTo(2));
            Assert.That(offers.Select(x => x.CharacterKey).Distinct().Count(), Is.EqualTo(2));
            Assert.That(offers.All(x => x.Kind == ShopProductKind.Unit), Is.True);
        }

        [Test]
        public void CanonicalOrdering_KeepsSeededDrawAndInputsStable()
        {
            var candidates = new[] { Item(4), ShopCandidateSpec.ForUnit(EntityKey.Unit_Sniper, 600), Item(1), Item(2) };
            var first = Plan(candidates, Array.Empty<OwnedCharacterRecord>(), new Random(42), 2, 1);
            var reversed = Plan(candidates.Reverse().ToArray(), Array.Empty<OwnedCharacterRecord>(), new Random(42), 2, 1);
            Assert.That(first.Select(x => (x.Kind, x.CharacterKey, x.ItemKey)),
                Is.EqualTo(reversed.Select(x => (x.Kind, x.CharacterKey, x.ItemKey))));
            Assert.That(candidates[0].ItemKey, Is.EqualTo(4));
            Assert.Throws<NotSupportedException>(() => ((IList<ShopPurchaseSnapshot>)first).Clear());
        }

        [Test]
        public void InvalidCandidatesAndWeights_AreConfigurationErrors()
        {
            var owned = Array.Empty<OwnedCharacterRecord>();
            Assert.Throws<ArgumentException>(() => Plan(new[] { Item(1), Item(1) }, owned, new Random(1), 2, 1));
            Assert.Throws<ArgumentException>(() => Plan(new ShopCandidateSpec[] { null }, owned, new Random(1), 2, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Plan(new[] { Item(1) }, owned, new Random(1), 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Plan(new[] { Item(1) }, owned, new Random(1), int.MaxValue, 1));
        }

        [Test]
        public void MainCandidate_IsOfferedAtMainAcquisitionLevel_WhenSubAverageAboveOne()
        {
            // 서브 평균 레벨(3)은 서브 후보에만 쓰고, 메인 후보는 소유 규칙의 메인 획득 레벨로 제시한다. 가격은 후보 값 그대로다.
            var owned = new[] { Owned(1, EntityKey.Unit_Cowboy, 3) };
            var offers = Plan(new[]
            {
                ShopCandidateSpec.ForUnit(EntityKey.Unit_Akimbo, 5), ShopCandidateSpec.ForUnit(EntityKey.Unit_Sniper, 7), Item(1)
            }, owned, new Random(1), 2, 1);
            var main = offers.Single(x => x.Kind == ShopProductKind.Unit && x.CharacterKey == EntityKey.Unit_Akimbo);
            var sub = offers.Single(x => x.Kind == ShopProductKind.Unit && x.CharacterKey == EntityKey.Unit_Sniper);
            Assert.That(main.InitialLevel, Is.EqualTo(CharacterRosterRules.MainAcquisitionLevel));
            Assert.That(main.Cost, Is.EqualTo(5));
            Assert.That(sub.InitialLevel, Is.EqualTo(3));
            Assert.That(sub.Cost, Is.EqualTo(7));
        }

        [Test]
        public void CategoryFull_ExcludesDifferentDefinitionInSameCategory()
        {
            var owned = new[] { Owned(1, EntityKey.Unit_Sniper, 1), Owned(2, EntityKey.Unit_Ballista, 1) };
            var offers = Plan(new[] { ShopCandidateSpec.ForUnit(EntityKey.Unit_Gravekeeper, 5), Item(1) }, owned, new Random(1), 2, 1);
            Assert.That(offers.Single().Kind, Is.EqualTo(ShopProductKind.ConsumableItem));
        }

        private static IReadOnlyList<ShopPurchaseSnapshot> Plan(IReadOnlyList<ShopCandidateSpec> candidates,
            IReadOnlyList<OwnedCharacterRecord> owned, Random random, int itemWeight, int unitWeight)
        {
            var definitions = new Dictionary<EntityKey, CharacterDefinitionFacts>
            {
                [EntityKey.Unit_Akimbo] = new CharacterDefinitionFacts(EntityKey.Unit_Akimbo, UnitRole.MainCharacter,
                    UnitOwnerEligibility.PlayerOnly, new MainCharacterPersistentState(10, 10, 5), CategoryKey.Main),
                [EntityKey.Unit_Cowboy] = new CharacterDefinitionFacts(EntityKey.Unit_Cowboy, UnitRole.Regular, UnitOwnerEligibility.Both, category: CategoryKey.Rider),
                [EntityKey.Unit_Sniper] = new CharacterDefinitionFacts(EntityKey.Unit_Sniper, UnitRole.Regular, UnitOwnerEligibility.Both, category: CategoryKey.Cannon),
                [EntityKey.Unit_Gravekeeper] = new CharacterDefinitionFacts(EntityKey.Unit_Gravekeeper, UnitRole.Regular, UnitOwnerEligibility.Both, category: CategoryKey.Cannon),
                [EntityKey.Unit_Ballista] = new CharacterDefinitionFacts(EntityKey.Unit_Ballista, UnitRole.Regular, UnitOwnerEligibility.Both, category: CategoryKey.Cannon),
                [EntityKey.Unit_DynamiteHammer] = new CharacterDefinitionFacts(EntityKey.Unit_DynamiteHammer, UnitRole.Regular, UnitOwnerEligibility.Both, category: CategoryKey.Rider)
            };
            return ShopOfferPlanner.Plan(candidates, owned, random, itemWeight, unitWeight, definitions);
        }

        private static ShopCandidateSpec Item(int key) => ShopCandidateSpec.ForItem(key, 100, "Item " + key, "");
        private static OwnedCharacterRecord Owned(int id, EntityKey key, int level,
            CharacterOwnershipRole role = CharacterOwnershipRole.Sub)
            => new OwnedCharacterRecord(new CharacterInstanceId(id), key, role, level);

        private sealed class DrawsRandom : Random
        {
            private readonly Queue<int> _draws;
            public List<int> Bounds { get; } = new List<int>();
            public DrawsRandom(params int[] draws) => _draws = new Queue<int>(draws);
            public override int Next(int maxValue)
            {
                Bounds.Add(maxValue);
                int value = _draws.Count == 0 ? 0 : _draws.Dequeue();
                Assert.That(value, Is.InRange(0, maxValue - 1));
                return value;
            }
        }
    }
}
