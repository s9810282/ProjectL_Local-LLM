using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Tests.Core
{
    public sealed class CharacterActionCostRulesTests
    {
        [TestCase(typeof(UnitSpec))]
        [TestCase(typeof(UnitDefinition))]
        [TestCase(typeof(CharacterBaselineStats))]
        [TestCase(typeof(CharacterBaselineStatsSnapshot))]
        [TestCase(typeof(UnitStatsSnapshot))]
        [TestCase(typeof(MainCharacterPersistentState))]
        public void UnitContracts_DoNotExposeABalanceOrTimelineClock(Type boundary)
        {
            foreach (var property in new[] { "MaxActionCost", "CurrentActionCost", "ActionPoint", "Delay" })
                Assert.That(boundary.GetProperty(property), Is.Null, boundary.Name + "." + property);
            foreach (var constructor in boundary.GetConstructors())
                foreach (var parameter in constructor.GetParameters())
                    Assert.That(parameter.Name, Is.Not.EqualTo("maxActionCost")
                        .And.Not.EqualTo("currentActionCost").And.Not.EqualTo("delay"), boundary.Name);
        }

        [TestCase(CategoryKey.Main, 3)]
        [TestCase(CategoryKey.Rider, 4)]
        [TestCase(CategoryKey.Cannon, 2)]
        public void DefinitionsDeriveMovementRangeFromCategory(CategoryKey category, int range)
        {
            var spec = UnitSpecTestFactory.Create(EntityKey.Unit_Akimbo, 1, 1, new[] { CellCoord.Zero },
                10, MoveMethod.LiftAndPlace, 1, 2, 10, Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits.TraitSpec>(),
                categoryKey: category);
            var definition = new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels.UnitCatalog(
                new[] { spec },
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits.TraitRegistry.CreateDefault(),
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels.CategoryRegistry(UnitSpecTestFactory.CreateDefaultCategoryCatalog()))
                .GetRequired(spec.Key);
            Assert.That(definition.MoveRange, Is.EqualTo(range));
            Assert.That(typeof(UnitSpec).GetProperty("MoveCost"), Is.Null);
            Assert.That(typeof(UnitSpec).GetProperty("AttackCost"), Is.Null);
            Assert.That(typeof(MainCharacterPersistentState).GetProperty("MoveRange"), Is.Null);
        }
    }
}
