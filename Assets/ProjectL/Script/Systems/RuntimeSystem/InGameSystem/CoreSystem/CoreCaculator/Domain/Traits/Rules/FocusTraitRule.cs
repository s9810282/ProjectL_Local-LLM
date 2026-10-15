using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contributions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class FocusTraitRule : ICombatTraitRule, IAttackDamageModifier, IMoveSucceededTrait, IAttackSucceededTrait, ITurnStartedTrait
    {
        public TraitKey Key => TraitKey.Focus;

        public AttackDamageContribution EvaluateAttackDamage(in AttackDamageTraitContext context)
        {
            if (context.TraitDefinition is not FocusTraitDefinition definition)
                throw new ArgumentException(
                    "Focus 규칙에는 FocusTraitDefinition이 필요합니다.",
                    nameof(context));
            if (context.TraitRuntimeState is not FocusRuntimeState runtimeState)
                throw new ArgumentException(
                    "Focus 규칙에는 FocusRuntimeState가 필요합니다.",
                    nameof(context));

            int damagePerStack = definition.DamagePerStack;
            int activeStacks = runtimeState.ActiveStacks;
            int flatBonus = checked(damagePerStack * activeStacks);

            return new AttackDamageContribution(Key, flatBonus);
        }

        public TraitStateChange PlanAttackSucceeded(in AttackSucceededTraitContext context)
        {
            if (context.TraitRuntimeState is FocusRuntimeState focusRuntime == false)
                throw new ArgumentException(
                    "Focus 규칙에는 FocusRuntimeState가 필요합니다.",
                    nameof(context));
            if (context.TraitDefinition is FocusTraitDefinition focusTraitDefinition == false)
                throw new ArgumentException(
                    "Focus 규칙에는 FocusTraitDefinition이 필요합니다.",
                    nameof(context));

            if (focusRuntime.MovedThisTurn == false &&
                focusRuntime.GainScheduled == false &&
                focusRuntime.ActiveStacks < focusTraitDefinition.MaxStacks)
            {
                return TraitStateChange.Change(
                    Key,
                    new FocusRuntimeState(focusRuntime.ActiveStacks, true, false),
                    TraitActivationKind.FocusGainScheduled,
                    1,
                    null);
            }

            return TraitStateChange.NoChange(Key);
        }

        public TraitStateChange PlanMoveSucceeded(in MoveSucceededTraitContext context)
        {
            if (context.TraitRuntimeState is FocusRuntimeState focusRuntime == false)
            {
                throw new ArgumentException(
                    "Focus 규칙에는 FocusRuntimeState가 필요합니다.",
                    nameof(context));
            }

            bool removedFocus = focusRuntime.ActiveStacks > 0 || focusRuntime.GainScheduled;
            bool hasChange = removedFocus || focusRuntime.MovedThisTurn == false;

            if (hasChange == false)
                return TraitStateChange.NoChange(Key);

            FocusRuntimeState finalState = new FocusRuntimeState(0, false, true);
            if (removedFocus)
            {
                return TraitStateChange.Change(
                    Key,
                    finalState,
                    TraitActivationKind.FocusReset,
                    focusRuntime.ActiveStacks,
                    null);
            }

            return TraitStateChange.Change(Key, finalState);
        }

        public TraitStateChange PlanTurnStarted(in TurnStartedTraitContext context)
        {
            if (context.TraitRuntimeState is FocusRuntimeState focusRuntime == false)
                throw new ArgumentException(
                    "Focus 규칙에는 FocusRuntimeState가 필요합니다.",
                    nameof(context));
            if (context.TraitDefinition is FocusTraitDefinition focusTraitDefinition == false)
                throw new ArgumentException(
                    "Focus 규칙에는 FocusTraitDefinition이 필요합니다.",
                    nameof(context));

            bool stackIncreased = focusRuntime.GainScheduled && focusRuntime.ActiveStacks < focusTraitDefinition.MaxStacks;

            int nextStack = 0;
            if (stackIncreased)
                nextStack = checked(focusRuntime.ActiveStacks + 1);
            else
                nextStack = focusRuntime.ActiveStacks;

            bool hasChange = nextStack != focusRuntime.ActiveStacks || focusRuntime.GainScheduled || focusRuntime.MovedThisTurn;

            if (hasChange == false)
                return TraitStateChange.NoChange(Key);

            FocusRuntimeState finalState = new FocusRuntimeState(nextStack, false, false);
            if (stackIncreased)
            {
                return TraitStateChange.Change(
                    Key,
                    finalState,
                    TraitActivationKind.FocusStackApplied,
                    nextStack - focusRuntime.ActiveStacks,
                    null);
            }

            return TraitStateChange.Change(Key, finalState);
        }
    }
}
