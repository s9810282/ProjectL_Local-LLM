using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ChapterStageRepeatReductionAuthoringTests
    {
        private static readonly string[] Fields =
            { "combatDistribution", "shopDistribution", "recoveryDistribution", "enhancementDistribution" };

        [Test]
        public void NewMap_DefaultsReduceFacilitiesButAllowCombat()
        {
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<StageMapDataSO>();
            var map = (StageMapData)asset.GetData();

            Assert.That(map.StageDistribution.Select(rule => rule.ReduceConsecutive),
                Is.EqualTo(new[] { false, true, true, true }));
            Assert.DoesNotThrow(map.Validate);
        }

        [Test]
        public void GetData_PreservesIndependentFlagsAndPreviouslyReturnedValues()
        {
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<StageMapDataSO>();
            for (int i = 0; i < Fields.Length; i++) SetFlag(asset, Fields[i], i == 2);
            var first = (StageMapData)asset.GetData();
            for (int i = 0; i < Fields.Length; i++) SetFlag(asset, Fields[i], i == 1 || i == 3);
            var second = (StageMapData)asset.GetData();

            Assert.That(first.StageDistribution.Select(rule => rule.ReduceConsecutive),
                Is.EqualTo(new[] { false, false, true, false }));
            Assert.That(second.StageDistribution.Select(rule => rule.ReduceConsecutive),
                Is.EqualTo(new[] { false, true, false, true }));
        }

        [Test]
        public void LegacyAsset_MissingNestedFlagsRemainDisabledAfterImport()
        {
            string path = $"Assets/ChapterStageRepeatReductionLegacy_{Guid.NewGuid():N}.asset";
            var asset = ScriptableObject.CreateInstance<StageMapDataSO>();
            try
            {
                AssetDatabase.CreateAsset(asset, path);
                // 테스트가 만든 SO만 저장하고 새 필드를 제외해 이전 직렬화 형식을 재현합니다.
                AssetDatabase.SaveAssetIfDirty(asset);
                string yaml = File.ReadAllText(path);
                Assert.That(yaml, Does.Contain("combatDistribution:"));
                string legacy = Regex.Replace(yaml, @"(?m)^    reduceConsecutive: [01]\r?\n", "");
                Resources.UnloadAsset(asset);
                asset = null;
                File.WriteAllText(path, legacy);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                var loaded = AssetDatabase.LoadAssetAtPath<StageMapDataSO>(path);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(((StageMapData)loaded.GetData()).StageDistribution.Select(rule => rule.ReduceConsecutive),
                    Is.EqualTo(new[] { false, false, false, false }));
            }
            finally
            {
                if (AssetDatabase.LoadAssetAtPath<StageMapDataSO>(path) != null) AssetDatabase.DeleteAsset(path);
                else if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [TestCase(ChapterStageKind.Shop, 1201)]
        [TestCase(ChapterStageKind.Recovery, 1301)]
        [TestCase(ChapterStageKind.Enhancement, 1202)]
        public void AuthoredFlag_ReachesStageGeneratorAndReducesThatKind(ChapterStageKind kind, int targetKey)
        {
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<StageMapDataSO>();
            for (int i = 0; i < Fields.Length; i++)
            {
                var serialized = new SerializedObject(asset);
                serialized.FindProperty(Fields[i] + ".weight").intValue = i == 0 ? 1 : 0;
                serialized.FindProperty(Fields[i] + ".minimumCount").intValue = i + 1 == (int)kind ? 3 : i == 1 || i == 2 ? 1 : 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                SetFlag(asset, Fields[i], false);
            }
            var disabled = (StageMapData)asset.GetData();
            SetFlag(asset, Fields[(int)kind - 1], true);
            var enabled = (StageMapData)asset.GetData();
            var data = ChapterDistributionTestData.Standard();
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());
            try
            {
                runtime.InitData(data);
                runtime.PostBindService();
                var generator = runtime.GetService(InGameSystemGroupServiceKey.Core).StageGeneratorSystem;
                int improved = 0;
                for (int seed = 0; seed < 32; seed++)
                {
                    data.SetMap(1, seed, disabled.StageDistribution, depth: 8);
                    var before = generator.PrepareGraph(1);
                    data.SetMap(1, seed, enabled.StageDistribution, depth: 8);
                    var after = generator.PrepareGraph(1);
                    Assert.That(Topology(after), Is.EqualTo(Topology(before)));
                    Assert.That(after.Count(node => node.StageKey == targetKey), Is.EqualTo(3));
                    Assert.That(Score(after, targetKey), Is.LessThanOrEqualTo(Score(before, targetKey)));
                    if (Score(after, targetKey) < Score(before, targetKey)) improved++;
                }
                Assert.That(improved, Is.GreaterThan(0));
            }
            finally
            {
                runtime.Shutdown();
            }
        }

        private static void SetFlag(StageMapDataSO asset, string field, bool value)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty(field + ".reduceConsecutive");
            Assert.That(property, Is.Not.Null, "출현 설정에 연속 완화 직렬화 필드가 필요합니다.");
            property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string[] Topology(IReadOnlyList<StageProgressData> nodes)
            => nodes.Select(node => $"{node.NodeId}:{node.Depth}:{node.WidthIndex}:{string.Join(",", node.NextNodeIds)}").ToArray();

        // 실제 연결 경로를 직접 열거하여 SO 설정이 생성 결과까지 전달됐는지 확인합니다.
        private static long Score(IReadOnlyList<StageProgressData> graph, int key)
        {
            var nodes = graph.ToDictionary(node => node.NodeId);
            long score = 0;
            foreach (var first in graph.Where(node => node.StageKey == key))
            {
                foreach (int next in first.NextNodeIds)
                {
                    var second = nodes[next];
                    if (second.StageKey != key) continue;
                    score++;
                    foreach (int last in second.NextNodeIds)
                        if (nodes[last].StageKey == key) score++;
                }
            }
            return score;
        }
    }

    // 운영 에셋 확인은 테스트 소유 SO·계산 검증과 분리합니다.
    public sealed class ChapterStageRepeatReductionProductionAssetTests
    {
        [TestCase(1)]
        [TestCase(2)]
        public void ProductionChapter_EnablesOnlyFacilityReduction(int chapter)
        {
            var asset = AssetDatabase.LoadAssetAtPath<StageMapDataSO>(
                $"Assets/ProjectL/DataBaseSO/InGameGroup/StageMap/Item/StageMap_Chapter{chapter}.asset");
            Assert.That(asset, Is.Not.Null);
            var data = (StageMapData)asset.GetData();
            Assert.DoesNotThrow(data.Validate);
            Assert.That(data.StageDistribution.Select(rule => rule.ReduceConsecutive),
                Is.EqualTo(new[] { false, true, true, true }));
        }
    }
}
