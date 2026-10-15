using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Jobs
{
    /// <summary>아티팩트 Plan에서 등록하는 Job의 기본 골격입니다.</summary>
    internal struct ArtifactJob : IJobService
    {
        private readonly IInGameSystemGroupService _service;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal ArtifactJob(IInGameSystemGroupService service, int priority)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (_service == null)
                throw new InvalidOperationException("ArtifactJob requires an in-game service.");

            // TODO: _service.CoreGroup / ViewGroup으로 실제 작업을 실행합니다.
            IsCompleted = true;
        }
    }
}
