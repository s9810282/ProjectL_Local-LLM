using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat
{
    /// <summary>
    /// Combat 씬 로딩 준비 단계(로딩 화면이 가린 채, Reveal 전)에서 첫 동작 Core 준비 Job을 거는 준비 작업입니다(첫 동작 Core 준비 계획 §2·FU-b).
    /// 시작할 때 Job 등록을 한 번 요청하고, 완료·실패는 그 Job이 알립니다. 단계 진행은 Core 서비스가 가집니다(FU-P15).
    /// 씬 진입마다 진입 서비스가 새로 만들어 현재 진입 요청에 붙입니다(FU-P7).
    /// </summary>
    internal sealed class CombatFirstUseLoadingPreparation : ILodingPreparation
    {
        private readonly Action _requestJob;
        private bool _started;

        /// <param name="requestJob">첫 동작 준비 Job을 거는 동작입니다. 시작할 때 한 번 부릅니다.</param>
        internal CombatFirstUseLoadingPreparation(Action requestJob)
        {
            _requestJob = requestJob ?? throw new ArgumentNullException(nameof(requestJob));
        }

        public LodingPreparationState State { get; private set; } = LodingPreparationState.Pending;

        /// <summary>시작됐고 아직 끝나지 않았는지입니다. 준비 Job은 이 동안만 Core를 부릅니다.</summary>
        internal bool IsRunning => _started && State == LodingPreparationState.Pending;

        public void Begin(bool isCovered)
        {
            if (_started || State != LodingPreparationState.Pending)
                throw new InvalidOperationException("[CombatFirstUseLoadingPreparation] 준비 작업은 한 번만 시작할 수 있습니다.");
            // 가림이 없는 진입(로딩 화면 없음)에서도 같은 준비를 돈다(FU-P9).
            _started = true;
            _requestJob();
        }

        // 완료·실패는 준비 Job이 알린다. 로딩 계약상 매 프레임 불리지만 여기서 할 일은 없다.
        public void Tick(float deltaTime)
        {
        }

        public void Cancel()
        {
            if (State == LodingPreparationState.Pending)
                State = LodingPreparationState.Cancelled;
        }

        /// <summary>Core 준비가 끝났습니다. 끝나지 않은 준비만 완료로 바꿉니다.</summary>
        internal void Complete()
        {
            if (State == LodingPreparationState.Pending)
                State = LodingPreparationState.Complete;
        }

        /// <summary>Core 준비가 예외로 멈췄습니다. 예외를 처리하는 catch가 원래 예외를 가리지 않게 예외를 내지 않습니다.</summary>
        internal void Fail()
        {
            if (State == LodingPreparationState.Pending)
                State = LodingPreparationState.Failed;
        }
    }
}
