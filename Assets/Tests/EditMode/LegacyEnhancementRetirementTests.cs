using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class LegacyEnhancementRetirementTests
    {
        [TestCase(TraitKey.ThornField)]
        [TestCase(TraitKey.DoorBreaching)]
        [TestCase(TraitKey.ContemptForWeak)]
        [TestCase(TraitKey.PrecisionShot)]
        [TestCase(TraitKey.DoubleShot)]
        [TestCase(TraitKey.LuckyBullet)]
        [TestCase(TraitKey.ChainExplosion)]
        public void RetiredTrait_IsUnavailableWithRetiredReason(TraitKey key)
        {
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(key), Is.False);
            Assert.That(DeferredCombatFeatures.IsTraitRetired(key), Is.True);
            Assert.That(DeferredCombatFeatures.GetTraitUnavailableReason(key), Is.EqualTo("Retired"));
            var availability = DeferredCombatFeatures.GetTraitAvailability(key);
            Assert.That(availability.IsAvailable, Is.False);
            Assert.That(availability.UnavailableReason, Is.EqualTo("Retired"));
            Assert.That(availability.PartialUnavailableReason, Is.Empty);
        }

        [TestCase(TraitKey.ThornField)]
        [TestCase(TraitKey.DoorBreaching)]
        [TestCase(TraitKey.ContemptForWeak)]
        [TestCase(TraitKey.PrecisionShot)]
        [TestCase(TraitKey.DoubleShot)]
        [TestCase(TraitKey.LuckyBullet)]
        [TestCase(TraitKey.ChainExplosion)]
        public void RetiredTrait_ActivePoolRejectedBeforeConstruction(TraitKey key)
        {
            var spec = new ReservedTraitSpec(key);
            var catalog = TraitGrowthFixtures.Catalog(Array.Empty<TraitGrowthStepSpec>(), definitions: new[]
            {
                new TraitGrowthDefinition(spec, null, "reserved", "reserved", "", "")
            });
            var failure = Assert.Throws<InvalidOperationException>(() =>
                TraitGrowthCatalogValidator.Validate(catalog, new[] { TraitGrowthFixtures.Profile() }));
            Assert.That(failure.Message, Does.Contain("Retired"));
            failure = Assert.Throws<InvalidOperationException>(() => TraitRegistry.CreateDefault().BuildDefinition(spec));
            Assert.That(failure.Message, Does.Contain("Retired"));
        }

        [Test]
        public void SerializedReservations_AreNotReused()
        {
            Assert.That(new[] { (int)TraitKey.DoorBreaching, (int)TraitKey.ContemptForWeak,
                (int)TraitKey.PrecisionShot, (int)TraitKey.DoubleShot, (int)TraitKey.LuckyBullet,
                (int)TraitKey.ChainExplosion }, Is.EqualTo(new[] { 130, 140, 150, 160, 170, 180 }));
            Assert.That(new[] { (int)EnhancementKey.LuckyBullet, (int)EnhancementKey.DoubleShot,
                (int)EnhancementKey.DoorBreaching, (int)EnhancementKey.ContemptForWeak,
                (int)EnhancementKey.ChainExplosion, (int)EnhancementKey.PoisonThorn,
                (int)EnhancementKey.PrecisionShot }, Is.EqualTo(new[] { 1, 3, 4, 5, 6, 7, 8 }));
            Assert.That(Enum.IsDefined(typeof(TraitKey), 120), Is.False);
            var values = Enum.GetValues(typeof(TraitKey)).Cast<TraitKey>().ToArray();
            Assert.That(values.Distinct().Count(), Is.EqualTo(values.Length));
        }

        [Test]
        public void ChangeRiderIdentity_IsPreserved()
        {
            Assert.That((int)EnhancementKey.ChangeRider, Is.EqualTo(2));
            Assert.That(Enum.GetNames(typeof(TraitKey)), Has.No.Member("ChangeRider"));
        }

        [Test]
        public void NativeKillChain_RetainsCreditedDeathsRewardsAndDeterministicEvents()
        {
            var input = CombatTraitTestFixtures.KillChainInput();
            input = CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId.Value != 4).Select(c =>
                c.EntityId.Value == 3 ? CombatCommandTestData.With(c, hp: 1) : c).ToArray());
            input = CombatTraitTestFixtures.WithRewards(input);
            var result = CombatTraitTestFixtures.Run(input);
            var repeat = CombatTraitTestFixtures.Run(input);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(2));
            Assert.That(result.FinalState.Rewards.KillGoldEarned, Is.EqualTo(14));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Experience.Earned, Is.EqualTo(20));
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(repeat.FinalState.EnvironmentProgress.RandomState));
            Assert.That(result.Events.Select(e => e.Kind), Is.EqualTo(repeat.Events.Select(e => e.Kind)));
            // 처치 연쇄 추가 공격도 공격 행동으로 센다(2026-11-02 결정).
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount,
                Is.EqualTo(1 + TraitActivationOutcomeTests.Activations(result.Events, TraitKey.KillChainAttack).Count()));
        }

        [Test]
        public void NativeKillChain_RecoveryIsScopedToEachRealStrike()
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            var actor = input.Characters[0].With(stats: input.Characters[0].Stats.With(attackPower: 1));
            input = TraitCombatFixtures.Characters(input, actor, input.Characters[1],
                CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy), hp: 1),
                CombatCommandTestData.With(CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy), hp: 1),
                CombatItemDirectDamageTests.Equip(CombatCommandTestData.Character(8, EntityKey.Unit_Demon, new CellCoord(0, 7), Owner.Enemy), 40, 1));
            input = CombatItemDirectDamageTests.Items(TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new KillChainAttackTraitSpec()));
            var result = CombatTraitTestFixtures.Run(input);
            var outcomes = CombatTraitTestFixtures.Activations(result, TraitKey.KillChainAttack).Select(t => t.ChildAttack).ToArray();
            Assert.That(outcomes.Length, Is.EqualTo(2));
            var first = outcomes[0].Hits.Single();
            var second = outcomes[1].Hits.Single();
            Assert.That(first.TargetId, Is.EqualTo(new EntityId(8)));
            Assert.That((first.TargetHealthBefore, first.TargetHealthAfter, first.HealthRecovery), Is.EqualTo((40, 49, 10)));
            Assert.That(second.TargetId, Is.EqualTo(first.TargetId));
            Assert.That((second.TargetHealthBefore, second.TargetHealthAfter, second.HealthRecovery, second.DamageDealt), Is.EqualTo((49, 48, 0, 1)));
        }

        [Test]
        public void SurvivingRegistry_PreservesAllNewAndDeferredIdentities()
        {
            var registrations = TraitRegistry.CreateDefault().OrderedRegistrations;
            Assert.That(registrations.Count(r => DeferredCombatFeatures.IsTraitAvailable(r.TraitKey)), Is.EqualTo(43));
            Assert.That(registrations.Count(r => !DeferredCombatFeatures.IsTraitAvailable(r.TraitKey) &&
                !DeferredCombatFeatures.IsImplementationPending(r.TraitKey)), Is.EqualTo(2));
            Assert.That(registrations.Where(r => DeferredCombatFeatures.IsImplementationPending(r.TraitKey)).Select(r => r.TraitKey),
                Is.Empty);
            Assert.That(registrations.Count(r => (int)r.TraitKey >= 220), Is.EqualTo(33));
            Assert.Throws<ArgumentOutOfRangeException>(() => DeferredCombatFeatures.IsTraitAvailable((TraitKey)9999));
            Assert.Throws<ArgumentOutOfRangeException>(() => DeferredCombatFeatures.IsTraitRetired(TraitKey.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => DeferredCombatFeatures.IsTraitRetired((TraitKey)9999));
        }

        private sealed class ReservedTraitSpec : TraitSpec
        {
            internal ReservedTraitSpec(TraitKey key) : base(key)
            {
            }

            protected override bool SameValuesCore(TraitSpec other) => true;
        }
    }
}
