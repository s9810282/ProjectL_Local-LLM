using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class ThornFieldEffectRule : IEffectThornFieldRule
    {
        private readonly ThornFieldTraitDefinition _definition;
        private readonly ThornFieldTraitRule _rule;

        internal ThornFieldTraitDefinition Definition => _definition;
        internal ThornFieldTraitRule Rule => _rule;

        internal ThornFieldEffectRule(
            ThornFieldTraitDefinition definition,
            ThornFieldTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public EffectThornFieldContribution EvaluateThornFieldApplication(
            in EffectRuleInput input,
            in ThornFieldApplicationEffectContext context)
        {
            ValidateBoundInput(input);
            return new EffectThornFieldContribution(
                input.Origin,
                _definition.ThornDamage,
                _definition.LifetimeUnitTurns,
                ThornFieldTraitRule.CalculateApplicationCells(context.SelectedCenter));
        }

        private static void ValidateBoundInput(in EffectRuleInput input)
        {
            EffectOriginKey origin = input.Origin;
            if (origin.OwnerInstanceId <= 0)
            {
                throw new ArgumentException(
                    "ThornField effect owner must be a valid unit.",
                    nameof(input));
            }

            TraitKey key = TraitEffectOrigin.GetRequiredTraitKey(
                origin,
                new EntityId(origin.OwnerInstanceId));
            if (key != TraitKey.ThornField)
            {
                throw new ArgumentException(
                    "ThornField effect requires its matching Trait origin.",
                    nameof(input));
            }
            if (input.State is not ThornFieldEffectStateSnapshot)
            {
                throw new ArgumentException(
                    "ThornField effect requires its matching snapshot.",
                    nameof(input));
            }
        }
    }
}
