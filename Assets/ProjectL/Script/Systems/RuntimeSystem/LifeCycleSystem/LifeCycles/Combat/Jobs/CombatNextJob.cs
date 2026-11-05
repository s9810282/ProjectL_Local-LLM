using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Plans;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// 전투 진행의 다음 단계를 실행 순간에 정하고, 그 단계 Job을 이 Job 안에서 차례로 실행합니다(골격 점검 설계 K-1).
    /// 등록 때는 할 일이 있는지만 봤고, 등록과 실행 사이에 다른 모듈이 돌아 사실이 바뀔 수 있으므로 여기서 다시 판단합니다.
    /// </summary>
    /// <remarks>
    /// 판단(<see cref="CombatPlan.BuildStep"/>)과 그 단계의 실행이 한 호출 안에 있어 그 사이에 다른 Job·모듈이 돌지 않습니다.
    /// 그래서 단계 Job은 판단 때의 사실을 다시 증명하지 않습니다. 할 일이 없어졌으면 빈 목록이라 아무것도 하지 않고 끝납니다.
    /// 단계 Job은 한 번 실행으로 끝나야 합니다(<see cref="CombatStepKind"/> 계약). 끝나지 않으면 예외로 멈춥니다(F1).
    /// </remarks>
    internal struct CombatNextJob : IJobService
    {
        private readonly CombatStepContext _context;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatNextJob(CombatStepContext context, int priority)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            // 목록에는 단계 Job(struct)이 상자로 들어 있어, 같은 참조로 실행하고 끝났는지 읽는다.
            List<IJobService> steps = CombatPlan.BuildStep(_context);
            for (int i = 0; i < steps.Count; i++)
            {
                IJobService step = steps[i];
                step.Execute();
                if (!step.IsCompleted)
                    throw new InvalidOperationException(
                        $"[CombatNextJob] 단계 Job이 한 번 실행으로 끝나지 않았습니다({step.GetType().Name}).");
            }
            IsCompleted = true;
        }
    }
}
