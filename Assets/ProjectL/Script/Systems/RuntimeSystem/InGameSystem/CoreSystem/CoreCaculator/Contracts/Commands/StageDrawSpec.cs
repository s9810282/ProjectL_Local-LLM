using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class StageDrawSpec
    {
        public RunId RunId { get; }
        public StageEntryId EntryId { get; }
        public long DeckRevision { get; }
        public ReadOnlyMemory<CharacterDeckEntry> Entries { get; }
        public int InitialDrawCount { get; }
        public int Seed { get; }

        public StageDrawSpec(
            RunId runId,
            StageEntryId entryId,
            long deckRevision,
            IReadOnlyList<CharacterDeckEntry> entries,
            int initialDrawCount,
            int seed)
        {
            if (runId.IsEmpty)
                throw new ArgumentException("RunId cannot be empty.", nameof(runId));
            if (entryId.IsEmpty)
                throw new ArgumentException("StageEntryId cannot be empty.", nameof(entryId));
            if (deckRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(deckRevision));
            if (initialDrawCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialDrawCount));

            CharacterDeckEntry[] copied = CharacterDeckEntry.CopyAndValidate(
                entries,
                nameof(entries),
                out _);

            RunId = runId;
            EntryId = entryId;
            DeckRevision = deckRevision;
            Entries = copied;
            InitialDrawCount = initialDrawCount;
            Seed = seed;
        }
    }
}
