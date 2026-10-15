using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class ShopStageSpec
    {
        public StageKey StageKey { get; }
        public ReadOnlyMemory<ShopCandidateSpec> Candidates { get; }

        public ShopStageSpec(StageKey stageKey, IReadOnlyList<ShopCandidateSpec> candidates)
        {
            if (stageKey.IsEmpty) throw new ArgumentException(nameof(stageKey));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (candidates.Count == 0) throw new ArgumentException("At least one candidate is required.", nameof(candidates));
            var copy = new ShopCandidateSpec[candidates.Count];
            var keys = new HashSet<EntityKey>();
            for (int i = 0; i < copy.Length; i++)
            {
                var candidate = candidates[i];
                if (candidate == null || !keys.Add(candidate.CharacterKey))
                    throw new ArgumentException("Candidates must be non-null and have unique character keys.", nameof(candidates));
                copy[i] = candidate;
            }
            StageKey = stageKey;
            Candidates = copy;
        }
    }
}
