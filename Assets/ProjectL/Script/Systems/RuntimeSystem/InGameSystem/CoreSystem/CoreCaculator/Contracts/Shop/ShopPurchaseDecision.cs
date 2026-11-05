using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop
{
    public sealed class ShopPurchaseDecision
    {
        public bool Succeeded => FailureReason == ShopFailureReason.None;
        public ShopFailureReason FailureReason { get; }
        public int FinalBalance { get; }
        public RosterAcquireRequest UnitRequest { get; }
        public ItemInventoryChange ItemChange { get; }

        private ShopPurchaseDecision(ShopFailureReason reason, int balance,
            RosterAcquireRequest request, ItemInventoryChange change)
        {
            FailureReason = reason;
            FinalBalance = balance;
            UnitRequest = request;
            ItemChange = change;
        }

        public static ShopPurchaseDecision ForUnit(int balance, RosterAcquireRequest request)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            if (request == null) throw new ArgumentNullException(nameof(request));

            return new ShopPurchaseDecision(ShopFailureReason.None, balance, request, null);
        }

        public static ShopPurchaseDecision ForItem(int balance, ItemInventoryChange change)
        {
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            return new ShopPurchaseDecision(ShopFailureReason.None, balance, null,
                change ?? throw new ArgumentNullException(nameof(change)));
        }

        public static ShopPurchaseDecision Failure(ShopFailureReason reason)
        {
            if (reason == ShopFailureReason.None || !Enum.IsDefined(typeof(ShopFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new ShopPurchaseDecision(reason, 0, null, null);
        }
    }
}
