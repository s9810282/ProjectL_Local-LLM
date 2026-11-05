using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class FocusedBombardmentEffectRule : IEffectTargetDamageRule
    {
        private readonly FocusedBombardmentTraitDefinition _definition;
        private readonly FocusedBombardmentTraitRule _rule;
        internal FocusedBombardmentEffectRule(FocusedBombardmentTraitDefinition definition, FocusedBombardmentTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        public EffectTargetDamageContribution EvaluateTargetDamage(in EffectRuleInput input, in TargetDamageEffectContext context)
        {
            Validate(input);
            return new EffectTargetDamageContribution(input.Origin, false, _rule.Numerator(context.CapturedEnemyCount, _definition.RequiredEnemyCount, _definition.ExtraDamagePercent), 100);
        }
        private void Validate(in EffectRuleInput input)
        {
            if (input.State is not StatelessEffectStateSnapshot ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _definition.TraitKey)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
        }
    }
}
