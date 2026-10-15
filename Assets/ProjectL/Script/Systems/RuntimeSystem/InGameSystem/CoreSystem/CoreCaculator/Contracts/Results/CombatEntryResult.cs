using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct CombatEntryResult
    {
        public bool Succeeded { get; }
        public CombatEntryFailureReason FailureReason { get; }
        public StageSnapshot CurrentStage { get; }

        private CombatEntryResult(bool succeeded, CombatEntryFailureReason failureReason, StageSnapshot currentStage)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            CurrentStage = currentStage;
        }

        public static CombatEntryResult Success(StageSnapshot currentStage)
        {
            if (currentStage == null) throw new ArgumentNullException(nameof(currentStage));
            return new CombatEntryResult(true, CombatEntryFailureReason.None, currentStage);
        }

        public static CombatEntryResult Failure(CombatEntryFailureReason failureReason, StageSnapshot currentStage)
        {
            if (failureReason == CombatEntryFailureReason.None ||
                Enum.IsDefined(typeof(CombatEntryFailureReason), failureReason) == false)
                throw new ArgumentOutOfRangeException(nameof(failureReason));
            if (currentStage == null) throw new ArgumentNullException(nameof(currentStage));

            return new CombatEntryResult(false, failureReason, currentStage);
        }
    }
}