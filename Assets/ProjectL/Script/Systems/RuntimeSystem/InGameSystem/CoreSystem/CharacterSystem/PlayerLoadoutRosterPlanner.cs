using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    /// <summary>
    /// 시작 로드아웃(DB)을 Core 시작 로스터 규칙에 넘겨 획득 결정을 만든다.
    /// 로스터 초기화(InitializeRoster)와 새 Run 시작(StartNewRun)이 같은 규칙을 쓰도록 한 곳에 둔다.
    /// </summary>
    internal static class PlayerLoadoutRosterPlanner
    {
        internal static CharacterAcquisitionDecision Plan(IPlayerLoadoutTableQuery loadouts, IUnitTableQuery units,
            int loadoutKey, bool rosterIsEmpty, out PlayerLoadoutData loadout)
        {
            if (loadouts == null) throw new ArgumentNullException(nameof(loadouts));
            if (units == null) throw new ArgumentNullException(nameof(units));
            if (loadouts.TryGetPlayerLoadout(loadoutKey, out loadout) == false)
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidDefinition);

            loadout.Validate();
            long count = 0;
            foreach (var entry in loadout.Entries)
                count += entry.Count;
            // 잘못된 DB 수량을 큰 배열로 펼치지 않고 Core 정책의 상한에서 거절한다.
            if (count > CharacterRosterRules.StartingSubCount)
                return CharacterAcquisitionDecision.Failure(RosterAcquireFailureReason.InvalidRequest);

            var subs = new List<CharacterDefinitionFacts>((int)count);
            foreach (var entry in loadout.Entries)
            {
                CharacterDefinitionFacts definition = CharacterDefinitionAdapter.Read(units, entry.UnitKey);
                for (int i = 0; i < entry.Count; i++)
                    subs.Add(definition);
            }

            return CharacterRosterRules.PlanInitialRoster(
                CharacterDefinitionAdapter.Read(units, loadout.MainCharacterKey), subs, rosterIsEmpty);
        }
    }
}
