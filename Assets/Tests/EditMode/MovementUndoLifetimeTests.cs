using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class MovementUndoLifetimeTests
    {
        private static readonly EntityId B = new EntityId(20);

        [Test]
        public void RejectedOrSpeculativeUse_DoesNotCommit_AndCopiesRetainExecutionIdentity()
        {
            var moved = MovementUndoTestData.Run(MovementUndoTestData.WithEnemy(MovementUndoTestData.Route()));
            var input = MovementUndoTestData.Command(MovementUndoTestData.After(moved), CombatCommandKind.Attack,
                aim: AttackAim.ForCell(new CellCoord(2, 2)));
            CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var speculative);
            Assert.That(speculative.MovementEffectUses, Is.Empty);
            Assert.That(input.State.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var rejected = CombatCommandCalculator.Reject(input, "Rejected");
            Assert.That(rejected.MovementEffectUses, Is.Empty);
            var attack = MovementUndoTestData.Run(input);
            var uses = attack.MovementEffectUses.ToArray();
            var copy = attack.WithMovementEffectUses(uses).WithFinalState(attack.FinalState);
            uses[0] = null;
            Assert.That(copy.MovementEffectUses.Single().SameValues(attack.MovementEffectUses.Single()), Is.True);
            Assert.Throws<ArgumentException>(() => rejected.WithMovementEffectUses(attack.MovementEffectUses));
            Assert.Throws<ArgumentException>(() => speculative.WithMovementEffectUses(attack.MovementEffectUses));
            var merged = CombatCommandComposition.Merge(input, new[] { attack });
            Assert.That(merged.MovementEffectUses.Single().SameValues(attack.MovementEffectUses.Single()), Is.True);
            Assert.That(MovementUndoPolicy.Reconcile(merged).FinalState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(input.State.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
        }


        [TestCase(false)]
        [TestCase(true)]
        public void ProposedSoulAdditionInvalidatesOnlyMatchingOrigin(bool atOrigin)
        {
            var input = WithB(MovementUndoTestData.Route());
            var bMove = MoveB(input);
            var request = MovementUndoTestData.Command(MovementUndoTestData.After(bMove), CombatCommandKind.Move,
                actor: input.ActorId, path: input.Path);
            var moved = CombatMovementCalculator.Calculate(request, Array.Empty<IMovementTileRule>());
            var soul = new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Trait, 2, (int)TraitKey.CallOfBlood, "main"),
                atOrigin ? new CellCoord(1, 4) : new CellCoord(6, 6), 2, 1);
            var field = new SoulFieldChange(request.Souls, new SoulFieldSnapshot(2, new[] { soul }));
            // 사망 영혼 생산은 현재 보류다. 이 경우는 게시 전 불변 제안의 범위 계약만 검증한다.
            var proposed = new CombatCommandTransition(request, true, "", moved.Characters, field, moved.FinalState, moved.Outcome,
                events: moved.Events.Append(new CombatSoulEvent(field)).ToArray(), pathMovement: moved.PathMovement,
                actionUseSettlement: moved.ActionUseSettlement);
            var result = MovementUndoPolicy.Reconcile(proposed);
            Assert.That(result.FinalState.MovementUndo.TryGet(B, out _), Is.EqualTo(!atOrigin));
        }

        [Test]
        public void AMoveBMoveInterleavedReturnsConsumeOnlyOwnToken()
        {
            var first = MovementUndoTestData.Run(WithB(MovementUndoTestData.Route()));
            var second = MoveB(MovementUndoTestData.After(first));
            Assert.That(second.FinalState.MovementUndo.Checkpoints.Count, Is.EqualTo(2));
            var undoA = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(second), CombatCommandKind.UndoMove,
                first.Input.Number, first.Input.ActorId));
            Assert.That(undoA.Accepted, Is.True, undoA.Reason);
            Assert.That(undoA.FinalState.MovementUndo.Checkpoints.Single().ActorId, Is.EqualTo(B));
            var undoB = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(undoA), CombatCommandKind.UndoMove,
                second.Input.Number, B));
            Assert.That(undoB.Accepted, Is.True, undoB.Reason);
            Assert.That(undoB.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AAttackOrRemoteKillPreservesBUndo(bool kill)
        {
            var input = WithB(MovementUndoTestData.WithEnemy(MovementUndoTestData.Route()));
            if (kill) input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(1, 100)) : c).ToArray());
            input = input.WithPersistentFacts(input.Owned, input.ItemInventory, 0, 0);
            var first = MovementUndoTestData.Run(input);
            var second = MoveB(MovementUndoTestData.After(first));
            second.FinalState.MovementUndo.TryGet(B, out var originalB);
            var attack = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(second), CombatCommandKind.Attack,
                actor: input.ActorId, aim: AttackAim.ForCell(new CellCoord(2, 2))));
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.FinalState.MovementUndo.TryGet(input.ActorId, out _), Is.False);
            var used = attack.MovementEffectUses.Single();
            Assert.That(used.Movement, Is.EqualTo(first.FinalState.MovementUndo.Checkpoints.Single().Movement));
            Assert.That(used.Kind, Is.EqualTo(MovementEffectUseKind.ParticipantAction));
            Assert.That(used.ConsumerId, Is.EqualTo(input.ActorId));
            Assert.That(attack.Events.Contains(used.Execution), Is.True);
            Assert.That(attack.FinalState.MovementUndo.TryGet(B, out var preserved), Is.True);
            Assert.That(preserved, Is.SameAs(originalB));
            if (kill)
            {
                Assert.That(attack.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Died), Is.True);
                Assert.That(attack.Field.Final.SameValues(attack.Field.Expected), Is.True);
            }
            Assert.That(MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(attack), CombatCommandKind.UndoMove,
                second.Input.Number, B)).Accepted, Is.True);
        }

        [Test]
        public void OccupiedOriginRejectsThenUnblockingPreservesToken()
        {
            var input = WithB(MovementUndoTestData.Route(), new CellCoord(0, 1));
            var moved = MovementUndoTestData.Run(input);
            var blocker = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(moved), CombatCommandKind.Move,
                actor: B, path: new MovementPath(new[] { new CellCoord(0, 1), new CellCoord(1, 1) })));
            var blocked = MovementUndoTestData.Command(MovementUndoTestData.After(blocker), CombatCommandKind.UndoMove, moved.Input.Number, input.ActorId);
            Assert.That(MovementUndoCalculator.Evaluate(blocked).Failure, Is.EqualTo(MovementUndoFailureReason.DestinationOccupied));
            var rejected = MovementUndoTestData.Run(blocked);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.FinalState.MovementUndo, Is.SameAs(blocker.FinalState.MovementUndo));
            var unblocked = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(blocker), CombatCommandKind.UndoMove, blocker.Input.Number, B));
            var request = MovementUndoTestData.Command(MovementUndoTestData.After(unblocked), CombatCommandKind.UndoMove, moved.Input.Number, input.ActorId);
            Assert.That(MovementUndoCalculator.Evaluate(request).CanUndo, Is.True);
            Assert.That(MovementUndoTestData.Run(request).Accepted, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReturnIntoNewPassiveCoverageRejectsWithoutEffects(bool cohesion)
        {
            var input = MovementUndoTestData.Route();
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(B.Value, EntityKey.Unit_Demon, new CellCoord(0, 3), Owner.Player)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo,
                cohesion ? (TraitSpec)new CohesionTraitSpec(2, 5, 1, 2) : new SoloActionTraitSpec(5, 1, 0, 2));
            var initialized = TraitPresenceReconciler.Calculate(input, input.Characters);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c =>
                initialized.Characters.SingleOrDefault(change => change.Final.EntityId == c.EntityId)?.Final ?? c).ToArray());
            var moved = MovementUndoTestData.Run(input);
            Assert.That(moved.FinalState.MovementUndo.TryGet(input.ActorId, out _), Is.True);
            var nearOrigin = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(moved), CombatCommandKind.Move,
                actor: B, path: new MovementPath(new[] { new CellCoord(0, 3), new CellCoord(0, 2) })));
            Assert.That(nearOrigin.FinalState.MovementUndo.TryGet(input.ActorId, out var checkpoint), Is.True);
            var request = MovementUndoTestData.Command(MovementUndoTestData.After(nearOrigin), CombatCommandKind.UndoMove, moved.Input.Number, input.ActorId);
            var result = MovementUndoTestData.Run(request);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(MovementUndoFailureReason.ReturnWouldCauseEffect.ToString()));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.MovementUndo.Checkpoints.Single(c => c.ActorId == input.ActorId), Is.SameAs(checkpoint));
        }

        [Test]
        public void NewTrailAtBOriginTemporarilyBlocksWithoutDiscardingCheckpoint()
        {
            var input = WithB(MovementUndoTestData.Route());
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(CombatCommandTestData.Character(30,
                EntityKey.Unit_Demon, new CellCoord(0, 4), Owner.Player)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new DustScreenTraitSpec(1, 20, 1));
            var first = MovementUndoTestData.Run(input);
            var second = MoveB(MovementUndoTestData.After(first));
            var trail = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(second), CombatCommandKind.Move,
                actor: new EntityId(30), path: new MovementPath(new[] { new CellCoord(0, 4), new CellCoord(1, 4) })));
            Assert.That(trail.Accepted, Is.True, trail.Reason);
            Assert.That(trail.FinalState.MovementUndo.TryGet(B, out _), Is.True);
            Assert.That(trail.FinalState.MovementUndo.TryGet(input.ActorId, out _), Is.True);
            var blocked = MovementUndoTestData.Command(MovementUndoTestData.After(trail), CombatCommandKind.UndoMove, second.Input.Number, B);
            Assert.That(MovementUndoCalculator.Evaluate(blocked).Failure, Is.EqualTo(MovementUndoFailureReason.DestinationOccupied));
            var freed = MovementUndoTestData.Run(MovementUndoTestData.Undo(trail));
            Assert.That(freed.Accepted, Is.True, freed.Reason);
            Assert.That(MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(freed),
                CombatCommandKind.UndoMove, second.Input.Number, B)).Accepted, Is.True);
        }

        [Test]
        public void OriginChangedThenRestoredWithinCommandStillInvalidates()
        {
            var source = new DestinationTileModifierFact(1, new CellCoord(1, 4), false, Array.Empty<Owner>(), 0, 0);
            var input = WithB(MovementUndoTestData.Route());
            var environment = new MovementEnvironmentFacts(new[] { source });
            input = CombatCommandTestData.With(input, state: input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)));
            var movedB = MoveB(input);
            var current = MovementUndoTestData.Command(MovementUndoTestData.After(movedB), CombatCommandKind.Move, actor: input.ActorId,
                path: input.Path);
            var projection = new MovementProjection(current);
            projection.BeginOrdinaryMove(); projection.Enter(new CellCoord(2, 1));
            projection.Stop(MovementStopReason.NextStepInvalid);
            var active = new DestinationTileModifierFact(1, source.Cell, true, Array.Empty<Owner>(), 0, 0);
            projection.ChangeSource(source, new MovementStepEffects(expectedSource: source, finalSource: active));
            projection.ChangeSource(active, new MovementStepEffects(expectedSource: active, finalSource: source));
            var result = MovementUndoPolicy.Reconcile(projection.Transition());
            Assert.That(result.FinalState.Environment.SameValues(current.State.Environment), Is.True);
            Assert.That(result.FinalState.MovementUndo.TryGet(B, out _), Is.False);
        }

        [Test]
        public void EndTurnClearsAllCheckpointsWithoutRequiringUndo()
        {
            var first = MovementUndoTestData.Run(WithB(MovementUndoTestData.Route()));
            var second = MoveB(MovementUndoTestData.After(first));
            var ended = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(second), CombatCommandKind.EndPlayerTurn));
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            Assert.That(ended.FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [TestCase(CombatEventKind.CombatEntered)]
        [TestCase(CombatEventKind.ControllerTurnStarted)]
        [TestCase(CombatEventKind.NightEnded)]
        public void LifecycleFactsClearRecords(CombatEventKind kind)
        {
            var first = MovementUndoTestData.Run(WithB(MovementUndoTestData.Route()));
            var request = MovementUndoTestData.Command(MovementUndoTestData.After(first), CombatCommandKind.Move, actor: B,
                path: new MovementPath(new[] { new CellCoord(1, 4), new CellCoord(2, 4) }));
            var resolved = CombatMovementCalculator.Calculate(request, Array.Empty<IMovementTileRule>());
            var boundary = new CombatTurnEvent(kind, default, request.State.Turn, resolved.FinalState.Turn);
            var combined = CombatCommandComposition.WithEvents(resolved, resolved.Events.Append(boundary).ToArray());
            Assert.That(MovementUndoPolicy.Reconcile(combined).FinalState.MovementUndo.Checkpoints, Is.Empty);
        }

        [Test]
        public void OldBattleCheckpointCannotAuthorizeNewBattle()
        {
            var first = MovementUndoTestData.Run(MovementUndoTestData.Route());
            var state = MovementUndoTestData.After(first);
            var current = new CombatCommandInput(2, CombatCommandKind.UndoMove, state.ActorId, default, state.State, state.Characters,
                state.Profiles, state.Main, state.Owned, state.SoulCounts, state.Souls, state.Artifacts, battleInstanceId: 2,
                expectedBattleInstanceId: 2, expectedControllerTurnSequence: 1, expectedMoveCommandNumber: 1);
            Assert.That(MovementUndoCalculator.Evaluate(current).Failure, Is.EqualTo(MovementUndoFailureReason.StaleBattle));
            Assert.That(MovementUndoTestData.Run(current).Accepted, Is.False);
        }

        private static CombatCommandInput WithB(CombatCommandInput input, CellCoord? origin = null) => TraitCombatFixtures.Characters(input,
            input.Characters.Append(CombatCommandTestData.Character(B.Value, EntityKey.Unit_Akimbo, origin ?? new CellCoord(1, 4), Owner.Player)).ToArray());

        private static CombatCommandTransition MoveB(CombatCommandInput input) => MovementUndoTestData.Run(MovementUndoTestData.Command(input,
            CombatCommandKind.Move, actor: B, path: new MovementPath(new[] { new CellCoord(1, 4), new CellCoord(2, 4) })));
    }
}
