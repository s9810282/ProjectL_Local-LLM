using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks
{
    internal static class LavaChainGeometry
    {
        internal static readonly IReadOnlyList<CellCoord> Directions = Array.AsReadOnly(new[]
        {
            new CellCoord(1, 0), new CellCoord(1, 1), new CellCoord(0, 1), new CellCoord(-1, 1),
            new CellCoord(-1, 0), new CellCoord(-1, -1), new CellCoord(0, -1), new CellCoord(1, -1)
        });

        internal static CellCoord LeftOf(CellCoord travelStep)
        {
            ValidateStep(travelStep);
            return new CellCoord(-travelStep.Y, travelStep.X);
        }

        internal static int StepsToBoundary(int width, int height, CellCoord origin, CellCoord step)
        {
            ValidateStep(step);
            if (width < 1 || height < 1 || origin.X < 0 || origin.Y < 0 || origin.X >= width || origin.Y >= height)
                throw new ArgumentOutOfRangeException(nameof(origin));
            int horizontal = step.X == 0 ? int.MaxValue : step.X > 0 ? width - 1 - origin.X : origin.X;
            int vertical = step.Y == 0 ? int.MaxValue : step.Y > 0 ? height - 1 - origin.Y : origin.Y;
            return Math.Min(horizontal, vertical);
        }

        internal static IReadOnlyList<CellCoord> LateralPriority(int width, int height, CellCoord victimCell, CellCoord travelStep)
        {
            var left = LeftOf(travelStep);
            var right = new CellCoord(-left.X, -left.Y);
            return StepsToBoundary(width, height, victimCell, left) <= StepsToBoundary(width, height, victimCell, right)
                ? new[] { left, right } : new[] { right, left };
        }

        private static void ValidateStep(CellCoord step)
        {
            if (step == CellCoord.Zero || step.X < -1 || step.X > 1 || step.Y < -1 || step.Y > 1)
                throw new ArgumentOutOfRangeException(nameof(step));
        }
    }
}
