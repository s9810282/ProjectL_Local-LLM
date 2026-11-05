using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    /// <summary>작성된 grant 순서와 같은 정의를 포착해 시작 로스터·초기 선택 판정에 함께 전달한다.</summary>
    internal static class PlayerLoadoutRosterPlanner
    {
        internal static CharacterAcquisitionDecision Plan(IPlayerLoadoutTableQuery loadouts, IUnitTableQuery units,
            int loadoutKey, bool rosterIsEmpty, out PlayerLoadoutData loadout) =>
            Plan(loadouts, units, loadoutKey, rosterIsEmpty, out loadout, out _);

        internal static CharacterAcquisitionDecision Plan(IPlayerLoadoutTableQuery loadouts, IUnitTableQuery units,
            int loadoutKey, bool rosterIsEmpty, out PlayerLoadoutData loadout,
            out IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions)
        {
            if (loadouts == null) throw new ArgumentNullException(nameof(loadouts));
            if (units == null) throw new ArgumentNullException(nameof(units));
            var byKey = new Dictionary<EntityKey, CharacterDefinitionFacts>();
            definitions = byKey;
            if (!loadouts.TryGetPlayerLoadout(loadoutKey, out loadout))
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);
            loadout.Validate();
            var main = CharacterDefinitionAdapter.Read(units, loadout.MainCharacterKey);
            if (main == null) return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);
            byKey.Add(main.Key, main);
            var resulting = new List<CharacterDefinitionFacts> { main };
            var subs = new List<CharacterDefinitionFacts>();
            foreach (var entry in loadout.Entries)
            {
                var definition = CharacterDefinitionAdapter.Read(units, entry.UnitKey);
                if (definition == null) return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);
                if (!byKey.ContainsKey(definition.Key)) byKey.Add(definition.Key, definition);
                for (int i = 0; i < entry.Count; i++)
                {
                    resulting.Add(definition);
                    // 같은 정책으로 상한 도달 즉시 거절하여 잘못된 큰 수량을 배열로 펼치지 않는다.
                    var reason = CharacterRosterRules.ValidateOwnership(resulting);
                    if (reason != RosterAcquireFailureReason.None) return CharacterAcquisitionDecision.Failure(reason);
                    subs.Add(definition);
                }
            }
            return CharacterRosterRules.PlanInitialRoster(main, subs, rosterIsEmpty);
        }
    }
}
