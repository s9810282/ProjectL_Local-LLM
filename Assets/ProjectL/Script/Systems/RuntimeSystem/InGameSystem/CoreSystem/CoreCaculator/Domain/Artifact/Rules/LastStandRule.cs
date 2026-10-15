using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules
{
    internal sealed class LastStandRule : IEffectAttackDamageRule
    {
        internal int BonusPerMissingAlly { get; }

        internal LastStandRule(int bonusPerMissingAlly)
        {
            if (bonusPerMissingAlly <= 0)
                throw new ArgumentOutOfRangeException(nameof(bonusPerMissingAlly));
            BonusPerMissingAlly = bonusPerMissingAlly;
        }

        public EffectAttackDamageContribution EvaluateAttackDamage(
            in EffectRuleInput input,
            in AttackDamageEffectContext context)
        {
            int difference = Math.Max(0, context.EnemyUnitCount - context.AllyUnitCount);
            int bonus = checked(difference * BonusPerMissingAlly);
            return new EffectAttackDamageContribution(input.Origin, bonus);
        }
    }
}
