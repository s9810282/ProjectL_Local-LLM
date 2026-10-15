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
    internal sealed class DistanceDamageScaleEffectRule : IEffectTargetDistanceDamageRule
    {
        private readonly DistanceDamageScaleTraitDefinition _definition;
        private readonly DistanceDamageScaleTraitRule _rule;

        internal DistanceDamageScaleEffectRule(
            DistanceDamageScaleTraitDefinition definition,
            DistanceDamageScaleTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        public EffectTargetDistanceDamageContribution EvaluateTargetDistanceDamage(
            in EffectRuleInput input,
            in TargetDistanceDamageEffectContext context)
        {
            ValidateBoundInput(input);
            return new EffectTargetDistanceDamageContribution(
                input.Origin,
                _rule.EvaluatePercent(_definition, context.Distance));
        }

        private void ValidateBoundInput(in EffectRuleInput input)
        {
            EffectOriginKey origin = input.Origin;
            if (origin.OwnerInstanceId <= 0)
            {
                throw new ArgumentException(
                    "DistanceDamageScale 효과의 소유자는 유효한 유닛이어야 합니다.",
                    nameof(input));
            }

            TraitKey key = TraitEffectOrigin.GetRequiredTraitKey(
                origin,
                new EntityId(origin.OwnerInstanceId));
            if (key != _rule.Key)
            {
                throw new ArgumentException(
                    "DistanceDamageScale 효과에는 같은 특성 원본이 필요합니다.",
                    nameof(input));
            }
            if (input.State is not DistanceDamageScaleEffectStateSnapshot)
            {
                throw new ArgumentException(
                    "DistanceDamageScale 효과에는 같은 특성 스냅샷이 필요합니다.",
                    nameof(input));
            }
        }
    }
}
