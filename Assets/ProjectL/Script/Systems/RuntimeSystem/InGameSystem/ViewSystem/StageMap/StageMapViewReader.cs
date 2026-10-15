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
            if (!_current.HasMap)
                return false;

            int currentMapKey = _current.CurrentMapKey;
            IReadOnlyList<StageProgressData> currentNodes = _stages.GetStages(currentMapKey);
            if (currentNodes.Count == 0)
                return false;

            mapKey = currentMapKey;
            nodes = currentNodes;
            return true;
        }
    }
}
