using System;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth
{
    /// <summary>등록·실행 계층에만 제공하는 정확한 요청 번호의 예약과 종료 경계입니다. 취소 입구는 없습니다(Core 인계 §5).</summary>
    public interface ITraitGrowthRequestExecution : IServiceModule
    {
        bool CanReserveExecution { get; }
        bool HasReservedExecution { get; }
        bool TryReserveExecution(out int number);
        bool TryBeginExecution(int number);
        void NotifyExecutionFinished(int number, bool processed, Exception error);
    }
}
