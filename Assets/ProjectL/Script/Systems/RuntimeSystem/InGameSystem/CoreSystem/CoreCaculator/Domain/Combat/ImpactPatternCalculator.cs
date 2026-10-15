using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// 강제 이동에서 사용하는 4방향의 전방 셀 간격을 계산합니다.
    /// </summary>
    public sealed class ImpactPatternCalculator
    {
        internal static CellCoord GetForwardStep(Rotation4 attackDirection)
        {
            switch (attackDirection)
            {
                case Rotation4.R0: return new CellCoord(0, 1);
                case Rotation4.R90: return new CellCoord(1, 0);
                case Rotation4.R180: return new CellCoord(0, -1);
                case Rotation4.R270: return new CellCoord(-1, 0);
                default: throw new ArgumentOutOfRangeException(nameof(attackDirection));
            }
        }
    }
}
