using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items
{
    public static class CombatItemResolver
    {
        private static readonly ItemRuleRegistry Rules = ItemRuleRegistry.CreateDefault();

        /// <summary>소지자가 장착한 아이템의 정의다. 빈 슬롯은 null이며, 정의 목록에 없는 키는 상태 오류다.</summary>
        internal static ItemDefinition EquippedDefinition(IReadOnlyList<ItemDefinition> definitions, CharacterCombatFacts holder)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (holder == null) throw new ArgumentNullException(nameof(holder));
            if (!holder.Items.EquippedItemKey.HasValue) return null;
            return definitions.SingleOrDefault(d => d.Key == holder.Items.EquippedItemKey.Value)
                ?? throw new InvalidOperationException("장착한 아이템 정의가 없습니다.");
        }

        public static ItemRuleDecision BeforeDamage(ItemDefinition definition, ItemDamageContext context)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Rules.IsAvailable(definition.Effect) && Rules.GetRequired(definition.Effect) is IItemFinalDamageRule rule
                ? rule.Evaluate(definition, context)
                : ItemRuleDecision.NoActivation();
        }

        public static ItemRuleDecision AfterDamage(ItemDefinition definition, ItemAfterDamageContext context)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Rules.IsAvailable(definition.Effect) && Rules.GetRequired(definition.Effect) is IItemAfterDamageRule rule
                ? rule.Evaluate(definition, context)
                : ItemRuleDecision.NoActivation();
        }

        public static ItemRuleDecision AfterOrdinaryAttack(ItemDefinition definition, ItemAttackContext context)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return Rules.IsAvailable(definition.Effect) && Rules.GetRequired(definition.Effect) is IItemAttackCompletedRule rule
                ? rule.Evaluate(definition, context)
                : ItemRuleDecision.NoActivation(context.RandomState);
        }
    }
}
