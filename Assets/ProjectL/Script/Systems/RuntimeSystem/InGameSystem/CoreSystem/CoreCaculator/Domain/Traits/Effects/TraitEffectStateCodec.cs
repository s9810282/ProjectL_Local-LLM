using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal static class TraitEffectStateCodec
    {
        internal static IEffectStateSnapshot Capture(TraitRuntimeState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            if (state is FocusRuntimeState focus)
            {
                return new FocusEffectStateSnapshot(
                    focus.ActiveStacks,
                    focus.GainScheduled,
                    focus.MovedThisTurn);
            }

            if (state is MoveAttackDiscountRuntimeState discount)
            {
                return new MoveAttackDiscountEffectStateSnapshot(
                    discount.FirstMoveConsumed,
                    discount.DiscountArmed);
            }

            if (state is PiercingFalloffRuntimeState)
                return new PiercingFalloffEffectStateSnapshot();

            if (state is ThornFieldRuntimeState)
                return new ThornFieldEffectStateSnapshot();

            if (state is KnockbackRuntimeState)
                return new KnockbackEffectStateSnapshot();

            if (state is DistanceDamageScaleRuntimeState)
                return new DistanceDamageScaleEffectStateSnapshot();

            if (state is KillChainAttackRuntimeState)
                return new KillChainAttackEffectStateSnapshot();

            throw new ArgumentException("Unsupported Trait runtime state", nameof(state));
        }

        internal static TraitRuntimeState Restore(IEffectStateSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            if (snapshot is FocusEffectStateSnapshot focus)
            {
                return new FocusRuntimeState(
                    focus.ActiveStacks,
                    focus.GainScheduled,
                    focus.MovedThisTurn);
            }

            if (snapshot is MoveAttackDiscountEffectStateSnapshot discount)
            {
                return new MoveAttackDiscountRuntimeState(
                    discount.FirstMoveConsumed,
                    discount.DiscountArmed);
            }

            if (snapshot is PiercingFalloffEffectStateSnapshot)
                return new PiercingFalloffRuntimeState();

            if (snapshot is ThornFieldEffectStateSnapshot)
                return new ThornFieldRuntimeState();

            if (snapshot is KnockbackEffectStateSnapshot)
                return new KnockbackRuntimeState();

            if (snapshot is DistanceDamageScaleEffectStateSnapshot)
                return new DistanceDamageScaleRuntimeState();

            if (snapshot is KillChainAttackEffectStateSnapshot)
                return new KillChainAttackRuntimeState();

            throw new ArgumentException("Unsupported Trait snapshot", nameof(snapshot));
        }
    }
}
