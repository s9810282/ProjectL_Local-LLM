using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    internal static class RankedCombatTestData
    {
        internal static TraitSpec Spec(TraitGrowthCatalog catalog, TraitKey key, int rank)
        {
            var definition = catalog.GetTrait(key);
            Assert.That(definition.IsTrainable, Is.True, key.ToString());
            var basis = new UnitSpec(EntityKey.Unit_Akimbo, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero },
                100, MoveMethod.LiftAndPlace, 3, CategoryKey.Cannon, 20, 0, 0, new[] { definition.Normal });
            var growth = OwnedTraitGrowth.Empty;
            for (int current = 1; current < rank; current++)
                growth = growth.WithPurchase(key);
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), basis.Key, CharacterOwnershipRole.Sub, growth: growth);
            return OwnedUnitSpecResolver.Resolve(basis, owned, catalog).Traits.ToArray().Single(t => t.TraitKey == key);
        }
    }
}
