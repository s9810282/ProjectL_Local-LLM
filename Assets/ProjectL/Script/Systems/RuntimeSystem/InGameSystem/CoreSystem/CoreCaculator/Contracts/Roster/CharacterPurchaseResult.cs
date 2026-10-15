using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public sealed class CharacterPurchaseResult
    {
        public bool Succeeded => FailureReason == ShopFailureReason.None;
        public ShopFailureReason FailureReason { get; }
        public RosterAcquireResult Acquisition { get; }

        private CharacterPurchaseResult(ShopFailureReason reason, RosterAcquireResult acquisition)
        {
            FailureReason = reason;
            Acquisition = acquisition;
        }

        public static CharacterPurchaseResult Success(RosterAcquireResult acquisition)
        {
            if (acquisition == null || acquisition.Succeeded == false)
                throw new ArgumentException("성공한 획득 결과가 필요합니다.", nameof(acquisition));
            return new CharacterPurchaseResult(ShopFailureReason.None, acquisition);
        }

        public static CharacterPurchaseResult Failure(ShopFailureReason reason)
        {
            if (reason == ShopFailureReason.None || Enum.IsDefined(typeof(ShopFailureReason), reason) == false)
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new CharacterPurchaseResult(reason, null);
        }
    }
}
