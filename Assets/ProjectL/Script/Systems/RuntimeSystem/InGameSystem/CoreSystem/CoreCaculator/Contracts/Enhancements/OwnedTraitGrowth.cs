using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    /// <summary>선택 순서와 단계 식별자를 보존하는 불변 성장 이력이다.</summary>
    public sealed class OwnedTraitGrowth
    {
        public static OwnedTraitGrowth Empty { get; } = new(Array.Empty<TraitGrowthSelection>());
        public IReadOnlyList<TraitGrowthSelection> Selections { get; }
        public IReadOnlyDictionary<TraitKey, int> Purchases { get; }
        public bool IsEmpty => Selections.Count == 0 && Purchases.Count == 0;

        public OwnedTraitGrowth(IReadOnlyList<TraitGrowthSelection> selections,
            IReadOnlyDictionary<TraitKey, int> purchases = null)
        {
            var copy = (selections ?? throw new ArgumentNullException(nameof(selections))).ToArray();
            if (copy.Any(s => s == null) || copy.Select(s => s.StepId).Distinct().Count() != copy.Length)
                throw new ArgumentException("성장 이력에 null 또는 중복 단계가 있습니다.", nameof(selections));
            var counts = purchases?.ToDictionary(p => p.Key, p => p.Value) ?? new Dictionary<TraitKey, int>();
            if (counts.Any(p => p.Key == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), p.Key) || p.Value < 1 || p.Value > 2))
                throw new ArgumentException("구매 이력의 키 또는 횟수가 올바르지 않습니다.", nameof(purchases));
            Purchases = new ReadOnlyDictionary<TraitKey, int>(counts);
            Selections = Array.AsReadOnly(copy);
        }
        public OwnedTraitGrowth Append(TraitGrowthSelection selection) => new(Selections.Append(selection).ToArray(), Purchases);

        public OwnedTraitGrowth WithPurchase(TraitKey key)
        {
            var counts = Purchases.ToDictionary(p => p.Key, p => p.Value);
            counts.TryGetValue(key, out var count);
            counts[key] = count + 1;
            return new OwnedTraitGrowth(Selections, counts);
        }

        internal bool SamePurchases(OwnedTraitGrowth other) => Purchases.Count == other.Purchases.Count &&
            Purchases.All(p => other.Purchases.TryGetValue(p.Key, out int count) && count == p.Value);
        public bool SameValues(OwnedTraitGrowth other) => other != null && SamePurchases(other) && Selections.Count == other.Selections.Count &&
            Selections.Select((s, i) => s.SameValues(other.Selections[i])).All(s => s);
    }
}
