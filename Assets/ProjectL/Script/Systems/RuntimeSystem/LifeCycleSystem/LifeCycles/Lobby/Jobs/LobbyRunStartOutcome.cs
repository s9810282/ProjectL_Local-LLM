using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.RunStart;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Jobs
{
    /// <summary>
    /// 로비 출발의 새 Run 결과를 출발 요청에 게시합니다. 정상 거절도 결과로 남기고, 전환은 다음 Job이 정합니다(로비 재구성 설계 §4.2).
    /// </summary>
    internal sealed class LobbyRunStartOutcome : IRunStartOutcome
    {
        private readonly LobbyDepartureRequest _request;

        internal LobbyRunStartOutcome(LobbyDepartureRequest request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        // 취소된 요청은 Core를 부르지 않습니다(설계 P5).
        public bool TryStart() => _request.TryStart();

        public void Publish(RosterAcquireResult result) => _request.PublishResult(result);

        public void Fail() => _request.Fail();
    }
}
