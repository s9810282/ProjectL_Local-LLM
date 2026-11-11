using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>현재 노드부터 보스까지의 실제 배치를 공통 예약 시각으로 바꾼 불변 값입니다.</summary>
    internal sealed class StageMapIntroTimeline
    {
        private StageMapIntroTimeline(int currentNodeId, int bossNodeId, Vector3 player, Vector3 boss,
            float dropSeconds, double firstChange, List<NodeTiming> nodes, List<LinkTiming> links,
            CameraTiming camera)
        {
            CurrentNodeId = currentNodeId;
            BossNodeId = bossNodeId;
            PlayerPosition = player;
            BossPosition = boss;
            EffectiveDropSeconds = dropSeconds;
            FirstChangeSeconds = firstChange;
            Nodes = nodes.AsReadOnly();
            Links = links.AsReadOnly();
            Camera = camera;
        }

        internal bool HasTargets => Links.Count > 0;
        internal int CurrentNodeId { get; }
        internal int BossNodeId { get; }
        internal Vector3 PlayerPosition { get; }
        internal Vector3 BossPosition { get; }
        internal float EffectiveDropSeconds { get; }
        internal double FirstChangeSeconds { get; }
        internal IReadOnlyList<NodeTiming> Nodes { get; }
        internal IReadOnlyList<LinkTiming> Links { get; }
        internal CameraTiming Camera { get; }

        internal static StageMapIntroTimeline Build(StageMapIntroData intro, StageNodeDropData drop,
            IReadOnlyList<StageProgressData> nodes, IReadOnlyDictionary<int, Vector3> positions,
            int currentNodeId, Vector3 forward, IReadOnlyCollection<int> dropNodeIds)
        {
            if (intro == null) throw new ArgumentNullException(nameof(intro));
            if (drop == null) throw new ArgumentNullException(nameof(drop));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (positions == null) throw new ArgumentNullException(nameof(positions));
            if (dropNodeIds == null) throw new ArgumentNullException(nameof(dropNodeIds));
            intro.Validate();
            drop.Validate();
            if (!Finite(forward) || !float.IsFinite(forward.sqrMagnitude) || forward.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException("[StageMapIntroTimeline] 유효한 맵 진행 축이 필요합니다.");
            Vector3 axis = forward.normalized;
            var byId = new Dictionary<int, StageProgressData>();
            int bossId = 0;
            foreach (StageProgressData node in nodes)
            {
                if (node.NodeId <= 0 || !byId.TryAdd(node.NodeId, node))
                    throw InvalidNode(node.NodeId, "노드 ID가 없거나 중복됩니다");
                if (!positions.TryGetValue(node.NodeId, out Vector3 position) || !Finite(position))
                    throw InvalidNode(node.NodeId, "유한한 표시 위치가 없습니다");
                if (node.IsStart || node.MapRole != StageMapRole.Boss) continue;
                if (bossId != 0) throw InvalidNode(node.NodeId, $"보스가 중복됩니다: {bossId}");
                bossId = node.NodeId;
            }
            if (!byId.ContainsKey(currentNodeId)) throw InvalidNode(currentNodeId, "현재 노드가 없습니다");
            if (bossId == 0) throw InvalidNode(currentNodeId, "보스가 없습니다");
            Vector3 player = positions[currentNodeId];
            Vector3 boss = positions[bossId];
            var nodeTimes = new List<NodeTiming>();
            var linkTimes = new List<LinkTiming>();
            if (currentNodeId == bossId && byId[currentNodeId].NextNodeIds.Count == 0)
                return new StageMapIntroTimeline(currentNodeId, bossId, player, boss, 0f, 0d,
                    nodeTimes, linkTimes, default);

            var reachable = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(currentNodeId);
            while (pending.Count > 0)
            {
                int id = pending.Pop();
                if (!reachable.Add(id)) continue;
                if (!byId.TryGetValue(id, out StageProgressData node)) throw InvalidNode(id, "연결된 노드가 없습니다");
                foreach (int nextId in node.NextNodeIds) pending.Push(nextId);
            }
            if (!reachable.Contains(bossId)) throw InvalidNode(bossId, "현재 노드에서 보스에 도달할 수 없습니다");
            float bossDistance = Vector3.Dot(boss - player, axis);
            if (!float.IsFinite(bossDistance) || bossDistance <= 0f)
                throw InvalidNode(bossId, "보스가 진행 축의 전방에 있어야 합니다");
            var progress = new Dictionary<int, double>();
            foreach (int id in reachable)
            {
                double q = Vector3.Dot(positions[id] - player, axis) / (double)bossDistance;
                if (double.IsNaN(q) || double.IsInfinity(q) || q < 0d || q > 1d || (q == 1d && id != bossId))
                    throw InvalidNode(id, "노드는 현재 위치와 마지막 보스 사이에 있어야 합니다");
                progress.Add(id, q);
            }
            foreach (int id in reachable)
            {
                foreach (int nextId in byId[id].NextNodeIds)
                {
                    if (progress[nextId] <= progress[id])
                        throw InvalidNode(id, $"0 길이 또는 역방향 연결입니다: {id} → {nextId}");
                }
            }

            var dropIds = new HashSet<int>(dropNodeIds);
            float duration = drop.Enabled && drop.Height > 0f ? drop.Seconds : 0f;
            double minimum = double.PositiveInfinity;
            foreach (int id in dropIds)
            {
                if (!byId.ContainsKey(id)) throw InvalidNode(id, "낙하 대상 노드가 없습니다");
                if (drop.Enabled && id != currentNodeId && reachable.Contains(id))
                    minimum = Math.Min(minimum, progress[id]);
            }
            double padding = double.IsPositiveInfinity(minimum) ? 0d : Math.Max(0d, duration - minimum * intro.RevealSeconds);
            double revealStart = intro.StartHoldSeconds + padding;
            double revealEnd = revealStart + intro.RevealSeconds;
            double returnStart = revealEnd + intro.BossHoldSeconds;
            double firstChange = revealStart;
            foreach (StageProgressData node in nodes)
            {
                int id = node.NodeId;
                if (!reachable.Contains(id)) continue;
                double arrival = revealStart + progress[id] * intro.RevealSeconds;
                bool hasDrop = drop.Enabled && id != currentNodeId && dropIds.Contains(id);
                double start = hasDrop ? arrival - duration : arrival;
                nodeTimes.Add(new NodeTiming(id, node.Depth, arrival, start, hasDrop));
                if (hasDrop) firstChange = Math.Min(firstChange, start);
                foreach (int nextId in node.NextNodeIds)
                    linkTimes.Add(new LinkTiming(id, nextId, arrival, revealStart + progress[nextId] * intro.RevealSeconds));
            }
            return new StageMapIntroTimeline(currentNodeId, bossId, player, boss, duration, firstChange,
                nodeTimes, linkTimes, new CameraTiming(revealStart, revealEnd, returnStart, returnStart + intro.ReturnSeconds));
        }

        private static bool Finite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static InvalidOperationException InvalidNode(int id, string reason)
            => new($"[StageMapIntroTimeline] 노드 {id}: {reason}.");

        internal readonly struct NodeTiming
        {
            internal NodeTiming(int nodeId, int depth, double arrivalSeconds, double dropStartSeconds, bool hasDrop)
            {
                NodeId = nodeId;
                Depth = depth;
                ArrivalSeconds = arrivalSeconds;
                DropStartSeconds = dropStartSeconds;
                HasDrop = hasDrop;
            }
            internal int NodeId { get; }
            internal int Depth { get; }
            internal double ArrivalSeconds { get; }
            internal double DropStartSeconds { get; }
            internal bool HasDrop { get; }
        }

        internal readonly struct LinkTiming
        {
            internal LinkTiming(int fromNodeId, int toNodeId, double startSeconds, double endSeconds)
            {
                FromNodeId = fromNodeId;
                ToNodeId = toNodeId;
                StartSeconds = startSeconds;
                EndSeconds = endSeconds;
            }
            internal int FromNodeId { get; }
            internal int ToNodeId { get; }
            internal double StartSeconds { get; }
            internal double EndSeconds { get; }
        }

        internal readonly struct CameraTiming
        {
            internal CameraTiming(double revealStartSeconds, double revealEndSeconds, double returnStartSeconds, double endSeconds)
            {
                RevealStartSeconds = revealStartSeconds;
                RevealEndSeconds = revealEndSeconds;
                ReturnStartSeconds = returnStartSeconds;
                EndSeconds = endSeconds;
            }
            internal double RevealStartSeconds { get; }
            internal double RevealEndSeconds { get; }
            internal double ReturnStartSeconds { get; }
            internal double EndSeconds { get; }
        }
    }
}
