using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    public sealed class MoveAttackDiscountTraitSpec : TraitSpec
    {
        public int DiscountAmount { get; }
        public int MinimumAttackCost { get; }

        public MoveAttackDiscountTraitSpec(int discountAmount, int minimumAttackCost)
            :base (TraitKey.MoveAttackDiscount)
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