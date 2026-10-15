using System;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
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

            public EntityPlacement ToData()
            {
                EntityKey key = unitDefinition != null ? unitDefinition.Key : EntityKey.None;
                return new EntityPlacement(key, anchor.ToCellCoord(), rotation);
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
        private sealed class RailwayInput
        {
            [SerializeField] private CellInput[] railCells = new CellInput[0];
            [SerializeField, Min(1)] private int firstActivationAfterUnitTurns = 3;
            [SerializeField, Min(1)] private int repeatIntervalUnitTurns = 3;

            public RailwayData ToData() =>
                new RailwayData(CellInput.ToCellCoords(railCells), firstActivationAfterUnitTurns, repeatIntervalUnitTurns);
        }

        [Header("Stage")]
        [SerializeField] private int timelineSeed;
        [SerializeField] private FieldDataSO fieldDefinition;
        [SerializeField] private UnitRosterDataSO unitRoster;
        [SerializeField] private StageEconomyDataSO economy;
        [Tooltip("밤 길이입니다. Core StageConfig.NightLength와 같은 단위이며 1 이상이어야 합니다.")]
        [SerializeField] private long nightLength = 500;

        [Header("Enemy")]
        [SerializeField] private UnitPlacementInput[] initialEnemyPlacements = new UnitPlacementInput[0];
        [SerializeField] private EnemyLoadoutDataSO enemyLoadout;

        [Header("Structures")]
        [SerializeField] private StructureDataSO[] structureCatalog = new StructureDataSO[0];
        [SerializeField] private StructurePlacementInput[] initialEnemyStructurePlacements = new StructurePlacementInput[0];

        [Header("Main Character")]
        [Tooltip("Player 배치 구역(맵 아래쪽) 안이어야 합니다. 플레이어 본부는 배치 단계에서 플레이어가 놓습니다.")]
        [FormerlySerializedAs("playerHeadquartersAnchor")]
        [SerializeField] private CellInput mainCharacterAnchor;
        [FormerlySerializedAs("playerHeadquartersRotation")]
        [SerializeField] private Rotation4 mainCharacterRotation = Rotation4.R0;

        [Header("Headquarters")]
        [Tooltip("적 본부 정의입니다. 비우면 이 Stage에는 적 본부가 없습니다.")]
        [SerializeField] private StructureDataSO headquarters;
        [Tooltip("Enemy 배치 구역(맵 위쪽) 안이어야 합니다.")]
        [SerializeField] private CellInput enemyHeadquartersAnchor;
        [SerializeField] private Rotation4 enemyHeadquartersRotation = Rotation4.R0;

        [Header("Walls")]
        [SerializeField] private CellInput[] initialWallCells = Array.Empty<CellInput>();
        [SerializeField, Range(0, 511)] private int cornerCutTicks = 128;

        [Header("Railway")]
        [SerializeField] private bool railwayEnabled;
        [SerializeField] private RailwayInput railway = new();

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

            return new CombatStageDetail(
                timelineSeed,
                fieldDefinition != null ? fieldDefinition.FieldId : null,
                unitRoster != null ? unitRoster.RosterKey : 0,
                enemyLoadout != null ? enemyLoadout.LoadoutKey : 0,
                economy != null ? economy.EconomyKey : 0,
                nightLength,
                enemyPlacements,
                structures,
                structurePlacements,
                headquarters != null ? headquarters.Key : EntityKey.None,
                mainCharacterAnchor.ToCellCoord(),
                mainCharacterRotation,
                enemyHeadquartersAnchor.ToCellCoord(),
                enemyHeadquartersRotation,
                railwayEnabled && railway != null ? railway.ToData() : null,
                wallCells: CellInput.ToCellCoords(initialWallCells), wallBlocking: new WallBlockingSpec(cornerCutTicks));
        }
    }
}
