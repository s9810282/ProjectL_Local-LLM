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
                        focus.ActiveStacks, focus.GainScheduled, focus.MovedThisTurn));
                }
                else if (state is MoveAttackDiscountRuntimeState discount)
                {
                    statuses.Add(new MoveAttackDiscountTraitStatusSnapshot(
                        discount.FirstMoveConsumed, discount.DiscountArmed));
                }
                else if (state is PiercingFalloffRuntimeState &&
                         traitDefinition is PiercingFalloffTraitDefinition falloff)
                {
                    statuses.Add(new PiercingFalloffTraitStatusSnapshot(
                        falloff.ReductionPercentPerTarget));
                }
                else if (state is ThornFieldRuntimeState &&
                         traitDefinition is ThornFieldTraitDefinition thornField)
                {
                    statuses.Add(new ThornFieldTraitStatusSnapshot(
                        thornField.ThornDamage,
                        thornField.LifetimeUnitTurns));
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
                else
                {
                    throw new InvalidOperationException(
                        $"Trait {key}의 RuntimeState를 변환할 StatusSnapshot이 없습니다.");
                }
            }

            return statuses.ToArray();
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
