using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MovementPublicationTests
    {
        [Test]
        public void ConfiguredMovement_SoToOwnedStatsBattlePreviewAndPublication_CapturesLifetime()
        {
            var authored = ScriptableObject.CreateInstance<CategoryConfigDataSO>();
            try
            {
                SetRiderMovement(authored, 5);
                using var battle = new ConfiguredMovementBattle((CategoryConfigData)authored.GetData());
                Assert.That(battle.OwnedStats.Stats.MoveRange, Is.EqualTo(5));
                Assert.That(battle.Actor.Stats.MoveRange, Is.EqualTo(5));
                Assert.That(battle.Main.Stats.MoveRange, Is.EqualTo(3));

                SetRiderMovement(authored, 2);
                Assert.That(battle.Actor.Stats.MoveRange, Is.EqualTo(5));
                var before = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
                var sequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
                var command = battle.PreviewMove(battle.Actor, Enumerable.Range(0, 6).Select(x => new CellCoord(x, 1)).ToArray());
                battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview);
                Assert.That(preview.Move.Value.CanMove, Is.True, preview.Move.Value.FailureReason.ToString());
                Assert.That(preview.Move.Value.QuotedDistance, Is.EqualTo(5));
                Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Available));
                battle.Submit(command);
                Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Moved));
                Assert.That(battle.Actor.Anchor, Is.EqualTo(new CellCoord(5, 1)));
                Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
                battle.Buffers.CombatResultReadSink.TryGetLatest(out var result);
                Assert.That(result.ActionUseSettlement.ActorId, Is.EqualTo(battle.Actor.EntityId));
                Assert.That(result.ActionUseSettlement.BeforeActionUse, Is.EqualTo(UnitActionUse.Available));
                Assert.That(result.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Moved));
                Assert.That(result.PathMovement.EnteredCells, Is.EqualTo(preview.Move.Value.PathMovement.EnteredCells));
                foreach (var original in before.Where(c => c.EntityId != battle.Actor.EntityId))
                    Assert.That(battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == original.EntityId)
                        .ToFacts().SameValues(original), Is.True);

                using var fresh = new ConfiguredMovementBattle((CategoryConfigData)authored.GetData());
                Assert.That(fresh.OwnedStats.Stats.MoveRange, Is.EqualTo(2));
                Assert.That(fresh.Actor.Stats.MoveRange, Is.EqualTo(2));
                Assert.That(battle.Actor.Stats.MoveRange, Is.EqualTo(5));
            }
            finally { UnityEngine.Object.DestroyImmediate(authored); }
        }

        [TestCase("occupancy")]
        [TestCase("allowance")]
        public void ConfiguredMovement_ChangedWorldPreservesWorldAndPublishesOneRejection(string change)
        {
            using var battle = new ConfiguredMovementBattle(new CategoryConfigData(3,5,3,2,0));
            var command = battle.PreviewMove(battle.Actor, battle.Actor.Anchor.Value, new CellCoord(1, 1));
            if (change == "occupancy")
            {
                battle.Move(battle.Main, new CellCoord(1, 0));
                battle.Move(battle.Character(EntityKey.Unit_DynamiteHammer), new CellCoord(3, 0));
                battle.Move(battle.Character(EntityKey.Unit_Sniper), new CellCoord(5, 0));
                var previewSequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
                var handled = battle.Buffers.CombatStateReadSink.HandledCommandNumber;
                battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(battle.Actor.EntityId,
                    new MovementPath(new[] { battle.Actor.Anchor.Value, new CellCoord(1, 1) })));
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview);
                Assert.That(preview.Move.Value.CanMove, Is.True, preview.Move.Value.FailureReason.ToString());
                Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(previewSequence));
                Assert.That(battle.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
            }
            else
            {
                var actor = battle.Actor;
                var changed = actor.With(stats: actor.Stats.With(moveRange: 6));
                battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(actor, changed) },
                    Array.Empty<SoulCountChange>()));
            }
            var before = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            var state = battle.Buffers.CombatStateReadSink.Current.CommandState;
            var souls = battle.Buffers.CombatStateReadSink.SoulField;
            var sequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
            int number = battle.Buffers.CombatViewWriteSink.Submit(command);
            var queued = battle.Buffers.CombatViewReadSink.Commands.ToArray();
            int presented = battle.Buffers.CombatViewReadSink.PresentedResultSequence;
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("StaleContext"));
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.Environment.SameValues(state.Environment), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.SoulField.SameValues(souls), Is.True);
            foreach (var original in before)
                Assert.That(battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == original.EntityId)
                    .ToFacts().SameValues(original), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(number));
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(battle.Buffers.CombatViewReadSink.Commands, Is.EqualTo(queued));
            Assert.That(battle.Buffers.CombatViewReadSink.Commands.Single(c => c.Number == number).Data, Is.EqualTo(command));
            Assert.That(battle.Buffers.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(presented));
        }

        private static void SetRiderMovement(CategoryConfigDataSO authored, int allowance)
        {
            var serialized = new SerializedObject(authored);
            serialized.FindProperty("riderMovement").FindPropertyRelative("movementAllowance").intValue = allowance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
