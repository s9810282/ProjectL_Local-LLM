using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct ShopCommandResult
    {
        public bool Succeeded { get; }
        public ShopFailureReason FailureReason { get; }
        public bool AlreadyApplied { get; }
        public ShopStageSnapshot CurrentShop { get; }

        private ShopCommandResult(bool succeeded, ShopFailureReason reason, bool alreadyApplied,
            ShopStageSnapshot currentShop)
        {
            Succeeded = succeeded;
            FailureReason = reason;
            AlreadyApplied = alreadyApplied;
            CurrentShop = currentShop;
        }

        public static ShopCommandResult Success(ShopStageSnapshot currentShop, bool alreadyApplied = false) =>
            new ShopCommandResult(true, ShopFailureReason.None, alreadyApplied,
                currentShop ?? throw new ArgumentNullException(nameof(currentShop)));

        public static ShopCommandResult Failure(ShopFailureReason reason, ShopStageSnapshot currentShop = null)
        {
            if (reason == ShopFailureReason.None || !Enum.IsDefined(typeof(ShopFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new ShopCommandResult(false, reason, false, currentShop);
        }
    }
}
