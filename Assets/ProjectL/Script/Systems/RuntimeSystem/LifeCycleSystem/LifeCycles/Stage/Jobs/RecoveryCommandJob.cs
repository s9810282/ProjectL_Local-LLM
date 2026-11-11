using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage.Jobs
{
    /// <summary>회복 선택 하나를 실행 순간에 예약하고 그 번호만 실행합니다. 몸통은 <see cref="StageRequestCommandJob"/>입니다.</summary>
    internal sealed class RecoveryCommandJob : StageRequestCommandJob
    {
        private readonly IRecoveryCommandProcessingService _core;
        private readonly IRecoveryRequestExecution _owner;

        internal RecoveryCommandJob(IRecoveryCommandProcessingService core, IRecoveryRequestExecution owner)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        protected override string NotificationErrorKey => "RecoveryRequestNotificationError";

        protected override bool TryReserveAndBegin(out int number)
        {
            number = 0;
            if (!_owner.TryReserveExecution(out RecoveryViewCommand command) || !_owner.TryBeginExecution(command.Number))
                return false;
            number = command.Number;
            return true;
        }

        protected override bool Process(int number) => _core.ProcessNextRecoveryCommand(number);

        protected override void NotifyFinished(int number, bool processed, Exception error) =>
            _owner.NotifyExecutionFinished(number, processed, error);
    }
}
