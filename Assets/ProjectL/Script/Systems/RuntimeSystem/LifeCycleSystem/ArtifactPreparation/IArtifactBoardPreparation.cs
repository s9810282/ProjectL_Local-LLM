using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.ArtifactPreparation
{
    /// <summary>최초 구간 진입 전에 아티팩트 보드 준비와 Job 배출을 요청하고 확인합니다.</summary>
    public interface IArtifactBoardPreparation : IServiceModule
    {
        void Request();
        bool IsReady { get; }
    }
}
