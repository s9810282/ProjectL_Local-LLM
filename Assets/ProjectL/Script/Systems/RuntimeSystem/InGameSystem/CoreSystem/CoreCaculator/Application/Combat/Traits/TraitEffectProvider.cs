using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contributions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits
{
    internal sealed class TraitEffectProvider
    {
        private readonly TraitRegistry _registry;

        internal TraitEffectProvider(TraitRegistry registry)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        internal TraitStatusSnapshot[] CreateStatuses(
            UnitDefinition definition,
            UnitTraitStateSet stateSet)
        {
            var statuses = new List<TraitStatusSnapshot>();
            foreach (TraitDefinition traitDefinition in definition.TraitDefinitions.Span)
            {
                TraitKey key = traitDefinition.TraitKey;
                TraitRuntimeState state = GetValidatedRuntimeState(
                    traitDefinition,
                    stateSet,
                    _registry.GetRequiredRegistration(key));

                if (state is FocusRuntimeState focus)
                {
                    statuses.Add(new FocusTraitStatusSnapshot(
                        0, false, false));
                }
                else if (state is PiercingFalloffRuntimeState &&
                         traitDefinition is PiercingFalloffTraitDefinition falloff)
                {
                    statuses.Add(new PiercingFalloffTraitStatusSnapshot(
                        falloff.ReductionPercentPerTarget));
                }
                else if (state is KnockbackRuntimeState &&
                         traitDefinition is KnockbackTraitDefinition knockback)
                {
                    statuses.Add(new KnockbackTraitStatusSnapshot(
                        knockback.DistanceCells));
                }
                else if (state is DistanceDamageScaleRuntimeState &&
                         traitDefinition is DistanceDamageScaleTraitDefinition distanceDamageScale)
                {
                    statuses.Add(new DistanceDamageScaleTraitStatusSnapshot(
                        distanceDamageScale.PercentByDistance));
                }
                else if (state is KillChainAttackRuntimeState &&
                         traitDefinition is KillChainAttackTraitDefinition)
                {
                    statuses.Add(new KillChainAttackTraitStatusSnapshot());
                }
                else if (state is NecromancyRuntimeState &&
                         traitDefinition is NecromancyTraitDefinition necromancy)
                {
                    statuses.Add(new NecromancyTraitStatusSnapshot(necromancy.SummonedUnitKey,
                        necromancy.MaxLivingSummons, necromancy.FirstActionDelay));
                }
                else if (state is RoarRuntimeState roar &&
                         traitDefinition is RoarTraitDefinition roarDefinition)
                {
                    statuses.Add(new RoarTraitStatusSnapshot(roar.HitCount, roarDefinition.HitThreshold, roarDefinition.CountPerHit,
                        roarDefinition.CountPerOwnSideTurnEnd, roarDefinition.CountPerOpposingSideTurnEnd));
                }
                else if (state is IntimidationRuntimeState &&
                         traitDefinition is IntimidationTraitDefinition intimidation)
                {
                    statuses.Add(new IntimidationTraitStatusSnapshot(intimidation.AttackReductionPercent));
                }
                else if (state is ImpactBoltRuntimeState && traitDefinition is ImpactBoltTraitDefinition bolt)
                {
                    statuses.Add(new ImpactBoltTraitStatusSnapshot(bolt.DistanceCells));
                }
                else if (state is ThornBombRuntimeState && traitDefinition is ThornBombTraitDefinition bomb)
                {
                    statuses.Add(new ThornBombTraitStatusSnapshot(bomb.DamagePercent));
                }
                else if (state is CrashImpactRuntimeState && traitDefinition is CrashImpactTraitDefinition crash)
                {
                    statuses.Add(new CrashImpactTraitStatusSnapshot(crash.DistanceCells, crash.WallDamagePercent, crash.UnitDamagePercent));
                }
                else if (state is DeadHandRuntimeState && traitDefinition is DeadHandTraitDefinition)
                {
                    statuses.Add(new DeadHandTraitStatusSnapshot());
                }
                else if (state is PullRuntimeState && traitDefinition is PullTraitDefinition pull)
                {
                    statuses.Add(new PullTraitStatusSnapshot(pull.ExtraRangeCells));
                }
                else if (state is HuntersMarkRuntimeState && traitDefinition is HuntersMarkTraitDefinition mark)
                {
                    statuses.Add(new HuntersMarkTraitStatusSnapshot(mark.BonusDamagePercent, mark.DurationAffectedSideTurnEnds));
                }
                else if (state is VanguardRuntimeState && traitDefinition is VanguardTraitDefinition)
                {
                    statuses.Add(new VanguardTraitStatusSnapshot());
                }
                else if (state is CleanSweepRuntimeState && traitDefinition is CleanSweepTraitDefinition)
                {
                    statuses.Add(new CleanSweepTraitStatusSnapshot());
                }
                else if (state is CallOfBloodRuntimeState && traitDefinition is CallOfBloodTraitDefinition)
                {
                    statuses.Add(new CallOfBloodTraitStatusSnapshot());
                }
                else if (state is MoraleBoostRuntimeState && traitDefinition is MoraleBoostTraitDefinition)
                {
                    statuses.Add(new MoraleBoostTraitStatusSnapshot());
                }
                else if (state is DisarmRuntimeState && traitDefinition is DisarmTraitDefinition)
                {
                    statuses.Add(new DisarmTraitStatusSnapshot());
                }
                else if (state is AnkleBreakerRuntimeState && traitDefinition is AnkleBreakerTraitDefinition)
                {
                    statuses.Add(new AnkleBreakerTraitStatusSnapshot());
                }
                else if (state is BleedingRuntimeState && traitDefinition is BleedingTraitDefinition)
                {
                    statuses.Add(new BleedingTraitStatusSnapshot());
                }
                else if (state is FearRuntimeState && traitDefinition is FearTraitDefinition)
                {
                    statuses.Add(new FearTraitStatusSnapshot());
                }
                else if (state is SelfDestructRuntimeState && traitDefinition is SelfDestructTraitDefinition selfDestruct)
                {
                    statuses.Add(new SelfDestructTraitStatusSnapshot(selfDestruct.Level));
                }
                else if (state is ReconstructionRuntimeState reconstructionState && traitDefinition is ReconstructionTraitDefinition reconstruction)
                {
                    statuses.Add(new ReconstructionTraitStatusSnapshot(reconstruction.Level, reconstructionState.Used));
                }
                else if (state is RampageRuntimeState rampageState && traitDefinition is RampageTraitDefinition rampage)
                {
                    statuses.Add(new RampageTraitStatusSnapshot(rampage.BaseAttackBonus, rampage.RankAttackBonus,
                        rampage.MovementBonus, rampageState.KillAttackBonus));
                }
                else if (state is LavaCannonRuntimeState && traitDefinition is LavaCannonTraitDefinition)
                {
                    statuses.Add(new LavaCannonTraitStatusSnapshot());
                }
                else if (state is SoloActionRuntimeState && traitDefinition is SoloActionTraitDefinition)
                {
                    statuses.Add(new SoloActionTraitStatusSnapshot());
                }
                else if (state is CohesionRuntimeState && traitDefinition is CohesionTraitDefinition)
                {
                    statuses.Add(new CohesionTraitStatusSnapshot());
                }
                else if (state is EntrenchmentRuntimeState && traitDefinition is EntrenchmentTraitDefinition)
                {
                    statuses.Add(new EntrenchmentTraitStatusSnapshot());
                }
                else if (state is SacrificeRuntimeState && traitDefinition is SacrificeTraitDefinition)
                {
                    statuses.Add(new SacrificeTraitStatusSnapshot());
                }
                else if (state is TauntRuntimeState && traitDefinition is TauntTraitDefinition)
                {
                    statuses.Add(new TauntTraitStatusSnapshot());
                }
                else if (state is SteadfastWillRuntimeState && traitDefinition is SteadfastWillTraitDefinition)
                {
                    statuses.Add(new SteadfastWillTraitStatusSnapshot());
                }
                else if (state is GroundCompactionRuntimeState && traitDefinition is GroundCompactionTraitDefinition)
                {
                    statuses.Add(new GroundCompactionTraitStatusSnapshot());
                }
                else if (state is ReconSquadRuntimeState && traitDefinition is ReconSquadTraitDefinition recon)
                {
                    statuses.Add(new ReconSquadTraitStatusSnapshot(recon.IncreasePercent, recon.Radius));
                }
                else if (state is DustScreenRuntimeState && traitDefinition is DustScreenTraitDefinition)
                {
                    statuses.Add(new DustScreenTraitStatusSnapshot());
                }
                else if (state is PositionSwapRuntimeState && traitDefinition is PositionSwapTraitDefinition)
                {
                    statuses.Add(new PositionSwapTraitStatusSnapshot());
                }
                else if (state is LeapRuntimeState && traitDefinition is LeapTraitDefinition)
                {
                    statuses.Add(new LeapTraitStatusSnapshot());
                }
                else if (state is AccelerationRuntimeState acceleration && traitDefinition is AccelerationTraitDefinition)
                {
                    statuses.Add(new AccelerationTraitStatusSnapshot(acceleration.HeldAttackBonus));
                }
                else if (state is StationaryFireRuntimeState stationaryfire && traitDefinition is StationaryFireTraitDefinition)
                {
                    statuses.Add(new StationaryFireTraitStatusSnapshot(stationaryfire.MovedThisOwnTurn));
                }
                else if (state is ArmorBreakerRuntimeState && traitDefinition is ArmorBreakerTraitDefinition)
                {
                    statuses.Add(new ArmorBreakerTraitStatusSnapshot());
                }
                else if (state is FinishingShotRuntimeState && traitDefinition is FinishingShotTraitDefinition)
                {
                    statuses.Add(new FinishingShotTraitStatusSnapshot());
                }
                else if (state is TrackingShotRuntimeState && traitDefinition is TrackingShotTraitDefinition)
                {
                    statuses.Add(new TrackingShotTraitStatusSnapshot());
                }
                else if (state is FocusedBombardmentRuntimeState && traitDefinition is FocusedBombardmentTraitDefinition)
                {
                    statuses.Add(new FocusedBombardmentTraitStatusSnapshot());
                }
                else if (state is HunterRuntimeState hunter && traitDefinition is HunterTraitDefinition)
                {
                    statuses.Add(new HunterTraitStatusSnapshot(hunter.TargetId, hunter.SuccessfulRewards));
                }
                else if (state is RegenerationRuntimeState regenerationState &&
                         traitDefinition is RegenerationTraitDefinition regeneration)
                {
                    statuses.Add(new RegenerationTraitStatusSnapshot(regeneration.HealAmount, regeneration.HealEveryOwnSideTurnEnds, regenerationState.CompletedEligibleTurnEnds));
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Trait {key}의 RuntimeState를 변환할 StatusSnapshot이 없습니다.");
                }
            }

            return statuses.ConvertAll(status => status.WithAvailability(
                DeferredCombatFeatures.GetTraitUnavailableReason(status.Key),
                DeferredCombatFeatures.GetTraitPartialUnavailableReason(status.Key))).ToArray();
        }

        private static TraitRuntimeState GetValidatedRuntimeState(
            TraitDefinition traitDefinition,
            UnitTraitStateSet stateSet,
            TraitRegistration registration)
        {
            if (registration.DefinitionType.IsInstanceOfType(traitDefinition) == false)
                throw new InvalidOperationException(
                    $"Trait {traitDefinition.TraitKey}의 Definition 타입이 Registration과 일치하지 않습니다.");
            if (stateSet.TryGetRuntimeState(traitDefinition.TraitKey, out TraitRuntimeState state) == false)
                throw new InvalidOperationException(
                    $"Trait {traitDefinition.TraitKey}의 RuntimeState를 찾을 수 없습니다.");
            if (registration.RuntimeStateType.IsInstanceOfType(state) == false)
                throw new InvalidOperationException(
                    $"Trait {traitDefinition.TraitKey}의 RuntimeState 타입이 Registration과 일치하지 않습니다.");
            return state;
        }
    }
}
