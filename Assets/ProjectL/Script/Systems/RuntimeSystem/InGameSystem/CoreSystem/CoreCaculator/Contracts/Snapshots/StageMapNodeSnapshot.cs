using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// View가 노드 하나를 표시하기 위해 필요한 데이터 제공
    /// </summary>
    public sealed class StageMapNodeSnapshot
    {
        /// <summary>
        /// View가 클릭 후 코어에 다시 전달할 식별자
        /// </summary>
        public StageNodeId NodeId { get; }
        /// <summary>
        /// 이 노드가 어떤 실제 스테이지를 가르키는 지
        /// </summary>
        public StageKey StageKey { get; }
        public StageExecutionKind ExecutionKind { get; }
        public int Depth { get; }
        public int WidthIndex { get; }

        public bool IsStart { get; }
        public bool IsBoss { get; }
        /// <summary>
        /// 현재 플레이어 위치 강조
        /// </summary>
        public bool IsCurrent { get; }
        /// <summary>
        /// 현재 UI에서 선택 가능한 후보 표시
        /// </summary>
        public bool IsSelectable { get; }
        /// <summary>
        /// 연결선 표시
        /// </summary>
        public ReadOnlyMemory<StageNodeId> NextNodeIds { get; }

        public StageMapNodeSnapshot(
            StageNodeId nodeId,
            StageKey stageKey,
            StageExecutionKind executionKind,
            int depth,
            int widthIndex,
            bool isStart,
            bool isBoss,
            bool isCurrent,
            bool isSelectable,
            StageNodeId[] nextNodeIds)
        {
            if (nodeId.IsEmpty)
                throw new ArgumentException(nameof(nodeId));
            if (!Enum.IsDefined(typeof(StageExecutionKind), executionKind))
                throw new ArgumentOutOfRangeException(nameof(executionKind));
            if (depth < 0) 
                throw new ArgumentOutOfRangeException(nameof(depth));
            if (widthIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(widthIndex));
            if (nextNodeIds == null)
                throw new ArgumentNullException(nameof(nextNodeIds));

            StageNodeId[] copiedStageNodeIds = new StageNodeId[nextNodeIds.Length];
            Array.Copy(nextNodeIds, copiedStageNodeIds, nextNodeIds.Length);

            NodeId = nodeId;
            StageKey = stageKey;
            ExecutionKind = executionKind;
            Depth = depth;
            WidthIndex = widthIndex;
            IsStart = isStart;
            IsBoss = isBoss;
            IsCurrent = isCurrent;
            IsSelectable = isSelectable;
            NextNodeIds = copiedStageNodeIds;
        }
    }
}
