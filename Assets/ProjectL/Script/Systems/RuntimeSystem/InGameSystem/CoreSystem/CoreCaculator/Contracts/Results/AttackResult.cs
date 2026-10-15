using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public readonly struct AttackResult
    {
        public bool Succeeded { get; }
        private readonly IReadOnlyList<AttackBlockedContact> _blockedContacts;
        public IReadOnlyList<AttackBlockedContact> BlockedContacts => _blockedContacts ?? Array.Empty<AttackBlockedContact>();
        public AttackFailureReason FailureReason { get; }
        public EntityId AttackerId { get; }
        public EntityId PrimaryTargetId { get; }
        public Rotation4 AttackDirection { get; }
        public ActionCostSettlementSnapshot ActionCostSettlement { get; }
        public int ActionCostSpent => ActionCostSettlement?.Spent ?? 0;
        public int ActionCostRefunded => ActionCostSettlement?.Refunded ?? 0;
        public int FinalActionCost => ActionCostSettlement?.Final ?? 0;
        public ReadOnlyMemory<CellCoord> AffectedCells { get; }
        public ReadOnlyMemory<AttackHitResult> Hits { get; }
        public StageSnapshot CurrentStage { get; }
        public ReadOnlyMemory<TraitActivationSnapshot> TraitActivations { get; }
        public ReadOnlyMemory<CategoryActivationSnapshot> CategoryActivations { get; }
        /// <summary>Cell 조준으로 선택된 중심 Cell입니다. Direction·All 조준에서는 null입니다.</summary>
        public CellCoord? TargetCell { get; }
        private readonly AttackCellSelection _cellSelection;
        /// <summary>단일 지정의 주 셀이다. 방향 및 전체 지정에는 값이 없다.</summary>
        public CellCoord? PrimaryCell => _cellSelection?.PrimaryCell ?? TargetCell;
        /// <summary>선택한 방식의 직접 지정 셀이다. 기존 경로에서는 비어 있다.</summary>
        public ReadOnlyMemory<CellCoord> DesignatedCells => _cellSelection?.DesignatedCells ?? ReadOnlyMemory<CellCoord>.Empty;
        /// <summary>주 셀 중심의 추가 셀이다. 최종 영향 셀과 구분한다.</summary>
        public ReadOnlyMemory<CellCoord> AdditionalCells => _cellSelection?.AdditionalCells ?? ReadOnlyMemory<CellCoord>.Empty;

        private AttackResult(
            bool succeeded,
            AttackFailureReason failureReason,
            EntityId attackerId,
            EntityId primaryTargetId,
            Rotation4 attackDirection,
            ActionCostSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations,
            CellCoord? targetCell, AttackCellSelection cellSelection = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            TraitActivationSnapshot[] copiedTraitActivations =
                new TraitActivationSnapshot[traitActivations.Length];
            for (int i = 0; i < traitActivations.Length; i++)
            {
                if (traitActivations[i] == null)
                    throw new ArgumentException(
                        "Trait Activation 목록에는 null 항목을 넣을 수 없습니다.",
                        nameof(traitActivations));

                copiedTraitActivations[i] = traitActivations[i];
            }

            Succeeded = succeeded;
            FailureReason = failureReason;
            AttackerId = attackerId;
            PrimaryTargetId = primaryTargetId;
            TargetCell = targetCell;
            AttackDirection = attackDirection;
            ActionCostSettlement = actionCostSettlement;
            AffectedCells = affectedCells;
            Hits = hits;
            CurrentStage = currentStage;
            TraitActivations = copiedTraitActivations;
            CategoryActivations = (CategoryActivationSnapshot[])categoryActivations.Clone();
            _cellSelection = cellSelection;
            _blockedContacts = AttackBlockedContact.CopyCollection(blockedContacts);
        }

        public static AttackResult Success(
            EntityId attackerId,
            EntityId primaryTargetId,
            Rotation4 attackDirection,
            ActionCostSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations,
            AttackCellSelection cellSelection = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            ValidateSuccess(
                attackerId, attackDirection, actionCostSettlement, affectedCells, hits,
                currentStage, traitActivations, categoryActivations);
            if (cellSelection == null && primaryTargetId.IsEmpty)
                throw new ArgumentException("대상 ID는 비어 있을 수 없습니다.", nameof(primaryTargetId));
            if (cellSelection != null)
            {
                if (cellSelection.PrimaryCell.HasValue == primaryTargetId.IsEmpty)
                    throw new ArgumentException("주 셀과 주 대상 ID의 유무가 일치해야 합니다.", nameof(primaryTargetId));
                foreach (AttackHitResult hit in hits)
                {
                    bool validRole = cellSelection.PrimaryCell.HasValue
                        ? (hit.Role == AttackTargetRole.Primary || hit.Role == AttackTargetRole.Additional) &&
                          (hit.Role == AttackTargetRole.Primary) == (hit.TargetId == primaryTargetId)
                        : hit.Role == AttackTargetRole.Designated;
                    if (validRole == false)
                        throw new ArgumentException("피격 역할이 지정 결과와 일치하지 않습니다.", nameof(hits));
                }
            }

            return new AttackResult(
                true, AttackFailureReason.None, attackerId, primaryTargetId, attackDirection,
                actionCostSettlement, (CellCoord[])affectedCells.Clone(),
                (AttackHitResult[])hits.Clone(), currentStage, traitActivations,
                categoryActivations, cellSelection?.PrimaryCell, cellSelection, blockedContacts);
        }

        public static AttackResult SuccessForCell(
            EntityId attackerId,
            CellCoord targetCell,
            Rotation4 attackDirection,
            ActionCostSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            ValidateSuccess(
                attackerId, attackDirection, actionCostSettlement, affectedCells, hits,
                currentStage, traitActivations, categoryActivations);

            return new AttackResult(
                true, AttackFailureReason.None, attackerId, EntityId.Empty, attackDirection,
                actionCostSettlement, (CellCoord[])affectedCells.Clone(),
                (AttackHitResult[])hits.Clone(), currentStage, traitActivations,
                categoryActivations, targetCell, blockedContacts: blockedContacts);
        }

        public static AttackResult Failure(
            AttackFailureReason failureReason,
            EntityId attackerId,
            EntityId primaryTargetId,
            StageSnapshot currentStage, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            if (failureReason == AttackFailureReason.None)
                throw new ArgumentException("실패 사유는 None일 수 없습니다.", nameof(failureReason));
            if (attackerId.IsEmpty)
                throw new ArgumentException("공격자 ID는 비어 있을 수 없습니다.", nameof(attackerId));
            if (currentStage == null)
                throw new ArgumentNullException(nameof(currentStage), "현재 스테이지 스냅샷은 null일 수 없습니다.");

            return new AttackResult(
                false, failureReason, attackerId, primaryTargetId, Rotation4.R0, null,
                Array.Empty<CellCoord>(), Array.Empty<AttackHitResult>(), currentStage,
                Array.Empty<TraitActivationSnapshot>(),
                Array.Empty<CategoryActivationSnapshot>(), null, blockedContacts: blockedContacts);
        }

        /// <summary>Buffer 경로의 결과를 만든다. 빈 주 셀에는 주 대상 ID를 만들지 않는다.</summary>
        public static AttackResult FromFacts(EntityId attackerId, EntityId primaryTargetId,
            Rotation4 direction, ActionCostSettlementSnapshot cost, CellCoord[] affectedCells,
            AttackHitResult[] hits, AttackCellSelection selection,
            AttackFailureReason cancellationReason = AttackFailureReason.None,
            TraitActivationSnapshot[] traitActivations = null, CategoryActivationSnapshot[] categoryActivations = null,
            IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            traitActivations ??= Array.Empty<TraitActivationSnapshot>();
            categoryActivations ??= Array.Empty<CategoryActivationSnapshot>();
            if (cancellationReason != AttackFailureReason.None &&
                (cancellationReason != AttackFailureReason.AttackerDiedOnMovementSettlement ||
                 cost == null || cost.Spent != 0 || cost.Refunded != 0 || hits == null || hits.Length != 0))
                throw new ArgumentException("이동 정산 사망은 공격 비용과 피해를 남길 수 없습니다.");
            ValidateSuccess(attackerId, direction, cost, affectedCells, hits, null,
                traitActivations, categoryActivations, false, cancellationReason != AttackFailureReason.None);
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            if (!selection.PrimaryCell.HasValue && !primaryTargetId.IsEmpty)
                throw new ArgumentException("방향 공격에는 주 대상 ID를 만들 수 없습니다.");
            foreach (var hit in hits)
            {
                bool valid = selection.PrimaryCell.HasValue
                    ? (hit.Role == AttackTargetRole.Primary || hit.Role == AttackTargetRole.Additional) &&
                      (hit.Role == AttackTargetRole.Primary) == (hit.TargetId == primaryTargetId)
                    : hit.Role == AttackTargetRole.Designated;
                if (!valid) throw new ArgumentException("피격 역할과 주 대상이 일치하지 않습니다.");
            }
            return new AttackResult(cancellationReason == AttackFailureReason.None, cancellationReason,
                attackerId, primaryTargetId, direction, cost, (CellCoord[])affectedCells.Clone(),
                (AttackHitResult[])hits.Clone(), null, traitActivations, categoryActivations, selection.PrimaryCell, selection, blockedContacts);
        }

        private static void ValidateSuccess(
            EntityId attackerId,
            Rotation4 attackDirection,
            ActionCostSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations, bool requireStage = true, bool allowUnspent = false)
        {
            if (attackerId.IsEmpty)
                throw new ArgumentException("공격자 ID는 비어 있을 수 없습니다.", nameof(attackerId));
            if (Enum.IsDefined(typeof(Rotation4), attackDirection) == false)
                throw new ArgumentOutOfRangeException(nameof(attackDirection));
            if (actionCostSettlement == null)
                throw new ArgumentNullException(nameof(actionCostSettlement));
            if (!allowUnspent && actionCostSettlement.Spent <= 0)
                throw new ArgumentOutOfRangeException(nameof(actionCostSettlement));
            if (affectedCells == null) throw new ArgumentNullException(nameof(affectedCells));
            if (hits == null) throw new ArgumentNullException(nameof(hits));
            if (requireStage && currentStage == null) throw new ArgumentNullException(nameof(currentStage));
            if (traitActivations == null) throw new ArgumentNullException(nameof(traitActivations));
            if (categoryActivations == null) throw new ArgumentNullException(nameof(categoryActivations));
        }
    }
}
