using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// 현재 미리보기 요청 하나를 Core 처리 서비스에 넘깁니다.
    /// 요청은 서비스가 실행 시점에 읽으며, 미리보기는 게임 상태를 바꾸지 않습니다.
    /// </summary>
    internal struct CombatPreviewJob : IJobService
    {
        private readonly IInGameSystemGroupService _service;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatPreviewJob(IInGameSystemGroupService service, int priority)
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
                throw new InvalidOperationException("CombatPreviewJob requires an in-game service.");

            // 실행 전 요청이 교체·해제돼 false여도 정상 완료한다.
            _service.CoreGroup.Combat.ProcessLatestPreview();
            IsCompleted = true;
        }
    }
}
