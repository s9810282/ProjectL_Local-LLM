using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement
{
    internal static class MovementPathCalculator
    {
        internal static CellCoord[] ResolvePathCells(
            CellCoord checkpointAnchor,
            CellCoord committedAnchor,
            TileFootprint footprint,
            Rotation4 rotation)
        {
            if (footprint == null)
                throw new ArgumentNullException(nameof(footprint));
            if (Enum.IsDefined(typeof(Rotation4), rotation) == false)
                throw new ArgumentOutOfRangeException(nameof(rotation));

            var excluded = new HashSet<CellCoord>();
            for (int i = 0; i < footprint.CellCount; i++)
            {
                excluded.Add(footprint.GetOccupiedCell(
                    i,
                    checkpointAnchor,
                    rotation));
            }

            var result = new List<CellCoord>();
            CellCoord cursor = checkpointAnchor;
            while (cursor != committedAnchor)
            {
                int stepX = Math.Sign((long)committedAnchor.X - cursor.X);
                int stepY = Math.Sign((long)committedAnchor.Y - cursor.Y);
                cursor = new CellCoord(
                    checked(cursor.X + stepX),
                    checked(cursor.Y + stepY));

                for (int i = 0; i < footprint.CellCount; i++)
                {
                    CellCoord cell = footprint.GetOccupiedCell(
                        i,
                        cursor,
                        rotation);
                    if (excluded.Add(cell))
                        result.Add(cell);
                }
            }

            return result.ToArray();
        }
    }
}
