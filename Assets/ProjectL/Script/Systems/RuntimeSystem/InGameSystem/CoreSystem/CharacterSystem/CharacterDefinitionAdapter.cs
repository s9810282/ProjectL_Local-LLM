using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    internal static class CharacterDefinitionAdapter
    {
        internal static IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> ReadAll(IUnitTableQuery units,
            IReadOnlyList<OwnedCharacterRecord> owned, IEnumerable<EntityKey> additionalKeys = null)
        {
            var definitions = new Dictionary<EntityKey, CharacterDefinitionFacts>();
            foreach (var key in owned.Select(x => x.CharacterKey).Concat(additionalKeys ?? Array.Empty<EntityKey>()).Distinct())
                definitions.Add(key, Read(units, key) ?? throw new InvalidOperationException("보유·획득 대상의 정의가 없습니다."));
            foreach (var record in owned)
                if ((definitions[record.CharacterKey].Role == UnitRole.MainCharacter) != (record.Role == CharacterOwnershipRole.Main))
                    throw new InvalidOperationException("보유 역할과 정의 역할이 일치해야 합니다.");
            return definitions;
        }

        internal static CharacterDefinitionFacts Read(IUnitTableQuery units, EntityKey key)
        {
            if (units.TryGetUnit(key, out var data) == false)
                return null;

            data.Validate();
            if (data.Key != key) throw new InvalidOperationException("조회한 유닛 정의의 키가 요청과 다릅니다.");
            MainCharacterPersistentState initialMain = data.Role == UnitRole.MainCharacter
                ? new MainCharacterPersistentState(data.MaxHealth, data.MaxHealth,
                    data.AttackPower, data.PhysicalDefense)
                : null;

            return new CharacterDefinitionFacts(data.Key, data.Role, data.OwnerEligibility, initialMain, data.Category);
        }
    }
}
