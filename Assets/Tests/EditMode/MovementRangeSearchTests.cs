using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    /// <summary>클릭 최단 경로 이동의 Core 계약(설계 C1~C3, 계획 K1·K3~K5)과 행동 범위·호버의 목적지 규칙 공유(리뷰 R6)를 고정한다.</summary>
    public sealed class MovementRangeSearchTests
    {
        private static readonly CellCoord Origin = new(1, 1);

        [Test]
        public void TiedShortestRoutes_FollowRightUpLeftDownDiscovery_AndRepeatExactly()
        {
            var input = Open();

            var first = Route(input, new CellCoord(3, 2));
            var again = Route(input, new CellCoord(3, 2));

            Assert.That(first.Cells, Is.EqualTo(new[] { Origin, new CellCoord(2, 1), new CellCoord(3, 1), new CellCoord(3, 2) }));
            Assert.That(again.Cells, Is.EqualTo(first.Cells));
        }

        [Test]
        public void DiagonalTarget_CountsOrthogonalSteps()
        {
            Assert.That(Route(Open(), new CellCoord(3, 3)).StepCount, Is.EqualTo(4));
        }

        [Test]
        public void WallDetour_UsesTheActualDetourLength()
        {
            var input = LavaChainGimmickTests.Walls(Open(), new CellCoord(2, 1));

            var route = Route(input, new CellCoord(3, 1));

            Assert.That(route.Cells, Is.EqualTo(new[]
                { Origin, new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(3, 2), new CellCoord(3, 1) }));
            Assert.That(CombatMovementCalculator.Range(input).TryGetRoute(new CellCoord(2, 1), out var blocked), Is.False);
            Assert.That(blocked, Is.Null);
        }

        [Test]
        public void AllowanceBoundary_ExcludesFartherCellsWithoutPartialRoutes()
        {
            var range = CombatMovementCalculator.Range(Open(allowance: 2));

            Assert.That(range.TryGetRoute(new CellCoord(3, 1), out var edge), Is.True);
            Assert.That(edge.StepCount, Is.EqualTo(2));
            Assert.That(range.TryGetRoute(new CellCoord(4, 1), out var beyond), Is.False);
            Assert.That(beyond, Is.Null);
            Assert.That(range.Destinations.ToArray().All(d => d.StepDistance <= 2), Is.True);
        }

        [Test]
        public void EveryDestination_HasOneOrthogonalRouteFromTheOrigin()
        {
            var range = CombatMovementCalculator.Range(Open());
            var destinations = range.Destinations.ToArray();

            Assert.That(destinations, Is.Not.Empty);
            Assert.That(destinations.Select(d => d.TargetAnchor), Is.EqualTo(destinations.Select(d => d.TargetAnchor)
                .OrderBy(c => c.Y).ThenBy(c => c.X)), "목적지 출력은 Y → X 순서다.");
            foreach (var destination in destinations)
            {
                Assert.That(range.TryGetRoute(destination.TargetAnchor, out var route), Is.True);
                Assert.That(route.Origin, Is.EqualTo(Origin));
                Assert.That(route.Endpoint, Is.EqualTo(destination.TargetAnchor));
                Assert.That(route.StepCount, Is.EqualTo(destination.StepDistance));
                Assert.That(route.Cells.Distinct().Count(), Is.EqualTo(route.Cells.Count));
                for (int i = 1; i < route.Cells.Count; i++)
                    Assert.That(Math.Abs(route.Cells[i].X - route.Cells[i - 1].X) + Math.Abs(route.Cells[i].Y - route.Cells[i - 1].Y),
                        Is.EqualTo(1));
            }
        }

        [Test]
        public void UnitMoveCells_UsesTheSameDestinationRuleAsActionRange()
        {
            var input = Open();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);

            var hover = CombatMovementCalculator.UnitMoveCells(input, actor, input.State.Environment.VisibleTo(actor.Owner));

            Assert.That(hover, Is.EqualTo(CombatMovementCalculator.Range(input).Destinations.ToArray().Select(d => d.TargetAnchor)));
        }

        [Test]
        public void SuccessCopiesRoutesAndRejectsMalformedRouteSets()
        {
            var unit = new EntityId(1);
            var routes = new List<MovementPath> { Path(Origin, new CellCoord(2, 1)) };

            var result = MovementRangeResult.Success(unit, routes);
            routes.Add(Path(Origin, new CellCoord(1, 2)));

            Assert.That(result.Destinations.Length, Is.EqualTo(1));
            Assert.That(result.TryGetRoute(new CellCoord(1, 2), out _), Is.False);
            Assert.Throws<ArgumentNullException>(() => MovementRangeResult.Success(unit, null));
            Assert.Throws<ArgumentException>(() => MovementRangeResult.Success(EntityId.Empty, routes));
            Assert.Throws<ArgumentException>(() => MovementRangeResult.Success(unit, new[] { Path(Origin) }));
            Assert.Throws<ArgumentException>(() => MovementRangeResult.Success(unit,
                new[] { Path(Origin, new CellCoord(2, 1)), Path(new CellCoord(3, 3), new CellCoord(3, 4)) }));
            Assert.Throws<ArgumentException>(() => MovementRangeResult.Success(unit,
                new[] { Path(Origin, new CellCoord(2, 1)), Path(Origin, new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(2, 1)) }));
        }

        [Test]
        public void SourceRevision_IsStampedOnlyAsAPositiveCopyAndNeverOnDefault()
        {
            var success = MovementRangeResult.Success(new EntityId(1), new[] { Path(Origin, new CellCoord(2, 1)) });
            var failure = MovementRangeResult.Failure(MovementFailureReason.OutOfBounds, new EntityId(1));

            var stamped = success.WithSourceRevision(7);

            Assert.That(success.SourceRevision, Is.Zero);
            Assert.That(stamped.SourceRevision, Is.EqualTo(7));
            Assert.That(stamped.TryGetRoute(new CellCoord(2, 1), out var route), Is.True);
            Assert.That(route.Cells, Is.EqualTo(new[] { Origin, new CellCoord(2, 1) }));
            Assert.That(failure.WithSourceRevision(3).Succeeded, Is.False);
            Assert.That(failure.WithSourceRevision(3).SourceRevision, Is.EqualTo(3));
            Assert.That(failure.TryGetRoute(new CellCoord(2, 1), out _), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => success.WithSourceRevision(0));
            Assert.That(default(MovementRangeResult).TryGetRoute(Origin, out var none), Is.False);
            Assert.That(none, Is.Null);
            Assert.Throws<InvalidOperationException>(() => default(MovementRangeResult).WithSourceRevision(1));
        }

        private static CombatCommandInput Open(int allowance = 5) =>
            MovementTestData.WithMovement(MovementTestData.SafeRoute(Origin, new CellCoord(2, 1)), allowance);

        private static MovementPath Route(CombatCommandInput input, CellCoord target)
        {
            var range = CombatMovementCalculator.Range(input);
            Assert.That(range.Succeeded, Is.True, range.FailureReason.ToString());
            Assert.That(range.TryGetRoute(target, out var route), Is.True, target.ToString());
            return route;
        }

        private static MovementPath Path(params CellCoord[] cells) => new(cells);
    }
}
