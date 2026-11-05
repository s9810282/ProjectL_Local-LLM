using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.Core
{
    public sealed class ChapterGraphGeneratorTests
    {
        [Test]
        public void SameSpec_ProducesSameGraph()
        {
            var first = ChapterGraphGenerator.Generate(Spec(seed: 77, ChapterGraphConnection.WeightedRandom, chance: 0.5f));
            var second = ChapterGraphGenerator.Generate(Spec(seed: 77, ChapterGraphConnection.WeightedRandom, chance: 0.5f));

            Assert.That(Describe(second), Is.EqualTo(Describe(first)));
        }

        [TestCase(ChapterGraphConnection.Nearest)]
        [TestCase(ChapterGraphConnection.Random)]
        [TestCase(ChapterGraphConnection.WeightedRandom)]
        public void Graph_HasStartLayersAndBoss_AndEveryNodeIsConnected(ChapterGraphConnection connection)
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var spec = Spec(seed, connection, chance: 0.3f);
                var nodes = ChapterGraphGenerator.Generate(spec);
                var byId = nodes.ToDictionary(n => n.NodeId);

                var start = nodes.Single(n => n.Depth == 0);
                Assert.That((start.NodeId, start.StageKey, start.WidthIndex), Is.EqualTo((1, 0, 0)));
                var boss = nodes.Single(n => n.Depth == spec.DepthCount - 1);
                Assert.That(boss.StageKey, Is.EqualTo(spec.BossStageKey));
                Assert.That(boss.NextNodeIds, Is.Empty);

                for (int depth = 1; depth < spec.DepthCount - 1; depth++)
                {
                    var layer = nodes.Where(n => n.Depth == depth).ToArray();
                    Assert.That(layer.Length, Is.InRange(1, spec.WidthMax));
                    Assert.That(layer.All(n => spec.StagePools.SelectMany(p => p.StageKeys).Contains(n.StageKey)), Is.True);
                }

                var incoming = new HashSet<int>();
                foreach (var node in nodes)
                {
                    if (node.Depth < spec.DepthCount - 1) Assert.That(node.NextNodeIds, Is.Not.Empty);
                    Assert.That(node.NextNodeIds.Distinct().Count(), Is.EqualTo(node.NextNodeIds.Count));
                    foreach (int next in node.NextNodeIds)
                    {
                        Assert.That(byId[next].Depth, Is.EqualTo(node.Depth + 1));
                        incoming.Add(next);
                    }
                }
                Assert.That(nodes.Where(n => n.Depth > 0).All(n => incoming.Contains(n.NodeId)), Is.True);
            }
        }

        [Test]
        public void DepthTwo_ConnectsStartDirectlyToBoss()
        {
            var nodes = ChapterGraphGenerator.Generate(new ChapterGraphSpec(2, 3, 1, false, 5, ChapterGraphConnection.Nearest,
                1f, 1f, 0f, Pools(10), 99));

            Assert.That(nodes.Count, Is.EqualTo(2));
            Assert.That(nodes[0].NextNodeIds, Is.EqualTo(new[] { nodes[1].NodeId }));
            Assert.That(nodes[1].StageKey, Is.EqualTo(99));
        }

        [Test]
        public void Spec_CopiesPoolsInKindOrder()
        {
            var pools = new List<ChapterStagePool>
            {
                new(new ChapterStageDistributionRule(ChapterStageKind.Shop, 1, 0, 1), new[] { 20 }),
                new(new ChapterStageDistributionRule(ChapterStageKind.Combat, 1, 0, 1), new[] { 10, 11 })
            };
            var spec = new ChapterGraphSpec(4, 2, 1, false, 1, ChapterGraphConnection.Random, 1f, 1f, 0f, pools, 99);
            pools.Clear();

            Assert.That(spec.StagePools.Select(p => p.Rule.Kind),
                Is.EqualTo(new[] { ChapterStageKind.Combat, ChapterStageKind.Shop }));
        }

        [Test]
        public void ObsoleteConstructor_AlwaysThrowsNotSupported()
        {
#pragma warning disable CS0618
            Assert.Throws<NotSupportedException>(() => new ChapterGraphSpec(4, 2, 1, ChapterGraphConnection.Nearest,
                1f, 1f, 0f, new[] { 10, 11 }, 99));
#pragma warning restore CS0618
        }

        [TestCase(1, 2, 1f, 1f, 0f, 99, TestName = "DepthBelowTwo")]
        [TestCase(4, 0, 1f, 1f, 0f, 99, TestName = "WidthBelowOne")]
        [TestCase(4, 2, 0f, 1f, 0f, 99, TestName = "NonPositiveNearestWeight")]
        [TestCase(4, 2, 1f, -1f, 0f, 99, TestName = "NegativeOtherWeight")]
        [TestCase(4, 2, 1f, 1f, 1.5f, 99, TestName = "ChanceAboveOne")]
        [TestCase(4, 2, 1f, 1f, float.NaN, 99, TestName = "NaNChance")]
        [TestCase(4, 2, 1f, 1f, 0f, 0, TestName = "EmptyBossKey")]
        public void Spec_RejectsInvalidValues(int depth, int width, float nearest, float other, float chance, int boss)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChapterGraphSpec(depth, width, 1, false, 1,
                ChapterGraphConnection.WeightedRandom, nearest, other, chance, Pools(10), boss));
        }

        [Test]
        public void Spec_RejectsMissingOrEmptyPools()
        {
            // null 그대로는 옛 생성자로 묶이므로 형식을 밝힌다.
            Assert.Throws<ArgumentNullException>(() => new ChapterGraphSpec(4, 2, 1, false, 1, ChapterGraphConnection.Nearest,
                1f, 1f, 0f, (IReadOnlyList<ChapterStagePool>)null, 99));
            Assert.Throws<ArgumentException>(() => new ChapterGraphSpec(4, 2, 1, false, 1, ChapterGraphConnection.Nearest,
                1f, 1f, 0f, Array.Empty<ChapterStagePool>(), 99));
            Assert.Throws<ArgumentException>(() => new ChapterGraphSpec(4, 2, 1, false, 1, ChapterGraphConnection.Nearest,
                1f, 1f, 0f, new ChapterStagePool[] { null }, 99));
        }

        private static ChapterGraphSpec Spec(int seed, ChapterGraphConnection connection, float chance)
            => new(6, 3, 1, false, seed, connection, 3f, 1f, chance, Pools(10, 11, 12), 99);

        // 최소 개수 없이 전투 하나만 쓰는 단순 입력이다. 연결·모양 검사만 본다.
        private static IReadOnlyList<ChapterStagePool> Pools(params int[] combatKeys)
            => new[] { new ChapterStagePool(new ChapterStageDistributionRule(ChapterStageKind.Combat, 1, 0, 1), combatKeys) };

        private static string[] Describe(IReadOnlyList<ChapterGraphNode> nodes)
            => nodes.Select(n => $"{n.NodeId}:{n.StageKey}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}").ToArray();
    }
}
