using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Shop
{
    public static class TrainingRules
    {
        public static IReadOnlyList<TrainingChoiceSnapshot> Choices(TrainingVisitDefinition definition,
            IReadOnlyList<OwnedCharacterRecord> owned, int balance, bool purchased = false)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            var choices = new List<TrainingChoiceSnapshot>();
            foreach (var record in owned)
            {
                var ranks = Ranks(definition, record);
                foreach (var trait in definition.Catalog.Definitions.Where(d => d.IsTrainable).OrderBy(d => d.Key))
                {
                    if (!ranks.TryGetValue(trait.Key, out int rank) || !trait.CanAdvanceFrom(rank) ||
                        !DeferredCombatFeatures.IsTraitAvailable(trait.Key)) continue;
                    int price = definition.Prices.GetPrice(rank);
                    choices.Add(new TrainingChoiceSnapshot(record.Id, trait.Key, rank, price, !purchased && balance >= price,
                        trait.ValuesAt(rank), trait.ValuesAt(rank + 1), trait.Name, trait.Description));
                }
            }
            return choices.AsReadOnly();
        }

        public static TrainingPurchaseDecision Plan(TrainingVisitDefinition definition, OwnedCharacterRecord record,
            TraitKey key, int expectedRank, int balance)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (balance < 0) throw new ArgumentOutOfRangeException(nameof(balance));
            if (record == null) return TrainingPurchaseDecision.Failure(ShopFailureReason.TargetNotOwned);
            var ranks = Ranks(definition, record);
            if (!ranks.TryGetValue(key, out int rank)) return TrainingPurchaseDecision.Failure(ShopFailureReason.TargetNotOwned);
            var trait = definition.Catalog.Definitions.FirstOrDefault(d => d.Key == key);
            if (trait?.IsTrainable != true || !DeferredCombatFeatures.IsTraitAvailable(key))
                return TrainingPurchaseDecision.Failure(ShopFailureReason.TraitNotTrainable);
            if (!trait.CanAdvanceFrom(rank)) return TrainingPurchaseDecision.Failure(ShopFailureReason.RankLimitReached);
            if (rank != expectedRank) return TrainingPurchaseDecision.Failure(ShopFailureReason.StaleRank);
            int price = definition.Prices.GetPrice(rank);
            if (balance < price) return TrainingPurchaseDecision.Failure(ShopFailureReason.InsufficientFunds);
            var final = record.WithGrowth(record.Growth.WithPurchase(key));
            _ = Ranks(definition, final);
            return TrainingPurchaseDecision.Success(final, balance - price, new TrainingReceiptSnapshot(record.Id, key, rank, price));
        }

        private static Dictionary<TraitKey, int> Ranks(TrainingVisitDefinition definition, OwnedCharacterRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            var basis = definition.Units.SingleOrDefault(u => u.Key == record.CharacterKey)
                ?? throw new InvalidOperationException("훈련 방문의 유닛 정의가 없습니다.");
            var ranks = new Dictionary<TraitKey, int>();
            OwnedUnitSpecResolver.Resolve(basis, record, definition.Catalog, ranks);
            return ranks;
        }
    }
}
