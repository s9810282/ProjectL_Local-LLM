using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class ArmorBreakerEffectRule : IEffectOrderedHitRule, IEffectAssessmentCoverageRule
    {
        public Contracts.CombatEffects.CombatAssessmentGap AssessmentGaps => Contracts.CombatEffects.CombatAssessmentGap.None;

        public bool RequiresOrderedHits => true;

        private readonly ArmorBreakerTraitDefinition _definition;
        private readonly ArmorBreakerTraitRule _rule;
        internal ArmorBreakerEffectRule(ArmorBreakerTraitDefinition definition, ArmorBreakerTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        internal CharacterDebuffState ApplyAfterHit(in EffectRuleInput input, CharacterDebuffState current)
        {
            if (input.State is not StatelessEffectStateSnapshot ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != _definition.TraitKey)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return _rule.ApplyAfterHit(current, _definition.DefenseReductionPercentPerHit, input.Origin);
        }
        internal int ReductionPercent => _definition.DefenseReductionPercentPerHit;
    }
}
