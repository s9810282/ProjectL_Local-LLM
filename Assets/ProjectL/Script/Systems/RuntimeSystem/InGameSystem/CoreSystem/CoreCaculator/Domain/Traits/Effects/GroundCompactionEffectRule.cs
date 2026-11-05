using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class GroundCompactionEffectRule : ICombatEffectRule
    {
        private readonly GroundCompactionTraitDefinition _definition;
        private readonly GroundCompactionTraitRule _rule;
        internal GroundCompactionEffectRule(GroundCompactionTraitDefinition definition, GroundCompactionTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        internal TraitTrailFact Create(in EffectRuleInput input, CharacterCombatFacts actor, CellCoord cell, long id, long turnSequence)
        {
            if (input.State is not StatelessEffectStateSnapshot || TraitEffectOrigin.GetRequiredTraitKey(input.Origin, actor.EntityId) != _rule.Key)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return _rule.Create(_definition, input.Origin, actor.Owner, cell, id, turnSequence);
        }
    }
}
