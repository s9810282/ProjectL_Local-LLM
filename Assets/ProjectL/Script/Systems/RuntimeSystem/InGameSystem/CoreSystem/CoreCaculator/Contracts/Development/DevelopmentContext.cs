#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Development
{
    /// <summary>개발 명령 호출자가 관측한 Run과 선택의 불변 식별 정보다.</summary>
    public sealed class DevelopmentContext
    {
        public RunId RunId { get; }
        public int ChapterKey { get; }
        public bool HasSelection { get; }
        public StageNodeId NodeId { get; }
        public StageKey StageKey { get; }
        public DevelopmentFailureReason AvailabilityReason { get; }
        public bool IsAvailable => AvailabilityReason == DevelopmentFailureReason.None;

        public DevelopmentContext(RunId runId, int chapterKey, bool hasSelection,
            StageNodeId nodeId, StageKey stageKey, DevelopmentFailureReason availabilityReason)
        {
            RunId = runId;
            ChapterKey = chapterKey;
            HasSelection = hasSelection;
            NodeId = hasSelection ? nodeId : default;
            StageKey = hasSelection ? stageKey : default;
            AvailabilityReason = availabilityReason;
        }
    }
}
#endif
