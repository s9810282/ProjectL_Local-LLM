using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    /// <summary>초기 배치 하나입니다. Key는 유닛 또는 구조물의 EntityKey입니다.</summary>
    public readonly struct EntityPlacement
    {
        public int? StartingItemKey { get; }
        public EntityKey Key { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }
        /// <summary>이 유닛 배치를 지정 승리 목표로 삼을지 나타냅니다. 구조물에는 사용할 수 없습니다.</summary>
        public bool IsVictoryTarget { get; }

        public EntityPlacement(EntityKey key, CellCoord anchor, Rotation4 rotation, bool isVictoryTarget = false, int? startingItemKey = null)
        {
            if (startingItemKey <= 0) throw new ArgumentOutOfRangeException(nameof(startingItemKey));
            StartingItemKey = startingItemKey;
            Key = key;
            Anchor = anchor;
            Rotation = rotation;
            IsVictoryTarget = isVictoryTarget;
        }
    }

    /// <summary>레일웨이 기믹 설정입니다.</summary>
    public sealed class RailwayData
    {
        public IReadOnlyList<CellCoord> RailCells { get; }
        public int FirstActivationAfterControllerTurns { get; }
        public int RepeatIntervalControllerTurns { get; }

        public RailwayData(CellCoord[] railCells, int firstActivationAfterControllerTurns, int repeatIntervalControllerTurns)
        {
            RailCells = new ReadOnlyCollection<CellCoord>(railCells ?? Array.Empty<CellCoord>());
            FirstActivationAfterControllerTurns = firstActivationAfterControllerTurns;
            RepeatIntervalControllerTurns = repeatIntervalControllerTurns;
        }
    }

    /// <summary>
    /// 전투 스테이지 상세 데이터입니다. Core가 받는 순수 C# 데이터이며 CombatStageData SO가 생성합니다.
    /// 다른 데이터는 키로 가리킵니다(FieldId, RosterKey, LoadoutKey, EconomyKey, EntityKey).
    /// </summary>
    /// <remarks>
    /// Core 대응: StageConfig(필드 크기·대체용 아군 배치 줄 수는 FieldTable, TimelineSeed·아군 배치 칸·초기 배치·레일웨이는 여기),
    /// StageEconomySpec(EconomyKey), MainCharacterEntrySpec(메인 캐릭터 앵커·회전, 체력은 Run 상태에서).
    /// 플레이어 쪽은 본부 대신 메인 캐릭터가 서고, 본부(HeadquartersKey)는 적 쪽에만 남습니다.
    /// </remarks>
    public sealed class CombatStageDetail : IStageData
    {
        public StageType StageType => StageType.Combat;
        public BattleObjectiveKind ObjectiveKind { get; }

        public int TimelineSeed { get; }
        public string FieldId { get; }
        public int UnitRosterKey { get; }
        /// <summary>0이면 적 구성이 없습니다.</summary>
        public int EnemyLoadoutKey { get; }
        /// <summary>이 스테이지 적의 레벨입니다. 직접 배치·로드아웃·넥서스 증원에 적용합니다.</summary>
        public int EnemyLevel { get; }
        public NexusReinforcementSpec NexusReinforcements { get; }
        public int EconomyKey { get; }
        public IReadOnlyList<EntityPlacement> InitialEnemyPlacements { get; }
        public IReadOnlyList<EntityKey> StructureCatalog { get; }
        public IReadOnlyList<EntityPlacement> InitialEnemyStructurePlacements { get; }
        /// <summary>적 본부입니다. None이면 이 스테이지에는 본부가 없습니다.</summary>
        public EntityKey HeadquartersKey { get; }
        public CellCoord MainCharacterAnchor { get; }
        public Rotation4 MainCharacterRotation { get; }
        public CellCoord EnemyHeadquartersAnchor { get; }
        public Rotation4 EnemyHeadquartersRotation { get; }
        /// <summary>레일웨이를 쓰지 않으면 null입니다.</summary>
        public RailwayData Railway { get; }
        public RockfallGimmickData Rockfall { get; }
        public HaboobGimmickData Haboob { get; }
        public TornadoGimmickSpec Tornado { get; }
        public MeteorGimmickData Meteor { get; }
        public LavaChainGimmickData LavaChain { get; }
        public IReadOnlyList<TerrainPresencePlacementData> TerrainPresencePlacements { get; }
        public IReadOnlyList<StaticTerrainPlacementData> StaticTerrainPlacements { get; }
        public IReadOnlyList<InitialObstaclePlacementSpec> ObstaclePlacements { get; }
        public IReadOnlyList<InitialObjectPlacementSpec> ObjectPlacements { get; }
        public IReadOnlyList<TornadoSourceFact> TornadoPlacements { get; }
        public IReadOnlyList<MovingPlatformSpec> MovingPlatforms { get; }
        /// <summary>최초 칸을 전투 준비가 무작위로 정하는 토네이도입니다. 칸이 없으므로 공간 검증에 넣지 않습니다.</summary>
        public IReadOnlyList<InitialRandomTornadoPlacementSpec> InitialRandomTornadoPlacements { get; }
        public IReadOnlyList<CellCoord> WallCells { get; }
        public WallBlockingSpec WallBlocking { get; }
        /// <summary>아군 배치 구역 칸입니다. 비어 있으면 필드의 아군 배치 줄 수(아래부터)를 씁니다. 대체 규칙은 BattleDefinitionAdapter가 정합니다.</summary>
        public IReadOnlyList<CellCoord> PlayerDeploymentCells { get; }

        public CombatStageDetail(
            int timelineSeed,
            string fieldId,
            int unitRosterKey,
            int enemyLoadoutKey,
            int economyKey,
            EntityPlacement[] initialEnemyPlacements,
            EntityKey[] structureCatalog,
            EntityPlacement[] initialEnemyStructurePlacements,
            EntityKey headquartersKey,
            CellCoord mainCharacterAnchor,
            Rotation4 mainCharacterRotation,
            CellCoord enemyHeadquartersAnchor,
            Rotation4 enemyHeadquartersRotation,
            RailwayData railway,
            IReadOnlyList<TerrainPresencePlacementData> terrainPresencePlacements = null,
            IReadOnlyList<StaticTerrainPlacementData> staticTerrainPlacements = null,
            IReadOnlyList<InitialObstaclePlacementSpec> obstaclePlacements = null,
            IReadOnlyList<InitialObjectPlacementSpec> objectPlacements = null,
            IReadOnlyList<CellCoord> wallCells = null, WallBlockingSpec wallBlocking = null,
            IReadOnlyList<TornadoSourceFact> tornadoPlacements = null,
            BattleObjectiveKind objectiveKind = BattleObjectiveKind.EnemyHeadquartersDestroyed,
            int enemyLevel = 1,
            IReadOnlyList<InitialRandomTornadoPlacementSpec> initialRandomTornadoPlacements = null,
            IReadOnlyList<CellCoord> playerDeploymentCells = null, RockfallGimmickData rockfall = null, HaboobGimmickData haboob = null,
            TornadoGimmickSpec tornado = null, NexusReinforcementSpec nexusReinforcements = null,
            IReadOnlyList<MovingPlatformSpec> movingPlatforms = null, MeteorGimmickData meteor = null, LavaChainGimmickData lavaChain = null)
        {
            Tornado = tornado;
            Rockfall = rockfall;
            Haboob = haboob;
            Meteor = meteor;
            LavaChain = lavaChain;
            ObjectiveKind = objectiveKind;
            PlayerDeploymentCells = Array.AsReadOnly((playerDeploymentCells ?? Array.Empty<CellCoord>()).ToArray());
            EnemyLevel = enemyLevel;
            NexusReinforcements = nexusReinforcements;
            TimelineSeed = timelineSeed;
            WallCells = Array.AsReadOnly((wallCells ?? Array.Empty<CellCoord>()).ToArray());
            WallBlocking = wallBlocking ?? WallBlockingSpec.Default;
            var obstacleCopy = (obstaclePlacements ?? Array.Empty<InitialObstaclePlacementSpec>()).ToArray();
            if (obstacleCopy.Any(p => p == null)) throw new ArgumentException("장애물 배치는 null을 포함할 수 없습니다.");
            ObstaclePlacements = Array.AsReadOnly(obstacleCopy);
            FieldId = fieldId;
            UnitRosterKey = unitRosterKey;
            EnemyLoadoutKey = enemyLoadoutKey;
            EconomyKey = economyKey;
            InitialEnemyPlacements = new ReadOnlyCollection<EntityPlacement>(
                initialEnemyPlacements ?? Array.Empty<EntityPlacement>());
            StructureCatalog = new ReadOnlyCollection<EntityKey>(structureCatalog ?? Array.Empty<EntityKey>());
            InitialEnemyStructurePlacements = new ReadOnlyCollection<EntityPlacement>(
                initialEnemyStructurePlacements ?? Array.Empty<EntityPlacement>());
            HeadquartersKey = headquartersKey;
            MainCharacterAnchor = mainCharacterAnchor;
            MainCharacterRotation = mainCharacterRotation;
            EnemyHeadquartersAnchor = enemyHeadquartersAnchor;
            EnemyHeadquartersRotation = enemyHeadquartersRotation;
            Railway = railway;
            var terrain = (terrainPresencePlacements ?? Array.Empty<TerrainPresencePlacementData>()).ToArray();
            if (terrain.Any(placement => placement == null) ||
                terrain.Select(placement => placement.SourceId).Distinct().Count() != terrain.Length)
                throw new ArgumentException("지형 배치에는 null 또는 중복 원본 ID를 넣을 수 없습니다.", nameof(terrainPresencePlacements));
            TerrainPresencePlacements = Array.AsReadOnly(terrain);
            var staticTerrain = (staticTerrainPlacements ?? Array.Empty<StaticTerrainPlacementData>()).ToArray();
            if (staticTerrain.Any(placement => placement == null) ||
                terrain.Select(p => p.SourceId).Concat(staticTerrain.Select(p => p.SourceId)).Distinct().Count() !=
                terrain.Length + staticTerrain.Length)
                throw new ArgumentException("지형 배치에는 null 또는 중복 원본 ID를 넣을 수 없습니다.", nameof(staticTerrainPlacements));
            StaticTerrainPlacements = Array.AsReadOnly(staticTerrain);
            var objects = (objectPlacements ?? Array.Empty<InitialObjectPlacementSpec>()).ToArray();
            if (objects.Any(p => p == null)) throw new ArgumentException("Object 배치는 null을 포함할 수 없습니다.");
            var tornadoes = (tornadoPlacements ?? Array.Empty<TornadoSourceFact>()).ToArray();
            if (tornadoes.Any(p => p == null || !p.Active))
                throw new ArgumentException("초기 토네이도 배치는 null이거나 비활성일 수 없습니다.", nameof(tornadoPlacements));
            var randomTornadoes = (initialRandomTornadoPlacements ?? Array.Empty<InitialRandomTornadoPlacementSpec>()).ToArray();
            if (randomTornadoes.Any(p => p == null))
                throw new ArgumentException("최초 무작위 토네이도 배치는 null일 수 없습니다.", nameof(initialRandomTornadoPlacements));
            var platforms = (movingPlatforms ?? Array.Empty<MovingPlatformSpec>()).ToArray();
            if (platforms.Any(p => p == null))
                throw new ArgumentException("플랫폼 정의는 null일 수 없습니다.", nameof(movingPlatforms));
            var sourceIds = terrain.Select(p => p.SourceId).Concat(staticTerrain.Select(p => p.SourceId))
                .Concat(objects.Select(p => p.CreateSource(1).Id)).Concat(tornadoes.Select(p => p.Id))
                .Concat(randomTornadoes.Select(p => p.SourceId)).Concat(platforms.Select(p => p.SourceId)).ToArray();
            if (sourceIds.Distinct().Count() != sourceIds.Length)
                throw new ArgumentException("환경 배치 원본 ID는 중복될 수 없습니다.");
            ObjectPlacements = Array.AsReadOnly(objects);
            TornadoPlacements = Array.AsReadOnly(tornadoes);
            InitialRandomTornadoPlacements = Array.AsReadOnly(randomTornadoes);
            MovingPlatforms = Array.AsReadOnly(platforms);
        }

        public void Validate()
        {
            // develop-4week StageEntryDataBuilder와 같이 Field, Roster, Economy가 필수입니다.
            if (string.IsNullOrWhiteSpace(FieldId) || UnitRosterKey < 1)
                throw new InvalidOperationException("[CombatStageDetail] Combat stage requires Field and Unit Roster.");
            if (EconomyKey < 1)
                throw new InvalidOperationException("[CombatStageDetail] Combat stage requires Economy.");
            if (EnemyLoadoutKey < 0)
                throw new InvalidOperationException("[CombatStageDetail] EnemyLoadoutKey must not be negative.");
            if (EnemyLevel < 1)
                throw new InvalidOperationException("[CombatStageDetail] EnemyLevel must be at least 1.");
            if (NexusReinforcements != null && NexusReinforcements.EnemyLevel != EnemyLevel)
                throw new InvalidOperationException("증원 레벨은 스테이지 적 레벨과 같아야 합니다.");

            ValidatePlacements(InitialEnemyPlacements, "initial enemy");
            ValidatePlacements(InitialEnemyStructurePlacements, "initial enemy structure");
            ValidateObjective();
            foreach (EntityKey key in StructureCatalog)
            {
                if (key == EntityKey.None)
                    throw new InvalidOperationException("[CombatStageDetail] Structure catalog has a missing structure.");
            }

            if (!System.Enum.IsDefined(typeof(Rotation4), MainCharacterRotation) ||
                !System.Enum.IsDefined(typeof(Rotation4), EnemyHeadquartersRotation))
                throw new InvalidOperationException("[CombatStageDetail] Main character or headquarters rotation is invalid.");

            if (Railway != null &&
                (Railway.FirstActivationAfterControllerTurns < 1 || Railway.RepeatIntervalControllerTurns < 1))
                throw new InvalidOperationException("[CombatStageDetail] Railway turn values must be at least 1.");
        }

        private void ValidateObjective()
        {
            if (ObjectiveKind == BattleObjectiveKind.None ||
                !System.Enum.IsDefined(typeof(BattleObjectiveKind), ObjectiveKind))
            {
                throw new InvalidOperationException("[CombatStageDetail] Objective kind is invalid.");
            }

            if (InitialEnemyStructurePlacements.Any(placement => placement.IsVictoryTarget || placement.StartingItemKey.HasValue))
            {
                throw new InvalidOperationException("[CombatStageDetail] A structure cannot be a designated victory target.");
            }

            int targetCount = InitialEnemyPlacements.Count(placement => placement.IsVictoryTarget);
            int requiredCount = ObjectiveKind == BattleObjectiveKind.DesignatedBossDefeated ? 1 : 0;
            if (targetCount != requiredCount)
            {
                throw new InvalidOperationException($"[CombatStageDetail] Objective {ObjectiveKind} requires {requiredCount} marked enemy placements.");
            }
        }

        private static void ValidatePlacements(IReadOnlyList<EntityPlacement> placements, string label)
        {
            foreach (EntityPlacement placement in placements)
            {
                if (placement.Key == EntityKey.None)
                    throw new InvalidOperationException($"[CombatStageDetail] {label} placement has a missing entity.");
                if (!System.Enum.IsDefined(typeof(Rotation4), placement.Rotation))
                    throw new InvalidOperationException($"[CombatStageDetail] {label} placement rotation is invalid.");
            }
        }
    }
}
