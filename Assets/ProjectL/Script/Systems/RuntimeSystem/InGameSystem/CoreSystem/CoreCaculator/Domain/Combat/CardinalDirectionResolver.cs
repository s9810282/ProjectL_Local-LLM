using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>
    /// 좌표 차이를 네 직교 Rotation4로 환산합니다.
    /// 공격 방향과 방어 Facing이 같은 방향 개념을 공유하도록 한 곳에 둡니다.
    /// 대각 동률의 최종 규칙은 Q6이며, 지금은 기존 공격 경로와 같은 수평 우선을 사용합니다.
    /// </summary>
    public static class CardinalDirectionResolver
    {
        public static Rotation4 FromDelta(CellCoord from, CellCoord to)
        {
            long deltaX = (long)to.X - from.X;
            long deltaY = (long)to.Y - from.Y;

            if (Math.Abs(deltaX) >= Math.Abs(deltaY))
            {
                if (deltaX > 0) return Rotation4.R90;
                if (deltaX < 0) return Rotation4.R270;
            }
            else
            {
                if (deltaY > 0) return Rotation4.R0;
                if (deltaY < 0) return Rotation4.R180;
            }

            return Rotation4.R0;
        }
    }
}
