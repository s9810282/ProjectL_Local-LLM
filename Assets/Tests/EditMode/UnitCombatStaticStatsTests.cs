using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class UnitCombatStaticStatsTests
    {
        [Test]
        public void UnitSpec_CanonicalConstructor_AssignsStaticStats()
        {
            UnitSpec spec = CreateCanonicalSpec();

            Assert.That(spec.MoveMethod, Is.EqualTo(MoveMethod.LiftAndPlace));
            Assert.That(spec.MoveRange, Is.EqualTo(5));
            Assert.That(spec.MoveCost, Is.EqualTo(1));
            Assert.That(spec.AttackRange, Is.EqualTo(4));
            Assert.That(spec.AttackPower, Is.EqualTo(3));
            Assert.That(spec.MaxActionCost, Is.EqualTo(8));
            Assert.That(spec.ActionPoint, Is.EqualTo(spec.MaxActionCost));
            Assert.That(spec.AttackCost, Is.EqualTo(2));
            Assert.That(spec.Priority, Is.EqualTo(30));
            Assert.That(spec.Delay, Is.EqualTo(35));
            Assert.That(spec.HasTimelineConfiguration, Is.True);
        }

        [TestCase(UnitOwnerEligibility.PlayerOnly)]
        [TestCase(UnitOwnerEligibility.EnemyOnly)]
        [TestCase(UnitOwnerEligibility.Both)]
        public void UnitOwnerEligibility_PropagatesFromSpecThroughCatalog(
            UnitOwnerEligibility ownerEligibility)
        {
            UnitSpec spec = CreateCanonicalSpec(ownerEligibility: ownerEligibility);
            var catalog = new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault());

            Assert.That(spec.OwnerEligibility, Is.EqualTo(ownerEligibility));
            Assert.That(catalog.TryGet(spec.Key, out UnitDefinition definition), Is.True);
            Assert.That(definition.OwnerEligibility, Is.EqualTo(ownerEligibility));
            Assert.That(catalog.TryGet(EntityKey.Unit_Demon, out UnitDefinition missing), Is.False);
            Assert.That(missing, Is.Null);
        }

        [TestCase(0, 35, "priority")]
        [TestCase(-1, 35, "priority")]
        [TestCase(30, 0, "delay")]
        [TestCase(30, -1, "delay")]
        public void CanonicalUnitSpec_RejectsInvalidTimelineStats(
            int priority,
            int delay,
            string expectedParameter)
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(
                () => CreateCanonicalSpec(priority: priority, delay: delay));

            Assert.That(exception.ParamName, Is.EqualTo(expectedParameter));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void UnitSpec_CanonicalConstructor_RejectsNonPositiveMoveCost(int moveCost)
        {
            ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateCanonicalSpec(moveCost: moveCost));

            Assert.That(exception.ParamName, Is.EqualTo("moveCost"));
        }

        [Test]
        public void UnitSpec_CanonicalConstructor_RejectsNegativeAttackRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => CreateCanonicalSpec(attackRange: -1)).ParamName,
                Is.EqualTo("attackRange"));
        }

        [TestCase(CategoryKey.None)]
        [TestCase((CategoryKey)999)]
        public void UnitSpec_CanonicalConstructor_RejectsInvalidCategory(CategoryKey category)
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo, 1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace, 5, 1, 4, 3, 8, 2,
                30, 35, Array.Empty<TraitSpec>(), categoryKey: category)).ParamName, Is.EqualTo("categoryKey"));
        }

        [Test]
        public void UnitSpec_CanonicalConstructor_RejectsNegativeAttackPower()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => CreateCanonicalSpec(attackPower: -1)).ParamName,
                Is.EqualTo("attackPower"));
        }

        [Test]
        public void UnitSpec_CanonicalConstructor_RejectsNegativeMaxActionCost()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => CreateCanonicalSpec(maxActionCost: -1)).ParamName,
                Is.EqualTo("maxActionCost"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void UnitSpec_CanonicalConstructor_RejectsNonPositiveAttackCost(int attackCost)
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => CreateCanonicalSpec(attackCost: attackCost)).ParamName,
                Is.EqualTo("attackCost"));
        }

        [Test]
        public void UnitDefinition_CanonicalConstructor_UsesUnitSpecStaticStatNames()
        {
            UnitDefinition definition = CreateCanonicalDefinition(attackRange: 4);

            Assert.That(definition.MoveMethod, Is.EqualTo(MoveMethod.LiftAndPlace));
            Assert.That(definition.MoveRange, Is.EqualTo(5));
            Assert.That(definition.MoveCost, Is.EqualTo(1));
            Assert.That(definition.AttackRange, Is.EqualTo(4));
            Assert.That(definition.AttackPower, Is.EqualTo(3));
            Assert.That(definition.MaxActionCost, Is.EqualTo(8));
            Assert.That(definition.ActionPoint, Is.EqualTo(definition.MaxActionCost));
            Assert.That(definition.AttackCost, Is.EqualTo(2));
        }

        [Test]
        public void UnitDefinition_CanonicalConstructor_RejectsNegativeAttackRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => CreateCanonicalDefinition(attackRange: -1)).ParamName,
                Is.EqualTo("attackRange"));
        }

        [Test]
        public void UnitStatsSnapshot_CanonicalConstructor_AssignsStaticAndCurrentStats()
        {
            UnitStatsSnapshot snapshot = new UnitStatsSnapshot(
                MoveMethod.LiftAndPlace,
                5,
                1,
                4,
                3,
                0,
                8,
                2,
                2,
                3,
                30,
                35,
                Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Base));

            Assert.That(snapshot.MoveMethod, Is.EqualTo(MoveMethod.LiftAndPlace));
            Assert.That(snapshot.MoveRange, Is.EqualTo(5));
            Assert.That(snapshot.MoveCost, Is.EqualTo(1));
            Assert.That(snapshot.AppointRange, Is.EqualTo(4));
            Assert.That(snapshot.AttackRange, Is.EqualTo(snapshot.AppointRange));
            Assert.That(snapshot.AttackPower, Is.EqualTo(3));
            Assert.That(snapshot.MaxActionCost, Is.EqualTo(8));
            Assert.That(snapshot.ActionPoint, Is.EqualTo(snapshot.MaxActionCost));
            Assert.That(snapshot.AttackCost, Is.EqualTo(2));
            Assert.That(snapshot.RemainingMoveRange, Is.EqualTo(2));
            Assert.That(snapshot.CurrentActionCost, Is.EqualTo(3));
            Assert.That(snapshot.Priority, Is.EqualTo(30));
            Assert.That(snapshot.Delay, Is.EqualTo(35));
        }

        [Test]
        public void UnitStatsSnapshot_PreTimelineConstructor_UsesZeroTimelineStats()
        {
            UnitStatsSnapshot snapshot = new UnitStatsSnapshot(
                MoveMethod.LiftAndPlace,
                5,
                1,
                4,
                3,
                0,
                8,
                2,
                0,
                0,
                0,
                0,
                Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Base));

            Assert.That(snapshot.Priority, Is.Zero);
            Assert.That(snapshot.Delay, Is.Zero);
        }

        private static UnitSpec CreateCanonicalSpec(
            int moveCost = 1,
            int attackRange = 4,
            int attackPower = 3,
            int maxActionCost = 8,
            int attackCost = 2,
            int priority = 30,
            int delay = 35,
            UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both)
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                1,
                1,
                new[] { CellCoord.Zero },
                10,
                MoveMethod.LiftAndPlace,
                5,
                moveCost,
                attackRange,
                attackPower,
                maxActionCost,
                attackCost,
                priority,
                delay,
                ownerEligibility);
        }

        private static UnitDefinition CreateCanonicalDefinition(int attackRange) =>
            UnitStateTestFactory.CreateDefinition(
                EntityKey.Unit_Akimbo,
                CreateFootprint(),
                10,
                MoveMethod.LiftAndPlace,
                5,
                1,
                attackRange,
                3,
                8,
                2,
                30,
                35);

        private static TileFootprint CreateFootprint()
        {
            return new TileFootprint(1, 1, new[] { CellCoord.Zero });
        }
    }
}
