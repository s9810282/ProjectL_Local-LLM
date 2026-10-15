using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class MovementContractTests
    {
        [Test]
        public void EnvironmentFacts_CopyAndCompareByValue()
        {
            var owners = new[] { Owner.Enemy, Owner.Player };
            var source = new DestinationTileModifierFact(7, CellCoord.Zero, true, owners, -2, 5);
            var tiles = new MovementTileFact[] { source };
            var environment = new MovementEnvironmentFacts(tiles);
            owners[0] = Owner.Player;
            tiles[0] = new DestinationTileModifierFact(9, CellCoord.Right, false, Array.Empty<Owner>(), 0, 0);
            var equal = new MovementEnvironmentFacts(new MovementTileFact[]
            {
                new DestinationTileModifierFact(7, CellCoord.Zero, true, new[] { Owner.Player, Owner.Enemy }, -2, 5)
            });
            Assert.That(environment.SameValues(equal), Is.True);
            Assert.That(environment.Tiles.Single(), Is.SameAs(source));
            Assert.That(source.KnownTo, Is.EquivalentTo(new[] { Owner.Player, Owner.Enemy }));
            Assert.That(source.SameValues(new DestinationTileModifierFact(7, CellCoord.Zero, false,
                new[] { Owner.Player, Owner.Enemy }, -2, 5)), Is.False);
            Assert.That(source.SameValues(new DestinationTileModifierFact(7, CellCoord.Zero, true,
                new[] { Owner.Enemy }, -2, 5)), Is.False);
            Assert.Throws<NotSupportedException>(() => ((IList<MovementTileFact>)environment.Tiles).Clear());
        }

        [Test]
        public void PositionModifier_DoesNotOverwriteArtifactBonuses()
        {
            var actor = SafeInput().Characters.First();
            var ids = new long[] { 7 };
            var modifier = new PositionTileModifier(actor.Anchor, ids, -2, 5);
            var final = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey, actor.Owner,
                actor.OwnershipRole, actor.Area, actor.Health, actor.Stats, actor.ReserveSlotIndex, actor.Anchor,
                actor.Rotation, actor.OccupiedCells, actor.MovementCheckpointAnchor, actor.CommittedMoveDistance,
                actor.PendingMoveActionCost, 2, 3, 2, 4, modifier);
            ids[0] = 9;
            var copied = CombatCommandTestData.With(final, hp: actor.Health.CurrentHealth - 1);
            Assert.That(copied.TemporaryAttackBonus, Is.EqualTo(2));
            Assert.That(copied.TemporaryMaxHealthBonus, Is.EqualTo(3));
            Assert.That(copied.PendingMoveDistance, Is.EqualTo(2));
            Assert.That(copied.AttackRefundEligibleMoveCost, Is.EqualTo(4));
            Assert.That(copied.PositionModifier.SameValues(new PositionTileModifier(actor.Anchor,
                new long[] { 7 }, -2, 5)), Is.True);
            Assert.That(actor.PositionModifier, Is.SameAs(PositionTileModifier.None));
        }

        [Test]
        public void Context_ExcludesHiddenEnvironment_AndRetainsNewActorFacts()
        {
            var input = SafeInput();
            var actor = input.Characters.First();
            var visible = new DestinationTileModifierFact(7, CellCoord.Zero, true, new[] { Owner.Player, Owner.Enemy }, 1, 2);
            var hidden = new DestinationTileModifierFact(8, CellCoord.Right, true, new[] { Owner.Enemy }, 2, 3);
            var context = new MovementPreviewContext(1, Owner.Player, actor, CombatCommandKind.Move, input.Path,
                visibleEnvironment: new MovementEnvironmentFacts(new MovementTileFact[] { visible, hidden }));
            Assert.That(context.VisibleEnvironment.Tiles.Select(tile => tile.Id), Is.EqualTo(new long[] { 7 }));
            Assert.That(context.VisibleEnvironment.Tiles.Single().KnownTo, Is.EqualTo(new[] { Owner.Player }));
            Assert.That(visible.KnownTo, Is.EquivalentTo(new[] { Owner.Player, Owner.Enemy }));
            Assert.That(context.PendingMoveDistance, Is.EqualTo(actor.PendingMoveDistance));
            Assert.That(context.AttackRefundEligibleMoveCost, Is.EqualTo(actor.AttackRefundEligibleMoveCost));
            Assert.That(context.PositionModifier, Is.SameAs(actor.PositionModifier));
        }

        [Test]
        public void TileRequests_CopyValues_AndRestrictSourceIdentity()
        {
            var request = new MovementHealthRequest(new EntityId(1), 2);
            var requests = new[] { request };
            var source = new DestinationTileModifierFact(7, CellCoord.Zero, true, new[] { Owner.Player }, 0, 0);
            var consumed = new DestinationTileModifierFact(7, CellCoord.Zero, false, new[] { Owner.Player }, 0, 0);
            var effects = new MovementStepEffects(damage: requests, expectedSource: source, finalSource: consumed);
            requests[0] = new MovementHealthRequest(new EntityId(2), 9);
            Assert.That(effects.Damage.Single(), Is.SameAs(request));
            Assert.That(effects.FinalSource, Is.SameAs(consumed));
            Assert.Throws<ArgumentException>(() => new MovementStepEffects(expectedSource: source));
            Assert.Throws<ArgumentException>(() => new MovementStepEffects(expectedSource: source,
                finalSource: new DestinationTileModifierFact(8, CellCoord.Zero, false, new[] { Owner.Player }, 0, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovementHealthRequest(new EntityId(1), -1));
        }

        [Test]
        public void Path_CopiesCells_AndCountsEdges()
        {
            var cells = new[] { new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 2) };
            var path = new MovementPath(cells);
            cells[1] = new CellCoord(7, 7);
            Assert.That(path.Cells, Is.EqualTo(new[] { new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 2) }));
            Assert.That(path.StepCount, Is.EqualTo(2));
            Assert.That(path.Origin, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(path.Endpoint, Is.EqualTo(new CellCoord(2, 2)));
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)path.Cells)[1] = CellCoord.Zero);
        }

        [Test]
        public void Path_RequiresOrigin_ButLeavesAdmissionToCore()
        {
            Assert.Throws<ArgumentNullException>(() => new MovementPath(null));
            Assert.Throws<ArgumentException>(() => new MovementPath(Array.Empty<CellCoord>()));
            Assert.That(new MovementPath(new[] { CellCoord.Zero }).StepCount, Is.Zero);
            var draft = new MovementPath(new[] { CellCoord.Zero, new CellCoord(9, 9), CellCoord.Zero });
            Assert.That(draft.StepCount, Is.EqualTo(2));
        }

        [Test]
        public void Result_CopiesEnteredPrefix_AndPreservesDeadStop()
        {
            var path = new MovementPath(new[] { new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 2), new CellCoord(3, 2) });
            var entered = new[] { new CellCoord(2, 1), new CellCoord(2, 2) };
            var result = new MovementPathResult(path, entered, entered[1], 2, 2, MovementStopReason.Dead, true);
            entered[1] = new CellCoord(7, 7);
            Assert.That(result.EnteredCells, Is.EqualTo(new[] { new CellCoord(2, 1), new CellCoord(2, 2) }));
            Assert.That(result.FinalStop, Is.EqualTo(new CellCoord(2, 2)));
            Assert.That(result.DistanceSpent, Is.EqualTo(2));
            Assert.That(result.StopReason, Is.EqualTo(MovementStopReason.Dead));
            Assert.That(result.ReturnBarrierOccurred, Is.True);
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)result.EnteredCells)[0] = CellCoord.Zero);
        }

        [Test]
        public void Result_RejectsNonPrefixOrUnenteredBilling_AndAllowsForcedFinalStop()
        {
            var path = new MovementPath(new[] { CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0) });
            Assert.Throws<ArgumentException>(() => new MovementPathResult(path, new[] { CellCoord.Up }, CellCoord.Up,
                1, 1, MovementStopReason.Dead, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovementPathResult(path, new[] { CellCoord.Right }, CellCoord.Right,
                1, 2, MovementStopReason.Dead, true));
            Assert.Throws<ArgumentException>(() => new MovementPathResult(path, new[] { CellCoord.Right }, CellCoord.Right,
                1, 1, MovementStopReason.Completed, false));
            var result = new MovementPathResult(path, new[] { CellCoord.Right }, new CellCoord(4, 4),
                0, 1, MovementStopReason.ForcedRelocation, true);
            Assert.That(result.ActionCostSpent, Is.Zero);
            Assert.That(result.FinalStop, Is.EqualTo(new CellCoord(4, 4)));
        }

        [Test]
        public void InputCopies_PreserveExactPathAndContext()
        {
            var input = SafeInput();
            var context = new MovementPreviewContext(1, Owner.Player, input.Characters[0], CombatCommandKind.Move, input.Path);
            input = CombatCommandComposition.Intent(input, CombatCommandKind.Move, path: input.Path, movementContext: context);
            var copied = CombatCommandComposition.Intent(input, CombatCommandKind.Move);
            Assert.That(copied.Path, Is.SameAs(input.Path));
            Assert.That(copied.MovementContext, Is.SameAs(context));
            Assert.That(CombatCommandTestData.With(input).Path, Is.SameAs(input.Path));
            Assert.That(CombatCommandTestData.With(input).MovementContext, Is.SameAs(context));
            Assert.That(CombatAttackQueries.WithAim(input, default).Path, Is.SameAs(input.Path));
            Assert.That(CombatAttackQueries.WithAim(input, default).MovementContext, Is.SameAs(context));
            Assert.That(context.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(context.CurrentActionCost, Is.EqualTo(20));
            Assert.That(context.OccupiedCells, Is.EqualTo(new[] { new CellCoord(1, 3) }));
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)context.OccupiedCells)[0] = CellCoord.Zero);
            Assert.Throws<ArgumentException>(() => new MovementPreviewContext(1, Owner.Player, input.Characters[0],
                CombatCommandKind.Move, input.Path, new CellCoord(1, 3)));
        }

        [Test]
        public void ComposedAttack_RetainsRoutePayload_AndOrderedNonMovementEvents()
        {
            var input = SafeInput();
            var context = new MovementPreviewContext(1, Owner.Player, input.Characters[0], CombatCommandKind.Move, input.Path);
            input = CombatCommandComposition.Intent(input, CombatCommandKind.Attack, movementContext: context);
            var payload = PathResult(input.Path);
            var original = input.Characters[0];
            var moved = CombatCommandTestData.With(original, position: input.Path.Endpoint, ap: 19);
            var moveEvent = new CombatCharacterEvent(CombatEventKind.Moved, original, moved);
            var first = Transition(input, new[] { new CharacterStateChange(original, moved) }, payload, events: new[] { moveEvent });
            var projected = MovementTestData.After(first);
            Assert.That(projected.Path, Is.SameAs(input.Path));
            Assert.That(projected.MovementContext, Is.SameAs(context));
            Assert.That(MovementTestData.Actor(first).Anchor, Is.EqualTo(new CellCoord(2, 3)));
            var damaged = CombatCommandTestData.With(moved, hp: 99);
            var healthEvent = new CombatCharacterEvent(CombatEventKind.HealthChanged, moved, damaged);
            var second = Transition(projected, new[] { new CharacterStateChange(moved, damaged) }, events: new[] { healthEvent });
            var merged = CombatCommandComposition.Merge(input, new[] { first, second });
            Assert.That(merged.Input.Kind, Is.EqualTo(CombatCommandKind.Attack));
            Assert.That(merged.PathMovement, Is.SameAs(payload));
            Assert.That(merged.Events, Is.EqualTo(new CombatEvent[] { moveEvent, healthEvent }));
            Assert.That(merged.Characters.Characters.Single().Expected, Is.SameAs(original));
            Assert.That(merged.Characters.Characters.Single().Final.Health.CurrentHealth, Is.EqualTo(99));
            Assert.That(merged.Characters.Characters.Single().Final.Stats.CurrentActionCost, Is.EqualTo(19));
        }

        [Test]
        public void CompletionCopy_PreservesMovementPayloadAndEvents()
        {
            var input = SafeInput();
            var payload = PathResult(input.Path);
            var outcome = new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.PlayerMainCharacterDied);
            var changes = new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>());
            var events = new CombatEvent[] { new CombatOutcomeEvent(outcome) };
            // 종료 승패는 Finished 국면과 함께여야 한다(계약 검사).
            var s = input.State;
            var finished = new CombatCommandState(s.BoardWidth, s.BoardHeight, CombatFlowPhase.Finished, s.ActiveActorId,
                s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment, s.EnvironmentProgress);
            var transition = new CombatCommandTransition(input, true, "", changes,
                new SoulFieldChange(input.Souls, input.Souls), finished, outcome, events: events, pathMovement: payload);
            var completion = new BattleCompletionChanges(changes, outcome,
                ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(10)));
            var copied = transition.WithCompletion(completion);
            Assert.That(copied.PathMovement, Is.SameAs(payload));
            Assert.That(copied.Events, Is.EqualTo(events));
            events[0] = null;
            Assert.That(copied.Events[0], Is.Not.Null);
        }

        [Test]
        public void Transition_RejectsEndedOutcomeWithoutFinishedPhase_AndForeignCompletionChanges()
        {
            // 제안 내부 일관성(종료 승패↔국면, 명령↔완료 변경)은 Buffer가 아니라 계약이 확인한다.
            var input = SafeInput();
            var outcome = new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.PlayerMainCharacterDied);
            var changes = new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>());
            var field = new SoulFieldChange(input.Souls, input.Souls);
            Assert.Throws<ArgumentException>(() => new CombatCommandTransition(input, true, "", changes, field, input.State, outcome));

            var s = input.State;
            var finished = new CombatCommandState(s.BoardWidth, s.BoardHeight, CombatFlowPhase.Finished, s.ActiveActorId,
                s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn, s.Environment, s.EnvironmentProgress);
            var completion = new BattleCompletionChanges(
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()), outcome,
                ChapterProgressionResult.Defeat(1, new StageNodeId(2), new StageKey(10)));
            Assert.Throws<ArgumentException>(() => new CombatCommandTransition(input, true, "", changes, field, finished,
                outcome, completion: completion));
            Assert.DoesNotThrow(() => new CombatCommandTransition(input, true, "", changes, field, finished, outcome)
                .WithCompletion(completion));
        }

        [Test]
        public void ReturnPayload_SurvivesComposition_AndRejectsMutationOnRejection()
        {
            var input = SafeInput();
            var payload = new CheckpointReturnResult(new CellCoord(1, 3), new CellCoord(1, 3), 6, 6);
            var merged = CombatCommandComposition.Merge(input,
                new[] { Transition(input, Array.Empty<CharacterStateChange>(), returned: payload) });
            Assert.That(merged.ReturnMovement, Is.SameAs(payload));
            Assert.Throws<ArgumentException>(() => new CombatCommandTransition(input, false, "Blocked",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), input.State, default, returnMovement: payload));
            Assert.Throws<ArgumentException>(() => new CombatResult(1, CombatProcessingKind.Command, 1, false,
                "Blocked", returnMovement: payload));
            Assert.Throws<ArgumentException>(() => new CombatResult(1, CombatProcessingKind.Command, 1, false,
                "Blocked", pathMovement: PathResult(input.Path)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CheckpointReturnResult(CellCoord.Zero, CellCoord.Right, -1, 1));
        }

        [Test]
        public void Preview_DistinguishesReturnFromMove_AndDoesNotAuthorizeInvalidPrefix()
        {
            var returned = new CheckpointReturnResult(CellCoord.Right, CellCoord.Zero, 0, 3);
            var response = CombatPreviewResult.ForCheckpointReturn(7,
                new CheckpointReturnPreview(true, MovementFailureReason.None, returned));
            Assert.That(response.CheckpointReturn.Return, Is.SameAs(returned));
            Assert.That(response.Move.HasValue, Is.False);
            Assert.That(response.RequestNumber, Is.EqualTo(7));
            Assert.Throws<ArgumentException>(() => new CheckpointReturnPreview(false, MovementFailureReason.None));
            Assert.Throws<ArgumentException>(() => new CheckpointReturnPreview(false, MovementFailureReason.DestinationOccupied, returned));
            var failed = MovePreview.Failure(MovementFailureReason.DestinationOccupied, 1);
            Assert.That(failed.CanMove, Is.False);
            Assert.That(failed.ValidStepCount, Is.EqualTo(1));
            var path = new MovementPath(new[] { CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0) });
            var success = MovePreview.Success(PathResult(path), Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Base));
            Assert.That(success.PathMovement.RequestedPath, Is.SameAs(path));
            Assert.That(success.ValidStepCount, Is.EqualTo(2));
            Assert.That(success.Destination.HasValue, Is.False);
        }

        private static CombatCommandInput SafeInput() =>
            MovementTestData.SafeRoute(20, 20, 1, new CellCoord(1, 3), new CellCoord(2, 3));

        private static MovementPathResult PathResult(MovementPath path) => new(path,
            path.Cells.Skip(1).ToArray(), path.Endpoint, path.StepCount, path.StepCount, MovementStopReason.Completed, false);

        private static CombatCommandTransition Transition(CombatCommandInput input, CharacterStateChange[] changes,
            MovementPathResult path = null, CheckpointReturnResult returned = null, CombatEvent[] events = null) =>
            new(input, true, "", new CharacterChanges(changes, Array.Empty<SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), input.State, default,
                events: events, pathMovement: path, returnMovement: returned);
    }
}
