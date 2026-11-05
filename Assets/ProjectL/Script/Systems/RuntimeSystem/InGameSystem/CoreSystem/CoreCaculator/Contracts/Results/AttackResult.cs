using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
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
        public PiercingShotDelivery PiercingShot { get; }
        public AttackPullSnapshot Pull { get; }
        private readonly IReadOnlyList<AttackKnockbackSnapshot> _knockbacks;
        /// <summary>이 공격의 대상별 넉백 요약입니다. 처리 순서(대상 ID 오름차순)입니다. 칸 단위 재생은 Moved 사건을 씁니다.</summary>
        public IReadOnlyList<AttackKnockbackSnapshot> Knockbacks => _knockbacks ?? Array.Empty<AttackKnockbackSnapshot>();
        /// <summary>후속 피해 출처와 대응하는 Core 공격 그룹입니다. 0은 대응 정보가 없는 옛 결과입니다.</summary>
        public long AttackGroup { get; }
        public Rotation4 AttackDirection { get; }
        public UnitActionUseSettlementSnapshot ActionUseSettlement { get; }
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
            UnitActionUseSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations,
            CellCoord? targetCell, AttackCellSelection cellSelection = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null,
            PiercingShotDelivery piercingShot = null, long attackGroup = 0, AttackPullSnapshot pull = null,
            IReadOnlyList<AttackKnockbackSnapshot> knockbacks = null)
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

            if (attackGroup < 0) throw new ArgumentOutOfRangeException(nameof(attackGroup));
            if (!succeeded && attackGroup != 0) throw new ArgumentException("취소된 공격은 공격 그룹을 갖지 않습니다.", nameof(attackGroup));
            if (hits.Any(h => h.Contacts != null))
            {
                if (traitActivations.Any(t => t.ChildAttack != null || t.TraitActivationKind == TraitActivationKind.DamageRedirected && t.Interception == null))
                    throw new ArgumentException("루트 공격에 잘못된 반응이 있습니다.", nameof(traitActivations));
                new AttackOutcomeSnapshot(hits, traitActivations.Where(t => t.Interception != null).ToArray()).ValidateScope(attackerId, attackGroup);
            }
            else if (traitActivations.Any(t => t.Interception != null)) throw new ArgumentException("반응에는 실제 접촉 결과가 필요합니다.", nameof(traitActivations));
            AttackGroup = attackGroup;
            Succeeded = succeeded;
            FailureReason = failureReason;
            AttackerId = attackerId;
            PrimaryTargetId = primaryTargetId;
            PiercingShot = piercingShot;
            Pull = pull;
            _knockbacks = knockbacks == null || knockbacks.Count == 0 ? null : Array.AsReadOnly(knockbacks.ToArray());
            TargetCell = targetCell;
            AttackDirection = attackDirection;
            ActionUseSettlement = actionCostSettlement;
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
            UnitActionUseSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations,
            AttackCellSelection cellSelection = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null,
            PiercingShotDelivery piercingShot = null, long attackGroup = 0)
        {
            ValidateSuccess(
                attackerId, attackDirection, actionCostSettlement, affectedCells, hits,
                currentStage, traitActivations, categoryActivations);
            PiercingShotDelivery.ValidateCorrespondence(piercingShot, attackerId, primaryTargetId,
                cellSelection?.PrimaryCell, cellSelection?.AdditionalCells ?? ReadOnlyMemory<CellCoord>.Empty,
                affectedCells, hits, hit => hit.TargetId, hit => hit.Role, hit => hit.Contacts);
            if (cellSelection == null && primaryTargetId.IsEmpty)
                throw new ArgumentException("대상 ID는 비어 있을 수 없습니다.", nameof(primaryTargetId));
            if (cellSelection != null)
            {
                if (!cellSelection.PrimaryCell.HasValue && !primaryTargetId.IsEmpty)
                    throw new ArgumentException("방향 공격에는 주 대상 ID를 만들 수 없습니다.", nameof(primaryTargetId));
                if (piercingShot == null)
                    AttackTargetRoleValidation.ValidateOrdinary(cellSelection.PrimaryCell, primaryTargetId, hits,
                        hit => hit.TargetId, hit => hit.Role, nameof(hits), hit => hit.Contacts);
            }

            return new AttackResult(
                true, AttackFailureReason.None, attackerId, primaryTargetId, attackDirection,
                actionCostSettlement, (CellCoord[])affectedCells.Clone(),
                (AttackHitResult[])hits.Clone(), currentStage, traitActivations,
                categoryActivations, cellSelection?.PrimaryCell, cellSelection, blockedContacts, piercingShot, attackGroup);
        }

        public static AttackResult SuccessForCell(
            EntityId attackerId,
            CellCoord targetCell,
            Rotation4 attackDirection,
            UnitActionUseSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations, IReadOnlyList<AttackBlockedContact> blockedContacts = null,
            PiercingShotDelivery piercingShot = null, long attackGroup = 0)
        {
            ValidateSuccess(
                attackerId, attackDirection, actionCostSettlement, affectedCells, hits,
                currentStage, traitActivations, categoryActivations);
            PiercingShotDelivery.ValidateCorrespondence(piercingShot, attackerId, EntityId.Empty,
                targetCell, ReadOnlyMemory<CellCoord>.Empty, affectedCells, hits, hit => hit.TargetId, hit => hit.Role, hit => hit.Contacts);
            var selection = piercingShot == null ? null :
                new AttackCellSelection(targetCell, new[] { targetCell }, Array.Empty<CellCoord>());

            return new AttackResult(
                true, AttackFailureReason.None, attackerId, EntityId.Empty, attackDirection,
                actionCostSettlement, (CellCoord[])affectedCells.Clone(),
                (AttackHitResult[])hits.Clone(), currentStage, traitActivations,
                categoryActivations, targetCell, selection, blockedContacts, piercingShot, attackGroup);
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
        /// <param name="reaction">반응 공격은 행동 사용 상태를 바꾸지 않는다. 일반 공격과 구분한다.</param>
        public static AttackResult FromFacts(EntityId attackerId, EntityId primaryTargetId,
            Rotation4 direction, UnitActionUseSettlementSnapshot cost, CellCoord[] affectedCells,
            AttackHitResult[] hits, AttackCellSelection selection,
            AttackFailureReason cancellationReason = AttackFailureReason.None,
            TraitActivationSnapshot[] traitActivations = null, CategoryActivationSnapshot[] categoryActivations = null,
            IReadOnlyList<AttackBlockedContact> blockedContacts = null, PiercingShotDelivery piercingShot = null, long attackGroup = 0,
            bool reaction = false, AttackPullSnapshot pull = null, IReadOnlyList<AttackKnockbackSnapshot> knockbacks = null)
        {
            if (reaction && cost != null && (cost.BeforeActionUse != cost.FinalActionUse))
                throw new ArgumentException("반응 공격은 행동 사용 상태를 바꿀 수 없습니다.", nameof(cost));
            traitActivations ??= Array.Empty<TraitActivationSnapshot>();
            categoryActivations ??= Array.Empty<CategoryActivationSnapshot>();
            if (cancellationReason != AttackFailureReason.None && piercingShot != null)
                throw new ArgumentException("취소된 공격에는 관통 사격 기록을 남길 수 없습니다.", nameof(piercingShot));
            if (cancellationReason != AttackFailureReason.None)
                throw new ArgumentException("거절된 공격은 명령 거절 결과로 게시해야 합니다.", nameof(cancellationReason));
            ValidateSuccess(attackerId, direction, cost, affectedCells, hits, null,
                traitActivations, categoryActivations, false, reaction || cancellationReason != AttackFailureReason.None);
            if (selection == null) throw new ArgumentNullException(nameof(selection));
            PiercingShotDelivery.ValidateCorrespondence(piercingShot, attackerId, primaryTargetId,
                selection.PrimaryCell, selection.AdditionalCells, affectedCells, hits, hit => hit.TargetId, hit => hit.Role, hit => hit.Contacts);
            if (!selection.PrimaryCell.HasValue && !primaryTargetId.IsEmpty)
                throw new ArgumentException("방향 공격에는 주 대상 ID를 만들 수 없습니다.");
            if (piercingShot == null)
                AttackTargetRoleValidation.ValidateOrdinary(selection.PrimaryCell, primaryTargetId, hits,
                    hit => hit.TargetId, hit => hit.Role, nameof(hits), hit => hit.Contacts);
            ValidateKnockbacks(knockbacks, hits);
            return new AttackResult(cancellationReason == AttackFailureReason.None, cancellationReason,
                attackerId, primaryTargetId, direction, cost, (CellCoord[])affectedCells.Clone(),
                (AttackHitResult[])hits.Clone(), null, traitActivations, categoryActivations, selection.PrimaryCell, selection, blockedContacts, piercingShot, attackGroup, pull,
                knockbacks);
        }

        /// <summary>
        /// 넉백 기록은 이 공격의 피격 대상마다 많아야 하나입니다. 경로 모양은 기록 생성자가 검사합니다.
        /// 대상은 원래 대상이거나 실제로 맞은 유닛입니다(희생이 대신 맞으면 수호자를 민다, 2026-11-02 결정).
        /// </summary>
        private static void ValidateKnockbacks(IReadOnlyList<AttackKnockbackSnapshot> knockbacks, AttackHitResult[] hits)
        {
            if (knockbacks == null) return;
            var recorded = new HashSet<EntityId>();
            foreach (var knockback in knockbacks)
            {
                if (knockback == null)
                    throw new ArgumentException("넉백 기록 목록에는 null 항목을 넣을 수 없습니다.", nameof(knockbacks));
                if (!recorded.Add(knockback.TargetId))
                    throw new ArgumentException("같은 대상의 넉백 기록은 하나만 둘 수 있습니다.", nameof(knockbacks));
                if (hits.All(hit => hit.TargetId != knockback.TargetId &&
                    (hit.Contacts == null || hit.Contacts.All(c => c.OriginalTargetId != knockback.TargetId))))
                    throw new ArgumentException("넉백 기록의 대상은 이 공격의 피격 대상이어야 합니다.", nameof(knockbacks));
            }
        }

        private static void ValidateSuccess(
            EntityId attackerId,
            Rotation4 attackDirection,
            UnitActionUseSettlementSnapshot actionCostSettlement,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            StageSnapshot currentStage,
            TraitActivationSnapshot[] traitActivations,
            CategoryActivationSnapshot[] categoryActivations, bool requireStage = true, bool allowUnchangedActionUse = false)
        {
            if (attackerId.IsEmpty)
                throw new ArgumentException("공격자 ID는 비어 있을 수 없습니다.", nameof(attackerId));
            if (Enum.IsDefined(typeof(Rotation4), attackDirection) == false)
                throw new ArgumentOutOfRangeException(nameof(attackDirection));
            if (actionCostSettlement == null)
                throw new ArgumentNullException(nameof(actionCostSettlement));
            if (actionCostSettlement.ActorId != attackerId)
                throw new ArgumentException("공격자와 행동 정산의 행위자가 다릅니다.", nameof(actionCostSettlement));
            if (!allowUnchangedActionUse && (!UnitActionUseRules.CanStartAttack(actionCostSettlement.BeforeActionUse) ||
                actionCostSettlement.FinalActionUse != UnitActionUse.Attacked))
                throw new ArgumentException("일반 공격은 공격을 시작할 수 있는 행동(미사용·이동함)을 공격으로 소비해야 합니다.", nameof(actionCostSettlement));
            if (affectedCells == null) throw new ArgumentNullException(nameof(affectedCells));
            if (hits == null) throw new ArgumentNullException(nameof(hits));
            if (requireStage && currentStage == null) throw new ArgumentNullException(nameof(currentStage));
            if (traitActivations == null) throw new ArgumentNullException(nameof(traitActivations));
            if (categoryActivations == null) throw new ArgumentNullException(nameof(categoryActivations));
        }
    }
}
