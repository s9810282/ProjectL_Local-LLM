using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    internal static class AttackSweepGeometry
    {
        internal static CellCoord Step(Rotation4 facing, AttackSweepSide side)
        {
            AttackSweepSideValidation.Validate(side);
            var right = facing switch
            {
                Rotation4.R0 => CellCoord.Right,
                Rotation4.R90 => CellCoord.Down,
                Rotation4.R180 => CellCoord.Left,
                Rotation4.R270 => CellCoord.Up,
                _ => throw new ArgumentOutOfRangeException(nameof(facing))
            };
            return side == AttackSweepSide.Right ? right : new CellCoord(-right.X, -right.Y);
        }

        internal static CellCoord Destination(IReadOnlyList<CellCoord> areaCells, CellCoord start, CellCoord step)
        {
            if (areaCells == null) throw new ArgumentNullException(nameof(areaCells));
            if (Math.Abs((long)step.X) + Math.Abs((long)step.Y) != 1)
                throw new ArgumentOutOfRangeException(nameof(step));
            bool found = false;
            var edge = start;
            foreach (var cell in areaCells)
            {
                if (step.X != 0 ? cell.Y != start.Y : cell.X != start.X) continue;
                found = true;
                if (Project(cell, step) > Project(edge, step)) edge = cell;
            }
            if (!found) throw new ArgumentException("대상의 쓸기 줄이 범위에 없습니다.", nameof(start));
            return new CellCoord(checked(edge.X + step.X), checked(edge.Y + step.Y));
        }

        internal static long Project(CellCoord cell, CellCoord axis) => (long)cell.X * axis.X + (long)cell.Y * axis.Y;

        internal static AttackSweepSide ResolveSide(AttackSweepSide? side)
        {
            AttackSweepSideValidation.Validate(side);
            return side ?? AttackSweepSide.Right;
        }

        internal static Rotation4 ResolveFacing(AttackAim aim, CellCoord origin, Rotation4 currentFacing)
        {
            if (aim.Kind != AttackAimKind.All)
                return AttackFacingCalculator.Resolve(aim, origin);
            if (!Enum.IsDefined(typeof(Rotation4), currentFacing))
                throw new ArgumentOutOfRangeException(nameof(currentFacing));
            return currentFacing;
        }
    }
}
