using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Jobs
{
    /// <summary>
    /// 새 Run Job 다음에 실행해, 이번 출발의 새 Run이 성공했을 때만 Stage로 한 번 전환합니다(로비 재구성 설계 §4.2).
    /// 거절·취소된 출발은 전환하지 않고 완료합니다. 결과는 요청 객체에서 읽습니다.
    /// </summary>
    internal sealed class LobbyTransitionJob : IJobService
    {
        private readonly LobbyDepartureRequest _request;
        private readonly ILifeCycleManager _manager;
        // 새 Run Job과 같은 우선순위라 등록 순서대로 그 바로 뒤에 실행됩니다.
        public int Priority => LobbyJobPriority.Departure;
        public bool IsCompleted { get; private set; }

        internal LobbyTransitionJob(LobbyDepartureRequest request, ILifeCycleManager manager)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public void Execute()
        {
            if (IsCompleted)
                return;
            try
            {
                // 거절·취소로 끝난 출발은 전환하지 않습니다.
                if (!_request.CanResubmit)
                {
                    if (!_request.IsPrepared)
                        throw new InvalidOperationException(
                            $"[LobbyTransitionJob] 전환할 수 없는 출발 단계입니다({_request.Status}).");
                    // 같은 Runner Tick의 연쇄라 거의 걸리지 않지만, 로비를 이미 떠났다면 이 자리에서 멈춥니다(설계 P8).
                    if (!_manager.HasActiveLifeCycle || !_manager.CurrentKey.Equals(LifeCycleKeys.Lobby))
                        throw new InvalidOperationException("[LobbyTransitionJob] 로비가 활성 구간이 아니라 Stage로 전환할 수 없습니다.");
                    _request.BeginTransition();
                    // 로딩 표현이 없으면 Change 안의 동기 이탈 검사가 이 Transitioning 요청을 봅니다. 로딩 표현이 있으면 이탈 검사는
                    // 덮기가 끝난 뒤 Tick에서 돌아, 그때 요청은 이미 Completed입니다(로비 리팩토링 2차 설계 N-79).
                    _manager.Change(LifeCycleKeys.Stage);
                    _request.CompleteTransition();
                }
                IsCompleted = true;
            }
            catch
            {
                _request.Fail();
                throw;
            }
        }
    }
}
