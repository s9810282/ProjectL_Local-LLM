using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat.Jobs
{
    /// <summary>
    /// Combat 씬 로딩 준비 동안 Tick마다 첫 동작 Core 준비 단계 하나를 진행합니다(첫 동작 Core 준비 계획 FU-b·FU-P8).
    /// 끝나지 않은 Job은 Runner가 다음 Tick에도 현재 Job으로 둡니다. 가변 값은 <see cref="IsCompleted"/> 하나이고,
    /// 단계 진행은 Core 서비스, 취소·완료는 준비 객체가 가집니다(FU-P15).
    /// </summary>
    internal struct CombatFirstUsePrepareJob : IJobService
    {
        private readonly ICombatFirstUsePreparation _core;
        private readonly CombatFirstUseLoadingPreparation _preparation;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal CombatFirstUsePrepareJob(ICombatFirstUsePreparation core, CombatFirstUseLoadingPreparation preparation, int priority)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _preparation = preparation ?? throw new ArgumentNullException(nameof(preparation));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            // 로딩이 취소됐으면 Core를 부르지 않고 끝낸다.
            if (_preparation.IsRunning)
            {
                try
                {
                    if (_core.PrepareNext())
                        return;
                    _preparation.Complete();
                }
                catch
                {
                    // 준비를 실패로 표시한 뒤 원래 예외를 오류 경계로 보낸다(FU-P4).
                    _preparation.Fail();
                    throw;
                }
            }
            IsCompleted = true;
        }
    }
}
