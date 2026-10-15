using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.StageMap
{
    /// <summary>
    /// 챕터 그래프 생성 알고리즘이다. 입력 묶음만 읽는 순수 계산이며 같은 입력이면 같은 그래프를 만든다.
    /// 층 구성: 시작 노드 하나(깊이 0) → 깊이 1~DepthCount-2 층마다 1~WidthMax개의 일반 노드 → 보스 노드 하나.
    /// 연결: 앞 층의 모든 노드는 다음 층으로 하나 이상 이어지고, 다음 층의 모든 노드는 들어오는 연결이 하나 이상 있다.
    /// </summary>
    public static class ChapterGraphGenerator
    {
        public static IReadOnlyList<ChapterGraphNode> Generate(ChapterGraphSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            // 난수 호출 순서가 결과를 정한다. 층 폭과 스테이지 배정 → 층 사이 연결 순서를 바꾸지 않는다.
            var random = new Random(spec.Seed);
            var layers = new List<List<Node>> { new() { new Node(1, 0, 0, 0) } };
            int nextId = 2;
            for (int depth = 1; depth < spec.DepthCount - 1; depth++)
            {
                int count = random.Next(spec.WidthMax) + 1;
                var layer = new List<Node>(count);
                for (int width = 0; width < count; width++)
                    layer.Add(new Node(nextId++, spec.RegularStageKeys[random.Next(spec.RegularStageKeys.Count)], depth, width));
                layers.Add(layer);
            }
            layers.Add(new List<Node> { new Node(nextId, spec.BossStageKey, spec.DepthCount - 1, 0) });

            for (int depth = 0; depth < layers.Count - 1; depth++) Connect(layers[depth], layers[depth + 1], random, spec);

            var result = new List<ChapterGraphNode>();
            foreach (List<Node> layer in layers)
                foreach (Node node in layer)
                    result.Add(new ChapterGraphNode(node.Id, node.StageKey, node.Depth, node.Width, node.Next));
            return result.AsReadOnly();
        }

        private static void Connect(List<Node> from, List<Node> to, Random random, ChapterGraphSpec spec)
        {
            for (int source = 0; source < from.Count; source++) Add(from[source], to[Select(source, from.Count, to.Count, random, spec)].Id);
            for (int target = 0; target < to.Count; target++)
            {
                bool hasIncoming = false;
                foreach (Node node in from) if (node.Next.Contains(to[target].Id)) { hasIncoming = true; break; }
                if (!hasIncoming) Add(from[Math.Min(target * from.Count / to.Count, from.Count - 1)], to[target].Id);
            }
            for (int source = 0; source < from.Count; source++)
                if (to.Count > 1 && random.NextDouble() < spec.AdditionalConnectionChance)
                    Add(from[source], to[Select(source, from.Count, to.Count, random, spec)].Id);
        }

        private static int Select(int source, int sourceCount, int targetCount, Random random, ChapterGraphSpec spec)
        {
            int nearest = Math.Min(source * targetCount / sourceCount, targetCount - 1);
            if (spec.Connection == ChapterGraphConnection.Nearest) return nearest;
            if (spec.Connection == ChapterGraphConnection.Random) return random.Next(targetCount);
            double total = spec.NearestWeight + spec.OtherWeight * (targetCount - 1);
            if (random.NextDouble() * total < spec.NearestWeight) return nearest;
            int target; do target = random.Next(targetCount); while (target == nearest);
            return target;
        }

        private static void Add(Node node, int targetId) { if (!node.Next.Contains(targetId)) node.Next.Add(targetId); }

        private sealed class Node
        {
            internal readonly int Id, StageKey, Depth, Width;
            internal readonly List<int> Next = new();
            internal Node(int id, int stageKey, int depth, int width) { Id = id; StageKey = stageKey; Depth = depth; Width = width; }
        }
    }
}
