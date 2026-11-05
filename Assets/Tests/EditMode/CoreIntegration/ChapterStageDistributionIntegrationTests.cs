using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.Development;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;

namespace ProjectL.Tests.CoreIntegration
{
    // 실제 어댑터(StageGeneratorSystem)와 챕터 진입 게시 경계를 거친다. 분포 규칙 자체는 Core 시험이 맡는다.
    public sealed class ChapterStageDistributionIntegrationTests
    {
        private readonly List<Harness> _harnesses = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Harness harness in _harnesses) harness.Dispose();
            _harnesses.Clear();
        }

        [Test]
        public void TrainingDefinition_UsesEnhancementDistributionAndPreservesOrdinaryMinimum()
        {
            var data = ChapterDistributionTestData.Standard();
            data.Add(new StageRootData(1202, 1, StageMapRole.Regular,
                new EnhancementStageDetail(new TrainingPrices(10, 20))));
            data.SetMap(1, 192, ChapterDistributionTestData.Rules((0, 0), (0, 1), (0, 1), (100, 1)));
            var middle = Middle(Start(data).Generator.PrepareGraph(1));

            Assert.That(middle.Count(n => n.StageKey == 1201), Is.EqualTo(1));
            Assert.That(middle.Count(n => n.StageKey == 1202), Is.GreaterThanOrEqualTo(1));
            Assert.That(middle.Where(n => n.StageKey == 1202).All(n => n.StageType == StageType.Shop), Is.True);
        }

        [Test]
        public void PrepareGraph_SeparatesOrdinaryShopFromEnhancement()
        {
            var data = ChapterDistributionTestData.Standard();
            var harness = Start(data);
            for (int seed = 0; seed < 16; seed++)
            {
                // 상점 가중치 0·최소 1, 강화 가중치를 크게 둬도 일반 상점은 정확히 하나다.
                data.SetMap(1, seed, ChapterDistributionTestData.Rules((10, 0), (0, 1), (10, 1), (80, 0)));
                var middle = Middle(harness.Generator.PrepareGraph(1));

                Assert.That(middle.Count(n => n.StageKey == 1201), Is.EqualTo(1), $"seed {seed}");
                Assert.That(middle.Count(n => n.StageKey == 1301), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                Assert.That(middle.Where(n => n.StageKey == 1201 || n.StageKey == 1202).All(n => n.StageType == StageType.Shop), Is.True);
                Assert.That(middle.All(n => n.MapRole == StageMapRole.Regular), Is.True);
            }
        }

        // 맵 SO의 최소 폭·출현 시작 깊이가 어댑터를 거쳐 생성에 쓰인다(챕터 분포 설계 §13).
        [Test]
        public void PrepareGraph_UsesWidthMinAndStartDepth()
        {
            var data = ChapterDistributionTestData.Standard();
            var rules = new[]
            {
                new ChapterStageDistributionRule(ChapterStageKind.Combat, 80, 1, 1),
                new ChapterStageDistributionRule(ChapterStageKind.Shop, 9, 1, 2),
                new ChapterStageDistributionRule(ChapterStageKind.Recovery, 7, 1, 2),
                new ChapterStageDistributionRule(ChapterStageKind.Enhancement, 7, 1, 2)
            };
            var harness = Start(data);
            for (int seed = 0; seed < 32; seed++)
            {
                data.SetMap(1, seed, rules, depth: 6, widthMin: 2);
                var middle = Middle(harness.Generator.PrepareGraph(1));

                Assert.That(middle.GroupBy(n => n.Depth).All(layer => layer.Count() >= 2), Is.True, $"seed {seed}");
                Assert.That(middle.Where(n => n.Depth == 1).All(n => n.StageType == StageType.Combat), Is.True, $"seed {seed}");
                Assert.That(middle.Count(n => n.StageKey == 1201), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                Assert.That(middle.Count(n => n.StageKey == 1301), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                Assert.That(middle.Count(n => n.StageKey == 1202), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
            }
        }

        [Test]
        public void PrepareGraph_SingleFirstLayer_MakesOneCombatNodeAfterStart()
        {
            var data = ChapterDistributionTestData.Standard();
            var rules = new[]
            {
                new ChapterStageDistributionRule(ChapterStageKind.Combat, 80, 1, 1),
                new ChapterStageDistributionRule(ChapterStageKind.Shop, 9, 1, 2),
                new ChapterStageDistributionRule(ChapterStageKind.Recovery, 7, 1, 2),
                new ChapterStageDistributionRule(ChapterStageKind.Enhancement, 7, 1, 2)
            };
            var harness = Start(data);
            for (int seed = 0; seed < 32; seed++)
            {
                data.SetMap(1, seed, rules, depth: 6, widthMin: 2, singleFirstLayer: true);
                var middle = Middle(harness.Generator.PrepareGraph(1));

                Assert.That(middle.Count(n => n.Depth == 1), Is.EqualTo(1), $"seed {seed}");
                Assert.That(middle.Single(n => n.Depth == 1).StageType, Is.EqualTo(StageType.Combat), $"seed {seed}");
                Assert.That(middle.Where(n => n.Depth > 1).GroupBy(n => n.Depth).All(layer => layer.Count() >= 2), Is.True, $"seed {seed}");
            }
        }

        [Test]
        public void PrepareGraph_FirstStageKey_PinsDepthOne_AndKeepsItOffOtherNodes()
        {
            var data = ChapterDistributionTestData.Standard();
            var rules = new[]
            {
                new ChapterStageDistributionRule(ChapterStageKind.Combat, 80, 1, 1),
                new ChapterStageDistributionRule(ChapterStageKind.Shop, 9, 1, 2),
                new ChapterStageDistributionRule(ChapterStageKind.Recovery, 7, 1, 2),
                new ChapterStageDistributionRule(ChapterStageKind.Enhancement, 7, 1, 2)
            };
            var harness = Start(data);
            for (int seed = 0; seed < 32; seed++)
            {
                data.SetMap(1, seed, rules, depth: 6, widthMin: 2, singleFirstLayer: true, firstStageKey: 101);
                var middle = Middle(harness.Generator.PrepareGraph(1));

                Assert.That(middle.Single(n => n.Depth == 1).StageKey, Is.EqualTo(101), $"seed {seed}");
                Assert.That(middle.Single(n => n.Depth == 1).StageType, Is.EqualTo(StageType.Combat), $"seed {seed}");
                Assert.That(middle.Count(n => n.StageKey == 101), Is.EqualTo(1), $"seed {seed}");
            }
        }

        [Test]
        public void PrepareGraph_FirstStageKeyOutsideMap_NamesTheMap()
        {
            var data = ChapterDistributionTestData.Standard();
            data.SetMap(1, 3, ChapterDistributionTestData.Rules((80, 1), (9, 1), (7, 1), (4, 1)), singleFirstLayer: true, firstStageKey: 999);
            var harness = Start(data);

            var error = Assert.Throws<InvalidOperationException>(() => harness.Generator.PrepareGraph(1));
            StringAssert.Contains("Map 1", error.Message);
            StringAssert.Contains("999", error.Message);
        }

        [Test]
        public void EnhancementOnly_CannotSatisfyShopMinimum()
        {
            var data = ChapterDistributionTestData.Standard();
            data.Remove(1201);
            var harness = Start(data);

            var error = Assert.Throws<InvalidOperationException>(() => harness.Generator.PrepareGraph(1));

            StringAssert.Contains("Map 1", error.Message);
            Assert.That(error.InnerException, Is.InstanceOf<ArgumentException>());
        }

        [TestCase(1201, StageType.Shop)]
        [TestCase(101, StageType.Combat)]
        public void PinnedStage_ThroughAdapter_KeepsTopologyAndMinimums(int stageKey, StageType expectedType)
        {
            var data = ChapterDistributionTestData.Standard();
            var harness = Start(data);
            for (int seed = 0; seed < 16; seed++)
            {
                data.SetMap(1, seed, ChapterDistributionTestData.Rules((80, 1), (9, 1), (7, 1), (4, 1)));
                var free = harness.Generator.PrepareGraph(1);
                var pinned = harness.Generator.PrepareGraph(1, Pin(2, stageKey));

                var node = pinned.Single(n => n.NodeId == 2);
                Assert.That((node.StageKey, node.StageType, node.MapRole), Is.EqualTo((stageKey, expectedType, StageMapRole.Regular)));
                Assert.That(Topology(pinned), Is.EqualTo(Topology(free)), $"seed {seed}");
                Assert.That(Middle(pinned).Count(n => n.StageKey == 1201), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                Assert.That(Middle(pinned).Count(n => n.StageKey == 1301), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
            }
        }

        [TestCase("missing-recovery")]
        [TestCase("unknown-detail")]
        [TestCase("two-bosses")]
        [TestCase("cross-map-pin")]
        [TestCase("missing-pin-node")]
        public void InvalidInput_RejectsEntryWithoutPublishing(string fault)
        {
            var data = ChapterDistributionTestData.Standard();
            ChapterStagePin pin = null;
            if (fault == "missing-recovery") data.Remove(1301);
            else if (fault == "unknown-detail") data.Add(new StageRootData(1401, 1, StageMapRole.Regular, new UnknownStageDetail()));
            else if (fault == "two-bosses") data.Add(ChapterDistributionTestData.CombatStage(2102, 1, StageMapRole.Boss));
            else if (fault == "cross-map-pin")
            {
                data.Add(ChapterDistributionTestData.CombatStage(3101, 2, StageMapRole.Regular));
                pin = Pin(2, 3101);
            }
            else pin = Pin(999, 101);
            var harness = Start(data, prepareRun: true);
            var snapshot = new CoreIntegrationSnapshot(harness.Buffers);

            Assert.Throws<InvalidOperationException>(() => harness.Core.ChapterProgression.EnterChapter(1, pin));

            snapshot.AssertUnchanged(harness.Buffers);
        }

        [Test]
        public void Prepare_DoesNotPublish_AndEntryPublishesThePreparedGraph()
        {
            var harness = Start(ChapterDistributionTestData.Standard(), prepareRun: true);
            var snapshot = new CoreIntegrationSnapshot(harness.Buffers);

            var prepared = harness.Generator.PrepareGraph(1);
            snapshot.AssertUnchanged(harness.Buffers);

            Assert.That(harness.Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            Assert.That(Describe(harness.Buffers.StageProgressReadSink.GetStages(1)), Is.EqualTo(Describe(prepared)));
        }

        [Test]
        public void EachMap_UsesItsOwnPolicy()
        {
            var data = ChapterDistributionTestData.Standard();
            data.SetMap(1, 3, ChapterDistributionTestData.Rules((10, 0), (0, 1), (10, 1), (10, 0)));
            data.SetMap(2, 3, ChapterDistributionTestData.Rules((10, 0), (0, 2), (10, 1), (0, 0)));
            data.Add(ChapterDistributionTestData.CombatStage(3101, 2, StageMapRole.Regular));
            data.Add(ChapterDistributionTestData.ShopStage(3201, 2));
            data.Add(ChapterDistributionTestData.ShopStage(3202, 2));
            data.Add(ChapterDistributionTestData.RecoveryStage(3301, 2));
            data.Add(ChapterDistributionTestData.CombatStage(3901, 2, StageMapRole.Boss));
            var harness = Start(data);

            Assert.That(Middle(harness.Generator.PrepareGraph(1)).Count(n => n.StageType == StageType.Shop && n.StageKey == 1201), Is.EqualTo(1));
            Assert.That(Middle(harness.Generator.PrepareGraph(2)).Count(n => n.StageType == StageType.Shop), Is.EqualTo(2));
        }

        [Test]
        public void PublishedGraph_IsNotRegenerated_WhenSettingsChange()
        {
            var data = ChapterDistributionTestData.Standard();
            var harness = Start(data, prepareRun: true);
            Assert.That(harness.Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            var published = Describe(harness.Buffers.StageProgressReadSink.GetStages(1));

            data.SetMap(1, 777, ChapterDistributionTestData.Rules((1, 0), (50, 3), (50, 1), (0, 0)));
            harness.Core.ChapterProgression.EnterChapter(1);

            Assert.That(Describe(harness.Buffers.StageProgressReadSink.GetStages(1)), Is.EqualTo(published));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TemporaryCoreDatabase_PreparesShopAndRecoveryPerChapter(bool withChapterContinuation)
        {
            var database = new TemporaryCoreDatabase(withChapterContinuation);
            var harness = Start(database);

            foreach (var map in database.GetAllStageMaps())
            {
                var middle = Middle(harness.Generator.PrepareGraph(map.MapKey));
                Assert.That(middle.Count(n => n.StageType == StageType.Shop), Is.EqualTo(1), $"map {map.MapKey}");
                Assert.That(middle.Count(n => n.StageType == StageType.Event), Is.EqualTo(1), $"map {map.MapKey}");
                Assert.That(middle.Count(n => n.StageType == StageType.Combat), Is.EqualTo(1), $"map {map.MapKey}");
            }
        }

        private Harness Start(IDataBaseQueryContext data, bool prepareRun = false)
        {
            var harness = new Harness(data, prepareRun);
            _harnesses.Add(harness);
            return harness;
        }

        private static StageProgressData[] Middle(IReadOnlyList<StageProgressData> nodes)
        {
            int boss = nodes.Max(n => n.Depth);
            return nodes.Where(n => n.Depth > 0 && n.Depth < boss).ToArray();
        }

        private static ChapterStagePin Pin(int node, int stage) => new(new StageNodeId(node), new StageKey(stage));

        private static string[] Topology(IReadOnlyList<StageProgressData> nodes)
            => nodes.Select(n => $"{n.NodeId}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}").ToArray();

        private static string[] Describe(IReadOnlyList<StageProgressData> nodes)
            => nodes.OrderBy(n => n.NodeId)
                .Select(n => $"{n.NodeId}:{n.StageKey}:{n.Depth}:{n.WidthIndex}:{n.StageType}:{n.MapRole}:{string.Join(",", n.NextNodeIds)}")
                .ToArray();

        // 생성 종류에 대응하지 않는 일반 스테이지 상세다. 어댑터가 거절해야 한다.
        private sealed class UnknownStageDetail : IStageData
        {
            public StageType StageType => StageType.Combat;
            public void Validate() { }
        }

        private sealed class Harness : IDisposable
        {
            private readonly FrameworkRuntime _runtime;
            internal IInGameCoreSystemGroupService Core { get; }
            internal IBufferQuery Buffers { get; }
            internal IStageGeneratorSystem Generator => Core.StageGeneratorSystem;

            internal Harness(IDataBaseQueryContext data, bool prepareRun)
            {
                var builder = new SystemBuilderHelper();
                BufferSystemFactory.Create().ExportServices(builder);
                InGameCoreSystemFactory.Create().ExportServices(builder);
                _runtime = new FrameworkRuntime(builder.BuildSystemReader());
                try
                {
                    _runtime.InitData(data);
                    _runtime.PostBindService();
                    Core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
                    Buffers = _runtime.GetService(BufferSystemKey.Buffer);
                    if (!prepareRun) return;
                    // 진입 게시가 읽는 Run·보유 상태를 운영 서비스로 채운다.
                    Assert.That(Core.Characters.InitializeRoster(1).Succeeded, Is.True);
                    CoreIntegrationTestData.InstallDeck(Buffers);
                }
                catch
                {
                    _runtime.Shutdown();
                    throw;
                }
            }

            public void Dispose() => _runtime.Shutdown();
        }
    }
}
