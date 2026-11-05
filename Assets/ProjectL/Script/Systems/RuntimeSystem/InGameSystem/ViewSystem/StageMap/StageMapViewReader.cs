using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>Core가 준비한 현재 맵의 노드 정보를 읽습니다. 상태 생성과 변경은 담당하지 않습니다.</summary>
    internal sealed class StageMapViewReader
    {
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IStageProgressBufferReadSink _stages;
        private int _readMapKey;
        private IReadOnlyList<StageProgressData> _readNodes = Array.Empty<StageProgressData>();

        internal StageMapViewReader(ICurrentProgressBufferReadSink current,
            IStageProgressBufferReadSink stages)
        {
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
        }

        /// <summary>
        /// 표시할 맵과 노드가 준비되면 true를 반환합니다.
        /// 실패 시 키는 0, 목록은 빈 목록입니다. 반환 목록은 고정 Snapshot이 아니므로
        /// 표시 갱신 시 다시 조회하며, Core 초기화·갱신과 동시에 조회하지 않습니다.
        /// </summary>
        internal bool TryRead(out int mapKey, out IReadOnlyList<StageProgressData> nodes)
        {
            mapKey = 0;
            nodes = Array.Empty<StageProgressData>();
            _readMapKey = 0;
            _readNodes = Array.Empty<StageProgressData>();
            if (!_current.HasMap)
                return false;

            int currentMapKey = _current.CurrentMapKey;
            IReadOnlyList<StageProgressData> currentNodes = _stages.GetStages(currentMapKey);
            if (currentNodes.Count == 0)
                return false;

            mapKey = currentMapKey;
            nodes = currentNodes;
            _readMapKey = currentMapKey;
            _readNodes = currentNodes;
            return true;
        }

        /// <summary>
        /// 마지막으로 읽은 맵과 현재 Buffer의 맵이 같을 때 현재 노드를 읽습니다.
        /// 첫 진입 전에는 그 맵의 시작 노드를 사용하며, 매 Tick GetStages 래퍼를 새로 만들지 않습니다.
        /// </summary>
        internal bool TryReadCurrentNode(out int nodeId)
        {
            nodeId = 0;
            if (!_current.HasMap || _current.CurrentMapKey != _readMapKey)
                return false;

            if (_current.HasSelection)
            {
                nodeId = _current.CurrentStage.NodeId;
                return nodeId > 0;
            }

            for (int i = 0; i < _readNodes.Count; i++)
            {
                StageProgressData node = _readNodes[i];
                if (!node.IsStart) continue;
                nodeId = node.NodeId;
                return true;
            }
            return false;
        }
    }
}
