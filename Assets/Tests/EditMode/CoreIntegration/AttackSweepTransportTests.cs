using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class AttackSweepTransportTests
    {
        [Test]
        public void QueuedAttackAndLatestPreviewRetainExplicitSideAndRequestIdentity()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)));
            var b = battle.Buffers;
            var actor = battle.Actor;
            var enemy = battle.Character(EntityKey.Unit_Demon);
            var aim = AttackAim.ForCell(enemy.Anchor.Value);
            var turn = b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence;
            var battleId = b.CombatStateReadSink.BattleInstanceId;
            var data = CombatViewCommandData.Attack(actor.EntityId, aim, battleId, turn, AttackSweepSide.Left);
            var number = b.CombatViewWriteSink.Submit(data);
            Assert.That(b.CombatViewReadSink.TryGetNextCommand(number - 1, out var command), Is.True);
            Assert.That(command.Data.SweepSide, Is.EqualTo(AttackSweepSide.Left));
            Assert.That(command.Data.ExpectedBattleInstanceId, Is.EqualTo(battleId));
            Assert.That(command.Data.ExpectedControllerTurnSequence, Is.EqualTo(turn));
            var assembled = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Attack,
                actor.EntityId, command.Data.Aim, expectedBattleInstanceId: battleId, expectedControllerTurnSequence: turn,
                sweepSide: command.Data.SweepSide));
            Assert.That(assembled.SweepSide, Is.EqualTo(AttackSweepSide.Left));

            var previewData = CombatViewPreviewData.Attack(actor.EntityId, aim, AttackSweepSide.Left);
            Assert.That(previewData.SweepSide, Is.EqualTo(AttackSweepSide.Left));
            int previewNumber = b.CombatViewWriteSink.SetPreview(previewData);
            Assert.That(b.CombatViewReadSink.Preview.Data.SweepSide, Is.EqualTo(AttackSweepSide.Left));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.RequestNumber, Is.EqualTo(previewNumber));
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.Throws<ArgumentOutOfRangeException>(() => CombatViewPreviewData.Attack(actor.EntityId, aim, (AttackSweepSide)0));
        }
    }
}
