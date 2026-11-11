using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class DustScreenUpgradeContractTests
    {
        [Test]
        public void Rank2OnlyChangesAlliedExemption()
        {
            using var fixture = new CoreAuthoringFixture();
            var row = fixture.Create<TraitGrowthDefinitionDataSO>();
            var normal = new DustScreenTraitAuthoring();
            CoreAuthoringFixture.Set(normal, "opposingTurnEndsToExpire", 3);
            var upgrade = new DustScreenRankAuthoring();
            CoreAuthoringFixture.Set(upgrade, "exemptsAlliedAttackRangePenalty", true);
            CoreAuthoringFixture.Set(row, "group", TraitGrowthGroup.Common);
            CoreAuthoringFixture.Set(row, "normal", normal);
            CoreAuthoringFixture.Set(row, "rank2", upgrade);

            var data = (TraitGrowthDefinitionData)row.GetData();
            var original = (DustScreenTraitSpec)TraitSpecAdapter.Read(data.Normal);
            var definition = new TraitGrowthDefinition(original, null, data.Name, data.Description,
                data.EnhancedName, data.EnhancedDescription, data.Rank2, data.Rank3);
            var unit = new UnitSpec(EntityKey.Unit_Cowboy, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 20,
                MoveMethod.LiftAndPlace, 2, CategoryKey.Rider, 5, 0, 1, new TraitSpec[] { original }, UnitOwnerEligibility.PlayerOnly);
            var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories, new[] { definition });
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), unit.Key, CharacterOwnershipRole.Sub);
            DustScreenTraitSpec Resolve(OwnedCharacterRecord record) => OwnedUnitSpecResolver.Resolve(unit, record, catalog)
                .Traits.ToArray().OfType<DustScreenTraitSpec>().Single();

            Assert.That(definition.MaxRank, Is.EqualTo(2));
            Assert.That((data.Rank2.Amount, data.Rank2.SecondAmount, data.Rank2.ExemptsAlliedAttackRangePenalty),
                Is.EqualTo((0, (int?)null, true)));
            Assert.That(Resolve(owned).SameValues(new DustScreenTraitSpec(1, 20, 3)), Is.True);
            Assert.That(Resolve(owned.WithGrowth(owned.Growth.WithPurchase(TraitKey.DustScreen)))
                .SameValues(new DustScreenTraitSpec(1, 20, 3, true)), Is.True);
        }
    }
}
