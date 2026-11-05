using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
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
                new RoarTraitSpec(2),
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
                new RoarTraitSpec(2),
                new FocusTraitSpec(damagePerStack: 3, maxStacks: 2),
            });

            UnitCatalog catalog = new UnitCatalog(
                new[] { spec },
                TraitRegistry.CreateDefault());

            ReadOnlySpan<TraitDefinition> definitions =
                catalog.GetRequired(EntityKey.Unit_Akimbo).TraitDefinitions.Span;

            Assert.That(definitions.Length, Is.EqualTo(2));
            Assert.That(definitions[0].TraitKey, Is.EqualTo(TraitKey.Focus));
            Assert.That(definitions[1].TraitKey, Is.EqualTo(TraitKey.Roar));
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
                CategorySnapshot.CreateStateless(CategoryKey.Main));
            AttackPreview attackPreview = AttackPreview.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                new UnitActionUseSettlementSnapshot(new EntityId(1),UnitActionUse.Available,UnitActionUse.Attacked),
                Array.Empty<CellCoord>(),
                Array.Empty<AttackTargetPreview>(),
                attackStatuses,
                CategorySnapshot.CreateStateless(CategoryKey.Main));
            MovementResult movementResult = MovementResult.Success(
                new EntityId(1),                CellCoord.Zero,                new CellCoord(1, 0),                new[] { new CellCoord(1, 0) },                stepDistance: 1,                distanceFromCheckpoint: 1,                stage,                movementActivations,                Array.Empty<CategoryActivationSnapshot>());
            AttackResult attackResult = AttackResult.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                new UnitActionUseSettlementSnapshot(new EntityId(1),UnitActionUse.Available,UnitActionUse.Attacked),
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

            unitStatuses[0] = new RoarTraitStatusSnapshot(0, 2);
            moveStatuses[0] = new RoarTraitStatusSnapshot(0, 2);
            attackStatuses[0] = new RoarTraitStatusSnapshot(0, 2);
            movementActivations[0] = CreateRoarActivation();
            attackActivations[0] = CreateRoarActivation();
            turnActivations[0] = CreateRoarActivation();

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
                CategorySnapshot.CreateStateless(CategoryKey.Main));
            AttackPreview attackPreview = AttackPreview.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                new UnitActionUseSettlementSnapshot(new EntityId(1),UnitActionUse.Available,UnitActionUse.Attacked),
                Array.Empty<CellCoord>(),
                Array.Empty<AttackTargetPreview>(),
                Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Main));
            MovementResult movementResult = MovementResult.Success(
                new EntityId(1),                CellCoord.Zero,                new CellCoord(1, 0),                new[] { new CellCoord(1, 0) },                stepDistance: 1,                distanceFromCheckpoint: 1,                stage,                Array.Empty<TraitActivationSnapshot>(),                Array.Empty<CategoryActivationSnapshot>());
            AttackResult attackResult = AttackResult.Success(
                new EntityId(1),
                new EntityId(2),
                Rotation4.R0,
                new UnitActionUseSettlementSnapshot(new EntityId(1),UnitActionUse.Available,UnitActionUse.Attacked),
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
        public void TraitFreeDefinition_CurrentBattleFactsHaveNoTraitStatuses()
        {
            var registry = TraitRegistry.CreateDefault();
            var definition = new UnitCatalog(new[] { CreateSpec(Array.Empty<TraitSpec>()) }, registry)
                .GetRequired(EntityKey.Unit_Akimbo);
            var character = BattleCharacterCalculator.CreateEnemy(new EntityId(1), definition,
                1, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, registry);
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
                4,
                3,
                30,
                traits);
        }

        private static UnitStatsSnapshot CreateUnitStatsSnapshot(
            TraitStatusSnapshot[] traitStatuses = null)
        {
            if (traitStatuses == null)
            {
                return new UnitStatsSnapshot(
                    MoveMethod.LiftAndPlace,                    moveRange: 5,                    appointRange: 4,                    attackPower: 3,                    physicalDefense: 0,                    priority: 30,                    traitStatuses: Array.Empty<TraitStatusSnapshot>(),                    category: CategorySnapshot.CreateStateless(CategoryKey.Main));
            }

            return new UnitStatsSnapshot(
                MoveMethod.LiftAndPlace,                moveRange: 5,                appointRange: 4,                attackPower: 3,                physicalDefense: 0,                priority: 30,                traitStatuses,                CategorySnapshot.CreateStateless(CategoryKey.Main));
        }

        private static MovementDestination CreateMovementDestination()
        {
            return new MovementDestination(
                new CellCoord(1, 0),                new[] { new CellCoord(1, 0) },                distance: 1);
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

        private static TraitActivationSnapshot CreateRoarActivation()
        {
            return new TraitActivationSnapshot(
                new EntityId(1),
                targetId: null,
                TraitKey.Roar,
                TraitActivationKind.StatusReset,
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
                4,
                3,
                30);
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
                4,
                3);
        }
    }
}
