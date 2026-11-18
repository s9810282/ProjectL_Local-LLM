using System;
using System.Diagnostics;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>측정 전용: 계획 AI가 부른 Core 함수 안에서 생긴 할당·시간을 칸마다 모은다(가장 바깥 호출만).</summary>
    internal static class AllocProbe
    {
        internal const int Slots = 10;
        internal static readonly long[] Bytes = new long[Slots];
        internal static readonly long[] Ticks = new long[Slots];
        internal static readonly long[] Calls = new long[Slots];
        private static int _depth;
        private static long _bytes0, _ticks0;

        internal static long B()
        {
            if (_depth++ == 0)
            {
                _bytes0 = GC.GetAllocatedBytesForCurrentThread();
                _ticks0 = Stopwatch.GetTimestamp();
            }
            return 0;
        }

        internal static T E<T>(int slot, long _, T value)
        {
            if (--_depth == 0)
            {
                Bytes[slot] += GC.GetAllocatedBytesForCurrentThread() - _bytes0;
                Ticks[slot] += Stopwatch.GetTimestamp() - _ticks0;
                Calls[slot]++;
            }
            return value;
        }

        internal static void Unwind() => _depth = 0;

        internal static void Reset()
        {
            Array.Clear(Bytes, 0, Slots);
            Array.Clear(Ticks, 0, Slots);
            Array.Clear(Calls, 0, Slots);
            _depth = 0;
        }
    }
}
