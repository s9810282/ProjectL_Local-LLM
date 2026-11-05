using System;
using System.Linq;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>기존 Combat 상태에 함께 게시하는 Object 순서·환경 식별자·난수 진행값이다.</summary>
    public sealed class EnvironmentProgressFacts
    {
        /// <summary>최초 무작위 배치가 없는 전투 준비의 환경 난수 시작값이다.</summary>
        public const uint InitialRandomState = 1;

        public long NextObjectEntryOrdinal { get; }
        public long NextSourceId { get; }
        public uint RandomState { get; }
        public EnvironmentProgressFacts(long nextObjectEntryOrdinal, long nextSourceId = 1, uint randomState = InitialRandomState)
        {
            if (nextObjectEntryOrdinal < 1) throw new ArgumentOutOfRangeException(nameof(nextObjectEntryOrdinal));
            if (nextSourceId < 1) throw new ArgumentOutOfRangeException(nameof(nextSourceId));
            NextObjectEntryOrdinal = nextObjectEntryOrdinal;
            NextSourceId = nextSourceId;
            RandomState = randomState;
        }
        public bool SameValues(EnvironmentProgressFacts other) => other != null &&
            NextObjectEntryOrdinal == other.NextObjectEntryOrdinal && NextSourceId == other.NextSourceId && RandomState == other.RandomState;

        /// <summary>초기 환경의 다음 Object 순서·원본 ID를 계산한다. 난수 시작값만 호출자가 정할 수 있다.</summary>
        public static EnvironmentProgressFacts Initial(MovementEnvironmentFacts environment, uint randomState = InitialRandomState)
        {
            if (environment == null) throw new ArgumentNullException(nameof(environment));
            return new(checked(environment.Tiles.OfType<ObjectSourceFact>().Select(o => o.EntryOrdinal).DefaultIfEmpty(0).Max() + 1),
                checked(environment.Tiles.Select(o => o.Id).DefaultIfEmpty(0).Max() + 1), randomState);
        }
    }
}
