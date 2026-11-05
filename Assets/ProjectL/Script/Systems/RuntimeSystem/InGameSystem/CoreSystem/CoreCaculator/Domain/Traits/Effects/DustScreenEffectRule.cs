using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class DustScreenEffectRule : IEffectIncomingDamageRule
    {
        private readonly DustScreenTraitDefinition _definition;
        private readonly DustScreenTraitRule _rule;
        private readonly TraitTrailFact _trail;
        internal DustScreenEffectRule(DustScreenTraitDefinition definition, DustScreenTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        internal DustScreenEffectRule(TraitTrailFact trail)
        {
            _trail = trail ?? throw new ArgumentNullException(nameof(trail));
            _rule = new DustScreenTraitRule();
        }
        internal TraitTrailFact Create(in EffectRuleInput input, CharacterCombatFacts actor, CellCoord cell, long id)
        {
            if (_definition == null || input.State is not StatelessEffectStateSnapshot ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, actor.EntityId) != _rule.Key)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return _rule.Create(_definition, input.Origin, actor.Owner, cell, id);
        }
        public EffectIncomingDamageContribution EvaluateIncomingDamage(in EffectRuleInput input, in IncomingDamageEffectContext context)
        {
            if (input.State is not StatelessEffectStateSnapshot ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _rule.Key ||
                _trail != null && input.Origin != _trail.Origin)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return new EffectIncomingDamageContribution(input.Origin, 0,
                _rule.Reduces(_trail, context.Recipient, context.Kind) ? 100 - _trail.AttackDamageReductionPercent : 100);
        }
    }
}
