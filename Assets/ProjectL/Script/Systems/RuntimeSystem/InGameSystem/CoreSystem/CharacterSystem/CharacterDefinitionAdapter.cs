using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    internal static class CharacterDefinitionAdapter
    {
        internal static CharacterDefinitionFacts Read(IUnitTableQuery units, EntityKey key)
        {
            if (units.TryGetUnit(key, out var data) == false)
                return null;

            data.Validate();
            MainCharacterPersistentState initialMain = data.Role == UnitRole.MainCharacter
                ? new MainCharacterPersistentState(data.MaxHealth, data.MaxHealth,
                    data.AttackPower, data.MaxActionCost, data.MoveRange, data.MoveCost)
                : null;

            return new CharacterDefinitionFacts(data.Key, data.Role, data.OwnerEligibility, initialMain);
        }
    }
}
