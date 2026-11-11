using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 줄 바꾸기(추가 연결)와 맵당 최소 갈래를 확인한다. Nearest는 기본 연결이 난수를 쓰지 않아
    /// 확률·최소 갈래를 바꿔도 기본 연결이 같으므로, 두 결과의 차이가 곧 이 규칙이 더한 선이다.
    /// </summary>
    public sealed class ChapterGraphLaneChangeTests
    {
        private const int Boss = 99;

        [TestCase(3, 2, false)]
        [TestCase(3, 1, false)]
        [TestCase(3, 2, true)]
        [TestCase(6, 1, true)]
        public void Nearest_LaneChangeAddsOneNeighbourEdgeToSingleEdgeNodes_WithoutCrossing(int widthMax, int widthMin, bool singleFirstLayer)
        {
            int added = 0;
            for (int seed = 0; seed < 64; seed++)
            {
                var plain = ChapterGraphGenerator.Generate(Spec(seed, 0f, 0, widthMax, widthMin, singleFirstLayer));
                var changed = ChapterGraphGenerator.Generate(Spec(seed, 1f, 0, widthMax, widthMin, singleFirstLayer));
                Assert.That(Shape(changed), Is.EqualTo(Shape(plain)), $"seed {seed}");

                var before = plain.ToDictionary(n => n.NodeId);
                var byId = changed.ToDictionary(n => n.NodeId);
                foreach (var node in changed)
                {
                    var baseEdges = before[node.NodeId].NextNodeIds;
                    Assert.That(node.NextNodeIds, Is.SupersetOf(baseEdges), $"seed {seed}, node {node.NodeId}");
                    var extra = node.NextNodeIds.Except(baseEdges).ToArray();
                    if (extra.Length == 0) continue;
                    added++;
                    Assert.That(extra.Length, Is.EqualTo(1), $"seed {seed}, node {node.NodeId}");
                    Assert.That(baseEdges.Count, Is.EqualTo(1), $"seed {seed}, node {node.NodeId}");
                    Assert.That(Math.Abs(byId[extra[0]].WidthIndex - byId[baseEdges[0]].WidthIndex), Is.EqualTo(1),
                        $"seed {seed}, node {node.NodeId}");
                }
                AssertNoCrossing(changed, seed);
            }
            Assert.That(added, Is.GreaterThan(0));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void MinimumForks_ChapterShape_ReachesMinimumByAddingEdgesOnly(int minimum)
        {
            int lacking = 0;
            for (int seed = 0; seed < 200; seed++)
            {
                var plain = ChapterGraphGenerator.Generate(ChapterShape(seed, 0));
                var ensured = ChapterGraphGenerator.Generate(ChapterShape(seed, minimum));
                int forks = Forks(plain);
                if (forks < minimum) lacking++;

                Assert.That(Shape(ensured), Is.EqualTo(Shape(plain)), $"seed {seed}");
                Assert.That(Forks(ensured), Is.EqualTo(Math.Max(minimum, forks)), $"seed {seed}");
                var before = plain.ToDictionary(n => n.NodeId);
                int addedEdges = 0;
                foreach (var node in ensured)
                {
                    Assert.That(node.NextNodeIds, Is.SupersetOf(before[node.NodeId].NextNodeIds), $"seed {seed}");
                    addedEdges += node.NextNodeIds.Count - before[node.NodeId].NextNodeIds.Count;
                }
                Assert.That(addedEdges, Is.EqualTo(Math.Max(0, minimum - forks)), $"seed {seed}");
                Assert.That(ensured.All(n => n.NextNodeIds.Count <= 2 || before[n.NodeId].NextNodeIds.Count > 2), Is.True, $"seed {seed}");
                AssertNoCrossing(ensured, seed);
            }
            Assert.That(lacking, Is.GreaterThan(0), "최소 갈래가 모자란 시드가 있어야 보장이 확인됩니다.");
        }

        [Test]
        public void MinimumForks_ChapterShape_EveryMapOffersALaneChange()
        {
            for (int seed = 0; seed < 200; seed++)
                Assert.That(Forks(ChapterGraphGenerator.Generate(ChapterShape(seed, 1))), Is.GreaterThanOrEqualTo(1), $"seed {seed}");
        }

        [Test]
        public void MinimumForks_DoNotDependOnPools()
        {
            var combatOnly = new[] { Pool(ChapterStageKind.Combat, 1, 0, 10, 11) };
            var mixed = new[]
            {
                Pool(ChapterStageKind.Combat, 5, 1, 10, 11),
                Pool(ChapterStageKind.Shop, 1, 2, 20),
                Pool(ChapterStageKind.Recovery, 0, 1, 30)
            };
            for (int seed = 0; seed < 64; seed++)
            {
                var first = ChapterGraphGenerator.Generate(ChapterShape(seed, 2, combatOnly));
                var second = ChapterGraphGenerator.Generate(ChapterShape(seed, 2, mixed));
                Assert.That(Topology(second), Is.EqualTo(Topology(first)), $"seed {seed}");
            }
        }

        [Test]
        public void MinimumForks_ImpossibleShape_KeepsGraphWithoutForks()
        {
            for (int seed = 0; seed < 16; seed++)
            {
                IReadOnlyList<ChapterGraphNode> nodes = null;
                Assert.DoesNotThrow(() => nodes = ChapterGraphGenerator.Generate(Spec(seed, 1f, 3, 1, 1, false)));
                Assert.That(Forks(nodes), Is.Zero);
                Assert.That(nodes.All(n => n.NextNodeIds.Count <= 1), Is.True);
            }
        }

        [Test]
        public void Spec_RejectsNegativeMinimumForks()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChapterGraphSpec(6, 3, 2, true, 1, ChapterGraphConnection.Nearest,
                10f, 1f, 0.1f, new[] { Pool(ChapterStageKind.Combat, 1, 0, 10) }, Boss, minimumBranchCount: -1));
        }

        // Chapter1·2와 같은 꼴(깊이 6, 폭 2~3, 첫 층 노드 1개, Nearest, 줄 바꾸기 10%)이다.
        private static ChapterGraphSpec ChapterShape(int seed, int minimum, IReadOnlyList<ChapterStagePool> pools = null)
            => new(6, 3, 2, true, seed, ChapterGraphConnection.Nearest, 10f, 1f, 0.1f,
                pools ?? new[] { Pool(ChapterStageKind.Combat, 1, 0, 10, 11) }, Boss, minimumBranchCount: minimum);

        private static ChapterGraphSpec Spec(int seed, float chance, int minimum, int widthMax, int widthMin, bool singleFirstLayer)
            => new(7, widthMax, widthMin, singleFirstLayer, seed, ChapterGraphConnection.Nearest, 10f, 1f, chance,
                new[] { Pool(ChapterStageKind.Combat, 1, 0, 10, 11) }, Boss, minimumBranchCount: minimum);

        private static ChapterStagePool Pool(ChapterStageKind kind, int weight, int minimum, params int[] keys)
            => new(new ChapterStageDistributionRule(kind, weight, minimum, 1), keys);

        // 갈래 노드: 노드가 둘 이상인 층에서 다음 노드 둘 이상과 이어진 노드.
        private static int Forks(IReadOnlyList<ChapterGraphNode> nodes)
            => nodes.GroupBy(n => n.Depth).Where(layer => layer.Count() > 1).Sum(layer => layer.Count(n => n.NextNodeIds.Count > 1));

        private static void AssertNoCrossing(IReadOnlyList<ChapterGraphNode> nodes, int seed)
        {
            var byId = nodes.ToDictionary(n => n.NodeId);
            foreach (var layer in nodes.GroupBy(n => n.Depth))
            {
                var edges = layer.SelectMany(n => n.NextNodeIds.Select(next => (From: n.WidthIndex, To: byId[next].WidthIndex))).ToArray();
                foreach (var a in edges)
                foreach (var b in edges)
                    Assert.That((a.From < b.From && a.To > b.To) || (a.From > b.From && a.To < b.To), Is.False,
                        $"seed {seed}, depth {layer.Key}: {a.From}->{a.To} 와 {b.From}->{b.To} 교차");
            }
        }

        private static string[] Shape(IReadOnlyList<ChapterGraphNode> nodes)
            => nodes.Select(n => $"{n.NodeId}:{n.Depth}:{n.WidthIndex}").ToArray();

        private static string[] Topology(IReadOnlyList<ChapterGraphNode> nodes)
            => nodes.Select(n => $"{n.NodeId}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}").ToArray();
    }
}
