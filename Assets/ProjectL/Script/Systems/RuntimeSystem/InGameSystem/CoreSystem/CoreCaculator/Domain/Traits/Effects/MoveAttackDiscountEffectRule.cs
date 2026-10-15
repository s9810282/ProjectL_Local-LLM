using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Contributions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class MoveAttackDiscountEffectRule :
        IEffectAttackCostRule,
        IEffectMoveSucceededRule,
        IEffectAttackSucceededRule,
        IEffectTurnStartedRule,
        IEffectTurnEndedRule
    {
        private readonly MoveAttackDiscountTraitDefinition _definition;
        private readonly MoveAttackDiscountTraitRule _rule;

        internal MoveAttackDiscountEffectRule(
            MoveAttackDiscountTraitDefinition definition,
            MoveAttackDiscountTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public EffectAttackCostContribution EvaluateAttackCost(
            in EffectRuleInput input,
            in AttackCostEffectContext context)
        {
            MoveAttackDiscountRuntimeState state = RestoreRequiredState(input);
            AttackCostContribution result = _rule.EvaluateAttackCost(
                new AttackCostTraitContext(_definition, state, context.BaseAttackCost));
            ValidateResultSource(result.Source);
            return new EffectAttackCostContribution(
                input.Origin,
                result.FlatReduction,
                result.MinimumCost,
                result.ConsumeOnSuccessfulAttack);
        }

        public EffectRuleResult PlanMoveSucceeded(
            in EffectRuleInput input,
            in MoveSucceededEffectContext context)
        {
            MoveAttackDiscountRuntimeState state = RestoreRequiredState(input);
            TraitStateChange change = _rule.PlanMoveSucceeded(new MoveSucceededTraitContext(
                _definition,
                state,
                context.Distance,
                context.AnchorChanged,
                context.ActionCostSpent));
            return TraitEffectResultAdapter.Convert(input, change);
        }

        public EffectRuleResult PlanAttackSucceeded(
            in EffectRuleInput input,
            in AttackSucceededEffectContext context)
        {
            MoveAttackDiscountRuntimeState state = RestoreRequiredState(input);
            TraitKey[] contributors = TraitEffectOrigin.GetTraitContributors(
                input.Origin,
                context.PositiveCostContributors);
            TraitStateChange change = _rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(_definition, state, contributors));
            return TraitEffectResultAdapter.Convert(input, change);
        }

        public EffectRuleResult PlanTurnStarted(in EffectRuleInput input)
        {
            MoveAttackDiscountRuntimeState state = RestoreRequiredState(input);
            TraitStateChange change = _rule.PlanTurnStarted(
                new TurnStartedTraitContext(_definition, state));
            return TraitEffectResultAdapter.Convert(input, change);
        }

        public EffectRuleResult PlanTurnEnded(in EffectRuleInput input)
        {
            MoveAttackDiscountRuntimeState state = RestoreRequiredState(input);
            TraitStateChange change = _rule.PlanTurnEnded(
                new TurnEndedTraitContext(_definition, state));
            return TraitEffectResultAdapter.Convert(input, change);
        }

        private MoveAttackDiscountRuntimeState RestoreRequiredState(in EffectRuleInput input)
        {
            ValidateBoundOrigin(input.Origin);
            if (input.State is not MoveAttackDiscountEffectStateSnapshot)
                throw new ArgumentException(
                    "MoveAttackDiscount effect requires a MoveAttackDiscount snapshot.",
                    nameof(input));
            return (MoveAttackDiscountRuntimeState)TraitEffectStateCodec.Restore(input.State);
        }

        private static void ValidateBoundOrigin(EffectOriginKey origin)
        {
            if (origin.OwnerInstanceId <= 0)
                throw new ArgumentException(
                    "MoveAttackDiscount effect owner must be a valid unit.",
                    nameof(origin));
            TraitKey key = TraitEffectOrigin.GetRequiredTraitKey(
                origin,
                new EntityId(origin.OwnerInstanceId));
            if (key != TraitKey.MoveAttackDiscount)
                throw new ArgumentException(
                    "MoveAttackDiscount effect requires the MoveAttackDiscount origin.",
                    nameof(origin));
        }

        private static void ValidateResultSource(TraitKey source)
        {
            if (source != TraitKey.MoveAttackDiscount)
                throw new InvalidOperationException("Trait origin mismatch");
        }
    }
}
