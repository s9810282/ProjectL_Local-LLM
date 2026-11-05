using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items
{
    public sealed class LanternItemRule : IItemAfterDamageRule
    {
        private static readonly HostilityPolicy Hostility = new HostilityPolicy();

        public ItemRuleDecision Evaluate(ItemDefinition definition, ItemAfterDamageContext context)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!(definition.Effect is LanternItemSpec spec))
                throw new InvalidOperationException("랜턴 규칙과 효과 명세가 일치해야 합니다.");
            var holder = context.RecipientAfterDamage;
            var hit = context.Damage;
            if (holder.Health.IsDead || holder.Items.EquippedItemKey != definition.Key ||
                hit.Kind != CombatDamageKind.OrdinaryAttack || hit.Attacker == null ||
                !Hostility.AreHostile(holder.Owner, hit.Attacker.Owner))
                return ItemRuleDecision.NoActivation();
            return ItemRuleDecision.ApplyEffect(hit.Attacker.EntityId,
                new ItemTemporaryEffect(definition.Key, ItemStatKind.PhysicalDefense, spec.DefensePenalty, hit.PreparationIndex));
        }
    }
}
