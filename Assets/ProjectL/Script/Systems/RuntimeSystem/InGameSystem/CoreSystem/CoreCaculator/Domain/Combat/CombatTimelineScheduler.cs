using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{

    public sealed class CombatTimelineScheduler
    {
        public const long TimeScale = 1000L;

        public static long CalculateActionInterval(int delay, int priority)
        {
            if (delay <= 0)
                throw new ArgumentOutOfRangeException(nameof(delay));
            if (priority <= 0)
                throw new ArgumentOutOfRangeException(nameof(priority));

            long numerator = checked((long)100);
            long interval = (numerator + priority - 1L) / priority;
            return Math.Max(1L, interval);
        }

        internal static uint CalculateTieKey(int seed, EntityId id, int completedTurnCount)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)seed) * 16777619u;
                hash = (hash ^ (uint)id.Value) * 16777619u;
                hash = (hash ^ (uint)completedTurnCount) * 16777619u;
                return hash;
            }
        }

        internal static int CompareTie(uint leftTieKey, EntityId leftId, uint rightTieKey, EntityId rightId)
        {
            int keyComparison = leftTieKey.CompareTo(rightTieKey);
            return keyComparison != 0 ? keyComparison : leftId.Value.CompareTo(rightId.Value);
        }
    }
}
