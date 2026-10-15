using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum ChapterContinuationKind { None, NextChapterSelected, RunCompleted }
    public enum ChapterContinuationFailureReason
    {
        None, RunEnded, BattleRetained, ChapterMismatch, StageNotFound, StageMismatch, ChapterNotCleared,
        InvalidBoss, CompletionMismatch, DefinitionUnavailable, InvalidDefinition, TargetNotRegistered, TargetAlreadyCleared
    }

    public sealed class ChapterContinuationResult
    {
        public bool Accepted => FailureReason == ChapterContinuationFailureReason.None;
        public ChapterContinuationFailureReason FailureReason { get; }
        public ChapterContinuationKind Kind { get; }
        public int ChapterKey { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public int NextChapterKey { get; }
        /// <summary>마지막 챕터 완료로 Run을 승리로 끝낼 때 Core가 확정한 영수증이다. 다른 경우 null이다.</summary>
        public RunCompletionReceipt RunCompletion { get; }

        private ChapterContinuationResult(ChapterContinuationFailureReason reason, ChapterContinuationKind kind,
            int chapterKey, StageNodeId nodeId, StageKey stageKey, int nextChapterKey)
        {
            FailureReason = reason; Kind = kind; ChapterKey = chapterKey; NodeId = nodeId; StageKey = stageKey; NextChapterKey = nextChapterKey;
            RunCompletion = kind == ChapterContinuationKind.RunCompleted
                ? new RunCompletionReceipt(Common.RunOutcome.Victory, chapterKey, nodeId, stageKey) : null;
        }

        public static ChapterContinuationResult Reject(ChapterContinuationFailureReason reason)
        {
            if (reason == ChapterContinuationFailureReason.None || !Enum.IsDefined(typeof(ChapterContinuationFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new(reason, ChapterContinuationKind.None, 0, StageNodeId.Empty, StageKey.Empty, 0);
        }

        public static ChapterContinuationResult SelectNext(int chapterKey, StageNodeId nodeId, StageKey stageKey, int nextChapterKey)
        {
            Validate(chapterKey, nodeId, stageKey);
            if (nextChapterKey < 1 || nextChapterKey == chapterKey) throw new ArgumentOutOfRangeException(nameof(nextChapterKey));
            return new(ChapterContinuationFailureReason.None, ChapterContinuationKind.NextChapterSelected, chapterKey, nodeId, stageKey, nextChapterKey);
        }

        public static ChapterContinuationResult FinishRun(int chapterKey, StageNodeId nodeId, StageKey stageKey)
        {
            Validate(chapterKey, nodeId, stageKey);
            return new(ChapterContinuationFailureReason.None, ChapterContinuationKind.RunCompleted, chapterKey, nodeId, stageKey, 0);
        }

        private static void Validate(int chapterKey, StageNodeId nodeId, StageKey stageKey)
        {
            if (chapterKey < 1 || nodeId.IsEmpty || stageKey.IsEmpty) throw new ArgumentException("챕터 완료 식별자가 필요합니다.");
        }
    }
}
