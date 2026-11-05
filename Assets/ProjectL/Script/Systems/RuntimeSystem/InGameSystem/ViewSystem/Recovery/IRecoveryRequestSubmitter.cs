using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery
{
    /// <summary>표시 계층은 의도만 제출합니다. 입력 제거와 소비 확인은 요청 소유자가 담당합니다.</summary>
    public interface IRecoveryRequestSubmitter : IServiceModule
    {
        int Submit(RecoveryCommandData command);
    }
}
