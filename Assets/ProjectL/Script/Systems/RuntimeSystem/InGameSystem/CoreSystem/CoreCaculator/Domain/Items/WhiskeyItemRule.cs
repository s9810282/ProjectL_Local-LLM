using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items
{
    public sealed class WhiskeyItemRule : IItemAfterDamageRule
    {
        public ItemRuleDecision Evaluate(ItemDefinition definition, ItemAfterDamageContext context)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (!(definition.Effect is WhiskeyItemSpec spec))
                throw new InvalidOperationException("위스키 규칙과 효과 명세가 일치해야 합니다.");

            var holder = context.RecipientAfterDamage;
            if (holder.Items.EquippedItemKey != definition.Key || holder.Health.IsDead ||
                context.ActualHealthLoss == 0 ||
                (long)holder.Health.CurrentHealth * 100 > (long)holder.Health.MaxHealth * spec.TriggerHealthPercent)
                return ItemRuleDecision.NoActivation();

            int heal = (int)((long)holder.Health.MaxHealth * spec.HealPercent / 100);
            return ItemRuleDecision.Heal(holder.EntityId, heal);
        }
    }
}
