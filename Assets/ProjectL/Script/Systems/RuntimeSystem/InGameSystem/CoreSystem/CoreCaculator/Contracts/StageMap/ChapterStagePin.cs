using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    /// <summary>
    /// 새로 만드는 챕터 그래프에서 중간 노드 하나의 스테이지를 정해 둡니다.
    /// 그래프 모양(노드·깊이·연결)은 고정이 없을 때와 같습니다. 다른 노드의 스테이지는 달라질 수 있지만,
    /// 종류별 최소 개수는 고정 노드를 포함해 그대로 지킵니다. 지금은 개발 진입만 씁니다.
    /// </summary>
    public sealed class ChapterStagePin
    {
        /// <summary>스테이지를 놓을 노드입니다. 시작·보스 노드가 아닌 중간 층이어야 합니다.</summary>
        public StageNodeId NodeId { get; }
        /// <summary>놓을 스테이지입니다. 그 맵의 일반 스테이지여야 합니다.</summary>
        public StageKey StageKey { get; }

        public ChapterStagePin(StageNodeId nodeId, StageKey stageKey)
        {
            if (nodeId.IsEmpty) throw new ArgumentException("고정할 노드가 비어 있습니다.", nameof(nodeId));
            if (stageKey.IsEmpty) throw new ArgumentException("고정할 스테이지가 비어 있습니다.", nameof(stageKey));
            NodeId = nodeId;
            StageKey = stageKey;
        }
    }
}
