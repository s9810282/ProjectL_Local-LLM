using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.Core
{
    public sealed class ChapterStageRepeatReductionTests
    {
        private const int Boss = 99;

        [Test]
        public void Rule_RepeatReductionIsOptIn()
        {
            var legacy = new ChapterStageDistributionRule(ChapterStageKind.Shop, 0, 2, 1);
            Assert.That(legacy.ReduceConsecutive, Is.False);
            Assert.That(Rule(ChapterStageKind.Shop, 0, 2, 1, true).ReduceConsecutive, Is.True);
        }

        [Test]
        public void Generate_ReducesFacilityRepeatsWithoutChangingCountsOrTopology()
        {
            int improved = 0;
            for (int seed = 0; seed < 32; seed++)
            {
                var before = ChapterGraphGenerator.Generate(Spec(seed, false));
                var after = ChapterGraphGenerator.Generate(Spec(seed, true));
                AssertPreserved(before, after);
                Assert.That(Score(after), Is.LessThanOrEqualTo(Score(before)), $"seed {seed}");
                if (Score(after) < Score(before)) improved++;
            }
            Assert.That(improved, Is.GreaterThan(0), "기존 연속 배치 중 개선되는 사례가 있어야 합니다.");
        }

        [TestCase(ChapterGraphConnection.Nearest)]
        [TestCase(ChapterGraphConnection.Random)]
        [TestCase(ChapterGraphConnection.WeightedRandom)]
        public void Generate_SeedSweepPreservesContractsAndImprovesScore(ChapterGraphConnection connection)
        {
            int improved = 0;
            for (int seed = 0; seed < 64; seed++)
            {
                var before = ChapterGraphGenerator.Generate(Spec(seed, false, 3, connection));
                var spec = Spec(seed, true, 3, connection);
                var after = ChapterGraphGenerator.Generate(spec);
                AssertPreserved(before, after);
                Assert.That(Score(after), Is.LessThanOrEqualTo(Score(before)), $"seed {seed}");
                Assert.That(Signature(ChapterGraphGenerator.Generate(spec)), Is.EqualTo(Signature(after)), $"seed {seed}");
                if (Score(after) < Score(before)) improved++;
            }
            Assert.That(improved, Is.GreaterThan(0));
        }

        [TestCase(ChapterStageKind.Shop)]
        [TestCase(ChapterStageKind.Recovery)]
        [TestCase(ChapterStageKind.Enhancement)]
        public void Generate_EachEnabledFacilityKindReducesRepeats(ChapterStageKind kind)
        {
            int improved = 0;
            int stageKey = (int)kind * 100 + 1;
            for (int seed = 0; seed < 32; seed++)
            {
                ChapterGraphSpec Create(bool reduce) => new ChapterGraphSpec(8, 1, 1, false, seed,
                    ChapterGraphConnection.Nearest, 10f, 1f, 0f,
                    new[]
                    {
                        new ChapterStagePool(Rule(ChapterStageKind.Combat, 1, 0, 1, false), new[] { 101 }),
                        new ChapterStagePool(Rule(kind, 0, 3, 1, reduce), new[] { stageKey })
                    }, Boss);
                var before = ChapterGraphGenerator.Generate(Create(false));
                var after = ChapterGraphGenerator.Generate(Create(true));
                AssertPreserved(before, after);
                Assert.That(Score(after), Is.Zero, $"{kind}, seed {seed}");
                if (Score(before) > 0) improved++;
            }
            Assert.That(improved, Is.GreaterThan(0));
        }

        [Test]
        public void Generate_DisabledFacilityRepeatsAreUnchangedWhenAnotherKindIsEnabled()
        {
            int repeated = 0;
            for (int seed = 0; seed < 32; seed++)
            {
                ChapterGraphSpec Create(bool reduce) => new ChapterGraphSpec(9, 1, 1, false, seed,
                    ChapterGraphConnection.Nearest, 10f, 1f, 0f,
                    new[]
                    {
                        new ChapterStagePool(Rule(ChapterStageKind.Combat, 1, 0, 1, false), new[] { 101 }),
                        new ChapterStagePool(Rule(ChapterStageKind.Shop, 0, 3, 1, false), new[] { 201 }),
                        new ChapterStagePool(Rule(ChapterStageKind.Enhancement, 0, 1, 1, reduce), new[] { 401 })
                    }, Boss);
                var before = ChapterGraphGenerator.Generate(Create(false));
                var after = ChapterGraphGenerator.Generate(Create(true));
                if (Score(before) > 0) repeated++;
                Assert.That(Signature(after), Is.EqualTo(Signature(before)), $"seed {seed}");
            }
            Assert.That(repeated, Is.GreaterThan(0));
        }

        [Test]
        public void Generate_CleanGraphIsUnchanged()
        {
            int clean = 0;
            for (int seed = 0; seed < 64; seed++)
            {
                var before = ChapterGraphGenerator.Generate(Spec(seed, false));
                if (Score(before) != 0) continue;
                clean++;
                Assert.That(Signature(ChapterGraphGenerator.Generate(Spec(seed, true))), Is.EqualTo(Signature(before)));
            }
            Assert.That(clean, Is.GreaterThan(0));
        }

        [Test]
        public void Generate_CombatAndDifferentFacilityKindsAreNotPenalized()
        {
            var pools = new[]
            {
                new ChapterStagePool(Rule(ChapterStageKind.Combat, 1, 0, 1, false), new[] { 101 }),
                new ChapterStagePool(Rule(ChapterStageKind.Shop, 0, 1, 1, true), new[] { 201 }),
                new ChapterStagePool(Rule(ChapterStageKind.Recovery, 0, 1, 1, true), new[] { 301 }),
                new ChapterStagePool(Rule(ChapterStageKind.Enhancement, 0, 1, 1, true), new[] { 401 })
            };
            for (int seed = 0; seed < 16; seed++)
            {
                var spec = new ChapterGraphSpec(10, 1, 1, false, seed, ChapterGraphConnection.Nearest,
                    10f, 1f, 0f, pools, Boss);
                var graph = ChapterGraphGenerator.Generate(spec);
                Assert.That(Score(graph), Is.Zero);
                var disabled = pools.Select(p => new ChapterStagePool(
                    Rule(p.Rule.Kind, p.Rule.Weight, p.Rule.MinimumCount, p.Rule.MinDepth, false), p.StageKeys)).ToArray();
                var baseline = new ChapterGraphSpec(10, 1, 1, false, seed, ChapterGraphConnection.Nearest,
                    10f, 1f, 0f, disabled, Boss);
                Assert.That(Signature(graph), Is.EqualTo(Signature(ChapterGraphGenerator.Generate(baseline))));
            }
        }

        [Test]
        public void Generate_FixedNodesStayInPlaceAndInfluenceReduction()
        {
            var pin = new ChapterStagePin(new StageNodeId(3), new StageKey(201));
            int improved = 0;
            for (int seed = 0; seed < 32; seed++)
            {
                var before = ChapterGraphGenerator.Generate(Spec(seed, false, pin: pin, firstStageKey: 201));
                var after = ChapterGraphGenerator.Generate(Spec(seed, true, pin: pin, firstStageKey: 201));
                Assert.That(after.Single(n => n.NodeId == 2).StageKey, Is.EqualTo(201));
                Assert.That(after.Single(n => n.NodeId == 3).StageKey, Is.EqualTo(201));
                Assert.That(after.Where(n => n.NodeId != 2 && n.NodeId != 3).Select(n => n.StageKey), Has.No.Member(201));
                AssertPreserved(before, after);
                // 앞의 고정 상점 두 개는 유지하고, 세 번째 상점까지 이어지는 배치를 줄입니다.
                Assert.That(Score(after), Is.EqualTo(1), $"seed {seed}");
                if (Score(before) > Score(after)) improved++;
            }
            Assert.That(improved, Is.GreaterThan(0));
        }

        [Test]
        public void Generate_LateDepthRulesAndZeroWeightMinimumsRemainValid()
        {
            var pin = new ChapterStagePin(new StageNodeId(2), new StageKey(301));
            for (int seed = 0; seed < 32; seed++)
            {
                var before = ChapterGraphGenerator.Generate(Spec(seed, false, 3, minimumWidth: 2, pin: pin, facilityMinDepth: 3));
                var after = ChapterGraphGenerator.Generate(Spec(seed, true, 3, minimumWidth: 2, pin: pin, facilityMinDepth: 3));
                AssertPreserved(before, after);
                Assert.That(after.Single(n => n.NodeId == 2).StageKey, Is.EqualTo(301));
                Assert.That(after.Where(n => n.StageKey / 100 >= 2 && n.NodeId != 2).All(n => n.Depth >= 3), Is.True);
                Assert.That(after.Count(n => n.StageKey / 100 == 3), Is.EqualTo(1));
                Assert.That(after.Count(n => n.StageKey / 100 == 4), Is.EqualTo(1));
                Assert.That(Score(after), Is.LessThanOrEqualTo(Score(before)));
            }
        }

        [Test]
        public void Generate_UnavoidableRepeatsAreValid()
        {
            var pool = new ChapterStagePool(Rule(ChapterStageKind.Shop, 1, 0, 1, true), new[] { 201 });
            var spec = new ChapterGraphSpec(6, 1, 1, false, 0, ChapterGraphConnection.Nearest,
                10f, 1f, 0f, new[] { pool }, Boss);
            var graph = ChapterGraphGenerator.Generate(spec);
            Assert.That(graph.Count(n => n.StageKey == 201), Is.EqualTo(4));
            Assert.That(Score(graph), Is.EqualTo(5));
        }

        [TestCase(ChapterGraphConnection.Nearest)]
        [TestCase(ChapterGraphConnection.Random)]
        [TestCase(ChapterGraphConnection.WeightedRandom)]
        public void Generate_WidthSixPreservesTopologyAndReportsElapsedTime(ChapterGraphConnection connection)
        {
            var timer = new Stopwatch();
            for (int seed = 0; seed < 16; seed++)
            {
                var before = ChapterGraphGenerator.Generate(Spec(seed, false, 6, connection, 6, true));
                timer.Start();
                var after = ChapterGraphGenerator.Generate(Spec(seed, true, 6, connection, 6, true));
                timer.Stop();
                Assert.That(after.Count(n => n.Depth == 1), Is.EqualTo(1));
                Assert.That(after.Count(n => n.Depth == 2), Is.EqualTo(6));
                AssertPreserved(before, after);
                Assert.That(Score(after), Is.LessThanOrEqualTo(Score(before)));
            }
            TestContext.WriteLine($"{connection}: 16 enabled width-six graphs, 31 middle nodes each, {timer.Elapsed.TotalMilliseconds:F3} ms total.");
        }

        private static string[] Signature(IReadOnlyList<ChapterGraphNode> nodes)
            => nodes.Select(n => $"{n.NodeId}:{n.StageKey}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}").ToArray();

        private static ChapterGraphSpec Spec(int seed, bool reduce, int width = 1,
            ChapterGraphConnection connection = ChapterGraphConnection.Nearest, int minimumWidth = 1,
            bool singleFirstLayer = false, ChapterStagePin pin = null, int firstStageKey = 0, int facilityMinDepth = 1)
        {
            return new ChapterGraphSpec(8, width, minimumWidth, singleFirstLayer || firstStageKey != 0, seed, connection, 10f, 1f, 0.25f,
                new[]
                {
                    new ChapterStagePool(Rule(ChapterStageKind.Combat, 1, 1, 1, false), new[] { 101 }),
                    new ChapterStagePool(Rule(ChapterStageKind.Shop, 1, 3, facilityMinDepth, reduce), new[] { 201, 202 }),
                    new ChapterStagePool(Rule(ChapterStageKind.Recovery, 0, 1, facilityMinDepth, reduce), new[] { 301 }),
                    new ChapterStagePool(Rule(ChapterStageKind.Enhancement, 0, 1, facilityMinDepth, reduce), new[] { 401 })
                }, Boss, pin, firstStageKey);
        }

        private static ChapterStageDistributionRule Rule(ChapterStageKind kind, int weight, int minimum,
            int minDepth, bool reduce)
        {
            return new ChapterStageDistributionRule(kind, weight, minimum, minDepth, reduce);
        }

        private static void AssertPreserved(IReadOnlyList<ChapterGraphNode> before, IReadOnlyList<ChapterGraphNode> after)
        {
            Assert.That(after.Select(n => $"{n.NodeId}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}"),
                Is.EqualTo(before.Select(n => $"{n.NodeId}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}")));
            Assert.That(after.Select(n => n.StageKey / 100).OrderBy(k => k),
                Is.EqualTo(before.Select(n => n.StageKey / 100).OrderBy(k => k)));
        }

        // 생성기의 계산 함수를 쓰지 않고 실제 2개·3개 노드 경로를 열거합니다.
        private static long Score(IReadOnlyList<ChapterGraphNode> graph)
        {
            var nodes = graph.ToDictionary(n => n.NodeId);
            long score = 0;
            foreach (var first in graph)
            {
                int kind = first.StageKey / 100;
                if (kind < 2) continue;
                foreach (int next in first.NextNodeIds)
                {
                    var second = nodes[next];
                    if (second.StageKey / 100 != kind) continue;
                    score++;
                    foreach (int last in second.NextNodeIds)
                        if (nodes[last].StageKey / 100 == kind) score++;
                }
            }
            return score;
        }
    }
}
