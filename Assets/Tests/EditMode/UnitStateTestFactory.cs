using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    internal static class UnitStateTestFactory
    {
        private static readonly CategoryRegistry Categories = CategoryRegistry.CreateDefault();

        internal static UnitDefinition CreateDefinition(
            EntityKey key,
            TileFootprint footprint,
            int maxHealth,
            MoveMethod moveMethod,
            int attackRange,
            int attackPower,
            int priority = 0,
            TraitDefinition[] traits = null,
            UnitRole role = UnitRole.Regular,
            CategoryKey categoryKey = CategoryKey.Main,
            int physicalDefense = 0, UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both)
        {
            CategoryDefinition category = Categories.GetRequired(categoryKey);

            return new UnitDefinition(
                key,
                role,
                footprint,
                maxHealth,
                moveMethod,
                attackRange,
                category,
                attackPower,
                physicalDefense,
                priority,
                traits ?? Array.Empty<TraitDefinition>(), ownerEligibility);
        }
    }
}
