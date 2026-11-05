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
    internal sealed class ImpactBoltEffectRule : IEffectKnockbackRule
    {
        private readonly ImpactBoltTraitDefinition _definition;
        private readonly ImpactBoltTraitRule _rule;

        internal ImpactBoltEffectRule(
            ImpactBoltTraitDefinition definition,
            ImpactBoltTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public EffectKnockbackContribution EvaluateKnockback(in EffectRuleInput input)
        {
            ValidateBoundInput(input);
            return new EffectKnockbackContribution(input.Origin, _definition.DistanceCells, EffectKnockbackMode.ImpactCenter);
        }

        private void ValidateBoundInput(in EffectRuleInput input)
        {
            EffectOriginKey origin = input.Origin;
            if (origin.OwnerInstanceId <= 0)
            {
                throw new ArgumentException(
                    "ImpactBolt effect owner must be a valid unit.",
                    nameof(input));
            }

            TraitKey key = TraitEffectOrigin.GetRequiredTraitKey(
                origin,
                new EntityId(origin.OwnerInstanceId));
            if (key != _rule.Key)
            {
                throw new ArgumentException(
                    "ImpactBolt effect requires its matching Trait origin.",
                    nameof(input));
            }
            if (input.State is not ImpactBoltEffectStateSnapshot)
            {
                throw new ArgumentException(
                    "ImpactBolt effect requires its matching snapshot.",
                    nameof(input));
            }
        }
    }
}
