using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>확정 출전 덱만 읽습니다. 아직 드로우되지 않은 선택도 자원 준비 대상입니다.</summary>
    internal sealed class CombatUnitPreparationReader
    {
        private readonly IRunDeckReadSink _deck;
        private readonly ICharacterBufferReadSink _characters;

        internal CombatUnitPreparationReader(IRunDeckReadSink deck, ICharacterBufferReadSink characters)
        {
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
        }

        internal CombatUnitPreparationInput Read(long battleInstance)
        {
            if (!_deck.TryGetCurrent(out var deck))
                throw new InvalidOperationException("[CombatUnitPreparation] 확정 RunDeck이 없습니다.");
            var current = new Dictionary<CharacterInstanceId, CharacterData>();
            foreach (CharacterData character in _characters.Characters)
            {
                if (!character.OwnedId.HasValue) continue;
                if (!current.TryAdd(character.OwnedId.Value, character))
                    throw new InvalidOperationException($"[CombatUnitPreparation] 전투 개체의 보유 ID가 중복됩니다: {character.OwnedId}.");
            }

            var targets = new CombatUnitPreparationTarget[deck.OrderedIds.Count];
            var counts = new Dictionary<EntityKey, int>();
            for (int i = 0; i < targets.Length; i++)
            {
                CharacterInstanceId id = deck.OrderedIds[i];
                if (!_characters.TryGetOwned(id, out var owned))
                    throw new InvalidOperationException($"[CombatUnitPreparation] 선택한 보유 유닛을 찾을 수 없습니다: {id}.");
                current.TryGetValue(id, out CharacterData character);
                if (character != null && character.CharacterKey != owned.CharacterKey)
                    throw new InvalidOperationException($"[CombatUnitPreparation] 보유/전투 유닛 종류가 다릅니다: {id}.");
                targets[i] = new CombatUnitPreparationTarget(id, owned.CharacterKey, character);
                counts.TryGetValue(owned.CharacterKey, out int count);
                counts[owned.CharacterKey] = checked(count + 1);
            }
            return new CombatUnitPreparationInput(deck.RunId, deck.DeckRevision, battleInstance,
                Array.AsReadOnly(targets), new ReadOnlyDictionary<EntityKey, int>(counts));
        }

        internal bool IsCurrent(CombatUnitPreparationInput input) =>
            _deck.TryGetCurrent(out var deck) && deck.RunId == input.RunId && deck.DeckRevision == input.DeckRevision;
    }

    /// <summary>한 연결에서만 소비하는 불변 작업 입력입니다. 전투 수치가 없으면 공통 자원만 준비합니다.</summary>
    internal sealed class CombatUnitPreparationInput
    {
        internal RunId RunId { get; }
        internal long DeckRevision { get; }
        internal long BattleInstance { get; }
        internal IReadOnlyList<CombatUnitPreparationTarget> Targets { get; }
        internal IReadOnlyDictionary<EntityKey, int> Counts { get; }

        internal CombatUnitPreparationInput(RunId runId, long deckRevision, long battleInstance,
            IReadOnlyList<CombatUnitPreparationTarget> targets, IReadOnlyDictionary<EntityKey, int> counts)
        {
            RunId = runId;
            DeckRevision = deckRevision;
            BattleInstance = battleInstance;
            Targets = targets;
            Counts = counts;
        }
    }

    internal readonly struct CombatUnitPreparationTarget
    {
        internal CharacterInstanceId OwnedId { get; }
        internal EntityKey Key { get; }
        internal CharacterData Character { get; }

        internal CombatUnitPreparationTarget(CharacterInstanceId ownedId, EntityKey key, CharacterData character)
        {
            OwnedId = ownedId;
            Key = key;
            Character = character;
        }
    }
}
