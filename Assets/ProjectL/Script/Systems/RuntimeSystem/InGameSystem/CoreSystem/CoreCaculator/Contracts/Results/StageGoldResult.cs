using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class StageGoldResult
    {
        public bool Succeeded { get; }
        public StageGoldFailureReason FailureReason { get; }
        public StageSnapshot CurrentStage { get; }

        private StageGoldResult(bool succeeded, StageGoldFailureReason failureReason, StageSnapshot currentStage)
        {
            if (!Enum.IsDefined(typeof(StageGoldFailureReason), failureReason))
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            if (succeeded != (failureReason == StageGoldFailureReason.None))
                throw new ArgumentException("성공 여부와 실패 이유가 일치하지 않습니다.", nameof(failureReason));
            CurrentStage = currentStage ?? throw new ArgumentNullException(nameof(currentStage));
            Succeeded = succeeded;
            FailureReason = failureReason;
        }

        public static StageGoldResult Success(StageSnapshot currentStage) =>
            new StageGoldResult(true, StageGoldFailureReason.None, currentStage);

        public static StageGoldResult Failure(StageGoldFailureReason reason, StageSnapshot currentStage) =>
            new StageGoldResult(false, reason, currentStage);
    }
}
