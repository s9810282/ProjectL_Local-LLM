using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion
{
    /// <summary>최신 Run의 수락된 방문과 연출 소비만 기록합니다. 클리어 사실은 저장하지 않습니다.</summary>
    internal sealed class StageNodeClearHistory
    {
        private readonly IRunProgressBufferReadSink _runs;
        // 키가 있으면 수락된 방문이며, 값이 true면 파괴 연출을 이미 소비했습니다.
        private readonly Dictionary<StageNodeClearKey, bool> _visits = new();
        private RunId _runId;

        internal StageNodeClearHistory(IRunProgressBufferReadSink runs)
        {
            _runs = runs ?? throw new ArgumentNullException(nameof(runs));
        }

        /// <summary>늦은 이전 Run의 방문은 버리고, 같은 방문의 소비 상태는 되돌리지 않습니다.</summary>
        internal void Arm(StageNodeClearKey key)
        {
            if (IsCurrent(key)) _visits.TryAdd(key, false);
        }

        internal bool IsPending(StageNodeClearKey key) =>
            IsCurrent(key) && _visits.TryGetValue(key, out bool consumed) && !consumed;

        internal bool TryConsume(StageNodeClearKey key)
        {
            if (!IsPending(key)) return false;
            _visits[key] = true;
            return true;
        }

        private bool IsCurrent(StageNodeClearKey key)
        {
            RunId current = _runs.RunId;
            if (_runId != current)
            {
                _visits.Clear();
                _runId = current;
            }
            return !key.RunId.IsEmpty && key.RunId == current && key.MapKey > 0 && key.NodeId > 0;
        }
    }
}
