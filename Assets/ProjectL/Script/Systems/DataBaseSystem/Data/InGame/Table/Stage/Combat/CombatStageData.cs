using System;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Item;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat
{
    /// <summary>
    /// CombatStageDetail의 입력 SO입니다. Field·Roster·Loadout·Economy·구조물은 SO를 끌어다 놓고,
    /// CreateStageData에서 키로 바꿉니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatStageData", menuName = "ProjectL/Database/InGame/Stage/Combat")]
    public sealed class CombatStageData : StageBaseData
    {
        [Serializable]
        private struct UnitPlacementInput
        {
            [SerializeField] private UnitDataSO unitDefinition;
            [SerializeField] private CellInput anchor;
            [SerializeField] private Rotation4 rotation;
            [SerializeField] private bool isVictoryTarget;
            [SerializeField] private ConsumableItemDataSO startingItem;

            public EntityPlacement ToData()
            {
                EntityKey key = unitDefinition != null ? unitDefinition.Key : EntityKey.None;
                int? itemKey = startingItem == null ? null : ((ConsumableItemData)startingItem.GetData()).Key;
                return new EntityPlacement(key, anchor.ToCellCoord(), rotation, isVictoryTarget, itemKey);
            }
        }

        [Serializable]
        private struct StructurePlacementInput
        {
            [SerializeField] private StructureDataSO structureDefinition;
            [SerializeField] private CellInput anchor;
            [SerializeField] private Rotation4 rotation;

            public EntityPlacement ToData()
            {
                EntityKey key = structureDefinition != null ? structureDefinition.Key : EntityKey.None;
                return new EntityPlacement(key, anchor.ToCellCoord(), rotation);
            }
        }

        [Serializable]
        private struct StaticTerrainPlacementInput
        {
            [SerializeField, Min(1)] private long sourceId;
            [SerializeField] private CellInput cell;
            [SerializeField] private StaticTerrainKind kind;
            [SerializeField] private Owner[] knownTo;
            [Tooltip("약한 지반의 붕괴까지 필요한 진입 횟수입니다. 약한 지반은 1 이상, 다른 지형은 0으로 설정합니다.")]
            [SerializeField] private int entryThreshold;

            public StaticTerrainPlacementData ToData() =>
                new StaticTerrainPlacementData(sourceId, cell.ToCellCoord(), kind, knownTo ?? Array.Empty<Owner>(), entryThreshold);
        }

        [Serializable]
        private sealed class RailwayInput
        {
            [SerializeField] private CellInput[] railCells = new CellInput[0];
            [Tooltip("플레이어·적 진영 종료 합산 횟수 기준의 최초 발동 시점입니다.")]
            [FormerlySerializedAs("firstActivationAfterUnitTurns")]
            [SerializeField, Min(1)] private int firstActivationAfterControllerTurns = 3;
            [Tooltip("플레이어·적 진영 종료 합산 횟수 기준의 반복 간격입니다.")]
            [FormerlySerializedAs("repeatIntervalUnitTurns")]
            [SerializeField, Min(1)] private int repeatIntervalControllerTurns = 3;

            public RailwayData ToData() =>
                new RailwayData(CellInput.ToCellCoords(railCells), firstActivationAfterControllerTurns, repeatIntervalControllerTurns);
        }

        [Header("Stage")]
        [SerializeField] private int timelineSeed;
        [SerializeField] private FieldDataSO fieldDefinition;
        [SerializeField] private UnitRosterDataSO unitRoster;
        [SerializeField] private StageEconomyDataSO economy;

        [Header("Enemy")]
        [SerializeField] private BattleObjectiveKind objectiveKind = BattleObjectiveKind.EnemyHeadquartersDestroyed;
        [SerializeField] private UnitPlacementInput[] initialEnemyPlacements = new UnitPlacementInput[0];
        [SerializeField] private EnemyLoadoutDataSO enemyLoadout;
        [Tooltip("이 스테이지 적의 레벨입니다. 직접 배치와 로드아웃 출현 적 모두에 적용하며 소환체는 레벨 1입니다.")]
        [SerializeField, Min(1)] private int enemyLevel = 1;

        [Header("Structures")]
        [SerializeField] private StructureDataSO[] structureCatalog = new StructureDataSO[0];
        [SerializeField] private StructurePlacementInput[] initialEnemyStructurePlacements = new StructurePlacementInput[0];

        [Header("Deployment")]
        [Tooltip("아군 배치 구역 칸입니다. 비우면 필드의 아군 배치 줄 수(아래부터)를 씁니다. 적은 아군 구역 밖 어디에나 놓입니다.")]
        [SerializeField] private CellInput[] playerDeploymentCells = Array.Empty<CellInput>();

        // Core가 읽지 않는 칸이라 인스펙터에서 숨긴다. 메인 캐릭터는 첫 손패 0번으로 오고 위치는 플레이어가 배치 국면에 정한다.
        // 값은 BattleInitialization.MainAnchor까지 전달되지만 읽는 곳이 없다. SO 모드 도구가 이름으로 읽어 칸은 남긴다(지우기는 PZ-d 때 도구와 함께).
        [FormerlySerializedAs("playerHeadquartersAnchor")]
        [SerializeField, HideInInspector] private CellInput mainCharacterAnchor;
        [FormerlySerializedAs("playerHeadquartersRotation")]
        [SerializeField, HideInInspector] private Rotation4 mainCharacterRotation = Rotation4.R0;

        [Header("Headquarters")]
        [Tooltip("적 본부 정의입니다. 비우면 이 Stage에는 적 본부가 없습니다.")]
        [SerializeField] private StructureDataSO headquarters;
        [Tooltip("아군 배치 구역 밖이어야 합니다.")]
        [SerializeField] private CellInput enemyHeadquartersAnchor;
        [SerializeField] private Rotation4 enemyHeadquartersRotation = Rotation4.R0;

        [Tooltip("신규 지형·벽·장애물·Object 배치는 여기서 설정합니다. 벽은 Terrain Placements에 Wall 정의를 참조합니다. Terrain/Object 외형 회전은 ViewGroup에서 설정합니다.")]
        [SerializeField] private CombatStageEnvironmentInput environment = new();

        [Header("Wall Settings")]
        [Tooltip("벽 모서리의 잘림 크기입니다. Environment의 Wall과 호환용 Wall Cells 모두에 적용합니다.")]
        [SerializeField, Range(0, 511)] private int cornerCutTicks = 128;

        [Header("Railway")]
        [SerializeField] private bool railwayEnabled;
        [SerializeField] private RailwayInput railway = new();

        [Header("Rockfall")]
        [SerializeField] private RockfallGimmickAuthoring rockfall = new();

        [Header("Haboob")]
        [SerializeField] private HaboobGimmickAuthoring haboob = new();

        // 기존 에셋의 직렬화 경로를 유지합니다. 전용 Inspector의 호환용 항목에서만 표시합니다.
        [SerializeField, HideInInspector] private CellInput[] initialWallCells = Array.Empty<CellInput>();
        [SerializeField, HideInInspector] private StaticTerrainPlacementInput[] staticTerrainPlacements = Array.Empty<StaticTerrainPlacementInput>();

        public override StageType StageType => StageType.Combat;

        protected override IStageData CreateStageData()
        {
            var enemyPlacements = new EntityPlacement[initialEnemyPlacements?.Length ?? 0];
            for (int i = 0; i < enemyPlacements.Length; i++)
                enemyPlacements[i] = initialEnemyPlacements[i].ToData();

            var structures = new EntityKey[structureCatalog?.Length ?? 0];
            for (int i = 0; i < structures.Length; i++)
                structures[i] = structureCatalog[i] != null ? structureCatalog[i].Key : EntityKey.None;

            var structurePlacements = new EntityPlacement[initialEnemyStructurePlacements?.Length ?? 0];
            for (int i = 0; i < structurePlacements.Length; i++)
                structurePlacements[i] = initialEnemyStructurePlacements[i].ToData();

            var terrainPlacements = new StaticTerrainPlacementData[staticTerrainPlacements?.Length ?? 0];
            for (int i = 0; i < terrainPlacements.Length; i++)
                terrainPlacements[i] = staticTerrainPlacements[i].ToData();

            var environmentData = (environment ?? new CombatStageEnvironmentInput()).ToData(terrainPlacements);
            var detail = new CombatStageDetail(
                timelineSeed,
                fieldDefinition != null ? fieldDefinition.FieldId : null,
                unitRoster != null ? unitRoster.RosterKey : 0,
                enemyLoadout != null ? enemyLoadout.LoadoutKey : 0,
                economy != null ? economy.EconomyKey : 0,
                enemyPlacements,
                structures,
                structurePlacements,
                headquarters != null ? headquarters.Key : EntityKey.None,
                mainCharacterAnchor.ToCellCoord(),
                mainCharacterRotation,
                enemyHeadquartersAnchor.ToCellCoord(),
                enemyHeadquartersRotation,
                railwayEnabled && railway != null ? railway.ToData() : null,
                terrainPresencePlacements: environmentData.Presence,
                staticTerrainPlacements: environmentData.Static,
                obstaclePlacements: environmentData.Obstacles,
                objectPlacements: environmentData.Objects,
                wallCells: CellInput.ToCellCoords(initialWallCells), wallBlocking: new WallBlockingSpec(cornerCutTicks),
                tornadoPlacements: environmentData.Tornadoes,
                objectiveKind: objectiveKind,
                enemyLevel: enemyLevel,
                initialRandomTornadoPlacements: environmentData.RandomTornadoes,
                playerDeploymentCells: CellInput.ToCellCoords(playerDeploymentCells),
                rockfall: rockfall?.ToData(), haboob: haboob?.ToData(), tornado: environmentData.Tornado);
            if (fieldDefinition != null)
            {
                var field = (FieldData)fieldDefinition.GetData();
                CombatStageEnvironmentAuthoringValidation.Validate(detail, field.ColumnCount, field.RowCount);
                ValidatePlayerDeploymentCells(detail, field.ColumnCount, field.RowCount);
            }

            return detail;
        }

        /// <summary>아군 배치 칸이 있으면 중복과 판 범위를 Core 구역 규칙(PlayerDeploymentZone)으로 검사합니다.</summary>
        private void ValidatePlayerDeploymentCells(CombatStageDetail detail, int width, int height)
        {
            if (detail.PlayerDeploymentCells.Count == 0)
                return;

            PlayerDeploymentZone zone;
            try
            {
                zone = PlayerDeploymentZone.Of(detail.PlayerDeploymentCells);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException($"[CombatStageData] {name}: {exception.Message}", exception);
            }

            var outside = zone.CellsOutside(width, height);
            if (outside.Count > 0)
            {
                throw new InvalidOperationException(
                    $"[CombatStageData] {name}: 아군 배치 칸이 판({width}×{height}) 밖입니다: " +
                    string.Join(", ", outside.Select(cell => $"({cell.X}, {cell.Y})")));
            }
        }
    }
}
