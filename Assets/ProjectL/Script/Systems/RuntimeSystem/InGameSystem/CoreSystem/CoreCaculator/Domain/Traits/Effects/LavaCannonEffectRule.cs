using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class LavaCannonEffectRule : ICombatEffectRule
    {
        private readonly LavaCannonTraitRule _rule;

        internal LavaCannonEffectRule(LavaCannonTraitDefinition definition, LavaCannonTraitRule rule)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        internal StatusEffectSet Apply(in EffectRuleInput input, Owner attackerSide, CharacterCombatFacts recipient, CombatTurnFacts turn)
        {
            if (input.State is not StatelessEffectStateSnapshot ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _rule.Key)
                throw new ArgumentException("특성 원본과 상태가 일치해야 합니다.", nameof(input));
            return _rule.Apply(attackerSide, recipient, input.Origin, turn);
        }
    }
}
