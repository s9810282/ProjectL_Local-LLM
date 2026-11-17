using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MeteorPublicationTests
    {
        [Test]
        public void StageSectionIsOptInAndConvertsTuningIntoBattleDefinition()
        {
            using var fixture = new CoreAuthoringFixture();
            var stage = fixture.Create<CombatStageData>();
            Assert.That(((CombatStageDetail)stage.GetData()).Meteor, Is.Null);
            var settings = new MeteorGimmickAuthoring();
            CoreAuthoringFixture.Set(settings, "enabled", true);
            CoreAuthoringFixture.Set(settings, "maximumHealthDamagePercent", 37);
            CoreAuthoringFixture.Set(settings, "fireAffectedSideStarts", 5);
            CoreAuthoringFixture.Set(stage, "meteor", settings);
            var data = ((CombatStageDetail)stage.GetData()).Meteor;
            Assert.That(data, Is.Not.SameAs(((CombatStageDetail)stage.GetData()).Meteor));
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1, Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(),
                    Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters, new CellCoord(1, 0), Rotation4.R0,
                    new CellCoord(3, 5), Rotation4.R180, null, meteor: data)));
            using var baseline = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config);
            var definition = CoreIntegrationTestData.DatabaseSource(baseline.Buffers).Read(database, 1, new StageKey(1101));
            var spec = definition.Stage.StageGimmicks.ToArray().OfType<MeteorGimmickSpec>().Single();
            Assert.That(spec.CycleControllerTurns, Is.EqualTo(4));
            Assert.That(spec.WarningLeadControllerTurns, Is.EqualTo(2));
            Assert.That(spec.MinimumMeteorCount, Is.EqualTo(2));
            Assert.That(spec.MaximumMeteorCount, Is.EqualTo(4));
            Assert.That(spec.FootprintSideLength, Is.EqualTo(3));
            Assert.That(spec.MaximumHealthDamagePercent, Is.EqualTo(37));
            Assert.That(spec.FireAffectedSideStarts, Is.EqualTo(5));
            Assert.That(spec.FlameControllerTurns, Is.EqualTo(4));
        }

        [Test]
        public void WarningImpactAndExpiryPublishOnExactBoundaries()
        {
            using var battle = Open(new MeteorGimmickSpec(4, 2, 2, 2, 1, 0, 3, 4));
            var warning = Reach(battle, 2).Events.OfType<CombatMeteorEvent>().Single();
            Assert.That(warning.Step, Is.EqualTo(MeteorGimmickStep.Warning));
            Assert.That(warning.Boundary.CompletedControllerTurnCount, Is.EqualTo(2));
            Assert.That(warning.Final.Warnings.Count, Is.EqualTo(2));
            Assert.That(Read(battle).SameValues(warning.Final), Is.True);
            uint rng = Current(battle).EnvironmentProgress.RandomState;

            var impact = Reach(battle, 4);
            var sites = impact.Events.OfType<CombatMeteorImpactEvent>().ToArray();
            Assert.That(sites.Select(site => site.Footprint), Is.EqualTo(warning.Final.Warnings));
            Assert.That(sites.All(site => !site.Cancelled), Is.True);
            Assert.That(Read(battle).Flames.Select(f => f.ExpiresAtBoundaryCount), Is.EqualTo(new long[] { 8, 8 }));
            Assert.That(Read(battle).Warnings, Is.Empty);
            Assert.That(Current(battle).EnvironmentProgress.RandomState, Is.EqualTo(rng));

            var events = Reach(battle, 8).Events.OfType<CombatMeteorEvent>().ToArray();
            Assert.That(events.Select(e => e.Step), Is.EqualTo(new[] { MeteorGimmickStep.Expired, MeteorGimmickStep.Resolved }));
            Assert.That(events[0].Expected.Flames.Select(f => f.Footprint), Is.EqualTo(warning.Final.Warnings));
            Assert.That(events[0].Final.Flames, Is.Empty);
            Assert.That(Read(battle).Flames.Select(f => f.ExpiresAtBoundaryCount), Is.EqualTo(new long[] { 12, 12 }));
        }

        [Test]
        public void NumericImpactAndFirePublishTogether()
        {
            // 8 × 6 판에서 본부 한 칸 빼고 전부 경고한다. 대상 위치를 난수에 맡기지 않는다.
            using var battle = Open(new MeteorGimmickSpec(4, 2, 47, 47, 1, 60, 3, 4));
            Reach(battle, 3);
            var before = battle.Actor;

            var impact = Reach(battle, 4);

            var hits = impact.Events.OfType<CombatMeteorImpactEvent>().Where(e => e.RecipientIds.Contains(before.EntityId)).ToArray();
            Assert.That(hits.Length, Is.EqualTo(1));
            Assert.That(battle.Actor.Health.CurrentHealth, Is.EqualTo(before.Health.CurrentHealth - before.Health.MaxHealth * 60 / 100));
            var fire = battle.Actor.StatusEffects.Entries.OfType<FireStatus>().Single();
            Assert.That(fire.Origin, Is.EqualTo(hits[0].Footprint.FireOrigin(hits[0].GimmickId)));
            Assert.That(fire.RemainingAffectedSideStarts, Is.EqualTo(3));
        }

        [Test]
        public void AnUnblockedWarnedSiteCannotBePublishedAsCancelled()
        {
            using var battle = Open(new MeteorGimmickSpec(4, 2, 47, 47, 1, 0, 3, 4));
            Reach(battle, 3);
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var input = battle.Publication().EnemyStep();
            var valid = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            var hit = valid.Events.OfType<CombatMeteorImpactEvent>().First(e => e.RecipientIds.Count == 0);
            var original = valid.Events.OfType<CombatMeteorEvent>().Single(e => e.Step == MeteorGimmickStep.Resolved);
            var final = new MeteorGimmickState(original.Final.Id, original.Final.NextBoundaryCount, original.Final.Warnings,
                original.Final.Flames.Where(f => !f.Footprint.SameValues(hit.Footprint)).ToArray());
            var cancelled = new CombatMeteorImpactEvent(hit.GimmickId, hit.Footprint, true, Array.Empty<EntityId>());
            var resolved = new CombatMeteorEvent(original.Step, original.Boundary, original.Expected, final);
            var forged = CombatCommandComposition.WithEvents(valid, valid.Events.Select(e =>
                ReferenceEquals(e, hit) ? cancelled : ReferenceEquals(e, original) ? resolved : e).ToArray())
                .WithFinalState(valid.FinalState.With(gimmicks: valid.FinalState.Gimmicks.Replace(final)));

            var stores = battle.CaptureStores();
            var error = Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            Assert.That(error.Message, Does.Contain("충돌 취소와 차단 칸"));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            battle.Buffers.BattleWriteSink.Apply(valid);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BufferRejectsMissingWarningOrRngChangedOutsideWarningEvidence(bool wrongRandom)
        {
            using var battle = Open(new MeteorGimmickSpec(2, 1, 1, 1, 1, 60, 3, 4));
            var input = HaboobPublicationTests.EndInput(battle);
            var valid = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(valid.Events.OfType<CombatMeteorEvent>().Single().Step, Is.EqualTo(MeteorGimmickStep.Warning));
            var random = valid.FinalState.EnvironmentProgress;
            var forged = wrongRandom ? valid.WithFinalState(valid.FinalState.With(environmentProgress:
                new EnvironmentProgressFacts(random.NextObjectEntryOrdinal, random.NextSourceId, random.RandomState ^ 1u))) :
                CombatCommandComposition.WithEvents(valid, valid.Events.Where(e => e is not CombatMeteorEvent).ToArray());

            var stores = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            battle.Buffers.BattleWriteSink.Apply(valid);
            Assert.That(Read(battle).Warnings.Count, Is.EqualTo(1));
        }

        [Test]
        public void RepeatedMovePreviewLeavesPublishedWarningAndRandomStateUnchanged()
        {
            using var battle = Open(new MeteorGimmickSpec(2, 1, 2, 2, 1, 0, 3, 4));
            Reach(battle, 1);
            battle.AdvanceToPlayerOrDay();
            var before = Current(battle);
            battle.PreviewMove(battle.Actor, battle.Actor.Anchor.Value, new CellCoord(1, 1));
            battle.PreviewMove(battle.Actor, battle.Actor.Anchor.Value, new CellCoord(1, 1));
            Assert.That(Current(battle).EnvironmentProgress.RandomState, Is.EqualTo(before.EnvironmentProgress.RandomState));
            Assert.That(Current(battle).Gimmicks.SameValues(before.Gimmicks), Is.True);
        }

        private static ConfiguredMovementBattle Open(MeteorGimmickSpec spec) =>
            new(ConfiguredMovementBattle.Config, new HaboobPublicationTests.Source(new StageGimmickSpec[] { spec }));

        private static CombatCommandState Current(ConfiguredMovementBattle battle) => battle.Buffers.CombatStateReadSink.Current.CommandState;
        private static MeteorGimmickState Read(ConfiguredMovementBattle battle) => Current(battle).Gimmicks.Entries.OfType<MeteorGimmickState>().Single();

        private static CombatResult Reach(ConfiguredMovementBattle battle, int boundary)
        {
            for (int step = 0; Current(battle).Turn.CompletedControllerTurnCount < boundary; step++)
            {
                Assert.That(step, Is.LessThan(256));
                if (Current(battle).Turn.ControllerPhase == CombatControllerPhase.Player) battle.Submit(battle.EndPlayerTurn());
                else Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            }
            Assert.That(Current(battle).Turn.CompletedControllerTurnCount, Is.EqualTo(boundary));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            return result;
        }
    }
}
