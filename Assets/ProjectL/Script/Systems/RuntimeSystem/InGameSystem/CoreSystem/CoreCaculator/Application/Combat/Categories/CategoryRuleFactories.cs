using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories
{
    internal static class CategoryRuleFactories
    {
        internal static CategoryRuleDefinition CreateDefinition(CategoryRuleSpec spec)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));

            return spec switch
            {
                SafeBombardmentCategoryRuleSpec => new SafeBombardmentCategoryRuleDefinition(),
                _ => throw new InvalidOperationException(
                    $"Category rule spec type {spec.GetType().Name} is not registered."),
            };
        }

        internal static ICombatEffectRule CreateRule(CategoryRuleDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            return definition switch
            {
                SafeBombardmentCategoryRuleDefinition bombardment =>
                    new SafeBombardmentCategoryRule(bombardment),
                _ => throw new InvalidOperationException(
                    $"Category rule definition type {definition.GetType().Name} is not registered."),
            };
        }

        internal static CategoryRuntimeState CreateInitialState(CategoryRuleDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            return definition switch
            {
                SafeBombardmentCategoryRuleDefinition => null,
                _ => throw new InvalidOperationException(
                    $"Category rule definition type {definition.GetType().Name} is not registered."),
            };
        }

    }
}
