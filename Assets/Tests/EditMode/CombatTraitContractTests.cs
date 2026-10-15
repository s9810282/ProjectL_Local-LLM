using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;

namespace ProjectL.Tests.Core
{
    public sealed class CombatTraitContractTests
    {
        [Test]
        public void ExistingUnitSpecConstructor_UsesEmptyTraits()
        {
            UnitSpec spec = CreateLegacyCompatibleSpec();

            Assert.That(spec.Traits.Length, Is.Zero);
        }

        [Test]
        public void ExistingPreTimelineUnitSpecConstructor_UsesEmptyTraits()
        {
            UnitSpec spec = CreatePreTimelineSpec();

            Assert.That(spec.Traits.Length, Is.Zero);
        }

        [Test]
        public void UnitSpec_CopiesTraitList()
        {
            TraitSpec originalFocus = new FocusTraitSpec(damagePerStack: 2, maxStacks: 3);
            TraitSpec[] traits =
            {
                originalFocus,
                new MoveAttackDiscountTraitSpec(discountAmount: 1, minimumAttackCost: 1),
            };

            UnitSpec spec = CreateSpec(traits);
            traits[0] = new FocusTraitSpec(damagePerStack: 99, maxStacks: 99);

            Assert.That(spec.Traits.Length, Is.EqualTo(2));
            Assert.That(spec.Traits.Span[0], Is.SameAs(originalFocus));
            Assert.That(spec.Traits.Span[0].TraitKey, Is.EqualTo(TraitKey.Focus));
        }

        [Test]
        public void UnitSpec_RejectsNullTraitList()
        {
            ArgumentNullException exception =
                Assert.Throws<ArgumentNullException>(() => CreateSpec(null));

            Assert.That(exception.ParamName, Is.EqualTo("traits"));
        }

        [Test]
        public void UnitSpec_RejectsNullTraitEntry()
        {
            TraitSpec[] traits =
            {
                new FocusTraitSpec(damagePerStack: 2, maxStacks: 3),
                null,
            };

            ArgumentException exception =
                Assert.Throws<ArgumentException>(() => CreateSpec(traits));

            Assert.That(exception.ParamName, Is.EqualTo("traits"));
        }

        [Test]
        public void UnitSpec_RejectsDuplicateTraitKeys()
        {
            TraitSpec[] traits =
            {
                new FocusTraitSpec(damagePerStack: 2, maxStacks: 3),
                new FocusTraitSpec(damagePerStack: 4, maxStacks: 5),
            };

            ArgumentException exception =
                Assert.Throws<ArgumentException>(() => CreateSpec(traits));

            Assert.That(exception.ParamName, Is.EqualTo("traits"));
        }

        [Test]
        public void UnitCatalog_TransfersTraitSpecConfigurationToDefinition()
        {
            UnitSpec spec = CreateSpec(new TraitSpec[]
            {
                new FocusTraitSpec(damagePerStack: 2, maxStacks: 3),
            });

            UnitCatalog catalog = new UnitCatalog(
                new[] { spec },
                TraitRegistry.CreateDefault());

            UnitDefinition definition = catalog.GetRequired(EntityKey.Unit_Akimbo);

            Assert.That(definition.TraitDefinitions.Length, Is.EqualTo(1));
            FocusTraitDefinition focusDefinition =
                definition.TraitDefinitions.Span[0] as FocusTraitDefinition;
            Assert.That(focusDefinition, Is.Not.Null);
            Assert.That(focusDefinition.DamagePerStack, Is.EqualTo(2));
            Assert.That(focusDefinition.MaxStacks, Is.EqualTo(3));
        }

        [Test]
        public void UnitCatalog_OrdersTraitDefinitionsByTraitKey()
        {
            UnitSpec spec = CreateSpec(new TraitSpec[]
            {
                new MoveAttackDiscountTraitSpec(discountAmount: 2, minimumAttackCost: 1),
                new FocusTraitSpec(damagePerStack: 3, maxStacks: 2),
            });

            UnitCatalog catalog = new UnitCatalog(
                new[] { spec },
                TraitRegistry.CreateDefault());

            ReadOnlySpan<TraitDefinition> definitions =
                catalog.GetRequired(EntityKey.Unit_Akimbo).TraitDefinitions.Span;

            Assert.That(definitions.Length, Is.EqualTo(2));
            Assert.That(definitions[0].TraitKey, Is.EqualTo(TraitKey.Focus));
            Assert.That(definitions[1].TraitKey, Is.EqualTo(TraitKey.MoveAttackDiscount));
        }

        [Test]
        public void UnitCatalog_RejectsFocusMaximumBonusDamageOverflow()
        {
            UnitSpec spec = CreateSpec(new TraitSpec[]
            {
                new FocusTraitSpec(damagePerStack: int.MaxValue, maxStacks: 2),
            });

            Assert.Throws<ArgumentException>(() =>
                new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault()));
        }

        [Test]
        public void TraitContractOutputs_CopyInputArrays()
        {
            TraitStatusSnapshot expectedStatus =
                new FocusTraitStatusSnapshot(activeStacks: 1, gainScheduled: false, movedThisTurn: false);
            TraitActivationSnapshot expectedActivation = new TraitActivationSnapshot(
                new EntityId(1),
                targetId: null,
                TraitKey.Focus,
                TraitActivationKind.FocusStackApplied,
                magnitude: 1);
            TraitStatusSnapshot[] unitStatuses = { expectedStatus };
            TraitStatusSnapshot[] moveStatuses = { expectedStatus };
            TraitStatusSnapshot[] attackStatuses = { expectedStatus };
            TraitActivationSnapshot[] movementActivations = { expectedActivation };
            TraitActivationSnapshot[] attackActivations = { expectedActivation };
            TraitActivationSnapshot[] turnActivations = { expectedActivation };
            StageSnapshot stage = CreateStageSnapshot();

            UnitStatsSnapshot unitStats = CreateUnitStatsSnapshot(unitStatuses);
            MovePreview movePreview = MovePreview.Success(
                CreateMovementDestination(),
                moveStatuses,
                CategorySnapshot.CreateStateless(CategoryKey.Base));
            AttackPreview attackPreview = AttackPreview.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                new ActionCostSettlementSnapshot(2, 2, 0, 0),
                Array.Empty<CellCoord>(),
                Array.Empty<AttackTargetPreview>(),
                attackStatuses,
                CategorySnapshot.CreateStateless(CategoryKey.Base));
            MovementResult movementResult = MovementResult.Success(
                new EntityId(1),
                CellCoord.Zero,
                new CellCoord(1, 0),
                new[] { new CellCoord(1, 0) },
                stepDistance: 1,
                distanceFromCheckpoint: 1,
                totalMoveActionCost: 1,
                actionCostDelta: 1,
                stage,
                movementActivations,
                Array.Empty<CategoryActivationSnapshot>());
            AttackResult attackResult = AttackResult.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                new ActionCostSettlementSnapshot(2, 2, 0, 0),
                Array.Empty<CellCoord>(),
                Array.Empty<AttackHitResult>(),
                stage,
                attackActivations,
                Array.Empty<CategoryActivationSnapshot>());
            CombatTurnResult turnResult = new CombatTurnResult(
                succeeded: true,
                CombatTurnFailureReason.None,
                new EntityId(1),
                stage,
                turnActivations);

            unitStatuses[0] = new MoveAttackDiscountTraitStatusSnapshot(false, false);
            moveStatuses[0] = new MoveAttackDiscountTraitStatusSnapshot(false, false);
            attackStatuses[0] = new MoveAttackDiscountTraitStatusSnapshot(false, false);
            movementActivations[0] = CreateDiscountActivation();
            attackActivations[0] = CreateDiscountActivation();
            turnActivations[0] = CreateDiscountActivation();

            Assert.That(unitStats.TraitStatuses.Span[0], Is.SameAs(expectedStatus));
            Assert.That(movePreview.ProjectedTraitStatuses.Span[0], Is.SameAs(expectedStatus));
            Assert.That(attackPreview.ProjectedTraitStatuses.Span[0], Is.SameAs(expectedStatus));
            Assert.That(movementResult.TraitActivations.Span[0], Is.SameAs(expectedActivation));
            Assert.That(attackResult.TraitActivations.Span[0], Is.SameAs(expectedActivation));
            Assert.That(turnResult.TraitActivations.Span[0], Is.SameAs(expectedActivation));
        }

        [Test]
        public void ExistingTraitContractPaths_UseEmptyTraitArrays()
        {
            StageSnapshot stage = CreateStageSnapshot();

            UnitStatsSnapshot unitStats = CreateUnitStatsSnapshot();
            MovePreview movePreview = MovePreview.Success(
                CreateMovementDestination(),
                Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Base));
            AttackPreview attackPreview = AttackPreview.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                new ActionCostSettlementSnapshot(2, 2, 0, 0),
                Array.Empty<CellCoord>(),
                Array.Empty<AttackTargetPreview>(),
                Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Base));
            MovementResult movementResult = MovementResult.Success(
                new EntityId(1),
                CellCoord.Zero,
                new CellCoord(1, 0),
                new[] { new CellCoord(1, 0) },
                stepDistance: 1,
                distanceFromCheckpoint: 1,
                totalMoveActionCost: 1,
                actionCostDelta: 1,
                stage,
                Array.Empty<TraitActivationSnapshot>(),
                Array.Empty<CategoryActivationSnapshot>());
            AttackResult attackResult = AttackResult.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                new ActionCostSettlementSnapshot(2, 2, 0, 0),
                Array.Empty<CellCoord>(),
                Array.Empty<AttackHitResult>(),
                stage,
                Array.Empty<TraitActivationSnapshot>(),
                Array.Empty<CategoryActivationSnapshot>());
            CombatTurnResult turnResult = new CombatTurnResult(
                succeeded: true,
                CombatTurnFailureReason.None,
                new EntityId(1),
                stage);

            Assert.That(unitStats.TraitStatuses.Length, Is.Zero);
            Assert.That(movePreview.ProjectedTraitStatuses.Length, Is.Zero);
            Assert.That(attackPreview.ProjectedTraitStatuses.Length, Is.Zero);
            Assert.That(movementResult.TraitActivations.Length, Is.Zero);
            Assert.That(attackResult.TraitActivations.Length, Is.Zero);
            Assert.That(turnResult.TraitActivations.Length, Is.Zero);
        }

        [Test]
        public void FailedTraitContractPaths_UseEmptyTraitArrays()
        {
            StageSnapshot stage = CreateStageSnapshot();

            MovePreview movePreview = MovePreview.Failure(MovementFailureReason.ActionCostInsufficient);
            AttackPreview attackPreview = AttackPreview.Failure(
                AttackFailureReason.ActionCostInsufficient,
                new EntityId(1),
                new EntityId(2));
            MovementResult movementResult = MovementResult.Failure(
                MovementFailureReason.ActionCostInsufficient,
                new EntityId(1),
                stage);
            AttackResult attackResult = AttackResult.Failure(
                AttackFailureReason.ActionCostInsufficient,
                new EntityId(1),
                new EntityId(2),
                stage);
            CombatTurnResult turnResult = new CombatTurnResult(
                succeeded: false,
                CombatTurnFailureReason.NoActiveUnit,
                EntityId.Empty,
                stage);

            Assert.That(movePreview.ProjectedTraitStatuses.Length, Is.Zero);
            Assert.That(attackPreview.ProjectedTraitStatuses.Length, Is.Zero);
            Assert.That(movementResult.TraitActivations.Length, Is.Zero);
            Assert.That(attackResult.TraitActivations.Length, Is.Zero);
            Assert.That(turnResult.TraitActivations.Length, Is.Zero);
        }

        [Test]
        public void MovementExecutionResult_RejectsNullTraitActivationArray()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MovementExecutionResult.Success(
                    new EntityId(1),
                    CellCoord.Zero,
                    CreateMovementDestination(),
                    null));
        }

        [Test]
        public void MovementExecutionResult_RejectsNullTraitActivationEntry()
        {
            Assert.Throws<ArgumentException>(() =>
                MovementExecutionResult.Success(
                    new EntityId(1),
                    CellCoord.Zero,
                    CreateMovementDestination(),
                    new TraitActivationSnapshot[] { null }));
        }


        [Test]
        public void PierceRayDetails_AreCopiedAndPreserveIndividuallyRoundedStages()
        {
            var previews = new[]
            {
                new PierceRayHitPreview(0, 1, 5, 9, 10, 5, 5, 0, 5),
                new PierceRayHitPreview(1, 0, 5, 9, 10, 5, 5, 0, 5),
            };
            var preview = new AttackTargetPreview(new EntityId(2), 10, 4,
                new[] { new CellCoord(2, 3) }, 10, 9, 10, 0, previews);
            previews[0] = default;
            Assert.That(preview.RayHits.Span[0].EncounterIndex, Is.EqualTo(1));
            Assert.That(preview.ExpectedDamage, Is.EqualTo(10));
            Assert.That(preview.TargetHealthAfter, Is.Zero);
            Assert.That(new AttackTargetPreview(new EntityId(2), 1, 4, Array.Empty<CellCoord>()).RayHits.IsEmpty, Is.True);
        }

        [Test]
        public void PierceRayDetails_RejectDuplicateRaysInvalidDefaultsAndMismatchedAggregates()
        {
            var a = new PierceRayHitPreview(0, 0, 1, 1, 1, 1, 1, 0, 1);
            var b = new PierceRayHitPreview(1, 0, 1, 1, 1, 1, 1, 0, 1);
            AttackTargetPreview Preview(PierceRayHitPreview[] hits, int expected = 2) =>
                new AttackTargetPreview(new EntityId(2), expected, 4, Array.Empty<CellCoord>(), 2, 1, 1, 0, hits);
            Assert.Throws<ArgumentException>(() => Preview(new[] { a }));
            Assert.Throws<ArgumentException>(() => Preview(new[] { a, a }));
            Assert.Throws<ArgumentException>(() => Preview(new[] { a, default(PierceRayHitPreview) }));
            Assert.Throws<ArgumentException>(() => Preview(new[] { a, b }, 1));
            Assert.Throws<ArgumentException>(() => Preview(new[] { a,
                new PierceRayHitPreview(1, 0, 1, 9, 10, 1, 1, 0, 1) }));
        }

        [Test]
        public void PierceRayRecords_RejectInvalidIndicesValuesAndSumOverflow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PierceRayHitPreview(-1, 0, 1, 1, 1, 1, 1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PierceRayHitPreview(0, -1, 1, 1, 1, 1, 1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PierceRayHitPreview(0, 0, 1, 1, 0, 1, 1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PierceRayHitPreview(0, 0, -1, 1, 1, 1, 1, 0, 1));
            var large = new PierceRayHitPreview(0, 0, int.MaxValue, 1, 1, int.MaxValue, int.MaxValue, 0, int.MaxValue);
            var extra = new PierceRayHitPreview(1, 0, 1, 1, 1, 1, 1, 0, 1);
            Assert.Throws<OverflowException>(() => new AttackTargetPreview(new EntityId(2), int.MaxValue,
                4, Array.Empty<CellCoord>(), int.MaxValue, 1, 1, 0, new[] { large, extra }));
        }

        [Test]
        public void TraitFreeDefinition_CurrentBattleFactsHaveNoTraitStatuses()
        {
            var registry = TraitRegistry.CreateDefault();
            var definition = new UnitCatalog(new[] { CreateSpec(Array.Empty<TraitSpec>()) }, registry)
                .GetRequired(EntityKey.Unit_Akimbo);
            var character = BattleCharacterCalculator.Create(new EntityId(1), definition,
                Owner.Enemy, null, null, 0, null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, registry);
            Assert.That(definition.TraitDefinitions.Length, Is.Zero);
            Assert.That(character.Stats.TraitStatuses.Length, Is.Zero);
        }

        private static UnitSpec CreateSpec(IReadOnlyList<TraitSpec> traits)
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                1,
                1,
                new[] { CellCoord.Zero },
                10,
                MoveMethod.LiftAndPlace,
                5,
                1,
                4,
                3,
                8,
                2,
                30,
                35,
                traits);
        }

        private static UnitStatsSnapshot CreateUnitStatsSnapshot(
            TraitStatusSnapshot[] traitStatuses = null)
        {
            if (traitStatuses == null)
            {
                return new UnitStatsSnapshot(
                    MoveMethod.LiftAndPlace,
                    moveRange: 5,
                    moveCost: 1,
                    appointRange: 4,
                    attackPower: 3,
                    physicalDefense: 0,
                    maxActionCost: 8,
                    attackCost: 2,
                    remainingMoveRange: 2,
                    currentActionCost: 3,
                    priority: 30,
                    delay: 35,
                    traitStatuses: Array.Empty<TraitStatusSnapshot>(),
                    category: CategorySnapshot.CreateStateless(CategoryKey.Base));
            }

            return new UnitStatsSnapshot(
                MoveMethod.LiftAndPlace,
                moveRange: 5,
                moveCost: 1,
                appointRange: 4,
                attackPower: 3,
                physicalDefense: 0,
                maxActionCost: 8,
                attackCost: 2,
                remainingMoveRange: 2,
                currentActionCost: 3,
                priority: 30,
                delay: 35,
                traitStatuses,
                CategorySnapshot.CreateStateless(CategoryKey.Base));
        }

        private static MovementDestination CreateMovementDestination()
        {
            return new MovementDestination(
                new CellCoord(1, 0),
                new[] { new CellCoord(1, 0) },
                distance: 1,
                actionCost: 1);
        }

        private static StageSnapshot CreateStageSnapshot()
        {
            return new StageSnapshot(
                mapWidth: 2,
                mapHeight: 2,
                StagePhase.Deployment,
                Array.Empty<EntitySnapshot>(),
                Array.Empty<ReserveSlotSnapshot>());
        }

        private static TraitActivationSnapshot CreateDiscountActivation()
        {
            return new TraitActivationSnapshot(
                new EntityId(1),
                targetId: null,
                TraitKey.MoveAttackDiscount,
                TraitActivationKind.AttackDiscountArmed,
                magnitude: 1);
        }

        private static UnitSpec CreateLegacyCompatibleSpec()
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                1,
                1,
                new[] { CellCoord.Zero },
                10,
                MoveMethod.LiftAndPlace,
                5,
                1,
                4,
                3,
                8,
                2,
                30,
                35);
        }

        private static UnitSpec CreatePreTimelineSpec()
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                1,
                1,
                new[] { CellCoord.Zero },
                10,
                MoveMethod.LiftAndPlace,
                5,
                1,
                4,
                3,
                8,
                2);
        }
    }
}
