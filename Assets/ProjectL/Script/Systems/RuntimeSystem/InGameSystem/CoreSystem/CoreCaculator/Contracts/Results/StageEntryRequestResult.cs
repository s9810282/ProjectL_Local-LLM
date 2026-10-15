using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct StageEntryRequestResult
    {
        public bool Succeeded { get; }
        public RunFailureReason FailureReason { get; }
        public StageEntryRequest Request { get; }

        private StageEntryRequestResult(bool succeeded, RunFailureReason reason, StageEntryRequest request)
        {
            Succeeded = succeeded;
            FailureReason = reason;
            Request = request;
        }

        public static StageEntryRequestResult Success(StageEntryRequest request) =>
            new StageEntryRequestResult(true, RunFailureReason.None,
                request ?? throw new ArgumentNullException(nameof(request)));

        public static StageEntryRequestResult Failure(RunFailureReason reason)
        {
            if (reason == RunFailureReason.None || !Enum.IsDefined(typeof(RunFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new StageEntryRequestResult(false, reason, null);
        }
    }
}
