using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// 전체 맵을 View가 한 번에 읽을 수 있는 데이터
    /// </summary>
    public sealed class StageMapSnapshot
    {
        public int DepthCount { get; }
        public int WidthMax { get; }
        public StageNodeId CurrentNodeId { get; }
        public bool IsAtFinalNode { get; }
        public ReadOnlyMemory<StageMapNodeSnapshot> Nodes { get; }

        public StageMapSnapshot(int depthCount, int widthMax, StageNodeId currentNodeId, bool isAtFinalNode, StageMapNodeSnapshot[] nodes)
        {
            if (depthCount < 2)
                throw new ArgumentOutOfRangeException(nameof(depthCount));
            if (widthMax < 1)
                throw new ArgumentOutOfRangeException(nameof(widthMax));
            if (currentNodeId.IsEmpty)
                throw new ArgumentException(nameof(currentNodeId));
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));
            if (nodes.Length == 0)
                throw new ArgumentException("Nodes cannot be empty.", nameof(nodes));

            StageMapNodeSnapshot[] copiedNodes = new StageMapNodeSnapshot[nodes.Length];
            Array.Copy(nodes, copiedNodes, nodes.Length);

            DepthCount = depthCount;
            WidthMax = widthMax;
            CurrentNodeId = currentNodeId;
            IsAtFinalNode = isAtFinalNode;
            Nodes = copiedNodes;
        }
    }
}
