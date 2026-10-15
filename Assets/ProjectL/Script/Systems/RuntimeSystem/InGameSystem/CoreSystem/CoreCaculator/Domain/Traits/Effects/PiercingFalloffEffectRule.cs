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
    internal sealed class PiercingFalloffEffectRule : IEffectPierceFalloffRule
    {
        private readonly PiercingFalloffTraitDefinition _definition;
        private readonly PiercingFalloffTraitRule _rule;

        internal PiercingFalloffEffectRule(
            PiercingFalloffTraitDefinition definition,
            PiercingFalloffTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public EffectPierceFalloffContribution EvaluatePierceFalloff(
            in EffectRuleInput input,
            in PierceFalloffEffectContext context)
        {
            ValidateBoundInput(input);
            int remainingPercent = _rule.EvaluateRemainingPercent(
                _definition,
                context.EncounterIndex);
            int targetBudget = _rule.CalculateTargetBudget(
                _definition,
                context.DamageAfterBonuses);
            return new EffectPierceFalloffContribution(
                input.Origin,
                remainingPercent,
                targetBudget);
        }

        private static void ValidateBoundInput(in EffectRuleInput input)
        {
            EffectOriginKey origin = input.Origin;
            if (origin.OwnerInstanceId <= 0)
            {
                throw new ArgumentException(
                    "PiercingFalloff effect owner must be a valid unit.",
                    nameof(input));
            }

            TraitKey key = TraitEffectOrigin.GetRequiredTraitKey(
                origin,
                new EntityId(origin.OwnerInstanceId));
            if (key != TraitKey.PiercingFalloff)
            {
                throw new ArgumentException(
                    "PiercingFalloff effect requires its matching Trait origin.",
                    nameof(input));
            }
            if (input.State is not PiercingFalloffEffectStateSnapshot)
            {
                throw new ArgumentException(
                    "PiercingFalloff effect requires its matching snapshot.",
                    nameof(input));
            }
        }
    }
}
