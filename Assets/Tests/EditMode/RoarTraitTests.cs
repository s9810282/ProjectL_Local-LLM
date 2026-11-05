using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    /// <summary>포효 Trait 등록·변환과 카운터 보존을 확인한다(계획 T5.1, D4·D13).</summary>
    public sealed class RoarTraitTests
    {
        [TestCase(1)]
        [TestCase(5)]
        [TestCase(7)]
        public void RoarTrait_RoundTripRetainsHitCountAndDefinitionThreshold(int threshold)
        {
            TraitRegistry registry = TraitRegistry.CreateDefault();
            TraitRegistration registration = registry.GetRequiredRegistration(TraitKey.Roar);
            Assert.That(registration.SpecType, Is.EqualTo(typeof(RoarTraitSpec)));
            Assert.That(registration.DefinitionType, Is.EqualTo(typeof(RoarTraitDefinition)));
            Assert.That(registration.RuntimeStateType, Is.EqualTo(typeof(RoarRuntimeState)));

            var catalog = new UnitCatalog(new[] { CombatCommandTestData.Spec(EntityKey.Unit_Demon, 5, CategoryKey.Main, new RoarTraitSpec(threshold)) }, registry);
            UnitDefinition definition = catalog.GetRequired(EntityKey.Unit_Demon);
            Assert.That(((RoarTraitDefinition)definition.TraitDefinitions.Span[0]).HitThreshold, Is.EqualTo(threshold));
            var initialStatus = new TraitEffectProvider(registry).CreateStatuses(definition,
                new UnitTraitStateSet(new TraitRuntimeState[] { new RoarRuntimeState(0) })).OfType<RoarTraitStatusSnapshot>().Single();
            Assert.That(initialStatus.HitCount, Is.Zero);
            Assert.That(initialStatus.HitThreshold, Is.EqualTo(threshold));

            var initial = (RoarRuntimeState)registration.TraitRuntimeStateFactory.BuildRuntimeState(definition.TraitDefinitions.Span[0]);
            Assert.That(initial.HitCount, Is.Zero);

            var state = new RoarRuntimeState(3);
            var clone = (RoarRuntimeState)state.Clone();
            Assert.That(clone, Is.Not.SameAs(state));
            Assert.That(clone.HitCount, Is.EqualTo(3));
            initial.CopyFinalValuesFrom(clone);
            Assert.That(initial.HitCount, Is.EqualTo(3));
            Assert.Throws<ArgumentException>(() => state.CopyFinalValuesFrom(new KnockbackRuntimeState()));

            IEffectStateSnapshot captured = TraitEffectStateCodec.Capture(state);
            Assert.That(captured.ValueEquals(new RoarEffectStateSnapshot(3)), Is.True);
            Assert.That(captured.ValueEquals(new RoarEffectStateSnapshot(4)), Is.False);
            Assert.That(((RoarRuntimeState)TraitEffectStateCodec.Restore(captured)).HitCount, Is.EqualTo(3));

            var statuses = new TraitEffectProvider(registry).CreateStatuses(definition, new UnitTraitStateSet(new TraitRuntimeState[] { state }));
            var status = (RoarTraitStatusSnapshot)statuses.Single();
            Assert.That(status.HitCount, Is.EqualTo(3));
            Assert.That(status.HitThreshold, Is.EqualTo(threshold));
            Assert.That(status.SameValues(new RoarTraitStatusSnapshot(3, threshold)), Is.True);
            Assert.That(status.SameValues(new RoarTraitStatusSnapshot(0, threshold)), Is.False);
            Assert.That(status.SameValues(new RoarTraitStatusSnapshot(3, threshold + 1)), Is.False);
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoarTraitSpec(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoarTraitStatusSnapshot(-1, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoarTraitStatusSnapshot(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoarTraitStatusSnapshot(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoarRuntimeState(-1));
        }

        [Test]
        public void RoarStatus_CapturesIntoEffectBindings()
        {
            var input = BossRoarTests.Input(2);
            var boss = input.Characters.Single(c => c.EntityId == BossRoarTests.Boss);

            var bindings = CombatCommandEffects.Capture(boss, CombatCommandCalculator.RequiredProfile(input, boss));

            Assert.That(bindings.Length, Is.EqualTo(2));
        }

        [TestCase(3)]
        [TestCase(5)]
        [TestCase(7)]
        public void TurnEnd_DoesNotResetCounterOrThreshold(int threshold)
        {
            // 보스 자기 턴의 시작·종료 훅이 카운터를 바꾸지 않는다(사령술 턴 = 소환 시도 + EndTurn).
            var input = BossTurnTests.WithRoar(BossTurnTests.BossInput(new CellCoord(3, 3)), 2, threshold);

            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(RoarHitCounter.HitCount(BossRoarTests.Final(result, CombatSummonTests.Boss)), Is.EqualTo(2));
            Assert.That(BossRoarTests.Final(result, CombatSummonTests.Boss).Stats.TraitStatuses.ToArray()
                .OfType<RoarTraitStatusSnapshot>().Single().HitThreshold, Is.EqualTo(threshold));
        }
    }
}
