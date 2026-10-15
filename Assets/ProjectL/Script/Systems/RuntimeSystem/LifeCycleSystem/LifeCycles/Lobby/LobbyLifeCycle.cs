using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby
{
    /// <summary>로비 Flow를 연결하기 위한 임시 LifeCycle 골격입니다.</summary>
    internal sealed class LobbyLifeCycle : LifeCycleBase
    {
        public override LifeCycleKey Key => LifeCycleKeys.Lobby;

        protected override int SceneIndex => SceneIndices.Lobby;

        protected override void OnEnter()
        {
            // TODO: LobbyFlow 시작
        }

        protected override void OnExit()
        {
            // TODO: LobbyFlow 종료
        }
    }
}
