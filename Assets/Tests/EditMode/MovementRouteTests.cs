using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class MovementRouteTests
    {
        [TestCase("origin", "PathOriginMismatch", 0)]
        [TestCase("diagonal", "InvalidPath", 0)]
        [TestCase("repeated", "RepeatedCell", 1)]
        [TestCase("empty", "SameAnchor", 0)]
        [TestCase("missing", "InvalidPath", 0)]
        [TestCase("outside", "OutOfBounds", 1)]
        public void MalformedRoute_RejectsWholeDraft_WithValidPrefix(string shape, string reason, int valid)
        {
            var cells = shape switch
            {
                "origin" => new[] { new CellCoord(2, 1), new CellCoord(3, 1) },
                "diagonal" => new[] { new CellCoord(1, 1), new CellCoord(2, 2) },
                "repeated" => new[] { new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(1, 1) },
                "empty" => new[] { new CellCoord(1, 1) },
                _ => new[] { new CellCoord(1, 1), new CellCoord(1, 0), new CellCoord(1, -1) }
            };
            var input = MovementTestData.Route(Detour(), cells);
            if (shape == "missing") input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim,
                input.State, input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts,
                input.Souls, input.Artifacts, targetCell: new CellCoord(2, 1));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var preview = CombatMovementCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(preview.ValidStepCount, Is.EqualTo(valid));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.PathMovement, Is.Null);
        }

        [TestCase(2, 20, 1, "ActionCostInsufficient", 2)]
        [TestCase(20, 2, 1, "MoveRangeExceeded", 2)]
        [TestCase(int.MaxValue, 20, int.MaxValue, "ActionCostInsufficient", 1)]
        public void FullRouteBudget_RejectsWithoutOverflowOrPartialMovement(int ap, int range, int cost, string reason, int valid)
        {
            var input = MovementTestData.SafeRoute(ap, range, cost, Detour().Path.Cells.ToArray());
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(CombatMovementCalculator.Preview(input, System.Array.Empty<IMovementTileRule>()).ValidStepCount, Is.EqualTo(valid));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [Test]
        public void AttackRefundLedgerOverflow_RejectsBeforeEntering()
        {
            var input = Detour();
            var c = input.Characters[0];
            var actor = new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                c.Area, c.Health, c.Stats, c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells,
                c.MovementCheckpointAnchor, c.CommittedMoveDistance, c.PendingMoveActionCost,
                c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus, c.PendingMoveDistance, int.MaxValue, c.PositionModifier);
            input = CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1] });
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("StateInconsistent"));
            Assert.That(result.Events, Is.Empty);
        }

        [Test]
        public void Range_QuotesReachableMinimum_WithoutReplacingTheDraft()
        {
            var input = Detour();
            var blocker = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 1), Owner.Enemy);
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(blocker).ToArray());
            var range = CombatMovementCalculator.Range(input);
            Assert.That(range.Succeeded, Is.True);
            var hint = range.Destinations.ToArray().Single(d => d.TargetAnchor == new CellCoord(3, 1));
            Assert.That(hint.StepDistance, Is.EqualTo(4));
            Assert.That(hint.ActionCostDelta, Is.EqualTo(4));
            Assert.That(hint.DistanceFromCheckpoint, Is.EqualTo(2));
            Assert.That(input.Path.StepCount, Is.EqualTo(4));
            Assert.That(CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()).PathMovement.DistanceSpent, Is.EqualTo(4));
        }

        [Test]
        public void EnemyRoute_SealsActualStepsImmediately()
        {
            var input = Detour();
            var c = input.Characters[0];
            var enemy = new CharacterCombatFacts(c.EntityId, null, c.CharacterKey, Owner.Enemy, null,
                c.Area, c.Health, c.Stats, null, c.Anchor, c.Rotation, c.OccupiedCells, c.MovementCheckpointAnchor, 0, 0);
            var state = new CombatCommandState(input.State.BoardWidth, input.State.BoardHeight, CombatFlowPhase.Automatic,
                enemy.EntityId, false, input.State.Structures, input.State.ThornFields, input.State.Timeline,
                input.State.Turn, input.State.Environment);
            input = new CombatCommandInput(0, CombatCommandKind.Move, enemy.EntityId, default, state,
                new[] { enemy, input.Characters[1] }, input.Profiles, input.Main, input.Owned, input.SoulCounts,
                input.Souls, input.Artifacts, processingKind: CombatProcessingKind.Advance, path: input.Path);
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var actor = MovementTestData.Actor(result);
            Assert.That(actor.CommittedMoveDistance, Is.EqualTo(4));
            Assert.That(actor.PendingMoveDistance, Is.Zero);
            Assert.That(actor.PendingMoveActionCost, Is.Zero);
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(16));
            Assert.That(actor.MovementCheckpointAnchor, Is.EqualTo(input.Path.Endpoint));
            Assert.That(CombatCommandCalculator.Calculate(MovementTestData.Return(MovementTestData.After(result)), System.Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }

        [Test]
        public void MovementTraits_RunOnce_AndReturnDoesNotRewindThem()
        {
            var input = Detour();
            var actor = CombatCommandTestData.With(input.Characters[0], traits: new TraitStatusSnapshot[]
            {
                new FocusTraitStatusSnapshot(2, true, false), new MoveAttackDiscountTraitStatusSnapshot(false, false)
            });
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, actor.Stats.AttackPower,
                CategoryKey.Base, new FocusTraitSpec(5, 3), new MoveAttackDiscountTraitSpec(1, 1)), profiles[0].Method);
            input = CombatCommandTestData.With(input, characters: new[] { actor, input.Characters[1] }, profiles: profiles);
            var moved = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.Events.OfType<CombatEffectsEvent>().Count(), Is.EqualTo(1));
            var afterMove = MovementTestData.Actor(moved);
            var focus = afterMove.Stats.TraitStatuses.ToArray().OfType<FocusTraitStatusSnapshot>().Single();
            var discount = afterMove.Stats.TraitStatuses.ToArray().OfType<MoveAttackDiscountTraitStatusSnapshot>().Single();
            Assert.That(focus.MovedThisTurn, Is.True);
            Assert.That(focus.ActiveStacks, Is.Zero);
            Assert.That(discount.FirstMoveConsumed, Is.True);
            Assert.That(discount.DiscountArmed, Is.True);
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(MovementTestData.After(moved)), System.Array.Empty<IMovementTileRule>());
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            Assert.That(MovementTestData.Actor(returned).Stats.TraitStatuses.ToArray(), Is.EqualTo(afterMove.Stats.TraitStatuses.ToArray()));
            Assert.That(returned.Events.OfType<CombatEffectsEvent>(), Is.Empty);
            Assert.That(input.Characters[0].Stats.TraitStatuses.ToArray().OfType<FocusTraitStatusSnapshot>().Single().ActiveStacks, Is.EqualTo(2));
        }

        internal static CombatCommandInput Detour(int moveCost = 1) => MovementTestData.SafeRoute(20, 20, moveCost,
            new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(3, 2), new CellCoord(3, 1));

        [Test]
        public void Detour_ChargesFourSteps()
        {
            var input = Detour();
            var preview = CombatMovementCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var actor = MovementTestData.Actor(result);
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(16));
            Assert.That(actor.PendingMoveDistance, Is.EqualTo(4));
            Assert.That(actor.PendingMoveActionCost, Is.EqualTo(4));
            Assert.That(actor.Stats.RemainingMoveRange, Is.EqualTo(16));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(4));
            Assert.That(result.PathMovement.ActionCostSpent, Is.EqualTo(4));
            Assert.That(result.PathMovement.EnteredCells, Is.EqualTo(input.Path.Cells.Skip(1)));
            Assert.That(result.Movement.HasValue, Is.False);
            Assert.That(preview.PathMovement.DistanceSpent, Is.EqualTo(4));
            Assert.That(preview.PathMovement.ActionCostSpent, Is.EqualTo(4));
            Assert.That(preview.ValidStepCount, Is.EqualTo(4));
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(20));
            var events = result.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved).ToArray();
            Assert.That(events.Select(e => e.Final.Anchor.Value), Is.EqualTo(input.Path.Cells.Skip(1)));
            Assert.That(events[0].Expected.Anchor, Is.EqualTo(input.Path.Origin));
            for (int i = 1; i < events.Length; i++)
                Assert.That(events[i].Expected, Is.SameAs(events[i - 1].Final));
        }

        [Test]
        public void BlockedIntermediateCell_RejectsAll()
        {
            var input = Detour();
            var blocker = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy);
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(blocker).ToArray());
            var preview = CombatMovementCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("DestinationOccupied"));
            Assert.That(preview.CanMove, Is.False);
            Assert.That(preview.ValidStepCount, Is.EqualTo(1));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(result.FinalState, Is.SameAs(input.State));
        }
    }
}
