using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class TauntEffectRule : IEffectIncomingDamageRule
    {
        private readonly TauntTraitDefinition _definition;
        private readonly TauntTraitRule _rule;

        internal TauntEffectRule(TauntTraitDefinition definition, TauntTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        internal bool Affects(in EffectRuleInput input, CharacterCombatFacts source, CharacterCombatFacts actor)
        {
            if (input.State is not StatelessEffectStateSnapshot || TraitEffectOrigin.GetRequiredTraitKey(input.Origin, source.EntityId) != _rule.Key)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return _rule.Affects(_definition, source, actor);
        }

        public EffectIncomingDamageContribution EvaluateIncomingDamage(in EffectRuleInput input, in IncomingDamageEffectContext context)
        {
            int reduction = 0;
            if (_definition.DamageReductionPercent > 0 && context.Recipient != null && context.DamageSourceId is { } id)
            {
                var attacker = context.Characters.SingleOrDefault(c => c.EntityId == id);
                if (attacker != null && Affects(input, context.Recipient, attacker))
                    reduction = _definition.DamageReductionPercent;
            }
            return new EffectIncomingDamageContribution(input.Origin, 0, 100 - reduction);
        }
    }
}
