using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class MovementRouteTests
    {
        [Test]
        public void Range_UsesInstalledAllowance()
        {
            var input = MovementTestData.WithMovement(MovementTestData.SafeRoute(                new CellCoord(1, 1),new CellCoord(2, 1)),5);
            var range = CombatMovementCalculator.Range(input);
            Assert.That(range.Succeeded, Is.True);
            Assert.That(range.Destinations.ToArray().Single(d => d.TargetAnchor == new CellCoord(6, 1)).StepDistance,
                Is.EqualTo(5));
            Assert.That(range.Destinations.ToArray().Any(d => d.TargetAnchor == new CellCoord(7, 1)), Is.False);
            Assert.That(range.Destinations.ToArray().All(d => d.StepDistance <= 5), Is.True);
            Assert.That(CombatMovementCalculator.Range(CombatCommandTestData.With(input,
                state: input.State.With())).Succeeded, Is.True);
        }

        [TestCase(5, 5L, MovementFailureReason.None)]
        [TestCase(5, 6L, MovementFailureReason.MoveRangeExceeded)]
        [TestCase(0, 1L, MovementFailureReason.VoluntaryMoveDisabled)]
        public void RouteBudget_UsesInstalledAllowance(int allowance, long distance, MovementFailureReason expected)
        {
            var input = MovementTestData.WithMovement(MovementTestData.SafeRoute(                new CellCoord(1, 1),new CellCoord(2, 1)),allowance);
            Assert.That(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
                .MovementRouteValidator.StepBudget(input.Characters.Single(c => c.EntityId == input.ActorId), distance),
                Is.EqualTo(expected));
        }

        [TestCase("origin", "PathOriginMismatch")]
        [TestCase("long", "InvalidPath")]
        [TestCase("repeated", "RepeatedCell")]
        [TestCase("empty", "SameAnchor")]
        [TestCase("missing", "InvalidPath")]
        [TestCase("outside", "OutOfBounds")]
        public void MalformedRoute_RejectsWholeDraft(string shape, string reason)
        {
            var cells = shape switch
            {
                "origin" => new[] { new CellCoord(2, 1), new CellCoord(3, 1) },
                "long" => new[] { new CellCoord(1, 1), new CellCoord(3, 1) },
                "repeated" => new[] { new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(1, 1) },
                "empty" => new[] { new CellCoord(1, 1) },
                _ => new[] { new CellCoord(1, 1), new CellCoord(1, 0), new CellCoord(1, -1) }
            };
            var source = MovementTestData.SafeRoute(new CellCoord(1, 1),new CellCoord(2, 1));
            var input = shape == "missing" ? source.WithIntent(CombatCommandKind.Move, default, new CellCoord(2, 1), null, null)
                : MovementTestData.Route(source, cells);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.PathMovement, Is.Null);
        }

        [Test]
        public void MovementRequiresNoSharedBudget()
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1),new CellCoord(2, 1));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.Events, Is.Not.Empty);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(new CellCoord(2, 1)));
        }

        [Test]
        public void Range_DoesNotReplaceDraft()
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1),new CellCoord(1, 2));
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 1), Owner.Enemy)).ToArray());
            var range = CombatMovementCalculator.Range(input);
            Assert.That(range.Succeeded, Is.True);
            Assert.That(range.Destinations.ToArray().Any(d => d.TargetAnchor == new CellCoord(3, 1)), Is.False);
            Assert.That(range.Destinations.ToArray().All(d => d.StepDistance <= 3), Is.True);
            Assert.That(input.Path.Cells, Is.EqualTo(new[] { new CellCoord(1, 1), new CellCoord(1, 2) }));
        }

        [Test]
        public void BlockedIntermediateCellRejectsAll()
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1),new CellCoord(2, 1),new CellCoord(3, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 1), Owner.Enemy)).ToArray());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(MovementFailureReason.DestinationOccupied.ToString()));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.FinalState, Is.SameAs(input.State));
        }
    }
}
