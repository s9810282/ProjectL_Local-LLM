using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Categories.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public readonly struct AttackExecutionResult
    {
        public bool Succeeded { get; }
        public AttackFailureReason FailureReason { get; }
        public EntityId AttackerId { get; }
        public EntityId PrimaryTargetId { get; }
        public Rotation4 AttackDirection { get; }
        public int ActionCostSpent { get; }
        public ReadOnlyMemory<CellCoord> AffectedCells { get; }
        public ReadOnlyMemory<AttackHitResult> Hits { get; }
        /// <summary>Cell 조준으로 선택된 중심 Cell입니다. Entity 조준에서는 null입니다.</summary>
        public CellCoord? TargetCell { get; }

        private AttackExecutionResult(
            bool succeeded,
            AttackFailureReason failureReason,
            EntityId attackerId,
            EntityId primaryTargetId,
            Rotation4 attackDirection,
            int actionCostSpent,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            CellCoord? targetCell)
        {
            Succeeded = succeeded;
            FailureReason = failureReason;
            AttackerId = attackerId;
            PrimaryTargetId = primaryTargetId;
            AttackDirection = attackDirection;
            ActionCostSpent = actionCostSpent;
            AffectedCells = affectedCells;
            Hits = hits;
            TargetCell = targetCell;
        }

        public static AttackExecutionResult Success(
            EntityId attackerId,
            EntityId primaryTargetId,
            Rotation4 attackDirection,
            int actionCostSpent,
            CellCoord[] affectedCells,
            AttackHitResult[] hits)
        {
            return Success(attackerId, primaryTargetId, attackDirection,
                actionCostSpent, affectedCells, hits, targetCell: null);
        }

        public static AttackExecutionResult Success(
            EntityId attackerId,
            EntityId primaryTargetId,
            Rotation4 attackDirection,
            int actionCostSpent,
            CellCoord[] affectedCells,
            AttackHitResult[] hits,
            CellCoord? targetCell)
        {
            if (attackerId.IsEmpty)
                throw new ArgumentException("공격자 ID는 비어 있을 수 없습니다.", nameof(attackerId));
            if (primaryTargetId.IsEmpty && targetCell == null)
                throw new ArgumentException("대상 ID는 비어 있을 수 없습니다.", nameof(primaryTargetId));
            if (Enum.IsDefined(typeof(Rotation4), attackDirection) == false)
                throw new ArgumentOutOfRangeException(nameof(attackDirection), "공격 방향은 0, 90, 180, 270 중 하나여야 합니다.");
            if (actionCostSpent <= 0)
                throw new ArgumentOutOfRangeException(nameof(actionCostSpent), "소모 행동력은 0보다 커야 합니다.");
            if (affectedCells == null)
                throw new ArgumentNullException(nameof(affectedCells), "영향 셀은 null일 수 없습니다.");
            if (hits == null)
                throw new ArgumentNullException(nameof(hits), "히트 결과는 null일 수 없습니다.");

            CellCoord[] copiedCells = new CellCoord[affectedCells.Length];
            Array.Copy(affectedCells, copiedCells, affectedCells.Length);

            AttackHitResult[] copiedHits = new AttackHitResult[hits.Length];
            Array.Copy(hits, copiedHits, hits.Length);

            return new AttackExecutionResult(
                succeeded: true,
                failureReason: AttackFailureReason.None,
                attackerId: attackerId,
                primaryTargetId: primaryTargetId,
                attackDirection: attackDirection,
                actionCostSpent: actionCostSpent,
                affectedCells: copiedCells,
                hits: copiedHits,
                targetCell: targetCell);
        }

        public static AttackExecutionResult Failure(
            AttackFailureReason failureReason,
            EntityId attackerId,
            EntityId primaryTargetId)
        {
            if (failureReason == AttackFailureReason.None)
                throw new ArgumentException("실패 사유는 None일 수 없습니다.", nameof(failureReason));
            if (attackerId.IsEmpty)
                throw new ArgumentException("공격자 ID는 비어 있을 수 없습니다.", nameof(attackerId));
            // Cell 조준 실패 결과도 Empty 주요 대상을 유지한다.

            return new AttackExecutionResult(
                succeeded: false,
                failureReason: failureReason,
                attackerId: attackerId,
                primaryTargetId: primaryTargetId,
                attackDirection: Rotation4.R0,
                actionCostSpent: 0,
                affectedCells: Array.Empty<CellCoord>(),
                hits: Array.Empty<AttackHitResult>(),
                targetCell: null);
        }
    }
}
