using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RunSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>현재 런의 포기를 Core에 한 번 요청하고 처리 결과를 Stage 진행에 알립니다.</summary>
    internal struct StageRunAbandonJob : IJobService
    {
        private readonly IRunAbandonService _core;
        private readonly IStageProgress _progress;
        private readonly RunId _runId;

        public int Priority => 0;
        public bool IsCompleted { get; private set; }

        internal StageRunAbandonJob(IRunAbandonService core, IStageProgress progress, RunId runId)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _runId = runId;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted) return;
            _progress.CompleteRunAbandon(_core.TryAbandonRun(_runId));
            IsCompleted = true;
        }
    }
}
