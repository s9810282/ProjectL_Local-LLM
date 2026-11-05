using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery
{
    /// <summary>등록·실행 계층에만 제공하는 정확한 요청의 예약과 종료 경계입니다.</summary>
    public interface IRecoveryRequestExecution : IServiceModule
    {
        bool CanReserveExecution { get; }
        bool HasReservedExecution { get; }
        bool TryReserveExecution(out RecoveryViewCommand command);
        bool TryBeginExecution(int number);
        void NotifyExecutionFinished(int number, bool processed, Exception error);
        void CancelPending();
    }
}
