using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class KnockbackEffectRule : IEffectKnockbackRule
    {
        private readonly KnockbackTraitDefinition _definition;
        private readonly KnockbackTraitRule _rule;

        internal KnockbackEffectRule(
            KnockbackTraitDefinition definition,
            KnockbackTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public EffectKnockbackContribution EvaluateKnockback(in EffectRuleInput input)
        {
            ValidateBoundInput(input);
            return new EffectKnockbackContribution(input.Origin, _definition.DistanceCells);
        }

        private void ValidateBoundInput(in EffectRuleInput input)
        {
            EffectOriginKey origin = input.Origin;
            if (origin.OwnerInstanceId <= 0)
            {
                throw new ArgumentException(
                    "Knockback effect owner must be a valid unit.",
                    nameof(input));
            }

            TraitKey key = TraitEffectOrigin.GetRequiredTraitKey(
                origin,
                new EntityId(origin.OwnerInstanceId));
            if (key != _rule.Key)
            {
                throw new ArgumentException(
                    "Knockback effect requires its matching Trait origin.",
                    nameof(input));
            }
            if (input.State is not KnockbackEffectStateSnapshot)
            {
                throw new ArgumentException(
                    "Knockback effect requires its matching snapshot.",
                    nameof(input));
            }
        }
    }
}
