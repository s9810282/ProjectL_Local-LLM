using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class MoveAttackDiscountTraitStatusSnapshot : TraitStatusSnapshot
    {
        public bool FirstMoveConsumed { get; } // 이번 턴의 첫 성공 이동을 이미 사용했는 지
        public bool DiscountArmed { get; } // 다음 공격 할인 효과가 현재 준비되어 있는 지

        public MoveAttackDiscountTraitStatusSnapshot(bool firstMoveConsumed, bool discountArmed)
            :base (global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitKey.MoveAttackDiscount)
        {
            FirstMoveConsumed = firstMoveConsumed;
            DiscountArmed = discountArmed;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is MoveAttackDiscountTraitStatusSnapshot b &&
            FirstMoveConsumed == b.FirstMoveConsumed && DiscountArmed == b.DiscountArmed;
    }
}
