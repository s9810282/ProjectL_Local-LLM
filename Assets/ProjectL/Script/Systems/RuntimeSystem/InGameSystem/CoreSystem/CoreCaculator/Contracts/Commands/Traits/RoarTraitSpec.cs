using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>
    /// 보스 포효 정의다. 밤 피격이 임계값에 닿으면 원래 행동 정산 뒤 반응 공격을 한다(실행은 BossRoarCalculator).
    /// 넉백 거리는 보스의 Knockback Trait가 가진다(D4).
    /// </summary>
    public sealed class RoarTraitSpec : TraitSpec
    {
        /// <summary>포효에 필요한 밤 피격 횟수다.</summary>
        public int HitThreshold { get; }

        public RoarTraitSpec(int hitThreshold)
            : base(TraitKey.Roar)
        {
            if (hitThreshold < 1)
                throw new ArgumentOutOfRangeException(nameof(hitThreshold));
            HitThreshold = hitThreshold;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (RoarTraitSpec)other;
            return HitThreshold == otherSpec.HitThreshold;
        }
    }
}
