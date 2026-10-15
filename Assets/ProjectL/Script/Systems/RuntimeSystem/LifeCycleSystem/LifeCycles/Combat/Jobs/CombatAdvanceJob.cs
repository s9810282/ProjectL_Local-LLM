using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// 자동 진행(적 턴·타임라인·기믹) 한 걸음을 Core 처리 서비스에 요청합니다.
    /// 수락·거절 판단은 Core 결과로 남으며 Job은 판단하지 않습니다.
    /// </summary>
    internal struct CombatAdvanceJob : IJobService
    {
        private readonly IInGameSystemGroupService _service;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatAdvanceJob(IInGameSystemGroupService service, int priority)
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
                throw new InvalidOperationException("CombatAdvanceJob requires an in-game service.");

            // 반환값으로 재시도하지 않는다. 결과의 종류·순번·수락 여부는 Flow가 확인한다.
            _service.CoreGroup.Combat.AdvanceCombat();
            IsCompleted = true;
        }
    }
}
