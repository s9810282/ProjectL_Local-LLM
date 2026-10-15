using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap
{
    public sealed class ChapterContinuationFacts
    {
        public RunOutcome Outcome { get; }
        public int RequestedChapterKey { get; }
        public int CurrentChapterKey { get; }
        public StageNodeId RequestedBossNodeId { get; }
        public StageNodeId SelectedNodeId { get; }
        public StageKey SelectedStageKey { get; }
        public bool HasSelection { get; }
        public bool HasBattle { get; }
        public bool SourceMapClear { get; }
        public bool SourceNodeClear { get; }
        public bool IsCombatBoss { get; }
        public bool MatchingVictory { get; }
        public bool TargetRegistered { get; }
        public bool TargetClear { get; }

        public ChapterContinuationFacts(RunOutcome outcome, int requestedChapterKey, int currentChapterKey,
            StageNodeId requestedBossNodeId, StageNodeId selectedNodeId, StageKey selectedStageKey,
            bool hasSelection, bool hasBattle, bool sourceMapClear, bool sourceNodeClear, bool isCombatBoss,
            bool matchingVictory, bool targetRegistered, bool targetClear)
        {
            if (!Enum.IsDefined(typeof(RunOutcome), outcome)) throw new ArgumentOutOfRangeException(nameof(outcome));
            if (requestedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(requestedChapterKey));
            if (currentChapterKey < 0) throw new ArgumentOutOfRangeException(nameof(currentChapterKey));
            if (requestedBossNodeId.IsEmpty) throw new ArgumentException("완료할 보스 노드가 필요합니다.", nameof(requestedBossNodeId));
            Outcome = outcome; RequestedChapterKey = requestedChapterKey; CurrentChapterKey = currentChapterKey;
            RequestedBossNodeId = requestedBossNodeId; SelectedNodeId = selectedNodeId; SelectedStageKey = selectedStageKey;
            HasSelection = hasSelection; HasBattle = hasBattle; SourceMapClear = sourceMapClear;
            SourceNodeClear = sourceNodeClear; IsCombatBoss = isCombatBoss; MatchingVictory = matchingVictory;
            TargetRegistered = targetRegistered; TargetClear = targetClear;
        }
    }
}
