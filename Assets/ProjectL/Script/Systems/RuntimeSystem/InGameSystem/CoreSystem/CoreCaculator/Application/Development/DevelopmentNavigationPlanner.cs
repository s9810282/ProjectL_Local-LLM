#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Development
{
    public static class DevelopmentNavigationPlanner
    {
        public static DevelopmentNavigationPlan PlanSkip(DevelopmentNavigationInput input, StageNodeId nodeId)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var nodes = input.Nodes.Select(n => n.Progress).ToArray();
            var normal = Evaluate(input, input.Expected.NodeId, nodes, nodeId);
            if (!normal.Accepted) return Refused(nodeId, normal.FailureReason);
            var target = input.Nodes.Single(n => n.Progress.NodeId == nodeId);
            if (!target.CanBypass || target.Progress.NextNodeIds.Count == 0)
                return new DevelopmentNavigationPlan(DevelopmentFailureReason.UnsupportedNode, nodeId,
                    normal.TargetStageKey, Array.Empty<StageNodeId>());
            return new DevelopmentNavigationPlan(DevelopmentFailureReason.None, nodeId, normal.TargetStageKey,
                new[] { nodeId });
        }

        public static DevelopmentNavigationPlan PlanJump(DevelopmentNavigationInput input, StageNodeId targetNodeId)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var simulated = input.Nodes.Select(n => n.Progress).ToArray();
            var direct = Evaluate(input, input.Expected.NodeId, simulated, targetNodeId);
            if (direct.Accepted)
                return new DevelopmentNavigationPlan(DevelopmentFailureReason.None, targetNodeId,
                    direct.TargetStageKey, Array.Empty<StageNodeId>());
            if (direct.FailureReason != ChapterSelectionFailureReason.NotConnected)
                return Refused(targetNodeId, direct.FailureReason);

            var indices = simulated.Select((node, index) => (node.NodeId, index)).ToDictionary(v => v.NodeId, v => v.index);
            var source = input.Expected.HasSelection
                ? simulated[indices[input.Expected.NodeId]]
                : simulated.Single(n => n.Depth == 0 && n.StageKey.IsEmpty);
            var bypassed = new List<StageNodeId>();
            var visited = new HashSet<StageNodeId>();

            bool Search(ChapterNodeProgress from)
            {
                if (!visited.Add(from.NodeId)) return false;
                foreach (var next in from.NextNodeIds.OrderBy(id => id.Value))
                {
                    // 연결·깊이·현재/대상 완료 규칙은 매 단계 기존 판정에 맡긴다.
                    var hop = Evaluate(input, from.NodeId, simulated, next);
                    if (!hop.Accepted) continue;
                    if (next == targetNodeId) return true;
                    int index = indices[next];
                    var candidate = simulated[index];
                    if (!input.Nodes[index].CanBypass || candidate.NextNodeIds.Count == 0) continue;
                    simulated[index] = new ChapterNodeProgress(candidate.NodeId, candidate.StageKey,
                        candidate.Depth, true, candidate.NextNodeIds);
                    bypassed.Add(next);
                    if (Search(simulated[index])) return true;
                    bypassed.RemoveAt(bypassed.Count - 1);
                    simulated[index] = candidate;
                }
                return false;
            }

            return Search(source)
                ? new DevelopmentNavigationPlan(DevelopmentFailureReason.None, targetNodeId,
                    simulated[indices[targetNodeId]].StageKey, bypassed)
                : new DevelopmentNavigationPlan(DevelopmentFailureReason.NoSupportedPath, targetNodeId,
                    simulated[indices[targetNodeId]].StageKey, Array.Empty<StageNodeId>());
        }

        private static ChapterSelectionResult Evaluate(DevelopmentNavigationInput input, StageNodeId from,
            IReadOnlyList<ChapterNodeProgress> nodes, StageNodeId target) =>
            ChapterNavigationRules.Evaluate(input.Expected.ChapterKey, input.Expected.ChapterKey,
                input.Expected.ChapterKey, from, input.IsChapterClear, nodes, target);

        private static DevelopmentNavigationPlan Refused(StageNodeId target, ChapterSelectionFailureReason reason) =>
            new DevelopmentNavigationPlan(reason == ChapterSelectionFailureReason.AlreadyCleared
                ? DevelopmentFailureReason.AlreadyPassed : DevelopmentFailureReason.InvalidTarget,
                target, default, Array.Empty<StageNodeId>(), reason);
    }
}
#endif
