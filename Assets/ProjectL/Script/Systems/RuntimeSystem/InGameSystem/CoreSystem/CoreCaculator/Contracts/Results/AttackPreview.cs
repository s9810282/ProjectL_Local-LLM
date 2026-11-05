using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
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
        public PiercingShotDelivery PiercingShot { get; }
        public AttackPullSnapshot Pull { get; }
        private readonly IReadOnlyList<AttackKnockbackSnapshot> _knockbacks;
        /// <summary>공격 결과의 넉백 요약과 같습니다. 칸 단위 재생은 Moved 사건을 씁니다.</summary>
        public IReadOnlyList<AttackKnockbackSnapshot> Knockbacks => _knockbacks ?? Array.Empty<AttackKnockbackSnapshot>();
        public Rotation4 AttackDirection { get; }
        public UnitActionUseSettlementSnapshot ActionUseSettlement { get; }
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
            UnitActionUseSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackTargetPreview[] targets,
            TraitStatusSnapshot[] projectedTraitStatuses,
            CategorySnapshot projectedCategory,
            AttackCellSelection cellSelection = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null,
            PiercingShotDelivery piercingShot = null, AttackPullSnapshot pull = null,
            IReadOnlyList<AttackKnockbackSnapshot> knockbacks = null)
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
            PiercingShot = piercingShot;
            Pull = pull;
            // 결과가 이미 복사한 읽기 전용 목록을 그대로 공유합니다.
            _knockbacks = knockbacks == null || knockbacks.Count == 0 ? null : knockbacks;
            TargetCell = targetCell;
            AttackDirection = attackDirection;
            ActionUseSettlement = actionCostSettlement;
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
            CellCoord[] affectedCells = result.AffectedCells.ToArray();
            ValidateSuccess(result.AttackerId, result.AttackDirection, result.ActionUseSettlement,
                affectedCells, targets, traits, category);
            PiercingShotDelivery.ValidateCorrespondence(result.PiercingShot, result.AttackerId, result.PrimaryTargetId,
                result.PrimaryCell, result.AdditionalCells, affectedCells, targets,
                target => target.TargetId, target => target.Role, target => target.Contacts);
            if (result.PiercingShot == null)
                AttackTargetRoleValidation.ValidateOrdinary(result.PrimaryCell, result.PrimaryTargetId, targets,
                    target => target.TargetId, target => target.Role, nameof(targets), target => target.Contacts);
            // 미리보기 대상은 결과와 따로 받으므로 넉백 대상이 그 안에 있는지 확인합니다. 경로·중복은 결과가 검사했습니다.
            foreach (var knockback in result.Knockbacks)
                if (!Array.Exists(targets, target => target.TargetId == knockback.TargetId))
                    throw new ArgumentException("넉백 기록의 대상이 미리보기 대상에 없습니다.", nameof(targets));
            return new AttackPreview(true, AttackFailureReason.None, result.AttackerId, result.PrimaryTargetId,
                result.PrimaryCell, result.AttackDirection, result.ActionUseSettlement, affectedCells,
                targets, traits, category, new AttackCellSelection(result.PrimaryCell,
                    result.DesignatedCells.ToArray(), result.AdditionalCells.ToArray()), result.BlockedContacts, result.PiercingShot, result.Pull,
                result.Knockbacks);
        }

        public static AttackPreview Success(
            EntityId attackerId,
            EntityId primaryTargetId,
            Rotation4 attackDirection,
            UnitActionUseSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackTargetPreview[] targets,
            TraitStatusSnapshot[] projectedTraitStatuses,
            CategorySnapshot projectedCategory,
            AttackCellSelection cellSelection = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null,
            PiercingShotDelivery piercingShot = null)
        {
            ValidateSuccess(
                attackerId, attackDirection, actionCostSettlement, affectedCells,
                targets, projectedTraitStatuses, projectedCategory);
            PiercingShotDelivery.ValidateCorrespondence(piercingShot, attackerId, primaryTargetId,
                cellSelection?.PrimaryCell, cellSelection?.AdditionalCells ?? ReadOnlyMemory<CellCoord>.Empty,
                affectedCells, targets, target => target.TargetId, target => target.Role, target => target.Contacts);
            if (cellSelection == null && primaryTargetId.IsEmpty)
                throw new ArgumentException("주요 대상의 아이디는 Empty일 수 없습니다.", nameof(primaryTargetId));
            if (cellSelection != null)
            {
                if (!cellSelection.PrimaryCell.HasValue && !primaryTargetId.IsEmpty)
                    throw new ArgumentException("방향 공격에는 주 대상 ID를 만들 수 없습니다.", nameof(primaryTargetId));
                if (piercingShot == null)
                    AttackTargetRoleValidation.ValidateOrdinary(cellSelection.PrimaryCell, primaryTargetId, targets,
                        target => target.TargetId, target => target.Role, nameof(targets), target => target.Contacts);
            }

            return new AttackPreview(
                true, AttackFailureReason.None, attackerId, primaryTargetId, cellSelection?.PrimaryCell,
                attackDirection, actionCostSettlement, affectedCells, targets,
                projectedTraitStatuses, projectedCategory, cellSelection, blockedContacts, piercingShot);
        }

        public static AttackPreview SuccessForCell(
            EntityId attackerId,
            CellCoord targetCell,
            Rotation4 attackDirection,
            UnitActionUseSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackTargetPreview[] targets,
            TraitStatusSnapshot[] projectedTraitStatuses,
            CategorySnapshot projectedCategory, IReadOnlyList<AttackBlockedContact> blockedContacts = null,
            PiercingShotDelivery piercingShot = null)
        {
            ValidateSuccess(
                attackerId, attackDirection, actionCostSettlement, affectedCells,
                targets, projectedTraitStatuses, projectedCategory);
            PiercingShotDelivery.ValidateCorrespondence(piercingShot, attackerId, EntityId.Empty,
                targetCell, ReadOnlyMemory<CellCoord>.Empty, affectedCells, targets, target => target.TargetId, target => target.Role, target => target.Contacts);
            var selection = piercingShot == null ? null :
                new AttackCellSelection(targetCell, new[] { targetCell }, Array.Empty<CellCoord>());

            return new AttackPreview(
                true, AttackFailureReason.None, attackerId, EntityId.Empty, targetCell,
                attackDirection, actionCostSettlement, affectedCells, targets,
                projectedTraitStatuses, projectedCategory, selection, blockedContacts, piercingShot);
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
            UnitActionUseSettlementSnapshot actionCostSettlement,
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
            if (actionCostSettlement.ActorId != attackerId)
                throw new ArgumentOutOfRangeException(nameof(actionCostSettlement));
            if (affectedCells == null) throw new ArgumentNullException(nameof(affectedCells));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (targets.Any(t => t.Contacts != null))
            {
                if (targets.Any(t => t.Contacts == null) || targets.Select(t => t.TargetId).Distinct().Count() != targets.Length)
                    throw new ArgumentException("신규 미리보기에는 중복 없는 실제 수신자와 접촉이 필요합니다.", nameof(targets));
                var contacts = targets.SelectMany(t => t.Contacts).ToArray();
                if (contacts.Select(c => c.DeliveryIndex).Distinct().Count() != contacts.Length ||
                    contacts.Any(c => c.Origin.CreditedCharacterId != attackerId) || contacts.Select(c => c.Origin.AttackGroup).Distinct().Count() > 1 ||
                    targets.Any(t => t.IsDirectHit != (t.Contacts.Count > 0)))
                    throw new ArgumentException("미리보기 접촉과 공격 출처가 일치하지 않습니다.", nameof(targets));
            }
            if (projectedTraitStatuses == null)
                throw new ArgumentNullException(nameof(projectedTraitStatuses));
            if (projectedCategory == null)
                throw new ArgumentNullException(nameof(projectedCategory));
        }
    }
}
