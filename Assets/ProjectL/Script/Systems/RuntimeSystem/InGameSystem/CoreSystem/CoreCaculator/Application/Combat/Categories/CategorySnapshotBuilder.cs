using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories
{

    internal sealed class CategorySnapshotBuilder
    {
        internal CategorySnapshot CreateSnapshot(
            CategoryDefinition definition,
            UnitCategoryState state)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (state == null) throw new ArgumentNullException(nameof(state));

            switch (definition.Key)
            {
                case CategoryKey.Base:
                case CategoryKey.Cannon:
                    return CategorySnapshot.CreateStateless(definition.Key);
                case CategoryKey.Rider:
                    return CategorySnapshot.CreateRider(
                        state.GetRequired<ManeuverRefundRuntimeState>(
                            CategoryRuleKey.ManeuverRefund).RefundUsedThisTurn);
                case CategoryKey.Heavy:
                    DirectionalArmorRuntimeState armor =
                        state.GetRequired<DirectionalArmorRuntimeState>(
                            CategoryRuleKey.DirectionalArmor);
                    DirectionalArmorCategoryRuleDefinition armorDefinition =
                        (DirectionalArmorCategoryRuleDefinition)GetRequiredDefinition(
                            definition,
                            CategoryRuleKey.DirectionalArmor);
                    return CategorySnapshot.CreateHeavy(
                        armor.DefenseFacing,
                        armor.MovedThisTurn,
                        armorDefinition.DamageReductionPercent);
                default:
                    throw new InvalidOperationException($"Unsupported Category {definition.Key}.");
            }
        }

        internal static CategoryRuleDefinition GetRequiredDefinition(
            CategoryDefinition definition,
            CategoryRuleKey key)
        {
            foreach (CategoryRuleDefinition rule in definition.RuleDefinitions.Span)
                if (rule.Key == key) return rule;
            throw new InvalidOperationException($"Category rule {key} was not found.");
        }
    }
}
