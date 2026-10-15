using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public readonly struct CombatTimelineEntrySnapshot
    {
        public EntityId? UnitId { get; }
        public int? CurrentPriority { get; }
        public long ScheduledTime { get; }
        public bool IsActive { get; }
        public CombatTimelineEntryKind Kind { get; }
        public StageGimmickId? GimmickId { get; }
        public StageGimmickKey GimmickKey { get; }
        public long? RemainingUnitTurns { get; }
        public long? ProjectedNightTimeAfterTurn { get; }
        public bool EndsNightAfterTurn { get; }
        public long? EnvironmentSourceId { get; }

        private CombatTimelineEntrySnapshot(
            EntityId? entityId,
            int? currentPriority,
            long scheduledTime,
            bool isActive,
            CombatTimelineEntryKind kind,
            StageGimmickId? gimmickId,
            StageGimmickKey gimmickKey,
            long? remainingUnitTurns,
            long? projectedNightTimeAfterTurn,
            bool endsNightAfterTurn, long? environmentSourceId = null)
        {
            UnitId = entityId;
            CurrentPriority = currentPriority;
            ScheduledTime = scheduledTime;
            IsActive = isActive;
            Kind = kind;
            GimmickId = gimmickId;
            GimmickKey = gimmickKey;
            RemainingUnitTurns = remainingUnitTurns;
            ProjectedNightTimeAfterTurn = projectedNightTimeAfterTurn;
            EndsNightAfterTurn = endsNightAfterTurn;
            EnvironmentSourceId = environmentSourceId;
        }

        public static CombatTimelineEntrySnapshot ForUnit(
            EntityId entityId,
            int currentPriority,
            long scheduledTime,
            bool isActive)
        {
            if (entityId.IsEmpty)
                throw new ArgumentException("EntityId는 빈 값일 수 없습니다.", nameof(entityId));
            if (currentPriority <= 0)
                throw new ArgumentOutOfRangeException(nameof(currentPriority));
            if (scheduledTime < 0)
                throw new ArgumentOutOfRangeException(nameof(scheduledTime));

            return new CombatTimelineEntrySnapshot(
                entityId,
                currentPriority,
                scheduledTime,
                isActive,
                CombatTimelineEntryKind.UnitTurn,
                null,
                StageGimmickKey.None,
                null,
                null,
                false);
        }

        public static CombatTimelineEntrySnapshot ForUnit(
            EntityId entityId,
            int currentPriority,
            long scheduledTime,
            bool isActive,
            long projectedNightTimeAfterTurn,
            bool endsNightAfterTurn)
        {
            if (entityId.IsEmpty)
                throw new ArgumentException("EntityId는 빈 값일 수 없습니다.", nameof(entityId));
            if (currentPriority <= 0)
                throw new ArgumentOutOfRangeException(nameof(currentPriority));
            if (scheduledTime < 0)
                throw new ArgumentOutOfRangeException(nameof(scheduledTime));
            if (projectedNightTimeAfterTurn < 0)
                throw new ArgumentOutOfRangeException(nameof(projectedNightTimeAfterTurn));

            return new CombatTimelineEntrySnapshot(
                entityId,
                currentPriority,
                scheduledTime,
                isActive,
                CombatTimelineEntryKind.UnitTurn,
                null,
                StageGimmickKey.None,
                null,
                projectedNightTimeAfterTurn,
                endsNightAfterTurn);
        }

        public static CombatTimelineEntrySnapshot ForTornado(long sourceId, long scheduledTime, long remainingUnitTurns)
        {
            if (sourceId < 1 || scheduledTime < 0 || remainingUnitTurns < 0) throw new ArgumentOutOfRangeException(nameof(sourceId));
            return new CombatTimelineEntrySnapshot(null, null, scheduledTime, false, CombatTimelineEntryKind.Environment,
                null, StageGimmickKey.None, remainingUnitTurns, null, false, sourceId);
        }

        public static CombatTimelineEntrySnapshot ForStageGimmick(
            StageGimmickId gimmickId,
            StageGimmickKey gimmickKey,
            long scheduledTime,
            long remainingUnitTurns)
        {
            if (gimmickId.IsEmpty)
                throw new ArgumentException("StageGimmickId는 빈 값일 수 없습니다.", nameof(gimmickId));
            if (gimmickKey == StageGimmickKey.None ||
                Enum.IsDefined(typeof(StageGimmickKey), gimmickKey) == false)
            {
                throw new ArgumentOutOfRangeException(nameof(gimmickKey));
            }
            if (scheduledTime < 0)
                throw new ArgumentOutOfRangeException(nameof(scheduledTime));
            if (remainingUnitTurns < 0)
                throw new ArgumentOutOfRangeException(nameof(remainingUnitTurns));

            return new CombatTimelineEntrySnapshot(
                null,
                null,
                scheduledTime,
                false,
                CombatTimelineEntryKind.StageGimmick,
                gimmickId,
                gimmickKey,
                remainingUnitTurns,
                null,
                false);
        }

        public static CombatTimelineEntrySnapshot ForNightEnd(
            long scheduledTime,
            long nightLength)
        {
            if (scheduledTime < 0)
                throw new ArgumentOutOfRangeException(nameof(scheduledTime));
            if (nightLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(nightLength));

            return new CombatTimelineEntrySnapshot(
                null,
                null,
                scheduledTime,
                false,
                CombatTimelineEntryKind.NightEnd,
                null,
                StageGimmickKey.None,
                null,
                null,
                false);
        }
    }
}
