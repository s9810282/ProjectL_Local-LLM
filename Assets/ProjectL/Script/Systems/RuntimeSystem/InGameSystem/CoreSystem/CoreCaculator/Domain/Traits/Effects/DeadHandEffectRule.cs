using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class DeadHandEffectRule : IEffectAttackSweepRule
    {
        private readonly DeadHandTraitDefinition _definition;

        internal DeadHandEffectRule(DeadHandTraitDefinition definition)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        }

        public EffectAttackSweepContribution? EvaluateAttackSweep(in EffectRuleInput input, in AttackSweepEffectContext context)
        {
            if (input.Origin.OwnerInstanceId <= 0 ||
                TraitEffectOrigin.GetRequiredTraitKey(input.Origin, new EntityId(input.Origin.OwnerInstanceId)) != TraitKey.DeadHand ||
                input.State is not DeadHandEffectStateSnapshot)
                throw new ArgumentException("죽음의 손 출처와 상태가 일치해야 합니다.", nameof(input));
            return context.DamageKind == CombatDamageKind.OrdinaryAttack
                ? new EffectAttackSweepContribution(input.Origin, _definition.DamagePercent) : null;
        }
    }
}
