using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum ChapterSelectionFailureReason
    {
        None = 0,
        ChapterMismatch,
        ChapterCompleted,
        CurrentNodeNotFound,
        CurrentStageUncleared,
        NodeNotFound,
        AlreadyCleared,
        FinalNodeReached,
        NotConnected
    }

    /// <summary>A rule decision. The caller applies accepted changes through Buffer APIs.</summary>
    public sealed class ChapterSelectionResult
    {
        public bool Accepted => FailureReason == ChapterSelectionFailureReason.None;
        public ChapterSelectionFailureReason FailureReason { get; }
        public StageNodeId TargetNodeId { get; }
        public StageKey TargetStageKey { get; }

        private ChapterSelectionResult(ChapterSelectionFailureReason reason,
            StageNodeId targetNodeId, StageKey targetStageKey)
        {
            FailureReason = reason;
            TargetNodeId = targetNodeId;
            TargetStageKey = targetStageKey;
        }

        public static ChapterSelectionResult Accept(StageNodeId nodeId, StageKey stageKey)
        {
            if (nodeId.IsEmpty || stageKey.IsEmpty)
                throw new ArgumentException("Accepted selection needs a stage node and key.");
            return new ChapterSelectionResult(ChapterSelectionFailureReason.None, nodeId, stageKey);
        }

        public static ChapterSelectionResult Reject(ChapterSelectionFailureReason reason)
        {
            if (reason == ChapterSelectionFailureReason.None ||
                !Enum.IsDefined(typeof(ChapterSelectionFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new ChapterSelectionResult(reason, StageNodeId.Empty, StageKey.Empty);
        }
    }
}
