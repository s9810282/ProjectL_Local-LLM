using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contributions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class MoveAttackDiscountTraitRule :
        ICombatTraitRule, IAttackCostModifier, IMoveSucceededTrait, IAttackSucceededTrait, ITurnStartedTrait, ITurnEndedTrait
    {
        public TraitKey Key => TraitKey.MoveAttackDiscount;

        public AttackCostContribution EvaluateAttackCost(in AttackCostTraitContext context)
        {
            if (context.TraitDefinition is not MoveAttackDiscountTraitDefinition definition)
                throw new ArgumentException(
                    "MoveAttackDiscount 규칙에는 MoveAttackDiscountTraitDefinition이 필요합니다.",
                    nameof(context));
            if (context.TraitRuntimeState is not MoveAttackDiscountRuntimeState runtimeState)
                throw new ArgumentException(
                    "MoveAttackDiscount 규칙에는 MoveAttackDiscountRuntimeState가 필요합니다.",
                    nameof(context));

            if (runtimeState.DiscountArmed && context.BaseAttackCost > definition.MinimumAttackCost)
            {
                return new AttackCostContribution(Key,
                    definition.DiscountAmount,
                    definition.MinimumAttackCost,
                    true);
            }

            return new AttackCostContribution(Key, 0, definition.MinimumAttackCost, false);
        }

        public TraitStateChange PlanAttackSucceeded(in AttackSucceededTraitContext context)
        {
            if (context.TraitRuntimeState is MoveAttackDiscountRuntimeState moveAttackDiscount == false)
                throw new ArgumentException("MoveAttackDiscount 규칙에는 MoveAttackDiscountRuntimeState가 필요합니다.", nameof(context));
            if (context.TraitDefinition is MoveAttackDiscountTraitDefinition definition == false)
                throw new ArgumentException("MoveAttackDiscount 규칙에는 MoveAttackDiscountTraitDefinition이 필요합니다.", nameof(context));

            bool findTraitKey = false;

            for (int i = 0; i < context.PositiveCostContributors.Count; i++)
            {
                if (context.PositiveCostContributors[i] == Key)
                {
                    findTraitKey = true;
                    break;
                }
            }

            if (moveAttackDiscount.DiscountArmed && findTraitKey)
            {
                return TraitStateChange.Change(
                    Key,
                    new MoveAttackDiscountRuntimeState(moveAttackDiscount.FirstMoveConsumed, false),
                    TraitActivationKind.AttackCostReduced,
                    definition.DiscountAmount,
                    null);
            }

            return TraitStateChange.NoChange(Key);
        }

        public TraitStateChange PlanMoveSucceeded(in MoveSucceededTraitContext context)
        {
            if (context.TraitRuntimeState is MoveAttackDiscountRuntimeState moveAttackDiscount == false)
                throw new ArgumentException("MoveAttackDiscount 규칙에는 MoveAttackDiscountRuntimeState가 필요합니다.", nameof(context));
            if (context.TraitDefinition is MoveAttackDiscountTraitDefinition definition == false)
                throw new ArgumentException("MoveAttackDiscount 규칙에는 MoveAttackDiscountTraitDefinition이 필요합니다.", nameof(context));

            if (moveAttackDiscount.FirstMoveConsumed == false &&
                context.AnchorChanged &&
                context.ActionCostSpent > 0)
            {
                return TraitStateChange.Change(
                    Key,
                    new MoveAttackDiscountRuntimeState(true, true),
                    TraitActivationKind.AttackDiscountArmed,
                    definition.DiscountAmount,
                    null);
            }

            return TraitStateChange.NoChange(Key);
        }

        public TraitStateChange PlanTurnEnded(in TurnEndedTraitContext context)
        {
            if (context.TraitRuntimeState is not MoveAttackDiscountRuntimeState runtimeState)
                throw new ArgumentException(
                    "MoveAttackDiscount 규칙에는 MoveAttackDiscountRuntimeState가 필요합니다.",
                    nameof(context));
            if (context.TraitDefinition is not MoveAttackDiscountTraitDefinition definition)
                throw new ArgumentException(
                    "MoveAttackDiscount 규칙에는 MoveAttackDiscountTraitDefinition이 필요합니다.",
                    nameof(context));

            if (runtimeState.DiscountArmed)
            {
                return TraitStateChange.Change(
                    Key,
                    new MoveAttackDiscountRuntimeState(
                        runtimeState.FirstMoveConsumed,
                        false),
                    TraitActivationKind.AttackDiscountExpired,
                    definition.DiscountAmount,
                    null);
            }

            return TraitStateChange.NoChange(Key);
        }

        public TraitStateChange PlanTurnStarted(in TurnStartedTraitContext context)
        {
            if (context.TraitRuntimeState is MoveAttackDiscountRuntimeState moveAttackDiscount == false)
                throw new ArgumentException("MoveAttackDiscount 규칙에는 MoveAttackDiscountRuntimeState가 필요합니다.", nameof(context));

            if (moveAttackDiscount.DiscountArmed == false && moveAttackDiscount.FirstMoveConsumed == false)
                return TraitStateChange.NoChange(Key);

            return TraitStateChange.Change(Key, new MoveAttackDiscountRuntimeState());
        }
    }
}
