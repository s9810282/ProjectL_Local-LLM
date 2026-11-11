using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class SacrificeEffectRule : ICombatEffectRule
    {
        private readonly SacrificeTraitDefinition _definition;
        private readonly SacrificeTraitRule _rule;

        internal bool PreventsLethalSacrificeDamage => _definition.PreventsLethalSacrificeDamage;
        internal int ShieldOnActivation => _definition.ShieldOnActivation;

        internal SacrificeEffectRule(SacrificeTraitDefinition definition, SacrificeTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        internal bool CanProtect(in EffectRuleInput input, CharacterCombatFacts actor, CharacterCombatFacts source,
            CharacterCombatFacts recipient, CombatControllerPhase controller, long sequence, int reserved = 0)
        {
            var state = State(input, source.EntityId);
            return _rule.Eligible(_definition, actor, source, recipient, controller) &&
                (long)SacrificeTraitRule.UsedAt(state.LastEnemyTurnSequence, state.ActivationsThisEnemyTurn, sequence) + reserved < _definition.MaxTriggersPerEnemyTurn;
        }

        internal SacrificeTraitStatusSnapshot Consume(in EffectRuleInput input, long sequence)
        {
            var state = State(input, new EntityId(input.Origin.OwnerInstanceId));
            int used = SacrificeTraitRule.UsedAt(state.LastEnemyTurnSequence, state.ActivationsThisEnemyTurn, sequence);
            if (used >= _definition.MaxTriggersPerEnemyTurn) throw new InvalidOperationException("희생 횟수를 이미 소모했습니다.");
            return new SacrificeTraitStatusSnapshot(sequence, checked(used + 1));
        }

        private SacrificeEffectStateSnapshot State(in EffectRuleInput input, EntityId source)
        {
            if (input.State is not SacrificeEffectStateSnapshot state || TraitEffectOrigin.GetRequiredTraitKey(input.Origin, source) != _rule.Key)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return state;
        }
    }
}
