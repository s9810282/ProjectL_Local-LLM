using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    public static class TraitGrowthOfferSampler
    {
        public static IReadOnlyList<TraitKey> Sample(IReadOnlyList<TraitKey> candidates, TraitGrowthKind kind, int seed)
        {
            var keys = SortedCandidates(candidates, kind);
            int count = Math.Min(TraitGrowthOffer.MaximumChoices, keys.Length);
            var random = new Random(seed);
            for (int i = 0; i < count; i++)
            {
                int selected = random.Next(i, keys.Length);
                (keys[i], keys[selected]) = (keys[selected], keys[i]);
            }
            return Array.AsReadOnly(keys.Take(count).OrderBy(k => k).ToArray());
        }

        public static IReadOnlyList<TraitKey> Sample(IReadOnlyList<TraitKey> candidates, TraitGrowthKind kind, int seed,
            IReadOnlyDictionary<TraitKey, TraitGrowthGroup> groups, int commonWeight, int categoryWeight)
        {
            if (kind != TraitGrowthKind.Acquire)
                return Sample(candidates, kind, seed);

            var keys = SortedCandidates(candidates, kind);
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));
            CategoryTraitProgressionSpec.ValidateWeights(commonWeight, categoryWeight);
            int totalWeight = commonWeight + categoryWeight;

            var common = new List<TraitKey>();
            var category = new List<TraitKey>();
            foreach (var key in keys)
            {
                if (!groups.TryGetValue(key, out var group))
                    throw new ArgumentException($"성장 후보의 특성 그룹이 없습니다: {key}.", nameof(groups));
                switch (group)
                {
                    case TraitGrowthGroup.Common:
                        common.Add(key);
                        break;
                    case TraitGrowthGroup.CategorySpecific:
                        category.Add(key);
                        break;
                    default:
                        throw new ArgumentException($"성장 후보의 특성 그룹이 올바르지 않습니다: {key}, {group}.", nameof(groups));
                }
            }

            var random = new Random(seed);
            var sample = new TraitKey[Math.Min(TraitGrowthOffer.MaximumChoices, keys.Length)];
            for (int i = 0; i < sample.Length; i++)
            {
                bool drawCommon = common.Count > 0 &&
                    (category.Count == 0 || random.Next(totalWeight) < commonWeight);
                var pool = drawCommon ? common : category;
                int index = random.Next(pool.Count);
                sample[i] = pool[index];
                pool.RemoveAt(index);
            }
            Array.Sort(sample);
            return Array.AsReadOnly(sample);
        }

        private static TraitKey[] SortedCandidates(IReadOnlyList<TraitKey> candidates, TraitGrowthKind kind)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));
            if (kind != TraitGrowthKind.Acquire && kind != TraitGrowthKind.Enhance)
                throw new ArgumentOutOfRangeException(nameof(kind));
            var keys = candidates.OrderBy(k => k).ToArray();
            if (keys.Distinct().Count() != keys.Length || keys.Any(k => k == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), k)))
                throw new ArgumentException("중복 없는 유효 후보가 필요합니다.", nameof(candidates));
            if (keys.Length == 0)
                throw new InvalidOperationException("성장 제안 후보가 부족합니다.");
            return keys;
        }

        public static int Seed(RunId runId, CharacterInstanceId ownedId, int stepId)
        {
            if (runId.IsEmpty || ownedId.IsEmpty || stepId < 1)
                throw new ArgumentException("제안 seed 식별자가 필요합니다.");
            unchecked
            {
                uint hash = 2166136261;
                foreach (byte value in runId.Value.ToByteArray())
                    hash = (hash ^ value) * 16777619;
                hash = (hash ^ (uint)ownedId.Value) * 16777619;
                return (int)((hash ^ (uint)stepId) * 16777619);
            }
        }
    }
}
