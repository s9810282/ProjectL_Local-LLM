using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>여러 Buffer 거래가 함께 쓰는 진입 조건 검사다. 읽기만 하고 저장소를 바꾸지 않는다.</summary>
    internal sealed class BufferGuards
    {
        private readonly IRunProgressBufferReadSink _run;
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly ICombatStateBufferReadSink _combat;

        internal BufferGuards(IRunProgressBufferReadSink run, ICurrentProgressBufferReadSink current,
            ICombatStateBufferReadSink combat)
        {
            _run = run ?? throw new System.ArgumentNullException(nameof(run));
            _current = current ?? throw new System.ArgumentNullException(nameof(current));
            _combat = combat ?? throw new System.ArgumentNullException(nameof(combat));
        }

        internal void EnsureRunInProgress()
        {
            if (_run.Outcome != RunOutcome.InProgress)
                throw new System.InvalidOperationException("RunEnded: 이미 종료된 Run입니다.");
        }

        internal void EnsureActiveBattle()
        {
            EnsureRunInProgress();
            if (!_combat.HasBattle || _combat.Completion != null)
                throw new System.InvalidOperationException("처리할 활성 전투가 없습니다.");
        }

        internal void ValidateSelection(int mapKey, StageNodeId nodeId, StageKey stageKey)
        {
            if (!_current.HasSelection || _current.CurrentMapKey != mapKey ||
                _current.CurrentStage.NodeId != nodeId.Value ||
                _current.CurrentStage.StageKey != stageKey.Value)
                throw new System.InvalidOperationException("제안이 현재 선택한 스테이지와 다릅니다.");
        }
    }
}
