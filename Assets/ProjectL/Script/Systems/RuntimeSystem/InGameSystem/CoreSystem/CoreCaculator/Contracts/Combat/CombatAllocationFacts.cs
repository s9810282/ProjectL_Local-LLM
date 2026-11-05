using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 전투 중 새 개체의 ID와 소환 위치 난수 진행값이다. 원본은 CombatCommandState 한 곳이며,
    /// EnvironmentProgressFacts처럼 명령의 원본 비교와 합성 전달을 함께 따른다.
    /// </summary>
    public sealed class CombatAllocationFacts
    {
        /// <summary>할당 원본이 없는 상태다(준비 이전 또는 할당이 필요 없는 테스트 입력). 이 상태에서는 개체를 만들 수 없다.</summary>
        public static CombatAllocationFacts Unavailable { get; } = new CombatAllocationFacts();

        /// <summary>전투 준비가 쓰는 소환 난수 시작값이다. 환경 난수와 같은 규약을 따른다.</summary>
        public const uint InitialSummonRandomState = 1;

        public long NextEntityId { get; }
        public uint SummonRandomState { get; }
        public bool IsAvailable => NextEntityId != 0;

        public CombatAllocationFacts(long nextEntityId, uint summonRandomState)
        {
            if (nextEntityId < 1 || nextEntityId > (long)int.MaxValue + 1)
                throw new ArgumentOutOfRangeException(nameof(nextEntityId));
            NextEntityId = nextEntityId;
            SummonRandomState = summonRandomState;
        }

        private CombatAllocationFacts()
        {
        }

        public bool SameValues(CombatAllocationFacts other) => other != null &&
            NextEntityId == other.NextEntityId && SummonRandomState == other.SummonRandomState;
    }
}
