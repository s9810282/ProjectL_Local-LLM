using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>결과 화면에 제공하는 확정된 기본 정보다. 표시 문구와 미정인 보상·통계 규칙은 포함하지 않는다.</summary>
    public sealed class BattleResultSnapshot
    {
        public int ChapterKey { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        /// <summary>종료 노드의 맵 진행 깊이다. 노드 ID나 스테이지 키를 표시 순번으로 해석하지 않는다.</summary>
        public int StageDepth { get; }
        public StageOutcomeSnapshot BattleOutcome { get; }
        public RunOutcome RunOutcome { get; }
        public bool IsRunEnded => RunOutcome != Common.RunOutcome.InProgress;
        /// <summary>종료 회복 및 임시 최대 체력 제거 후의 체력이다. 영혼의 최대 체력 기여는 유지한다.</summary>
        public HealthStateSnapshot MainHealth { get; }

        public BattleResultSnapshot(int chapterKey, StageNodeId nodeId, StageKey stageKey, int stageDepth,
            StageOutcomeSnapshot battleOutcome, RunOutcome runOutcome, HealthStateSnapshot mainHealth)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (nodeId.IsEmpty || stageKey.IsEmpty) throw new ArgumentException("종료 스테이지 식별자가 필요합니다.");
            if (stageDepth < 0) throw new ArgumentOutOfRangeException(nameof(stageDepth));
            if (!battleOutcome.IsStageEnded) throw new ArgumentException("확정된 전투 종료 결과가 필요합니다.", nameof(battleOutcome));
            if (!Enum.IsDefined(typeof(RunOutcome), runOutcome)) throw new ArgumentOutOfRangeException(nameof(runOutcome));
            if ((battleOutcome.Outcome == StageOutcome.Defeat) != (runOutcome == Common.RunOutcome.Defeat))
                throw new ArgumentException("전투와 회차의 패배 결과가 일치해야 합니다.", nameof(runOutcome));
            if (mainHealth.MaxHealth < 1) throw new ArgumentException("메인 캐릭터의 체력이 필요합니다.", nameof(mainHealth));
            ChapterKey = chapterKey;
            NodeId = nodeId;
            StageKey = stageKey;
            StageDepth = stageDepth;
            BattleOutcome = battleOutcome;
            RunOutcome = runOutcome;
            MainHealth = mainHealth;
        }
    }
}
