using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// Heavy의 전방 확장 보호 영역을 계산하는 순수 geometry입니다.
    /// 전방 1칸마다 좌우로 1칸씩 넓어지며 첫 줄 폭은 1입니다. 즉 깊이 d에서 폭 2d-1입니다.
    /// 이 Facing은 footprint Rotation과 무관한 독립 방어 방향입니다.
    /// </summary>
    public static class DirectionalArmorCoverageCalculator
    {
        /// <summary>
        /// 피해 원점이 방어자의 보호 영역 안인지 판정합니다.
        /// </summary>
        public static bool IsProtected(CellCoord defenderAnchor, Rotation4 defenseFacing, CellCoord impactOrigin)
        {
            if (Enum.IsDefined(typeof(Rotation4), defenseFacing) == false)
                throw new ArgumentOutOfRangeException(nameof(defenseFacing));

            long deltaX = (long)impactOrigin.X - defenderAnchor.X;
            long deltaY = (long)impactOrigin.Y - defenderAnchor.Y;

            GetForwardAndLateral(defenseFacing, deltaX, deltaY, out long forwardDistance, out long lateralOffset);

            return forwardDistance >= 1 &&
                   Math.Abs(lateralOffset) <= forwardDistance - 1;
        }

        /// <summary>
        /// 월드 좌표 차이를 Facing 기준 전방/측면 거리로 변환합니다.
        /// </summary>
        private static void GetForwardAndLateral(
            Rotation4 defenseFacing,
            long deltaX,
            long deltaY,
            out long forwardDistance,
            out long lateralOffset)
        {
            switch (defenseFacing)
            {
                case Rotation4.R0: // +Y
                    forwardDistance = deltaY;
                    lateralOffset = deltaX;
                    return;
                case Rotation4.R90: // +X
                    forwardDistance = deltaX;
                    lateralOffset = -deltaY;
                    return;
                case Rotation4.R180: // -Y
                    forwardDistance = -deltaY;
                    lateralOffset = -deltaX;
                    return;
                case Rotation4.R270: // -X
                    forwardDistance = -deltaX;
                    lateralOffset = deltaY;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(defenseFacing));
            }
        }
    }
}
