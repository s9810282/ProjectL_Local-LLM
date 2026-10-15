using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// 처리하지 않은 전투 명령 하나를 Core 처리 서비스에 넘깁니다.
    /// 어떤 명령을 처리할지는 서비스가 처리 번호 기준으로 고르며, Job은 명령 내용을 들고 있지 않습니다.
    /// </summary>
    internal struct CombatCommandJob : IJobService
    {
        private readonly IInGameSystemGroupService _service;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatCommandJob(IInGameSystemGroupService service, int priority)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            if (_service == null)
                throw new InvalidOperationException("CombatCommandJob requires an in-game service.");

            // false도 정상 반환이다. 실제 처리 기록의 확인은 Flow가 맡는다.
            _service.CoreGroup.Combat.ProcessNextCommand();
            IsCompleted = true;
        }
    }
}
