using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct ShopStageEntryRequestResult
    {
        public bool Succeeded { get; }
        public RunFailureReason FailureReason { get; }
        public ShopStageEntryRequest Request { get; }

        private ShopStageEntryRequestResult(bool succeeded, RunFailureReason reason, ShopStageEntryRequest request)
        {
            Succeeded = succeeded;
            FailureReason = reason;
            Request = request;
        }

        public static ShopStageEntryRequestResult Success(ShopStageEntryRequest request) =>
            new ShopStageEntryRequestResult(true, RunFailureReason.None,
                request ?? throw new ArgumentNullException(nameof(request)));

        public static ShopStageEntryRequestResult Failure(RunFailureReason reason)
        {
            if (reason == RunFailureReason.None || !Enum.IsDefined(typeof(RunFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new ShopStageEntryRequestResult(false, reason, null);
        }
    }
}
