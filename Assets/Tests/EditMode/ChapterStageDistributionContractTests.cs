using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.Core
{
    public sealed class ChapterStageDistributionContractTests
    {
        [TestCase(0, 1, 0, 1)]
        [TestCase(5, 1, 0, 1)]
        [TestCase(1, -1, 0, 1)]
        [TestCase(1, 10001, 0, 1)]
        [TestCase(1, 1, -1, 1)]
        [TestCase(1, 1, 0, 0)]
        public void Rule_RejectsUnknownKindAndOutOfRangeValues(int kind, int weight, int minimum, int minDepth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ChapterStageDistributionRule((ChapterStageKind)kind, weight, minimum, minDepth));
        }

        [TestCase(ChapterStageKind.Combat, 0, 0, 1, false)]
        [TestCase(ChapterStageKind.Shop, 0, 1, 2, true)]
        [TestCase(ChapterStageKind.Recovery, 10000, 0, 1, true)]
        [TestCase(ChapterStageKind.Enhancement, 1, 3, 5, true)]
        public void Rule_KeepsBoundaryValues(ChapterStageKind kind, int weight, int minimum, int minDepth, bool active)
        {
            var rule = new ChapterStageDistributionRule(kind, weight, minimum, minDepth);

            Assert.That((rule.Kind, rule.Weight, rule.MinimumCount, rule.MinDepth, rule.IsActive),
                Is.EqualTo((kind, weight, minimum, minDepth, active)));
        }

        [Test]
        public void Pool_CopiesAndSortsKeys()
        {
            var source = new List<int> { 30, 10, 20 };
            var pool = new ChapterStagePool(Rule(ChapterStageKind.Combat, 1, 0), source);
            source[0] = 99;
            source.Add(40);

            Assert.That(pool.StageKeys, Is.EqualTo(new[] { 10, 20, 30 }));
        }

        [Test]
        public void Pool_RejectsInvalidOrDuplicateKeys()
        {
            var rule = Rule(ChapterStageKind.Combat, 1, 0);

            Assert.Throws<ArgumentNullException>(() => new ChapterStagePool(null, new[] { 1 }));
            Assert.Throws<ArgumentNullException>(() => new ChapterStagePool(rule, null));
            Assert.Throws<ArgumentException>(() => new ChapterStagePool(rule, new[] { 0 }));
            Assert.Throws<ArgumentException>(() => new ChapterStagePool(rule, new[] { 3, -1 }));
            Assert.Throws<ArgumentException>(() => new ChapterStagePool(rule, new[] { 5, 7, 5 }));
        }

        [TestCase(1, 0)]
        [TestCase(0, 1)]
        public void Pool_RejectsActiveEmptyCandidates(int weight, int minimum)
        {
            Assert.Throws<ArgumentException>(
                () => new ChapterStagePool(Rule(ChapterStageKind.Shop, weight, minimum), Array.Empty<int>()));
        }

        [Test]
        public void Pool_AcceptsZeroWeightWithMinimumAndOneCandidate()
        {
            var pool = new ChapterStagePool(Rule(ChapterStageKind.Shop, 0, 1), new[] { 1201 });

            Assert.That(pool.StageKeys, Is.EqualTo(new[] { 1201 }));
        }

        [Test]
        public void Pool_AllowsInactiveEmptyCandidates()
        {
            var pool = new ChapterStagePool(Rule(ChapterStageKind.Enhancement, 0, 0), Array.Empty<int>());

            Assert.That(pool.StageKeys, Is.Empty);
        }

        [Test]
        public void Pool_AllowsInactiveCandidatesWithKeys()
        {
            var pool = new ChapterStagePool(Rule(ChapterStageKind.Enhancement, 0, 0), new[] { 1202 });

            Assert.That(pool.Rule.IsActive, Is.False);
            Assert.That(pool.StageKeys, Is.EqualTo(new[] { 1202 }));
        }

        // 최소 폭 1·시작 깊이 모두 1이면 옛 검사(1(고정) + 최소 합 − 고정 종류 1 ≤ 중간 층 수)와 같다.
        [Test]
        public void Placement_DefaultsMatchOldCapacity_CountingPinnedKindOnce()
        {
            var rules = new[]
            {
                Rule(ChapterStageKind.Combat, 70, 0),
                Rule(ChapterStageKind.Shop, 10, 1),
                Rule(ChapterStageKind.Recovery, 10, 1),
                Rule(ChapterStageKind.Enhancement, 10, 0)
            };

            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 2, 1, false), Is.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 1, 1, false), Is.Not.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 2, 1, false, ChapterStageKind.Combat), Is.Not.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 3, 1, false, ChapterStageKind.Combat), Is.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 2, 1, false, ChapterStageKind.Shop), Is.Null);
        }

        [Test]
        public void Placement_NoMiddleLayers_AllowsOnlyNoMinimumAndNoPin()
        {
            var plain = new[] { Rule(ChapterStageKind.Combat, 1, 0) };
            var withMinimum = new[] { Rule(ChapterStageKind.Shop, 1, 1) };

            Assert.That(ChapterStageDistributionRule.FindPlacementError(plain, 0, 1, false), Is.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(Array.Empty<ChapterStageDistributionRule>(), 0, 1, false), Is.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(withMinimum, 0, 1, false), Is.Not.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(plain, 0, 1, false, ChapterStageKind.Combat), Is.Not.Null);
        }

        [Test]
        public void Placement_WidthMinMultipliesGuaranteedSlots()
        {
            var rules = new[]
            {
                Rule(ChapterStageKind.Combat, 80, 2),
                Rule(ChapterStageKind.Shop, 9, 1),
                Rule(ChapterStageKind.Recovery, 7, 1)
            };

            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 2, 1, false), Is.Not.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 2, 2, false), Is.Null);
        }

        [Test]
        public void Placement_RejectsActiveStartDepthBeyondMiddleLayers_IgnoresInactive()
        {
            var activeDeep = new[] { Rule(ChapterStageKind.Combat, 1, 0), Rule(ChapterStageKind.Shop, 1, 0, minDepth: 5) };
            var inactiveDeep = new[] { Rule(ChapterStageKind.Combat, 1, 0), Rule(ChapterStageKind.Shop, 0, 0, minDepth: 5) };

            Assert.That(ChapterStageDistributionRule.FindPlacementError(activeDeep, 4, 1, false), Is.Not.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(activeDeep, 5, 1, false), Is.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(inactiveDeep, 4, 1, false), Is.Null);
        }

        [Test]
        public void Placement_RequiresPositiveWeightKindAtDepthOne()
        {
            var lateCombat = new[] { Rule(ChapterStageKind.Combat, 80, 0, minDepth: 2), Rule(ChapterStageKind.Shop, 0, 1) };
            var zeroWeightEarly = new[] { Rule(ChapterStageKind.Combat, 0, 1), Rule(ChapterStageKind.Shop, 5, 0, minDepth: 2) };

            Assert.That(ChapterStageDistributionRule.FindPlacementError(lateCombat, 3, 1, false), Is.Not.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(zeroWeightEarly, 3, 1, false), Is.Not.Null);
        }

        // Chapter1 꼴: 중간 층 4, 상점·회복·강화는 깊이 2부터 최소 1씩, 전투는 깊이 1부터 최소 1.
        [Test]
        public void Placement_ChecksEachDepthWithPinCorrection()
        {
            var rules = new[]
            {
                Rule(ChapterStageKind.Combat, 80, 1),
                Rule(ChapterStageKind.Shop, 9, 1, minDepth: 2),
                Rule(ChapterStageKind.Recovery, 7, 1, minDepth: 2),
                Rule(ChapterStageKind.Enhancement, 7, 1, minDepth: 2)
            };

            // 깊이 2 이상: 최소 3 ≤ 3자리(최소 폭 1).
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 4, 1, false), Is.Null);
            // 고정 노드가 깊이 2 이상에 생기면 그 자리를 차지하는데, 고정 종류(전투)는 깊이 2 이상 최소에 들지 않는다.
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 4, 1, false, ChapterStageKind.Combat), Is.Not.Null);
            // 고정 종류가 상점이면 상점 최소 하나와 상쇄된다.
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 4, 1, false, ChapterStageKind.Shop), Is.Null);
            // 최소 폭 2면 깊이 2 이상이 6자리라 전투 고정도 들어간다.
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 4, 2, false, ChapterStageKind.Combat), Is.Null);
            // 상점·회복·강화가 깊이 4부터면 깊이 4 한 층(1자리)에 3개가 필요하다.
            var deep = new[]
            {
                Rule(ChapterStageKind.Combat, 80, 1),
                Rule(ChapterStageKind.Shop, 9, 1, minDepth: 4),
                Rule(ChapterStageKind.Recovery, 7, 1, minDepth: 4),
                Rule(ChapterStageKind.Enhancement, 7, 1, minDepth: 4)
            };
            Assert.That(ChapterStageDistributionRule.FindPlacementError(deep, 4, 1, false), Is.Not.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(deep, 4, 3, false), Is.Null);
        }

        [Test]
        public void Placement_SingleFirstLayer_LeavesOneSlotAtDepthOne()
        {
            var twoCombats = new[] { Rule(ChapterStageKind.Combat, 80, 2) };

            // 중간 층 1, 최소 폭 3: 첫 층이 하나면 1자리라 최소 2를 못 놓는다.
            Assert.That(ChapterStageDistributionRule.FindPlacementError(twoCombats, 1, 3, false), Is.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(twoCombats, 1, 3, true), Is.Not.Null);
            // 깊이 2 이상 자리는 그대로다.
            var chapter = new[]
            {
                Rule(ChapterStageKind.Combat, 80, 1),
                Rule(ChapterStageKind.Shop, 9, 1, minDepth: 2),
                Rule(ChapterStageKind.Recovery, 7, 1, minDepth: 2),
                Rule(ChapterStageKind.Enhancement, 7, 1, minDepth: 2)
            };
            Assert.That(ChapterStageDistributionRule.FindPlacementError(chapter, 4, 2, true), Is.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(chapter, 4, 2, true, ChapterStageKind.Combat), Is.Null);
        }

        [Test]
        public void Placement_FirstLayerKind_CountsOnlyAtDepthOne_AndCoversItsMinimum()
        {
            var rules = new[]
            {
                Rule(ChapterStageKind.Combat, 80, 0),
                Rule(ChapterStageKind.Shop, 0, 1, minDepth: 2),
                Rule(ChapterStageKind.Recovery, 0, 1, minDepth: 2)
            };

            // 중간 층 3, 최소 폭 1, 첫 층 1개: 깊이 1 이상 3자리, 깊이 2 이상 2자리.
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 3, 1, true), Is.Null);
            // 첫 층 전투는 깊이 1 자리 하나만 쓴다: 깊이 1 이상 3개(전투 + 상점 + 회복), 깊이 2 이상 2개.
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 3, 1, true, firstLayerKind: ChapterStageKind.Combat), Is.Null);
            // 개발 고정은 깊이를 모르니 깊이 1 이상에 하나 더 세어 4 > 3.
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 3, 1, true, ChapterStageKind.Combat, ChapterStageKind.Combat),
                Is.Not.Null);
            // 고정 상점은 상점 최소를 채워 깊이 1 이상 3개(첫 층 + 고정 상점 + 회복), 깊이 2 이상 2개(고정 상점 + 회복).
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 3, 1, true, ChapterStageKind.Shop, ChapterStageKind.Combat), Is.Null);
        }

        [Test]
        public void Placement_FirstLayerKind_RequiresSingleFirstLayerAndMiddleLayer()
        {
            var rules = new[] { Rule(ChapterStageKind.Combat, 80, 0) };

            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 3, 1, false, firstLayerKind: ChapterStageKind.Combat), Is.Not.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 0, 1, true, firstLayerKind: ChapterStageKind.Combat), Is.Not.Null);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ChapterStageDistributionRule.FindPlacementError(rules, 3, 1, true, firstLayerKind: (ChapterStageKind)9));
        }

        [Test]
        public void Placement_DoesNotOverflow()
        {
            var rules = new[]
            {
                Rule(ChapterStageKind.Combat, 1, 0),
                Rule(ChapterStageKind.Shop, 0, int.MaxValue),
                Rule(ChapterStageKind.Recovery, 0, int.MaxValue)
            };

            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 3, int.MaxValue, false), Is.Null);
            Assert.That(ChapterStageDistributionRule.FindPlacementError(rules, 3, 1, false), Is.Not.Null);
        }

        [Test]
        public void Placement_RejectsInvalidInput()
        {
            var empty = Array.Empty<ChapterStageDistributionRule>();

            Assert.Throws<ArgumentNullException>(() => ChapterStageDistributionRule.FindPlacementError(null, 1, 1, false));
            Assert.Throws<ArgumentException>(
                () => ChapterStageDistributionRule.FindPlacementError(new ChapterStageDistributionRule[] { null }, 1, 1, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => ChapterStageDistributionRule.FindPlacementError(empty, -1, 1, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => ChapterStageDistributionRule.FindPlacementError(empty, 1, 0, false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => ChapterStageDistributionRule.FindPlacementError(empty, 1, 1, false, (ChapterStageKind)9));
        }

        private static ChapterStageDistributionRule Rule(ChapterStageKind kind, int weight, int minimum, int minDepth = 1)
            => new(kind, weight, minimum, minDepth);
    }
}
