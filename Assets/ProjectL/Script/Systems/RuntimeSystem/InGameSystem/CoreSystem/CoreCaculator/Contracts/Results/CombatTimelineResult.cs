using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct CombatTimelineResult
    {
        public bool Succeeded { get; }
        public long Revision { get; }
        public long CurrentTime { get; }
        public CombatTimelineFailureReason FailureReason { get; }
        public ReadOnlyMemory<CombatTimelineEntrySnapshot> Entries { get; }

        private CombatTimelineResult(
            bool succeeded,
            long revision,
            long currentTime,
            CombatTimelineFailureReason failureReason,
            CombatTimelineEntrySnapshot[] entries)
        {
            CombatTimelineEntrySnapshot[] copiedEntrySnapshot = new CombatTimelineEntrySnapshot[entries.Length];
            Array.Copy(entries, copiedEntrySnapshot, entries.Length);

            Succeeded = succeeded;
            Revision = revision;
            CurrentTime = currentTime;
            FailureReason = failureReason;
            Entries = new ReadOnlyMemory<CombatTimelineEntrySnapshot>(copiedEntrySnapshot);
        }

        public static CombatTimelineResult Success(long revision, long currentTime, CombatTimelineEntrySnapshot[] entries)
        {
            if (revision < 0)
                throw new ArgumentOutOfRangeException(nameof(revision));
            if (currentTime < 0)
                throw new ArgumentOutOfRangeException(nameof(currentTime));
            if (entries == null)
                throw new ArgumentNullException(nameof(entries));

            return new CombatTimelineResult(
                succeeded: true,
                revision: revision,
                currentTime: currentTime,
                failureReason: CombatTimelineFailureReason.None,
                entries: entries);
        }

        public static CombatTimelineResult Failure(CombatTimelineFailureReason failureReason)
        {
            if (failureReason == CombatTimelineFailureReason.None)
                throw new ArgumentException("Failure reason cannot be None for a failure result.", nameof(failureReason));

            return new CombatTimelineResult(
                succeeded: false,
                revision: 0,
                currentTime: 0,
                failureReason: failureReason,
                entries: Array.Empty<CombatTimelineEntrySnapshot>());
        }
    }
}
