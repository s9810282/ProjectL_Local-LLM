using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
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

        /// <summary>대상 챕터가 미완료이고, 대상 노드가 Core 판정 때와 같은 종류·역할의 미완료 노드인지 본다.</summary>
        internal static bool IsOpenTarget(IMapProgressBufferReadSink map, IStageProgressBufferReadSink stages,
            int chapterKey, StageNodeId nodeId, StageKey stageKey,
            StageType stageType, StageMapRole mapRole)
        {
            return !map.GetMap(chapterKey).IsClear &&
                stages.GetStages(chapterKey).Any(stage =>
                    stage.NodeId == nodeId.Value && stage.StageKey == stageKey.Value &&
                    !stage.IsClear && stage.StageType == stageType && stage.MapRole == mapRole);
        }
    }
}
