using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public readonly struct StageNightSnapshot
    {
        public long NightLength { get; }
        public long CurrentNightTime { get; }
        public long RemainingNightTime { get; }

        public StageNightSnapshot(long nightLength, long currentNightTime)
        {
            if (nightLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(nightLength));
            if (currentNightTime < 0)
                throw new ArgumentOutOfRangeException(nameof(currentNightTime));

            NightLength = nightLength;
            CurrentNightTime = currentNightTime;
            RemainingNightTime = Math.Max(0L, nightLength - currentNightTime);
        }
    }
}