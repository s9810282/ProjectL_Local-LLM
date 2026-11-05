using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>
    /// 회복 선택 하나를 실행 순간에 예약하고 그 번호만 실행하며, 예외 때도 요청 소유자에게 종료를 통지합니다.
    /// 예약을 Plan이 아니라 여기서 해, 예약과 실행이 떨어지지 않습니다(검사 후속 정리 계획 TP-R9).
    /// </summary>
    internal sealed class RecoveryCommandJob : IJobService
    {
        private readonly IRecoveryCommandProcessingService _core;
        private readonly IRecoveryRequestExecution _owner;
        private bool _started;

        internal RecoveryCommandJob(IRecoveryCommandProcessingService core, IRecoveryRequestExecution owner)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }
        public int Priority => 3;
        public bool IsCompleted { get; private set; }
        public void Execute()
        {
            if (_started) return;
            _started = true;
            if (!_owner.TryReserveExecution(out RecoveryViewCommand command) || !_owner.TryBeginExecution(command.Number))
            {
                IsCompleted = true;
                return;
            }
            int number = command.Number;
            bool processed = false;
            Exception error = null;
            try
            {
                processed = _core.ProcessNextRecoveryCommand(number);
            }
            catch (Exception original)
            {
                error = original;
                throw;
            }
            finally
            {
                IsCompleted = true;
                try
                {
                    _owner.NotifyExecutionFinished(number, processed, error);
                }
                catch (Exception notificationError) when (error != null)
                {
                    error.Data["RecoveryRequestNotificationError"] = notificationError;
                }
            }
        }
    }
}
