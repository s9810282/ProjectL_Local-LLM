using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
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
        public CombatTurnFacts CombatTurn { get; }
        public StageOutcomeSnapshot Outcome { get; }
        public EntityId MainCharacterId { get; }
        public ReadOnlyMemory<ArtifactActivationSnapshot> ArtifactActivations { get; }

        public StageSnapshot(
            int mapWidth,
            int mapHeight,
            StagePhase currentPhase,
            EntitySnapshot[] entities,
            ReserveSlotSnapshot[] reserveSlots,
            CombatTurnFacts combatTurnSnapshot,
            StageOutcomeSnapshot outcome,
            EntityId mainCharacterId = default,
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

            EntitySnapshot[] copiedEntities = new EntitySnapshot[entities.Length];
            Array.Copy(entities, copiedEntities, entities.Length);

            ReserveSlotSnapshot[] copiedSlots = new ReserveSlotSnapshot[reserveSlots.Length];
            Array.Copy(reserveSlots, copiedSlots, reserveSlots.Length);

            var copiedArtifactActivations = artifactActivations == null
                ? Array.Empty<ArtifactActivationSnapshot>()
                : (ArtifactActivationSnapshot[])artifactActivations.Clone();

            MapWidth = mapWidth;
            MapHeight = mapHeight;
            CurrentPhase = currentPhase;
            Entities = copiedEntities;
            ReserveSlots = copiedSlots;
            ArtifactActivations = copiedArtifactActivations;
            CombatTurn = combatTurnSnapshot;
            Outcome = outcome;
            MainCharacterId = mainCharacterId;
        }

        // 구버전 호환용
        public StageSnapshot(
            int mapWidth,
            int mapHeight,
            StagePhase currentPhase,
            EntitySnapshot[] entities,
            ReserveSlotSnapshot[] reserveSlots,
            CombatTurnFacts combatTurnSnapshot)
            : this(
                mapWidth,
                mapHeight,
                currentPhase,
                entities,
                reserveSlots,
                combatTurnSnapshot,
                new StageOutcomeSnapshot(StageOutcome.InProgress, global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results.StageOutcomeReason.None))
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
                new CombatTurnFacts(currentPhase, controllerPhase: currentPhase == StagePhase.Combat
                    ? CombatControllerPhase.StartingPlayer : CombatControllerPhase.None))
        {
        }
    }
}
