using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class TargetingAngleGeometryTests
    {
        private static readonly CellCoord[] Neighbors =
        {
            new CellCoord(-1, -1), new CellCoord(0, -1), new CellCoord(1, -1),
            new CellCoord(-1, 0), new CellCoord(1, 0),
            new CellCoord(-1, 1), new CellCoord(0, 1), new CellCoord(1, 1),
        };

        [TestCase(Owner.Player, TargetingChoiceAngle.Unrestricted, "11111111")]
        [TestCase(Owner.Player, TargetingChoiceAngle.Front, "00000111")]
        [TestCase(Owner.Player, TargetingChoiceAngle.FrontAndSides, "00011111")]
        [TestCase(Owner.Player, TargetingChoiceAngle.FrontAndRear, "11100111")]
        [TestCase(Owner.Enemy, TargetingChoiceAngle.Unrestricted, "11111111")]
        [TestCase(Owner.Enemy, TargetingChoiceAngle.Front, "11100000")]
        [TestCase(Owner.Enemy, TargetingChoiceAngle.FrontAndSides, "11111000")]
        [TestCase(Owner.Enemy, TargetingChoiceAngle.FrontAndRear, "11100111")]
        public void EightNeighborMatrix_MatchesAllAnglesForBothOwners(
            Owner owner, TargetingChoiceAngle angle, string expected)
        {
            for (int i = 0; i < Neighbors.Length; i++)
                Assert.That(TargetingAngleCalculator.Contains(angle, owner, CellCoord.Zero, Neighbors[i]),
                    Is.EqualTo(expected[i] == '1'), Neighbors[i].ToString());
        }

        [TestCase(Owner.Player, 1)]
        [TestCase(Owner.Enemy, -1)]
        public void FilledMask_UsesExactSectorNotNearestDirection(Owner owner, int sign)
        {
            foreach (CellCoord origin in new[] { CellCoord.Zero, new CellCoord(11, -7) })
            {
                Assert.That(TargetingAngleCalculator.Contains(TargetingChoiceAngle.Front, owner,
                    origin, origin + new CellCoord(2, 3 * sign)), Is.True);
                Assert.That(TargetingAngleCalculator.Contains(TargetingChoiceAngle.Front, owner,
                    origin, origin + new CellCoord(3, 2 * sign)), Is.False);
                Assert.That(TargetingAngleCalculator.Contains(TargetingChoiceAngle.Front, owner,
                    origin, origin + new CellCoord(-3, 3 * sign)), Is.True);
            }
        }

        [TestCase(TargetingChoiceAngle.Unrestricted)]
        [TestCase(TargetingChoiceAngle.Front)]
        [TestCase(TargetingChoiceAngle.FrontAndSides)]
        [TestCase(TargetingChoiceAngle.FrontAndRear)]
        public void OriginAndInvalidInputs_AreRejected(TargetingChoiceAngle angle)
        {
            Assert.That(TargetingAngleCalculator.Contains(angle, Owner.Player, CellCoord.Zero, CellCoord.Zero), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TargetingAngleCalculator.Contains(angle, Owner.Neutral, CellCoord.Zero, CellCoord.Up));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TargetingAngleCalculator.Contains(angle, (Owner)99, CellCoord.Zero, CellCoord.Up));
        }

        [TestCase(-1)]
        [TestCase(4)]
        [TestCase(99)]
        public void Predicate_RejectsUndefinedAngle(int angle)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TargetingAngleCalculator.Contains((TargetingChoiceAngle)angle, Owner.Player, CellCoord.Zero, CellCoord.Up));
        }

        [Test]
        public void Sector_WidensCoordinateSubtraction()
        {
            var low = new CellCoord(int.MinValue, int.MinValue);
            var high = new CellCoord(int.MaxValue, int.MaxValue);
            Assert.That(TargetingAngleCalculator.Contains(TargetingChoiceAngle.Front, Owner.Player, low, high), Is.True);
            Assert.That(TargetingAngleCalculator.Contains(TargetingChoiceAngle.Front, Owner.Enemy, high, low), Is.True);
            Assert.That(TargetingAngleCalculator.Contains(TargetingChoiceAngle.Front, Owner.Player, high, low), Is.False);
        }

        [TestCase(TargetingShape.CardinalLine)]
        [TestCase(TargetingShape.DiagonalLine)]
        [TestCase(TargetingShape.EightDirectionLine)]
        [TestCase(TargetingShape.Fan)]
        [TestCase(TargetingShape.RadialSquare)]
        public void Range_IsSubsetAndPreservesShapeBoundaries(TargetingShape shape)
        {
            var calculator = new AppointRangeCalculator();
            var origin = new CellCoord(3, 4);
            var unrestricted = Method(shape, TargetingChoiceAngle.Unrestricted);
            var original = calculator.CalculateRangeCells(unrestricted, 2, origin);
            foreach (Owner owner in new[] { Owner.Player, Owner.Enemy })
            {
                Assert.That(calculator.CalculateRangeCells(unrestricted, 2, origin, owner), Is.EqualTo(original));
                foreach (TargetingChoiceAngle angle in Enum.GetValues(typeof(TargetingChoiceAngle)))
                {
                    var actual = calculator.CalculateRangeCells(Method(shape, angle), 2, origin, owner);
                    Assert.That(actual, Is.SubsetOf(original));
                    Assert.That(actual, Is.Ordered.Using<CellCoord>((a, b) =>
                        a.Y == b.Y ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y)));
                    Assert.That(calculator.CalculateRangeCells(Method(shape, angle), 0, origin, owner), Is.Empty);
                }
            }
        }

        [TestCase(TargetingShape.CardinalLine)]
        [TestCase(TargetingShape.Fan)]
        public void Front_DoesNotAddMissingDiagonals(TargetingShape shape)
        {
            Assert.That(new AppointRangeCalculator().CalculateRangeCells(
                Method(shape, TargetingChoiceAngle.Front), 1, CellCoord.Zero, Owner.Player),
                Is.EqualTo(new[] { CellCoord.Up }));
        }

        [Test]
        public void Front_EightDirectionRangeKeepsExactBoundary()
        {
            Assert.That(new AppointRangeCalculator().CalculateRangeCells(
                Method(TargetingShape.EightDirectionLine, TargetingChoiceAngle.Front), 1, CellCoord.Zero, Owner.Player),
                Is.EqualTo(new[] { new CellCoord(-1, 1), CellCoord.Up, new CellCoord(1, 1) }));
        }

        [TestCase(TargetingChoiceAngle.Front)]
        [TestCase(TargetingChoiceAngle.FrontAndSides)]
        [TestCase(TargetingChoiceAngle.FrontAndRear)]
        public void OwnerlessRange_RejectsRestrictedMethod(TargetingChoiceAngle angle)
        {
            Assert.Throws<ArgumentException>(() => new AppointRangeCalculator().CalculateRangeCells(
                Method(TargetingShape.RadialSquare, angle), 1, CellCoord.Zero));
        }

        [Test]
        public void Range_PreservesArgumentAndOverflowChecks()
        {
            var calculator = new AppointRangeCalculator();
            var method = Method(TargetingShape.RadialSquare, TargetingChoiceAngle.Front);
            Assert.Throws<ArgumentNullException>(() => calculator.CalculateRangeCells(null, 1, CellCoord.Zero, Owner.Player));
            Assert.Throws<ArgumentOutOfRangeException>(() => calculator.CalculateRangeCells(method, -1, CellCoord.Zero, Owner.Player));
            Assert.Throws<ArgumentOutOfRangeException>(() => calculator.CalculateRangeCells(method, 0, CellCoord.Zero, Owner.Neutral));
            Assert.Throws<OverflowException>(() =>
                calculator.CalculateRangeCells(method, 1, new CellCoord(0, int.MaxValue), Owner.Player));
        }

        private static TargetingMethodSpec Method(TargetingShape shape, TargetingChoiceAngle angle)
        {
            return new TargetingMethodSpec(shape, TargetingChoiceMode.All, TargetingSplash.None, 0, angle);
        }
    }
}
