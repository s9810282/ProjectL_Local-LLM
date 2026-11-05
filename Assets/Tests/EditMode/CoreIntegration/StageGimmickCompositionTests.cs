using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class StageGimmickCompositionTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void RealEndOrdersGroupedTornadoRailwayHaboobRockfallAndStopsOnTerminal(bool terminal)
        {
            var source = new HaboobPublicationTests.Source(new StageGimmickSpec[] {
                new RockfallGimmickSpec(1, 1, 64, 64), new HaboobGimmickSpec(), new RailwayGimmickSpec(1, 1) },
                new[] { terminal ? CellCoord.Zero : new CellCoord(0, 1) }, tornado: true);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, source);
            var actorId = battle.Actor.EntityId;
            var before = battle.Buffers.CombatStateReadSink.Current.Gimmicks;
            RockfallPublicationTests.EndPlayer(battle);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var events = result.Events.ToList();
            int tornado = events.FindIndex(e => e is CombatTornadoGimmickEvent);
            int railway = events.FindIndex(e => e is CombatGimmickEvent);
            int haboob = events.FindIndex(e => e is CombatHaboobEvent);
            int rockfall = events.FindIndex(e => e is CombatRockfallEvent);
            Assert.That(tornado, Is.GreaterThanOrEqualTo(0));
            Assert.That(events.OfType<CombatTornadoGimmickEvent>().Count(), Is.EqualTo(1));
            Assert.That(railway, Is.GreaterThan(tornado));
            if (terminal)
            {
                Assert.That(haboob, Is.EqualTo(-1));
                Assert.That(rockfall, Is.EqualTo(-1));
                Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
                Assert.That(battle.Buffers.CombatStateReadSink.Current.Gimmicks.Get(new StageGimmickId(1)).SameValues(before.Get(new StageGimmickId(1))), Is.True);
                return;
            }
            Assert.That(haboob, Is.GreaterThan(railway));
            Assert.That(rockfall, Is.GreaterThan(haboob));
            Assert.That(battle.Actor.Health.IsDead, Is.True);
            var warning = events.OfType<CombatRockfallEvent>().Single().Final;
            CombatRockfallEvent impact = null;
            for (int steps = 0; battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.CompletedControllerTurnCount < 2; steps++)
            {
                Assert.That(steps, Is.LessThan(4));
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
                Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var latest), Is.True);
                impact = latest.Events.OfType<CombatRockfallEvent>().SingleOrDefault() ?? impact;
            }
            Assert.That(impact, Is.Not.Null);
            Assert.That(impact.Expected.SameValues(warning), Is.True);
            Assert.That(impact.RecipientIds.Contains(actorId), Is.False);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.CompletedControllerTurnCount, Is.EqualTo(2));
            Assert.That(battle.Actor.Health.IsDead, Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.OfType<RockfallGimmickState>().Single().Phase, Is.EqualTo(RockfallGimmickPhase.Cooldown));
            Assert.That(warning.Id, Is.EqualTo(new StageGimmickId(1)));
            Assert.That(HaboobPublicationTests.Cloud(battle).Id, Is.EqualTo(new StageGimmickId(2)));
        }

        [Test]
        public void DeploymentActionsPreserveScheduleAreaAndRandomState()
        {
            using var battle = HaboobPublicationTests.Create(enterCombat: false);
            var before = battle.Buffers.CombatStateReadSink.Current.CommandState;
            battle.Submit(CombatViewCommandData.ReturnUnit(battle.Actor.EntityId));
            battle.Submit(CombatViewCommandData.PlaceUnit(battle.Actor.EntityId, new CellCoord(0, 1), Rotation4.R0));
            var after = battle.Buffers.CombatStateReadSink.Current.CommandState;
            Assert.That(after.Turn.CompletedControllerTurnCount, Is.EqualTo(0));
            Assert.That(after.Gimmicks.SameValues(before.Gimmicks), Is.True);
            Assert.That(after.EnvironmentProgress.SameValues(before.EnvironmentProgress), Is.True);
        }
    }
}
