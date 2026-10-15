using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap
{
    public static class ChapterContinuationRules
    {
        public static ChapterContinuationResult Evaluate(ChapterContinuationFacts facts, ChapterContinuationDefinition definition)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (facts.Outcome != RunOutcome.InProgress) return Reject(ChapterContinuationFailureReason.RunEnded);
            if (facts.HasBattle) return Reject(ChapterContinuationFailureReason.BattleRetained);
            if (facts.CurrentChapterKey != facts.RequestedChapterKey) return Reject(ChapterContinuationFailureReason.ChapterMismatch);
            if (!facts.HasSelection || facts.SelectedNodeId.IsEmpty || facts.SelectedStageKey.IsEmpty)
                return Reject(ChapterContinuationFailureReason.StageNotFound);
            if (facts.SelectedNodeId != facts.RequestedBossNodeId) return Reject(ChapterContinuationFailureReason.StageMismatch);
            if (!facts.SourceMapClear || !facts.SourceNodeClear) return Reject(ChapterContinuationFailureReason.ChapterNotCleared);
            if (!facts.IsCombatBoss) return Reject(ChapterContinuationFailureReason.InvalidBoss);
            if (!facts.MatchingVictory) return Reject(ChapterContinuationFailureReason.CompletionMismatch);
            if (definition == null) return Reject(ChapterContinuationFailureReason.DefinitionUnavailable);
            if (definition.ChapterKey != facts.RequestedChapterKey) return Reject(ChapterContinuationFailureReason.InvalidDefinition);
            if (definition.Kind == ChapterContinuationDefinitionKind.FinalChapter)
                return ChapterContinuationResult.FinishRun(facts.RequestedChapterKey, facts.SelectedNodeId, facts.SelectedStageKey);
            if (!facts.TargetRegistered) return Reject(ChapterContinuationFailureReason.TargetNotRegistered);
            if (facts.TargetClear) return Reject(ChapterContinuationFailureReason.TargetAlreadyCleared);
            return ChapterContinuationResult.SelectNext(facts.RequestedChapterKey, facts.SelectedNodeId, facts.SelectedStageKey, definition.NextChapterKey);
        }

        /// <summary>챕터 계속의 근거가 되는 완료가 해당 보스 노드의 승리(스테이지·챕터 클리어, 시도 계속)인지 판단한다.</summary>
        public static bool IsMatchingVictory(BattleCompletionChanges completion, int chapterKey, int nodeId, int stageKey)
            => completion != null && completion.Outcome.Outcome == StageOutcome.Victory &&
                completion.Progression.Accepted && completion.Progression.ClearStage && completion.Progression.ClearMap &&
                !completion.Progression.EndAttempt && completion.Progression.ChapterKey == chapterKey &&
                completion.Progression.NodeId.Value == nodeId && completion.Progression.StageKey.Value == stageKey;

        private static ChapterContinuationResult Reject(ChapterContinuationFailureReason reason) => ChapterContinuationResult.Reject(reason);
    }
}
