using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>임의 기울기의 유한 선분이 벽의 열린 내부와 겹치는지만 계산한다.</summary>
    internal static class WallSegmentClipper
    {
        public static bool IntersectsInterior(CellCoord start, CellCoord end, WallRun run,
            out SegmentParameter entry)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            entry = SegmentParameter.Zero;
            bool intersects = false;
            foreach (var part in run.Parts)
            {
                if (!IntersectsPart(start, end, part, out var candidate)) continue;
                if (!intersects || candidate.CompareTo(entry) < 0) entry = candidate;
                intersects = true;
            }
            return intersects;
        }

        private static bool IntersectsPart(CellCoord start, CellCoord end, WallConvexPart part,
            out SegmentParameter entry)
        {
            entry = SegmentParameter.Zero;
            var exit = SegmentParameter.One;
            checked
            {
                long ax = (long)start.X * WallBlockingSpec.TicksPerTile;
                long ay = (long)start.Y * WallBlockingSpec.TicksPerTile;
                long dx = ((long)end.X - start.X) * WallBlockingSpec.TicksPerTile;
                long dy = ((long)end.Y - start.Y) * WallBlockingSpec.TicksPerTile;
                var enter = SegmentParameter.Zero;
                // 외곽은 열린 경계, 내부 공유 변은 닫힌 분할 경계로 처리한다.
                if (!Clip(-1, 0, -part.XMin, ax, ay, dx, dy, ref enter, ref exit, (part.Exterior & WallBoundary.Left) != 0) ||
                    !Clip(1, 0, part.XMax, ax, ay, dx, dy, ref enter, ref exit, (part.Exterior & WallBoundary.Right) != 0) ||
                    !Clip(0, -1, -part.YMin, ax, ay, dx, dy, ref enter, ref exit, (part.Exterior & WallBoundary.Bottom) != 0) ||
                    !Clip(0, 1, part.YMax, ax, ay, dx, dy, ref enter, ref exit, (part.Exterior & WallBoundary.Top) != 0) ||
                    ((part.CutCorners & WallCorner.BottomLeft) != 0 && !Clip(-1, -1, -part.XMin - part.YMin - part.CornerCutTicks, ax, ay, dx, dy, ref enter, ref exit)) ||
                    ((part.CutCorners & WallCorner.TopLeft) != 0 && !Clip(-1, 1, -part.XMin + part.YMax - part.CornerCutTicks, ax, ay, dx, dy, ref enter, ref exit)) ||
                    ((part.CutCorners & WallCorner.BottomRight) != 0 && !Clip(1, -1, part.XMax - part.YMin - part.CornerCutTicks, ax, ay, dx, dy, ref enter, ref exit)) ||
                    ((part.CutCorners & WallCorner.TopRight) != 0 && !Clip(1, 1, part.XMax + part.YMax - part.CornerCutTicks, ax, ay, dx, dy, ref enter, ref exit)))
                    return false;

                entry = enter;
                return true;
            }
        }

        private static bool Clip(int nx, int ny, long limit, long ax, long ay, long dx, long dy,
            ref SegmentParameter enter, ref SegmentParameter exit, bool exterior = true)
        {
            checked
            {
                long u = nx * ax + ny * ay - limit;
                long v = nx * dx + ny * dy;
                if (v == 0) return exterior ? u < 0 : u <= 0;

                if (v > 0)
                {
                    var upper = new SegmentParameter(-u, v);
                    if (upper.CompareTo(exit) < 0) exit = upper;
                }
                else
                {
                    var lower = new SegmentParameter(u, -v);
                    if (lower.CompareTo(enter) > 0) enter = lower;
                }
                return enter.CompareTo(exit) < 0;
            }
        }
    }
}
