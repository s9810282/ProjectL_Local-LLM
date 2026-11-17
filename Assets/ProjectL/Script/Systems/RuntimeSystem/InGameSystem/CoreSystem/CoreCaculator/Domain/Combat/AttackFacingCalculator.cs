using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    internal static class AttackFacingCalculator
    {
        internal static Rotation4 Resolve(AttackAim aim, CellCoord origin)
        {
            if (aim.Kind == AttackAimKind.Direction)
                return aim.Direction switch
                {
                    AttackDirection.Right => Rotation4.R90,
                    AttackDirection.Down => Rotation4.R180,
                    AttackDirection.Left => Rotation4.R270,
                    _ => Rotation4.R0
                };
            if (!aim.Cell.HasValue) return Rotation4.R0;
            long x = (long)aim.Cell.Value.X - origin.X;
            long y = (long)aim.Cell.Value.Y - origin.Y;
            if (Math.Abs(x) >= Math.Abs(y))
                return x > 0 ? Rotation4.R90 : x < 0 ? Rotation4.R270 : Rotation4.R0;
            return y > 0 ? Rotation4.R0 : Rotation4.R180;
        }
    }
}
