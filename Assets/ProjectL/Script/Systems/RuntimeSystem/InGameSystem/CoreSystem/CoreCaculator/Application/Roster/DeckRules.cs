using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    public static class DeckRules
    {
        private const int MaximumCardCount = 4;

        private readonly struct Candidate
        {
            internal bool Known { get; }
            internal EntityKey Key { get; }
            internal CharacterOwnershipRole Role { get; }
            internal Candidate(EntityKey key, CharacterOwnershipRole role)
            {
                Known = true;
                Key = key;
                Role = role;
            }
        }

        public static DeckSelectionDecision Evaluate(IReadOnlyList<CharacterInstanceId> requested,
            IReadOnlyList<OwnedCharacterRecord> owned, IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions)
        {
            if (requested == null) throw new ArgumentNullException(nameof(requested));
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var countReason = ValidateCount(requested.Count);
            if (countReason != DeckFailureReason.None) return new DeckSelectionDecision(countReason);
            var byId = new Dictionary<CharacterInstanceId, OwnedCharacterRecord>();
            foreach (var record in owned)
            {
                if (record == null || byId.ContainsKey(record.Id))
                    throw new ArgumentException("보유 목록에 null 또는 중복 ID가 있습니다.", nameof(owned));
                byId.Add(record.Id, record);
            }
            var candidates = new Candidate[requested.Count];
            for (int i = 0; i < candidates.Length; i++)
                if (byId.TryGetValue(requested[i], out var record)) candidates[i] = new Candidate(record.CharacterKey, record.Role);
            var result = EvaluateResolved(requested, candidates, definitions);
            return new DeckSelectionDecision(result.Reason, result.Ordered);
        }

        public static (DeckFailureReason Reason, IReadOnlyList<int> CanonicalGrantIndices) EvaluateInitial(
            IReadOnlyList<int> requestedGrantIndices, IReadOnlyList<RosterAcquireRequest> grants,
            IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions)
        {
            if (requestedGrantIndices == null) throw new ArgumentNullException(nameof(requestedGrantIndices));
            if (grants == null) throw new ArgumentNullException(nameof(grants));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var countReason = ValidateCount(requestedGrantIndices.Count);
            if (countReason != DeckFailureReason.None) return (countReason, null);
            var candidates = new Candidate[requestedGrantIndices.Count];
            for (int i = 0; i < candidates.Length; i++)
            {
                int index = requestedGrantIndices[i];
                if (index < 0 || index >= grants.Count) continue;
                var grant = grants[index];
                candidates[i] = grant == null ? new Candidate(EntityKey.None, CharacterOwnershipRole.Sub) :
                    new Candidate(grant.CharacterKey, grant.Role);
            }
            var result = EvaluateResolved(requestedGrantIndices, candidates, definitions);
            return (result.Reason, result.Ordered);
        }

        private static DeckFailureReason ValidateCount(int count) => count == 0 ? DeckFailureReason.EmptyDeck :
            count > MaximumCardCount ? DeckFailureReason.TooManyCards : DeckFailureReason.None;

        // 소유 ID와 초기 grant index는 각자 유지하고, 해석한 행의 적법성·순서 판정만 공유한다.
        private static (DeckFailureReason Reason, IReadOnlyList<T> Ordered) EvaluateResolved<T>(
            IReadOnlyList<T> requested, Candidate[] candidates, IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions)
        {
            var seen = new HashSet<T>();
            int mainIndex = -1;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (!seen.Add(requested[i])) return (DeckFailureReason.DuplicateOwnedId, null);
                var candidate = candidates[i];
                if (!candidate.Known) return (DeckFailureReason.UnknownOwnedId, null);
                if (!definitions.TryGetValue(candidate.Key, out var definition) || definition == null ||
                    definition.Key != candidate.Key || !CharacterRosterRules.IsSupportedDefinition(definition))
                    return (DeckFailureReason.UnsupportedUnit, null);
                bool isMain = definition.Role == UnitRole.MainCharacter;
                if (candidate.Role != (isMain ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub))
                    return (DeckFailureReason.UnsupportedUnit, null);
                if (isMain)
                {
                    if (mainIndex >= 0) return (DeckFailureReason.MultipleMains, null);
                    mainIndex = i;
                }
            }
            if (mainIndex < 0) return (DeckFailureReason.MissingMain, null);
            var ordered = new T[requested.Count];
            ordered[0] = requested[mainIndex];
            int next = 1;
            for (int i = 0; i < requested.Count; i++)
                if (i != mainIndex) ordered[next++] = requested[i];
            return (DeckFailureReason.None, Array.AsReadOnly(ordered));
        }
    }
}
