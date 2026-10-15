using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    /// <summary>체크포인트 복귀 가능성과 환급 예상값이다. 실제 복귀는 최신 상태로 다시 계산한다.</summary>
    public sealed class CheckpointReturnPreview
    {
        public bool CanReturn { get; }
        public MovementFailureReason FailureReason { get; }
        public CheckpointReturnResult Return { get; }

        public CheckpointReturnPreview(bool canReturn, MovementFailureReason failureReason,
            CheckpointReturnResult @return = null)
        {
            if (!Enum.IsDefined(typeof(MovementFailureReason), failureReason))
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            if (canReturn ? failureReason != MovementFailureReason.None || @return == null :
                failureReason == MovementFailureReason.None || @return != null)
                throw new ArgumentException("복귀 가능 여부와 사유·환급 결과가 일치해야 합니다.");
            CanReturn = canReturn;
            FailureReason = failureReason;
            Return = @return;
        }
    }
}
