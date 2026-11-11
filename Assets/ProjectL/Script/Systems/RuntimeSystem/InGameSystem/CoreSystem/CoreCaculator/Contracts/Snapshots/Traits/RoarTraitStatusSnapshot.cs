using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>포효의 진행값과 정의에서 가져온 임계값·증가량이다. 증가량은 카운터를 쌓는 계산이 이 값에서 읽는다.</summary>
    public sealed class RoarTraitStatusSnapshot : TraitStatusSnapshot
    {
        /// <summary>마지막 포효(또는 전투 시작) 이후 쌓인 카운터다. 밤 피격과 진영 턴 종료로 오른다. 이름은 옛 "밤 피격 횟수" 그대로 둔다.</summary>
        public int HitCount { get; }

        /// <summary>정의에서 가져온 포효 발동 카운터 값이다.</summary>
        public int HitThreshold { get; }

        /// <summary>밤 피격 1회마다 더하는 양이다.</summary>
        public int CountPerHit { get; }

        /// <summary>소유자 진영 턴 종료마다 더하는 양이다.</summary>
        public int CountPerOwnSideTurnEnd { get; }

        /// <summary>상대 진영 턴 종료마다 더하는 양이다.</summary>
        public int CountPerOpposingSideTurnEnd { get; }

        /// <remarks>증가량 기본값(피격 1, 턴 종료 0)은 턴 종료 카운트를 넣기 전 규칙과 같다.</remarks>
        public RoarTraitStatusSnapshot(int hitCount, int hitThreshold, int countPerHit = 1, int countPerOwnSideTurnEnd = 0,
            int countPerOpposingSideTurnEnd = 0)
            : base(TraitKey.Roar)
        {
            if (hitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(hitCount));
            if (hitThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(hitThreshold));
            if (countPerHit < 0)
                throw new ArgumentOutOfRangeException(nameof(countPerHit));
            if (countPerOwnSideTurnEnd < 0)
                throw new ArgumentOutOfRangeException(nameof(countPerOwnSideTurnEnd));
            if (countPerOpposingSideTurnEnd < 0)
                throw new ArgumentOutOfRangeException(nameof(countPerOpposingSideTurnEnd));
            HitCount = hitCount;
            HitThreshold = hitThreshold;
            CountPerHit = countPerHit;
            CountPerOwnSideTurnEnd = countPerOwnSideTurnEnd;
            CountPerOpposingSideTurnEnd = countPerOpposingSideTurnEnd;
        }

        /// <summary>카운터만 바꾼 사본이다. 임계값·증가량과 사용 가능 사유는 그대로 둔다.</summary>
        public RoarTraitStatusSnapshot WithHitCount(int hitCount)
        {
            if (hitCount == HitCount) return this;
            return (RoarTraitStatusSnapshot)new RoarTraitStatusSnapshot(hitCount, HitThreshold, CountPerHit,
                CountPerOwnSideTurnEnd, CountPerOpposingSideTurnEnd).WithAvailability(UnavailableReason, PartialUnavailableReason);
        }

        /// <summary>진영 턴이 끝날 때 더하는 양이다. 소유자 진영이 끝났으면 자기 진영 값, 아니면 상대 진영 값이다.</summary>
        public int CountPerTurnEnd(bool ownSideEnded) => ownSideEnded ? CountPerOwnSideTurnEnd : CountPerOpposingSideTurnEnd;

        public override bool SameValues(TraitStatusSnapshot other) =>
            other is RoarTraitStatusSnapshot r && HitCount == r.HitCount && HitThreshold == r.HitThreshold &&
            CountPerHit == r.CountPerHit && CountPerOwnSideTurnEnd == r.CountPerOwnSideTurnEnd &&
            CountPerOpposingSideTurnEnd == r.CountPerOpposingSideTurnEnd;
    }
}
