using System;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>입력 조립에 사용한 불변 원본과 값을 고정한다. 전투 상태의 별도 원본은 만들지 않는다.</summary>
    internal sealed class EnemyDecisionSourceStamp
    {
        private readonly long _battleId;
        private readonly long _queryRevision;
        private readonly int _resultSequence;
        private readonly CombatStateData _battle;
        private readonly SoulFieldSnapshot _field;
        private readonly long _rosterRevision;
        private readonly CharacterData[] _characters;
        private readonly OwnedCharacterRecord[] _owned;
        private readonly int[] _souls;
        private readonly MainCharacterPersistentState _main;
        private readonly BattleInitialization _definitions;
        private readonly ArtifactBufferSnapshot _artifacts;
        private readonly object _artifactConfiguration;
        private readonly ArtifactDefinitionData[] _artifactDefinitions;
        private readonly ItemInventorySnapshot _items;
        private readonly int _gold;
        private readonly int _fame;

        internal EnemyDecisionSourceStamp(ICombatStateBufferReadSink combat, ICharacterBufferReadSink characters,
            IBattleDefinitionRead definitions, ArtifactLoadoutReader loadout, ArtifactBufferSnapshot artifacts,
            ItemInventorySnapshot items, int gold, int fame)
        {
            _battleId = combat.BattleInstanceId;
            _queryRevision = combat.QueryRevision;
            _resultSequence = combat.LatestResultSequence;
            _battle = combat.Current;
            _field = combat.SoulField;
            _rosterRevision = characters.RosterRevision;
            _definitions = definitions.Require(_battleId);
            _artifacts = artifacts;
            _artifactConfiguration = loadout.Configuration;
            _artifactDefinitions = new ArtifactDefinitionData[artifacts.PlacedItems.Count];
            for (int i = 0; i < _artifactDefinitions.Length; i++)
                _artifactDefinitions[i] = loadout.ReadDefinition(artifacts.PlacedItems[i].ArtifactId);
            _items = items;
            _gold = gold;
            _fame = fame;
            _characters = new CharacterData[characters.Characters.Count];
            for (int i = 0; i < _characters.Length; i++) _characters[i] = characters.Characters[i];
            var membership = _battle.Membership ?? throw new InvalidOperationException("전투 참가 목록이 없습니다.");
            _owned = new OwnedCharacterRecord[membership.OrderedIds.Count];
            _souls = new int[_owned.Length];
            for (int i = 0; i < _owned.Length; i++)
            {
                var id = membership.OrderedIds[i];
                if (!characters.TryGetOwned(id, out _owned[i]))
                    throw new InvalidOperationException("전투 참가자의 보유 기록이 없습니다.");
                _souls[i] = characters.GetSoulCount(id);
            }
            if (!characters.TryGetMainState(membership.MainId, out _main))
                throw new InvalidOperationException("메인 지속 상태가 없습니다.");
        }

        internal bool Matches(ICombatStateBufferReadSink combat, ICharacterBufferReadSink characters,
            IBattleDefinitionRead definitions, ArtifactLoadoutReader loadout, ArtifactBufferSnapshot artifacts,
            ItemInventorySnapshot items, int gold, int fame)
        {
            if (!combat.HasBattle || combat.Completion != null || combat.BattleInstanceId != _battleId ||
                combat.QueryRevision != _queryRevision || combat.LatestResultSequence != _resultSequence ||
                !ReferenceEquals(combat.Current, _battle) || !ReferenceEquals(combat.SoulField, _field) ||
                characters.RosterRevision != _rosterRevision || characters.Characters.Count != _characters.Length ||
                !ReferenceEquals(artifacts, _artifacts) || !ReferenceEquals(items, _items) || gold != _gold || fame != _fame ||
                !ReferenceEquals(loadout.Configuration, _artifactConfiguration) ||
                !ReferenceEquals(definitions.Require(_battleId), _definitions)) return false;
            for (int i = 0; i < _characters.Length; i++)
                if (!ReferenceEquals(characters.Characters[i], _characters[i])) return false;
            for (int i = 0; i < _owned.Length; i++)
                if (!characters.TryGetOwned(_owned[i].Id, out var owned) || !ReferenceEquals(owned, _owned[i]) ||
                    characters.GetSoulCount(_owned[i].Id) != _souls[i]) return false;
            for (int i = 0; i < _artifactDefinitions.Length; i++)
                if (!ReferenceEquals(loadout.ReadDefinition(_artifacts.PlacedItems[i].ArtifactId), _artifactDefinitions[i])) return false;
            return characters.TryGetMainState(_battle.Membership.MainId, out var main) && ReferenceEquals(main, _main);
        }
    }
}
