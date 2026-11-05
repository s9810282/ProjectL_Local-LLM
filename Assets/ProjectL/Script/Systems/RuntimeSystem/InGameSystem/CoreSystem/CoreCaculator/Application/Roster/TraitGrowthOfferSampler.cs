using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    public static class TraitGrowthOfferSampler
    {
        public static IReadOnlyList<TraitKey> Sample(IReadOnlyList<TraitKey> candidates, TraitGrowthKind kind, int seed)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));
            if (kind != TraitGrowthKind.Acquire && kind != TraitGrowthKind.Enhance)
                throw new ArgumentOutOfRangeException(nameof(kind));
            var keys = candidates.OrderBy(k => k).ToArray();
            if (keys.Distinct().Count() != keys.Length || keys.Any(k => k == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), k)))
                throw new ArgumentException("중복 없는 유효 후보가 필요합니다.", nameof(candidates));
            int count = Math.Min(3, keys.Length);
            if (keys.Length < count || count == 0)
                throw new InvalidOperationException("성장 제안 후보가 부족합니다.");
            var random = new Random(seed);
            for (int i = 0; i < count; i++)
            {
                int selected = random.Next(i, keys.Length);
                (keys[i], keys[selected]) = (keys[selected], keys[i]);
            }
            return Array.AsReadOnly(keys.Take(count).OrderBy(k => k).ToArray());
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
