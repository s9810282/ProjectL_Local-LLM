using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class TornadoGeometryTests
    {
        [TestCase(1, 1, 5, 5)]
        [TestCase(1, -1, 3, 1)]
        [TestCase(-1, 1, 1, 3)]
        [TestCase(-1, -1, 1, 1)]
        public void TravelKeepsOriginEveryDiagonalStepAndShortensAtFullSquareEdge(int dx, int dy, int x, int y)
        {
            var path = TornadoGeometry.Travel(new CellCoord(2, 2), 3, 3, 8, 8, new CellCoord(dx, dy));
            Assert.That(path[0], Is.EqualTo(new CellCoord(2, 2)));
            Assert.That(path[1], Is.EqualTo(new CellCoord(2 + dx, 2 + dy)));
            Assert.That(path.Last(), Is.EqualTo(new CellCoord(x, y)));
            if (dx == 1 && dy == 1)
                Assert.That(path, Is.EqualTo(new[] { new CellCoord(2, 2), new CellCoord(3, 3), new CellCoord(4, 4), new CellCoord(5, 5) }));
        }

        [Test]
        public void FullSquareIsRowMajorAndCannotClipBoardOrOverflow()
        {
            Assert.That(TornadoGeometry.Footprint(new CellCoord(2, 2), 3), Is.EqualTo(new[] {
                new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1),
                new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(3, 2),
                new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3) }));
            Assert.That(TornadoGeometry.Fits(CellCoord.Zero, 3, 8, 8), Is.False);
            Assert.That(TornadoGeometry.Fits(new CellCoord(6, 6), 3, 8, 8), Is.True);
            Assert.That(TornadoGeometry.Fits(new CellCoord(int.MaxValue, 1), 3, 8, 8), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => TornadoGeometry.Travel(new CellCoord(2, 2), 3, 3, 8, 8, CellCoord.Right));
        }

        [Test]
        public void WarningLocksFeasibleVerticalSignAndTravelReflectsOnFollowingActivation()
        {
            var spec = new TornadoMemberSpec(31, new[] { Owner.Player }, new CellCoord(2, 2), 1);
            uint random = 1;
            var warning = TornadoGimmickPreparationCalculator.Warn(spec,
                new TornadoMemberState(31, new CellCoord(5, 1), 1, TornadoGimmickPhase.Cooldown, 1, null), 8, 8, ref random);
            Assert.That(warning.LockedDirection, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(random, Is.EqualTo(1u));
            var travel = TornadoGimmickPreparationCalculator.CompleteTravel(spec, warning, 8, 8, ref random);
            Assert.That(travel.Path, Is.EqualTo(new[] { new CellCoord(5, 1), new CellCoord(6, 2) }));
            Assert.That(travel.State.HorizontalSign, Is.EqualTo(-1));
            Assert.That(travel.State.NextBoundaryCount, Is.EqualTo(3));
            var next = TornadoGimmickPreparationCalculator.Warn(spec, travel.State, 8, 8, ref random);
            Assert.That(next.LockedDirection.Value.X, Is.EqualTo(-1));
            Assert.That(next.NextBoundaryCount, Is.EqualTo(4));
        }

        [Test]
        public void EqualLeadRepeatImmediatelyLocksNextWarningAndZeroTravelKeepsOneCenter()
        {
            var spec = new TornadoMemberSpec(31, new[] { Owner.Player }, new CellCoord(1, 1), 1,
                firstActivationAfterControllerTurns: 1, repeatIntervalControllerTurns: 1);
            uint random = 1;
            var group = TornadoGimmickPreparationCalculator.Prepare(new TornadoGimmickSpec(new[] { spec }), new StageGimmickId(1), 3, 5, ref random);
            var result = TornadoGimmickPreparationCalculator.CompleteTravel(spec, group.Members[0], 3, 5, ref random);
            Assert.That(result.Path, Is.EqualTo(new[] { new CellCoord(1, 1) }));
            Assert.That(result.State.Phase, Is.EqualTo(TornadoGimmickPhase.Warning));
            Assert.That(result.State.NextBoundaryCount, Is.EqualTo(2));
            Assert.That(result.State.LockedDirection, Is.EqualTo(CellCoord.Zero));
            Assert.That(random, Is.EqualTo(1u));
        }
    }
}
