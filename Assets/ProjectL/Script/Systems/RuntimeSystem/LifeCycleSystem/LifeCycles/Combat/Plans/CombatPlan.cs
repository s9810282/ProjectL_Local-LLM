using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans
{
    /// <summary>
    /// 세부 타입에 따라 서비스를 주입한 Job 목록을 구성합니다.
    /// 전투 처리 시나리오는 Job이 하나이며, 준비 선행과 처리 순서는 Priority가 아니라 CombatFlow의 등록 조건이 정합니다.
    /// 새 Run 준비는 Core RunStart가 이전 Run 정리·시작 로스터·시작 Run 재화를 한 번에 처리하는 NewRunJob 하나입니다(설계 K21, StagePlan과 같은 Job).
    /// </summary>
    internal static class CombatPlan
    {
        /// <summary>새 Run 시작 로스터의 로드아웃입니다. Title의 새 게임 선택이 생기기 전까지 쓰는 임시 값입니다(설계 K21).</summary>
        internal const int NewRunLoadoutKey = 1;

        /// <summary>이번 진입의 대상과 결과 수신 객체를 한 Job에 전달합니다.</summary>
        /// <param name="lifeCycles">진입 전환에 쓸 LifeCycle 그룹입니다(재구성 설계 J5·J14, R3에서 연결).</param>
        internal static List<IJobService> RegisterEntry(IInGameSystemGroupService service, CombatEntryRequest request,
            ILifeCycleSystemGroupService lifeCycles)
        {
            return new List<IJobService> { new CombatPrepareJob(service, 0, request) };
        }

        /// <param name="lifeCycles">전환·진입 요청·진행 사실 읽기에 쓸 LifeCycle 그룹입니다(재구성 설계 J14). 기존 처리 시나리오는 쓰지 않습니다.</param>
        internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType,
            ILifeCycleSystemGroupService lifeCycles)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var jobs = new List<IJobService>();
            switch (jobType)
            {
                case JobType.None:
                    break;
                case JobType.Start:
                    jobs.Add(new CombatPrepareJob(service, 0));
                    break;
                case JobType.NewRun:
                    // 정리·로스터·Run 재화는 Core가 한 번에 처리한다. 거절이면 Job이 예외로 흐름을 멈춘다.
                    jobs.Add(new NewRunJob(service, NewRunLoadoutKey, 0));
                    break;
                case JobType.ProcessCommand:
                    jobs.Add(new CombatCommandJob(service, 0));
                    break;
                case JobType.ProcessPreview:
                    jobs.Add(new CombatPreviewJob(service, 0));
                    break;
                case JobType.AdvanceCombat:
                    jobs.Add(new CombatAdvanceJob(service, 0));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown Combat job type.");
            }

            return jobs;
        }
    }
}
