#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development
{
    public sealed class DevelopmentNode
    {
        public ChapterNodeProgress Progress { get; }
        public bool CanBypass { get; }

        public DevelopmentNode(ChapterNodeProgress progress, bool canBypass)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            CanBypass = canBypass;
        }
    }

    /// <summary>한 명령의 경로 계산에만 사용하는 불변 진행 사본이다.</summary>
    public sealed class DevelopmentNavigationInput
    {
        public DevelopmentContext Expected { get; }
        public bool IsChapterClear { get; }
        public IReadOnlyList<DevelopmentNode> Nodes { get; }

        public DevelopmentNavigationInput(DevelopmentContext expected, bool isChapterClear,
            IReadOnlyList<DevelopmentNode> nodes)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            var copy = nodes.ToArray();
            if (copy.Any(n => n == null) || copy.Select(n => n.Progress.NodeId).Distinct().Count() != copy.Length)
                throw new ArgumentException("노드는 null이 아니며 ID가 서로 달라야 합니다.", nameof(nodes));
            IsChapterClear = isChapterClear;
            Nodes = Array.AsReadOnly(copy);
        }
    }

    public sealed class DevelopmentNavigationPlan
    {
        public DevelopmentFailureReason Reason { get; }
        public ChapterSelectionFailureReason? NavigationFailureReason { get; }
        public StageNodeId TargetNodeId { get; }
        public StageKey TargetStageKey { get; }
        public IReadOnlyList<StageNodeId> BypassedNodes { get; }
        public StageNodeId? FinalSelectedNodeId => BypassedNodes.Count == 0
            ? (StageNodeId?)null : BypassedNodes[BypassedNodes.Count - 1];

        public DevelopmentNavigationPlan(DevelopmentFailureReason reason, StageNodeId targetNodeId,
            StageKey targetStageKey, IReadOnlyList<StageNodeId> bypassedNodes,
            ChapterSelectionFailureReason? navigationFailureReason = null)
        {
            if (bypassedNodes == null) throw new ArgumentNullException(nameof(bypassedNodes));
            var copy = bypassedNodes.ToArray();
            if (reason != DevelopmentFailureReason.None && copy.Length != 0)
                throw new ArgumentException("거절된 계획에는 통과할 노드를 담을 수 없습니다.", nameof(bypassedNodes));
            if (copy.Any(id => id.IsEmpty) || copy.Distinct().Count() != copy.Length)
                throw new ArgumentException("통과할 노드 ID는 비어 있거나 중복될 수 없습니다.", nameof(bypassedNodes));
            Reason = reason;
            TargetNodeId = targetNodeId;
            TargetStageKey = targetStageKey;
            BypassedNodes = Array.AsReadOnly(copy);
            NavigationFailureReason = navigationFailureReason;
        }
    }
}
#endif
