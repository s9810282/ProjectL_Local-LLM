using System;
using System.Linq;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>기존 Combat 상태에 함께 게시하는 Object 순서·환경 식별자·난수 진행값이다.</summary>
    public sealed class EnvironmentProgressFacts
    {
        public long NextObjectEntryOrdinal { get; }
        public long NextSourceId { get; }
        public uint RandomState { get; }
        public EnvironmentProgressFacts(long nextObjectEntryOrdinal, long nextSourceId = 1, uint randomState = 1)
        {
            if (nextObjectEntryOrdinal < 1) throw new ArgumentOutOfRangeException(nameof(nextObjectEntryOrdinal));
            if (nextSourceId < 1) throw new ArgumentOutOfRangeException(nameof(nextSourceId));
            NextObjectEntryOrdinal = nextObjectEntryOrdinal;
            NextSourceId = nextSourceId;
            RandomState = randomState;
        }
        public bool SameValues(EnvironmentProgressFacts other) => other != null &&
            NextObjectEntryOrdinal == other.NextObjectEntryOrdinal && NextSourceId == other.NextSourceId && RandomState == other.RandomState;
        internal static EnvironmentProgressFacts Initial(MovementEnvironmentFacts environment) =>
            new(checked(environment.Tiles.OfType<ObjectSourceFact>().Select(o => o.EntryOrdinal).DefaultIfEmpty(0).Max() + 1),
                checked(environment.Tiles.Select(o => o.Id).DefaultIfEmpty(0).Max() + 1));
    }
}
