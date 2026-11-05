using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop
{
    public static class ShopPurchaseRules
    {
        public static ShopPurchaseDecision Plan(ShopPurchaseInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Main == null || input.Main.CurrentHealth == 0)
                return ShopPurchaseDecision.Failure(ShopFailureReason.WrongPhase);
            if (input.HasPurchase)
                return ShopPurchaseDecision.Failure(ShopFailureReason.PurchaseAlreadyUsed);
            var offer = input.Offer;
            if (offer == null)
                return ShopPurchaseDecision.Failure(ShopFailureReason.UnknownOffer);
            if (offer.Kind == ShopProductKind.Enhancement)
                return ShopPurchaseDecision.Failure(ShopFailureReason.SubTraitPurchaseUnavailable);

            if (offer.Kind == ShopProductKind.Unit)
            {
                if (input.Unit == null || input.Unit.Key != offer.CharacterKey)
                    return ShopPurchaseDecision.Failure(ShopFailureReason.InvalidCharacter);
                var existing = new List<CharacterDefinitionFacts>();
                foreach (var record in input.Owned)
                {
                    if (!input.Definitions.TryGetValue(record.CharacterKey, out var definition))
                        return ShopPurchaseDecision.Failure(ShopFailureReason.InvalidCharacter);
                    existing.Add(definition);
                }
                var acquisition = CharacterRosterRules.PlanAcquire(input.Unit, existing, offer.InitialLevel);
                if (!acquisition.Succeeded)
                    return ShopPurchaseDecision.Failure(acquisition.FailureReason == RosterAcquireFailureReason.OwnershipLimitReached
                        ? ShopFailureReason.OwnershipLimitReached : ShopFailureReason.InvalidCharacter);
                if (input.Balance < offer.Cost)
                    return ShopPurchaseDecision.Failure(ShopFailureReason.InsufficientFunds);
                return ShopPurchaseDecision.ForUnit(input.Balance - offer.Cost, acquisition.Requests[0]);
            }
            if (offer.Kind == ShopProductKind.ConsumableItem)
            {
                // 재고 계산만 재사용하며 여기서는 저장소나 장착 상태를 바꾸지 않는다.
                var grant = ItemInventoryCalculator.Grant(input.Inventory, input.Items, offer.ItemKey, 1);
                if (!grant.Accepted)
                    return ShopPurchaseDecision.Failure(grant.Reason == "QuantityOverflow"
                        ? ShopFailureReason.ItemQuantityOverflow : ShopFailureReason.InvalidItem);
                if (input.Balance < offer.Cost)
                    return ShopPurchaseDecision.Failure(ShopFailureReason.InsufficientFunds);
                return ShopPurchaseDecision.ForItem(input.Balance - offer.Cost, grant.Change);
            }
            return ShopPurchaseDecision.Failure(ShopFailureReason.UnknownOffer);
        }
    }
}
