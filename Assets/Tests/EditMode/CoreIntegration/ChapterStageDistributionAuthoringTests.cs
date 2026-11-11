using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.Factory.Runtime.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tests.CoreIntegration
{
    // 테스트가 구성한 맵 SO의 출현 설정과 Core 후보 입력 경계를 확인합니다. 그래프 생성은 별도 통합 시험이 맡습니다.
    public sealed class ChapterStageDistributionAuthoringTests
    {
        private readonly List<UnityEngine.Object> _owned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object item in _owned) UnityEngine.Object.DestroyImmediate(item);
            _owned.Clear();
        }

        [Test]
        public void GetData_KeepsFourDistinctValuesInKindOrder()
        {
            var asset = CreateMap(7, (11, 2), (12, 3), (13, 4), (14, 5));

            var data = (StageMapData)asset.GetData();

            Assert.That(data.StageDistribution.Select(r => (r.Kind, r.Weight, r.MinimumCount)), Is.EqualTo(new[]
            {
                (ChapterStageKind.Combat, 11, 2), (ChapterStageKind.Shop, 12, 3),
                (ChapterStageKind.Recovery, 13, 4), (ChapterStageKind.Enhancement, 14, 5)
            }));
        }

        [Test]
        public void GetData_ReturnsIndependentValues()
        {
            var asset = CreateMap(7, (80, 1), (9, 1), (7, 1), (4, 1));
            var first = (StageMapData)asset.GetData();

            SetDistribution(asset, "shopDistribution", 50, 2);
            var second = (StageMapData)asset.GetData();

            Assert.That((first.StageDistribution[1].Weight, first.StageDistribution[1].MinimumCount), Is.EqualTo((9, 1)));
            Assert.That((second.StageDistribution[1].Weight, second.StageDistribution[1].MinimumCount), Is.EqualTo((50, 2)));
            Assert.That(second.StageDistribution, Is.Not.SameAs(first.StageDistribution));
        }

        [Test]
        public void Constructor_CopiesAndSortsRules()
        {
            var rules = new List<ChapterStageDistributionRule>(ValidRules().Reverse());
            var data = Map(7, rules);
            rules.Clear();

            Assert.That(data.StageDistribution.Select(r => r.Kind), Is.EqualTo(new[]
                { ChapterStageKind.Combat, ChapterStageKind.Shop, ChapterStageKind.Recovery, ChapterStageKind.Enhancement }));
            Assert.Throws<ArgumentNullException>(() => Map(7, null));
            Assert.Throws<ArgumentException>(() => Map(7, new ChapterStageDistributionRule[] { null }));
        }

        [Test]
        public void Validate_AcceptsOperationalPolicy()
        {
            Assert.DoesNotThrow(() => Map(7, ValidRules()).Validate());
        }

        [Test]
        public void Validate_RejectsInvalidOperationalPolicy()
        {
            var valid = ValidRules();
            ChapterStageDistributionRule[] Replace(ChapterStageKind kind, int weight, int minimum)
                => valid.Select(r => r.Kind == kind ? new ChapterStageDistributionRule(kind, weight, minimum, 1) : r).ToArray();

            Assert.Throws<InvalidOperationException>(() => Map(7, valid.Take(3).ToArray()).Validate(), "missing kind");
            Assert.Throws<InvalidOperationException>(() => Map(7, valid.Take(3).Append(valid[0]).ToArray()).Validate(), "duplicate kind");
            Assert.Throws<InvalidOperationException>(() => Map(7, valid.Select(r =>
                new ChapterStageDistributionRule(r.Kind, 0, r.MinimumCount, r.MinDepth)).ToArray()).Validate(), "all-zero weights");
            Assert.Throws<InvalidOperationException>(() => Map(7, Replace(ChapterStageKind.Shop, 9, 0)).Validate(), "shop minimum");
            Assert.Throws<InvalidOperationException>(() => Map(7, Replace(ChapterStageKind.Recovery, 7, 0)).Validate(), "recovery minimum");
            // 최소 개수 합 4가 중간 층 3개보다 크다.
            Assert.Throws<InvalidOperationException>(() => Map(5, valid).Validate(), "capacity");
        }

        [Test]
        public void Validate_FirstStageKey_RequiresSingleFirstLayer()
        {
            Assert.DoesNotThrow(() => Map(7, ValidRules(), singleFirstLayer: true, firstStageKey: 1120).Validate());
            Assert.DoesNotThrow(() => Map(7, ValidRules(), singleFirstLayer: true).Validate());
            Assert.Throws<InvalidOperationException>(() => Map(7, ValidRules(), firstStageKey: 1120).Validate(), "first layer off");
            Assert.Throws<InvalidOperationException>(() => Map(7, ValidRules(), singleFirstLayer: true, firstStageKey: -1).Validate(), "negative");
        }

        [Test]
        public void GetData_OutOfRangeInspectorValue_NamesMapAndKind()
        {
            var asset = CreateMap(7, (80, 1), (10001, 1), (7, 1), (4, 1));

            var error = Assert.Throws<InvalidOperationException>(() => asset.GetData());

            StringAssert.Contains("MapKey 7", error.Message);
            StringAssert.Contains(nameof(ChapterStageKind.Shop), error.Message);
        }

        [Test]
        public void ObsoleteConstructor_HasEmptyPolicyAndFailsValidate()
        {
#pragma warning disable CS0618
            var data = new StageMapData(1, 7, 3, 0, StageConnectionAlgorithm.Nearest, 1, 1, 0, false, 0);
#pragma warning restore CS0618

            Assert.That(data.StageDistribution, Is.Empty);
            Assert.Throws<InvalidOperationException>(() => data.Validate());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TemporaryCoreDatabase_BuildsValidMaps(bool withChapterContinuation)
        {
            // 생성자가 모든 맵의 Validate를 부른다. 이 개발 DB의 다른 사용처는 Framework 시험뿐이다.
            var database = new TemporaryCoreDatabase(withChapterContinuation);

            foreach (StageMapData map in database.GetAllStageMaps())
            {
                var regular = database.GetStages(map.MapKey).Where(s => s.MapRole == StageMapRole.Regular).ToArray();
                Assert.That(regular.Any(s => s.StageData is ShopStageDetail), Is.True, $"map {map.MapKey} shop");
                Assert.That(regular.Any(s => s.StageData is EventStageDetail), Is.True, $"map {map.MapKey} recovery");
                Assert.That(regular.Any(s => s.StageData is CombatStageDetail), Is.True, $"map {map.MapKey} combat");
            }
        }

        [TestCase(0, 0, false)]
        [TestCase(0, 1, true)]
        [TestCase(7, 0, true)]
        public void ConfiguredMapRule_RequiresCandidatesOnlyWhenActive(int weight, int minimum, bool requiresCandidate)
        {
            var asset = CreateMap(7, (80, 1), (9, 1), (7, 1), (weight, minimum));
            var map = (StageMapData)asset.GetData();
            map.Validate();
            var rule = map.StageDistribution.Single(r => r.Kind == ChapterStageKind.Enhancement);
            Assert.That(rule.IsActive, Is.EqualTo(requiresCandidate));
            if (requiresCandidate)
            {
                var failure = Assert.Throws<ArgumentException>(() => new ChapterStagePool(rule, Array.Empty<int>()));
                Assert.That(failure.Message, Does.Contain("Enhancement").And.Contain("후보 스테이지"));
            }
            else
            {
                Assert.That(new ChapterStagePool(rule, Array.Empty<int>()).StageKeys, Is.Empty);
            }
            int[] keys = { 1402, 1401 };
            var pool = new ChapterStagePool(rule, keys);
            keys[0] = 9999;
            Assert.That(pool.StageKeys, Is.EqualTo(new[] { 1401, 1402 }));
            Assert.That(pool.Rule, Is.SameAs(rule));
        }

        private StageMapDataSO CreateMap(int depth, (int weight, int minimum) combat, (int weight, int minimum) shop,
            (int weight, int minimum) recovery, (int weight, int minimum) enhancement)
        {
            var asset = ScriptableObject.CreateInstance<StageMapDataSO>();
            _owned.Add(asset);
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("mapKey").intValue = 7;
            serialized.FindProperty("depthCount").intValue = depth;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SetDistribution(asset, "combatDistribution", combat.weight, combat.minimum);
            SetDistribution(asset, "shopDistribution", shop.weight, shop.minimum);
            SetDistribution(asset, "recoveryDistribution", recovery.weight, recovery.minimum);
            SetDistribution(asset, "enhancementDistribution", enhancement.weight, enhancement.minimum);
            return asset;
        }

        private static void SetDistribution(StageMapDataSO asset, string field, int weight, int minimum)
        {
            var serialized = new SerializedObject(asset);
            serialized.FindProperty(field + ".weight").intValue = weight;
            serialized.FindProperty(field + ".minimumCount").intValue = minimum;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static StageMapData Map(int depth, IReadOnlyList<ChapterStageDistributionRule> rules, bool singleFirstLayer = false, int firstStageKey = 0)
            => new(7, depth, 3, 1, singleFirstLayer, firstStageKey, 0, 0, StageConnectionAlgorithm.Nearest, 1, 1, 0, false, 0, rules);

        private static ChapterStageDistributionRule[] ValidRules() => new[]
        {
            new ChapterStageDistributionRule(ChapterStageKind.Combat, 80, 1, 1),
            new ChapterStageDistributionRule(ChapterStageKind.Shop, 9, 1, 1),
            new ChapterStageDistributionRule(ChapterStageKind.Recovery, 7, 1, 1),
            new ChapterStageDistributionRule(ChapterStageKind.Enhancement, 4, 1, 1)
        };
    }
}
