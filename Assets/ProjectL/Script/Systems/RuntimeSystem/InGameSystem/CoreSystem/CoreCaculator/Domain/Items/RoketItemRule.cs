using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items
{
    public sealed class RoketItemRule : IItemFinalDamageRule
    {
        private static readonly HostilityPolicy Hostility = new HostilityPolicy();

        public ItemRuleDecision Evaluate(ItemDefinition definition, ItemDamageContext context)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!(definition.Effect is RoketItemSpec spec))
                throw new InvalidOperationException("로켓 규칙과 효과 명세가 일치해야 합니다.");

            var holder = context.Recipient;
            if (holder.Items.EquippedItemKey != definition.Key || holder.Health.IsDead ||
                context.Kind != CombatDamageKind.OrdinaryAttack || context.Attacker == null ||
                !Hostility.AreHostile(context.Attacker.Owner, holder.Owner) ||
                (long)context.DamageBeforeItems * 100 < (long)holder.Health.MaxHealth * spec.TriggerDamagePercent)
                return ItemRuleDecision.NoActivation();

            int remaining = (int)((long)context.DamageBeforeItems * (100 - spec.ReductionPercent) / 100);
            return ItemRuleDecision.Mitigation(holder.EntityId, remaining);
        }
    }
}
