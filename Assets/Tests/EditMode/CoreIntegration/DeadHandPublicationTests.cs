using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class DeadHandPublicationTests
    {
        [Test]
        public void PublishedHeaderAndMovementPrecedeGroupedDamageAndInvalidateUndo()
        {
            using var battle = DeadHandRequestTests.CreateBattle();
            var state = battle.Buffers.CombatStateReadSink;
            Assert.That(state.Current.CommandState.MovementUndo.TryGet(battle.Actor.EntityId, out _), Is.True);

            battle.Submit(DeadHandRequestTests.Command(battle));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var attack = result.Attack.Value;
            var events = result.Events.ToList();
            var header = events.OfType<CombatSweepEvent>().Single();
            var occurrence = events.OfType<CombatAttackEvent>().Single();
            var moved = events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved).ToArray();
            var damage = events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.HealthChanged);

            Assert.That(header.Sweep, Is.SameAs(attack.Sweep));
            Assert.That(header.AttackGroup, Is.EqualTo(attack.AttackGroup).And.Positive);
            Assert.That(header.Sweep.Origin.DefinitionKey, Is.EqualTo((int)TraitKey.DeadHand));
            Assert.That(moved.Length, Is.EqualTo(2));
            Assert.That(moved.All(e => e.MovementCause.Agency == CombatMovementAgency.Forced), Is.True);
            Assert.That(moved.All(e => events.IndexOf(header) < events.IndexOf(e) && events.IndexOf(e) < events.IndexOf(occurrence)), Is.True);
            Assert.That(events.IndexOf(occurrence), Is.LessThan(events.IndexOf(damage)));
            Assert.That(damage.DamageOrigins.Any(o => o.AttackGroup == header.AttackGroup), Is.True);
            Assert.That(state.Current.CommandState.MovementUndo.TryGet(battle.Actor.EntityId, out _), Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)header.Sweep.Targets.Single().EnteredCells)[0] = CellCoord.Zero);
            Assert.That(header.Sweep.Targets.Single().EnteredCells, Is.EqualTo(new[] { new CellCoord(2, 3), new CellCoord(3, 3) }));
        }

        [Test]
        public void PublishedTraitSettingsAndAttackUseTheSameResolvedRank()
        {
            using var battle = DeadHandRequestTests.CreateBattle(rankTwo: true);
            var settings = battle.Buffers.CombatStateReadSink.Current.TraitSettings.For(battle.Actor);
            Assert.That(settings.TryGet<DeadHandTraitSpec>(out var spec), Is.True);
            Assert.That(spec.DamagePercent, Is.EqualTo(140));

            var buffers = battle.Buffers;
            buffers.CombatViewWriteSink.SetPreview(DeadHandRequestTests.Preview(battle));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Attack.Value.Targets.Span[0].DamageBeforeMatchup, Is.EqualTo(7));
            battle.Submit(DeadHandRequestTests.Command(battle));
            Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt,
                Is.EqualTo(preview.Attack.Value.Targets.Span[0].ExpectedDamage));
        }

        [Test]
        public void LethalEntryPublishesInterruptionAndOutcomeWithoutOrdinaryDamage()
        {
            var landing = new CellCoord(1, 2);
            using var battle = DeadHandRequestTests.CreateBattle(
                new MineSourceFact(21, landing, 1, 100, true, new[] { Owner.Player, Owner.Enemy }), terminal: true);
            battle.Submit(DeadHandRequestTests.Command(battle));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var events = result.Events.ToList();
            var header = events.OfType<CombatSweepEvent>().Single();
            var activation = events.OfType<CombatObjectActivationEvent>().Single();
            Assert.That(header.Sweep.Interrupted, Is.True);
            Assert.That(header.Sweep.Targets.Single().Stop, Is.EqualTo(AttackSweepStop.Interrupted));
            Assert.That(header.Sweep.Targets.Single().EnteredCells, Is.EqualTo(new[] { landing }));
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            Assert.That(result.Outcome.HasValue, Is.True);
            Assert.That(events.IndexOf(header), Is.LessThan(events.IndexOf(activation)));
            Assert.That(events.Any(e => e.Kind == CombatEventKind.StageEnded), Is.True);
        }

        [Test]
        public void FailureAfterFirstForcedEntryLeavesAllStoresAndCommandCursorIntact()
        {
            using var battle = DeadHandRequestTests.CreateBattle(DeadHandRequestTests.Mine());
            var services = battle.Publication();
            var rule = new DeadHandRequestTests.DuringMineEntry(step =>
            {
                Assert.That(step.Anchor, Is.EqualTo(DeadHandRequestTests.MineCell));
                Assert.That(battle.Character(EntityKey.Unit_Demon).Anchor, Is.EqualTo(new CellCoord(1, 3)));
                throw new InvalidOperationException("sweep-entry test fault");
            });
            var service = new CombatCommandService(services.Assembler, services.Publisher, new[] { rule });
            service.PostBindService(DeadHandRequestTests.Reader(battle));
            int number = battle.Buffers.CombatViewWriteSink.Submit(DeadHandRequestTests.Command(battle));
            var before = battle.CaptureStores();

            Assert.That(Assert.Throws<InvalidOperationException>(() => service.ProcessNextCommand()).Message,
                Is.EqualTo("sweep-entry test fault"));
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var retried), Is.True);
            Assert.That(retried.CommandNumber, Is.EqualTo(number));
            Assert.That(retried.Events.OfType<CombatObjectActivationEvent>().Count(), Is.EqualTo(1));
        }

    }
}
