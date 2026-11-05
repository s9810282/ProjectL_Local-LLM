using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CombatCommandServiceTests
    {
        [Test]
        public void NoCommandOrNoBattle_ReturnsFalseWithoutPublication()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config);
            var before = battle.CaptureStores();
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));

            // 처리할 입력을 남겨 두어 전투 없음 분기가 입력 없음 분기에 가려지지 않게 한다.
            battle.Buffers.CombatViewWriteSink.Submit(battle.EndPlayerTurn());
            battle.Buffers.BattleWriteSink.EndBattle();
            Assert.That(battle.Buffers.CombatStateReadSink.HasBattle, Is.False);
            before = battle.CaptureStores();
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
        }

        [Test]
        public void AcceptedAttack_PublishesAndConsumesActionExactlyOnce()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));
            var b = battle.Buffers;
            var actor = battle.Actor;
            var enemy = battle.Character(EntityKey.Unit_Demon);
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            int presented = b.CombatViewReadSink.PresentedResultSequence;
            var history = b.CombatResultHistoryReadSink.Results.ToArray();
            int number = b.CombatViewWriteSink.Submit(battle.Attack(actor, enemy.Anchor.Value));
            var queued = b.CombatViewReadSink.Commands.ToArray();

            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.CommandKind, Is.EqualTo(CombatCommandKind.Attack));
            Assert.That(result.ActorId, Is.EqualTo(actor.EntityId));
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Sequence, Is.EqualTo(sequence + 1));
            Assert.That(result.ActionUseSettlement.ActorId, Is.EqualTo(actor.EntityId));
            Assert.That(result.ActionUseSettlement.BeforeActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(result.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            var afterEnemy = battle.Character(enemy.CharacterKey);
            Assert.That(afterEnemy.Health.CurrentHealth, Is.EqualTo(enemy.Health.CurrentHealth - actor.Stats.AttackPower));
            Assert.That(afterEnemy.Health.CurrentHealth, Is.GreaterThan(0));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(number));
            Assert.That(b.CombatResultHistoryReadSink.Results, Is.EqualTo(history.Concat(new[] { result })));
            Assert.That(b.CombatViewReadSink.Commands, Is.EqualTo(queued));
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(presented));

            var settled = battle.CaptureStores();
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            Assert.That(battle.CaptureStores(), Is.EqualTo(settled));
        }

        [Test]
        public void RunEnded_ReturnsFalseWithoutConsumingCommand()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config);
            int handled = battle.Buffers.CombatStateReadSink.HandledCommandNumber;
            int pending = battle.Buffers.CombatViewWriteSink.Submit(battle.EndPlayerTurn());
            battle.EndRun();
            var before = battle.CaptureStores();

            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            Assert.That(battle.Buffers.CombatViewReadSink.TryGetNextCommand(handled, out var next), Is.True);
            Assert.That(next.Number, Is.EqualTo(pending));
        }

        [Test]
        public void EndPlayerTurn_PublishesActorFreeBoundaryOnce()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource());
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var b = battle.Buffers;
            var turn = b.CombatStateReadSink.Current.CommandState.Turn;
            var end = battle.EndPlayerTurn();
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            battle.Submit(end);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandKind, Is.EqualTo(CombatCommandKind.EndPlayerTurn));
            Assert.That(result.ActorId.IsEmpty, Is.True);
            Assert.That(result.Sequence, Is.EqualTo(sequence + 1));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.PlayerTurnEnded), Is.EqualTo(1));
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingEnemy));
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.CompletedPlayerTurnCount,
                Is.EqualTo(turn.CompletedPlayerTurnCount + 1));
            var ended = battle.CaptureStores();
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            Assert.That(battle.CaptureStores(), Is.EqualTo(ended));

            battle.AdvanceToPlayerOrDay();
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence, Is.GreaterThan(turn.ControllerTurnSequence));
            var currentState = b.CombatStateReadSink.Current.CommandState;
            var characters = b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            var souls = b.CombatStateReadSink.SoulField;
            sequence = b.CombatStateReadSink.LatestResultSequence;
            int number = b.CombatViewWriteSink.Submit(end);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out result), Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("StaleTurn"));
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Sequence, Is.EqualTo(sequence + 1));
            Assert.That(result.Events, Is.Empty);
            // 거절 결과도 조회 래퍼를 새로 게시한다. 래퍼 참조가 아니라 현재 턴·게임 사실을 비교한다.
            Assert.That(b.CombatStateReadSink.Current.CommandState.SameValues(currentState), Is.True);
            Assert.That(b.CombatStateReadSink.SoulField.SameValues(souls), Is.True);
            Assert.That(b.CharacterReadSink.Characters.Count, Is.EqualTo(characters.Length));
            foreach (var character in characters)
                Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == character.EntityId)
                    .ToFacts().SameValues(character), Is.True);
        }
    }
}
