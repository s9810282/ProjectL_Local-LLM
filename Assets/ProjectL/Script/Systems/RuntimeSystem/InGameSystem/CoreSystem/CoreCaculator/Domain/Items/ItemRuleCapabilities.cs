using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Items
{
    public interface IItemRule
    {
    }

    public interface IItemFinalDamageRule : IItemRule
    {
        ItemRuleDecision Evaluate(ItemDefinition definition, ItemDamageContext context);
    }
    public interface IItemAfterDamageRule : IItemRule
    {
        ItemRuleDecision Evaluate(ItemDefinition definition, ItemAfterDamageContext context);
    }
    public interface IItemAttackCompletedRule : IItemRule
    {
        ItemRuleDecision Evaluate(ItemDefinition definition, ItemAttackContext context);
    }
}
