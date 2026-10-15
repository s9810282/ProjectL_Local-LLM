using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>카드 한 장의 획득 요청이다. 정의 확인과 ID 발급은 처리·저장 계층이 담당한다.</summary>
    public sealed class RosterAcquireRequest
    {
        public EntityKey CharacterKey { get; }
        public CharacterOwnershipRole Role { get; }

        public RosterAcquireRequest(EntityKey characterKey, CharacterOwnershipRole role)
        {
            if (characterKey == EntityKey.None)
                throw new ArgumentException("캐릭터 정의 키가 필요합니다.", nameof(characterKey));
            if (role != CharacterOwnershipRole.Main && role != CharacterOwnershipRole.Sub)
                throw new ArgumentOutOfRangeException(nameof(role));
            CharacterKey = characterKey;
            Role = role;
        }
    }
}
