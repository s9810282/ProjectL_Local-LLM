using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct RecoveryStageEntryRequestResult
    {
        public bool Succeeded { get; }
        public RunFailureReason FailureReason { get; }
        public RecoveryStageEntryRequest Request { get; }

        private RecoveryStageEntryRequestResult(bool succeeded, RunFailureReason reason, RecoveryStageEntryRequest request)
        {
            Succeeded = succeeded;
            FailureReason = reason;
            Request = request;
        }

        public static RecoveryStageEntryRequestResult Success(RecoveryStageEntryRequest request) =>
            new RecoveryStageEntryRequestResult(true, RunFailureReason.None,
                request ?? throw new ArgumentNullException(nameof(request)));

        public static RecoveryStageEntryRequestResult Failure(RunFailureReason reason)
        {
            if (reason == RunFailureReason.None || !Enum.IsDefined(typeof(RunFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new RecoveryStageEntryRequestResult(false, reason, null);
        }
    }
}
