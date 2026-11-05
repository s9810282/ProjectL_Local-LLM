using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery
{
    /// <summary>잠금과 소비된 자기 결과만 읽습니다. 조회는 ack나 게임 상태를 변경하지 않습니다.</summary>
    public interface IRecoveryRequestStatus : IServiceModule
    {
        bool HasPending { get; }
        bool TryGetResult(int number, RecoveryCommandData command, out RecoveryProcessedResult result);
    }
}
