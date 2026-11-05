using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>카드 한 장의 획득 요청이다. 정의 확인과 ID 발급은 처리·저장 계층이 담당한다.</summary>
    public sealed class RosterAcquireRequest
    {
        public EntityKey CharacterKey { get; }
        public CharacterOwnershipRole Role { get; }
        /// <summary>새 보유 개체에 적용할 시작 단계다. 기존 보유 개체의 레벨을 변경하는 요청은 아니다.</summary>
        public int InitialLevel { get; }
        public MainCharacterPersistentState InitialMain { get; }

        public RosterAcquireRequest(EntityKey characterKey, CharacterOwnershipRole role, int initialLevel = 1, MainCharacterPersistentState initialMain = null)
        {
            if (characterKey == EntityKey.None)
                throw new ArgumentException("캐릭터 정의 키가 필요합니다.", nameof(characterKey));
            if (role != CharacterOwnershipRole.Main && role != CharacterOwnershipRole.Sub)
                throw new ArgumentOutOfRangeException(nameof(role));
            if (initialLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(initialLevel));
            // 메인은 레벨업마다 고른 성장 결과를 지속 상태에 쌓으므로 1에서 시작한다. 이후 레벨은 레벨업 처리가 바꾼다.
            if (role == CharacterOwnershipRole.Main && initialLevel != 1)
                throw new ArgumentException("메인의 시작 레벨은 1이어야 합니다.", nameof(initialLevel));
            CharacterKey = characterKey;
            Role = role;
            InitialLevel = initialLevel;
            InitialMain = initialMain;
        }
    }
}
