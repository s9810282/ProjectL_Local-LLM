using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public sealed class CharacterPurchaseDecision
    {
        public bool Succeeded => FailureReason == ShopFailureReason.None;
        public ShopFailureReason FailureReason { get; }
        public int FinalBalance { get; }
        public RosterAcquireRequest Request { get; }

        private CharacterPurchaseDecision(ShopFailureReason reason, int finalBalance,
            RosterAcquireRequest request)
        {
            FailureReason = reason;
            FinalBalance = finalBalance;
            Request = request;
        }

        public static CharacterPurchaseDecision Success(int finalBalance, RosterAcquireRequest request)
        {
            if (finalBalance < 0)
                throw new ArgumentOutOfRangeException(nameof(finalBalance));
            return new CharacterPurchaseDecision(ShopFailureReason.None, finalBalance,
                request ?? throw new ArgumentNullException(nameof(request)));
        }

        public static CharacterPurchaseDecision Failure(ShopFailureReason reason)
        {
            if (reason == ShopFailureReason.None || Enum.IsDefined(typeof(ShopFailureReason), reason) == false)
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new CharacterPurchaseDecision(reason, 0, null);
        }
    }
}
