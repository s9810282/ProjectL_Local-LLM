using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>벽 모서리 절삭량만 정의한다. 이동 점유와 공격 정책은 별도 책임이다.</summary>
    public sealed class WallBlockingSpec
    {
        public const int TicksPerTile = 1024;
        public static WallBlockingSpec Default { get; } = new WallBlockingSpec(128);
        public int CornerCutTicks { get; }

        public WallBlockingSpec(int cornerCutTicks)
        {
            if (cornerCutTicks < 0 || cornerCutTicks >= TicksPerTile / 2)
                throw new ArgumentOutOfRangeException(nameof(cornerCutTicks));
            CornerCutTicks = cornerCutTicks;
        }
    }
}
