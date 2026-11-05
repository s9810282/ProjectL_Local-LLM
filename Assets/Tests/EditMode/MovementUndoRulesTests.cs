using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class MovementUndoRulesTests
    {
        [Test]
        public void OutOfBoundsSavedOriginRejectsWithoutChangingFacts()
        {
            var input = MovementTestData.SafeRoute(new CellCoord(3, 1), new CellCoord(2, 1));
            var moved = MovementUndoTestData.Run(input);
            var current = MovementUndoTestData.After(moved);
            var s = current.State;
            var smaller = new CombatCommandState(3, s.BoardHeight, s.Phase, s.IsNight, s.Structures, s.ThornFields, s.Turn,
                s.Environment, s.EnvironmentProgress, s.WallBlocking, s.Objective, s.Allocation, s.ItemRandomState, s.Rewards, s.TraitTurn, s.MovementUndo);
            current = current.WithWorld(smaller, current.Characters, current.Main, current.SoulCounts, current.Souls, current.EnvironmentBindings);
            var result = MovementUndoTestData.Run(MovementUndoTestData.Command(current, CombatCommandKind.UndoMove, moved.Input.Number));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(MovementUndoFailureReason.DestinationInvalid.ToString()));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState, Is.SameAs(smaller));
        }

        [Test]
        public void MainUndoPreservesPersistentHealthAndOtherUnits()
        {
            var input = MovementUndoTestData.Command(MovementUndoTestData.Route(), CombatCommandKind.Move, actor: new EntityId(10),
                path: new MovementPath(new[] { new CellCoord(7, 7), new CellCoord(6, 7) }));
            var moved = MovementUndoTestData.Run(input);
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.Characters.Main, Is.Null);
            Assert.That(undo.Characters.Characters.Count, Is.EqualTo(1));
            Assert.That(MovementTestData.Actor(undo).StageRecord.DistanceMoved, Is.Zero);
            Assert.That(MovementTestData.Actor(undo).Anchor, Is.EqualTo(new CellCoord(7, 7)));
        }

        [Test]
        public void QueryReturnsSavedOriginAndMoveTokenWithoutMutatingState()
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.Route());
            var current = MovementUndoTestData.After(moved);
            var query = CombatCommandCalculator.GetActionAvailability(current);
            Assert.That(query.MovementUndo.CanUndo, Is.True);
            Assert.That(query.MovementUndo.Destination, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(query.MovementUndo.MoveCommandNumber, Is.EqualTo(moved.Input.Number));
            Assert.That(query.SourceRevision, Is.Zero);
            Assert.That(current.State.MovementUndo, Is.SameAs(moved.FinalState.MovementUndo));
        }

        [TestCase("actor", MovementUndoFailureReason.ActorChanged)]
        [TestCase("origin", MovementUndoFailureReason.DestinationInvalid)]
        [TestCase("deployment", MovementUndoFailureReason.WrongPhase)]
        [TestCase("controller", MovementUndoFailureReason.WrongController)]
        [TestCase("no-record", MovementUndoFailureReason.NoCheckpoint)]
        public void ChangedOrUnavailableWorldRejectsWithoutMutation(string change, MovementUndoFailureReason reason)
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.Route());
            var input = MovementUndoTestData.After(moved);
            if (change == "actor") input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.WithHealth(new HealthStateSnapshot(99, 100)) : c).ToArray());
            if (change == "origin")
            {
                var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                    new DestinationTileModifierFact(1, new CellCoord(1, 1), false, Array.Empty<Owner>(), 0, 0) });
                input = CombatCommandTestData.With(input, state: input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)));
            }
            if (change == "deployment") input = CombatCommandTestData.With(input, state: input.State.With(turn: new CombatTurnFacts(StagePhase.Deployment)));
            if (change == "controller") input = CombatCommandTestData.With(input, state: input.State.With(phase: CombatFlowPhase.Automatic,
                turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: 1)));
            if (change == "no-record") input = CombatCommandTestData.With(input, state: input.State.With(movementUndo: MovementUndoState.Empty));
            var request = MovementUndoTestData.Command(input, CombatCommandKind.UndoMove, moved.Input.Number);
            var result = MovementUndoTestData.Run(request);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason.ToString()));
            Assert.That(result.FinalState, Is.SameAs(input.State));
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [Test]
        public void EnemyCannotUsePlayerUndoToken()
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.WithEnemy(MovementUndoTestData.Route()));
            var request = MovementUndoTestData.Command(MovementUndoTestData.After(moved), CombatCommandKind.UndoMove, moved.Input.Number, new EntityId(2));
            Assert.That(MovementUndoCalculator.Evaluate(request).Failure, Is.EqualTo(MovementUndoFailureReason.ActorUnavailable));
            Assert.That(MovementUndoTestData.Run(request).Accepted, Is.False);
        }

        [Test]
        public void SafeMoveCanUndo()
        {
            var input = MovementUndoTestData.Route();
            var moved = MovementUndoTestData.Run(input);
            Assert.That(moved.FinalState.MovementUndo.TryGet(input.ActorId, out var checkpoint), Is.True);
            Assert.That(checkpoint.RecordedDistance, Is.EqualTo(3));
            Assert.That(checkpoint.MoveCommandNumber, Is.EqualTo(input.Number));
            Assert.That(checkpoint.ExpectedActorAfterMove.SameValues(MovementTestData.Actor(moved)), Is.True);
            var request = MovementUndoTestData.Undo(moved);
            Assert.That(MovementUndoCalculator.Evaluate(request).CanUndo, Is.True);
            var undo = MovementUndoTestData.Run(request);
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.UndoMovement.DistanceReverted, Is.EqualTo(3));
            Assert.That(undo.UndoMovement.From, Is.EqualTo(new CellCoord(4, 1)));
            Assert.That(undo.UndoMovement.To, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(undo.FinalState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(undo.Events.Count, Is.EqualTo(1));
            Assert.That(undo.Events.Single().Kind, Is.EqualTo(CombatEventKind.Returned));
            Assert.That(MovementTestData.Actor(undo).ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(undo.PathMovement, Is.Null);
            Assert.That(undo.Field.Final.SameValues(request.Souls), Is.True);
        }

        [Test]
        public void ShortMoveStillUsesOneAction()
        {
            var moved = MovementUndoTestData.Run(MovementTestData.Route(MovementUndoTestData.Route(), new CellCoord(1, 1), new CellCoord(2, 1)));
            var next = MovementUndoTestData.Command(MovementUndoTestData.After(moved), CombatCommandKind.Move,
                path: new MovementPath(new[] { new CellCoord(2, 1), new CellCoord(3, 1) }));
            var rejected = MovementUndoTestData.Run(next);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo("ActionAlreadyUsed"));
            Assert.That(rejected.FinalState.MovementUndo, Is.SameAs(next.State.MovementUndo));
        }

        [Test]
        public void UndoSubtractsOnlyRecordedDistance()
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.Route(10));
            Assert.That(MovementTestData.Actor(moved).StageRecord.DistanceMoved, Is.EqualTo(13));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(MovementTestData.Actor(undo).StageRecord.SameValues(CharacterStageRecord.Empty.Add(2, 7, 10, 1)), Is.True);
            var again = MovementUndoTestData.Command(MovementUndoTestData.After(undo), CombatCommandKind.Move,
                path: new MovementPath(new[] { new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(1, 3) }));
            var remMoved = MovementUndoTestData.Run(again);
            Assert.That(remMoved.Accepted, Is.True, remMoved.Reason);
            Assert.That(MovementTestData.Actor(remMoved).StageRecord.DistanceMoved, Is.EqualTo(12));
        }

        [Test]
        public void UndoThenRemovementUsesFullCategoryLimit()
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.Route(10));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
            var next = MovementUndoTestData.Command(MovementUndoTestData.After(undo), CombatCommandKind.Move,
                path: new MovementPath(new[] { new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(1, 3), new CellCoord(1, 4), new CellCoord(1, 5) }));
            var result = MovementUndoTestData.Run(next);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(4));
            Assert.That(MovementTestData.Actor(result).StageRecord.DistanceMoved, Is.EqualTo(14));
        }

        [Test]
        public void RepeatedMoveUndoHasZeroNetDistance()
        {
            var current = MovementUndoTestData.Route(10);
            for (int i = 0; i < 4; i++)
            {
                var moved = MovementUndoTestData.Run(current);
                var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
                Assert.That(undo.Accepted, Is.True, undo.Reason);
                Assert.That(MovementTestData.Actor(undo).StageRecord.DistanceMoved, Is.EqualTo(10));
                current = MovementUndoTestData.Command(MovementUndoTestData.After(undo), CombatCommandKind.Move, path: moved.Input.Path);
            }
        }

        [Test]
        public void AttackAfterUndoOnlyOnce()
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.WithEnemy(MovementUndoTestData.Route()));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
            var attack = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(undo), CombatCommandKind.Attack,
                aim: AttackAim.ForCell(new CellCoord(2, 2))));
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            var again = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(attack), CombatCommandKind.Attack,
                aim: AttackAim.ForCell(new CellCoord(2, 2))));
            Assert.That(again.Accepted, Is.False);
        }

        [Test]
        public void AttackFirstCannotUndo()
        {
            var input = MovementUndoTestData.Command(MovementUndoTestData.WithEnemy(MovementUndoTestData.Route()), CombatCommandKind.Attack, aim: AttackAim.ForCell(new CellCoord(2, 2)));
            var attacked = MovementUndoTestData.Run(input);
            Assert.That(attacked.Accepted, Is.True, attacked.Reason);
            Assert.That(MovementUndoCalculator.Evaluate(MovementUndoTestData.After(attacked)).CanUndo, Is.False);
        }

        [TestCase(0, 1, 1)]
        [TestCase(99, 1, 1)]
        [TestCase(1, 99, 1)]
        [TestCase(1, 1, 99)]
        public void FailedUndoDoesNotSubtract(int token, long battle, long turn)
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.Route(10));
            var source = MovementUndoTestData.After(moved);
            var request = MovementUndoTestData.Command(source, CombatCommandKind.UndoMove, token, expectedBattle: battle, expectedTurn: turn);
            var result = MovementUndoTestData.Run(request);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState, Is.SameAs(source.State));
            Assert.That(MovementTestData.Actor(result).StageRecord.DistanceMoved, Is.EqualTo(13));
        }

        [Test]
        public void ConsumedTokenCannotSubtractTwice()
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.Route(10));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Undo(moved));
            var again = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(undo), CombatCommandKind.UndoMove, moved.Input.Number));
            Assert.That(again.Accepted, Is.False);
            Assert.That(MovementTestData.Actor(again).StageRecord.DistanceMoved, Is.EqualTo(10));
        }

        [Test]
        public void RejectedCommandKeepsCheckpoint()
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.Route());
            var rejected = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(moved), CombatCommandKind.Attack,
                aim: AttackAim.ForCell(new CellCoord(99, 99))));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.FinalState.MovementUndo, Is.SameAs(moved.FinalState.MovementUndo));
            Assert.That(MovementUndoTestData.Run(MovementUndoTestData.Undo(moved)).Accepted, Is.True);
        }

        [Test]
        public void CommitDoesNotAddDistanceAgain()
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.WithEnemy(MovementUndoTestData.Route(10)));
            var attack = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(moved), CombatCommandKind.Attack,
                aim: AttackAim.ForCell(new CellCoord(2, 2))));
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.FinalState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(MovementTestData.Actor(attack).StageRecord.DistanceMoved, Is.EqualTo(13));
        }

        [Test]
        public void PreviewDoesNotCreateCheckpoint()
        {
            var input = MovementUndoTestData.Route();
            var preview = CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanMove, Is.True);
            Assert.That(input.State.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void SafePartialMoveCanUndo()
        {
            var projection = new MovementProjection(MovementUndoTestData.Route());
            projection.BeginOrdinaryMove();
            projection.Enter(new CellCoord(2, 1));
            projection.Stop(MovementStopReason.NextStepInvalid);
            var resolved = MovementUndoPolicy.Reconcile(projection.Transition());
            Assert.That(resolved.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(resolved.FinalState.MovementUndo.TryGet(resolved.Input.ActorId, out var checkpoint), Is.True);
            Assert.That(checkpoint.RecordedDistance, Is.EqualTo(1));
            Assert.That(MovementUndoTestData.Run(MovementUndoTestData.Undo(resolved)).Accepted, Is.True);
        }

        [Test]
        public void ZeroEnteredCellsCannotUndo()
        {
            var projection = new MovementProjection(MovementUndoTestData.Route());
            projection.BeginOrdinaryMove();
            projection.Stop(MovementStopReason.NextStepInvalid);
            var resolved = MovementUndoPolicy.Reconcile(projection.Transition());
            Assert.That(resolved.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }
    }
}
