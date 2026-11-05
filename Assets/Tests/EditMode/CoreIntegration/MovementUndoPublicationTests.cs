using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MovementUndoPublicationTests
    {
        private static ConfiguredMovementBattle Battle() => new(ConfiguredMovementBattle.Config,
            new ConfiguredMovementBattle.EncounterSource());

        [Test]
        public void PublicQueryAndUndo_ForwardIdentityAndRejectConsumedToken()
        {
            using var battle = Battle();
            var before = battle.Actor;
            battle.Move(before, new CellCoord(1, 1));
            var query = Query(battle);
            Assert.That(query.SourceRevision, Is.EqualTo(battle.Buffers.CombatStateReadSink.QueryRevision));
            Assert.That(query.MovementUndo.CanUndo, Is.True);
            var command = PublicUndo(battle, query.MovementUndo.MoveCommandNumber);
            battle.Submit(command);
            Assert.That(battle.Actor.SameValues(before), Is.True);
            var revision = battle.Buffers.CombatStateReadSink.QueryRevision;
            battle.Buffers.CombatViewWriteSink.Submit(command);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var rejected), Is.True);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(battle.Buffers.CombatStateReadSink.QueryRevision, Is.EqualTo(revision));
            Assert.That(battle.Actor.SameValues(before), Is.True);
        }

        [Test]
        public void BlockedOriginAndNoOp_RefreshQueryWithoutLosingTokenOrAdvancingOnPreview()
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var original = Query(battle);
            var other = battle.Character(EntityKey.Unit_DynamiteHammer);
            var blocker = other.WithPlacement(other.Area, other.ReserveSlotIndex, new CellCoord(0, 1),
                other.Rotation, new[] { new CellCoord(0, 1) });
            Apply(battle, other, blocker);
            var revision = battle.Buffers.CombatStateReadSink.QueryRevision;
            Assert.That(revision, Is.EqualTo(original.SourceRevision + 1));
            var blocked = Query(battle);
            Assert.That(blocked.MovementUndo.CanUndo, Is.False);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints.Single().MoveCommandNumber,
                Is.EqualTo(original.MovementUndo.MoveCommandNumber));
            Apply(battle, blocker, blocker);
            Assert.That(battle.Buffers.CombatStateReadSink.QueryRevision, Is.EqualTo(revision));
            Query(battle);
            Assert.That(battle.Buffers.CombatStateReadSink.QueryRevision, Is.EqualTo(revision));
            Apply(battle, blocker, other);
            Assert.That(Query(battle).MovementUndo.CanUndo, Is.True);
            battle.Submit(PublicUndo(battle, original.MovementUndo.MoveCommandNumber));
        }

        [TestCase("revision")]
        [TestCase("unbound")]
        [TestCase("actor")]
        public void StaleOrForeignAvailability_CannotAnswerCurrentRequest(string defect)
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var old = Query(battle);
            var b = battle.Buffers;
            var actor = defect == "actor" ? battle.Main.EntityId : battle.Actor.EntityId;
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ActionAvailability(actor));
            var number = b.CombatViewReadSink.Preview.Number;
            if (defect == "revision")
            {
                var other = battle.Character(EntityKey.Unit_DynamiteHammer);
                Apply(battle, other, other.WithPlacement(other.Area, other.ReserveSlotIndex, new CellCoord(2, 1),
                    other.Rotation, new[] { new CellCoord(2, 1) }));
            }
            var snapshot = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.PublishPreview(
                CombatPreviewResult.ForActionAvailability(number, defect == "unbound" ? old.WithSourceRevision(0) : old)));
            Assert.That(battle.CaptureStores(), Is.EqualTo(snapshot));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
        }

        [Test]
        public void PreviewReplacement_SeparatesActorTokensAndPreservesOrdinaryQueries()
        {
            using var battle = Battle();
            var b = battle.Buffers;
            Assert.That(Query(battle).MovementUndo.Failure, Is.EqualTo(MovementUndoFailureReason.NoCheckpoint));
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var original = Query(battle);
            int replaced = b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ActionAvailability(battle.Actor.EntityId));
            int current = b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ActionAvailability(battle.Main.EntityId));
            var stores = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.PublishPreview(
                CombatPreviewResult.ForActionAvailability(replaced, original)));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            Assert.That(b.CombatViewWriteSink.ClearPreview(replaced), Is.False);
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var other), Is.True);
            Assert.That(other.RequestNumber, Is.EqualTo(current));
            Assert.That(other.ActionAvailability.ActorId, Is.EqualTo(battle.Main.EntityId));
            Assert.That(other.ActionAvailability.MovementUndo.Failure, Is.EqualTo(MovementUndoFailureReason.NoCheckpoint));
            Assert.That(other.ActionAvailability.MovementUndo.MoveCommandNumber, Is.Zero);
            Assert.That(other.ActionAvailability.MovementUndo.Destination, Is.Null);

            var refreshed = Query(battle);
            Assert.That(refreshed.MovementUndo.MoveCommandNumber, Is.EqualTo(original.MovementUndo.MoveCommandNumber));
            battle.Submit(PublicUndo(battle, refreshed.MovementUndo.MoveCommandNumber));
            var revision = b.CombatStateReadSink.QueryRevision;
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.MoveRange(battle.Actor.EntityId));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var movement), Is.True);
            Assert.That(movement.MoveRange.Value.Succeeded, Is.True);
            Assert.That(movement.MoveRange.Value.Destinations.Length, Is.GreaterThan(0));
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(battle.Actor.EntityId));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var attack), Is.True);
            Assert.That(attack.Range.Value.Succeeded, Is.True);
            Assert.That(b.CombatStateReadSink.QueryRevision, Is.EqualTo(revision));
        }

        [Test]
        public void PublicDistanceCycles_PreserveCommittedDistanceAndPublishOneReturn()
        {
            using var battle = new ConfiguredMovementBattle(new(3, 4, 3, 2, 0),
                new ConfiguredMovementBattle.EncounterSource());
            var b = battle.Buffers;
            foreach (int distance in new[] { 4, 4, 2 })
            {
                int direction = battle.Actor.Anchor.Value.X == 0 ? 1 : -1;
                var path = Enumerable.Range(0, distance + 1)
                    .Select(step => new CellCoord(battle.Actor.Anchor.Value.X + direction * step, 1)).ToArray();
                battle.Submit(battle.PreviewMove(battle.Actor, path));
                battle.Submit(battle.EndPlayerTurn());
                battle.AdvanceToPlayerOrDay();
                Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase,
                    Is.EqualTo(CombatControllerPhase.Player));
            }
            var committed = battle.Actor;
            Assert.That(committed.StageRecord.DistanceMoved, Is.EqualTo(10));
            var turn = b.CombatStateReadSink.Current.CommandState.Turn;
            int gold = b.CurrencyReadSink.Amount;
            int fame = b.FameReadSink.Amount;
            var field = b.CombatStateReadSink.SoulField;
            foreach (int distance in new[] { 3, 3, 2 })
            {
                var path = Enumerable.Range(0, distance + 1)
                    .Select(step => new CellCoord(committed.Anchor.Value.X + step, 1)).ToArray();
                battle.Submit(battle.PreviewMove(battle.Actor, path));
                Assert.That(battle.Actor.StageRecord.DistanceMoved, Is.EqualTo(10 + distance));
                var moved = battle.Actor;
                var query = Query(battle);
                Assert.That(query.CanMove, Is.False);
                Assert.That(query.MovementUndo.Destination, Is.EqualTo(committed.Anchor));
                int sequence = b.CombatStateReadSink.LatestResultSequence;
                battle.Submit(PublicUndo(battle, query.MovementUndo.MoveCommandNumber));
                Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
                Assert.That(result.Sequence, Is.EqualTo(sequence + 1));
                Assert.That(result.UndoMovement.ActorId, Is.EqualTo(committed.EntityId));
                Assert.That(result.UndoMovement.MoveCommandNumber, Is.EqualTo(query.MovementUndo.MoveCommandNumber));
                Assert.That(result.UndoMovement.From, Is.EqualTo(moved.Anchor.Value));
                Assert.That(result.UndoMovement.To, Is.EqualTo(committed.Anchor.Value));
                Assert.That(result.UndoMovement.Rotation, Is.EqualTo(committed.Rotation));
                Assert.That(result.UndoMovement.DistanceReverted, Is.EqualTo(distance));
                Assert.That(result.Events.Count, Is.EqualTo(1));
                var returned = result.Events.Single() as CombatCharacterEvent;
                Assert.That(returned, Is.Not.Null);
                Assert.That(returned.Kind, Is.EqualTo(CombatEventKind.Returned));
                Assert.That(returned.Expected.SameValues(moved), Is.True);
                Assert.That(returned.Final.SameValues(committed), Is.True);
                Assert.That(result.ActionUseSettlement.BeforeActionUse, Is.EqualTo(UnitActionUse.Moved));
                Assert.That(result.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Available));
                Assert.That(result.PathMovement, Is.Null);
                Assert.That(result.Movement.HasValue, Is.False);
                Assert.That(result.Attack.HasValue, Is.False);
                Assert.That(battle.Actor.SameValues(committed), Is.True);
                Assert.That(b.CombatStateReadSink.Current.CommandState.Turn.SameValues(turn), Is.True);
                Assert.That(b.CombatStateReadSink.SoulField.SameValues(field), Is.True);
                Assert.That((b.CurrencyReadSink.Amount, b.FameReadSink.Amount), Is.EqualTo((gold, fame)));
                b.CombatViewWriteSink.AcknowledgePresented(result.Sequence);
                b.CombatViewWriteSink.RemoveCommandsThrough(b.CombatStateReadSink.HandledCommandNumber);
                Assert.That(b.CombatViewReadSink.PresentedResultSequence, Is.EqualTo(result.Sequence));
                Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            }
            Assert.That(Query(battle).MovementAllowance, Is.EqualTo(4));
            var fullRange = Enumerable.Range(0, 5)
                .Select(step => new CellCoord(committed.Anchor.Value.X + step, 1)).ToArray();
            battle.Submit(battle.PreviewMove(battle.Actor, fullRange));
            Assert.That(battle.Actor.StageRecord.DistanceMoved, Is.EqualTo(14));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PublicAttackOrRemoteKill_CommitsOnlyItsActorAndPreservesOtherUndo(bool kill)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource(new CellCoord(2, 3)));
            var b = battle.Buffers;
            var enemy = battle.Character(EntityKey.Unit_Demon);
            if (kill)
                Apply(battle, enemy, enemy.WithHealth(new HealthStateSnapshot(1, enemy.Health.MaxHealth)));
            var otherBefore = battle.Main;
            battle.Move(otherBefore, new CellCoord(1, 0));
            var otherToken = Query(battle, battle.Main);
            battle.Move(battle.Actor, new CellCoord(1, 1));
            battle.Submit(battle.Attack(battle.Actor, enemy.Anchor.Value));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(Query(battle).MovementUndo.Failure, Is.EqualTo(MovementUndoFailureReason.NoCheckpoint));
            var refreshed = Query(battle, battle.Main);
            Assert.That(refreshed.MovementUndo.CanUndo, Is.True);
            Assert.That(refreshed.MovementUndo.MoveCommandNumber, Is.EqualTo(otherToken.MovementUndo.MoveCommandNumber));
            Assert.That(refreshed.SourceRevision, Is.GreaterThan(otherToken.SourceRevision));
            var attackingActor = battle.Actor;
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.IsDead, Is.EqualTo(kill));
            battle.Submit(CombatViewCommandData.UndoMove(refreshed.ActorId, refreshed.BattleInstanceId,
                refreshed.ControllerTurnSequence, refreshed.MovementUndo.MoveCommandNumber));
            Assert.That(battle.Main.SameValues(otherBefore), Is.True);
            Assert.That(battle.Actor.SameValues(attackingActor), Is.True);
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.IsDead, Is.EqualTo(kill));
        }

        [Test]
        public void PublicQueriesAfterOriginSourceChange_InvalidateOnlyMatchingActor()
        {
            using var battle = Battle();
            var b = battle.Buffers;
            battle.Move(battle.Actor, new CellCoord(1, 1));
            battle.Move(battle.Main, new CellCoord(1, 0));
            var before = Query(battle, battle.Main);
            var souls = new SoulFieldSnapshot(2, new[] { Soul(1, new CellCoord(0, 1)) });
            b.BattleWriteSink.Apply(new SoulActionChanges(EmptyChanges(),
                new SoulFieldChange(b.CombatStateReadSink.SoulField, souls), Array.Empty<long>()));
            Assert.That(Query(battle).MovementUndo.Failure, Is.EqualTo(MovementUndoFailureReason.NoCheckpoint));
            var unaffected = Query(battle, battle.Main);
            Assert.That(unaffected.MovementUndo.CanUndo, Is.True);
            Assert.That(unaffected.MovementUndo.MoveCommandNumber, Is.EqualTo(before.MovementUndo.MoveCommandNumber));
            Assert.That(unaffected.SourceRevision, Is.EqualTo(before.SourceRevision + 1));
            battle.Submit(CombatViewCommandData.UndoMove(unaffected.ActorId, unaffected.BattleInstanceId,
                unaffected.ControllerTurnSequence, unaffected.MovementUndo.MoveCommandNumber));
            Assert.That(b.CombatStateReadSink.SoulField.SameValues(souls), Is.True);
        }

        private static CombatActionAvailability Query(ConfiguredMovementBattle battle, CharacterCombatFacts actor = null)
        {
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ActionAvailability((actor ?? battle.Actor).EntityId));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            return preview.ActionAvailability;
        }

        private static CombatViewCommandData PublicUndo(ConfiguredMovementBattle battle, int token)
        {
            return CombatViewCommandData.UndoMove(battle.Actor.EntityId,
                battle.Buffers.CombatStateReadSink.BattleInstanceId,
                battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence, token);
        }

        [TestCase("battle")]
        [TestCase("turn")]
        [TestCase("token")]
        [TestCase("actor")]
        public void PublicStaleIdentity_RejectsWithoutGameplayMutation(string defect)
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var b = battle.Buffers;
            var checkpoint = b.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints.Single();
            var command = CombatViewCommandData.UndoMove(defect == "actor" ? battle.Main.EntityId : battle.Actor.EntityId,
                checkpoint.BattleInstanceId + (defect == "battle" ? 1 : 0),
                checkpoint.ControllerTurnSequence + (defect == "turn" ? 1 : 0),
                checkpoint.MoveCommandNumber + (defect == "token" ? 1 : 0));
            var original = b.CombatStateReadSink.Current.CommandState;
            var revision = b.CombatStateReadSink.QueryRevision;
            var actorBefore = battle.Actor;
            b.CombatViewWriteSink.Submit(command);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
            // 거절 결과 번호는 진행하지만 게임 값과 조회 버전은 보존한다.
            Assert.That(b.CombatStateReadSink.Current.CommandState.SameValues(original), Is.True);
            Assert.That(b.CombatStateReadSink.QueryRevision, Is.EqualTo(revision));
            Assert.That(battle.Actor.SameValues(actorBefore), Is.True);
        }

        [TestCase("origin")]
        [TestCase("dependency")]
        [TestCase("source")]
        [TestCase("turn")]
        [TestCase("battle")]
        [TestCase("distance")]
        [TestCase("actor")]
        public void ForgedCheckpointCreation_RejectsAfterCharacterPreparation(string defect)
        {
            using var battle = Battle();
            var b = battle.Buffers;
            var command = battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1));
            int number = b.CombatViewWriteSink.Submit(command);
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move,
                command.EntityId, default, path: command.Path, movementContext: command.MovementContext,
                expectedBattleInstanceId: command.ExpectedBattleInstanceId, expectedControllerTurnSequence: command.ExpectedControllerTurnSequence));
            var move = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var c = move.FinalState.MovementUndo.Checkpoints.Single();
            var origin = defect == "origin" ? new CellCoord(1, 0) : c.Anchor;
            var souls = new SoulFieldSnapshot(2, new[] { Soul(1, origin) });
            var forged = new MovementUndoCheckpoint(c.BattleInstanceId + (defect == "battle" ? 1 : 0),
                c.ControllerTurnSequence + (defect == "turn" ? 1 : 0), c.MoveCommandNumber + (defect == "source" ? 1 : 0),
                c.ActorId, origin, c.Rotation, new[] { origin }, defect == "distance" ? 0 : c.RecordedDistance,
                MovementUndoOriginFacts.Capture(origin, input.State, input.EnvironmentBindings, defect == "dependency" ? souls : input.Souls),
                defect == "actor" ? c.ExpectedActorAfterMove.With(stageRecord: c.ExpectedActorAfterMove.StageRecord.Add(kills: 1)) : c.ExpectedActorAfterMove);
            var stores = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(move.WithFinalState(
                move.FinalState.With(movementUndo: new MovementUndoState(new[] { forged })))));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            b.BattleWriteSink.Apply(move);
            Assert.That(battle.Actor.Anchor, Is.EqualTo(new CellCoord(1, 1)));
        }

        [Test]
        public void MainOnlyAndSoulChanges_AreSelectiveAndFailedTransactionsAreAtomic()
        {
            using var battle = Battle();
            var b = battle.Buffers;
            battle.Move(battle.Actor, new CellCoord(1, 1));
            battle.Move(battle.Main, new CellCoord(1, 0));
            var revision = b.CombatStateReadSink.QueryRevision;
            var mainId = battle.Main.OwnedId.Value;
            Assert.That(b.CharacterReadSink.TryGetMainState(mainId, out var main), Is.True);
            var damaged = main.WithCurrentHealth(main.CurrentHealth - 1);
            b.BattleWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(mainId, main, damaged)));
            b.BattleWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(),
                new MainCharacterPersistentChange(mainId, damaged, main)));
            Assert.That(b.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints.Single().ActorId, Is.EqualTo(battle.Actor.EntityId));
            Assert.That(b.CombatStateReadSink.QueryRevision, Is.EqualTo(revision + 2));
            var remote = new SoulFieldSnapshot(2, new[] { Soul(1, new CellCoord(6, 5)) });
            b.BattleWriteSink.Apply(new SoulActionChanges(EmptyChanges(), new SoulFieldChange(b.CombatStateReadSink.SoulField, remote), Array.Empty<long>()));
            Assert.That(Query(battle).MovementUndo.CanUndo, Is.True);
            var original = battle.CaptureStores();
            var current = battle.Actor;
            var badCharacters = new CharacterChanges(new[] { new CharacterStateChange(
                current.With(stageRecord: current.StageRecord.Add(distanceMoved: 1)), current) }, Array.Empty<SoulCountChange>());
            var atOrigin = new SoulFieldSnapshot(3, remote.Entries.Append(Soul(2, new CellCoord(0, 1))).ToArray());
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(new SoulActionChanges(badCharacters,
                new SoulFieldChange(remote, atOrigin), Array.Empty<long>())));
            Assert.That(battle.CaptureStores(), Is.EqualTo(original));
            b.BattleWriteSink.Apply(new SoulActionChanges(EmptyChanges(), new SoulFieldChange(remote, atOrigin), Array.Empty<long>()));
            Assert.That(b.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void InventoryGrantsAndGenericStatisticGuards_PreserveUndoAndRevision()
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var b = battle.Buffers;
            var undo = b.CombatStateReadSink.Current.CommandState.MovementUndo;
            var revision = b.CombatStateReadSink.QueryRevision;
            var grant = ItemInventoryCalculator.Grant(b.ItemInventoryReadSink.Current, new[] { new ItemDefinition(1, new WhiskeyItemSpec(50, 10)) }, 1, 1);
            Assert.That(grant.Accepted, Is.True);
            b.ItemInventoryWriteSink.PublishGrant(grant.Change);
            var stores = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => b.ItemInventoryWriteSink.PublishGrant(grant.Change));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            foreach (var record in new[] { CharacterStageRecord.Empty, battle.Actor.StageRecord.Add(distanceMoved: 1) })
            {
                Assert.Throws<InvalidOperationException>(() => Apply(battle, battle.Actor, battle.Actor.With(stageRecord: record)));
                Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            }
            Assert.That(b.CombatStateReadSink.QueryRevision, Is.EqualTo(revision));
            Assert.That(b.CombatStateReadSink.Current.CommandState.MovementUndo, Is.SameAs(undo));
        }

        [Test]
        public void DirectCompletionAndEndBattle_ClearCheckpointAndRevisionLifetime()
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var b = battle.Buffers;
            long revision = b.CombatStateReadSink.QueryRevision;
            var completion = new BattleCompletionChanges(EmptyChanges(),
                new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed),
                ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(1101), true));
            Assert.That(b.BattleWriteSink.CompleteBattle(completion), Is.True);
            Assert.That(b.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(b.CombatStateReadSink.QueryRevision, Is.EqualTo(revision + 1));
            Assert.That(b.BattleWriteSink.CompleteBattle(completion), Is.False);
            Assert.That(b.CombatStateReadSink.QueryRevision, Is.EqualTo(revision + 1));
            b.BattleWriteSink.EndBattle();
            Assert.That(b.CombatStateReadSink.QueryRevision, Is.Zero);
        }

        private static CharacterChanges EmptyChanges() => new(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>());
        private static SoulFieldFacts Soul(long id, CellCoord cell) => new(id,
            new EffectOriginKey(EffectOwnerKind.Trait, 2, (int)TraitKey.CallOfBlood, "main"), cell, 2, 1);

        [TestCase(false)]
        [TestCase(true)]
        public void PartialMoveCheckpoint_RequiresActuallyEnteredFinalPlacement(bool forgePlacement)
        {
            using var battle = Battle();
            var b = battle.Buffers;
            var command = battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1));
            int number = b.CombatViewWriteSink.Submit(command);
            var input = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.Move,
                command.EntityId, default, path: command.Path, movementContext: command.MovementContext,
                expectedBattleInstanceId: command.ExpectedBattleInstanceId, expectedControllerTurnSequence: command.ExpectedControllerTurnSequence));
            var move = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var c = move.FinalState.MovementUndo.Checkpoints.Single();
            var differentCell = forgePlacement ? new CellCoord(1, 2) : new CellCoord(1, 1);
            var after = c.ExpectedActorAfterMove.WithPlacement(CharacterRuntimeArea.Field, null,
                differentCell, c.ExpectedActorAfterMove.Rotation, new[] { differentCell });
            var forged = new MovementUndoCheckpoint(c.BattleInstanceId, c.ControllerTurnSequence, c.MoveCommandNumber,
                c.ActorId, c.Anchor, c.Rotation, c.OccupiedCells, c.RecordedDistance, c.OriginFacts, after);
            var proposal = new CombatCommandTransition(input, true, "", new CharacterChanges(new[] {
                new CharacterStateChange(battle.Actor, after) }, Array.Empty<SoulCountChange>()), move.Field,
                move.FinalState.With(movementUndo: new MovementUndoState(new[] { forged })), move.Outcome,
                events: move.Events, pathMovement: new MovementPathResult(input.Path, move.PathMovement.EnteredCells,
                    differentCell, 1, MovementStopReason.NextStepInvalid), actionUseSettlement: move.ActionUseSettlement);
            var before = battle.CaptureStores();
            if (forgePlacement)
            {
                Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(proposal));
                Assert.That(battle.CaptureStores(), Is.EqualTo(before));
            }
            else
            {
                b.BattleWriteSink.Apply(proposal);
                Assert.That(Query(battle).MovementUndo.CanUndo, Is.True);
                battle.Submit(PublicUndo(battle, number));
                Assert.That(battle.Actor.Anchor, Is.EqualTo(new CellCoord(0, 1)));
                Assert.That(battle.Actor.StageRecord.DistanceMoved, Is.Zero);
            }
        }

        [Test]
        public void NativeMoveUndoAndDuplicate_PublishExactlyOneReturnAndDistanceReversal()
        {
            using var battle = Battle();
            var origin = battle.Actor;
            battle.Move(origin, new CellCoord(1, 1));
            var transition = Undo(battle);
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(battle.Actor.SameValues(origin), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.UndoMovement.DistanceReverted, Is.EqualTo(1));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.Returned), Is.EqualTo(1));
            var stores = battle.CaptureStores();
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            battle.Move(battle.Actor, new CellCoord(1, 1));
        }

        [Test]
        public void DirectActorChangeAndRestore_CannotReviveCheckpoint()
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var before = battle.Actor;
            var changed = before.WithPlacement(before.Area, before.ReserveSlotIndex, new CellCoord(1, 2),
                before.Rotation, new[] { new CellCoord(1, 2) });
            Apply(battle, before, changed);
            Apply(battle, changed, before);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void ForgedCheckpointOnUnrelatedCommand_IsAtomic()
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var transition = Undo(battle);
            var forged = transition.WithFinalState(transition.FinalState.With(
                movementUndo: transition.Input.State.MovementUndo));
            var stores = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
        }

        [TestCase("distance")]
        [TestCase("extra-actor")]
        [TestCase("world")]
        [TestCase("source")]
        public void ForgedUndoFacts_RejectEveryStoreAfterCharacterPreparation(string defect)
        {
            using var battle = Battle();
            battle.Move(battle.Actor, new CellCoord(1, 1));
            var valid = Undo(battle);
            var returned = valid.Characters.Characters.Single();
            var after = defect == "distance" ? returned.Final.With(stageRecord: returned.Expected.StageRecord) : returned.Final;
            var changes = new[] { new CharacterStateChange(returned.Expected, after) }.AsEnumerable();
            if (defect == "extra-actor")
            {
                var other = battle.Character(EntityKey.Unit_DynamiteHammer);
                changes = changes.Append(new CharacterStateChange(other, other.WithPlacement(other.Area, other.ReserveSlotIndex,
                    new CellCoord(2, 1), other.Rotation, new[] { new CellCoord(2, 1) })));
            }
            var u = valid.UndoMovement;
            var result = new MovementUndoResult(u.ActorId, u.MoveCommandNumber + (defect == "source" ? 1 : 0),
                u.From, u.To, u.Rotation, defect == "distance" ? 0 : u.DistanceReverted);
            var forged = new CombatCommandTransition(valid.Input, true, "",
                new CharacterChanges(changes.ToArray(), Array.Empty<SoulCountChange>()), valid.Field,
                defect == "world" ? valid.FinalState.With(itemRandomState: valid.FinalState.ItemRandomState + 1) : valid.FinalState,
                valid.Outcome, events: new[] { new CombatCharacterEvent(CombatEventKind.Returned, returned.Expected, after) },
                actionUseSettlement: valid.ActionUseSettlement, undoMovement: result);
            var stores = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            battle.Buffers.BattleWriteSink.Apply(valid);
        }

        private static void Apply(ConfiguredMovementBattle battle, CharacterCombatFacts before, CharacterCombatFacts after) =>
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(before, after) },
                Array.Empty<SoulCountChange>()));

        private static CombatCommandTransition Undo(ConfiguredMovementBattle battle)
        {
            var b = battle.Buffers;
            int number = b.CombatViewWriteSink.Submit(battle.EndPlayerTurn());
            var i = battle.Publication().Assembler.Assemble(new CombatActionRequest(number, CombatCommandKind.UndoMove,
                battle.Actor.EntityId, default, expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            var token = i.State.MovementUndo.Checkpoints.Single(c => c.ActorId == i.ActorId).MoveCommandNumber;
            var input = new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim, i.State, i.Characters, i.Profiles,
                i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, i.TargetCell, i.Rotation, i.Battle,
                i.ProcessingKind, i.Path, i.MovementContext, i.BattleInstanceId, i.ArtifactItems, i.EnvironmentBindings,
                i.ObstacleContents, i.ItemKey, i.ItemInventory, i.ItemDefinitions, i.GoldBalance, i.FameBalance,
                i.Membership, i.ExpectedBattleInstanceId, i.ExpectedControllerTurnSequence, token);
            return CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
        }
    }
}
