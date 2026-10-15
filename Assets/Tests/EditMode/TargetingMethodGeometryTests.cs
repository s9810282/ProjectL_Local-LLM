using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class TargetingMethodGeometryTests
    {
        private static readonly AttackGeometryCalculator Geometry = new AttackGeometryCalculator(new AppointRangeCalculator());

        internal static System.Collections.IEnumerable Slides()
        {
            foreach (var example in TargetingMethodFixtures.Examples)
                foreach (var origin in new[] { CellCoord.Zero, new CellCoord(13, -7) })
                    yield return new TestCaseData(example, origin).SetName("Geometry_" + example + "_" + origin.X);
        }

        [TestCaseSource(nameof(Slides))]
        public void LiteralSlide_HasExactSelectionAndSplash(object data, CellCoord origin)
        {
            var example = (TargetingMethodFixtures.Example)data;
            var result = Geometry.CalculateCells(example.Method, example.Range, origin, example.Aim(origin));
            Assert.That(result.DesignatedCells.ToArray(), Is.EqualTo(example.Cells("T", origin)));
            Assert.That(result.AdditionalCells.ToArray(), Is.EqualTo(example.Cells("A", origin)));
            Assert.That(result.PrimaryCell, Is.EqualTo(example.Method.ChoiceMode == TargetingChoiceMode.Single
                ? (CellCoord?)example.Cells("T", origin)[0] : null));
            Assert.That(AppointShapeCalculator.CreateBaseMask(example.Method.Shape, example.Range),
                Is.EqualTo(example.BaseCells(CellCoord.Zero)));
            Assert.That(new AppointRangeCalculator().CalculateRangeCells(example.Method, example.Range, origin),
                Is.EqualTo(example.BaseCells(origin)));
        }

        [TestCase(TargetingShape.CardinalLine, 4)]
        [TestCase(TargetingShape.DiagonalLine, 4)]
        [TestCase(TargetingShape.EightDirectionLine, 8)]
        [TestCase(TargetingShape.Fan, 4)]
        [TestCase(TargetingShape.RadialSquare, 8)]
        public void SameMethod_UsesSeparateOwnerRange(TargetingShape shape, int rangeOneCount)
        {
            var method = new TargetingMethodSpec(shape, TargetingChoiceMode.All, TargetingSplash.None, 0);
            Assert.That(Geometry.CalculateCells(method, 1, CellCoord.Zero, AttackAim.ForAll()).DesignatedCells.Length,
                Is.EqualTo(rangeOneCount));
            var rangeThree = Geometry.CalculateCells(method, 3, CellCoord.Zero, AttackAim.ForAll()).DesignatedCells.ToArray();
            Assert.That(rangeThree.Length, Is.GreaterThan(rangeOneCount));
            Assert.That(rangeThree, Has.Member(shape == TargetingShape.DiagonalLine ? new CellCoord(3, 3) : new CellCoord(0, 3)));
            Assert.That(method.Shape, Is.EqualTo(shape));
        }

        [TestCase(TargetingShape.CardinalLine, AttackDirection.Up)]
        [TestCase(TargetingShape.DiagonalLine, AttackDirection.UpLeft)]
        [TestCase(TargetingShape.EightDirectionLine, AttackDirection.Right)]
        [TestCase(TargetingShape.Fan, AttackDirection.Left)]
        [TestCase(TargetingShape.RadialSquare, AttackDirection.Left)]
        public void DirectionMask_AllQuarterTurnsMatchLiteralSubset(TargetingShape shape, AttackDirection direction)
        {
            var example = TargetingMethodFixtures.Examples.Single(e => e.Method.Shape == shape &&
                e.Method.ChoiceMode == TargetingChoiceMode.Direction);
            CellCoord[] expected = example.Cells("T", CellCoord.Zero);
            for (int turn = 0; turn < 4; turn++)
            {
                Assert.That(AppointShapeCalculator.CreateDirectionMask(shape, example.Range, direction), Is.EqualTo(expected));
                expected = TargetingMethodFixtures.Order(expected.Select(cell => new CellCoord(cell.Y, -cell.X)));
                direction = (AttackDirection)(((int)direction - 1 + 2) % 8 + 1);
            }
        }

        [Test]
        public void EightDirection_UpAndLeftHaveThreeRays()
        {
            Assert.That(AppointShapeCalculator.CreateDirectionMask(TargetingShape.EightDirectionLine, 2, AttackDirection.Up),
                Is.EqualTo(new[] { new CellCoord(-1,1), new CellCoord(0,1), new CellCoord(1,1),
                    new CellCoord(-2,2), new CellCoord(0,2), new CellCoord(2,2) }));
            Assert.That(AppointShapeCalculator.CreateDirectionMask(TargetingShape.EightDirectionLine, 1, AttackDirection.Left),
                Is.EqualTo(new[] { new CellCoord(-1,-1), new CellCoord(-1,0), new CellCoord(-1,1) }));
        }

        [Test]
        public void Splash_IsCenteredAtSelectedCellAndExtendsOutsideBase()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Cross, 0);
            var result = Geometry.CalculateCells(method, 2, CellCoord.Zero, AttackAim.ForCell(new CellCoord(-1, 2)));
            Assert.That(result.AdditionalCells.ToArray(), Is.EqualTo(new[] {
                new CellCoord(-1,1), new CellCoord(-2,2), new CellCoord(0,2), new CellCoord(-1,3) }));
            Assert.That(result.AdditionalCells.ToArray(), Has.No.Member(CellCoord.Zero));
            Assert.That(result.AdditionalCells.ToArray(), Has.No.Member(result.PrimaryCell.Value));
        }

        [Test]
        public void CellSelection_CopiesArrays()
        {
            var designated = new[] { CellCoord.Up };
            var additional = new[] { CellCoord.Left };
            var selection = new AttackCellSelection(CellCoord.Up, designated, additional);
            designated[0] = CellCoord.Down;
            additional[0] = CellCoord.Right;
            Assert.That(selection.DesignatedCells.ToArray(), Is.EqualTo(new[] { CellCoord.Up }));
            Assert.That(selection.AdditionalCells.ToArray(), Is.EqualTo(new[] { CellCoord.Left }));
            Assert.Throws<ArgumentNullException>(() => new AttackCellSelection(null, null, additional));
            Assert.Throws<ArgumentException>(() => new AttackCellSelection(CellCoord.Zero, new[] { CellCoord.Up }, additional));
            Assert.Throws<ArgumentException>(() => new AttackCellSelection(null, new[] { CellCoord.Up }, additional));
        }

        [TestCase(TargetingShape.CardinalLine)]
        [TestCase(TargetingShape.DiagonalLine)]
        [TestCase(TargetingShape.EightDirectionLine)]
        [TestCase(TargetingShape.Fan)]
        [TestCase(TargetingShape.RadialSquare)]
        public void Range_ZeroIsEmptyAndNegativeRejected(TargetingShape shape)
        {
            Assert.That(AppointShapeCalculator.CreateBaseMask(shape, 0), Is.Empty);
            var direction = shape == TargetingShape.DiagonalLine ? AttackDirection.UpLeft : AttackDirection.Up;
            Assert.That(AppointShapeCalculator.CreateDirectionMask(shape, 0, direction), Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => AppointShapeCalculator.CreateBaseMask(shape, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => AppointShapeCalculator.CreateDirectionMask(shape, -1, direction));
        }

        [TestCase(TargetingShape.CardinalLine)]
        [TestCase(TargetingShape.DiagonalLine)]
        [TestCase(TargetingShape.EightDirectionLine)]
        [TestCase(TargetingShape.Fan)]
        [TestCase(TargetingShape.RadialSquare)]
        public void UnrepresentableMask_RejectsBeforeAllocation(TargetingShape shape)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AppointShapeCalculator.CreateBaseMask(shape, int.MaxValue));
        }

        [TestCase(TargetingShape.EightDirectionLine)]
        [TestCase(TargetingShape.Fan)]
        [TestCase(TargetingShape.RadialSquare)]
        public void UnrepresentableDirectionMask_RejectsBeforeAllocation(TargetingShape shape)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AppointShapeCalculator.CreateDirectionMask(shape, int.MaxValue, AttackDirection.Up));
        }

        [Test]
        public void TranslationOverflow_RemainsFault()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0);
            Assert.Throws<OverflowException>(() => Geometry.CalculateCells(method, 1, new CellCoord(int.MaxValue, 0), AttackAim.ForAll()));
            var resolver = new AttackSelectionResolver();
            Assert.Throws<OverflowException>(() => resolver.TryResolve(method, 1, new CellCoord(int.MinValue, 0),
                AttackAim.ForAll(), out _, out _));
            method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 50);
            Assert.Throws<OverflowException>(() => Geometry.CalculateCells(method, 1, new CellCoord(int.MaxValue-1, 0),
                AttackAim.ForCell(new CellCoord(int.MaxValue, 0))));
        }

        [Test]
        public void InvalidDefinitions_AreNotUserAimFailures()
        {
            Assert.Throws<ArgumentNullException>(() => Geometry.CalculateCells(null, 1, CellCoord.Zero, AttackAim.ForAll()));
            Assert.Throws<ArgumentOutOfRangeException>(() => AppointShapeCalculator.CreateBaseMask((TargetingShape)99, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => AppointShapeCalculator.CreateSplashOffsets((TargetingSplash)99));
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.All, TargetingSplash.None, 0);
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackSelectionResolver().TryResolve(method, -1, CellCoord.Zero,
                AttackAim.ForAll(), out _, out _));
        }
    }
}
