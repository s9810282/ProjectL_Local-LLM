using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact
{
    /// <summary>
    /// 예약된 독립 아티팩트 씬의 LifeCycle 골격입니다. 현재 진입·이탈 훅에는 동작이 없습니다.
    /// 운영 아티팩트 팝업은 Lobby의 LobbyPlan(아티팩트 편집 Job·확인 Job, `Lobby/Artifact/`)과 ArtifactPopupSession이 처리합니다.
    /// </summary>
    internal sealed class ArtifactLifeCycle : LifeCycleBase
    {
        public override LifeCycleKey Key => LifeCycleKeys.Artifact;

        protected override int SceneIndex => SceneIndices.Artifact;

        protected override void OnEnter()
        {
            // 독립 씬 동작을 예약한 훅이며 현재 팝업을 여는 경로가 아닙니다.
        }

        protected override void OnExit()
        {
            // 현재 팝업의 정리는 Lobby와 View의 수명에서 수행합니다.
        }
    }
}
