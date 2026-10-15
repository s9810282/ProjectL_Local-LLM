using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Chapter
{
    public static class ChapterProgressionCommands
    {
        public static ChapterProgressionResult EnterChapter(
            int requestedChapterKey, int currentChapterKey,
            bool isCurrentChapterClear, bool isRequestedChapterClear,
            bool hasRegisteredGraph)
        {
            if (requestedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(requestedChapterKey));
            if (currentChapterKey < 0) throw new ArgumentOutOfRangeException(nameof(currentChapterKey));
            if (currentChapterKey != 0 && currentChapterKey != requestedChapterKey &&
                !isCurrentChapterClear)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (isRequestedChapterClear)
                return ChapterProgressionRules.EvaluateChapterEntry(requestedChapterKey, true);
            if (!hasRegisteredGraph)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            return ChapterProgressionRules.EvaluateChapterEntry(
                requestedChapterKey, isRequestedChapterClear);
        }

        public static ChapterProgressionResult ClickNode(
            int requestedChapterKey, int currentChapterKey, int graphChapterKey,
            StageNodeId fromNodeId, bool isChapterClear,
            IReadOnlyList<ChapterNodeProgress> nodes, StageNodeId targetNodeId)
        {
            if (requestedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(requestedChapterKey));
            if (currentChapterKey == 0)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (nodes.Count == 0)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            return ChapterProgressionRules.EvaluateNodeClick(
                requestedChapterKey, currentChapterKey, graphChapterKey,
                fromNodeId, isChapterClear, nodes, targetNodeId);
        }

        public static ChapterProgressionResult CompleteRecoveryStage(
            int requestedChapterKey, int currentChapterKey, int graphChapterKey,
            bool hasEnteredStage, StageNodeId enteredNodeId, StageKey enteredStageKey,
            bool isChapterClear, IReadOnlyList<ChapterNodeProgress> nodes)
        {
            if (requestedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(requestedChapterKey));
            if (currentChapterKey == 0)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (!hasEnteredStage)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            return ChapterProgressionRules.EvaluateRecoveryCompletion(
                requestedChapterKey, currentChapterKey, graphChapterKey,
                enteredNodeId, enteredStageKey, isChapterClear, nodes);
        }

        public static ChapterProgressionResult CompleteShopStage(
            int requestedChapterKey, int currentChapterKey, int graphChapterKey,
            bool hasEnteredStage, StageNodeId enteredNodeId, StageKey enteredStageKey,
            bool isChapterClear, IReadOnlyList<ChapterNodeProgress> nodes)
        {
            if (requestedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(requestedChapterKey));
            if (currentChapterKey == 0)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (!hasEnteredStage)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            return ChapterProgressionRules.EvaluateShopCompletion(requestedChapterKey, currentChapterKey, graphChapterKey,
                enteredNodeId, enteredStageKey, isChapterClear, nodes);
        }

        public static ChapterProgressionResult ResolveStage(
            int requestedChapterKey, int currentChapterKey, int graphChapterKey,
            bool hasEnteredStage, StageNodeId enteredNodeId, StageKey enteredStageKey,
            bool isChapterClear, IReadOnlyList<ChapterNodeProgress> nodes,
            StageOutcome outcome, bool isBossStage)
        {
            if (requestedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(requestedChapterKey));
            if (currentChapterKey == 0)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (!hasEnteredStage)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            return ChapterProgressionRules.EvaluateStageOutcome(
                requestedChapterKey, currentChapterKey, graphChapterKey,
                enteredNodeId, enteredStageKey, isChapterClear, nodes,
                outcome, isBossStage);
        }
    }
}
