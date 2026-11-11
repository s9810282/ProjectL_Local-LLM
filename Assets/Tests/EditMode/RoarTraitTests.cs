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

        [Test]
        public void OwnSideTurnEnd_AddsConfiguredCount_BelowThresholdDoesNotRoar()
        {
            var input = WithCounts(BossTurnTests.WithRoar(BossTurnTests.BossInput(new CellCoord(3, 3)), 1), CombatSummonTests.Boss,
                perOwnEnd: 2);

            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.True);
            Assert.That(RoarHitCounter.HitCount(BossRoarTests.Final(result, CombatSummonTests.Boss)), Is.EqualTo(3));
            // 체력 변화가 없으므로 특성 상태 사건이 카운터 변경의 근거다.
            Assert.That(result.Events.OfType<CombatTraitStateEvent>().Any(e => e.Final.EntityId == CombatSummonTests.Boss), Is.True);
            Assert.That(result.Events.OfType<CombatRoarEvent>(), Is.Empty);
        }

        [Test]
        public void OwnSideTurnEnd_ReachingThreshold_RoarsAfterEnemyPhaseEnds()
        {
            var input = WithCounts(BossTurnTests.WithRoar(BossTurnTests.BossInput(new CellCoord(3, 3)), 4), CombatSummonTests.Boss,
                perOwnEnd: 1);

            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            var events = result.Events.ToList();
            int ended = events.FindIndex(e => e.Kind == CombatEventKind.EnemyPhaseEnded);
            int roared = events.FindIndex(e => e is CombatRoarEvent);
            Assert.That(ended, Is.GreaterThanOrEqualTo(0));
            Assert.That(roared, Is.GreaterThan(ended));
            Assert.That(events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(RoarHitCounter.HitCount(BossRoarTests.Final(result, CombatSummonTests.Boss)), Is.Zero);
        }

        [Test]
        public void OpposingSideTurnEnd_ReachingThreshold_RoarsAfterPlayerTurnEnds()
        {
            var input = CombatCommonCommandTests.Command(CombatCommandKind.EndPlayerTurn,
                WithCounts(BossRoarTests.Input(4), BossRoarTests.Boss, perOpposingEnd: 1));

            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            var events = result.Events.ToList();
            int ended = events.FindIndex(e => e.Kind == CombatEventKind.PlayerTurnEnded);
            int roared = events.FindIndex(e => e is CombatRoarEvent);
            Assert.That(ended, Is.GreaterThanOrEqualTo(0));
            Assert.That(roared, Is.GreaterThan(ended));
            Assert.That(RoarHitCounter.HitCount(BossRoarTests.Final(result, BossRoarTests.Boss)), Is.Zero);
        }

        [Test]
        public void TurnEnd_UsesOnlyTheEndedSidesCount()
        {
            // 플레이어 턴 종료는 보스에게 상대 진영 종료다. 자기 진영 값만 있으면 카운터가 그대로다.
            var input = CombatCommonCommandTests.Command(CombatCommandKind.EndPlayerTurn,
                WithCounts(BossRoarTests.Input(1), BossRoarTests.Boss, perOwnEnd: 3));

            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(RoarHitCounter.HitCount(BossRoarTests.Final(result, BossRoarTests.Boss)), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatTraitStateEvent>().Any(e => e.Final.EntityId == BossRoarTests.Boss), Is.False);
        }

        [TestCase(2, 3)]
        [TestCase(0, 1)]
        public void NightHit_AddsConfiguredCountPerHit(int perHit, int expected)
        {
            var result = CombatCommandCalculator.Calculate(WithCounts(BossRoarTests.Input(1), BossRoarTests.Boss, perHit: perHit),
                Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(RoarHitCounter.HitCount(BossRoarTests.Final(result, BossRoarTests.Boss)), Is.EqualTo(expected));
            Assert.That(result.Events.OfType<CombatRoarEvent>(), Is.Empty);
        }

        [Test]
        public void CountValues_FollowOneRule()
        {
            Assert.That(RoarCountRules.FindDefinitionError(5, 1, 0, 0), Is.Null);
            Assert.That(RoarCountRules.FindDefinitionError(5, 0, 0, 1), Is.Null);
            Assert.That(RoarCountRules.FindDefinitionError(5, 0, 0, 0), Is.Not.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoarTraitSpec(5, 0, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoarTraitSpec(5, countPerOwnSideTurnEnd: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoarTraitDefinition(5, -1, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RoarTraitStatusSnapshot(0, 5, countPerOpposingSideTurnEnd: -1));

            var definition = (RoarTraitDefinition)new RoarTraitDefinitionFactory().BuildDefinition(new RoarTraitSpec(5, 2, 0, 1));
            Assert.That((definition.CountPerHit, definition.CountPerOwnSideTurnEnd, definition.CountPerOpposingSideTurnEnd),
                Is.EqualTo((2, 0, 1)));
            var status = new RoarTraitStatusSnapshot(3, 5, 2, 0, 1);
            Assert.That(status.WithHitCount(0).SameValues(new RoarTraitStatusSnapshot(0, 5, 2, 0, 1)), Is.True);
            Assert.That(status.SameValues(new RoarTraitStatusSnapshot(3, 5)), Is.False);
        }

        /// <summary>보스 포효 상태의 증가량만 바꾼다. 카운터·임계값은 그대로다.</summary>
        private static CombatCommandInput WithCounts(CombatCommandInput input, EntityId bossId, int perHit = 1, int perOwnEnd = 0,
            int perOpposingEnd = 0) =>
            CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId != bossId ? c : CombatCommandTestData.With(c,
                traits: c.Stats.TraitStatuses.ToArray().Select(t => t is RoarTraitStatusSnapshot r
                    ? (TraitStatusSnapshot)new RoarTraitStatusSnapshot(r.HitCount, r.HitThreshold, perHit, perOwnEnd, perOpposingEnd)
                    : t).ToArray())).ToArray());
    }
}
