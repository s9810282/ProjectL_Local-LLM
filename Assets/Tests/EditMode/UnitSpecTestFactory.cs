using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    internal static class UnitSpecTestFactory
    {
        internal static CategorySpec[] CreateDefaultCategoryCatalog()
        {
            return new[]
            {
                new CategorySpec(CategoryKey.Base, Array.Empty<CategoryRuleSpec>()),
                new CategorySpec(CategoryKey.Rider, new CategoryRuleSpec[]
                {
                    new ManeuverRefundCategoryRuleSpec(),
                }),
                new CategorySpec(CategoryKey.Heavy, new CategoryRuleSpec[]
                {
                    new DirectionalArmorCategoryRuleSpec(50, Rotation4.R0),
                }),
                new CategorySpec(CategoryKey.Cannon, new CategoryRuleSpec[]
                {
                    new SafeBombardmentCategoryRuleSpec(),
                }),
            };
        }

        internal static CategoryMatchupSpec CreateDefaultCategoryMatchups() =>
            CategoryMatchupSpec.CreateDefault();

        internal static UnitSpec Create(
            EntityKey key,
            int footprintWidth,
            int footprintHeight,
            CellCoord[] footprintLocalCells,
            int maxHealth,
            MoveMethod moveMethod,
            int moveRange,
            int moveCost,
            int attackRange,
            int attackPower,
            int maxActionCost,
            int attackCost,
            int priority = 0,
            int delay = 0,
            UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both)
        {
            return Create(
                key,
                footprintWidth,
                footprintHeight,
                footprintLocalCells,
                maxHealth,
                moveMethod,
                moveRange,
                moveCost,
                attackRange,
                attackPower,
                maxActionCost,
                attackCost,
                priority,
                delay,
                Array.Empty<TraitSpec>(),
                ownerEligibility: ownerEligibility);
        }

        internal static UnitSpec Create(
            EntityKey key,
            int footprintWidth,
            int footprintHeight,
            CellCoord[] footprintLocalCells,
            int maxHealth,
            MoveMethod moveMethod,
            int moveRange,
            int moveCost,
            int attackRange,
            int attackPower,
            int maxActionCost,
            int attackCost,
            int priority,
            int delay,
            IReadOnlyList<TraitSpec> traits,
            UnitRole role = UnitRole.Regular,
            CategoryKey categoryKey = CategoryKey.Base,
            int physicalDefense = 0,
            UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both)
        {
            bool hasTimelineConfiguration = priority != 0 || delay != 0;

            return new UnitSpec(
                key,
                role,
                footprintWidth,
                footprintHeight,
                footprintLocalCells,
                maxHealth,
                moveMethod,
                moveRange,
                moveCost,
                attackRange,
                categoryKey,
                attackPower,
                maxActionCost,
                attackCost,
                physicalDefense,
                priority,
                delay,
                hasTimelineConfiguration,
                traits,
                ownerEligibility);
        }
    }
}
