using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct RecoveryCommandResult
    {
        public bool Succeeded { get; }
        public RecoveryFailureReason FailureReason { get; }
        public bool AlreadyApplied { get; }
        public RecoveryStageSnapshot CurrentRecovery { get; }

        private RecoveryCommandResult(bool succeeded, RecoveryFailureReason reason, bool alreadyApplied,
            RecoveryStageSnapshot currentRecovery)
        {
            Succeeded = succeeded;
            FailureReason = reason;
            AlreadyApplied = alreadyApplied;
            CurrentRecovery = currentRecovery;
        }

        public static RecoveryCommandResult Success(RecoveryStageSnapshot currentRecovery, bool alreadyApplied = false) =>
            new RecoveryCommandResult(true, RecoveryFailureReason.None, alreadyApplied,
                currentRecovery ?? throw new ArgumentNullException(nameof(currentRecovery)));

        public static RecoveryCommandResult Failure(RecoveryFailureReason reason, RecoveryStageSnapshot currentRecovery = null)
        {
            if (reason == RecoveryFailureReason.None || !Enum.IsDefined(typeof(RecoveryFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new RecoveryCommandResult(false, reason, false, currentRecovery);
        }
    }
}
