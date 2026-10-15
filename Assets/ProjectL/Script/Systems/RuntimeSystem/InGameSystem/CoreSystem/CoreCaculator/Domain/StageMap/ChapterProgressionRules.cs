using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap
{
    /// <summary>Buffer에서 받은 값으로 한 명령의 진행 결과만 계산한다.</summary>
    public static class ChapterProgressionRules
    {
        public static ChapterProgressionResult EvaluateChapterEntry(int chapterKey, bool isChapterClear)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            return isChapterClear
                ? ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterCompleted)
                : ChapterProgressionResult.EnterChapter(chapterKey);
        }

        public static ChapterProgressionResult EvaluateNodeClick(
            int requestedChapterKey, int currentChapterKey, int graphChapterKey,
            StageNodeId fromNodeId, bool isChapterClear,
            IReadOnlyList<ChapterNodeProgress> nodes, StageNodeId targetNodeId)
        {
            ChapterSelectionResult selection = ChapterNavigationRules.Evaluate(
                requestedChapterKey, currentChapterKey, graphChapterKey,
                fromNodeId, isChapterClear, nodes, targetNodeId);
            return selection.Accepted
                ? ChapterProgressionResult.EnterStage(currentChapterKey,
                    selection.TargetNodeId, selection.TargetStageKey)
                : ChapterProgressionResult.Reject(
                    ChapterProgressionFailureReason.NavigationRejected, selection.FailureReason);
        }

        /// <summary>전투 결과를 만들지 않고 선택한 노드의 완료만 판단한다.</summary>
        public static ChapterProgressionResult EvaluateRecoveryCompletion(
            int requestedChapterKey, int currentChapterKey, int graphChapterKey,
            StageNodeId selectedNodeId, StageKey selectedStageKey, bool isChapterClear,
            IReadOnlyList<ChapterNodeProgress> nodes)
            => EvaluatePreparedStageCompletion(requestedChapterKey, currentChapterKey, graphChapterKey,
                selectedNodeId, selectedStageKey, isChapterClear, nodes);

        public static ChapterProgressionResult EvaluateShopCompletion(
            int requestedChapterKey, int currentChapterKey, int graphChapterKey,
            StageNodeId selectedNodeId, StageKey selectedStageKey, bool isChapterClear,
            IReadOnlyList<ChapterNodeProgress> nodes)
            => EvaluatePreparedStageCompletion(requestedChapterKey, currentChapterKey, graphChapterKey,
                selectedNodeId, selectedStageKey, isChapterClear, nodes);

        private static ChapterProgressionResult EvaluatePreparedStageCompletion(
            int requestedChapterKey, int currentChapterKey, int graphChapterKey,
            StageNodeId selectedNodeId, StageKey selectedStageKey, bool isChapterClear,
            IReadOnlyList<ChapterNodeProgress> nodes)
        {
            if (requestedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(requestedChapterKey));
            if (currentChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(currentChapterKey));
            if (graphChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(graphChapterKey));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (requestedChapterKey != currentChapterKey || graphChapterKey != currentChapterKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (isChapterClear)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterCompleted);

            ChapterNodeProgress selected = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                ChapterNodeProgress node = nodes[i] ?? throw new ArgumentException("노드 목록에 null이 있습니다.", nameof(nodes));
                if (node.NodeId == selectedNodeId) selected = node;
            }
            if (selected == null || selected.StageKey.IsEmpty)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            if (selected.StageKey != selectedStageKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            if (selected.IsClear)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageAlreadyCleared);
            return ChapterProgressionResult.FinishStage(currentChapterKey, selectedNodeId, selectedStageKey, false);
        }

        public static ChapterProgressionResult EvaluateStageOutcome(
            int requestedChapterKey, int currentChapterKey, int graphChapterKey,
            StageNodeId selectedNodeId, StageKey selectedStageKey,
            bool isChapterClear, IReadOnlyList<ChapterNodeProgress> nodes,
            StageOutcome outcome, bool isBossStage)
        {
            if (requestedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(requestedChapterKey));
            if (currentChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(currentChapterKey));
            if (graphChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(graphChapterKey));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            if (requestedChapterKey != currentChapterKey || graphChapterKey != currentChapterKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (isChapterClear)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterCompleted);
            if (outcome != StageOutcome.Victory && outcome != StageOutcome.Defeat)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.InvalidOutcome);

            ChapterNodeProgress selected = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                ChapterNodeProgress node = nodes[i] ??
                    throw new ArgumentException("Node list contains null.", nameof(nodes));
                if (node.NodeId == selectedNodeId) selected = node;
            }

            if (selected == null || selected.StageKey.IsEmpty)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            if (selected.StageKey != selectedStageKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            if (selected.IsClear)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageAlreadyCleared);
            if (isBossStage && selected.NextNodeIds.Count != 0)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.BossNodeHasSuccessors);

            return outcome == StageOutcome.Defeat
                ? ChapterProgressionResult.Defeat(currentChapterKey, selectedNodeId, selectedStageKey)
                : ChapterProgressionResult.FinishStage(
                    currentChapterKey, selectedNodeId, selectedStageKey, isBossStage);
        }
    }
}
