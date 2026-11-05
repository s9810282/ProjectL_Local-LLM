using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    public sealed class DeckSelectionDecision
    {
        public bool Accepted => Reason == DeckFailureReason.None;
        public DeckFailureReason Reason { get; }
        public IReadOnlyList<CharacterInstanceId> CanonicalIds { get; }

        public DeckSelectionDecision(DeckFailureReason reason, IReadOnlyList<CharacterInstanceId> canonicalIds = null)
        {
            if (!Enum.IsDefined(typeof(DeckFailureReason), reason)) throw new ArgumentOutOfRangeException(nameof(reason));
            if ((reason == DeckFailureReason.None) != (canonicalIds != null))
                throw new ArgumentException("성공 결정만 정규화된 ID를 가져야 합니다.", nameof(canonicalIds));
            if (canonicalIds != null)
            {
                if (canonicalIds.Count == 0) throw new ArgumentException("성공 결정은 비어 있을 수 없습니다.", nameof(canonicalIds));
                var copy = new CharacterInstanceId[canonicalIds.Count];
                var seen = new HashSet<CharacterInstanceId>();
                for (int i = 0; i < copy.Length; i++)
                {
                    if (canonicalIds[i].IsEmpty || !seen.Add(canonicalIds[i]))
                        throw new ArgumentException("성공 결정의 ID는 비거나 중복될 수 없습니다.", nameof(canonicalIds));
                    copy[i] = canonicalIds[i];
                }
                CanonicalIds = Array.AsReadOnly(copy);
            }
            Reason = reason;
        }
    }
}
