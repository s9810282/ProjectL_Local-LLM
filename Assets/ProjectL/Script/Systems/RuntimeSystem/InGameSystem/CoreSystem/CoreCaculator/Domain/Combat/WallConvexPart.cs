using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    [Flags]
    internal enum WallBoundary
    {
        None = 0,
        Left = 1,
        Right = 2,
        Bottom = 4,
        Top = 8,
        All = Left | Right | Bottom | Top
    }

    [Flags]
    internal enum WallCorner
    {
        None = 0,
        BottomLeft = 1,
        TopLeft = 2,
        BottomRight = 4,
        TopRight = 8,
        All = BottomLeft | TopLeft | BottomRight | TopRight
    }

    /// <summary>셀 합집합의 볼록 조각. 내부 공유 변은 분할 경계이며 실제 벽의 외곽이 아니다.</summary>
    internal readonly struct WallConvexPart
    {
        public long XMin { get; }
        public long XMax { get; }
        public long YMin { get; }
        public long YMax { get; }
        public int CornerCutTicks { get; }
        public WallBoundary Exterior { get; }
        public WallCorner CutCorners { get; }

        internal WallConvexPart(long xMin, long xMax, long yMin, long yMax, int cornerCutTicks,
            WallBoundary exterior, WallCorner cutCorners)
        {
            XMin = xMin;
            XMax = xMax;
            YMin = yMin;
            YMax = yMax;
            CornerCutTicks = cornerCutTicks;
            Exterior = exterior;
            CutCorners = cutCorners;
        }
    }
}
