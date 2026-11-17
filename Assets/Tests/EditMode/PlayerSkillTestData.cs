using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    internal static class PlayerSkillTestData
    {
        internal static readonly EntityId MainActor = new(10);
        internal static readonly CharacterInstanceId MainOwner = new(1);
        internal static readonly PlayerSkillDefinition Definition = new(PlayerSkillKey.PowderBlossom, EntityKey.Unit_Akimbo, 2);

        internal static CombatCommandInput Input(bool configured = true, long battleId = 1)
        {
            var basis = CombatCommandTestData.Input();
            var main = CombatCommandTestData.Character(MainActor.Value, EntityKey.Unit_Akimbo, new CellCoord(3, 2), Owner.Player, 20, main: true);
            var characters = basis.Characters.Select(c => c.EntityId == MainActor ? main : c).ToArray();
            var owned = characters.Where(c => c.OwnedId.HasValue)
                .Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray();
            var state = configured
                ? basis.State.With(playerSkill: new PlayerSkillBattleState(MainOwner, MainActor, Definition, PlayerSkillUpgradeKey.None))
                : basis.State;
            PlayerSkillSourceStamp? source = battleId == 0 ? null : new PlayerSkillSourceStamp(battleId, 1, 7, MainActor);
            return new CombatCommandInput(1, CombatCommandKind.Attack, MainActor, AttackAim.ForCell(new CellCoord(4, 3)),
                state, characters, basis.Profiles, basis.Main, owned, basis.SoulCounts, basis.Souls, basis.Artifacts,
                path: new MovementPath(new[] { main.Anchor.Value, new CellCoord(3, 1) }),
                battleInstanceId: battleId, membership: new RunDeckSnapshot(new RunId(new Guid("07190ff1-79f8-429a-a4a9-739a72f71c87")), 1,
                    owned.Select(o => o.Id).OrderBy(id => id.Value).ToArray()),
                expectedBattleInstanceId: battleId, expectedControllerTurnSequence: 1,
                playerSkillRequest: new PlayerSkillRequest(Array.Empty<CellCoord>(), source), playerSkillSource: source);
        }
    }
}
