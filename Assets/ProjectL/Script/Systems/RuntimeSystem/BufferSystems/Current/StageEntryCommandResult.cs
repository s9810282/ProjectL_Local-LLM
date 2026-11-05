using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Current
{
    public enum StageEntryCommandKind { Node, Battle }

    /// <summary>Core가 실제 실행한 맵 진입 시도다. 전투 수락은 준비 수락이며 활성화 완료가 아니다.</summary>
    public readonly struct StageEntryCommandResult
    {
        public int RequestNumber { get; }
        public int ChapterKey { get; }
        public StageNodeId NodeId { get; }
        public StageEntryCommandKind Kind { get; }
        public ChapterProgressionResult Result { get; }
        internal int Seed { get; }

        internal StageEntryCommandResult(int requestNumber, int chapterKey, StageNodeId nodeId,
            StageEntryCommandKind kind, int seed, ChapterProgressionResult result)
        {
            RequestNumber = requestNumber;
            ChapterKey = chapterKey;
            NodeId = nodeId;
            Kind = kind;
            Seed = seed;
            Result = result;
        }
    }
}
