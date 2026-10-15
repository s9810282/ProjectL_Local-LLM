using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// View가 Grid, 전체 Entity, Reserve UI, 현재 Phase와 Combat Turn을 한 번에 동기화할 때 읽는 복사본이다.
    /// </summary>
    public sealed class StageSnapshot
    {
        public int MapWidth { get; }
        public int MapHeight { get; }
        public StagePhase CurrentPhase { get; }
        public ReadOnlyMemory<EntitySnapshot> Entities { get; }
        public ReadOnlyMemory<ReserveSlotSnapshot> ReserveSlots { get; }
        public CombatTurnSnapshot CombatTurn { get; }
        public StageOutcomeSnapshot Outcome { get; }
        public StageNightSnapshot Night { get; }
        public EntityId MainCharacterId { get; }
        public bool MainCharacterPlacementLocked { get; }
        public int PreparationIndex { get; }
        public StageDrawSnapshot Draw { get; }
        public StageEconomySnapshot Economy { get; }
        public ReadOnlyMemory<ThornFieldCellSnapshot> ThornFields { get; }
        public ReadOnlyMemory<ArtifactActivationSnapshot> ArtifactActivations { get; }

        public StageSnapshot(
            int mapWidth,
            int mapHeight,
            StagePhase currentPhase,
            EntitySnapshot[] entities,
            ReserveSlotSnapshot[] reserveSlots,
            CombatTurnSnapshot combatTurnSnapshot,
            StageOutcomeSnapshot outcome,
            StageNightSnapshot night,
            EntityId mainCharacterId = default,
            bool mainCharacterPlacementLocked = false)
            : this(mapWidth, mapHeight, currentPhase, entities, reserveSlots, combatTurnSnapshot,
                outcome, night, mainCharacterId, mainCharacterPlacementLocked, 1,
                new StageDrawSnapshot(0, false, outcome.IsStageEnded ? CharacterDrawFailureReason.StageEnded :
                    currentPhase == StagePhase.Combat ? CharacterDrawFailureReason.WrongPhase : CharacterDrawFailureReason.FirstPreparation),
                new StageEconomySnapshot(0, 0))
        {
        }

        public StageSnapshot(
            int mapWidth,
            int mapHeight,
            StagePhase currentPhase,
            EntitySnapshot[] entities,
            ReserveSlotSnapshot[] reserveSlots,
            CombatTurnSnapshot combatTurnSnapshot,
            StageOutcomeSnapshot outcome,
            StageNightSnapshot night,
            EntityId mainCharacterId,
            bool mainCharacterPlacementLocked,
            int preparationIndex,
            StageDrawSnapshot draw,
            StageEconomySnapshot economy)
            : this(
                mapWidth,
                mapHeight,
                currentPhase,
                entities,
                reserveSlots,
                combatTurnSnapshot,
                outcome,
                night,
                mainCharacterId,
                mainCharacterPlacementLocked,
                preparationIndex,
                draw,
                economy,
                Array.Empty<ThornFieldCellSnapshot>())
        {
        }

        public StageSnapshot(
            int mapWidth,
            int mapHeight,
            StagePhase currentPhase,
            EntitySnapshot[] entities,
            ReserveSlotSnapshot[] reserveSlots,
            CombatTurnSnapshot combatTurnSnapshot,
            StageOutcomeSnapshot outcome,
            StageNightSnapshot night,
            EntityId mainCharacterId,
            bool mainCharacterPlacementLocked,
            int preparationIndex,
            StageDrawSnapshot draw,
            StageEconomySnapshot economy,
            ThornFieldCellSnapshot[] thornFields,
            ArtifactActivationSnapshot[] artifactActivations = null)
        {
            if (mapWidth <= 0)
                throw new ArgumentOutOfRangeException(nameof(mapWidth));
            if (mapHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(mapHeight));
            if (currentPhase != StagePhase.Deployment && currentPhase != StagePhase.Combat)
                throw new ArgumentOutOfRangeException(nameof(currentPhase));
            if (entities == null)
                throw new ArgumentNullException(nameof(entities));
            if (reserveSlots == null)
                throw new ArgumentNullException(nameof(reserveSlots));
            if (thornFields == null)
                throw new ArgumentNullException(nameof(thornFields));
            if (preparationIndex < 1)
                throw new ArgumentOutOfRangeException(nameof(preparationIndex));
            Draw = draw ?? throw new ArgumentNullException(nameof(draw));
            Economy = economy ?? throw new ArgumentNullException(nameof(economy));
            PreparationIndex = preparationIndex;

            EntitySnapshot[] copiedEntities = new EntitySnapshot[entities.Length];
            Array.Copy(entities, copiedEntities, entities.Length);

            ReserveSlotSnapshot[] copiedSlots = new ReserveSlotSnapshot[reserveSlots.Length];
            Array.Copy(reserveSlots, copiedSlots, reserveSlots.Length);

            var copiedThornFields =
                (ThornFieldCellSnapshot[])thornFields.Clone();
            var copiedArtifactActivations = artifactActivations == null
                ? Array.Empty<ArtifactActivationSnapshot>()
                : (ArtifactActivationSnapshot[])artifactActivations.Clone();
            var occupiedThornCells = new HashSet<CellCoord>();
            for (int i = 0; i < copiedThornFields.Length; i++)
            {
                ThornFieldCellSnapshot thornField = copiedThornFields[i];
                if (thornField.Cell.X < 0 || thornField.Cell.X >= mapWidth ||
                    thornField.Cell.Y < 0 || thornField.Cell.Y >= mapHeight)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(thornFields),
                        "ThornField cell must be inside the Stage map.");
                }
                if (thornField.RemainingUnitTurns < 1 || thornField.ThornDamage < 1)
                    throw new ArgumentException(
                        "ThornField snapshot values must be positive.",
                        nameof(thornFields));
                if (occupiedThornCells.Add(thornField.Cell) == false)
                    throw new ArgumentException(
                        "ThornField snapshot cells must be unique.",
                        nameof(thornFields));
            }
            Array.Sort(copiedThornFields, (left, right) =>
            {
                int yComparison = left.Cell.Y.CompareTo(right.Cell.Y);
                return yComparison != 0
                    ? yComparison
                    : left.Cell.X.CompareTo(right.Cell.X);
            });

            MapWidth = mapWidth;
            MapHeight = mapHeight;
            CurrentPhase = currentPhase;
            Entities = copiedEntities;
            ReserveSlots = copiedSlots;
            ThornFields = copiedThornFields;
            ArtifactActivations = copiedArtifactActivations;
            CombatTurn = combatTurnSnapshot;
            Outcome = outcome;
            Night = night;
            MainCharacterId = mainCharacterId;
            MainCharacterPlacementLocked = mainCharacterPlacementLocked;
        }

        // 구버전 호환용
        public StageSnapshot(
            int mapWidth,
            int mapHeight,
            StagePhase currentPhase,
            EntitySnapshot[] entities,
            ReserveSlotSnapshot[] reserveSlots,
            CombatTurnSnapshot combatTurnSnapshot)
            : this(
                mapWidth,
                mapHeight,
                currentPhase,
                entities,
                reserveSlots,
                combatTurnSnapshot,
                new StageOutcomeSnapshot(StageOutcome.InProgress, global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.StageOutcomeReason.None),
                new StageNightSnapshot(1000, 0))
        {
        }

        // 구버전 호환용
        public StageSnapshot(
            int mapWidth,
            int mapHeight,
            StagePhase currentPhase,
            EntitySnapshot[] entities,
            ReserveSlotSnapshot[] reserveSlots)
            : this(
                mapWidth,
                mapHeight,
                currentPhase,
                entities,
                reserveSlots,
                new CombatTurnSnapshot(false, null))
        {
        }
    }
}
