using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct AttackPreview
    {
        public bool CanAttack { get; }
        private readonly IReadOnlyList<AttackBlockedContact> _blockedContacts;
        public IReadOnlyList<AttackBlockedContact> BlockedContacts => _blockedContacts ?? Array.Empty<AttackBlockedContact>();
        public AttackFailureReason FailureReason { get; }
        public EntityId AttackerId { get; }
        public EntityId PrimaryTargetId { get; }
        public Rotation4 AttackDirection { get; }
        public ActionCostSettlementSnapshot ActionCostSettlement { get; }
        public int AttackCost => ActionCostSettlement?.Spent ?? 0;
        public int ActionCostRefunded => ActionCostSettlement?.Refunded ?? 0;
        public int FinalActionCost => ActionCostSettlement?.Final ?? 0;
        public ReadOnlyMemory<CellCoord> AffectedCells { get; }
        public ReadOnlyMemory<AttackTargetPreview> Targets { get; }
        public ReadOnlyMemory<TraitStatusSnapshot> ProjectedTraitStatuses { get; }
        public CategorySnapshot ProjectedCategory { get; }
        /// <summary>Cell 조준으로 선택된 중심 Cell입니다. Direction·All 조준에서는 null입니다.</summary>
        public CellCoord? TargetCell { get; }
        private readonly AttackCellSelection _cellSelection;
        /// <summary>단일 지정의 주 셀이다. 방향 및 전체 지정에는 값이 없다.</summary>
        public CellCoord? PrimaryCell => _cellSelection?.PrimaryCell ?? TargetCell;
        /// <summary>선택한 방식의 직접 지정 셀이다. 기존 경로에서는 비어 있다.</summary>
        public ReadOnlyMemory<CellCoord> DesignatedCells => _cellSelection?.DesignatedCells ?? ReadOnlyMemory<CellCoord>.Empty;
        /// <summary>주 셀 중심의 추가 셀이다. 최종 영향 셀과 구분한다.</summary>
        public ReadOnlyMemory<CellCoord> AdditionalCells => _cellSelection?.AdditionalCells ?? ReadOnlyMemory<CellCoord>.Empty;

        private AttackPreview(
            bool canAttack,
            AttackFailureReason failureReason,
            EntityId attackerId,
            EntityId primaryTargetId,
            CellCoord? targetCell,
            Rotation4 attackDirection,
            ActionCostSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackTargetPreview[] targets,
            TraitStatusSnapshot[] projectedTraitStatuses,
            CategorySnapshot projectedCategory,
            AttackCellSelection cellSelection = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            CellCoord[] copiedCells = (CellCoord[])affectedCells.Clone();
            AttackTargetPreview[] copiedTargets = (AttackTargetPreview[])targets.Clone();
            TraitStatusSnapshot[] copiedTraitStatuses =
                new TraitStatusSnapshot[projectedTraitStatuses.Length];
            for (int i = 0; i < projectedTraitStatuses.Length; i++)
            {
                if (projectedTraitStatuses[i] == null)
                    throw new ArgumentException(
                        "Projected Trait Status 목록에는 null 항목을 넣을 수 없습니다.",
                        nameof(projectedTraitStatuses));

                copiedTraitStatuses[i] = projectedTraitStatuses[i];
            }

            CanAttack = canAttack;
            FailureReason = failureReason;
            AttackerId = attackerId;
            PrimaryTargetId = primaryTargetId;
            TargetCell = targetCell;
            AttackDirection = attackDirection;
            ActionCostSettlement = actionCostSettlement;
            AffectedCells = copiedCells;
            Targets = copiedTargets;
            ProjectedTraitStatuses = copiedTraitStatuses;
            ProjectedCategory = projectedCategory;
            _cellSelection = cellSelection;
            _blockedContacts = AttackBlockedContact.CopyCollection(blockedContacts);
        }

        /// <summary>공통 정산으로 계산한 피해를 표시한다. 빈 단일 중심에도 가짜 주 대상을 만들지 않는다.</summary>
        public static AttackPreview FromFacts(AttackResult result, AttackTargetPreview[] targets,
            TraitStatusSnapshot[] traits, CategorySnapshot category)
        {
            if (result.FailureReason != AttackFailureReason.None)
                return Failure(result.FailureReason, result.AttackerId, result.PrimaryTargetId, result.BlockedContacts);
            ValidateSuccess(result.AttackerId, result.AttackDirection, result.ActionCostSettlement,
                result.AffectedCells.ToArray(), targets, traits, category);
            foreach (var target in targets)
            {
                bool valid = result.PrimaryCell.HasValue
                    ? (target.Role == AttackTargetRole.Primary || target.Role == AttackTargetRole.Additional) &&
                      (target.Role == AttackTargetRole.Primary) == (target.TargetId == result.PrimaryTargetId)
                    : target.Role == AttackTargetRole.Designated;
                if (!valid) throw new ArgumentException("예상 피해의 역할이 지정 결과와 다릅니다.", nameof(targets));
            }
            return new AttackPreview(true, AttackFailureReason.None, result.AttackerId, result.PrimaryTargetId,
                result.PrimaryCell, result.AttackDirection, result.ActionCostSettlement, result.AffectedCells.ToArray(),
                targets, traits, category, new AttackCellSelection(result.PrimaryCell,
                    result.DesignatedCells.ToArray(), result.AdditionalCells.ToArray()), result.BlockedContacts);
        }

        public static AttackPreview Success(
            EntityId attackerId,
            EntityId primaryTargetId,
            Rotation4 attackDirection,
            ActionCostSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackTargetPreview[] targets,
            TraitStatusSnapshot[] projectedTraitStatuses,
            CategorySnapshot projectedCategory,
            AttackCellSelection cellSelection = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            ValidateSuccess(
                attackerId, attackDirection, actionCostSettlement, affectedCells,
                targets, projectedTraitStatuses, projectedCategory);
            if (cellSelection == null && primaryTargetId.IsEmpty)
                throw new ArgumentException("주요 대상의 아이디는 Empty일 수 없습니다.", nameof(primaryTargetId));
            if (cellSelection != null)
            {
                if (cellSelection.PrimaryCell.HasValue == primaryTargetId.IsEmpty)
                    throw new ArgumentException("주 셀과 주 대상 ID의 유무가 일치해야 합니다.", nameof(primaryTargetId));
                foreach (AttackTargetPreview target in targets)
                {
                    bool validRole = cellSelection.PrimaryCell.HasValue
                        ? (target.Role == AttackTargetRole.Primary || target.Role == AttackTargetRole.Additional) &&
                          (target.Role == AttackTargetRole.Primary) == (target.TargetId == primaryTargetId)
                        : target.Role == AttackTargetRole.Designated;
                    if (validRole == false)
                        throw new ArgumentException("대상 역할이 지정 결과와 일치하지 않습니다.", nameof(targets));
                }
            }

            return new AttackPreview(
                true, AttackFailureReason.None, attackerId, primaryTargetId, cellSelection?.PrimaryCell,
                attackDirection, actionCostSettlement, affectedCells, targets,
                projectedTraitStatuses, projectedCategory, cellSelection, blockedContacts);
        }

        public static AttackPreview SuccessForCell(
            EntityId attackerId,
            CellCoord targetCell,
            Rotation4 attackDirection,
            ActionCostSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackTargetPreview[] targets,
            TraitStatusSnapshot[] projectedTraitStatuses,
            CategorySnapshot projectedCategory, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            ValidateSuccess(
                attackerId, attackDirection, actionCostSettlement, affectedCells,
                targets, projectedTraitStatuses, projectedCategory);

            return new AttackPreview(
                true, AttackFailureReason.None, attackerId, EntityId.Empty, targetCell,
                attackDirection, actionCostSettlement, affectedCells, targets,
                projectedTraitStatuses, projectedCategory, blockedContacts: blockedContacts);
        }

        public static AttackPreview Failure(
            AttackFailureReason failureReason,
            EntityId attackerId,
            EntityId primaryTargetId, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            if (attackerId.IsEmpty)
                throw new ArgumentException("공격자의 아이디는 Empty일 수 없습니다.", nameof(attackerId));
            if (failureReason == AttackFailureReason.None)
                throw new ArgumentException("실패 사유는 None일 수 없습니다.", nameof(failureReason));

            return new AttackPreview(
                false, failureReason, attackerId, primaryTargetId, null, Rotation4.R0,
                null, Array.Empty<CellCoord>(), Array.Empty<AttackTargetPreview>(),
                Array.Empty<TraitStatusSnapshot>(), null, blockedContacts: blockedContacts);
        }

        private static void ValidateSuccess(
            EntityId attackerId,
            Rotation4 attackDirection,
            ActionCostSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackTargetPreview[] targets,
            TraitStatusSnapshot[] projectedTraitStatuses,
            CategorySnapshot projectedCategory)
        {
            if (attackerId.IsEmpty)
                throw new ArgumentException("공격자의 아이디는 Empty일 수 없습니다.", nameof(attackerId));
            if (Enum.IsDefined(typeof(Rotation4), attackDirection) == false)
                throw new ArgumentOutOfRangeException(nameof(attackDirection));
            if (actionCostSettlement == null)
                throw new ArgumentNullException(nameof(actionCostSettlement));
            if (actionCostSettlement.Spent <= 0)
                throw new ArgumentOutOfRangeException(nameof(actionCostSettlement));
            if (affectedCells == null) throw new ArgumentNullException(nameof(affectedCells));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (projectedTraitStatuses == null)
                throw new ArgumentNullException(nameof(projectedTraitStatuses));
            if (projectedCategory == null)
                throw new ArgumentNullException(nameof(projectedCategory));
        }
    }
}
