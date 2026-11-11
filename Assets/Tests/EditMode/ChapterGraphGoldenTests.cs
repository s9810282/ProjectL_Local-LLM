using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 고정 시드의 챕터 그래프를 정답 문자열로 박아 둔 시험이다. 생성 규칙을 바꿀 때 기본 설정에서
    /// 같은 시드가 같은 맵을 만드는지(이전 맵 보존) 확인한다. 정답은 층 최소 폭·출현 시작 깊이를 넣기 전
    /// 생성기로 만든 값이다(챕터 분포 설계 §13 MS5). 노드 하나는 "ID:스테이지:깊이:폭:다음 노드들"이다.
    /// 2026-11-11 줄 바꾸기(옆 노드 추가 연결) 도입: 옛 Nearest 추가 연결은 늘 이미 이어진 노드를 골라 효과가 없었으므로,
    /// Nearest 사례는 확률 0으로 옛 정답을 그대로 지킨다. Random·WeightedRandom·고정 노드 사례는 추가 연결 규칙이 바뀌어
    /// 새 생성기로 다시 기록했다. 실패하면 메시지에 실제 문자열 전체가 찍히므로 의도한 변경이면 그 값으로 다시 기록한다.
    /// </summary>
    public sealed class ChapterGraphGoldenTests
    {
        private const int Boss = 99;

        private static IEnumerable<TestCaseData> Cases()
        {
            yield return new TestCaseData(ChapterGraphConnection.Nearest, 0, "1:0:0:0:2,3,4 2:20:1:0:5 3:12:1:1:6 4:10:1:2:7 5:12:2:0:8 6:10:2:1:9 7:30:2:2:10 8:20:3:0:11 9:11:3:1:11 10:41:3:2:12 11:10:4:0:13 12:40:4:1:13 13:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Nearest, 1, "1:0:0:0:2 2:30:1:0:3 3:20:2:0:4,5 4:11:3:0:6 5:12:3:1:7,8 6:12:4:0:9 7:40:4:1:9 8:30:4:2:9 9:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Nearest, 2, "1:0:0:0:2,3,4 2:41:1:0:5 3:30:1:1:5 4:11:1:2:6 5:20:2:0:7 6:40:2:1:7 7:12:3:0:8,9,10 8:11:4:0:11 9:30:4:1:11 10:20:4:2:11 11:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Nearest, 3, "1:0:0:0:2 2:10:1:0:3,4,5 3:40:2:0:6 4:12:2:1:7 5:30:2:2:8 6:11:3:0:9 7:20:3:1:9 8:30:3:2:9 9:10:4:0:10 10:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Nearest, 4, "1:0:0:0:2,3,4 2:11:1:0:5 3:20:1:1:6 4:20:1:2:7 5:10:2:0:8 6:30:2:1:8 7:11:2:2:9 8:41:3:0:10 9:11:3:1:11 10:10:4:0:12 11:11:4:1:12 12:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Nearest, 5, "1:0:0:0:2,3 2:30:1:0:4 3:41:1:1:4 4:11:2:0:5 5:12:3:0:6,7 6:20:4:0:8 7:12:4:1:8 8:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Nearest, 6, "1:0:0:0:2,3,4 2:40:1:0:5 3:30:1:1:5 4:12:1:2:6 5:41:2:0:7 6:20:2:1:8,9 7:11:3:0:10 8:12:3:1:11 9:11:3:2:12 10:10:4:0:13 11:11:4:1:13 12:12:4:2:13 13:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Nearest, 7, "1:0:0:0:2,3 2:12:1:0:4 3:30:1:1:5,6 4:41:2:0:7 5:10:2:1:7 6:12:2:2:8 7:20:3:0:9 8:40:3:1:9 9:10:4:0:10 10:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Random, 0, "1:0:0:0:2,3,4 2:20:1:0:7,5 3:12:1:1:6 4:10:1:2:7 5:12:2:0:9,8 6:10:2:1:9 7:30:2:2:10 8:20:3:0:12 9:11:3:1:11 10:41:3:2:12 11:10:4:0:13 12:40:4:1:13 13:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Random, 1, "1:0:0:0:2 2:30:1:0:3 3:20:2:0:4,5 4:11:3:0:6,7 5:12:3:1:7,8 6:12:4:0:9 7:40:4:1:9 8:30:4:2:9 9:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Random, 2, "1:0:0:0:2,3,4 2:41:1:0:6,5 3:30:1:1:5 4:11:1:2:5,6 5:20:2:0:7 6:40:2:1:7 7:12:3:0:9,8,10 8:11:4:0:11 9:30:4:1:11 10:20:4:2:11 11:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Random, 3, "1:0:0:0:2 2:10:1:0:3,4,5 3:40:2:0:8,6 4:12:2:1:7 5:30:2:2:7 6:11:3:0:9 7:20:3:1:9 8:30:3:2:9 9:10:4:0:10 10:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Random, 4, "1:0:0:0:2,3,4 2:11:1:0:7,5 3:20:1:1:6 4:20:1:2:6 5:10:2:0:8 6:30:2:1:9 7:11:2:2:9 8:41:3:0:11,10 9:11:3:1:11 10:10:4:0:12 11:11:4:1:12 12:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Random, 5, "1:0:0:0:2,3 2:30:1:0:4 3:41:1:1:4 4:11:2:0:5 5:12:3:0:6,7 6:20:4:0:8 7:12:4:1:8 8:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Random, 6, "1:0:0:0:4,2,3 2:40:1:0:6 3:30:1:1:5 4:12:1:2:6 5:41:2:0:9,7 6:20:2:1:8 7:11:3:0:11,10 8:12:3:1:12 9:11:3:2:12 10:10:4:0:13 11:11:4:1:13 12:12:4:2:13 13:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.Random, 7, "1:0:0:0:2,3 2:12:1:0:4,5 3:30:1:1:6 4:41:2:0:7 5:10:2:1:8 6:12:2:2:7 7:20:3:0:9 8:40:3:1:9 9:10:4:0:10 10:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.WeightedRandom, 0, "1:0:0:0:2,3,4 2:20:1:0:6 3:12:1:1:5 4:10:1:2:7 5:12:2:0:10,8 6:10:2:1:10,9 7:30:2:2:10 8:20:3:0:11 9:11:3:1:11 10:41:3:2:12 11:10:4:0:13 12:40:4:1:13 13:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.WeightedRandom, 1, "1:0:0:0:2 2:30:1:0:3 3:20:2:0:4,5 4:11:3:0:6,7 5:12:3:1:7,8 6:12:4:0:9 7:40:4:1:9 8:30:4:2:9 9:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.WeightedRandom, 2, "1:0:0:0:2,3,4 2:41:1:0:5 3:30:1:1:5 4:11:1:2:6,5 5:20:2:0:7 6:40:2:1:7 7:12:3:0:8,9,10 8:11:4:0:11 9:30:4:1:11 10:20:4:2:11 11:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.WeightedRandom, 3, "1:0:0:0:2 2:10:1:0:3,4,5 3:40:2:0:7,6 4:12:2:1:7 5:30:2:2:8,7 6:11:3:0:9 7:20:3:1:9 8:30:3:2:9 9:10:4:0:10 10:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.WeightedRandom, 4, "1:0:0:0:2,3,4 2:11:1:0:5 3:20:1:1:6,7 4:20:1:2:7 5:10:2:0:8 6:30:2:1:8 7:11:2:2:9 8:41:3:0:10 9:11:3:1:11 10:10:4:0:12 11:11:4:1:12 12:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.WeightedRandom, 5, "1:0:0:0:2,3 2:30:1:0:4 3:41:1:1:4 4:11:2:0:5 5:12:3:0:6,7 6:20:4:0:8 7:12:4:1:8 8:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.WeightedRandom, 6, "1:0:0:0:4,2,3 2:40:1:0:5 3:30:1:1:5,6 4:12:1:2:6 5:41:2:0:7 6:20:2:1:8,9 7:11:3:0:10 8:12:3:1:10,11 9:11:3:2:12 10:10:4:0:13 11:11:4:1:13 12:12:4:2:13 13:99:5:0:");
            yield return new TestCaseData(ChapterGraphConnection.WeightedRandom, 7, "1:0:0:0:2,3 2:12:1:0:4,5 3:30:1:1:6 4:41:2:0:8 5:10:2:1:7 6:12:2:2:8 7:20:3:0:9 8:40:3:1:9 9:10:4:0:10 10:99:5:0:");
        }

        [TestCaseSource(nameof(Cases))]
        public void DefaultSettings_KeepRecordedGraph(ChapterGraphConnection connection, int seed, string expected)
        {
            string actual = Describe(ChapterGraphGenerator.Generate(Spec(seed, connection, pin: null)));
            Assert.That(actual, Is.EqualTo(expected), actual);
        }

        [Test]
        public void DefaultSettings_KeepRecordedPinnedGraph()
        {
            var pin = new ChapterStagePin(new StageNodeId(3), new StageKey(30));

            string actual = Describe(ChapterGraphGenerator.Generate(Spec(3, ChapterGraphConnection.WeightedRandom, pin)));
            Assert.That(actual,
                Is.EqualTo("1:0:0:0:2 2:40:1:0:3,4,5 3:30:2:0:7,6 4:10:2:1:7 5:12:2:2:8,7 6:30:3:0:9 7:20:3:1:9 8:41:3:2:9 9:10:4:0:10 10:99:5:0:"),
                actual);
        }

        // 운영 Chapter1·2와 같은 연결 설정(폭 2~3, 첫 층 노드 1개, Nearest, 줄 바꾸기 10%, 맵당 최소 갈래 1)이다.
        [TestCase(0, "1:0:0:0:2 2:11:1:0:3,4,5 3:20:2:0:6 4:20:2:1:7 5:12:2:2:8,7 6:10:3:0:9 7:10:3:1:10 8:41:3:2:11 9:11:4:0:12 10:11:4:1:12 11:30:4:2:12 12:99:5:0:")]
        [TestCase(1, "1:0:0:0:2 2:12:1:0:3,4 3:12:2:0:5 4:30:2:1:6 5:12:3:0:7 6:12:3:1:8,9 7:20:4:0:10 8:40:4:1:10 9:10:4:2:10 10:99:5:0:")]
        [TestCase(2, "1:0:0:0:2 2:11:1:0:3,4 3:11:2:0:5 4:40:2:1:6 5:40:3:0:7 6:20:3:1:8,9 7:30:4:0:10 8:11:4:1:10 9:20:4:2:10 10:99:5:0:")]
        [TestCase(3, "1:0:0:0:2 2:30:1:0:3,4,5 3:10:2:0:6 4:30:2:1:7 5:10:2:2:8 6:11:3:0:9 7:12:3:1:9,10 8:10:3:2:10 9:40:4:0:11 10:20:4:1:11 11:99:5:0:")]
        public void ChapterSettings_KeepRecordedGraph(int seed, string expected)
        {
            var spec = new ChapterGraphSpec(6, 3, 2, true, seed, ChapterGraphConnection.Nearest, 10f, 1f, 0.1f, Pools(), Boss,
                minimumBranchCount: 1);

            string actual = Describe(ChapterGraphGenerator.Generate(spec));
            Assert.That(actual, Is.EqualTo(expected), actual);
        }

        // Chapter1과 같은 꼴(깊이 6, 최대 폭 3, 네 종류 80/9/7/7, 최소 1씩)이다.
        // Nearest는 확률 0으로 옛 맵을 지키고(옛 추가 연결은 효과가 없었다), 나머지는 0.25 그대로 다시 기록한 값을 본다.
        private static ChapterGraphSpec Spec(int seed, ChapterGraphConnection connection, ChapterStagePin pin)
            => new(6, 3, 1, false, seed, connection, 10f, 1f, connection == ChapterGraphConnection.Nearest ? 0f : 0.25f, Pools(), Boss, pin);

        private static ChapterStagePool[] Pools() => new[]
        {
            new ChapterStagePool(new ChapterStageDistributionRule(ChapterStageKind.Combat, 80, 1, 1), new[] { 10, 11, 12 }),
            new ChapterStagePool(new ChapterStageDistributionRule(ChapterStageKind.Shop, 9, 1, 1), new[] { 20 }),
            new ChapterStagePool(new ChapterStageDistributionRule(ChapterStageKind.Recovery, 7, 1, 1), new[] { 30 }),
            new ChapterStagePool(new ChapterStageDistributionRule(ChapterStageKind.Enhancement, 7, 1, 1), new[] { 40, 41 })
        };

        private static string Describe(IReadOnlyList<ChapterGraphNode> nodes)
            => string.Join(" ", nodes.Select(n => $"{n.NodeId}:{n.StageKey}:{n.Depth}:{n.WidthIndex}:{string.Join(",", n.NextNodeIds)}"));
    }
}
