using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>종류가 같은 카드도 보유 ID로 구별하는 드로우 후보이다.</summary>
    public sealed class StageDrawCandidate
    {
        public CharacterInstanceId Id { get; }
        public EntityKey CharacterKey { get; }

        public StageDrawCandidate(CharacterInstanceId id, EntityKey characterKey)
        {
            if (id.IsEmpty)
                throw new ArgumentException("드로우 후보의 보유 ID가 필요합니다.", nameof(id));
            if (characterKey == EntityKey.None)
                throw new ArgumentException("캐릭터 정의 키가 필요합니다.", nameof(characterKey));
            Id = id;
            CharacterKey = characterKey;
        }
    }
}
