using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery
{
    /// <summary>
    /// 현재 회복 진입의 표시 상태를 읽는 계약입니다. 이 네임스페이스에 있지만 Core 조회 계약입니다.
    /// 구현은 Core <c>RecoveryStateQueryService</c>이고 <c>InGameCoreSystemGroup.RecoveryState</c>로 읽습니다.
    /// </summary>
    public interface IRecoveryStateReadSink : IServiceModule
    {
        /// <summary>
        /// 권위 상태의 최신 값을 읽습니다. 활성 회복 진입이 없거나 교체·해제되었거나 Run이 끝났으면 false와 null을 반환합니다.
        /// 완료된 진입이 아직 선택되어 있으면 완료 상태를 반환할 수 있으며, 조회 오류를 false로 숨기지 않습니다.
        /// </summary>
        bool TryGetCurrent(out RecoveryStateSnapshot state);
    }
}
