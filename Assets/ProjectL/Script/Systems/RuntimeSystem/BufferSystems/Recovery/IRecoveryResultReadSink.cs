using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery
{
    /// <summary>
    /// 처리된 회복 요청의 이력을 읽는 계약입니다. 구현은 <see cref="RecoveryResultBuffer"/>이고 Buffer 그룹이 냅니다.
    /// </summary>
    public interface IRecoveryResultReadSink : IServiceModule
    {
        /// <summary>
        /// 마지막으로 결과가 게시된 요청 번호이며 처리 전에는 0입니다. 발급된 요청의 개수가 아닙니다.
        /// 같은 런타임 Buffer 수명 동안 입력 Clear, 노드 변경, 새 Run에도 유지됩니다.
        /// </summary>
        int HandledCommandNumber { get; }

        /// <summary>
        /// 게시된 결과가 없으면 false와 null을 반환합니다. 결과는 현재 표시 상태가 아닌 마지막 처리 이력입니다.
        /// 같은 런타임 Buffer 수명 동안 입력 Clear, 노드 변경, 새 Run에도 보존하며 조회 오류를 false로 숨기지 않습니다.
        /// </summary>
        bool TryGetLatest(out RecoveryProcessedResult result);
    }
}
