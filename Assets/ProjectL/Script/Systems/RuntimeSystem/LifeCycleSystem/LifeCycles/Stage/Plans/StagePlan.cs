using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Plans
{
    /// <summary>세부 타입에 따라 서비스를 주입한 Job 목록을 구성합니다.</summary>
    internal static class StagePlan
    {
        /// <summary>새 Run의 시작 로드아웃 키. Run 설정 원본(DT)이 생기기 전까지 쓰는 기본값이다.</summary>
        internal const int DefaultLoadoutKey = 1;

        internal static List<IJobService> Register(IInGameSystemGroupService service,
            JobType jobType, IBufferQuery buffers)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var jobs = new List<IJobService>();
            switch (jobType)
            {
                case JobType.None:
                    break;
                case JobType.NewRun:
                    // 새 Run을 시작(이전 Run 정리·로스터·Run 재화)한 뒤 맵을 준비한다. 작은 우선순위가 먼저 실행된다.
                    jobs.Add(new NewRunJob(service, DefaultLoadoutKey, 0));
                    jobs.Add(new StageJob(service, 1, buffers.CurrentProgressReadSink));
                    break;
                case JobType.Start:
                    jobs.Add(new StageJob(service, 0, buffers.CurrentProgressReadSink));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown Stage job type.");
            }

            return jobs;
        }
    }
}
