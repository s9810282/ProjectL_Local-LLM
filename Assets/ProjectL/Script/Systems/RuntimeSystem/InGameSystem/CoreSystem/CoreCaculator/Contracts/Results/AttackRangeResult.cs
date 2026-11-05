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
        public ReadOnlyMemory<CellCoord> BasicAttackCells { get; }
        public ReadOnlyMemory<EntityId> InRangeTargetIds { get; }
        public ReadOnlyMemory<AttackAim> AttackAims { get; }
        /// <summary>
        /// 사거리 안 대상 중 공격자의 마무리 사격(즉시 처치) 조건을 지금 채운 대상입니다(마무리 사격 대상 표시 설계 FS1).
        /// 판정은 실제 공격과 같은 규칙이고, 마무리 사격이 없는 공격자면 비어 있습니다.
        /// </summary>
        public ReadOnlyMemory<EntityId> InstantDefeatTargetIds { get; }
        private readonly IReadOnlyList<AttackBlockedContact> _blockedContacts;
        public IReadOnlyList<AttackBlockedContact> BlockedContacts => _blockedContacts ?? Array.Empty<AttackBlockedContact>();

        private AttackRangeResult(
            bool succeeded,            AttackFailureReason failureReason,            EntityId attackerId,            CellCoord[] rangeCells,            EntityId[] inRangeTargetIds,            AttackAim[] attackAims,IReadOnlyList<AttackBlockedContact> blockedContacts = null,CellCoord[] basicAttackCells = null,
            EntityId[] instantDefeatTargetIds = null)
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
            BasicAttackCells = basicAttackCells == null ? copiedRangeCells : (CellCoord[])basicAttackCells.Clone();
            InRangeTargetIds = copiedInRangeTargetIds;
            AttackAims = (AttackAim[])attackAims.Clone();
            InstantDefeatTargetIds = instantDefeatTargetIds == null ? Array.Empty<EntityId>() : (EntityId[])instantDefeatTargetIds.Clone();
            _blockedContacts = AttackBlockedContact.CopyCollection(blockedContacts);
        }

        public static AttackRangeResult Success(
            EntityId attackerId,            CellCoord[] rangeCells,            EntityId[] inRangeTargetIds,            AttackAim[] attackAims,IReadOnlyList<AttackBlockedContact> blockedContacts = null,CellCoord[] basicAttackCells = null,
            EntityId[] instantDefeatTargetIds = null)
        {
            return new AttackRangeResult(
                true,                AttackFailureReason.None,                attackerId,                rangeCells,                inRangeTargetIds,                attackAims,blockedContacts,basicAttackCells,
                instantDefeatTargetIds);
        }

        public static AttackRangeResult Failure(
            AttackFailureReason failureReason,
            EntityId attackerId)
        {
            return new AttackRangeResult(
                false,                failureReason,                attackerId,                Array.Empty<CellCoord>(),                Array.Empty<EntityId>(),                Array.Empty<AttackAim>());
        }
    }
}
