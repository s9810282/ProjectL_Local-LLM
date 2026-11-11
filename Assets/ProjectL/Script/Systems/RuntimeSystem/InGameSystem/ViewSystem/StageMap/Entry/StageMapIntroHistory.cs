using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>실제 최신 Run의 챕터별 연출 시작 이력입니다. 진행 Buffer에는 쓰지 않습니다.</summary>
    internal sealed class StageMapIntroHistory
    {
        private readonly IRunProgressBufferReadSink _runs;
        private readonly HashSet<int> _started = new();
        private RunId _runId;

        internal StageMapIntroHistory(IRunProgressBufferReadSink runs)
        {
            _runs = runs ?? throw new ArgumentNullException(nameof(runs));
        }

        internal bool HasStarted(RunId runId, int mapKey)
        {
            return IsCurrent(runId, mapKey) && _started.Contains(mapKey);
        }

        internal bool TryMarkStarted(RunId runId, int mapKey)
        {
            return IsCurrent(runId, mapKey) && _started.Add(mapKey);
        }

        private bool IsCurrent(RunId runId, int mapKey)
        {
            RunId current = _runs.RunId;
            if (_runId != current)
            {
                _started.Clear();
                _runId = current;
            }
            return !runId.IsEmpty && runId == current && mapKey > 0;
        }
    }
}
