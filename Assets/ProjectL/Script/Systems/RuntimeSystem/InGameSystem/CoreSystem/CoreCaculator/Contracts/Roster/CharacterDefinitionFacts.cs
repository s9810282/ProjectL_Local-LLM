using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>획득 규칙에 필요한 정의 값만 전달한다.</summary>
    public sealed class CharacterDefinitionFacts
    {
        public EntityKey Key { get; }
        public UnitRole Role { get; }
        public UnitOwnerEligibility OwnerEligibility { get; }
        public MainCharacterPersistentState InitialMain { get; }

        public CharacterDefinitionFacts(EntityKey key, UnitRole role,
            UnitOwnerEligibility ownerEligibility, MainCharacterPersistentState initialMain = null)
        {
            if (key == EntityKey.None)
                throw new ArgumentException("캐릭터 정의 키가 필요합니다.", nameof(key));
            if (Enum.IsDefined(typeof(UnitRole), role) == false)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (Enum.IsDefined(typeof(UnitOwnerEligibility), ownerEligibility) == false)
                throw new ArgumentOutOfRangeException(nameof(ownerEligibility));

            Key = key;
            Role = role;
            OwnerEligibility = ownerEligibility;
            InitialMain = initialMain;
        }
    }
}
