using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum ChapterProgressionFailureReason
    {
        None = 0,
        NavigationRejected,
        ChapterMismatch,
        ChapterCompleted,
        StageNotFound,
        StageMismatch,
        StageAlreadyCleared,
        InvalidOutcome,
        BossNodeHasSuccessors,
        RunEnded,
        ContinuationRejected,
        /// <summary>보유 메인 캐릭터가 없다. 새 Run 시작(로스터 설치)을 거치지 않고 전투에 들어가려 한 경우다.</summary>
        RosterNotInitialized
    }

    /// <summary>승인된 명령에 필요한 Buffer 기록만 기술하며 진행 상태를 소유하지 않는다.</summary>
    public sealed class ChapterProgressionResult
    {
        public bool Accepted => FailureReason == ChapterProgressionFailureReason.None;
        public ChapterProgressionFailureReason FailureReason { get; }
        public ChapterSelectionFailureReason NavigationFailureReason { get; }
        public int ChapterKey { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public bool SelectMap { get; }
        public bool SelectStage { get; }
        public bool ClearStage { get; }
        public bool ClearMap { get; }
        public bool EndAttempt { get; }

        private ChapterProgressionResult(ChapterProgressionFailureReason failureReason,
            ChapterSelectionFailureReason navigationFailureReason, int chapterKey,
            StageNodeId nodeId, StageKey stageKey, bool selectMap, bool selectStage,
            bool clearStage, bool clearMap, bool endAttempt)
        {
            FailureReason = failureReason;
            NavigationFailureReason = navigationFailureReason;
            ChapterKey = chapterKey;
            NodeId = nodeId;
            StageKey = stageKey;
            SelectMap = selectMap;
            SelectStage = selectStage;
            ClearStage = clearStage;
            ClearMap = clearMap;
            EndAttempt = endAttempt;
        }

        public static ChapterProgressionResult EnterChapter(int chapterKey)
        {
            ValidateChapter(chapterKey);
            return new ChapterProgressionResult(ChapterProgressionFailureReason.None,
                ChapterSelectionFailureReason.None, chapterKey, StageNodeId.Empty,
                StageKey.Empty, true, false, false, false, false);
        }

        public static ChapterProgressionResult EnterStage(int chapterKey,
            StageNodeId nodeId, StageKey stageKey)
        {
            ValidateStage(chapterKey, nodeId, stageKey);
            return new ChapterProgressionResult(ChapterProgressionFailureReason.None,
                ChapterSelectionFailureReason.None, chapterKey, nodeId, stageKey,
                false, true, false, false, false);
        }

        public static ChapterProgressionResult FinishStage(int chapterKey,
            StageNodeId nodeId, StageKey stageKey, bool bossVictory)
        {
            ValidateStage(chapterKey, nodeId, stageKey);
            return new ChapterProgressionResult(ChapterProgressionFailureReason.None,
                ChapterSelectionFailureReason.None, chapterKey, nodeId, stageKey,
                false, false, true, bossVictory, false);
        }

        public static ChapterProgressionResult Defeat(int chapterKey,
            StageNodeId nodeId, StageKey stageKey)
        {
            ValidateStage(chapterKey, nodeId, stageKey);
            return new ChapterProgressionResult(ChapterProgressionFailureReason.None,
                ChapterSelectionFailureReason.None, chapterKey, nodeId, stageKey,
                false, false, false, false, true);
        }

        public static ChapterProgressionResult Reject(ChapterProgressionFailureReason reason,
            ChapterSelectionFailureReason navigationReason = ChapterSelectionFailureReason.None)
        {
            if (reason == ChapterProgressionFailureReason.None ||
                !Enum.IsDefined(typeof(ChapterProgressionFailureReason), reason) ||
                (reason == ChapterProgressionFailureReason.NavigationRejected) !=
                (navigationReason != ChapterSelectionFailureReason.None))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new ChapterProgressionResult(reason, navigationReason, 0,
                StageNodeId.Empty, StageKey.Empty, false, false, false, false, false);
        }

        private static void ValidateChapter(int chapterKey)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
        }

        private static void ValidateStage(int chapterKey, StageNodeId nodeId, StageKey stageKey)
        {
            ValidateChapter(chapterKey);
            if (nodeId.IsEmpty || stageKey.IsEmpty)
                throw new ArgumentException("A stage decision needs a node and stage key.");
        }
    }
}
