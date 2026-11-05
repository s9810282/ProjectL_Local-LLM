using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// 진입 Job 다음에 실행해, 이번 요청이 수락됐을 때만 Combat으로 한 번 전환합니다(재구성 설계 J5, K20).
    /// 맵 요청이 아직 착지 전이면 Runner를 붙들지 않고 착지 대기를 남긴 채 끝나며, 착지 게시가 이 Job을 다시 겁니다(개선 설계 §6.1).
    /// 거절·취소된 요청은 전환하지 않고 완료합니다. 결과는 요청 객체에서 읽습니다.
    /// </summary>
    internal struct CombatEntryTransitionJob : IJobService
    {
        private readonly CombatEntryRequest _request;
        private readonly ILifeCycleManager _manager;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatEntryTransitionJob(CombatEntryRequest request, ILifeCycleManager manager, int priority)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            if (!_request.IsJobCompleted)
                throw new InvalidOperationException("[CombatEntryTransitionJob] 진입 Job이 끝나기 전에 전환할 수 없습니다.");

            switch (_request.Status)
            {
                case CombatEntryStatus.Rejected:
                case CombatEntryStatus.Cancelled:
                    break;
                case CombatEntryStatus.Prepared:
                    try
                    {
                        MapCombatPresentationState presentation = _request.ReadMapPresentationState();
                        if (presentation == MapCombatPresentationState.Pending)
                        {
                            _request.MarkAwaitingLanding();
                            break;
                        }
                        if (presentation != MapCombatPresentationState.Ready)
                            throw new InvalidOperationException("[CombatEntryTransitionJob] 수락된 맵 진입의 표시 소유권을 잃었습니다.");
                        _request.BeginActivation();
                        _manager.Change(LifeCycleKeys.Combat);
                    }
                    catch
                    {
                        _request.Fail();
                        throw;
                    }
                    break;
                default:
                    throw new InvalidOperationException(
                        $"[CombatEntryTransitionJob] 전환할 수 없는 진입 단계입니다({_request.Status}).");
            }
            IsCompleted = true;
        }
    }
}
