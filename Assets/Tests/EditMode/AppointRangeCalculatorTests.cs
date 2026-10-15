using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class AppointRangeCalculatorTests
    {
        [Test]
        public void SingleCellSquareRange_UsesChebyshevBoundary()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 100);
            var calculator = new AppointRangeCalculator();
            var source = new CellCoord(1, 1);
            var target = new CellCoord(4, 3);
            CollectionAssert.DoesNotContain(calculator.CalculateRangeCells(method, 2, source), target);
            CollectionAssert.Contains(calculator.CalculateRangeCells(method, 3, source), target);
        }
    }
}
