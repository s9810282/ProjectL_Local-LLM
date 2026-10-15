using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    public static class CharacterShopRules
    {
        public static bool TryPlanOffers(IReadOnlyList<ShopCandidateSpec> candidates,
            IReadOnlyList<CharacterDefinitionFacts> definitions, int seed,
            out IReadOnlyList<ShopPurchaseSnapshot> offers)
        {
            offers = null;
            if (candidates == null || definitions == null)
                throw new ArgumentNullException(candidates == null ? nameof(candidates) : nameof(definitions));
            if (candidates.Count == 0 || candidates.Count != definitions.Count)
                return false;

            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i] == null || definitions[i] == null ||
                    candidates[i].CharacterKey != definitions[i].Key ||
                    CharacterRosterRules.PlanAcquireSub(definitions[i]).Succeeded == false)
                    return false;

            var random = new Random(seed);
            var planned = new ShopPurchaseSnapshot[ShopStageSnapshot.OfferCount];
            for (int i = 0; i < planned.Length; i++)
            {
                ShopCandidateSpec candidate = candidates[random.Next(candidates.Count)];
                planned[i] = new ShopPurchaseSnapshot(new ShopOfferId(i + 1), candidate.CharacterKey, candidate.Cost);
            }

            offers = Array.AsReadOnly(planned);
            return true;
        }

        public static CharacterPurchaseDecision PlanPurchase(ShopPurchaseSnapshot offer,
            bool purchased, int balance, CharacterDefinitionFacts definition,
            MainCharacterPersistentState main)
        {
            if (balance < 0)
                throw new ArgumentOutOfRangeException(nameof(balance));
            if (main == null || main.CurrentHealth == 0)
                return CharacterPurchaseDecision.Failure(ShopFailureReason.WrongPhase);
            if (offer == null)
                return CharacterPurchaseDecision.Failure(ShopFailureReason.UnknownOffer);
            if (purchased)
                return CharacterPurchaseDecision.Failure(ShopFailureReason.SoldOut);

            CharacterAcquisitionDecision acquisition = CharacterRosterRules.PlanAcquireSub(definition);
            if (acquisition.Succeeded == false || definition.Key != offer.CharacterKey)
                return CharacterPurchaseDecision.Failure(ShopFailureReason.InvalidCharacter);
            if (balance < offer.Cost)
                return CharacterPurchaseDecision.Failure(ShopFailureReason.InsufficientFunds);

            return CharacterPurchaseDecision.Success(balance - offer.Cost, acquisition.Requests[0]);
        }
    }
}
