using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>동일 노드 번호를 쓰는 새 Run과 이전 표시 이동을 구분하는 시작 문맥입니다.</summary>
    internal readonly struct StageMapMovementContext
    {
        internal StageMapMovementContext(RunId runId, int mapKey, long preparationVersion)
        {
            RunId = runId;
            MapKey = mapKey;
            PreparationVersion = preparationVersion;
        }

        internal RunId RunId { get; }
        internal int MapKey { get; }
        internal long PreparationVersion { get; }
    }
}
