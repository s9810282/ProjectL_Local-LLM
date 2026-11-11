using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>
    /// 보스 포효 정의다. 밤 피격과 진영 턴 종료로 쌓인 카운터가 임계값에 닿으면 원래 행동 정산 뒤 반응 공격을 한다(실행은 BossRoarCalculator).
    /// 넉백 거리는 보스의 Knockback Trait가 가진다(D4).
    /// </summary>
    public sealed class RoarTraitSpec : TraitSpec
    {
        /// <summary>포효에 필요한 카운터 값이다. 이름은 옛 "밤 피격 횟수" 그대로 둔다.</summary>
        public int HitThreshold { get; }

        /// <summary>밤 피격 1회마다 카운터에 더하는 양이다. 0이면 피격으로는 쌓이지 않는다.</summary>
        public int CountPerHit { get; }

        /// <summary>소유자 진영 턴이 끝날 때마다 카운터에 더하는 양이다. 0이면 쌓이지 않는다.</summary>
        public int CountPerOwnSideTurnEnd { get; }

        /// <summary>상대 진영 턴이 끝날 때마다 카운터에 더하는 양이다. 0이면 쌓이지 않는다.</summary>
        public int CountPerOpposingSideTurnEnd { get; }

        /// <remarks>기본값(피격 1, 턴 종료 0)은 턴 종료 카운트를 넣기 전 규칙과 같다.</remarks>
        public RoarTraitSpec(int hitThreshold, int countPerHit = 1, int countPerOwnSideTurnEnd = 0, int countPerOpposingSideTurnEnd = 0)
            : base(TraitKey.Roar)
        {
            RoarCountRules.RequireDefinition(hitThreshold, countPerHit, countPerOwnSideTurnEnd, countPerOpposingSideTurnEnd);
            HitThreshold = hitThreshold;
            CountPerHit = countPerHit;
            CountPerOwnSideTurnEnd = countPerOwnSideTurnEnd;
            CountPerOpposingSideTurnEnd = countPerOpposingSideTurnEnd;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (RoarTraitSpec)other;
            return HitThreshold == otherSpec.HitThreshold && CountPerHit == otherSpec.CountPerHit &&
                CountPerOwnSideTurnEnd == otherSpec.CountPerOwnSideTurnEnd &&
                CountPerOpposingSideTurnEnd == otherSpec.CountPerOpposingSideTurnEnd;
        }
    }
}
