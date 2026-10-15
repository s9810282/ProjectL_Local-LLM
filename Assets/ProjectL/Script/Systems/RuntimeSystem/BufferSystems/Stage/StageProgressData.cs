using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Common;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Stage
{
    /// <summary>Init에서 생성한 노드의 식별·배치·연결 정보와 클리어 상태를 보관하는 기록입니다.</summary>
    [Serializable]
    public readonly struct StageProgressData
    {
        public int NodeId { get; }
        public int StageKey { get; }
        public bool IsClear { get; }
        public int Depth { get; }
        public int WidthIndex { get; }
        /// <summary>노드가 가리키는 스테이지의 종류입니다. 시작 노드(StageKey 0)는 기본값이며 의미가 없습니다.</summary>
        public StageType StageType { get; }
        /// <summary>노드가 가리키는 스테이지의 맵 역할입니다. 시작 노드(StageKey 0)는 기본값이며 의미가 없습니다.</summary>
        public StageMapRole MapRole { get; }
        private readonly IReadOnlyList<int> nextNodeIds;
        public IReadOnlyList<int> NextNodeIds => nextNodeIds ?? Array.Empty<int>();

        public StageProgressData(int nodeId, int stageKey, int depth, int widthIndex,
            IEnumerable<int> nextNodeIds, bool isClear = false,
            StageType stageType = default, StageMapRole mapRole = default)
        {
            if (nodeId < 1) throw new ArgumentOutOfRangeException(nameof(nodeId));
            // 시작 노드는 실제 스테이지가 없으므로 StageKey 0을 사용합니다.
            if (stageKey < 0 || (depth > 0 && stageKey == 0))
                throw new ArgumentOutOfRangeException(nameof(stageKey));
            if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth));
            if (widthIndex < 0) throw new ArgumentOutOfRangeException(nameof(widthIndex));
            if (nextNodeIds == null) throw new ArgumentNullException(nameof(nextNodeIds));
            var connections = new List<int>();
            var seen = new HashSet<int>();
            foreach (int nextId in nextNodeIds)
            {
                if (nextId < 1 || nextId == nodeId || !seen.Add(nextId))
                    throw new ArgumentException("Connections require positive, distinct node IDs other than this node.", nameof(nextNodeIds));
                connections.Add(nextId);
            }
            StageKey = stageKey;
            NodeId = nodeId;
            IsClear = isClear;
            Depth = depth;
            WidthIndex = widthIndex;
            StageType = stageType;
            MapRole = mapRole;
            this.nextNodeIds = connections.AsReadOnly();
        }
    }
}
