using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>로드아웃의 작성 기본 메인과 선택 가능한 메인 키를 담는 읽기 전용 응답이다.</summary>
    public sealed class RunStartMainOptions
    {
        public int LoadoutKey { get; }
        public EntityKey DefaultMainCharacterKey { get; }
        public IReadOnlyList<EntityKey> CandidateKeys { get; }

        public RunStartMainOptions(int loadoutKey, EntityKey defaultMainCharacterKey,
            IReadOnlyList<EntityKey> candidateKeys)
        {
            if (candidateKeys == null)
                throw new ArgumentNullException(nameof(candidateKeys));

            LoadoutKey = loadoutKey;
            DefaultMainCharacterKey = defaultMainCharacterKey;
            CandidateKeys = Array.AsReadOnly(candidateKeys.ToArray());
        }
    }
}
