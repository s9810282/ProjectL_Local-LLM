using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 기본·호버 표시 전용 진입 조회입니다. 2026-11-05 사용자 승인 예외로 공개 조회 API만 사용하며 현재 위치·Buffer를 쓰지 않습니다.
    /// 캐시는 표시용이며 클릭 승인에 사용하지 않습니다. 맵 재표시나 진행 사실 변경 때 버립니다.
    /// </summary>
    internal sealed class StageNodeAvailabilityReader
    {
        private readonly IChapterProgressionCoordinator _progression;
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IStageProgressBufferReadSink _stages;
        private readonly IStageTraitGrowthFacts _growth;
        private IReadOnlyList<StageProgressData> _nodes;
        private readonly Dictionary<int, bool> _decisions = new();
        private bool[] _cleared;
        private int _mapKey;
        private (int Current, bool Growth, RunOutcome Outcome) _key;
        private bool _cached;

        internal StageNodeAvailabilityReader(IChapterProgressionCoordinator progression,
            ICurrentProgressBufferReadSink current, IStageProgressBufferReadSink stages, IStageTraitGrowthFacts growth)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _stages = stages ?? throw new ArgumentNullException(nameof(stages));
            _growth = growth ?? throw new ArgumentNullException(nameof(growth));
        }

        internal void Invalidate()
        {
            _cached = false;
            _nodes = null;
            _cleared = null;
            _mapKey = 0;
            _decisions.Clear();
        }

        /// <summary>표시용 목록에 가능한 노드만 담습니다. 진행이 같으면 Core 조회 결과를 노드별로 재사용합니다.</summary>
        internal void FillAvailable(int chapterKey, IReadOnlyList<int> visibleNodes, HashSet<int> available)
        {
            available.Clear();
            if (chapterKey < 1 || !_current.HasMap || _current.CurrentMapKey != chapterKey)
            {
                Invalidate();
                return;
            }
            if (_nodes == null || _mapKey != chapterKey)
            {
                Invalidate();
                _nodes = _stages.GetStages(chapterKey);
                _cleared = new bool[_nodes.Count];
                _mapKey = chapterKey;
            }
            int currentNode = _current.HasSelection ? _current.CurrentStage.NodeId : 0;
            var key = (currentNode, _growth.IsPending, _progression.Outcome);
            bool changed = !_cached || !_key.Equals(key);
            for (int i = 0; i < _nodes.Count; i++)
            {
                bool cleared = _nodes[i].IsClear;
                if (_cleared[i] != cleared) changed = true;
                _cleared[i] = cleared;
            }
            if (changed) _decisions.Clear();
            _key = key;
            _cached = true;
            for (int i = 0; i < visibleNodes.Count; i++)
            {
                int nodeId = visibleNodes[i];
                if (!_decisions.TryGetValue(nodeId, out bool accepted))
                {
                    // Prepare는 현재 위치를 기록하지 않습니다. 실제 클릭 승인은 기존 진입 경로가 다시 판정합니다.
                    accepted = _progression.PrepareStageEntry(chapterKey, new StageNodeId(nodeId)).Accepted;
                    _decisions.Add(nodeId, accepted);
                }
                if (accepted) available.Add(nodeId);
            }
        }
    }
}
