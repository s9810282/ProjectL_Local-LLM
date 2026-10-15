using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class HighGroundMovementTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [TestCase(3, true)]
        [TestCase(2, false)]
        public void AscendingEdge_ChargesThreeApAndOneStep(int ap, bool accepted)
        {
            var input = TerrainInput(MovementTestData.SafeRoute(ap, 1, 1, CellCoord.Zero, CellCoord.Right),
                High(11, CellCoord.Right));
            var preview = CombatMovementCalculator.Preview(input, Rules);
            var move = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(preview.CanMove, Is.EqualTo(accepted));
            Assert.That(move.Accepted, Is.EqualTo(accepted));
            Assert.That(CombatMovementCalculator.Range(input).Destinations.ToArray()
                .Any(d => d.TargetAnchor == CellCoord.Right), Is.EqualTo(accepted));
            if (!accepted)
            {
                Assert.That(preview.FailureReason, Is.EqualTo(MovementFailureReason.ActionCostInsufficient));
                Assert.That(move.Reason, Is.EqualTo(MovementFailureReason.ActionCostInsufficient.ToString()));
                Assert.That(move.Characters.Characters, Is.Empty);
                return;
            }
            var actor = MovementTestData.Actor(move);
            Assert.That(actor.Stats.CurrentActionCost, Is.Zero);
            Assert.That(actor.PendingMoveActionCost, Is.EqualTo(3));
            Assert.That(actor.PendingMoveDistance, Is.EqualTo(1));
            Assert.That(actor.Stats.RemainingMoveRange, Is.Zero);
            Assert.That(preview.QuotedActionCost, Is.EqualTo(3));
            Assert.That(preview.QuotedDistance, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DescendingAndPlateau_NoSurcharge(bool plateau)
        {
            var input = TerrainInput(MovementTestData.SafeRoute(1, 1, 1, CellCoord.Zero, CellCoord.Right),
                High(11, CellCoord.Zero), High(12, CellCoord.Right, active: plateau));
            var preview = CombatMovementCalculator.Preview(input, Rules);
            var move = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(preview.QuotedActionCost, Is.EqualTo(1));
            Assert.That(MovementTestData.Actor(move).PendingMoveActionCost, Is.EqualTo(1));
        }

        [Test]
        public void MixedRoute_ReportsAndRefundsActualCost()
        {
            var input = TerrainInput(MovementTestData.SafeRoute(8, 6, 1,
                CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0), new CellCoord(3, 0)),
                High(11, CellCoord.Right), High(12, new CellCoord(2, 0)));
            var preview = CombatMovementCalculator.Preview(input, Rules);
            var move = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            var actor = MovementTestData.Actor(move);
            Assert.That(preview.QuotedActionCost, Is.EqualTo(5));
            Assert.That(move.PathMovement.ActionCostSpent, Is.EqualTo(5));
            Assert.That(actor.PendingMoveActionCost, Is.EqualTo(5));
            Assert.That(actor.AttackRefundEligibleMoveCost, Is.EqualTo(5));
            Assert.That(actor.PendingMoveDistance, Is.EqualTo(3));
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(3));
            var after = TerrainInput(MovementTestData.After(move));
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Return(after), Rules);
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            var restored = MovementTestData.Actor(returned);
            Assert.That(restored.Stats.CurrentActionCost, Is.EqualTo(8));
            Assert.That(restored.PendingMoveActionCost, Is.Zero);
            Assert.That(restored.PendingMoveDistance, Is.Zero);
            Assert.That(restored.AttackRefundEligibleMoveCost, Is.Zero);
        }

        [Test]
        public void LethalEntry_QuotesWholeRouteButPaysOnlyEnteredEdges()
        {
            var input = TerrainInput(MovementTestData.SafeRoute(5, 3, 1,
                CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0), new CellCoord(3, 0)),
                High(11, new CellCoord(2, 0)));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, s.Structures,
                new[] { new ThornFieldCellSnapshot(CellCoord.Right, 3, 100) }, s.Timeline, s.Turn, s.Environment));
            var rules = new IMovementTileRule[] { new ThornMovementTileRule() };
            var preview = CombatMovementCalculator.Preview(input, rules);
            var moved = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(preview.QuotedActionCost, Is.EqualTo(5));
            Assert.That(preview.QuotedDistance, Is.EqualTo(3));
            Assert.That(preview.PathMovement.ActionCostSpent, Is.EqualTo(1));
            Assert.That(moved.PathMovement.ActionCostSpent, Is.EqualTo(1));
            Assert.That(moved.PathMovement.EnteredCells, Is.EqualTo(new[] { CellCoord.Right }));
            Assert.That(moved.PathMovement.StopReason, Is.EqualTo(MovementStopReason.Dead));
        }

        [Test]
        public void ZeroBaseCost_StillChargesAscent()
        {
            var input = TerrainInput(MovementTestData.SafeRoute(2, 2, 0, CellCoord.Zero,
                CellCoord.Right, new CellCoord(2, 0)), High(11, CellCoord.Right));
            var move = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.PathMovement.ActionCostSpent, Is.EqualTo(2));
            Assert.That(MovementTestData.Actor(move).PendingMoveDistance, Is.EqualTo(2));
            Assert.That(CombatMovementCalculator.Preview(input, Rules).QuotedActionCost, Is.EqualTo(2));
        }

        [Test]
        public void Range_KeepsCheaperLongerAndCostlierShorterLabels()
        {
            var input = TerrainInput(MovementTestData.SafeRoute(2, 5, 0,
                new CellCoord(0, 1), new CellCoord(1, 1)), High(11, new CellCoord(1, 1)));
            var range = CombatMovementCalculator.Range(input);
            Assert.That(range.Succeeded, Is.True);
            var merge = range.Destinations.ToArray().Single(d => d.TargetAnchor == new CellCoord(2, 1));
            var beyond = range.Destinations.ToArray().Single(d => d.TargetAnchor == new CellCoord(4, 1));
            Assert.That(merge.ActionCostDelta, Is.Zero);
            Assert.That(merge.StepDistance, Is.EqualTo(4));
            Assert.That(beyond.ActionCostDelta, Is.EqualTo(2));
            Assert.That(beyond.StepDistance, Is.EqualTo(4));
            Assert.That(range.Destinations.ToArray().Select(d => d.TargetAnchor).Distinct().Count(),
                Is.EqualTo(range.Destinations.Length));
        }

        [Test]
        public void Preview_OriginTerrainChangeIsRelevant()
        {
            var route = MovementTestData.SafeRoute(6, 2, 1, CellCoord.Zero, CellCoord.Right);
            var before = TerrainInput(route, High(11, CellCoord.Zero), High(12, CellCoord.Right));
            var context = MovementContextCalculator.Capture(before, Owner.Player, Rules);
            var changed = TerrainInput(route, High(11, CellCoord.Zero, active: false), High(12, CellCoord.Right));
            Assert.That(context.VisibleEnvironment.Tiles.Select(t => t.Id), Does.Contain(11));
            Assert.That(MovementContextCalculator.Matches(context, changed, Owner.Player, Rules), Is.False);
            Assert.That(CombatMovementCalculator.Preview(before, Rules).QuotedActionCost, Is.EqualTo(1));
            Assert.That(CombatMovementCalculator.Preview(changed, Rules).QuotedActionCost, Is.EqualTo(3));
        }

        [Test]
        public void Preview_VisibleClassificationChangeIsRelevant()
        {
            var route = MovementTestData.SafeRoute(6, 2, 1, CellCoord.Zero, CellCoord.Right);
            var before = TerrainInput(route, High(11, CellCoord.Right));
            var context = MovementContextCalculator.Capture(before, Owner.Player, Rules);
            var unclassified = WithBindings(before, EnvironmentSemanticBindings.Empty);
            Assert.That(MovementContextCalculator.Matches(context, unclassified, Owner.Player, Rules), Is.False);
            Assert.That(CombatMovementCalculator.Preview(unclassified, Rules).QuotedActionCost, Is.EqualTo(1));
        }

        [Test]
        public void Preview_HiddenOnlyChangeDoesNotLeak()
        {
            var route = MovementTestData.SafeRoute(2, 2, 1, CellCoord.Zero, CellCoord.Right);
            var clear = TerrainInput(route);
            var hidden = TerrainInput(route, High(11, CellCoord.Right, owner: Owner.Enemy));
            var preview = CombatMovementCalculator.Preview(hidden, Rules);
            var context = MovementContextCalculator.Capture(clear, Owner.Player, Rules);
            Assert.That(preview.CanMove, Is.True);
            Assert.That(preview.QuotedActionCost, Is.EqualTo(1));
            Assert.That(preview.ProjectedActor.Stats.CurrentActionCost, Is.EqualTo(1));
            Assert.That(MovementContextCalculator.Matches(context, hidden, Owner.Player, Rules), Is.True);
            Assert.That(CombatMovementCalculator.Range(hidden).Destinations.ToArray()
                .Single(d => d.TargetAnchor == CellCoord.Right).ActionCostDelta, Is.EqualTo(1));
            Assert.That(CombatCommandCalculator.Calculate(hidden, Rules).Accepted, Is.False);
        }

        internal static StaticTerrainFact High(long id, CellCoord cell, bool active = true, Owner owner = Owner.Player) =>
            new(id, cell, StaticTerrainKind.HighGround, active, new[] { owner });

        internal static CombatCommandInput TerrainInput(CombatCommandInput input, params StaticTerrainFact[] terrain)
        {
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId,
                s.IsNight, s.Structures, s.ThornFields, s.Timeline, s.Turn,
                new MovementEnvironmentFacts(terrain.Cast<MovementTileFact>().ToArray()));
            return Copy(input, state, new EnvironmentSemanticBindings(terrain.Select(t => t.Id).ToArray(),
                Array.Empty<long>(), Array.Empty<EntityId>()));
        }

        private static CombatCommandInput WithBindings(CombatCommandInput input, EnvironmentSemanticBindings bindings) =>
            Copy(input, input.State, bindings);

        private static CombatCommandInput Copy(CombatCommandInput input, CombatCommandState state,
            EnvironmentSemanticBindings bindings) => new(input.Number, input.Kind, input.ActorId, input.Aim,
                state, input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls,
                input.Artifacts, input.TargetCell, input.Rotation, input.Battle, input.ProcessingKind,
                input.Path, input.MovementContext, 1, input.ArtifactItems, bindings);
    }
}
