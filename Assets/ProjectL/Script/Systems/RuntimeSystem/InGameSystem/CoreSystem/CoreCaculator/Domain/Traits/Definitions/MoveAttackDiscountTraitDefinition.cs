using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>
    /// 이동 후 공격 할인 Trait가 사용하는 변경되지 않는 비용 설정입니다.
    /// </summary>
    public sealed class MoveAttackDiscountTraitDefinition : TraitDefinition
    {
        /// <summary>
        /// 할인이 준비된 공격에서 요청하는 고정 공격 비용 감소량입니다.
        /// 최종 비용은 MinimumAttackCost보다 낮아지지 않도록 이후 계산 단계에서 제한합니다.
        /// </summary>
        public int DiscountAmount { get; }

        /// <summary>
        /// 이 Trait의 할인 Contribution을 적용해도 내려갈 수 없는 최소 공격 비용입니다.
        /// </summary>
        public int MinimumAttackCost { get; }

        public MoveAttackDiscountTraitDefinition(int discountAmount, int minimumAttackCost)
            : base(TraitKey.MoveAttackDiscount)
        {
            if (discountAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(discountAmount));
            if (minimumAttackCost <= 0)
                throw new ArgumentOutOfRangeException(nameof(minimumAttackCost));

            DiscountAmount = discountAmount;
            MinimumAttackCost = minimumAttackCost;
        }
    }
}
