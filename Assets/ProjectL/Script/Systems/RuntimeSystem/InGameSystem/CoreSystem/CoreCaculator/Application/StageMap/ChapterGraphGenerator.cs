using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.StageMap
{
    /// <summary>
    /// 챕터 그래프 생성 알고리즘이다. 입력 묶음만 읽는 순수 계산이며 같은 입력이면 같은 그래프를 만든다.
    /// 층 구성: 시작 노드 하나(깊이 0) → 깊이 1~DepthCount-2 층마다 WidthMin~WidthMax개의 일반 노드 → 보스 노드 하나.
    /// SingleFirstLayer면 깊이 1은 노드 하나다.
    /// 연결: 앞 층의 모든 노드는 다음 층으로 하나 이상 이어지고, 다음 층의 모든 노드는 들어오는 연결이 하나 이상 있다.
    /// 배정: 고정 노드(개발 진입 고정·첫 층 스테이지) → 종류별 최소 개수 → 남은 자리는 종류 가중치 추첨
    /// → 종류 안에서 스테이지 키 균등 추첨(첫 층 스테이지는 추첨 후보에서 뺀다).
    /// 종류는 출현 시작 깊이 이상의 노드에만 놓인다(고정 노드는 예외). WidthMin 1, 모든 시작 깊이 1이면
    /// 깊이 규칙이 없던 때와 같은 시드에서 같은 그래프를 만든다.
    /// </summary>
    public static class ChapterGraphGenerator
    {
        // 난수는 쓰임마다 따로 둔다. 가중치·최소 개수를 바꿔도 모양이 같고, 후보를 늘려도 종류 배치가 같다.
        private const int DistributionSeedMask = 0x5F3759DF;
        private const int StageKeySeedMask = 0x6C8E9CF5;

        public static IReadOnlyList<ChapterGraphNode> Generate(ChapterGraphSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            List<List<Node>> layers = BuildTopology(spec, new Random(spec.Seed));
            var middle = new List<Node>();
            for (int depth = 1; depth < layers.Count - 1; depth++) middle.AddRange(layers[depth]);

            var fixedNodes = new List<Node>(2);
            Node pinned = PlacePin(spec, layers, middle);
            if (pinned != null) fixedNodes.Add(pinned);
            if (spec.PlacesFirstStage) fixedNodes.Add(PlaceFirstStage(spec, layers));
            AssignKinds(spec, middle, fixedNodes, new Random(unchecked(spec.Seed ^ DistributionSeedMask)));
            AssignStageKeys(spec, middle, fixedNodes, new Random(unchecked(spec.Seed ^ StageKeySeedMask)));
            EnsureMinimums(spec, middle);

            var result = new List<ChapterGraphNode>();
            foreach (List<Node> layer in layers)
                foreach (Node node in layer)
                    result.Add(new ChapterGraphNode(node.Id, node.StageKey, node.Depth, node.Width, node.Next));
            return result.AsReadOnly();
        }

        // 층 폭 → 층 사이 연결 순서로 모양 전용 난수만 쓴다. 이 순서를 바꾸지 않는다.
        private static List<List<Node>> BuildTopology(ChapterGraphSpec spec, Random random)
        {
            var layers = new List<List<Node>> { new() { new Node(1, 0, 0) } };
            int nextId = 2;
            for (int depth = 1; depth < spec.DepthCount - 1; depth++)
            {
                // 첫 층을 하나로 만들 때도 폭은 뽑는다. 난수 횟수가 같아야 깊이 2부터의 폭이 켜고 꺼도 같다.
                int count = random.Next(spec.WidthMax - spec.WidthMin + 1) + spec.WidthMin;
                if (depth == 1 && spec.SingleFirstLayer) count = 1;
                var layer = new List<Node>(count);
                for (int width = 0; width < count; width++) layer.Add(new Node(nextId++, depth, width));
                layers.Add(layer);
            }
            layers.Add(new List<Node> { new Node(nextId, spec.DepthCount - 1, 0) { StageKey = spec.BossStageKey } });

            for (int depth = 0; depth < layers.Count - 1; depth++) Connect(layers[depth], layers[depth + 1], random, spec);
            return layers;
        }

        private static Node PlacePin(ChapterGraphSpec spec, List<List<Node>> layers, List<Node> middle)
        {
            ChapterStagePin pin = spec.Pin;
            if (pin == null) return null;

            int nodeId = pin.NodeId.Value;
            Node node = middle.Find(n => n.Id == nodeId);
            if (node == null)
            {
                bool exists = layers[0][0].Id == nodeId || layers[layers.Count - 1][0].Id == nodeId;
                throw new ArgumentException(exists
                    ? $"고정 노드 {nodeId}는 시작·보스 노드입니다. 중간 깊이 1~{spec.DepthCount - 2}의 노드만 고정할 수 있습니다."
                    : $"고정 노드 {nodeId}가 생성된 그래프에 없습니다.", nameof(spec));
            }
            node.Pool = PoolOf(spec, pin.StageKey.Value);
            node.StageKey = pin.StageKey.Value;
            return node;
        }

        // 첫 층을 하나로 만들면 그 노드가 FirstLayerNodeId다. 모양 난수는 이미 다 썼으므로 그래프 모양은 고정이 없을 때와 같다.
        private static Node PlaceFirstStage(ChapterGraphSpec spec, List<List<Node>> layers)
        {
            Node node = layers[1][0];
            if (layers[1].Count != 1 || node.Id != ChapterGraphSpec.FirstLayerNodeId)
                throw new InvalidOperationException("첫 층 노드가 하나가 아닙니다. 입력 검사와 생성 결과가 어긋났습니다.");
            node.Pool = PoolOf(spec, spec.FirstStageKey);
            node.StageKey = spec.FirstStageKey;
            return node;
        }

        // 섞기와 남은 자리 추첨은 분포 전용 난수만 쓴다. 섞기는 최소 개수와 무관하게 고정 노드 밖 모든 자리를 섞는다.
        private static void AssignKinds(ChapterGraphSpec spec, List<Node> middle, List<Node> fixedNodes, Random random)
        {
            var slots = new List<Node>(middle.Count);
            foreach (Node node in middle) if (!fixedNodes.Contains(node)) slots.Add(node);
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            // 최소 배치는 시작 깊이가 깊은(놓일 곳이 좁은) 종류부터 섞인 순서의 빈 자리를 가져간다.
            // 허용 자리가 "깊이 d 이상"으로 포개지므로 좁은 종류가 먼저 고르면 넓은 종류가 자리를 빼앗지 않는다.
            // 시작 깊이가 같으면 종류 오름차순이라, 모두 1이면 깊이 규칙이 없던 때와 같은 자리를 고른다.
            var order = new List<ChapterStagePool>(spec.StagePools);
            order.Sort((a, b) => a.Rule.MinDepth != b.Rule.MinDepth
                ? b.Rule.MinDepth.CompareTo(a.Rule.MinDepth)
                : a.Rule.Kind.CompareTo(b.Rule.Kind));
            foreach (ChapterStagePool pool in order)
            {
                int need = pool.Rule.MinimumCount;
                foreach (Node node in fixedNodes) if (node.Pool == pool) need--;
                for (int i = 0; i < slots.Count && need > 0; i++)
                {
                    if (slots[i].Pool != null || slots[i].Depth < pool.Rule.MinDepth) continue;
                    slots[i].Pool = pool;
                    need--;
                }
                if (need > 0)
                    throw new InvalidOperationException("최소 배치 자리가 부족합니다. 입력의 용량 검사와 생성 결과가 어긋났습니다.");
            }

            // 남은 자리는 노드 ID 순서로 자리마다 한 번씩, 그 깊이에 놓일 수 있는 종류의 가중치로 추첨한다.
            foreach (Node node in middle)
            {
                if (node.Pool != null) continue;
                int totalWeight = 0;
                foreach (ChapterStagePool pool in spec.StagePools)
                    if (pool.Rule.MinDepth <= node.Depth) totalWeight += pool.Rule.Weight;
                if (totalWeight == 0)
                    throw new InvalidOperationException($"깊이 {node.Depth}에 놓일 종류가 없습니다. 입력 검사와 생성 결과가 어긋났습니다.");
                int roll = random.Next(totalWeight);
                foreach (ChapterStagePool pool in spec.StagePools)
                {
                    if (pool.Rule.MinDepth > node.Depth) continue;
                    if (roll < pool.Rule.Weight) { node.Pool = pool; break; }
                    roll -= pool.Rule.Weight;
                }
            }
        }

        // 키 추첨은 키 전용 난수로 노드 ID 순서에 한 번씩 한다. 후보가 하나여도 건너뛰지 않는다.
        // 건너뛰면 한 종류에 후보를 추가할 때 뒤 노드들의 다른 종류 키까지 바뀐다.
        // 첫 층 스테이지는 그 종류의 후보에서 빼고 뽑는다(입력 검사가 다른 후보가 있음을 보장한다).
        private static void AssignStageKeys(ChapterGraphSpec spec, List<Node> middle, List<Node> fixedNodes, Random random)
        {
            foreach (Node node in middle)
            {
                if (fixedNodes.Contains(node)) continue;
                IReadOnlyList<int> keys = DrawableKeys(spec, node.Pool);
                node.StageKey = keys[random.Next(keys.Count)];
            }
        }

        private static IReadOnlyList<int> DrawableKeys(ChapterGraphSpec spec, ChapterStagePool pool)
        {
            if (spec.FirstStageKey == 0) return pool.StageKeys;
            int index = -1;
            for (int i = 0; i < pool.StageKeys.Count; i++) if (pool.StageKeys[i] == spec.FirstStageKey) index = i;
            if (index < 0) return pool.StageKeys;
            var keys = new List<int>(pool.StageKeys);
            keys.RemoveAt(index);
            return keys;
        }

        private static void EnsureMinimums(ChapterGraphSpec spec, List<Node> middle)
        {
            foreach (ChapterStagePool pool in spec.StagePools)
            {
                int count = 0;
                foreach (Node node in middle) if (node.Pool == pool) count++;
                if (count < pool.Rule.MinimumCount)
                    throw new InvalidOperationException($"{pool.Rule.Kind} 노드가 {count}개로 최소 {pool.Rule.MinimumCount}개보다 적습니다.");
            }
        }

        private static ChapterStagePool PoolOf(ChapterGraphSpec spec, int stageKey)
        {
            foreach (ChapterStagePool pool in spec.StagePools)
                foreach (int key in pool.StageKeys)
                    if (key == stageKey) return pool;
            // 입력 검사가 이미 거른다.
            throw new InvalidOperationException($"고정 스테이지 {stageKey}의 후보 묶음이 없습니다.");
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
            internal readonly int Id, Depth, Width;
            internal readonly List<int> Next = new();
            /// <summary>배정된 종류의 후보 묶음. 시작·보스 노드는 null이다.</summary>
            internal ChapterStagePool Pool;
            internal int StageKey;
            internal Node(int id, int depth, int width) { Id = id; Depth = depth; Width = width; }
        }
    }
}
