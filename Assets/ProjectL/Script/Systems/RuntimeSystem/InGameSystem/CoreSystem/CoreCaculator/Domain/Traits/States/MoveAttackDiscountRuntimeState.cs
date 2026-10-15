using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>
    /// 개별 Unit이 자기 턴 동안 보유하는 이동 후 공격 할인 Trait의 현재 상태입니다.
    /// </summary>
    public sealed class MoveAttackDiscountRuntimeState : TraitRuntimeState
    {
        /// <summary>
        /// 이번 자기 턴에 할인을 준비할 수 있는 첫 성공 이동이 이미 사용되었는지를 나타냅니다.
        /// true이면 같은 턴의 이후 이동으로 할인을 다시 준비하지 않습니다.
        /// </summary>
        public bool FirstMoveConsumed { get; private set; }

        /// <summary>
        /// 이동으로 준비된 공격 비용 할인이 아직 사용 가능하게 남아 있는지를 나타냅니다.
        /// true인 상태는 항상 FirstMoveConsumed가 true인 상태를 전제로 합니다.
        /// </summary>
        public bool DiscountArmed { get; private set; }

        public MoveAttackDiscountRuntimeState()
            : this(firstMoveConsumed: false, discountArmed: false)
        {
        }

        public MoveAttackDiscountRuntimeState(
            bool firstMoveConsumed,
            bool discountArmed)
            : base(TraitKey.MoveAttackDiscount)
        {
            if (discountArmed && firstMoveConsumed == false)
            {
                throw new ArgumentException("공격 할인이 준비되어 있다면 첫 이동이 이미 사용된 상태여야 합니다.", nameof(discountArmed));
            }

            FirstMoveConsumed = firstMoveConsumed;
            DiscountArmed = discountArmed;
        }

        internal override TraitRuntimeState Clone()
        {
            return new MoveAttackDiscountRuntimeState(FirstMoveConsumed, DiscountArmed);
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not MoveAttackDiscountRuntimeState discountSource)
            {
                throw new ArgumentException("MoveAttackDiscount 상태에는 같은 Trait의 최종값만 복사할 수 있습니다.", nameof(source));
            }

            FirstMoveConsumed = discountSource.FirstMoveConsumed;
            DiscountArmed = discountSource.DiscountArmed;
        }
    }
}
