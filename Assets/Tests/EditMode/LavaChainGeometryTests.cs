using System;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class LavaChainGeometryTests
    {
        [TestCase(1, 0, 0, 1)]
        [TestCase(1, 1, -1, 1)]
        [TestCase(0, 1, -1, 0)]
        [TestCase(-1, 1, -1, -1)]
        [TestCase(-1, 0, 0, -1)]
        [TestCase(-1, -1, 1, -1)]
        [TestCase(0, -1, 1, 0)]
        [TestCase(1, -1, 1, 1)]
        public void LeftIsPerpendicularForEveryHeading(int x, int y, int leftX, int leftY)
        {
            Assert.That(Invoke("LeftOf", new CellCoord(x, y)), Is.EqualTo(new CellCoord(leftX, leftY)));
        }

        [TestCase(1, 0, 4)]
        [TestCase(1, 1, 4)]
        [TestCase(0, 1, 4)]
        [TestCase(-1, 1, 4)]
        [TestCase(-1, 0, 4)]
        [TestCase(-1, -1, 4)]
        [TestCase(0, -1, 4)]
        [TestCase(1, -1, 4)]
        public void BoundaryDistanceCountsGridSteps(int x, int y, int distance)
        {
            Assert.That(Invoke("StepsToBoundary", 9, 9, new CellCoord(4, 4), new CellCoord(x, y)), Is.EqualTo(distance));
        }

        [Test]
        public void EqualBoundaryDistancesPreferLeft()
        {
            Assert.That(Invoke("LateralPriority", 9, 9, new CellCoord(4, 4), new CellCoord(1, 1)),
                Is.EqualTo(new[] { new CellCoord(-1, 1), new CellCoord(1, -1) }));
        }

        [Test]
        public void NearerBoundaryPrecedesLeft()
        {
            Assert.That(Invoke("LateralPriority", 9, 9, new CellCoord(4, 1), new CellCoord(1, 0)),
                Is.EqualTo(new[] { new CellCoord(0, -1), new CellCoord(0, 1) }));
        }

        private static object Invoke(string name, params object[] args)
        {
            var type = typeof(CombatCommandCalculator).Assembly.GetType(
                "ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks.LavaChainGeometry");
            Assert.That(type, Is.Not.Null, "용암 사슬 기하 규칙이 아직 없습니다.");
            return type.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).Invoke(null, args);
        }
    }
}
