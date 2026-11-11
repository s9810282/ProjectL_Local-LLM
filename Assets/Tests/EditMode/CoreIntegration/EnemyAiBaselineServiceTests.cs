using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class EnemyAiBaselineServiceTests
    {
        [TestCase("N5", 5)]
        [TestCase("N6", 6)]
        [TestCase("N8", 8)]
        public void OpenEncounter_MatchesDeclaredGeometryAndPublishesOneAdvance(string fixtureId, int enemyCount)
        {
            using var battle = EnemyAiPreparationTestData.Create(fixtureId, 7);
            var b = battle.Buffers;
            var state = b.CombatStateReadSink.Current;
            Assert.That(state.BoardWidth, Is.EqualTo(16));
            Assert.That(state.BoardHeight, Is.EqualTo(16));
            Assert.That(state.TimelineSeed, Is.EqualTo(7));
            Assert.That(state.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
            var enemies = b.CharacterReadSink.Characters.Where(c => c.Owner == Owner.Enemy).OrderBy(c => c.EntityId).ToArray();
            Assert.That(enemies.Length, Is.EqualTo(enemyCount));
            Assert.That(enemies.Select(c => c.Anchor), Is.EqualTo(Enumerable.Range(0, enemyCount)
                .Select(index => new CellCoord(5 + index % 4, 1 + index / 4))));
            Assert.That(enemies.Select(c => c.Health.CurrentHealth), Is.All.EqualTo(100));
            Assert.That(enemies.Select(c => c.Stats.AppointRange), Is.All.EqualTo(4));
            var players = b.CharacterReadSink.Characters.Where(c => c.Owner == Owner.Player).ToArray();
            Assert.That(players.Select(c => c.Anchor), Is.EquivalentTo(new[] { CellCoord.Zero, new CellCoord(0, 1),
                new CellCoord(2, 0), new CellCoord(4, 0) }));

            int handled = b.CombatStateReadSink.HandledCommandNumber;
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            int presented = b.CombatViewReadSink.PresentedResultSequence;
            int pending = b.CombatViewWriteSink.Submit(battle.EndPlayerTurn());
            var queued = b.CombatViewReadSink.Commands.ToArray();
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Sequence, Is.EqualTo(sequence + 1));
            Assert.That(result.Kind, Is.EqualTo(CombatProcessingKind.Advance));
            Assert.That(result.ActorId, Is.EqualTo(enemies[0].EntityId));
            Assert.That(result.CommandNumber, Is.Zero);
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(presented));
            Assert.That(b.CombatViewReadSink.Commands, Is.EqualTo(queued));
            Assert.That(b.CombatViewReadSink.TryGetNextCommand(handled, out var next), Is.True);
            Assert.That(next.Number, Is.EqualTo(pending));
        }

        [Test]
        public void PlayerInputAndTerminalRun_DoNotPublish()
        {
            using var player = EnemyAiPreparationTestData.Create("N5", 1, startEnemy: false);
            var beforePlayer = player.CaptureStores();
            Assert.That(player.Core.CombatAdvance.AdvanceCombat(), Is.False);
            Assert.That(player.CaptureStores(), Is.EqualTo(beforePlayer));

            using var terminal = EnemyAiPreparationTestData.Create("N5", 1);
            terminal.EndRun();
            var beforeTerminal = terminal.CaptureStores();
            Assert.That(terminal.Core.CombatAdvance.AdvanceCombat(), Is.False);
            Assert.That(terminal.CaptureStores(), Is.EqualTo(beforeTerminal));
        }
    }
}
