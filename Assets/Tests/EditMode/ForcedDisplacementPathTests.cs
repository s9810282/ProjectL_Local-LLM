using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    /// <summary>강제 이동 공통 경로 진행(거리 N, 첫 막힌 칸 직전 정지, 마지막 칸 판정)을 확인한다(계획 T4.1, D6).</summary>
    public sealed class ForcedDisplacementPathTests
    {
        private static readonly CellCoord Right = new CellCoord(1, 0);

        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        public void OpenPath_EntersEveryCell_AndOnlyFinalIsLast(int distance)
        {
            var entered = new List<(CellCoord To, bool Last)>();

            var path = ForcedDisplacementPathCalculator.Execute(CellCoord.Zero, Right, distance, (_, _) => true,
                (_, to, last) => { entered.Add((to, last)); return true; });

            Assert.That(path.Stop, Is.EqualTo(ForcedDisplacementStop.Completed));
            Assert.That(path.RequestedDistance, Is.EqualTo(distance));
            Assert.That(path.Steps, Is.EqualTo(Enumerable.Range(1, distance).Select(x => new CellCoord(x, 0))));
            Assert.That(entered.Select(e => e.Last), Is.EqualTo(Enumerable.Range(1, distance).Select(x => x == distance)));
        }

        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        public void BlockedCell_StopsJustBefore_WithoutSkipping(int blockedX, int expectedSteps)
        {
            var entered = new List<(CellCoord To, bool Last)>();

            var path = ForcedDisplacementPathCalculator.Execute(CellCoord.Zero, Right, 3,
                (_, to) => to.X != blockedX && to.X != 5,
                (_, to, last) => { entered.Add((to, last)); return true; });

            Assert.That(path.Stop, Is.EqualTo(ForcedDisplacementStop.Blocked));
            Assert.That(path.Steps.Count, Is.EqualTo(expectedSteps));
            // 요청 거리를 소진하기 전에 막혔으므로 진입한 칸들은 즉시 효과를 처리하는 칸이다.
            Assert.That(entered.All(e => !e.Last), Is.True);
        }

        [Test]
        public void EnterReturningFalse_CancelsRemainingSteps()
        {
            var path = ForcedDisplacementPathCalculator.Execute(CellCoord.Zero, Right, 3, (_, _) => true,
                (_, to, _) => to.X < 2);

            Assert.That(path.Stop, Is.EqualTo(ForcedDisplacementStop.Interrupted));
            Assert.That(path.Steps, Is.EqualTo(new[] { new CellCoord(1, 0), new CellCoord(2, 0) }));
        }

        [Test]
        public void DiagonalStep_ChecksOnlyEnteredCells()
        {
            var checkedTargets = new List<CellCoord>();

            var path = ForcedDisplacementPathCalculator.Execute(CellCoord.Zero, new CellCoord(1, 1), 2,
                (_, to) => { checkedTargets.Add(to); return true; }, (_, _, _) => true);

            Assert.That(path.Steps, Is.EqualTo(new[] { new CellCoord(1, 1), new CellCoord(2, 2) }));
            Assert.That(checkedTargets.Distinct(), Is.EquivalentTo(new[] { new CellCoord(1, 1), new CellCoord(2, 2) }));
        }

        [Test]
        public void InvalidArguments_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => ForcedDisplacementPathCalculator.Execute(CellCoord.Zero, Right, 0,
                (_, _) => true, (_, _, _) => true));
            Assert.Throws<ArgumentOutOfRangeException>(() => ForcedDisplacementPathCalculator.Execute(CellCoord.Zero, new CellCoord(2, 0), 1,
                (_, _) => true, (_, _, _) => true));
        }
    }
}
