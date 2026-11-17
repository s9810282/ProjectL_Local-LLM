using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public enum PlatformAxis { Horizontal = 1, Vertical }

    /// <summary>플랫폼의 고정 경로와 모양, 전투 진입 위치를 정의한다.</summary>
    public sealed class MovingPlatformSpec
    {
        public long SourceId { get; }
        public int Length { get; }
        public PlatformAxis ShapeAxis { get; }
        public PlatformAxis TravelAxis { get; }
        public CellCoord RouteStart { get; }
        public CellCoord RouteEnd { get; }
        public CellCoord InitialAnchor { get; }
        public int InitialSign { get; }

        public MovingPlatformSpec(long sourceId, int length, PlatformAxis shapeAxis, CellCoord routeStart,
            CellCoord routeEnd, CellCoord initialAnchor, int initialSign)
        {
            if (sourceId < 1) throw new ArgumentOutOfRangeException(nameof(sourceId));
            if (length < 1 || length > 3) throw new ArgumentOutOfRangeException(nameof(length));
            if (shapeAxis is not (PlatformAxis.Horizontal or PlatformAxis.Vertical))
                throw new ArgumentOutOfRangeException(nameof(shapeAxis));
            if (routeStart == routeEnd || routeStart.X != routeEnd.X && routeStart.Y != routeEnd.Y)
                throw new ArgumentException("플랫폼 경로는 서로 다른 두 점을 이은 가로 또는 세로 선분이어야 합니다.", nameof(routeEnd));
            if (initialSign is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(initialSign));
            SourceId = sourceId;
            Length = length;
            ShapeAxis = shapeAxis;
            TravelAxis = routeStart.X == routeEnd.X ? PlatformAxis.Vertical : PlatformAxis.Horizontal;
            RouteStart = routeStart;
            RouteEnd = routeEnd;
            if (!ContainsAnchor(initialAnchor)) throw new ArgumentOutOfRangeException(nameof(initialAnchor));
            InitialAnchor = initialAnchor;
            InitialSign = initialSign;
        }

        public bool ContainsAnchor(CellCoord anchor) =>
            anchor.X >= Math.Min(RouteStart.X, RouteEnd.X) && anchor.X <= Math.Max(RouteStart.X, RouteEnd.X) &&
            anchor.Y >= Math.Min(RouteStart.Y, RouteEnd.Y) && anchor.Y <= Math.Max(RouteStart.Y, RouteEnd.Y);

        /// <summary>지정한 기준 칸에서 모양 축의 양의 방향으로 전개한 점유 셀이다.</summary>
        public IReadOnlyList<CellCoord> FootprintAt(CellCoord anchor)
        {
            if (!ContainsAnchor(anchor)) throw new ArgumentOutOfRangeException(nameof(anchor));
            var cells = new CellCoord[Length];
            for (int index = 0; index < cells.Length; index++) cells[index] = CellAt(anchor, index);
            return Array.AsReadOnly(cells);
        }

        internal CellCoord CellAt(CellCoord anchor, int index) => ShapeAxis == PlatformAxis.Horizontal
            ? new CellCoord(checked(anchor.X + index), anchor.Y) : new CellCoord(anchor.X, checked(anchor.Y + index));

        internal bool SameValues(MovingPlatformSpec other) => other != null && SourceId == other.SourceId &&
            Length == other.Length && ShapeAxis == other.ShapeAxis && RouteStart == other.RouteStart &&
            RouteEnd == other.RouteEnd && InitialAnchor == other.InitialAnchor && InitialSign == other.InitialSign;
    }
}
