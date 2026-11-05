using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>
    /// 스테이지 내부(배치·전투) 구간의 수명입니다. 전투 진행 Job을 거는 트리거만 맡고,
    /// 무엇을 할지는 <see cref="Plans.CombatPlan"/>이 정합니다(재구성 설계 J1).
    /// </summary>
    /// <remarks>
    /// Buffer·Core·View를 모릅니다. 들어올 때 진입 서비스에 수락된 요청의 활성화를 맡기고,
    /// 활성 동안 Runner가 비면 다음 판단을 한 번 걸고, 나갈 때 정상 이탈이면 요청을 닫고 전투가 남았으면 사후 확인을 겁니다.
    /// 오류 경계는 조립 때 받습니다.
    /// </remarks>
    internal sealed class CombatLifeCycle : LifeCycleBase
    {
        private readonly CombatEntryService _entry;
        private IJobRunner _runner;
        private IJobRegisterSystem _register;

        internal CombatLifeCycle(RuntimeFaultBoundary faultBoundary, CombatEntryService entry)
        {
            FaultBoundary = faultBoundary ?? throw new ArgumentNullException(nameof(faultBoundary));
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        /// <summary>런타임 공통 오류 경계입니다. Bootstrap·View와 같은 인스턴스입니다.</summary>
        internal RuntimeFaultBoundary FaultBoundary { get; }

        public override LifeCycleKey Key => LifeCycleKeys.Combat;

        protected override int SceneIndex => SceneIndices.Combat;

        protected override void OnBindServices(ISystemServiceStoreReader resolver)
        {
            var jobs = resolver.GetService(SystemGroupsService.Global).JobServiceCommand;
            _runner = jobs.JobRunner;
            _register = jobs.JobRegister;
        }

        /// <summary>Combat 씬 Enter 노드에서 View보다 먼저 돈다. 활성화는 Job으로 미루지 않는다(설계 J6).</summary>
        protected override void OnEnter() => _entry.ActivateAccepted();

        protected override void OnTick(float deltaTime)
        {
            // 같은 Tick 앞쪽에서 오류가 보고됐으면 새 등록을 하지 않는다(골격 §4.1 0행).
            if (FaultBoundary.IsFaulted)
                return;
            if (_runner.IsIdle)
                _register.Register(JobRootType.Combat, JobType.Next);
        }

        /// <summary>
        /// 정상 이탈이면 이 자리에서 요청을 닫는다(개선 설계 §4.6). 전투가 남은 이탈은 정리 순서를 거치지 않은 전환이므로
        /// 사후 확인을 걸어 F1로 잡는다(설계 J9, D1-a). 이미 오류로 멈춘 런타임에는 아무것도 하지 않는다.
        /// 런타임 종료(Shutdown) 중에 걸린 확인은 Runner 정리와 함께 버려진다.
        /// </summary>
        protected override void OnExit()
        {
            if (FaultBoundary.IsFaulted)
                return;
            if (!_entry.TryCloseExited())
                _register.Register(JobRootType.Combat, JobType.ExitCheck);
        }
    }
}
