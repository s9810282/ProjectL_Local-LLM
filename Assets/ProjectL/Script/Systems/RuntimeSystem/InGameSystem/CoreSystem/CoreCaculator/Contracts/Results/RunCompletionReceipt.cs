using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>Run 종료를 확정한 결과와 원본 스테이지 식별자만 보관한다.</summary>
    public sealed class RunCompletionReceipt
    {
        public RunOutcome Outcome { get; }
        public int ChapterKey { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }

        public RunCompletionReceipt(RunOutcome outcome, int chapterKey, StageNodeId nodeId, StageKey stageKey)
        {
            if (outcome != RunOutcome.Victory && outcome != RunOutcome.Defeat)
                throw new ArgumentOutOfRangeException(nameof(outcome));
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (nodeId.IsEmpty || stageKey.IsEmpty)
                throw new ArgumentException("Run 종료의 노드와 스테이지 키가 필요합니다.");
            Outcome = outcome;
            ChapterKey = chapterKey;
            NodeId = nodeId;
            StageKey = stageKey;
        }
    }
}
