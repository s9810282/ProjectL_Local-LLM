using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class CombatAdvanceServiceTests
    {
        [Test]
        public void NotAutomatic_ReturnsFalseWithoutPublication()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config);
            var before = battle.CaptureStores();
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.False);
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
        }

        [Test]
        public void RunEnded_ReturnsFalseWithoutPublication()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, startPlayerTurn: false);
            battle.EndRun();
            var before = battle.CaptureStores();
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.False);
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
        }

        [Test]
        public void Automatic_StartsControllerWithoutConsumingViewCommands()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, startPlayerTurn: false);
            var b = battle.Buffers;
            int handled = b.CombatStateReadSink.HandledCommandNumber;
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            int presented = b.CombatViewReadSink.PresentedResultSequence;
            // 아직 소유자 턴이 시작되지 않았으므로 배치 입력을 보류시켜 자동 진행과 입력 처리를 구분한다.
            int pending = b.CombatViewWriteSink.Submit(CombatViewCommandData.EnterCombat());
            var queued = b.CombatViewReadSink.Commands.ToArray();
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Deferred));

            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Kind, Is.EqualTo(CombatProcessingKind.Advance));
            Assert.That(result.CommandKind, Is.EqualTo(CombatCommandKind.StartControllerTurn));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.CommandNumber, Is.Zero);
            Assert.That(result.Sequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Player));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
            Assert.That(b.CombatViewReadSink.TryGetNextCommand(handled, out var next), Is.True);
            Assert.That(next.Number, Is.EqualTo(pending));
            Assert.That(b.CombatViewReadSink.Commands, Is.EqualTo(queued));
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(presented));
        }

        [Test]
        public void EnemyAction_ConsumesOwnActionAndPreservesPendingInput()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));
            battle.Move(battle.Character(EntityKey.Unit_Sniper), new CellCoord(5, 0));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var b = battle.Buffers;
            Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
            var enemy = battle.Character(EntityKey.Unit_Demon);
            Assert.That(enemy.ActionUse, Is.EqualTo(UnitActionUse.Available));
            var health = b.CharacterReadSink.Characters.Where(c => c.Owner == Owner.Player).Sum(c => c.Health.CurrentHealth);
            int handled = b.CombatStateReadSink.HandledCommandNumber;
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            int presented = b.CombatViewReadSink.PresentedResultSequence;
            int pending = b.CombatViewWriteSink.Submit(battle.EndPlayerTurn());
            var queued = b.CombatViewReadSink.Commands.ToArray();

            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Kind, Is.EqualTo(CombatProcessingKind.Advance));
            Assert.That(result.ActorId, Is.EqualTo(enemy.EntityId));
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.ActionUseSettlement.ActorId, Is.EqualTo(enemy.EntityId));
            Assert.That(result.ActionUseSettlement.BeforeActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(result.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(battle.Character(EntityKey.Unit_Demon).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(b.CharacterReadSink.Characters.Where(c => c.Owner == Owner.Player).Sum(c => c.Health.CurrentHealth), Is.LessThan(health));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
            Assert.That(b.CombatViewReadSink.TryGetNextCommand(handled, out var next), Is.True);
            Assert.That(next.Number, Is.EqualTo(pending));
            Assert.That(b.CombatViewReadSink.Commands, Is.EqualTo(queued));
            Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(presented));
        }

        [Test]
        public void TerminalAction_PreventsFurtherControllerPublication()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(terminalHeadquarters: true));
            var b = battle.Buffers;
            battle.Submit(battle.Attack(battle.Actor, new CellCoord(1, 3)));
            Assert.That(b.CombatStateReadSink.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(b.CombatStateReadSink.Completion, Is.Not.Null);
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            var before = battle.CaptureStores();
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.False);
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.False);
            Assert.That(battle.CaptureStores(), Is.EqualTo(before));
        }
    }
}
