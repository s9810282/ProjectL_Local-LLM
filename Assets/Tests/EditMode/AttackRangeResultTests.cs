using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
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
                new EntityId(1),                rangeCells,                inRangeTargetIds,attackAims: Array.Empty<AttackAim>());

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
        }

        [Test]
        public void Success_RejectsNullRangeCells()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackRangeResult.Success(
                    new EntityId(1),                    null,                    Array.Empty<EntityId>(),attackAims: Array.Empty<AttackAim>())).ParamName,
                Is.EqualTo("rangeCells"));
        }

        [Test]
        public void Success_RejectsNullInRangeTargetIds()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackRangeResult.Success(
                    new EntityId(1),                    Array.Empty<CellCoord>(),                    null,attackAims: Array.Empty<AttackAim>())).ParamName,
                Is.EqualTo("inRangeTargetIds"));
        }
    }

    public sealed class AttackPreviewContractTests
    {
        [Test]
        public void CompleteSettlement_PreservesActionUseAndCategory()
        {
            var valid = new UnitActionUseSettlementSnapshot(
                new EntityId(1),beforeActionUse: UnitActionUse.Available,finalActionUse: UnitActionUse.Attacked);

            AttackPreview preview = AttackPreview.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                valid,
                new[] { CellCoord.Zero },
                Array.Empty<AttackTargetPreview>(),
                Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Heavy));

            Assert.That(preview.ActionUseSettlement.ActorId, Is.EqualTo(new EntityId(1)));
            Assert.That(preview.ActionUseSettlement.BeforeActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(preview.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(preview.ProjectedCategory.Key, Is.EqualTo(CategoryKey.Heavy));

        }


        [Test]
        public void Success_WithEmptyPrimaryTargetId_Throws()
        {
            Assert.That(
                Assert.Throws<ArgumentException>(() => AttackPreview.Success(
                    new EntityId(1),
                    EntityId.Empty,
                    Rotation4.R0,
                    new UnitActionUseSettlementSnapshot(new EntityId(1),                        UnitActionUse.Available,UnitActionUse.Attacked),
                    Array.Empty<CellCoord>(),
                    Array.Empty<AttackTargetPreview>(),
                    Array.Empty<TraitStatusSnapshot>(),
                    CategorySnapshot.CreateStateless(CategoryKey.Main))).ParamName,
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
        public void AttackResultSuccess_WithUndefinedDirection_ThrowsOutOfRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => AttackResult.Success(
                    new EntityId(1),
                    new EntityId(2),
                    (Rotation4)99,
                    new UnitActionUseSettlementSnapshot(new EntityId(1),                        UnitActionUse.Available,UnitActionUse.Attacked),
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
                    new UnitActionUseSettlementSnapshot(new EntityId(1),                        UnitActionUse.Available,UnitActionUse.Attacked),
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
                    new UnitActionUseSettlementSnapshot(new EntityId(1),                        UnitActionUse.Available,UnitActionUse.Attacked),
                    Array.Empty<CellCoord>(),
                    null,
                    CreateStageSnapshot(),
                    Array.Empty<TraitActivationSnapshot>(),
                    Array.Empty<CategoryActivationSnapshot>())).ParamName,
                Is.EqualTo("hits"));
        }


        [Test]
        public void AttackExecutionResultSuccess_WithUndefinedDirection_ThrowsOutOfRange()
        {
            Assert.That(
                Assert.Throws<ArgumentOutOfRangeException>(() => AttackExecutionResult.Success(
                    new EntityId(1),                    new EntityId(2),                    (Rotation4)99,                    Array.Empty<CellCoord>(),                    Array.Empty<AttackHitResult>())).ParamName,
                Is.EqualTo("attackDirection"));
        }

        [Test]
        public void AttackExecutionResultSuccess_WithNullAffectedCells_ThrowsArgumentNull()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackExecutionResult.Success(
                    new EntityId(1),                    new EntityId(2),                    Rotation4.R0,                    null,                    Array.Empty<AttackHitResult>())).ParamName,
                Is.EqualTo("affectedCells"));
        }

        [Test]
        public void AttackExecutionResultSuccess_WithNullHits_ThrowsArgumentNull()
        {
            Assert.That(
                Assert.Throws<ArgumentNullException>(() => AttackExecutionResult.Success(
                    new EntityId(1),                    new EntityId(2),                    Rotation4.R0,                    Array.Empty<CellCoord>(),                    null)).ParamName,
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
