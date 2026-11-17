using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Tests.Core
{
    public sealed class StatusEffectRetirementTests
    {
        [Test]
        public void Intimidation_ExposesAttackReductionWithoutPriorityConfigurationOrPayload()
        {
            foreach (var type in new[] { typeof(IntimidationTraitSpec), typeof(IntimidationTraitDefinition), typeof(IntimidationTraitStatusSnapshot) })
            {
                Assert.That(type.GetProperty("PriorityReduction"), Is.Null, type.Name);
                Assert.That(type.GetConstructor(new[] { typeof(int) }), Is.Not.Null, type.Name);
            }
            Assert.That(typeof(IntimidationDebuffState).GetProperty("PriorityPenalty"), Is.Null);
            Assert.That(typeof(AttackDecreaseStatus).GetProperty("LegacyPriorityPenalty"), Is.Null);
            Assert.That(typeof(CombatDebuffCalculator).GetMethod("EvaluatePriority", BindingFlags.Static | BindingFlags.NonPublic), Is.Null);
        }

        [Test]
        public void Intimidation_ReapplicationKeepsFirstReductionAndGeneralPriorityUntilCompletion()
        {
            var first = CombatDebuffCalculator.ApplyIntimidation(CharacterDebuffState.Empty, new IntimidationTraitSpec(20));
            var repeated = CombatDebuffCalculator.ApplyIntimidation(first, new IntimidationTraitSpec(30));
            Assert.That(repeated, Is.SameAs(first));
            Assert.That(CombatDebuffCalculator.EvaluateAttackPower(100, repeated), Is.EqualTo(80));
            var actor = CombatCommandTestData.Input().Characters[0];
            actor = actor.With(stats: actor.Stats.With(priority: 17), debuffs: repeated);
            Assert.That(CombatEffectiveStatsCalculator.Priority(actor), Is.EqualTo(17));
            Assert.That(CombatDebuffCalculator.EvaluateAttackPower(100, CombatDebuffCalculator.ClearIntimidation(repeated)), Is.EqualTo(100));
            Assert.That(first.Intimidation.AttackReductionPercent, Is.EqualTo(20));
        }

        [Test]
        public void DiscountReservedIdentity_IsRetiredAndCannotEnterRegistryOrGrowth()
        {
            Assert.That((int)TraitKey.MoveAttackDiscount, Is.EqualTo(20));
            Assert.That(DeferredCombatFeatures.IsTraitRetired(TraitKey.MoveAttackDiscount), Is.True);
            Assert.That(DeferredCombatFeatures.GetTraitUnavailableReason(TraitKey.MoveAttackDiscount), Is.EqualTo("Retired"));
            var registry = TraitRegistry.CreateDefault();
            Assert.That(registry.OrderedRegistrations.Any(r => r.TraitKey == TraitKey.MoveAttackDiscount), Is.False);
            Assert.That(Assert.Throws<InvalidOperationException>(() => registry.GetRequiredRegistration(TraitKey.MoveAttackDiscount)).Message, Does.Contain("Retired"));
            var spec = new ReservedTraitSpec();
            Assert.That(Assert.Throws<InvalidOperationException>(() => registry.BuildDefinition(spec)).Message, Does.Contain("Retired"));
            var catalog = TraitGrowthFixtures.Catalog(Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements.TraitGrowthStepSpec>(),
                definitions: new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements.TraitGrowthDefinition(spec, null, "reserved", "reserved", "", "") });
            Assert.That(Assert.Throws<InvalidOperationException>(() => TraitGrowthCatalogValidator.Validate(catalog, new[] { TraitGrowthFixtures.Profile() })).Message,
                Does.Contain("Retired"));
            Assert.That(typeof(TraitSpec).Assembly.GetType(typeof(TraitSpec).Namespace + ".MoveAttackDiscountTraitSpec"), Is.Null);
        }

        [Test]
        public void Availability_RetainsSurvivorsDeferredFeaturesAndEveryReservedKey()
        {
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(TraitKey.Regeneration), Is.True);
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(TraitKey.Intimidation), Is.True);
            Assert.That(DeferredCombatFeatures.GetTraitAvailability(TraitKey.Intimidation).PartialUnavailableReason, Is.Empty);
            foreach (var key in new[] { TraitKey.Necromancy, TraitKey.Focus })
            {
                Assert.That(DeferredCombatFeatures.IsTraitAvailable(key), Is.False);
                Assert.That(DeferredCombatFeatures.GetTraitUnavailableReason(key), Is.EqualTo("DeferredUnitTurnTiming"));
            }
            var retired = new[] { TraitKey.MoveAttackDiscount, TraitKey.ThornField, TraitKey.DoorBreaching, TraitKey.ContemptForWeak,
                TraitKey.PrecisionShot, TraitKey.DoubleShot, TraitKey.LuckyBullet, TraitKey.ChainExplosion };
            foreach (var key in retired) Assert.That(DeferredCombatFeatures.IsTraitRetired(key), Is.True);
            Assert.That(retired.Select(k => (int)k), Is.EqualTo(new[] { 20, 40, 130, 140, 150, 160, 170, 180 }));
            Assert.That(DeferredCombatFeatures.IsFlagPickupAvailable, Is.False);
        }

        private sealed class ReservedTraitSpec : TraitSpec
        {
            internal ReservedTraitSpec() : base(TraitKey.MoveAttackDiscount) { }

            protected override bool SameValuesCore(TraitSpec other) => true;
        }
    }
}
