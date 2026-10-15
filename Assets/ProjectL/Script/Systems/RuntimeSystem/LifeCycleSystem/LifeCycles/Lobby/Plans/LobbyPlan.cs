using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Jobs;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Plans
{
    /// <summary>세부 타입에 따라 서비스를 주입한 Job 목록을 구성합니다.</summary>
    internal static class LobbyPlan
    {
        internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var jobs = new List<IJobService>();
            switch (jobType)
            {
                case JobType.None:
                    break;
                case JobType.Start:
                    // 임시 Job 골격입니다. 실제 작업 조합은 이 Plan에서 정의합니다.
                    jobs.Add(new LobbyJob(service, 0));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown Lobby job type.");
            }

            return jobs;
        }
    }
}
