using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class RunCharacterDeckSnapshot
    {
        public RunId RunId { get; }
        public long DeckRevision { get; }
        public ReadOnlyMemory<CharacterDeckEntry> Entries { get; }
        public int TotalCount { get; }

        public RunCharacterDeckSnapshot(
            RunId runId,
            long deckRevision,
            IReadOnlyList<CharacterDeckEntry> entries)
        {
            if (runId.IsEmpty)
                throw new ArgumentException("RunId cannot be empty.", nameof(runId));
            if (deckRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(deckRevision));

            CharacterDeckEntry[] copied = CharacterDeckEntry.CopyAndValidate(
                entries,
                nameof(entries),
                out int totalCount);

            RunId = runId;
            DeckRevision = deckRevision;
            Entries = copied;
            TotalCount = totalCount;
        }
    }
}
