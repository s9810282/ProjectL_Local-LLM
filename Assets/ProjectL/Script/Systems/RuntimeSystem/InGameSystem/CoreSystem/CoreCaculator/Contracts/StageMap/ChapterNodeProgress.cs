using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    /// <summary>A command-local copy of one StageProgressBuffer node; it is not a state owner.</summary>
    public sealed class ChapterNodeProgress
    {
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public int Depth { get; }
        public bool IsClear { get; }
        public IReadOnlyList<StageNodeId> NextNodeIds { get; }

        public ChapterNodeProgress(StageNodeId nodeId, StageKey stageKey, int depth,
            bool isClear, IEnumerable<StageNodeId> nextNodeIds)
        {
            if (nodeId.IsEmpty) throw new ArgumentException("Node ID is required.", nameof(nodeId));
            if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth));
            if (stageKey.IsEmpty != (depth == 0))
                throw new ArgumentException("Only the start node has an empty stage key.", nameof(stageKey));
            if (nextNodeIds == null) throw new ArgumentNullException(nameof(nextNodeIds));

            var copied = new List<StageNodeId>();
            var seen = new HashSet<StageNodeId>();
            foreach (StageNodeId next in nextNodeIds)
            {
                if (next.IsEmpty || next == nodeId || !seen.Add(next))
                    throw new ArgumentException("Next node IDs must be distinct, nonempty, and not self.", nameof(nextNodeIds));
                copied.Add(next);
            }

            NodeId = nodeId;
            StageKey = stageKey;
            Depth = depth;
            IsClear = isClear;
            NextNodeIds = copied.AsReadOnly();
        }
    }
}
