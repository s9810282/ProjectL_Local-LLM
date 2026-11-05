using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact.Jobs;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact.Plans
{
    /// <summary>아티팩트 편집·프리뷰 Job을 만듭니다. 언제 만들지는 로비 Plan의 <c>Decide</c>가 정합니다.</summary>
    /// <remarks>
    /// 아티팩트는 로비에서만 편집하므로 로비 폴더 아래에 둡니다. 따로 거는 등록 열쇠는 없습니다(검사 후속 정리 R-f J7).
    /// 부트 때의 보드 초기화는 <see cref="ArtifactPreparation.ArtifactBoardInitializeJob.Create"/>가 맡습니다.
    /// </remarks>
    internal static class ArtifactPlan
    {
        /// <summary>편집 요청 N을 Core에 넘기는 Job을 만듭니다. 기대 번호와 좁은 Core 서비스만 받습니다.</summary>
        internal static IJobService CreateCommand(int number, IArtifactCommandProcessingService core) =>
            new ArtifactCoreCallJob(LobbyArtifactRequestKind.Edit, number, core);

        /// <summary>최신 프리뷰 요청 N을 Core에 넘기는 Job을 만듭니다. 기대 번호와 좁은 Core 서비스만 받습니다.</summary>
        internal static IJobService CreatePreview(int number, IArtifactCommandProcessingService core) =>
            new ArtifactCoreCallJob(LobbyArtifactRequestKind.Preview, number, core);
    }
}
