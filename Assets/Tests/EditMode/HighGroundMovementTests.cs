using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class HighGroundMovementTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void AscendingEdge_UsesOneMovementStep()
        {
            var input = TerrainInput(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right), High(11, CellCoord.Right));
            var preview = CombatMovementCalculator.Preview(input, Rules);
            var move = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(preview.CanMove, Is.True);
            Assert.That(move.Accepted, Is.True);
            Assert.That(CombatMovementCalculator.Range(input).Succeeded, Is.True);
            Assert.That(move.PathMovement.DistanceSpent, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DescendingAndPlateau_AreTraversable(bool plateau)
        {
            var input = TerrainInput(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right),
                High(11, CellCoord.Zero), High(12, CellCoord.Right, active: plateau));
            var move = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
        }

        [Test]
        public void MixedRoute_ConsumesMovement()
        {
            var input = TerrainInput(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,                new CellCoord(2, 0),new CellCoord(3, 0)), High(11, CellCoord.Right), High(12, new CellCoord(2, 0)));
            var move = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.PathMovement.DistanceSpent, Is.EqualTo(3));
            Assert.That(MovementTestData.Actor(move).ActionUse, Is.EqualTo(UnitActionUse.Moved));
        }

        [Test]
        public void LethalEntry_StopsAtActualCell()
        {
            var input = TerrainInput(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right,new CellCoord(2, 0)),
                High(11, new CellCoord(2, 0)));
            input = CombatCommandTestData.With(input, state: input.State.With(thornFields:
                new[] { new ThornFieldCellSnapshot(CellCoord.Right, 3, 100) }));
            var moved = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new ThornMovementTileRule() });
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(moved.PathMovement.EnteredCells, Is.EqualTo(new[] { CellCoord.Right }));
            Assert.That(moved.PathMovement.StopReason, Is.EqualTo(MovementStopReason.Dead));
        }

        [Test]
        public void Range_UsesActualShortestSteps()
        {
            var input = TerrainInput(MovementTestData.SafeRoute(new CellCoord(0, 1),new CellCoord(1, 1)),
                High(11, new CellCoord(1, 1)));
            var range = CombatMovementCalculator.Range(input);
            Assert.That(range.Succeeded, Is.True);
            var destination = range.Destinations.ToArray().Single(d => d.TargetAnchor == new CellCoord(3, 1));
            Assert.That(destination.StepDistance, Is.EqualTo(3));
            Assert.That(range.Destinations.ToArray().Any(d => d.TargetAnchor == new CellCoord(4, 1)), Is.False);
        }

        [Test]
        public void Preview_VisibleTerrainOrClassificationChangeIsRelevant()
        {
            var route = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right);
            var before = TerrainInput(route, High(11, CellCoord.Zero), High(12, CellCoord.Right));
            var context = MovementContextCalculator.Capture(before, Owner.Player, Rules);
            var changed = TerrainInput(route, High(11, CellCoord.Zero, active: false), High(12, CellCoord.Right));
            Assert.That(MovementContextCalculator.Matches(context, changed, Owner.Player, Rules), Is.False);
            var unclassified = before.WithWorld(before.State, before.Characters, before.Main, before.SoulCounts,
                before.Souls, EnvironmentSemanticBindings.Empty);
            Assert.That(MovementContextCalculator.Matches(context, unclassified, Owner.Player, Rules), Is.False);
        }

        [Test]
        public void Preview_HiddenOnlyChangeDoesNotLeak()
        {
            var route = MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right);
            var clear = TerrainInput(route);
            var hidden = TerrainInput(route, High(11, CellCoord.Right, owner: Owner.Enemy));
            var context = MovementContextCalculator.Capture(clear, Owner.Player, Rules);
            Assert.That(MovementContextCalculator.Matches(context, hidden, Owner.Player, Rules), Is.True);
            Assert.That(CombatCommandCalculator.Calculate(hidden, Rules).Accepted, Is.True);
        }

        internal static StaticTerrainFact High(long id, CellCoord cell, bool active = true, Owner owner = Owner.Player) =>
            new(id, cell, StaticTerrainKind.HighGround, active, new[] { owner });

        internal static CombatCommandInput TerrainInput(CombatCommandInput input, params StaticTerrainFact[] terrain)
        {
            var environment = new MovementEnvironmentFacts(terrain.Cast<MovementTileFact>().ToArray());
            var state = input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment));
            return input.WithWorld(state, input.Characters, input.Main, input.SoulCounts, input.Souls,
                new EnvironmentSemanticBindings(terrain.Select(t => t.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>()));
        }
    }
}
