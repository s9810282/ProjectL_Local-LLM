using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.StageMap
{
    /// <summary>Calculates chapter navigation from Buffer-supplied command values without retaining them.</summary>
    public static class ChapterNavigationRules
    {
        public static ChapterSelectionResult Evaluate(int requestedChapterKey, int currentChapterKey,
            int graphChapterKey,
            StageNodeId fromNodeId, bool isChapterClear,
            IReadOnlyList<ChapterNodeProgress> nodes, StageNodeId targetNodeId)
        {
            if (requestedChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(requestedChapterKey));
            if (currentChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(currentChapterKey));
            if (graphChapterKey < 1) throw new ArgumentOutOfRangeException(nameof(graphChapterKey));
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            if (requestedChapterKey != currentChapterKey || graphChapterKey != currentChapterKey)
                return ChapterSelectionResult.Reject(ChapterSelectionFailureReason.ChapterMismatch);
            if (isChapterClear)
                return ChapterSelectionResult.Reject(ChapterSelectionFailureReason.ChapterCompleted);

            ChapterNodeProgress current = null;
            ChapterNodeProgress target = null;
            for (int i = 0; i < nodes.Count; i++)
            {
                ChapterNodeProgress node = nodes[i] ??
                    throw new ArgumentException("Node list contains null.", nameof(nodes));
                if (fromNodeId.IsEmpty)
                {
                    if (node.Depth == 0 && node.StageKey.IsEmpty)
                        current = node;
                }
                else if (node.NodeId == fromNodeId)
                    current = node;

                if (node.NodeId == targetNodeId)
                    target = node;
            }

            if (current == null)
                return ChapterSelectionResult.Reject(ChapterSelectionFailureReason.CurrentNodeNotFound);
            if (!current.StageKey.IsEmpty && !current.IsClear)
                return ChapterSelectionResult.Reject(ChapterSelectionFailureReason.CurrentStageUncleared);
            if (target == null)
                return ChapterSelectionResult.Reject(ChapterSelectionFailureReason.NodeNotFound);
            if (target.IsClear)
                return ChapterSelectionResult.Reject(ChapterSelectionFailureReason.AlreadyCleared);
            if (current.NextNodeIds.Count == 0)
                return ChapterSelectionResult.Reject(ChapterSelectionFailureReason.FinalNodeReached);

            for (int i = 0; i < current.NextNodeIds.Count; i++)
                if (current.NextNodeIds[i] == targetNodeId)
                {
                    if (target.StageKey.IsEmpty || target.Depth != current.Depth + 1)
                        return ChapterSelectionResult.Reject(ChapterSelectionFailureReason.NotConnected);
                    return ChapterSelectionResult.Accept(targetNodeId, target.StageKey);
                }

            return ChapterSelectionResult.Reject(ChapterSelectionFailureReason.NotConnected);
        }
    }
}
