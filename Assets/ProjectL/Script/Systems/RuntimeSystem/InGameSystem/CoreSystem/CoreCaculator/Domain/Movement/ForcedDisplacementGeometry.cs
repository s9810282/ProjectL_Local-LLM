using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
{
    internal static class ForcedDisplacementGeometry
    {
        internal static CellCoord ResolveBranchStep(
            CellCoord attackerOrigin,
            CellCoord hitCell)
        {
            int stepX = Math.Sign((long)hitCell.X - attackerOrigin.X);
            int stepY = Math.Sign((long)hitCell.Y - attackerOrigin.Y);
            CellCoord step = new CellCoord(stepX, stepY);
            ValidateStep(step);
            return step;
        }

        internal static CellCoord ResolveDestinationAnchor(
            CellCoord targetAnchor,
            CellCoord step,
            int distanceCells)
        {
            ValidateStep(step);
            if (distanceCells <= 0)
                throw new ArgumentOutOfRangeException(nameof(distanceCells));

            return new CellCoord(
                checked(targetAnchor.X + checked(step.X * distanceCells)),
                checked(targetAnchor.Y + checked(step.Y * distanceCells)));
        }

        internal static CellCoord[] ResolveDestinationCells(
            TileFootprint footprint,
            CellCoord anchor,
            Rotation4 rotation)
        {
            if (footprint == null)
                throw new ArgumentNullException(nameof(footprint));
            if (Enum.IsDefined(typeof(Rotation4), rotation) == false)
                throw new ArgumentOutOfRangeException(nameof(rotation));

            var cells = new CellCoord[footprint.CellCount];
            for (int i = 0; i < cells.Length; i++)
            {
                CellCoord local = footprint.GetRotatedLocalCell(i, rotation);
                cells[i] = new CellCoord(
                    checked(anchor.X + local.X),
                    checked(anchor.Y + local.Y));
            }

            Array.Sort(cells, CompareByYThenX);
            return cells;
        }

        private static void ValidateStep(CellCoord step)
        {
            bool componentOutOfRange = step.X < -1 || step.X > 1 ||
                                       step.Y < -1 || step.Y > 1;
            if (componentOutOfRange || step == CellCoord.Zero)
                throw new ArgumentOutOfRangeException(nameof(step));
        }

        private static int CompareByYThenX(CellCoord left, CellCoord right)
        {
            int yComparison = left.Y.CompareTo(right.Y);
            return yComparison != 0 ? yComparison : left.X.CompareTo(right.X);
        }
    }
}
