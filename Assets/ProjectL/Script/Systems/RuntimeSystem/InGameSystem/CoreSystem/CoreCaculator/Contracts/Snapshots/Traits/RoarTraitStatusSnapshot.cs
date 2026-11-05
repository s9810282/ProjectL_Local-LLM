using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>포효의 진행값과 정의에서 가져온 표시용 임계값이다.</summary>
    public sealed class RoarTraitStatusSnapshot : TraitStatusSnapshot
    {
        /// <summary>마지막 포효(또는 전투 시작) 이후 센 밤 피격 횟수다.</summary>
        public int HitCount { get; }

        /// <summary>정의에서 가져온 포효 발동 피격 횟수다.</summary>
        public int HitThreshold { get; }

        public RoarTraitStatusSnapshot(int hitCount, int hitThreshold)
            : base(TraitKey.Roar)
        {
            if (hitCount < 0)
                throw new ArgumentOutOfRangeException(nameof(hitCount));
            if (hitThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(hitThreshold));
            HitCount = hitCount;
            HitThreshold = hitThreshold;
        }

        public override bool SameValues(TraitStatusSnapshot other) =>
            other is RoarTraitStatusSnapshot r && HitCount == r.HitCount && HitThreshold == r.HitThreshold;
    }
}
