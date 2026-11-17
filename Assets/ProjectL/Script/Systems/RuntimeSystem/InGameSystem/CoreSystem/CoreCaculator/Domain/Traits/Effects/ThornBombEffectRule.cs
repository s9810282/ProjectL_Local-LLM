using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class ThornBombEffectRule : IEffectThornBombRule, IEffectAttackTargetingRule
    {
        private readonly ThornBombTraitDefinition _definition;

        internal ThornBombEffectRule(ThornBombTraitDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public EffectAttackTargetingContribution EvaluateAttackTargeting(in EffectRuleInput input)
        {
            GetDamagePercent(input);
            return new EffectAttackTargetingContribution(input.Origin, new AttackTargetingPolicy(
                allowsEmptySingle: true, allowsFriendlyTargets: false, allowsNeutralTargets: false,
                excludesAdjacentDirectCells: false, allowsFriendlyCharacters: true));
        }

        public int GetDamagePercent(in EffectRuleInput input)
        {
            if (input.State is not StatelessEffectStateSnapshot || input.Origin.OwnerInstanceId <= 0 ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _definition.TraitKey)
                throw new ArgumentException("가시 폭탄 효과의 출처와 상태가 일치해야 합니다.", nameof(input));

            return _definition.DamagePercent;
        }
    }
}
