using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct AttackRangeResult
    {
        public bool Succeeded { get; }
        public AttackFailureReason FailureReason { get; }
        public EntityId AttackerId { get; }
        public ReadOnlyMemory<CellCoord> RangeCells { get; }
        public ReadOnlyMemory<EntityId> InRangeTargetIds { get; }
        public bool CanAffordAttack { get; }
        public ReadOnlyMemory<AttackAim> AttackAims { get; }
        private readonly IReadOnlyList<AttackBlockedContact> _blockedContacts;
        public IReadOnlyList<AttackBlockedContact> BlockedContacts => _blockedContacts ?? Array.Empty<AttackBlockedContact>();

        private AttackRangeResult(
            bool succeeded,
            AttackFailureReason failureReason,
            EntityId attackerId,
            CellCoord[] rangeCells,
            EntityId[] inRangeTargetIds,
            bool canAffordAttack,
            AttackAim[] attackAims, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            if (rangeCells == null)
                throw new ArgumentNullException(nameof(rangeCells));
            if (inRangeTargetIds == null)
                throw new ArgumentNullException(nameof(inRangeTargetIds));
            if (attackAims == null)
                throw new ArgumentNullException(nameof(attackAims));

            CellCoord[] copiedRangeCells = new CellCoord[rangeCells.Length];
            Array.Copy(rangeCells, copiedRangeCells, rangeCells.Length);

            EntityId[] copiedInRangeTargetIds = new EntityId[inRangeTargetIds.Length];
            Array.Copy(inRangeTargetIds, copiedInRangeTargetIds, inRangeTargetIds.Length);

            Succeeded = succeeded;
            FailureReason = failureReason;
            AttackerId = attackerId;
            RangeCells = copiedRangeCells;
            InRangeTargetIds = copiedInRangeTargetIds;
            CanAffordAttack = canAffordAttack;
            AttackAims = (AttackAim[])attackAims.Clone();
            _blockedContacts = AttackBlockedContact.CopyCollection(blockedContacts);
        }

        public static AttackRangeResult Success(
            EntityId attackerId,
            CellCoord[] rangeCells,
            EntityId[] inRangeTargetIds,
            bool canAffordAttack,
            AttackAim[] attackAims, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            return new AttackRangeResult(
                true,
                AttackFailureReason.None,
                attackerId,
                rangeCells,
                inRangeTargetIds,
                canAffordAttack,
                attackAims, blockedContacts);
        }

        public static AttackRangeResult Failure(
            AttackFailureReason failureReason,
            EntityId attackerId)
        {
            return new AttackRangeResult(
                false,
                failureReason,
                attackerId,
                Array.Empty<CellCoord>(),
                Array.Empty<EntityId>(),
                false,
                Array.Empty<AttackAim>());
        }
    }
}
