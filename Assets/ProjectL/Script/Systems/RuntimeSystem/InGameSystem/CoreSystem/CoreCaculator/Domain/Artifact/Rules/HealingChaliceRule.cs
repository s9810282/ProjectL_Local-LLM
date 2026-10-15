using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules
{
    internal sealed class HealingChaliceRule : IEffectStageClearedRule
    {
        internal int HealAmount { get; }

        internal HealingChaliceRule(int healAmount)
        {
            if (healAmount <= 0) throw new ArgumentOutOfRangeException(nameof(healAmount));
            HealAmount = healAmount;
        }

        public EffectHealContribution EvaluateStageCleared(
            in EffectRuleInput input,
            in StageClearedEffectContext context)
        {
            return new EffectHealContribution(input.Origin, context.MainCharacterId, HealAmount);
        }
    }
}
