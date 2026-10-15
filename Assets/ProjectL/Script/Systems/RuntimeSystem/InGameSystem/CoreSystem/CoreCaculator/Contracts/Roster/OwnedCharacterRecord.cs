using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public sealed class OwnedCharacterRecord
    {
        public CharacterInstanceId Id { get; }
        public EntityKey CharacterKey { get; }
        public CharacterOwnershipRole Role { get; }

        public OwnedCharacterRecord(CharacterInstanceId id, EntityKey characterKey, CharacterOwnershipRole role)
        {
            if (id.IsEmpty)
                throw new ArgumentException("보유 ID는 비어 있을 수 없습니다.", nameof(id));
            if (characterKey == EntityKey.None)
                throw new ArgumentException("캐릭터 정의 키가 필요합니다.", nameof(characterKey));
            if (role != CharacterOwnershipRole.Main && role != CharacterOwnershipRole.Sub)
                throw new ArgumentOutOfRangeException(nameof(role));
            Id = id;
            CharacterKey = characterKey;
            Role = role;
        }
    }
}
