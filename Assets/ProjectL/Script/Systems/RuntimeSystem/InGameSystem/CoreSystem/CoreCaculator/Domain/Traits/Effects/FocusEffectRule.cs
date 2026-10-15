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
    internal sealed class FocusEffectRule :
        IEffectAttackDamageRule,
        IEffectMoveSucceededRule,
        IEffectAttackSucceededRule,
        IEffectTurnStartedRule
    {
        private readonly FocusTraitDefinition _definition;
        private readonly FocusTraitRule _rule;

        internal FocusEffectRule(FocusTraitDefinition definition, FocusTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public EffectAttackDamageContribution EvaluateAttackDamage(
            in EffectRuleInput input,
            in AttackDamageEffectContext context)
        {
            FocusRuntimeState state = RestoreRequiredState(input);
            AttackDamageContribution result = _rule.EvaluateAttackDamage(
                new AttackDamageTraitContext(_definition, state, context.BaseDamage));
            ValidateResultSource(result.Source);
            return new EffectAttackDamageContribution(input.Origin, result.FlatBonus);
        }

        public EffectRuleResult PlanMoveSucceeded(
            in EffectRuleInput input,
            in MoveSucceededEffectContext context)
        {
            FocusRuntimeState state = RestoreRequiredState(input);
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
            FocusRuntimeState state = RestoreRequiredState(input);
            TraitKey[] contributors = TraitEffectOrigin.GetTraitContributors(
                input.Origin,
                context.PositiveCostContributors);
            TraitStateChange change = _rule.PlanAttackSucceeded(
                new AttackSucceededTraitContext(_definition, state, contributors));
            return TraitEffectResultAdapter.Convert(input, change);
        }

        public EffectRuleResult PlanTurnStarted(in EffectRuleInput input)
        {
            FocusRuntimeState state = RestoreRequiredState(input);
            TraitStateChange change = _rule.PlanTurnStarted(
                new TurnStartedTraitContext(_definition, state));
            return TraitEffectResultAdapter.Convert(input, change);
        }

        private FocusRuntimeState RestoreRequiredState(in EffectRuleInput input)
        {
            ValidateBoundOrigin(input.Origin);
            if (input.State is not FocusEffectStateSnapshot)
                throw new ArgumentException("Focus effect requires a Focus snapshot.", nameof(input));
            return (FocusRuntimeState)TraitEffectStateCodec.Restore(input.State);
        }

        private static void ValidateBoundOrigin(EffectOriginKey origin)
        {
            if (origin.OwnerInstanceId <= 0)
                throw new ArgumentException("Focus effect owner must be a valid unit.", nameof(origin));
            TraitKey key = TraitEffectOrigin.GetRequiredTraitKey(
                origin,
                new EntityId(origin.OwnerInstanceId));
            if (key != TraitKey.Focus)
                throw new ArgumentException("Focus effect requires the Focus origin.", nameof(origin));
        }

        private static void ValidateResultSource(TraitKey source)
        {
            if (source != TraitKey.Focus) throw new InvalidOperationException("Trait origin mismatch");
        }
    }
}
