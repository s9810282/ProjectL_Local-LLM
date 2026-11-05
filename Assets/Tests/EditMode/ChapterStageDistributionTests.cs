using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.Core
{
    public sealed class ChapterStageDistributionTests
    {
        private const int Boss = 99;
        private static readonly ChapterGraphConnection[] Connections =
            { ChapterGraphConnection.Nearest, ChapterGraphConnection.Random, ChapterGraphConnection.WeightedRandom };

        [Test]
        public void Spec_RejectsConflictingPools()
        {
            Assert.Throws<ArgumentException>(() => Spec(7, 3, 1, new[]
                { Pool(ChapterStageKind.Combat, 1, 0, 101), Pool(ChapterStageKind.Combat, 1, 0, 102) }));
            Assert.Throws<ArgumentException>(() => Spec(7, 3, 1, new[]
                { Pool(ChapterStageKind.Combat, 1, 0, 101), Pool(ChapterStageKind.Shop, 1, 0, 101) }));
            Assert.Throws<ArgumentException>(() => Spec(7, 3, 1, new[] { Pool(ChapterStageKind.Combat, 1, 0, 101, Boss) }));
            Assert.Throws<ArgumentException>(() => Spec(7, 3, 1, new[] { Pool(ChapterStageKind.Shop, 0, 1, 201) }));
        }

        [Test]
        public void Spec_RejectsMinimumsAboveGuaranteedCapacity()
        {
            var pools = new[]
            {
                Pool(ChapterStageKind.Combat, 1, 0, 101),
                Pool(ChapterStageKind.Shop, 0, 1, 201),
                Pool(ChapterStageKind.Recovery, 0, 1, 301)
            };

            // 깊이 3은 중간 층 하나라 폭이 운 좋게 커도 보장 자리는 하나다.
            Assert.Throws<ArgumentException>(() => Spec(3, 3, 1, pools));
            Assert.DoesNotThrow(() => Spec(4, 3, 1, pools));
        }

        [Test]
        public void Spec_CopiesPoolList()
        {
            var pools = new List<ChapterStagePool>(StandardPools());
            var spec = Spec(7, 3, 1, pools);
            pools.RemoveAt(0);

            Assert.That(spec.StagePools.Count, Is.EqualTo(4));
        }

        [TestCase(ChapterGraphConnection.Nearest)]
        [TestCase(ChapterGraphConnection.Random)]
        [TestCase(ChapterGraphConnection.WeightedRandom)]
        public void Generate_GuaranteesMinimumsAcrossSeedsAndConnectionModes(ChapterGraphConnection connection)
        {
            var pools = StandardPools();
            var kinds = KindsByKey(pools);
            for (int seed = 0; seed < 128; seed++)
            {
                var spec = Spec(7, 3, seed, pools, connection: connection);
                var nodes = ChapterGraphGenerator.Generate(spec);
                var middle = nodes.Where(n => n.Depth > 0 && n.Depth < spec.DepthCount - 1).ToArray();

                Assert.That(middle.All(n => kinds.ContainsKey(n.StageKey)), Is.True, $"seed {seed}");
                Assert.That(middle.Count(n => kinds[n.StageKey] == ChapterStageKind.Shop), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                Assert.That(middle.Count(n => kinds[n.StageKey] == ChapterStageKind.Recovery), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                AssertConnected(nodes, spec.DepthCount);
            }
        }

        [Test]
        public void ZeroWeightMinimum_OccursExactlyOnce_AndInactiveKindNever()
        {
            var pools = new[]
            {
                Pool(ChapterStageKind.Combat, 1, 0, 101, 102),
                Pool(ChapterStageKind.Shop, 0, 1, 201),
                Pool(ChapterStageKind.Recovery, 0, 0, 301)
            };
            for (int seed = 0; seed < 64; seed++)
            {
                var keys = MiddleKeys(ChapterGraphGenerator.Generate(Spec(7, 3, seed, pools)), 7);

                Assert.That(keys.Count(k => k == 201), Is.EqualTo(1), $"seed {seed}");
                Assert.That(keys, Has.No.Member(301), $"seed {seed}");
            }
        }

        [Test]
        public void SinglePositiveWeight_FillsEveryUnreservedSlot()
        {
            var pools = new[] { Pool(ChapterStageKind.Combat, 5, 0, 101), Pool(ChapterStageKind.Shop, 0, 1, 201) };
            for (int seed = 0; seed < 32; seed++)
            {
                var keys = MiddleKeys(ChapterGraphGenerator.Generate(Spec(7, 3, seed, pools)), 7);

                Assert.That(keys.Count(k => k == 101), Is.EqualTo(keys.Length - 1), $"seed {seed}");
            }
        }

        [Test]
        public void MinimumsEqualToCapacity_FillEveryMiddleNode()
        {
            var pools = new[]
            {
                Pool(ChapterStageKind.Combat, 1, 0, 101),
                Pool(ChapterStageKind.Shop, 0, 1, 201),
                Pool(ChapterStageKind.Recovery, 0, 1, 301)
            };
            for (int seed = 0; seed < 32; seed++)
            {
                var keys = MiddleKeys(ChapterGraphGenerator.Generate(Spec(4, 1, seed, pools)), 4);

                Assert.That(keys.OrderBy(k => k), Is.EqualTo(new[] { 201, 301 }), $"seed {seed}");
            }
        }

        [Test]
        public void Pin_PlacesExactStage_KeepsTopologyAndMinimums()
        {
            var pools = StandardPools();
            var kinds = KindsByKey(pools);
            foreach (ChapterGraphConnection connection in Connections)
            {
                for (int seed = 0; seed < 32; seed++)
                {
                    var free = ChapterGraphGenerator.Generate(Spec(7, 3, seed, pools, connection: connection));
                    var pinned = ChapterGraphGenerator.Generate(Spec(7, 3, seed, pools, Pin(2, 102), connection));

                    Assert.That(pinned.Single(n => n.NodeId == 2).StageKey, Is.EqualTo(102));
                    Assert.That(Topology(pinned), Is.EqualTo(Topology(free)));
                    var keys = MiddleKeys(pinned, 7);
                    Assert.That(keys.Count(k => kinds[k] == ChapterStageKind.Shop), Is.GreaterThanOrEqualTo(1));
                    Assert.That(keys.Count(k => kinds[k] == ChapterStageKind.Recovery), Is.GreaterThanOrEqualTo(1));
                }
            }
        }

        [Test]
        public void PinnedShop_CountsTowardShopMinimum()
        {
            var pools = new[]
            {
                Pool(ChapterStageKind.Combat, 1, 0, 101),
                Pool(ChapterStageKind.Shop, 0, 1, 201, 202),
                Pool(ChapterStageKind.Recovery, 0, 1, 301)
            };
            for (int seed = 0; seed < 32; seed++)
            {
                var nodes = ChapterGraphGenerator.Generate(Spec(7, 3, seed, pools, Pin(2, 202)));
                var shops = nodes.Where(n => n.StageKey == 201 || n.StageKey == 202).ToArray();

                Assert.That(shops.Select(n => n.NodeId), Is.EqualTo(new[] { 2 }), $"seed {seed}");
                Assert.That(nodes.Count(n => n.StageKey == 301), Is.EqualTo(1), $"seed {seed}");
            }
        }

        [TestCase(102)]
        [TestCase(401)]
        public void PinnedOtherKind_StillLeavesShopAndRecovery(int pinnedKey)
        {
            var pools = new[]
            {
                Pool(ChapterStageKind.Combat, 1, 0, 101, 102),
                Pool(ChapterStageKind.Shop, 0, 1, 201),
                Pool(ChapterStageKind.Recovery, 0, 1, 301),
                Pool(ChapterStageKind.Enhancement, 0, 0, 401)
            };
            for (int seed = 0; seed < 32; seed++)
            {
                var nodes = ChapterGraphGenerator.Generate(Spec(5, 1, seed, pools, Pin(2, pinnedKey)));

                Assert.That(nodes.Single(n => n.NodeId == 2).StageKey, Is.EqualTo(pinnedKey));
                Assert.That(nodes.Count(n => n.StageKey == 201), Is.EqualTo(1), $"seed {seed}");
                Assert.That(nodes.Count(n => n.StageKey == 301), Is.EqualTo(1), $"seed {seed}");
                Assert.That(nodes.Count(n => n.StageKey == 401), Is.EqualTo(pinnedKey == 401 ? 1 : 0), $"seed {seed}");
            }
        }

        [Test]
        public void Pin_RejectsStartBossMissingNodeAndForeignStage()
        {
            var pools = StandardPools();
            int bossNode = ChapterGraphGenerator.Generate(Spec(7, 3, 5, pools)).Single(n => n.Depth == 6).NodeId;

            Assert.Throws<ArgumentException>(() => ChapterGraphGenerator.Generate(Spec(7, 3, 5, pools, Pin(1, 101))));
            Assert.Throws<ArgumentException>(() => ChapterGraphGenerator.Generate(Spec(7, 3, 5, pools, Pin(bossNode, 101))));
            Assert.Throws<ArgumentException>(() => ChapterGraphGenerator.Generate(Spec(7, 3, 5, pools, Pin(999, 101))));
            Assert.Throws<ArgumentException>(() => Spec(7, 3, 5, pools, Pin(2, 777)));
            Assert.Throws<ArgumentException>(() => Spec(7, 3, 5, pools, Pin(2, Boss)));
        }

        [Test]
        public void PinCapacity_CountsPinnedNodeAgainstMiddleLayers()
        {
            var pools = new[]
            {
                Pool(ChapterStageKind.Combat, 1, 0, 101),
                Pool(ChapterStageKind.Shop, 0, 1, 201),
                Pool(ChapterStageKind.Recovery, 0, 1, 301)
            };

            Assert.Throws<ArgumentException>(() => Spec(4, 3, 1, pools, Pin(2, 101)));
            Assert.DoesNotThrow(() => Spec(4, 3, 1, pools, Pin(2, 201)));
            Assert.DoesNotThrow(() => Spec(5, 3, 1, pools, Pin(2, 101)));
        }

        [Test]
        public void WeightsAndMinimums_DoNotChangeTopology()
        {
            var first = StandardPools();
            var second = new[]
            {
                Pool(ChapterStageKind.Combat, 3, 2, 101, 102, 103, 104, 105),
                Pool(ChapterStageKind.Shop, 0, 1, 201),
                Pool(ChapterStageKind.Recovery, 50, 2, 301),
                Pool(ChapterStageKind.Enhancement, 0, 0, 401)
            };
            foreach (ChapterGraphConnection connection in Connections)
                for (int seed = 0; seed < 32; seed++)
                    Assert.That(Topology(ChapterGraphGenerator.Generate(Spec(7, 3, seed, second, connection: connection))),
                        Is.EqualTo(Topology(ChapterGraphGenerator.Generate(Spec(7, 3, seed, first, connection: connection)))));
        }

        [TestCase(1, 2)]
        [TestCase(2, 5)]
        public void CombatCandidateAddition_DoesNotChangeKindPlacement(int before, int after)
        {
            for (int seed = 0; seed < 64; seed++)
            {
                var small = ChapterGraphGenerator.Generate(Spec(7, 3, seed, PoolsWithCombat(before)));
                var large = ChapterGraphGenerator.Generate(Spec(7, 3, seed, PoolsWithCombat(after)));

                Assert.That(Kinds(large), Is.EqualTo(Kinds(small)), $"seed {seed}");
                // 후보가 하나뿐일 때 키 추첨을 건너뛰면 뒤 노드의 다른 종류 키가 바뀐다.
                Assert.That(NonCombatKeys(large), Is.EqualTo(NonCombatKeys(small)), $"seed {seed}");
            }
        }

        [Test]
        public void WeightChange_KeepsReservedMinimumNodes()
        {
            ChapterStagePool[] Pools(int combat, int enhancement) => new[]
            {
                Pool(ChapterStageKind.Combat, combat, 0, 101),
                Pool(ChapterStageKind.Shop, 0, 1, 201),
                Pool(ChapterStageKind.Recovery, 0, 1, 301),
                Pool(ChapterStageKind.Enhancement, enhancement, 0, 401)
            };
            for (int seed = 0; seed < 64; seed++)
            {
                var first = ChapterGraphGenerator.Generate(Spec(7, 3, seed, Pools(1, 2)));
                var second = ChapterGraphGenerator.Generate(Spec(7, 3, seed, Pools(5, 1)));

                Assert.That(NodeOf(second, 201), Is.EqualTo(NodeOf(first, 201)), $"seed {seed}");
                Assert.That(NodeOf(second, 301), Is.EqualTo(NodeOf(first, 301)), $"seed {seed}");
            }
        }

        [Test]
        public void InputOrder_DoesNotChangeGraph()
        {
            var ordered = StandardPools();
            var shuffled = new[]
            {
                Pool(ChapterStageKind.Enhancement, 10, 0, 401),
                Pool(ChapterStageKind.Recovery, 10, 1, 301),
                Pool(ChapterStageKind.Combat, 70, 0, 105, 103, 101, 104, 102),
                Pool(ChapterStageKind.Shop, 10, 1, 201)
            };
            for (int seed = 0; seed < 32; seed++)
                Assert.That(Describe(ChapterGraphGenerator.Generate(Spec(7, 3, seed, shuffled))),
                    Is.EqualTo(Describe(ChapterGraphGenerator.Generate(Spec(7, 3, seed, ordered)))));
        }

        [Test]
        public void SameSpecIncludingPin_ProducesSameGraph()
        {
            // 같은 값으로 따로 만든 입력 두 개를 비교한다.
            var first = Spec(7, 3, 42, StandardPools(), Pin(3, 401), ChapterGraphConnection.WeightedRandom);
            var second = Spec(7, 3, 42, StandardPools(), Pin(3, 401), ChapterGraphConnection.WeightedRandom);

            Assert.That(Describe(ChapterGraphGenerator.Generate(second)), Is.EqualTo(Describe(ChapterGraphGenerator.Generate(first))));
        }

        [Test]
        public void WidthMin_KeepsEveryMiddleLayerWithinRange()
        {
            foreach (ChapterGraphConnection connection in Connections)
                for (int seed = 0; seed < 64; seed++)
                {
                    var nodes = ChapterGraphGenerator.Generate(Spec(7, 3, seed, StandardPools(), connection: connection, widthMin: 2));
                    var widths = nodes.Where(n => n.Depth > 0 && n.Depth < 6).GroupBy(n => n.Depth).Select(g => g.Count()).ToArray();

                    Assert.That(widths.Length, Is.EqualTo(5), $"seed {seed}");
                    Assert.That(widths.All(w => w >= 2 && w <= 3), Is.True, $"seed {seed}");
                    AssertConnected(nodes, 7);
                }
        }

        [Test]
        public void WidthMinEqualToMax_MakesEveryMiddleLayerFull()
        {
            var nodes = ChapterGraphGenerator.Generate(Spec(7, 3, 5, StandardPools(), widthMin: 3));

            Assert.That(nodes.Count(n => n.Depth > 0 && n.Depth < 6), Is.EqualTo(15));
        }

        [Test]
        public void SingleFirstLayer_MakesDepthOneOneNode_AndKeepsLaterWidths()
        {
            foreach (ChapterGraphConnection connection in Connections)
                for (int seed = 0; seed < 64; seed++)
                {
                    var single = ChapterGraphGenerator.Generate(Spec(6, 3, seed, StartDepthPools(2), connection: connection,
                        widthMin: 2, singleFirstLayer: true));
                    var normal = ChapterGraphGenerator.Generate(Spec(6, 3, seed, StartDepthPools(2), connection: connection,
                        widthMin: 2));
                    int[] Widths(IReadOnlyList<ChapterGraphNode> nodes)
                        => Enumerable.Range(2, 3).Select(depth => nodes.Count(n => n.Depth == depth)).ToArray();

                    Assert.That(single.Count(n => n.Depth == 1), Is.EqualTo(1), $"seed {seed}");
                    Assert.That(single.Single(n => n.Depth == 1).StageKey / 100, Is.EqualTo(1), $"seed {seed}");
                    Assert.That(Widths(single), Is.EqualTo(Widths(normal)), $"seed {seed}");
                    AssertConnected(single, 6);
                }
        }

        [TestCase(0, 3)]
        [TestCase(4, 3)]
        public void Spec_RejectsWidthMinOutsideOneToMax(int widthMin, int widthMax)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Spec(7, widthMax, 1, StandardPools(), widthMin: widthMin));
        }

        [Test]
        public void StartDepth_KeepsKindsAwayFromShallowerNodes_AndKeepsMinimums()
        {
            var pools = StartDepthPools(2);
            var depthOf = new Dictionary<int, int> { [1] = 1, [2] = 2, [3] = 2, [4] = 2 };
            foreach (ChapterGraphConnection connection in Connections)
                for (int seed = 0; seed < 64; seed++)
                {
                    var nodes = ChapterGraphGenerator.Generate(Spec(6, 3, seed, pools, connection: connection));

                    foreach (ChapterGraphNode node in nodes.Where(n => n.Depth > 0 && n.Depth < 5))
                        Assert.That(node.Depth, Is.GreaterThanOrEqualTo(depthOf[node.StageKey / 100]), $"seed {seed} node {node.NodeId}");
                    Assert.That(nodes.Count(n => n.StageKey == 201), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                    Assert.That(nodes.Count(n => n.StageKey == 301), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                    Assert.That(nodes.Count(n => n.StageKey == 401), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
                }
        }

        [Test]
        public void StartDepth_DoesNotChangeTopology()
        {
            foreach (ChapterGraphConnection connection in Connections)
                for (int seed = 0; seed < 32; seed++)
                    Assert.That(Topology(ChapterGraphGenerator.Generate(Spec(6, 3, seed, StartDepthPools(2), connection: connection))),
                        Is.EqualTo(Topology(ChapterGraphGenerator.Generate(Spec(6, 3, seed, StartDepthPools(1), connection: connection)))));
        }

        [Test]
        public void StartDepth_NarrowerKindIsPlacedFirst()
        {
            // 폭 1이라 깊이 2~4는 한 자리씩이다. 강화는 깊이 4에만 놓일 수 있어서,
            // 종류 순서대로 회복이 먼저 고르면 깊이 4를 가져가 강화를 놓을 수 없다.
            var pools = new[]
            {
                Pool(ChapterStageKind.Combat, 1, 0, 101),
                DepthPool(ChapterStageKind.Recovery, 0, 2, 2, 301),
                DepthPool(ChapterStageKind.Enhancement, 0, 1, 4, 401)
            };
            for (int seed = 0; seed < 64; seed++)
            {
                var nodes = ChapterGraphGenerator.Generate(Spec(6, 1, seed, pools));

                Assert.That(nodes.Single(n => n.Depth == 4).StageKey, Is.EqualTo(401), $"seed {seed}");
                Assert.That(nodes.Where(n => n.Depth == 2 || n.Depth == 3).All(n => n.StageKey == 301), Is.True, $"seed {seed}");
                Assert.That(nodes.Single(n => n.Depth == 1).StageKey, Is.EqualTo(101), $"seed {seed}");
            }
        }

        [Test]
        public void Pin_IgnoresStartDepth_AndCountsTowardItsMinimum()
        {
            var pools = new[]
            {
                Pool(ChapterStageKind.Combat, 70, 0, 101),
                DepthPool(ChapterStageKind.Shop, 0, 1, 3, 201),
                DepthPool(ChapterStageKind.Recovery, 0, 1, 2, 301)
            };
            for (int seed = 0; seed < 32; seed++)
            {
                // 노드 2는 늘 깊이 1이다. 상점의 시작 깊이 3보다 얕지만 고정 노드라 놓인다.
                var nodes = ChapterGraphGenerator.Generate(Spec(7, 3, seed, pools, Pin(2, 201)));

                Assert.That(nodes.Single(n => n.NodeId == 2).StageKey, Is.EqualTo(201), $"seed {seed}");
                Assert.That(nodes.Count(n => n.StageKey == 201), Is.EqualTo(1), $"seed {seed}");
                Assert.That(nodes.Where(n => n.StageKey == 301).All(n => n.Depth >= 2), Is.True, $"seed {seed}");
            }
        }

        [Test]
        public void FirstStage_AlwaysOnDepthOne_NeverElsewhere_AndKeepsTopology()
        {
            var pools = StartDepthPools(2);
            foreach (ChapterGraphConnection connection in Connections)
                for (int seed = 0; seed < 64; seed++)
                {
                    var nodes = ChapterGraphGenerator.Generate(Spec(6, 3, seed, pools, connection: connection,
                        widthMin: 2, singleFirstLayer: true, firstStageKey: 101));
                    var plain = ChapterGraphGenerator.Generate(Spec(6, 3, seed, pools, connection: connection,
                        widthMin: 2, singleFirstLayer: true));

                    Assert.That(nodes.Single(n => n.Depth == 1).NodeId, Is.EqualTo(ChapterGraphSpec.FirstLayerNodeId), $"seed {seed}");
                    Assert.That(nodes.Single(n => n.Depth == 1).StageKey, Is.EqualTo(101), $"seed {seed}");
                    Assert.That(nodes.Count(n => n.StageKey == 101), Is.EqualTo(1), $"seed {seed}");
                    Assert.That(Topology(nodes), Is.EqualTo(Topology(plain)), $"seed {seed}");
                    foreach (int key in new[] { 201, 301, 401 })
                        Assert.That(nodes.Count(n => n.StageKey == key), Is.GreaterThanOrEqualTo(1), $"seed {seed} key {key}");
                }
        }

        [Test]
        public void FirstStage_DevPinOnFirstNodeWins_AndPinElsewhereKeepsBoth()
        {
            var pools = StartDepthPools(2);
            for (int seed = 0; seed < 32; seed++)
            {
                var overridden = ChapterGraphGenerator.Generate(Spec(6, 3, seed, pools, Pin(ChapterGraphSpec.FirstLayerNodeId, 102),
                    widthMin: 2, singleFirstLayer: true, firstStageKey: 101));
                Assert.That(overridden.Single(n => n.Depth == 1).StageKey, Is.EqualTo(102), $"seed {seed}");
                Assert.That(overridden.Count(n => n.StageKey == 101), Is.Zero, $"seed {seed}");

                // 노드 3은 늘 깊이 2다(첫 층이 노드 하나).
                var both = ChapterGraphGenerator.Generate(Spec(6, 3, seed, pools, Pin(3, 201),
                    widthMin: 2, singleFirstLayer: true, firstStageKey: 101));
                Assert.That(both.Single(n => n.NodeId == 2).StageKey, Is.EqualTo(101), $"seed {seed}");
                Assert.That(both.Single(n => n.NodeId == 3).StageKey, Is.EqualTo(201), $"seed {seed}");
                Assert.That(both.Count(n => n.StageKey == 101), Is.EqualTo(1), $"seed {seed}");
            }
        }

        [Test]
        public void FirstStage_SoleCandidateOfKind_IsAllowedOnlyWhenTheKindCannotAppearElsewhere()
        {
            var lonelyCombat = new[]
            {
                Pool(ChapterStageKind.Combat, 80, 1, 101),
                Pool(ChapterStageKind.Shop, 10, 1, 201)
            };
            Assert.Throws<ArgumentException>(() => Spec(6, 3, 1, lonelyCombat, singleFirstLayer: true, firstStageKey: 101));

            // 상점은 가중치 0·최소 1이라 첫 층 하나로 끝난다. 다른 노드는 모두 전투다.
            var onceShop = new[]
            {
                Pool(ChapterStageKind.Combat, 80, 0, 101, 102),
                Pool(ChapterStageKind.Shop, 0, 1, 201)
            };
            for (int seed = 0; seed < 32; seed++)
            {
                var nodes = ChapterGraphGenerator.Generate(Spec(6, 3, seed, onceShop, singleFirstLayer: true, firstStageKey: 201));
                Assert.That(nodes.Single(n => n.Depth == 1).StageKey, Is.EqualTo(201), $"seed {seed}");
                Assert.That(nodes.Count(n => n.StageKey == 201), Is.EqualTo(1), $"seed {seed}");
            }
        }

        [Test]
        public void FirstStage_RejectsWithoutSingleFirstLayer_ForeignOrBossStage_AndNegativeKey()
        {
            var pools = StartDepthPools(2);

            Assert.Throws<ArgumentException>(() => Spec(6, 3, 1, pools, widthMin: 2, firstStageKey: 101));
            Assert.Throws<ArgumentException>(() => Spec(6, 3, 1, pools, widthMin: 2, singleFirstLayer: true, firstStageKey: 999));
            Assert.Throws<ArgumentException>(() => Spec(6, 3, 1, pools, widthMin: 2, singleFirstLayer: true, firstStageKey: Boss));
            Assert.Throws<ArgumentOutOfRangeException>(() => Spec(6, 3, 1, pools, widthMin: 2, singleFirstLayer: true, firstStageKey: -1));
        }

        [Test]
        public void ResidualShare_FollowsWeights()
        {
            // 폭 1·깊이 22라 그래프마다 일반 노드 20개, seed 2048개면 40,960개다.
            var free = new[] { Pool(ChapterStageKind.Combat, 1, 0, 101), Pool(ChapterStageKind.Shop, 3, 0, 201) };
            Assert.That(ShopShare(free, reservedShops: 0, residual: 40960), Is.InRange(0.72, 0.78));

            // 전투·상점 최소 1개씩을 빼면 남은 자리는 그래프마다 18개, 모두 36,864개다.
            var reserved = new[] { Pool(ChapterStageKind.Combat, 1, 1, 101), Pool(ChapterStageKind.Shop, 3, 1, 201) };
            Assert.That(ShopShare(reserved, reservedShops: 2048, residual: 36864), Is.InRange(0.72, 0.78));
        }

        private static double ShopShare(ChapterStagePool[] pools, int reservedShops, int residual)
        {
            int shops = 0;
            for (int seed = 0; seed < 2048; seed++)
                shops += ChapterGraphGenerator.Generate(Spec(22, 1, seed, pools)).Count(n => n.StageKey == 201);
            return (double)(shops - reservedShops) / residual;
        }

        private static void AssertConnected(IReadOnlyList<ChapterGraphNode> nodes, int depthCount)
        {
            var byId = nodes.ToDictionary(n => n.NodeId);
            Assert.That(byId.Count, Is.EqualTo(nodes.Count));
            var reachable = new HashSet<int> { 1 };
            foreach (var node in nodes.OrderBy(n => n.Depth))
            {
                Assert.That(node.NextNodeIds.Distinct().Count(), Is.EqualTo(node.NextNodeIds.Count));
                if (node.Depth < depthCount - 1) Assert.That(node.NextNodeIds, Is.Not.Empty);
                foreach (int next in node.NextNodeIds)
                {
                    Assert.That(byId[next].Depth, Is.EqualTo(node.Depth + 1));
                    if (reachable.Contains(node.NodeId)) reachable.Add(next);
                }
            }
            Assert.That(reachable.Count, Is.EqualTo(nodes.Count));
            // 모든 노드가 다음 층으로 하나 이상 이어지므로, 보스 앞까지 모두 보스에 닿는다.
        }

        private static ChapterGraphSpec Spec(int depth, int width, int seed, IReadOnlyList<ChapterStagePool> pools,
            ChapterStagePin pin = null, ChapterGraphConnection connection = ChapterGraphConnection.WeightedRandom, int widthMin = 1,
            bool singleFirstLayer = false, int firstStageKey = 0)
            => new(depth, width, widthMin, singleFirstLayer, seed, connection, 3f, 1f, 0.3f, pools, Boss, pin, firstStageKey);

        private static ChapterStagePool Pool(ChapterStageKind kind, int weight, int minimum, params int[] keys)
            => DepthPool(kind, weight, minimum, 1, keys);

        private static ChapterStagePool DepthPool(ChapterStageKind kind, int weight, int minimum, int minDepth, params int[] keys)
            => new(new ChapterStageDistributionRule(kind, weight, minimum, minDepth), keys);

        // Chapter1 꼴: 전투는 깊이 1부터, 상점·회복·강화는 시작 깊이 specialDepth부터, 모두 최소 1.
        private static ChapterStagePool[] StartDepthPools(int specialDepth) => new[]
        {
            Pool(ChapterStageKind.Combat, 80, 1, 101, 102, 103),
            DepthPool(ChapterStageKind.Shop, 9, 1, specialDepth, 201),
            DepthPool(ChapterStageKind.Recovery, 7, 1, specialDepth, 301),
            DepthPool(ChapterStageKind.Enhancement, 7, 1, specialDepth, 401)
        };

        private static ChapterStagePin Pin(int node, int stage) => new(new StageNodeId(node), new StageKey(stage));

        private static ChapterStagePool[] StandardPools() => new[]
        {
            Pool(ChapterStageKind.Combat, 70, 0, 101, 102, 103, 104, 105),
            Pool(ChapterStageKind.Shop, 10, 1, 201),
            Pool(ChapterStageKind.Recovery, 10, 1, 301),
            Pool(ChapterStageKind.Enhancement, 10, 0, 401)
        };

        private static ChapterStagePool[] PoolsWithCombat(int combatCount) => new[]
        {
            Pool(ChapterStageKind.Combat, 70, 0, Enumerable.Range(101, combatCount).ToArray()),
            Pool(ChapterStageKind.Shop, 10, 1, 201, 202),
            Pool(ChapterStageKind.Recovery, 10, 1, 301, 302),
            Pool(ChapterStageKind.Enhancement, 10, 0, 401, 402)
        };

        private static Dictionary<int, ChapterStageKind> KindsByKey(IEnumerable<ChapterStagePool> pools)
            => pools.SelectMany(p => p.StageKeys.Select(k => (k, p.Rule.Kind))).ToDictionary(x => x.k, x => x.Kind);

        private static int[] MiddleKeys(IReadOnlyList<ChapterGraphNode> nodes, int depthCount)
            => nodes.Where(n => n.Depth > 0 && n.Depth < depthCount - 1).Select(n => n.StageKey).ToArray();

        private static int[] NodeOf(IReadOnlyList<ChapterGraphNode> nodes, int stageKey)
            => nodes.Where(n => n.StageKey == stageKey).Select(n => n.NodeId).ToArray();

        // 키 100번대는 전투, 200번대는 상점, 300번대는 회복, 400번대는 강화다.
        private static string[] Kinds(IReadOnlyList<ChapterGraphNode> nodes)
            => nodes.Select(n => $"{n.NodeId}:{n.StageKey / 100}").ToArray();

        private static string[] NonCombatKeys(IReadOnlyList<ChapterGraphNode> nodes)
            => nodes.Where(n => n.StageKey / 100 != 1).Select(n => $"{n.NodeId}:{n.StageKey}").ToArray();

        private static string[] Topology(IReadOnlyList<ChapterGraphNode> nodes)
            => nodes.Select(n => $"{n.NodeId}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}").ToArray();

        private static string[] Describe(IReadOnlyList<ChapterGraphNode> nodes)
            => nodes.Select(n => $"{n.NodeId}:{n.StageKey}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}").ToArray();
    }
}
