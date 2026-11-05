using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class SoloActionEffectRule : ICombatEffectRule
    {
        private readonly SoloActionTraitDefinition _definition;
        private readonly SoloActionTraitRule _rule;

        internal SoloActionEffectRule(SoloActionTraitDefinition definition, SoloActionTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        internal TraitShieldGrant Evaluate(in EffectRuleInput input, CharacterCombatFacts source, CharacterCombatFacts recipient,
            IReadOnlyList<CharacterCombatFacts> characters, TraitShieldGrant prior, long completed, long completedOwnSideTurnEnds, bool resetDue)
        {
            if (input.State is not StatelessEffectStateSnapshot || TraitEffectOrigin.GetRequiredTraitKey(input.Origin, source.EntityId) != _rule.Key)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            // 주기는 자기 진영 종료 횟수로 판정하고, 마지막 초기화 기록은 기존 합산 누계 단위를 유지합니다.
            bool resetOwnSide = resetDue && completedOwnSideTurnEnds > 0 &&
                completedOwnSideTurnEnds % _definition.ResetEveryControllerTurnEnds == 0;
            return ShieldGrantRules.Reconcile(prior, input.Origin, _definition.ShieldAmount,
                _rule.Eligible(_definition, source, recipient, characters), completed, 1, resetOwnSide);
        }
    }
}
