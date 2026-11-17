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
            if (state is RampageRuntimeState rampage) return new RampageEffectStateSnapshot(rampage.KillAttackBonus);

            if (state is AccelerationRuntimeState acceleration) return new AccelerationEffectStateSnapshot(acceleration.HeldAttackBonus);
            if (state is StationaryFireRuntimeState stationaryfire) return new StationaryFireEffectStateSnapshot(stationaryfire.MovedThisOwnTurn);
            if (state is HunterRuntimeState hunter) return new HunterEffectStateSnapshot(hunter.TargetId, hunter.SuccessfulRewards);
            if (state is FocusRuntimeState focus)
            {
                return new FocusEffectStateSnapshot(
                    focus.ActiveStacks,
                    focus.GainScheduled,
                    focus.MovedThisTurn);
            }

            if (state is PiercingFalloffRuntimeState)
                return new PiercingFalloffEffectStateSnapshot();

            if (state is KnockbackRuntimeState)
                return new KnockbackEffectStateSnapshot();

            if (state is DistanceDamageScaleRuntimeState)
                return new DistanceDamageScaleEffectStateSnapshot();

            if (state is KillChainAttackRuntimeState)
                return new KillChainAttackEffectStateSnapshot();

            if (state is NecromancyRuntimeState)
                return new NecromancyEffectStateSnapshot();

            if (state is RoarRuntimeState roar)
                return new RoarEffectStateSnapshot(roar.HitCount);


            if (state is IntimidationRuntimeState)
                return new IntimidationEffectStateSnapshot();

            if (state is ImpactBoltRuntimeState)
                return new ImpactBoltEffectStateSnapshot();

            if (state is ThornBombRuntimeState)
                return StatelessEffectStateSnapshot.Instance;

            if (state is CrashImpactRuntimeState)
                return new CrashImpactEffectStateSnapshot();

            if (state is DeadHandRuntimeState) return new DeadHandEffectStateSnapshot();

            if (state is PullRuntimeState)
                return new PullEffectStateSnapshot();

            if (state is HuntersMarkRuntimeState)
                return new HuntersMarkEffectStateSnapshot();







            if (state is RegenerationRuntimeState regeneration)
                return new RegenerationEffectStateSnapshot(regeneration.CompletedEligibleTurnEnds);

            throw new ArgumentException("Unsupported Trait runtime state", nameof(state));
        }

        internal static TraitRuntimeState Restore(IEffectStateSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot is RampageEffectStateSnapshot rampage) return new RampageRuntimeState(rampage.KillAttackBonus);

            if (snapshot is AccelerationEffectStateSnapshot acceleration) return new AccelerationRuntimeState(acceleration.HeldAttackBonus);
            if (snapshot is StationaryFireEffectStateSnapshot stationaryfire) return new StationaryFireRuntimeState(stationaryfire.MovedThisOwnTurn);
            if (snapshot is HunterEffectStateSnapshot hunter) return new HunterRuntimeState(hunter.TargetId, hunter.SuccessfulRewards);
            if (snapshot is FocusEffectStateSnapshot focus)
            {
                return new FocusRuntimeState(
                    focus.ActiveStacks,
                    focus.GainScheduled,
                    focus.MovedThisTurn);
            }

            if (snapshot is PiercingFalloffEffectStateSnapshot)
                return new PiercingFalloffRuntimeState();

            if (snapshot is KnockbackEffectStateSnapshot)
                return new KnockbackRuntimeState();

            if (snapshot is DistanceDamageScaleEffectStateSnapshot)
                return new DistanceDamageScaleRuntimeState();

            if (snapshot is KillChainAttackEffectStateSnapshot)
                return new KillChainAttackRuntimeState();

            if (snapshot is NecromancyEffectStateSnapshot)
                return new NecromancyRuntimeState();

            if (snapshot is RoarEffectStateSnapshot roar)
                return new RoarRuntimeState(roar.HitCount);


            if (snapshot is IntimidationEffectStateSnapshot)
                return new IntimidationRuntimeState();

            if (snapshot is ImpactBoltEffectStateSnapshot)
                return new ImpactBoltRuntimeState();

            if (snapshot is CrashImpactEffectStateSnapshot)
                return new CrashImpactRuntimeState();

            if (snapshot is DeadHandEffectStateSnapshot) return new DeadHandRuntimeState();

            if (snapshot is PullEffectStateSnapshot)
                return new PullRuntimeState();

            if (snapshot is HuntersMarkEffectStateSnapshot)
                return new HuntersMarkRuntimeState();







            if (snapshot is RegenerationEffectStateSnapshot regeneration)
                return new RegenerationRuntimeState(regeneration.CompletedEligibleTurnEnds);

            throw new ArgumentException("Unsupported Trait snapshot", nameof(snapshot));
        }
    }
}
