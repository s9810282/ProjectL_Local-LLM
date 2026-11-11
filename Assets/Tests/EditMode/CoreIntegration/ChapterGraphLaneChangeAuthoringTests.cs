using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;
using UnityEditor;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>줄 바꾸기 확률·맵당 최소 갈래가 SO → Data → 스테이지 생성기까지 그대로 가는지 확인한다.</summary>
    public sealed class ChapterGraphLaneChangeAuthoringTests
    {
        [Test]
        public void NewMap_DefaultsToTenPercentLaneChangeAndOneFork()
        {
            using var fixture = new CoreAuthoringFixture();
            var map = (StageMapData)fixture.Create<StageMapDataSO>().GetData();

            Assert.That(map.AdditionalConnectionChance, Is.EqualTo(0.1f));
            Assert.That(map.MinimumBranchCount, Is.EqualTo(1));
            Assert.DoesNotThrow(map.Validate);
        }

        [Test]
        public void GetData_CarriesAuthoredMinimumForks()
        {
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<StageMapDataSO>();
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty("minimumBranchCount");
            Assert.That(property, Is.Not.Null, "맵 SO에 최소 갈래 직렬화 필드가 필요합니다.");
            property.intValue = 3;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(((StageMapData)asset.GetData()).MinimumBranchCount, Is.EqualTo(3));
        }

        [Test]
        public void Validate_RejectsNegativeMinimumForks()
        {
            var data = new StageMapData(1, 6, 3, 2, true, 0, 0.1f, -1, StageConnectionAlgorithm.Nearest, 10f, 1f, 0, false, 0,
                ChapterDistributionTestData.Rules((80, 1), (9, 1), (7, 1), (7, 1)));

            Assert.Throws<InvalidOperationException>(data.Validate);
        }

        [Test]
        public void MinimumForks_ReachStageGeneratorAndOnlyAddEdges()
        {
            var data = ChapterDistributionTestData.Standard();
            var rules = ChapterDistributionTestData.Rules((80, 1), (9, 1), (7, 1), (4, 1));
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());
            try
            {
                runtime.InitData(data);
                runtime.PostBindService();
                var generator = runtime.GetService(InGameSystemGroupServiceKey.Core).StageGeneratorSystem;
                // 시험 데이터는 WeightedRandom이라 기본 연결이 이미 갈래를 자주 만든다. 넉넉한 최소 수로 더 이어지는지를 본다.
                int increased = 0;
                for (int seed = 0; seed < 32; seed++)
                {
                    data.SetMap(1, seed, rules, depth: 7, widthMin: 2);
                    var before = generator.PrepareGraph(1);
                    data.SetMap(1, seed, rules, depth: 7, widthMin: 2, minimumBranchCount: 10);
                    var after = generator.PrepareGraph(1);

                    var plain = before.ToDictionary(node => node.NodeId);
                    foreach (var node in after)
                        Assert.That(node.NextNodeIds, Is.SupersetOf(plain[node.NodeId].NextNodeIds), $"seed {seed}");
                    Assert.That(Forks(after), Is.GreaterThanOrEqualTo(Forks(before)), $"seed {seed}");
                    if (Forks(after) > Forks(before)) increased++;
                }
                Assert.That(increased, Is.GreaterThan(0), "최소 갈래가 생성기까지 전달되어 갈래가 늘어난 시드가 있어야 합니다.");
            }
            finally
            {
                runtime.Shutdown();
            }
        }

        // 갈래 노드: 노드가 둘 이상인 층에서 다음 노드 둘 이상과 이어진 노드.
        private static int Forks(IReadOnlyList<StageProgressData> nodes)
            => nodes.GroupBy(node => node.Depth).Where(layer => layer.Count() > 1).Sum(layer => layer.Count(node => node.NextNodeIds.Count > 1));
    }

    // 운영 에셋 확인은 테스트 소유 SO·계산 검증과 분리합니다.
    public sealed class ChapterGraphLaneChangeProductionAssetTests
    {
        [TestCase(1)]
        [TestCase(2)]
        public void ProductionChapter_UsesTenPercentLaneChangeAndOneFork(int chapter)
        {
            var asset = AssetDatabase.LoadAssetAtPath<StageMapDataSO>(
                $"Assets/ProjectL/DataBaseSO/InGameGroup/StageMap/Item/StageMap_Chapter{chapter}.asset");
            Assert.That(asset, Is.Not.Null);
            var data = (StageMapData)asset.GetData();

            Assert.DoesNotThrow(data.Validate);
            Assert.That(data.ConnectionAlgorithm, Is.EqualTo(StageConnectionAlgorithm.Nearest));
            Assert.That(data.AdditionalConnectionChance, Is.EqualTo(0.1f));
            Assert.That(data.MinimumBranchCount, Is.EqualTo(1));
        }
    }
}
