using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public readonly struct CombatTurnSnapshot
    {
        public bool HasActiveUnit { get; }
        public EntityId? ActiveUnitId { get; }
        public long TimelineRevision { get; }
        public long CompletedUnitTurnCount { get; }

        /// <summary>
        /// 구 버전 생성자. 이후 삭제 예정
        /// </summary>
        /// <param name="hasActiveUnit"></param>
        /// <param name="activeUnitId"></param>
        public CombatTurnSnapshot(bool hasActiveUnit, EntityId? activeUnitId)
            : this(hasActiveUnit, activeUnitId, timelineRevision: 0L, completedUnitTurnCount: 0L)
        {
        }

        public CombatTurnSnapshot(bool hasActiveUnit, EntityId? activeUnitId, long timelineRevision, long completedUnitTurnCount)
        {
            if (timelineRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(timelineRevision));
            if (completedUnitTurnCount < 0)
                throw new ArgumentOutOfRangeException(nameof(completedUnitTurnCount));

            HasActiveUnit = hasActiveUnit;
            ActiveUnitId = activeUnitId;
            TimelineRevision = timelineRevision;
            CompletedUnitTurnCount = completedUnitTurnCount;
        }
    }
}
