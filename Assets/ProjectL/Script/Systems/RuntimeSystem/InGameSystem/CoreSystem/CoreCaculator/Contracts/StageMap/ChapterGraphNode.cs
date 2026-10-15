using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    /// <summary>생성된 챕터 그래프의 노드 하나다. 시작 노드만 스테이지 키가 0이다.</summary>
    public sealed class ChapterGraphNode
    {
        public int NodeId { get; }
        public int StageKey { get; }
        public int Depth { get; }
        public int WidthIndex { get; }
        public IReadOnlyList<int> NextNodeIds { get; }

        public ChapterGraphNode(int nodeId, int stageKey, int depth, int widthIndex, IReadOnlyList<int> nextNodeIds)
        {
            if (nodeId < 1) throw new ArgumentOutOfRangeException(nameof(nodeId));
            if (stageKey < 0) throw new ArgumentOutOfRangeException(nameof(stageKey));
            if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth));
            if (widthIndex < 0) throw new ArgumentOutOfRangeException(nameof(widthIndex));
            if (nextNodeIds == null) throw new ArgumentNullException(nameof(nextNodeIds));

            var next = new int[nextNodeIds.Count];
            for (int i = 0; i < next.Length; i++) next[i] = nextNodeIds[i];
            NodeId = nodeId;
            StageKey = stageKey;
            Depth = depth;
            WidthIndex = widthIndex;
            NextNodeIds = Array.AsReadOnly(next);
        }
    }
}
