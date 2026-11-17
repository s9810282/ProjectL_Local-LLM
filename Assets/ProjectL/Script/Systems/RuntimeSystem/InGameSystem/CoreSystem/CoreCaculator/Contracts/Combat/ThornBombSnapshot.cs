using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>부착 시점에 확정해 대상과 함께 유지하는 폭탄 사실이다.</summary>
    public sealed class ThornBombSnapshot
    {
        public long BombId { get; }
        public EntityId SourceId { get; }
        public Owner SourceOwner { get; }
        public EntityId TargetId { get; }
        public CellCoord AttachmentLocalCell { get; }
        public int CapturedAttackPower { get; }
        public int DamagePercent { get; }
        public long DueControllerTurnSequence { get; }

        public ThornBombSnapshot(long bombId, EntityId sourceId, Owner sourceOwner, EntityId targetId,
            CellCoord attachmentLocalCell, int capturedAttackPower, int damagePercent, long dueControllerTurnSequence)
        {
            if (bombId < 1) throw new ArgumentOutOfRangeException(nameof(bombId));
            if (sourceId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(sourceId));
            if (sourceOwner is not (Owner.Player or Owner.Enemy))
                throw new ArgumentOutOfRangeException(nameof(sourceOwner));
            if (targetId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(targetId));
            if (capturedAttackPower < 0) throw new ArgumentOutOfRangeException(nameof(capturedAttackPower));
            if (damagePercent < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
            if (dueControllerTurnSequence < 1) throw new ArgumentOutOfRangeException(nameof(dueControllerTurnSequence));

            BombId = bombId;
            SourceId = sourceId;
            SourceOwner = sourceOwner;
            TargetId = targetId;
            AttachmentLocalCell = attachmentLocalCell;
            CapturedAttackPower = capturedAttackPower;
            DamagePercent = damagePercent;
            DueControllerTurnSequence = dueControllerTurnSequence;
        }

        internal bool SameValues(ThornBombSnapshot other) =>
            other != null && BombId == other.BombId && SourceId == other.SourceId && SourceOwner == other.SourceOwner &&
            TargetId == other.TargetId && AttachmentLocalCell == other.AttachmentLocalCell &&
            CapturedAttackPower == other.CapturedAttackPower && DamagePercent == other.DamagePercent &&
            DueControllerTurnSequence == other.DueControllerTurnSequence;
    }
}
