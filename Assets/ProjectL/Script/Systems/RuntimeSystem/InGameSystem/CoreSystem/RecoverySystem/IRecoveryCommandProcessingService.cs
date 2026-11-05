using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem
{
    /// <summary>
    /// 지정 번호의 회복 요청을 처리하는 계약입니다. 구현과 서비스 등록은 RC2/RC3에서 제공합니다.
    /// </summary>
    public interface IRecoveryCommandProcessingService : IServiceModule
    {
        /// <summary>
        /// 양수인 expectedNumber와 정확히 일치하는 미처리 요청의 결과를 게시하면 true를 반환합니다.
        /// 거절 결과도 true이며 회복 거래 성공을 뜻하지 않습니다. 0 이하의 번호는 인자 오류입니다.
        /// 입력 없음·취소·이미 처리됨·이전 결과 미확인 상태에서는 새 거래나 결과 없이 false를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 커밋 전 입력이 사라지면 취소하지만 커밋 후에는 Clear/Hide와 무관하게 결과 게시를 마칩니다.
        /// 입력 제거는 View의 책임입니다. 런타임 스레드에서 동기 실행하며 병렬·재진입 처리는 허용하지 않습니다.
        /// </remarks>
        bool ProcessNextRecoveryCommand(int expectedNumber);
    }
}
