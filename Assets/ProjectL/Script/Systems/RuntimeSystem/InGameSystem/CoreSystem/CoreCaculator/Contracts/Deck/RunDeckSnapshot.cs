using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    /// <summary>Core가 정규화한 Run 선택이다. 역할·장수 판정은 DTO가 아닌 Core 규칙이 담당한다.</summary>
    public sealed class RunDeckSnapshot
    {
        public RunId RunId { get; }
        public long DeckRevision { get; }
        public IReadOnlyList<CharacterInstanceId> OrderedIds { get; }
        public CharacterInstanceId MainId => OrderedIds[0];

        public RunDeckSnapshot(RunId runId, long deckRevision, IReadOnlyList<CharacterInstanceId> orderedIds)
        {
            if (runId.IsEmpty) throw new ArgumentException("Run 식별자가 필요합니다.", nameof(runId));
            if (deckRevision < 0) throw new ArgumentOutOfRangeException(nameof(deckRevision));
            if (orderedIds == null) throw new ArgumentNullException(nameof(orderedIds));
            if (orderedIds.Count == 0) throw new ArgumentException("확정 선택은 비어 있을 수 없습니다.", nameof(orderedIds));
            var copy = new CharacterInstanceId[orderedIds.Count];
            var seen = new HashSet<CharacterInstanceId>();
            for (int i = 0; i < copy.Length; i++)
            {
                if (orderedIds[i].IsEmpty || !seen.Add(orderedIds[i]))
                    throw new ArgumentException("선택 ID는 비거나 중복될 수 없습니다.", nameof(orderedIds));
                copy[i] = orderedIds[i];
            }
            RunId = runId;
            DeckRevision = deckRevision;
            OrderedIds = Array.AsReadOnly(copy);
        }
    }
}
