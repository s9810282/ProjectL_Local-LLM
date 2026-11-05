using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck
{
    /// <summary>같은 원본에서 포착한 선택·보유·정의·능력치와 버전 묶음이다. 런타임 캡처는 서비스의 책임이다.</summary>
    public sealed class DeckEditorSnapshot
    {
        public RunId RunId => Deck.RunId;
        public long RosterRevision { get; }
        public long DeckRevision => Deck.DeckRevision;
        public RunDeckSnapshot Deck { get; }
        public IReadOnlyList<OwnedCharacterRecord> Owned { get; }
        public IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> Definitions { get; }
        public IReadOnlyList<OwnedCharacterStatsSnapshot> Stats { get; }
        public DeckEditAvailability Availability { get; }

        public DeckEditorSnapshot(long rosterRevision, RunDeckSnapshot deck, IReadOnlyList<OwnedCharacterRecord> owned,
            IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions, IReadOnlyList<OwnedCharacterStatsSnapshot> stats,
            DeckEditAvailability availability)
        {
            if (rosterRevision < 0) throw new ArgumentOutOfRangeException(nameof(rosterRevision));
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
            Availability = availability ?? throw new ArgumentNullException(nameof(availability));
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (stats == null) throw new ArgumentNullException(nameof(stats));
            var definitionCopy = new Dictionary<EntityKey, CharacterDefinitionFacts>();
            foreach (var entry in definitions)
            {
                if (entry.Value == null || entry.Key != entry.Value.Key)
                    throw new ArgumentException("정의 키와 값이 일치해야 합니다.", nameof(definitions));
                definitionCopy.Add(entry.Key, entry.Value);
            }
            var ownedCopy = new OwnedCharacterRecord[owned.Count];
            var byId = new Dictionary<CharacterInstanceId, OwnedCharacterRecord>();
            for (int i = 0; i < ownedCopy.Length; i++)
            {
                var record = owned[i];
                if (record == null || byId.ContainsKey(record.Id) || !definitionCopy.ContainsKey(record.CharacterKey))
                    throw new ArgumentException("보유 ID는 고유하며 대응 정의가 필요합니다.", nameof(owned));
                byId.Add(record.Id, record);
                ownedCopy[i] = record;
            }
            foreach (var id in deck.OrderedIds)
                if (!byId.ContainsKey(id)) throw new ArgumentException("선택 ID가 보유 목록에 없습니다.", nameof(deck));
            if (stats.Count != owned.Count) throw new ArgumentException("보유마다 능력치가 필요합니다.", nameof(stats));
            var statsCopy = new OwnedCharacterStatsSnapshot[stats.Count];
            var seen = new HashSet<CharacterInstanceId>();
            for (int i = 0; i < statsCopy.Length; i++)
            {
                var item = stats[i];
                if (item == null || !seen.Add(item.OwnedId) || !byId.TryGetValue(item.OwnedId, out var record) ||
                    item.CharacterKey != record.CharacterKey || item.Role != record.Role || item.Level != record.Level)
                    throw new ArgumentException("능력치가 보유 개체와 일치해야 합니다.", nameof(stats));
                statsCopy[i] = item;
            }
            RosterRevision = rosterRevision;
            Owned = Array.AsReadOnly(ownedCopy);
            Definitions = new System.Collections.ObjectModel.ReadOnlyDictionary<EntityKey, CharacterDefinitionFacts>(definitionCopy);
            Stats = Array.AsReadOnly(statsCopy);
        }
    }
}
