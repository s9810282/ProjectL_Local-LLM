using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules
{
    internal sealed class SafeBombardmentCategoryRule : IEffectAttackTargetingRule
    {
        internal SafeBombardmentCategoryRule(SafeBombardmentCategoryRuleDefinition definition)
        {
            _ = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        /// <summary>캐논의 지면 조준과 피격 대상 확장을 허용하고 직접 조준의 근거리 제한을 제공한다.</summary>
        public EffectAttackTargetingContribution EvaluateAttackTargeting(in EffectRuleInput input)
            => new EffectAttackTargetingContribution(input.Origin, new AttackTargetingPolicy(
                allowsEmptySingle: true, allowsFriendlyTargets: true,
                allowsNeutralTargets: true, excludesAdjacentDirectCells: true));
    }
}
