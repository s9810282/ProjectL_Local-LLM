using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    /// <summary>한 번의 전투 게시에서 비교할 현재 선택과 설치할 대상 선택이다.</summary>
    public sealed class BattleEntrySelection
    {
        public int ExpectedChapterKey { get; }
        public bool ExpectedHasSelection { get; }
        public StageSelectionData ExpectedStage { get; }
        public StageNodeId TargetNodeId { get; }
        public StageKey TargetStageKey { get; }
        public string TargetStageName { get; }

        public BattleEntrySelection(int expectedChapterKey, bool expectedHasSelection,
            StageSelectionData expectedStage, StageNodeId targetNodeId,
            StageKey targetStageKey, string targetStageName)
        {
            StageSelectionData.RequireEntry(expectedChapterKey, expectedHasSelection, expectedStage,
                targetNodeId, targetStageKey, targetStageName);

            ExpectedChapterKey = expectedChapterKey;
            ExpectedHasSelection = expectedHasSelection;
            ExpectedStage = expectedStage;
            TargetNodeId = targetNodeId;
            TargetStageKey = targetStageKey;
            TargetStageName = targetStageName;
        }
    }
}
