using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Map;

namespace ProjectL.Tests.Core
{
    public sealed class AttackRangeResultTests
    {
        [Test]
        public void Success_AssignsValuesAndCopiesInputArrays()
        {
            CellCoord[] rangeCells =
            {
                new CellCoord(1, 2),
                new CellCoord(2, 2),
            };
            EntityId[] inRangeTargetIds =
            {
                new EntityId(2),
                new EntityId(5),
            };

            AttackRangeResult result = AttackRangeResult.Success(
                new EntityId(1),
                rangeCells,
                inRangeTargetIds,
                canAffordAttack: false, attackAims: Array.Empty<AttackAim>());

            rangeCells[0] = new CellCoord(9, 9);
            inRangeTargetIds[0] = new EntityId(99);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.FailureReason, Is.EqualTo(AttackFailureReason.None));
            Assert.That(result.AttackerId, Is.EqualTo(new EntityId(1)));
            Assert.That(
                result.RangeCells.ToArray(),
                Is.EqualTo(new[] { new CellCoord(1, 2), new CellCoord(2, 2) }));
            Assert.That(
                result.InRangeTargetIds.ToArray(),
                Is.EqualTo(new[] { new EntityId(2), new EntityId(5) }));
            Assert.That(result.CanAffordAttack, Is.False);
        }

        [Test]
        public void Failure_AssignsReasonAndReturnsEmptyCollections()
        {
            AttackRangeResult result = AttackRangeResult.Failure(
                AttackFailureReason.MapOccupancyMismatch,
                new EntityId(1));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(
                result.FailureReason,
                Is.EqualTo(AttackFailureReason.MapOccupancyMismatch));
            Assert.That(result.AttackerId, Is.EqualTo(new EntityId(1)));
            Assert.That(result.RangeCells.Length, Is.Zero);
            Assert.That(result.InRangeTargetIds.Length, Is.Zero);
            Assert.That(result.CanAffordAttack, Is.False);
        }

        [Test]
        public void Success_RejectsNullRangeCells()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackRangeResult.Success(
                    new EntityId(1),
                    null,
                    Array.Empty<EntityId>(),
                    canAffordAttack: true, attackAims: Array.Empty<AttackAim>())).ParamName,
                Is.EqualTo("rangeCells"));
        }

        [Test]
        public void Success_RejectsNullInRangeTargetIds()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackRangeResult.Success(
                    new EntityId(1),
                    Array.Empty<CellCoord>(),
                    null,
                    canAffordAttack: true, attackAims: Array.Empty<AttackAim>())).ParamName,
                Is.EqualTo("inRangeTargetIds"));
        }
    }

    public sealed class AttackPreviewContractTests
    {
        [Test]
        public void CompleteSettlement_AllowsCalculatedZeroAndRejectsInconsistentFinal()
        {
            var valid = new ActionCostSettlementSnapshot(
                before: 3,
                spent: 2,
                refunded: 0,
                final: 1);

            AttackPreview preview = AttackPreview.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                valid,
                new[] { CellCoord.Zero },
                Array.Empty<AttackTargetPreview>(),
                Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Heavy));

            Assert.That(preview.ActionCostSettlement.Before, Is.EqualTo(3));
            Assert.That(preview.AttackCost, Is.EqualTo(2));
            Assert.That(preview.ActionCostRefunded, Is.Zero);
            Assert.That(preview.FinalActionCost, Is.EqualTo(1));
            Assert.That(preview.ProjectedCategory.Key, Is.EqualTo(CategoryKey.Heavy));

            Assert.Throws<ArgumentException>(() =>
                new ActionCostSettlementSnapshot(3, 2, 0, final: 2));
        }

        [Test]
        public void Success_WithPositiveAttackCost_PreservesAttackCost()
        {
            AttackPreview result = AttackPreview.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                new ActionCostSettlementSnapshot(2, 2, 0, 0),
                Array.Empty<CellCoord>(),
                Array.Empty<AttackTargetPreview>(),
                Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Base));

            Assert.That(result.AttackCost, Is.EqualTo(2));
        }

        [Test]
        public void Success_WithEmptyPrimaryTargetId_Throws()
        {
            Assert.That(
                Assert.Throws<ArgumentException>(() => AttackPreview.Success(
                    new EntityId(1),
                    EntityId.Empty,
                    Rotation4.R0,
                    new ActionCostSettlementSnapshot(2, 2, 0, 0),
                    Array.Empty<CellCoord>(),
                    Array.Empty<AttackTargetPreview>(),
                    Array.Empty<TraitStatusSnapshot>(),
                    CategorySnapshot.CreateStateless(CategoryKey.Base))).ParamName,
                Is.EqualTo("primaryTargetId"));
        }

        [Test]
        public void Failure_PreservesRequestedPrimaryTargetId()
        {
            EntityId attackerId = new EntityId(1);
            EntityId primaryTargetId = new EntityId(2);
            AttackPreview result = AttackPreview.Failure(
                AttackFailureReason.ActionCostInsufficient,
                attackerId,
                primaryTargetId);

            Assert.That(result.CanAttack, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(AttackFailureReason.ActionCostInsufficient));
            Assert.That(result.AttackerId, Is.EqualTo(attackerId));
            Assert.That(result.PrimaryTargetId, Is.EqualTo(primaryTargetId));
        }
    }

    public sealed class AttackCommandResultContractTests
    {
        [Test]
        public void AttackResultSuccess_WithZeroActionCostSpent_ThrowsOutOfRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => AttackResult.Success(
                    new EntityId(1),
                    new EntityId(2),
                    Rotation4.R0,
                    new ActionCostSettlementSnapshot(1, 0, 0, 1),
                    Array.Empty<CellCoord>(),
                    Array.Empty<AttackHitResult>(),
                    CreateStageSnapshot(),
                    Array.Empty<TraitActivationSnapshot>(),
                    Array.Empty<CategoryActivationSnapshot>())).ParamName,
                Is.EqualTo("actionCostSettlement"));
        }

        [Test]
        public void AttackResultSuccess_WithUndefinedDirection_ThrowsOutOfRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => AttackResult.Success(
                    new EntityId(1),
                    new EntityId(2),
                    (Rotation4)99,
                    new ActionCostSettlementSnapshot(1, 1, 0, 0),
                    Array.Empty<CellCoord>(),
                    Array.Empty<AttackHitResult>(),
                    CreateStageSnapshot(),
                    Array.Empty<TraitActivationSnapshot>(),
                    Array.Empty<CategoryActivationSnapshot>())).ParamName,
                Is.EqualTo("attackDirection"));
        }

        [Test]
        public void AttackResultSuccess_WithNullAffectedCells_ThrowsArgumentNull()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackResult.Success(
                    new EntityId(1),
                    new EntityId(2),
                    Rotation4.R0,
                    new ActionCostSettlementSnapshot(1, 1, 0, 0),
                    null,
                    Array.Empty<AttackHitResult>(),
                    CreateStageSnapshot(),
                    Array.Empty<TraitActivationSnapshot>(),
                    Array.Empty<CategoryActivationSnapshot>())).ParamName,
                Is.EqualTo("affectedCells"));
        }

        [Test]
        public void AttackResultSuccess_WithNullHits_ThrowsArgumentNull()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackResult.Success(
                    new EntityId(1),
                    new EntityId(2),
                    Rotation4.R0,
                    new ActionCostSettlementSnapshot(1, 1, 0, 0),
                    Array.Empty<CellCoord>(),
                    null,
                    CreateStageSnapshot(),
                    Array.Empty<TraitActivationSnapshot>(),
                    Array.Empty<CategoryActivationSnapshot>())).ParamName,
                Is.EqualTo("hits"));
        }

        [Test]
        public void AttackExecutionResultSuccess_WithZeroActionCostSpent_ThrowsOutOfRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => AttackExecutionResult.Success(
                    new EntityId(1),
                    new EntityId(2),
                    Rotation4.R0,
                    actionCostSpent: 0,
                    Array.Empty<CellCoord>(),
                    Array.Empty<AttackHitResult>())).ParamName,
                Is.EqualTo("actionCostSpent"));
        }

        [Test]
        public void AttackExecutionResultSuccess_WithUndefinedDirection_ThrowsOutOfRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => AttackExecutionResult.Success(
                    new EntityId(1),
                    new EntityId(2),
                    (Rotation4)99,
                    actionCostSpent: 1,
                    Array.Empty<CellCoord>(),
                    Array.Empty<AttackHitResult>())).ParamName,
                Is.EqualTo("attackDirection"));
        }

        [Test]
        public void AttackExecutionResultSuccess_WithNullAffectedCells_ThrowsArgumentNull()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackExecutionResult.Success(
                    new EntityId(1),
                    new EntityId(2),
                    Rotation4.R0,
                    actionCostSpent: 1,
                    null,
                    Array.Empty<AttackHitResult>())).ParamName,
                Is.EqualTo("affectedCells"));
        }

        [Test]
        public void AttackExecutionResultSuccess_WithNullHits_ThrowsArgumentNull()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackExecutionResult.Success(
                    new EntityId(1),
                    new EntityId(2),
                    Rotation4.R0,
                    actionCostSpent: 1,
                    Array.Empty<CellCoord>(),
                    null)).ParamName,
                Is.EqualTo("hits"));
        }

        [Test]
        public void AttackHitResult_WithNegativeTargetHealthBefore_ThrowsOutOfRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => new AttackHitResult(
                    new EntityId(2),
                    targetHealthBefore: -1,
                    targetHealthAfter: 0)).ParamName,
                Is.EqualTo("targetHealthBefore"));
        }

        [Test]
        public void AttackHitResult_WithNegativeTargetHealthAfter_ThrowsOutOfRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => new AttackHitResult(
                    new EntityId(2),
                    targetHealthBefore: 0,
                    targetHealthAfter: -1)).ParamName,
                Is.EqualTo("targetHealthAfter"));
        }

        [Test]
        public void AttackHitResult_WithHealthAfterGreaterThanBefore_ThrowsOutOfRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => new AttackHitResult(
                    new EntityId(2),
                    targetHealthBefore: 3,
                    targetHealthAfter: 4)).ParamName,
                Is.EqualTo("targetHealthAfter"));
        }

        private static StageSnapshot CreateStageSnapshot()
        {
            return new StageSnapshot(
                1,
                1,
                StagePhase.Combat,
                Array.Empty<global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.EntitySnapshot>(),
                Array.Empty<ReserveSlotSnapshot>());
        }
    }
}
