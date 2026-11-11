using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class HunterEffectRule : IEffectOrderedHitRule, IEffectAssessmentCoverageRule, IEffectAssessmentHarmRule
    {
        public EffectAssessmentHarmScope AssessmentHarmScope => EffectAssessmentHarmScope.NoAdditionalImmediateHarm;

        public Contracts.CombatEffects.CombatAssessmentGap AssessmentGaps => Contracts.CombatEffects.CombatAssessmentGap.RandomOutcome;

        public bool RequiresOrderedHits => true;

        private readonly HunterTraitDefinition _definition;
        private readonly HunterTraitRule _rule;
        internal HunterEffectRule(HunterTraitDefinition definition, HunterTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        internal int MaxRewards => _definition.MaxRewardsPerStage;
        internal IReadOnlyList<EntityId> Candidates(in EffectRuleInput input, CharacterCombatFacts owner, IEnumerable<CharacterCombatFacts> characters)
        {
            Validate(input);
            return _rule.Highest(owner, characters);
        }
        internal OwnedCharacterRecord Award(in EffectRuleInput input, OwnedCharacterRecord source)
        {
            Validate(input);
            return _rule.Award(source, _definition.AttackPerTargetKill);
        }
        private void Validate(in EffectRuleInput input)
        {
            if (input.State is not HunterEffectStateSnapshot ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _definition.TraitKey)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
        }
    }
}
