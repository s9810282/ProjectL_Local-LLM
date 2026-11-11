using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    /// <summary>적 전용 사망 특성(재구성·자폭·공포)의 Phase A 기반 계약입니다. 전투 동작은 아직 없습니다.</summary>
    public sealed class EnemyDeathTraitFoundationTests
    {
        private static readonly TraitKey[] DeathTraits = { TraitKey.Reconstruction, TraitKey.SelfDestruct, TraitKey.Fear };

        [Test]
        public void TraitKeys_UseTheReservedNumbers()
        {
            Assert.That((int)TraitKey.Reconstruction, Is.EqualTo(490));
            Assert.That((int)TraitKey.SelfDestruct, Is.EqualTo(500));
            Assert.That((int)TraitKey.Fear, Is.EqualTo(510));
        }

        [Test]
        public void OwnerPolicy_OnlyTheDeathTraitsAreEnemyOnly()
        {
            foreach (TraitKey key in Enum.GetValues(typeof(TraitKey)))
                Assert.That(TraitOwnerPolicy.IsEnemyOnly(key), Is.EqualTo(DeathTraits.Contains(key)), key.ToString());
        }

        [TestCase(1, 0, false, 50)]
        [TestCase(2, 50, false, 75)]
        [TestCase(3, 50, true, 110)]
        public void Levels_ResolveTheDesignedValues(int level, int shieldPercent, bool pushes, int selfDestructPercent)
        {
            Assert.That(EnemyDeathTraitLevels.IsValid(level), Is.True);
            Assert.That(EnemyDeathTraitLevels.ReconstructionShieldPercent(level), Is.EqualTo(shieldPercent));
            Assert.That(EnemyDeathTraitLevels.ReconstructionPushesOnRevive(level), Is.EqualTo(pushes));
            Assert.That(EnemyDeathTraitLevels.SelfDestructDamagePercent(level), Is.EqualTo(selfDestructPercent));
        }

        [TestCase(0)]
        [TestCase(4)]
        public void Levels_RejectOutOfRange(int level)
        {
            Assert.That(EnemyDeathTraitLevels.IsValid(level), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyDeathTraitLevels.ReconstructionShieldPercent(level));
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyDeathTraitLevels.ReconstructionPushesOnRevive(level));
            Assert.Throws<ArgumentOutOfRangeException>(() => EnemyDeathTraitLevels.SelfDestructDamagePercent(level));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ReconstructionTraitSpec(level));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SelfDestructTraitSpec(level));
        }

        [Test]
        public void Specs_CompareByLevel()
        {
            Assert.That(new ReconstructionTraitSpec(2).SameValues(new ReconstructionTraitSpec(2)), Is.True);
            Assert.That(new ReconstructionTraitSpec(2).SameValues(new ReconstructionTraitSpec(3)), Is.False);
            Assert.That(new SelfDestructTraitSpec(1).SameValues(new SelfDestructTraitSpec(1)), Is.True);
            Assert.That(new SelfDestructTraitSpec(1).SameValues(new SelfDestructTraitSpec(2)), Is.False);
            Assert.That(new FearTraitSpec().SameValues(new FearTraitSpec()), Is.True);
            Assert.That(new FearTraitSpec().TraitKey, Is.EqualTo(TraitKey.Fear));
        }

        [TestCase(TraitKey.Fear)]
        [TestCase(TraitKey.SelfDestruct)]
        [TestCase(TraitKey.Reconstruction)]
        public void DeathTraits_AreAvailableAndRegistered(TraitKey key)
        {
            Assert.That(DeferredCombatFeatures.IsImplementationPending(key), Is.False);
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(key), Is.True);
            Assert.That(TraitRegistry.CreateDefault().OrderedRegistrations.Select(r => r.TraitKey), Has.Member(key));
        }

        [Test]
        public void UnitCatalog_BuildsEnemyOnlyUnitsWithDeathTraits()
        {
            var spec = new UnitSpec(EntityKey.Unit_Demon, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 2, CategoryKey.Heavy, 5, 0, 1,
                new TraitSpec[] { new ReconstructionTraitSpec(2), new SelfDestructTraitSpec(3), new FearTraitSpec() },
                UnitOwnerEligibility.EnemyOnly);
            var definition = new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault()).GetRequired(spec.Key);
            Assert.That(definition.TraitDefinitions.ToArray().Select(t => t.TraitKey),
                Is.EqualTo(new[] { TraitKey.Reconstruction, TraitKey.SelfDestruct, TraitKey.Fear }));
        }

        [Test]
        public void GrowthCatalog_RejectsADeathTraitAsCategoryDefault()
        {
            var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories,
                new[] { new TraitGrowthDefinition(new FearTraitSpec(), null, "n", "d", "", "") },
                new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Heavy] = TraitKey.Fear });
            var error = Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.ValidateDefaultTraits(catalog));
            Assert.That(error.Message, Does.Contain("적 전용"));
        }

        [Test]
        public void GrowthCatalog_RejectsADeathTraitDefinition()
        {
            var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories,
                new[] { new TraitGrowthDefinition(new SelfDestructTraitSpec(1), null, "n", "d", "", "") });
            var error = Assert.Throws<InvalidOperationException>(() =>
                TraitGrowthCatalogValidator.Validate(catalog, Array.Empty<CombatAttackProfile>()));
            Assert.That(error.Message, Does.Contain("EnemyOnly"));
        }
    }
}
