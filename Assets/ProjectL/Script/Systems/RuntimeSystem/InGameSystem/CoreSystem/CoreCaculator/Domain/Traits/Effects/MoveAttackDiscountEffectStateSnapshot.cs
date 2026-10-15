using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class MoveAttackDiscountEffectStateSnapshot : IEffectStateSnapshot
    {
        public bool FirstMoveConsumed { get; }
        public bool DiscountArmed { get; }

        internal MoveAttackDiscountEffectStateSnapshot(bool firstMoveConsumed, bool discountArmed)
        {
            if (discountArmed && firstMoveConsumed == false)
                throw new ArgumentException(
                    "An armed attack discount requires the first move to be consumed.",
                    nameof(discountArmed));

            FirstMoveConsumed = firstMoveConsumed;
            DiscountArmed = discountArmed;
        }

        public bool ValueEquals(IEffectStateSnapshot other)
        {
            return other is MoveAttackDiscountEffectStateSnapshot snapshot &&
                   snapshot.FirstMoveConsumed == FirstMoveConsumed &&
                   snapshot.DiscountArmed == DiscountArmed;
        }
    }
}
