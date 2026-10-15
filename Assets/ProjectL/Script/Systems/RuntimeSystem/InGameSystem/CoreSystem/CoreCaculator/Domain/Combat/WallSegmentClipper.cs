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
            var exit = SegmentParameter.One;
            checked
            {
                long ax = (long)start.X * WallBlockingSpec.TicksPerTile;
                long ay = (long)start.Y * WallBlockingSpec.TicksPerTile;
                long dx = ((long)end.X - start.X) * WallBlockingSpec.TicksPerTile;
                long dy = ((long)end.Y - start.Y) * WallBlockingSpec.TicksPerTile;
                var enter = SegmentParameter.Zero;
                // 각 경계는 엄격한 부등식이다. 같은 진입·이탈 시각이나 경계 위 구간은 차단하지 않는다.
                if (!Clip(-1, 0, -run.XMin, ax, ay, dx, dy, ref enter, ref exit) ||
                    !Clip(1, 0, run.XMax, ax, ay, dx, dy, ref enter, ref exit) ||
                    !Clip(0, -1, -run.YMin, ax, ay, dx, dy, ref enter, ref exit) ||
                    !Clip(0, 1, run.YMax, ax, ay, dx, dy, ref enter, ref exit) ||
                    !Clip(-1, -1, -run.XMin - run.YMin - run.CornerCutTicks, ax, ay, dx, dy, ref enter, ref exit) ||
                    !Clip(-1, 1, -run.XMin + run.YMax - run.CornerCutTicks, ax, ay, dx, dy, ref enter, ref exit) ||
                    !Clip(1, -1, run.XMax - run.YMin - run.CornerCutTicks, ax, ay, dx, dy, ref enter, ref exit) ||
                    !Clip(1, 1, run.XMax + run.YMax - run.CornerCutTicks, ax, ay, dx, dy, ref enter, ref exit))
                    return false;

                entry = enter;
                return true;
            }
        }

        private static bool Clip(int nx, int ny, long limit, long ax, long ay, long dx, long dy,
            ref SegmentParameter enter, ref SegmentParameter exit)
        {
            checked
            {
                long u = nx * ax + ny * ay - limit;
                long v = nx * dx + ny * dy;
                if (v == 0) return u < 0;

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
