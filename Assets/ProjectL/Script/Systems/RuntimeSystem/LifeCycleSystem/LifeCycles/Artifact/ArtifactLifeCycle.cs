using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact
{
    /// <summary>아티팩트 선택 Flow를 연결하기 위한 임시 LifeCycle 골격입니다.</summary>
    internal sealed class ArtifactLifeCycle : LifeCycleBase
    {
        public override LifeCycleKey Key => LifeCycleKeys.Artifact;

        protected override int SceneIndex => SceneIndices.Artifact;

        protected override void OnEnter()
        {
            // TODO: ArtifactFlow 시작
        }

        protected override void OnExit()
        {
            // TODO: ArtifactFlow 종료
        }
    }
}
