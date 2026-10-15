using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public readonly struct CharacterDeckEntry
    {
        public EntityKey Key { get; }
        public int Count { get; }

        public CharacterDeckEntry(EntityKey key, int count)
        {
            if (key == EntityKey.None)
                throw new ArgumentException("Character key cannot be None.", nameof(key));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            Key = key;
            Count = count;
        }

        internal static CharacterDeckEntry[] CopyAndValidate(
            IReadOnlyList<CharacterDeckEntry> entries,
            string parameterName,
            out int totalCount)
        {
            if (entries == null)
                throw new ArgumentNullException(parameterName);

            var copied = new CharacterDeckEntry[entries.Count];
            var keys = new HashSet<EntityKey>();
            totalCount = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                CharacterDeckEntry entry = entries[i];
                if (entry.Key == EntityKey.None || entry.Count <= 0)
                    throw new ArgumentException("Character deck contains an invalid entry.", parameterName);
                if (keys.Add(entry.Key) == false)
                    throw new ArgumentException("Character deck contains a duplicate key.", parameterName);

                totalCount = checked(totalCount + entry.Count);
                copied[i] = entry;
            }

            return copied;
        }
    }

    public sealed class RunCharacterDeckSpec
    {
        public ReadOnlyMemory<CharacterDeckEntry> Entries { get; }
        public int InitialDrawCount { get; }
        public int DrawBaseSeed { get; }

        public RunCharacterDeckSpec(
            IReadOnlyList<CharacterDeckEntry> entries,
            int initialDrawCount,
            int drawBaseSeed)
        {
            CharacterDeckEntry[] copied = CharacterDeckEntry.CopyAndValidate(
                entries,
                nameof(entries),
                out _);
            if (initialDrawCount < 0)
                throw new ArgumentOutOfRangeException(nameof(initialDrawCount));

            Entries = copied;
            InitialDrawCount = initialDrawCount;
            DrawBaseSeed = drawBaseSeed;
        }
    }
}
