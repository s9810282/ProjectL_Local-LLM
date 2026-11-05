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
        [TestCase(EntityKey.Unit_Stage1Boss, UnitRole.Regular)]
        [TestCase(EntityKey.Unit_Gravekeeper, UnitRole.Regular)]
        [TestCase(EntityKey.Unit_Akimbo, UnitRole.Regular)]
        [TestCase(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter)]
        public void UnitDefinition_DefinedUnitKeysCanCreateRuntimeDefinition(EntityKey key, UnitRole role)
        {
            var definition = CreateIdentityDefinition(key, role);

            Assert.That(definition.Key, Is.EqualTo(key));
            Assert.That(definition.Role, Is.EqualTo(role));
        }

        [TestCase(EntityKey.None)]
        [TestCase(EntityKey.Structure_Headquarters)]
        [TestCase(EntityKey.Structure_PowderBarrel)]
        [TestCase(EntityKey.Structure_Supplies)]
        [TestCase((EntityKey)(-1))]
        [TestCase((EntityKey)9)]
        [TestCase((EntityKey)999)]
        public void UnitDefinition_NonUnitAndUndefinedKeysRemainRejected(EntityKey key)
        {
            var error = Assert.Throws<ArgumentException>(() => CreateIdentityDefinition(key, UnitRole.Regular));

            Assert.That(error.ParamName, Is.EqualTo("key"));
        }

        [TestCase(UnitOwnerEligibility.Both)]
        [TestCase(UnitOwnerEligibility.EnemyOnly)]
        public void UnitDefinition_MainRequiresPlayerOnlyEligibility(UnitOwnerEligibility eligibility)
        {
            Assert.Throws<InvalidOperationException>(() => UnitStateTestFactory.CreateDefinition(EntityKey.Unit_Akimbo,
                CreateFootprint(), 10, MoveMethod.LiftAndPlace, 2, 3, 30, role: UnitRole.MainCharacter, ownerEligibility: eligibility));
        }

        [Test]
        public void UnitDefinition_RetiredMainPlayerKeyIsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => CreateIdentityDefinition(EntityKey.Unit_MainCharacter, UnitRole.MainCharacter));
        }

        [TestCase(EntityKey.Unit_Stage1Boss)]
        [TestCase(EntityKey.Unit_Gravekeeper)]
        public void UnitDefinition_UndefinedRoleRemainsRejected(EntityKey key)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() => CreateIdentityDefinition(key, (UnitRole)999));

            Assert.That(error.ParamName, Is.EqualTo("role"));
        }

        private static UnitDefinition CreateIdentityDefinition(EntityKey key, UnitRole role)
        {
            return UnitStateTestFactory.CreateDefinition(key, CreateFootprint(), 10, MoveMethod.LiftAndPlace, 1, 3, 30, role: role, ownerEligibility: role == UnitRole.MainCharacter ? UnitOwnerEligibility.PlayerOnly : UnitOwnerEligibility.Both);
        }

        [Test]
        public void UnitSpec_CanonicalConstructor_AssignsStaticStats()
        {
            UnitSpec spec = CreateCanonicalSpec();

            Assert.That(spec.MoveMethod, Is.EqualTo(MoveMethod.LiftAndPlace));
            Assert.That(spec.AttackRange, Is.EqualTo(4));
            Assert.That(spec.AttackPower, Is.EqualTo(3));
            Assert.That(spec.Priority, Is.EqualTo(30));
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

        [Test]
        public void CanonicalUnitSpec_RejectsNegativePriority()
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                CreateCanonicalSpec(priority: -1)).ParamName, Is.EqualTo("priority"));
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
                EntityKey.Unit_Akimbo, 1, 1, new[] { CellCoord.Zero }, 10, MoveMethod.LiftAndPlace, 4, 3,
                30, Array.Empty<TraitSpec>(), categoryKey: category)).ParamName, Is.EqualTo("categoryKey"));
        }

        [Test]
        public void UnitSpec_CanonicalConstructor_RejectsNegativeAttackPower()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => CreateCanonicalSpec(attackPower: -1)).ParamName,
                Is.EqualTo("attackPower"));
        }


        [Test]
        public void UnitDefinition_CanonicalConstructor_UsesUnitSpecStaticStatNames()
        {
            UnitDefinition definition = CreateCanonicalDefinition(attackRange: 4);

            Assert.That(definition.MoveMethod, Is.EqualTo(MoveMethod.LiftAndPlace));
            Assert.That(definition.MoveRange, Is.EqualTo(3));
            Assert.That(definition.AttackRange, Is.EqualTo(4));
            Assert.That(definition.AttackPower, Is.EqualTo(3));
        }

        [Test]
        public void UnitDefinition_CanonicalConstructor_RejectsNegativeAttackRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => CreateCanonicalDefinition(attackRange: -1)).ParamName,
                Is.EqualTo("attackRange"));
        }

        [Test]
        public void UnitStatsSnapshot_AssignsPublishedStaticStats()
        {
            UnitStatsSnapshot snapshot = new UnitStatsSnapshot(
                MoveMethod.LiftAndPlace,                5,                4,                3,                0,                30,                Array.Empty<TraitStatusSnapshot>(),                CategorySnapshot.CreateStateless(CategoryKey.Main));

            Assert.That(snapshot.MoveMethod, Is.EqualTo(MoveMethod.LiftAndPlace));
            Assert.That(snapshot.MoveRange, Is.EqualTo(5));
            Assert.That(snapshot.AppointRange, Is.EqualTo(4));
            Assert.That(snapshot.AttackRange, Is.EqualTo(snapshot.AppointRange));
            Assert.That(snapshot.AttackPower, Is.EqualTo(3));
            Assert.That(snapshot.Priority, Is.EqualTo(30));
        }

        [Test]
        public void UnitStatsSnapshot_AllowsZeroPriorityWithoutAScheduler()
        {
            UnitStatsSnapshot snapshot = new UnitStatsSnapshot(
                MoveMethod.LiftAndPlace,                5,                4,                3,                0,                0,                Array.Empty<TraitStatusSnapshot>(),                CategorySnapshot.CreateStateless(CategoryKey.Main));

            Assert.That(snapshot.Priority, Is.Zero);
        }

        private static UnitSpec CreateCanonicalSpec(
            int attackRange = 4,
            int attackPower = 3,
            int priority = 30,
            UnitOwnerEligibility ownerEligibility = UnitOwnerEligibility.Both)
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                1,
                1,
                new[] { CellCoord.Zero },
                10,
                MoveMethod.LiftAndPlace,
                attackRange,
                attackPower,
                priority,
                ownerEligibility);
        }

        private static UnitDefinition CreateCanonicalDefinition(int attackRange) =>
            UnitStateTestFactory.CreateDefinition(
                EntityKey.Unit_Akimbo,
                CreateFootprint(),
                10,
                MoveMethod.LiftAndPlace,
                attackRange,
                3,
                30);

        private static TileFootprint CreateFootprint()
        {
            return new TileFootprint(1, 1, new[] { CellCoord.Zero });
        }
    }
}
