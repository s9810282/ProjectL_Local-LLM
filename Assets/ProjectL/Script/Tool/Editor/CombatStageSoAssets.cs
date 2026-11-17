using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using CellCoord = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CellCoord;
using ChapterStageKind = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap.ChapterStageKind;
using DeploymentZonePolicy = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement.DeploymentZonePolicy;
using MovingPlatformSpec = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.MovingPlatformSpec;
using ObstacleContentKind = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.ObstacleContentKind;
using TornadoGeometry = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.TornadoGeometry;
using ObstacleContentProfile = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.ObstacleContentProfile;
using Owner = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.Owner;
using PlayerDeploymentZone = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.PlayerDeploymentZone;
using UnitOwnerEligibility = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.UnitOwnerEligibility;
using UnitRole = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.UnitRole;

namespace ProjectL.Tool.Editor
{
    // SO 에셋과 작업본 사이의 창구입니다. SO 직렬화 필드 이름은 이 클래스에만 둡니다(설계 SM3, Q-SM1 A).
    // DB SO 형식 대부분이 internal이라 형식은 이름으로 찾고 값은 SerializedObject로 읽습니다.
    internal static class CombatStageSoAssets
    {
        internal const string RootType = "StageRootDataSO", CombatType = "CombatStageData", ViewType = "ViewStageDataSO";
        internal const string UnitType = "UnitDataSO", StructureType = "StructureDataSO", TerrainType = "TerrainDefinitionDataSO";
        internal const string ObstacleType = "ObstacleDefinitionDataSO", ObjectType = "ObjectDefinitionDataSO";
        internal const string FieldType = "FieldDataSO", RosterType = "UnitRosterDataSO", EconomyType = "StageEconomyDataSO";
        internal const string LoadoutType = "EnemyLoadoutDataSO", ItemType = "ConsumableItemDataSO";
        internal const string RootTableType = "StageRootDataTableSO", ViewTableType = "ViewStageDataTableSO";
        internal const string FieldViewTableType = "StageFieldViewDataTableSO";
        private const string UnitVisualType = "UnitVisualDataSO", TerrainVisualType = "TerrainVisualDataSO", ObjectVisualType = "ObjectVisualDataSO";
        private const string FieldTableType = "FieldDataTableSO", RosterTableType = "UnitRosterDataTableSO";
        private const string EconomyTableType = "StageEconomyDataTableSO", LoadoutTableType = "EnemyLoadoutDataTableSO";
        private const string FieldId = "fieldId", RosterKey = "rosterKey", EconomyKey = "economyKey", LoadoutKey = "loadoutKey", Reserve = "reserveCapacity";
        private const string AttackRange = "attackRange", TargetingMethod = "targetingMethod", Shape = "shape", Splash = "splash";
        private const string TargetingWidth = "width";
        private const string UnitRoleField = "role", UnitOwnerField = "ownerEligibility";
        private const string FieldViewType = "StageFieldViewDataSO";
        private const string FieldViewMode = "stageFieldViewData.fieldDisplayMode", FieldViewSpacing = "stageFieldViewData.outlineCellSpacing";
        private const string FieldViewUnitScale = "stageFieldViewData.fieldUnitScale";
        private const string FieldGridMaterial = "stageFieldViewData.gridDecalMaterial";
        private const string FieldGridLineWidth = "stageFieldViewData.gridLineWidth";
        private const string FieldGridDepth = "stageFieldViewData.gridDecalDepth";
        private const string FieldGridLayer = "stageFieldViewData.gridDecalRenderingLayer";
        private const string UnitVisualPrefab = "unitVisualData.prefab", VisualPrefab = "prefab", VisualOffset = "localOffset";
        private const string VisualScale = "localScale", VisualSurface = "surfaceHeight", VisualMode = "visualMode", ReplaceTileMode = "ReplaceTile";
        // 지형 외형의 이웃 모양 칸 다섯입니다. 하나라도 차 있으면 이웃 모양 지형입니다(병합 대응 설계 MG1).
        private static readonly string[] EdgeShapeSlots =
            { "oneSidePrefab", "twoSidePrefab", "twoSideOppositePrefab", "threeSidePrefab", "fourSidePrefab" };

        private const string Key = "key", Kind = "kind", VisualName = "displayName";
        private const string UnitVisualKey = "unitVisualData.key", UnitVisualName = "unitVisualData.displayName";
        private const string StageKey = "stageKey", MapKey = "mapKey", MapRole = "mapRole", StageData = "stageData";
        private const string Seed = "timelineSeed", Field = "fieldDefinition", Roster = "unitRoster", Economy = "economy";
        private const string Objective = "objectiveKind", EnemyUnits = "initialEnemyPlacements", EnemyLoadout = "enemyLoadout";
        private const string EnemyLevel = "enemyLevel", StructureCatalog = "structureCatalog", Structures = "initialEnemyStructurePlacements";
        private const string MainAnchor = "mainCharacterAnchor", MainRotation = "mainCharacterRotation", Headquarters = "headquarters";
        private const string HeadquartersAnchor = "enemyHeadquartersAnchor", HeadquartersRotation = "enemyHeadquartersRotation";
        private const string Environment = "environment", Terrains = "terrainPlacements", Obstacles = "obstaclePlacements", Objects = "objectPlacements";
        private const string Platforms = "movingPlatforms";
        private const string CornerCut = "cornerCutTicks", RailwayEnabled = "railwayEnabled", Railway = "railway", RailCells = "railCells";
        private const string RailFirst = "firstActivationAfterControllerTurns", RailRepeat = "repeatIntervalControllerTurns";
        private const string Rockfall = "rockfall", RockfallEnabled = "enabled", RockfallCooldown = "cooldownControllerTurns", RockfallWarning = "warningControllerTurns";
        private const string RockfallMinimum = "minimumRockCount", RockfallMaximum = "maximumRockCount", RockfallHealthLoss = "maximumHealthLossPercent";
        private const string RockfallBind = "bindAffectedSideTurns", RockfallFootprint = "footprintOffsets";
        private const string Haboob = "haboob", HaboobEnabled = "enabled", HaboobInterval = "relocationIntervalControllerTurns";
        private const string HaboobWarning = "warningLeadControllerTurns", HaboobFootprint = "footprintOffsets";
        private const string HaboobFixedAnchor = "fixedInitialAnchor", HaboobAnchor = "initialAnchor";
        // 낙석 터널·능선 오브젝트 이름의 사본입니다. 원본은 게임 StageViewPropPresenter의 같은 이름 상수입니다(설계 §17 RF6, 낙석 View 설계 RG2).
        internal const string RockfallTunnelObjectName = "RockfallTunnelBoom";
        internal const string RockfallRidgeStartObjectName = "RockfallRidgeStart";
        internal const string RockfallRidgeEndObjectName = "RockfallRidgeEnd";
        // 게임이 맵 프리팹에서 하나만 허용하는 낙석 오브젝트 이름입니다. 둘 이상이면 낙석이 꺼져 있어도 전투 진입 때 멈춥니다.
        internal static readonly string[] RockfallUniqueObjectNames = { RockfallTunnelObjectName, RockfallRidgeStartObjectName, RockfallRidgeEndObjectName };
        private const string LegacyWalls = "initialWallCells", LegacyTerrains = "staticTerrainPlacements";
        private const string PlayerDeploymentCells = "playerDeploymentCells", SpawnCount = "spawnCount";
        // 이동·배치가 들어갈 수 없는 지형 종류 이름입니다(계획 P-AZT5). 원본은 Core EnvironmentTerrainRules.BlocksVoluntaryEntry(벽·낭떠러지)입니다.
        private static readonly string[] BlockingTerrainKinds = { "Wall", "Cliff" };
        private const string UnitDefinition = "unitDefinition", StructureDefinition = "structureDefinition", Definition = "definition";
        private const string Anchor = "anchor", Cell = "cell", Rotation = "rotation", VictoryTarget = "isVictoryTarget", StartingItem = "startingItem";
        private const string SourceId = "sourceId", KnownTo = "knownTo", RandomStart = "randomizeInitialPosition", X = "x", Y = "y";
        private const string Prop = "stageViewPropData", PropObject = "gameObject", PropMap = "map";
        private const string DisableRockfallTunnelExplosion = "disableRockfallTunnelExplosion";
        private const string TornadoSign = "tornadoInitialHorizontalSign", TornadoSide = "tornadoSideLength";
        private const string TornadoPresentation = "tornadoPresentation", TornadoBodyReference = "bodyReferenceCells";
        private const string PositionOffset = "positionOffset", RotationOffset = "rotationOffset", ScaleOffset = "scaleOffset";
        private const string TerrainRotations = "terrainRotations", ObjectRotations = "objectRotations";
        // 배경 방식과 바닥 타일입니다(설계 §12 UT1). 타일 배치(CombatMapTilesInput)와 타일 세트(CombatMapTileSetSO)는 같은 이름 "tiles"를 씁니다.
        internal const string TileSetType = "CombatMapTileSetSO";
        private const string ViewMode = "mode", MapTiles = "mapTiles", TileSet = "tileSet", TileColumns = "columns", TileRows = "rows";
        private const string TileOffset = "offset", AnchorTile = "anchorTile", Tiles = "tiles", TileRotations = "rotations", TilePrefabs = "prefabs";
        private const string TableItems = "databaseItems", FootprintWidth = "footprintWidth", FootprintHeight = "footprintHeight";
        private const string FieldColumns = "columnCount", FieldRows = "rowCount", PlayerDepth = "playerDeploymentDepth", EnemyDepth = "enemyDeploymentDepth";
        // 맵 생성 규칙 판정 재료입니다(병합 대응 설계 MG5). 맵 SO는 읽기만 합니다.
        private const string ShopDetailType = "ShopStageData", EventDetailType = "EventStageData", EnhancementDetailType = "EnhancementStageData";
        private const string MapTableType = "StageMapDataTableSO", RegularRole = "Regular";
        private const string MapDepth = "depthCount", MapWidth = "widthMax", MapWidthMin = "widthMin", MapSingleFirst = "singleFirstLayer", MapFirstStage = "firstStageKey";
        private const string MapSeed = "seed", MapConnection = "connectionAlgorithm";
        private const string MapNearest = "nearestConnectionWeight", MapOther = "otherConnectionWeight", MapChance = "additionalConnectionChance";
        private const string DistributionWeight = "weight", DistributionMinimum = "minimumCount", DistributionMinDepth = "minDepth";
        // 맵 SO 출현 칸 넷과 그 종류입니다. 원본은 StageMapDataSO.GetData(칸이 종류를 정함)이고 여기는 그 짝의 사본입니다.
        private static readonly (string Field, ChapterStageKind Kind)[] DistributionFields =
        {
            ("combatDistribution", ChapterStageKind.Combat), ("shopDistribution", ChapterStageKind.Shop),
            ("recoveryDistribution", ChapterStageKind.Recovery), ("enhancementDistribution", ChapterStageKind.Enhancement),
        };

        internal static CombatStageSoDocument Read(Object root, Object view, CombatStageSoNames names)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (names == null) throw new ArgumentNullException(nameof(names));
            var rootObject = new SerializedObject(root);
            Object combat = Require(rootObject, StageData).objectReferenceValue;
            if (!IsType(combat, CombatType))
                throw new InvalidOperationException($"{root.name}: 전투 데이터(CombatStageData)를 가리키지 않습니다.");
            var document = new CombatStageSoDocument
            {
                format = CombatStageSoDocument.CurrentFormat,
                rootGuid = GuidOf(root), combatGuid = GuidOf(combat), viewGuid = GuidOf(view),
                stageKey = Require(rootObject, StageKey).intValue,
                mapKey = Require(rootObject, MapKey).intValue,
                mapRole = EnumIndex(Require(rootObject, MapRole), ref names.mapRole),
            };
            ReadCombat(new SerializedObject(combat), document, names);
            if (view != null) ReadView(new SerializedObject(view), document.view, names);
            return document;
        }

        internal static int ViewStageKey(Object view) => Require(Require(new SerializedObject(view), Prop), StageKey).intValue;

        // 맵 작업실의 전투 구도 맥락입니다. SO를 고르기만 하며 값이나 프리팹은 바꾸지 않습니다.
        internal static (int StageKey, string Mode, GameObject Prefab) ReadStageViewPropSource(Object asset)
        {
            if (!IsType(asset, ViewType))
                throw new InvalidOperationException("운영 ViewStageDataSO 에셋을 선택하세요.");
            if (!RegisteredItems(ViewTableType).Contains(asset))
                throw new InvalidOperationException($"{asset.name}: 운영 ViewStage 표에 등록되지 않았습니다.");
            SerializedProperty prop = Require(new SerializedObject(asset), Prop);
            SerializedProperty mode = Require(prop, ViewMode);
            string modeName = NameAt(mode.enumNames, mode.enumValueIndex);
            if (modeName != "Prefab" && modeName != "MapDefinition" && modeName != "PrefabWithTiles")
                throw new InvalidOperationException($"{asset.name}: 지원하지 않는 배경 방식입니다: {modeName}.");
            GameObject prefab = modeName == "MapDefinition"
                ? (Require(prop, PropMap).objectReferenceValue as MapDefinitionSO)?.Prefab
                : Require(prop, PropObject).objectReferenceValue as GameObject;
            return (Require(prop, StageKey).intValue, modeName, prefab);
        }

        internal static CombatStageSoView ReadStagePreviewView(Object asset)
        {
            ReadStageViewPropSource(asset);
            var values = new CombatStageSoView();
            ReadView(new SerializedObject(asset), values, new CombatStageSoNames());
            return values;
        }

        // 작업실의 배경 보정 조작만 운영 ViewStage 에셋에 씁니다. 맵 프리팹은 수정하지 않습니다.
        internal static void WriteStagePreviewOffsets(Object asset, Vector3 position, Vector3 rotation, Vector3 scale,
            Vector3 tileOffset, Vector2Int anchorTile, bool tiles)
        {
            ReadStageViewPropSource(asset);
            Undo.RecordObject(asset, "전투 배경 보정");
            var values = new SerializedObject(asset);
            SerializedProperty prop = Require(values, Prop);
            Require(prop, PositionOffset).vector3Value = position;
            Require(prop, RotationOffset).vector3Value = rotation;
            if (!tiles) Require(prop, ScaleOffset).vector3Value = scale;
            if (tiles)
            {
                SerializedProperty mapTiles = Require(prop, MapTiles);
                Require(mapTiles, TileOffset).vector3Value = tileOffset;
                Require(mapTiles, AnchorTile).vector2IntValue = anchorTile;
            }
            values.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        // 선택한 StageKey의 전투 Field 원본을 읽습니다. 중복 키·비전투 Root는 임의로 고르지 않습니다.
        internal static (Object Root, CombatStageSoField Field) ReadStageField(int stageKey)
        {
            Object[] roots = RegisteredItems(RootTableType).Where(root => IsType(root, RootType) &&
                Require(new SerializedObject(root), StageKey).intValue == stageKey).ToArray();
            if (roots.Length != 1)
                throw new InvalidOperationException($"운영 StageRoot 표에서 StageKey {stageKey}의 전투 Root가 {roots.Length}개입니다. 정확히 하나가 필요합니다.");
            Object combat = Require(new SerializedObject(roots[0]), StageData).objectReferenceValue;
            if (!IsType(combat, CombatType))
                throw new InvalidOperationException($"{roots[0].name}: 전투 Stage 데이터가 아닙니다.");
            Object field = Require(new SerializedObject(combat), Field).objectReferenceValue;
            if (!IsType(field, FieldType))
                throw new InvalidOperationException($"{combat.name}: FieldDataSO 참조가 없습니다.");
            return (roots[0], ReadField(field));
        }

        // 작업본을 에셋(또는 저장 전 복제본)에 씁니다. 배열은 작업본 순서로 다시 만들고, 숨김 호환 필드는 건드리지 않습니다(설계 SM10).
        // Root의 stageData는 받은 combat을 가리키게 합니다. 복제본 검사 때 Root 복제본이 전투 데이터 복제본을 보게 하려는 것입니다(계획 P11).
        // resolve는 저장 전 검사에서 공유 SO 대신 그 편집 복제본을 가리킬 때 씁니다. null을 내면 에셋을 씁니다.
        internal static void Write(CombatStageSoDocument document, Object root, Object combat, Object view, Func<string, Object> resolve = null)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (combat == null) throw new ArgumentNullException(nameof(combat));
            Func<string, Object> load = guid => resolve?.Invoke(guid) ?? Load(guid);
            var rootValues = new SerializedObject(root);
            Require(rootValues, StageKey).intValue = document.stageKey;
            Require(rootValues, MapKey).intValue = document.mapKey;
            Require(rootValues, MapRole).enumValueIndex = document.mapRole;
            Require(rootValues, StageData).objectReferenceValue = combat;
            rootValues.ApplyModifiedPropertiesWithoutUndo();
            WriteCombat(new SerializedObject(combat), document, load);
            if (view != null) WriteView(new SerializedObject(view), document);
        }

        // 전용 복제를 할 수 있는 공유 SO의 키 필드와 표 형식입니다(설계 SM7). 정의 SO는 복제하지 않습니다(계획 P4).
        private static bool SharedSpec(Object asset, out string keyField, out string tableType)
        {
            switch (asset != null ? asset.GetType().Name : "")
            {
                case FieldType: keyField = FieldId; tableType = FieldTableType; return true;
                case RosterType: keyField = RosterKey; tableType = RosterTableType; return true;
                case EconomyType: keyField = EconomyKey; tableType = EconomyTableType; return true;
                case LoadoutType: keyField = LoadoutKey; tableType = LoadoutTableType; return true;
                default: keyField = tableType = null; return false;
            }
        }

        internal static string SharedTableType(Object asset) => SharedSpec(asset, out _, out string tableType) ? tableType : null;

        // 키 필드는 표 안에서 유일해야 해 편집 칸에서 막습니다.
        internal static bool IsSharedKeyProperty(Object asset, string propertyPath) =>
            SharedSpec(asset, out string keyField, out _) && propertyPath == keyField;

        // 복사한 공유 SO에 새 키를 씁니다. 필드 fieldId는 preferredId(겹치면 뒤에 번호), 나머지 정수 키는 같은 형식 에셋 키의 최댓값 + 1입니다.
        internal static void AssignNewSharedKey(Object copy, string preferredId)
        {
            if (!SharedSpec(copy, out string keyField, out _)) throw new InvalidOperationException($"{copy.name}: 전용 복제를 할 수 없는 SO입니다.");
            var others = FindAll(copy.GetType().Name).Where(asset => asset != copy).Select(asset => Require(new SerializedObject(asset), keyField));
            var values = new SerializedObject(copy);
            SerializedProperty key = Require(values, keyField);
            if (key.propertyType == SerializedPropertyType.String)
            {
                var used = new HashSet<string>(others.Select(property => property.stringValue));
                string id = preferredId;
                for (int suffix = 2; used.Contains(id); suffix++) id = preferredId + "_" + suffix;
                key.stringValue = id;
            }
            else key.intValue = others.Select(property => property.intValue).DefaultIfEmpty(0).Max() + 1;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static string SharedKeyText(Object asset)
        {
            if (!SharedSpec(asset, out string keyField, out _)) return "";
            SerializedProperty key = Require(new SerializedObject(asset), keyField);
            return key.propertyType == SerializedPropertyType.String ? key.stringValue : key.intValue.ToString();
        }

        // 적 배치 깊이는 쓰지 않습니다. Core가 읽지 않는 값이라 복사한 틀 필드 값을 그대로 둡니다(설계 P-AZ8).
        internal static void WriteFieldValues(Object field, int columns, int rows, int reserve, int playerDepth)
        {
            var values = new SerializedObject(field);
            Require(values, FieldColumns).intValue = columns;
            Require(values, FieldRows).intValue = rows;
            Require(values, Reserve).intValue = reserve;
            Require(values, PlayerDepth).intValue = playerDepth;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        // 필드의 아군 줄 수(아래부터)만 씁니다. 적 줄 수와 다른 필드 값은 그대로입니다(계획 P-AZT9).
        internal static void WritePlayerDepth(Object field, int playerDepth)
        {
            var values = new SerializedObject(field);
            Require(values, PlayerDepth).intValue = playerDepth;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        // 아군 배치 구역입니다(설계 §11 AZ2). 칸 목록이 있으면 그 칸, 비면 필드 아군 줄 수(아래부터)입니다.
        // 이 갈래가 BattleDefinitionAdapter.cs:90-92 대체 규칙의 사본이고, 도구에 남는 유일한 구역 사본입니다(설계 P-AZ10).
        internal static CombatStageSoZone ZoneOf(IReadOnlyCollection<Vector2Int> cells, CombatStageSoField field)
        {
            if (cells != null && cells.Count > 0)
            {
                // 중복은 합쳐서 그립니다. 중복이 든 에셋은 저장 검사(GetData → PlayerDeploymentZone.Of)가 막습니다(계획 P-AZT1).
                var distinct = cells.Distinct().ToList();
                return new CombatStageSoZone(PlayerDeploymentZone.Of(distinct.Select(cell => new CellCoord(cell.x, cell.y))), distinct.Count, 0);
            }
            int depth = field != null ? Mathf.Max(0, field.PlayerDepth) : 0;
            return new CombatStageSoZone(PlayerDeploymentZone.Rows(depth), 0, depth);
        }

        // 지형 정의가 벽·낭떠러지인지입니다(설계 P-AZ9). kind 열거 이름으로 보고 Systems 열거는 참조하지 않습니다(계획 P-AZT5).
        // 원본은 Core EnvironmentTerrainRules.BlocksVoluntaryEntry(출현 후보·초기 배치가 같이 씀)이고 여기는 그 사본입니다. 지형 정의가 아니면 거짓입니다.
        internal static bool BlocksEntry(Object definition)
        {
            if (!IsType(definition, TerrainType)) return false;
            SerializedProperty kind = Require(new SerializedObject(definition), Kind);
            return BlockingTerrainKinds.Contains(NameAt(kind.enumNames, kind.enumValueIndex));
        }

        // 적 로드아웃의 출현 수입니다(설계 §11 AZ6). 로드아웃이 없으면 null입니다.
        internal static int? SpawnCountOf(Object loadout) =>
            loadout == null ? (int?)null : Require(new SerializedObject(loadout), SpawnCount).intValue;

        // View가 없던 스테이지에 새 View를 붙일 때, 작업본의 다른 편집은 두고 View 값만 읽어 옵니다.
        internal static void ReadViewInto(Object view, CombatStageSoDocument document, CombatStageSoNames names)
        {
            ReadView(new SerializedObject(view), document.view, names);
            document.viewGuid = GuidOf(view);
        }

        // 새로 복사한 View가 틀 스테이지와 같은 키로 남지 않게 키만 바로 씁니다(같은 키 View가 둘이면 틀 스테이지를 열 수 없음, 계획 P12).
        internal static void WriteViewStageKey(Object view, int stageKey)
        {
            var values = new SerializedObject(view);
            Require(Require(values, Prop), StageKey).intValue = stageKey;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        // "배치 비우고 만들기"는 레이아웃 외부에서 저작한 배치도 비웁니다.
        internal static void ClearPlacementsOutsideLayout(Object combat)
        {
            var values = new SerializedObject(combat);
            Require(values, LegacyWalls).arraySize = 0;
            Require(values, LegacyTerrains).arraySize = 0;
            Require(Require(values, Environment), Platforms).arraySize = 0;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static bool TableContains(Object table, Object item) =>
            Elements(Require(new SerializedObject(table), TableItems)).Any(element => element.objectReferenceValue == item);

        internal static void AddTableItem(Object table, Object item)
        {
            if (TableContains(table, item)) return;
            var values = new SerializedObject(table);
            SerializedProperty items = Require(values, TableItems);
            items.arraySize++;
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = item;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void RemoveTableItem(Object table, Object item)
        {
            var values = new SerializedObject(table);
            SerializedProperty items = Require(values, TableItems);
            for (int i = items.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty element = items.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != item) continue;
                // 오브젝트 참조 칸은 비운 뒤에 지워야 칸 자체가 빠집니다.
                element.objectReferenceValue = null;
                items.DeleteArrayElementAtIndex(i);
            }
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        // 공개 대상처럼 값이 없으면 이름을 읽을 수 없는 열거도 있어, 저장하지 않는 복제본에 칸을 하나 만들어 이름을 읽습니다(계획 P10).
        internal static void FillNames(Object root, Object combat, CombatStageSoNames names)
        {
            names.mapRole = Require(new SerializedObject(root), MapRole).enumNames;
            Object copy = Object.Instantiate(combat);
            try
            {
                var values = new SerializedObject(copy);
                names.rotation = Require(values, MainRotation).enumNames;
                names.objective = Require(values, Objective).enumNames;
                SerializedProperty terrains = Require(Require(values, Environment), Terrains);
                terrains.arraySize = 1;
                SerializedProperty knownTo = Require(terrains.GetArrayElementAtIndex(0), KnownTo);
                knownTo.arraySize = 1;
                names.owner = knownTo.GetArrayElementAtIndex(0).enumNames;
            }
            finally { Object.DestroyImmediate(copy); }
        }

        // FU-a: 저장 직전 복제본에서만 읽습니다. 공개 대상은 배치 금지 판정과 무관합니다.
        internal static List<CombatStageSoIssue> CheckFixedPlacements(Object root, Object combat, Object view)
        {
            var names = new CombatStageSoNames();
            var document = Read(root, view, names);
            var values = new SerializedObject(combat);
            var field = ReadField(Require(values, Field).objectReferenceValue);
            if (field == null) throw new InvalidOperationException("배치 검사에 필요한 필드가 없습니다.");
            var grid = new CombatStageSoGrid(field.Columns, field.Rows,
                guid => Footprint(Load(guid)), names.IsQuarterTurn);
            var walls = new HashSet<Vector2Int>(Elements(Require(values, LegacyWalls)).Select(ReadCell));
            var cliffs = new HashSet<Vector2Int>();
            foreach (var legacy in Elements(Require(values, LegacyTerrains)))
            {
                var kind = Require(legacy, Kind);
                string name = NameAt(kind.enumNames, kind.enumValueIndex);
                if (name == "Wall") walls.Add(ReadCell(Require(legacy, Cell)));
                if (name == "Cliff") cliffs.Add(ReadCell(Require(legacy, Cell)));
            }
            foreach (var terrain in document.terrains.Where(marker => !marker.randomizeInitialPosition))
            {
                Object definition = Load(terrain.definitionGuid);
                if (definition == null) throw new InvalidOperationException("배치 검사에서 지형 정의를 찾을 수 없습니다.");
                var kind = Require(new SerializedObject(definition), Kind);
                string name = NameAt(kind.enumNames, kind.enumValueIndex);
                var cell = new Vector2Int(terrain.x, terrain.y);
                if (name == "Wall") walls.Add(cell);
                if (name == "Cliff") cliffs.Add(cell);
            }
            var targets = document.Placements(grid).Where(p => p.Layer == CombatStageSoLayer.Unit ||
                p.Layer == CombatStageSoLayer.Structure || p.Layer == CombatStageSoLayer.Headquarters)
                .Select(p => (Label: CombatStageSoDocument.LayerName(p.Layer) + " " + NameOf(document.DefinitionOf(p.Item)), Cells: p.Cells, Target: CombatStageSoIssueTarget.Of(document, p.Item)));
            var supported = new HashSet<Vector2Int>();
            if (document.platformSourceIds.Count > 0)
            {
                // 형상 변환과 초기 바닥 검증은 SO의 기존 GetData 경계가 맡습니다.
                var data = ((DatabaseItem)combat).GetData();
                var platforms = data.GetType().GetProperty("MovingPlatforms")?.GetValue(data) as IReadOnlyList<MovingPlatformSpec>
                    ?? throw new InvalidOperationException("스테이지 데이터에 플랫폼 정의 목록이 없습니다.");
                foreach (var platform in platforms)
                    foreach (var cell in platform.FootprintAt(platform.InitialAnchor))
                        supported.Add(new Vector2Int(cell.X, cell.Y));
            }
            return CombatStageSoPlacementChecks.Check(targets, walls, cliffs, supported);
        }

        // 지형 종류 이름은 직렬화된 원본에서 읽습니다. 정의가 빈 배치는 토네이도가 아닌 것으로 답하고, SO 검사가 빈 정의를 알립니다.
        internal static bool IsTornado(string guid)
        {
            Object definition = Load(guid);
            if (definition == null) return false;
            var kind = Require(new SerializedObject(definition), Kind);
            return NameAt(kind.enumNames, kind.enumValueIndex) == "Tornado";
        }

        // 토네이도 정의의 판정 칸 수입니다(한 변). 정의가 없으면 IsTornado처럼 멈춥니다.
        internal static int TornadoSideLength(string guid)
        {
            Object definition = Load(guid);
            if (definition == null) throw new InvalidOperationException("토네이도 정의를 찾을 수 없습니다: " + guid);
            return Require(new SerializedObject(definition), TornadoSide).intValue;
        }

        // FieldTornadoPresenter.Create의 ratio 사본입니다: 판정 칸 수 ÷ 프리팹 기준 칸 수. 외형이나 연출 설정이 없으면 1입니다.
        // 같은 종류의 외형이 여럿이면 VisualTable처럼 첫 에셋을 씁니다.
        internal static float TornadoBodyRatio(string guid)
        {
            int kind = VisualId(Load(guid));
            foreach (Object visual in FindAll(TerrainVisualType))
            {
                var values = new SerializedObject(visual);
                if (Require(values, Kind).intValue != kind) continue;
                Object settings = Require(values, TornadoPresentation).objectReferenceValue;
                if (settings == null) return 1f;
                return TornadoSideLength(guid) / (float)Require(new SerializedObject(settings), TornadoBodyReference).intValue;
            }
            return 1f;
        }

        // 고정 중심 토네이도의 저장 문제입니다. 원본은 Core TornadoGimmickPreparationCalculator.Prepare의 입력 검사이고, 지금은 전투 진입 때에야 멈춰 도구가 미리 막습니다.
        // 판에 들어가는지는 Core 함수를 그대로 부르고, 안쪽 중심의 방향 규칙만 사본입니다(Core 쪽이 internal). 짝수 칸 수는 SO 검사가 알립니다.
        internal static string TornadoProblem(CombatStageSoMarker marker, int columns, int rows)
        {
            int side = TornadoSideLength(marker.definitionGuid);
            if (side < 1 || side % 2 == 0) return null;
            if (columns < side || rows < side) return $"토네이도 {side}×{side}이(가) {columns}×{rows} 판에 들어가지 않습니다 (sourceId {marker.sourceId}).";
            if (marker.randomizeInitialPosition) return null;
            if (!TornadoGeometry.Fits(new CellCoord(marker.x, marker.y), side, columns, rows))
                return $"토네이도 {side}×{side}이(가) 판 밖으로 나갑니다. 중심을 안쪽으로 옮겨 주세요: ({marker.x}, {marker.y}).";
            int radius = side / 2;
            if (marker.x != radius && marker.x != columns - 1 - radius && marker.tornadoHorizontalSign == 0)
                return $"가장자리가 아닌 중심의 토네이도는 가로 방향을 정해야 합니다: ({marker.x}, {marker.y}).";
            return null;
        }

        internal static CombatStageSoField ReadField(Object field)
        {
            if (field == null) return null;
            var values = new SerializedObject(field);
            return new CombatStageSoField(Require(values, FieldColumns).intValue, Require(values, FieldRows).intValue,
                Require(values, PlayerDepth).intValue, Require(values, EnemyDepth).intValue, Require(values, Reserve).intValue);
        }

        internal static Vector2Int Footprint(Object definition)
        {
            if (definition == null) return Vector2Int.one;
            var values = new SerializedObject(definition);
            SerializedProperty width = values.FindProperty(FootprintWidth), height = values.FindProperty(FootprintHeight);
            // 지형·오브젝트 정의에는 크기 칸이 없고 한 칸을 차지합니다.
            return width == null || height == null ? Vector2Int.one : new Vector2Int(Mathf.Max(1, width.intValue), Mathf.Max(1, height.intValue));
        }

        // 전투 스테이지 목록을 내고, 스테이지 키 중복 검사를 위해 모든 Root(상점·이벤트 포함)의 키를 모읍니다.
        // 한국어 이름은 View DB 외형 표의 displayName에서 읽습니다. 게임 화면이 쓰는 이름과 같습니다.
        // 유닛 외형 표는 유닛·구조물·장애물을 키로, 지형·오브젝트 외형 표는 종류로 찾습니다.
        internal static Dictionary<int, string> UnitVisualNames() => VisualNames(UnitVisualType, UnitVisualKey, UnitVisualName);
        internal static Dictionary<int, string> TerrainVisualNames() => VisualNames(TerrainVisualType, Kind, VisualName);
        internal static Dictionary<int, string> ObjectVisualNames() => VisualNames(ObjectVisualType, Kind, VisualName);

        // 정의 SO를 외형 표에서 찾는 번호입니다. 장애물 정의에는 키가 저장돼 있지 않아 Core 규칙으로 종류에서 구합니다.
        internal static int VisualId(Object definition)
        {
            var values = new SerializedObject(definition);
            switch (definition.GetType().Name)
            {
                case UnitType:
                case StructureType:
                    return Require(values, Key).intValue;
                case ObstacleType:
                    return (int)ObstacleContentProfile.KeyFor((ObstacleContentKind)Require(values, Kind).intValue);
                default:
                    return Require(values, Kind).intValue;
            }
        }

        // 3D 미리보기용 외형 표입니다(추가 설계 AD1). 유닛 외형은 키(유닛·구조물·장애물), 지형·오브젝트 외형은 종류로 찾습니다.
        internal static Dictionary<int, CombatStageSoVisual> UnitVisuals() =>
            VisualTable(UnitVisualType, UnitVisualKey, UnitVisualPrefab, null, null, null, null);

        internal static Dictionary<int, CombatStageSoVisual> TerrainVisuals() =>
            VisualTable(TerrainVisualType, Kind, VisualPrefab, VisualOffset, VisualScale, VisualSurface, VisualMode);

        internal static Dictionary<int, CombatStageSoVisual> ObjectVisuals() =>
            VisualTable(ObjectVisualType, Kind, VisualPrefab, VisualOffset, VisualScale, null, null);

        // 이웃 모양을 쓰는 지형 종류입니다(병합 대응 설계 MG1). 원본은 게임 TerrainEdgeShapePrefabs.HasAny(모양 칸이 하나라도 있음)이고
        // 여기는 그 조건의 사본입니다. 그런 지형은 이웃이 모양과 회전을 정해, 외형 회전이 적혀 있으면 전투 화면이 멈춥니다(CombatTerrainLook.Resolve).
        // 같은 종류의 외형이 여럿이면 VisualTable처럼 이름순 첫 에셋을 씁니다(MG-P1).
        internal static HashSet<int> EdgeShapeTerrainKinds()
        {
            var kinds = new HashSet<int>();
            var seen = new HashSet<int>();
            foreach (Object visual in FindAll(TerrainVisualType))
            {
                var values = new SerializedObject(visual);
                int kind = Require(values, Kind).intValue;
                if (!seen.Add(kind)) continue;
                if (EdgeShapeSlots.Any(slot => Require(values, slot).objectReferenceValue != null)) kinds.Add(kind);
            }
            return kinds;
        }

        // 지형 정의가 이웃 모양 지형인지입니다. 지형 정의가 아니면 거짓입니다.
        internal static bool UsesEdgeShapes(string definitionGuid, ICollection<int> edgeKinds)
        {
            Object definition = Load(definitionGuid);
            return IsType(definition, TerrainType) && edgeKinds.Contains(VisualId(definition));
        }

        // 필드 보기 값입니다. GridOutline이면 칸 크기는 outlineCellSpacing이고, CellPrefab이면 1×1로 근사합니다(추가 계획 P-A10).
        internal static CombatStageSoFieldView ReadFieldView()
        {
            Object asset = FindAll(FieldViewType).FirstOrDefault();
            if (asset == null) return new CombatStageSoFieldView(1f, 1f, 1f, true);
            return ReadFieldView(asset);
        }

        // 전투 런타임과 같은 설정형 표의 유일한 필드 보기 값을 읽습니다.
        internal static CombatStageSoFieldView ReadRegisteredFieldView()
        {
            return ReadFieldView(RegisteredFieldViewAsset());
        }

        /// <summary>편집용 격자의 재질·선 폭·투영 깊이를 운영 Field View SO에서 읽습니다. 운영 표시 스위치는 바꾸지 않습니다.</summary>
        internal static (Material Material, float LineWidth, float Depth, uint RenderingLayers) ReadRegisteredGridDecal()
        {
            var values = new SerializedObject(RegisteredFieldViewAsset());
            SerializedProperty mode = Require(values, FieldViewMode);
            if (NameAt(mode.enumNames, mode.enumValueIndex) != "GridOutline")
                throw new InvalidOperationException("편집용 필드 그리드는 GridOutline 방식의 칸 간격이 필요합니다.");
            Material material = Require(values, FieldGridMaterial).objectReferenceValue as Material;
            float lineWidth = Require(values, FieldGridLineWidth).floatValue;
            float depth = Require(values, FieldGridDepth).floatValue;
            string layerName = Require(values, FieldGridLayer).stringValue;
            int layer = string.IsNullOrEmpty(layerName) ? -1 : RenderingLayerMask.NameToRenderingLayer(layerName);
            if (material == null || !float.IsFinite(lineWidth) || lineWidth <= 0f
                || !float.IsFinite(depth) || depth <= 0f || layer < 0 || layer >= 32)
                throw new InvalidOperationException("운영 Field View의 격자 데칼 재질·선 폭·깊이·렌더링 레이어가 올바르지 않습니다.");
            return (material, lineWidth, depth, 1u << layer);
        }

        private static Object RegisteredFieldViewAsset()
        {
            Object[] assets = RegisteredItems(FieldViewTableType).Where(asset => IsType(asset, FieldViewType)).ToArray();
            if (assets.Length != 1)
                throw new InvalidOperationException($"운영 StageFieldView 표에 등록된 설정이 {assets.Length}개입니다. 정확히 하나가 필요합니다.");
            return assets[0];
        }

        private static CombatStageSoFieldView ReadFieldView(Object asset)
        {
            var values = new SerializedObject(asset);
            SerializedProperty mode = Require(values, FieldViewMode);
            float unitScale = Require(values, FieldViewUnitScale).floatValue;
            if (NameAt(mode.enumNames, mode.enumValueIndex) != "GridOutline") return new CombatStageSoFieldView(1f, 1f, unitScale, true);
            Vector2 spacing = Require(values, FieldViewSpacing).vector2Value;
            if (!float.IsFinite(spacing.x) || !float.IsFinite(spacing.y) || spacing.x <= 0f || spacing.y <= 0f)
                throw new InvalidOperationException($"{asset.name}: OutlineCellSpacing은 양의 유한 값이어야 합니다: {spacing}");
            return new CombatStageSoFieldView(spacing.x, spacing.y, unitScale, false);
        }

        // 적 배치에 쓸 수 있는 유닛 정의인지입니다. 원본은 Core UnitEligibilityRules.Require(definition, Owner.Enemy, false)
        // (BattlePreparationCalculator가 적 배치마다 부름)이고, 여기는 그 조건의 사본입니다: 일반 역할이고 아군 전용이 아님.
        // 아니면 전투 진입 때 Core가 멈춥니다.
        internal static bool CanBeEnemy(Object unit)
        {
            var values = new SerializedObject(unit);
            return (UnitRole)Require(values, UnitRoleField).intValue == UnitRole.Regular &&
                (UnitOwnerEligibility)Require(values, UnitOwnerField).intValue != UnitOwnerEligibility.PlayerOnly;
        }

        // 공격 범위 미리보기용 값입니다(추가 설계 AD2). 지정 방식 SO가 없으면 false입니다.
        // 모양·스플래시는 Core 함수에 그대로 넘기므로 열거의 실제 값(intValue)을 냅니다.
        internal static bool ReadAttack(Object unit, out int range, out int shape, out int splash, out int width)
        {
            var values = new SerializedObject(unit);
            range = Require(values, AttackRange).intValue;
            shape = splash = width = 0;
            Object method = Require(values, TargetingMethod).objectReferenceValue;
            if (method == null) return false;
            var methodValues = new SerializedObject(method);
            shape = Require(methodValues, Shape).intValue;
            splash = Require(methodValues, Splash).intValue;
            width = Require(methodValues, TargetingWidth).intValue;
            return true;
        }

        // 에셋 이름의 첫 '_' 앞(Terrain_, Unit_ 등)을 뗀 짧은 이름입니다.
        internal static string ShortAssetName(string name)
        {
            int separator = name.IndexOf('_');
            return separator >= 0 && separator < name.Length - 1 ? name.Substring(separator + 1) : name;
        }

        internal static List<CombatStageSoEntry> FindStages(HashSet<Object> registeredRoots, Dictionary<string, int> rootKeys)
        {
            var stages = new List<CombatStageSoEntry>();
            foreach (Object root in FindAll(RootType))
            {
                var values = new SerializedObject(root);
                rootKeys[GuidOf(root)] = Require(values, StageKey).intValue;
                Object combat = Require(values, StageData).objectReferenceValue;
                if (!IsType(combat, CombatType)) continue;
                SerializedProperty role = Require(values, MapRole);
                stages.Add(new CombatStageSoEntry(root, combat, Require(values, StageKey).intValue, Require(values, MapKey).intValue,
                    NameAt(role.enumNames, role.enumValueIndex), registeredRoots.Contains(root)));
            }
            return stages.OrderBy(stage => stage.StageKey).ToList();
        }

        // 표에 든 Root 모두의 맵 판정 재료입니다(MG5). 상점·이벤트·강화 Root도 셉니다. 저장된 에셋 기준입니다.
        internal static List<CombatStageSoChapterStage> ReadChapterStages() =>
            RegisteredItems(RootTableType).Where(item => IsType(item, RootType)).Select(ReadChapterStage)
                .OrderBy(stage => stage.StageKey).ToList();

        internal static CombatStageSoChapterStage ReadChapterStage(Object root)
        {
            var values = new SerializedObject(root);
            SerializedProperty role = Require(values, MapRole);
            Object detail = Require(values, StageData).objectReferenceValue;
            return new CombatStageSoChapterStage(GuidOf(root), root.name, Require(values, StageKey).intValue, Require(values, MapKey).intValue,
                IsBossRole(NameAt(role.enumNames, role.enumValueIndex)), KindOfDetail(detail), detail != null ? detail.GetType().Name : "(없음)");
        }

        // 사본(StageGeneratorSystem.PrepareGraph): 역할이 Regular면 일반 노드, 그 밖은 보스입니다. 이름으로 봅니다(계획 P-MG3).
        internal static bool IsBossRole(string roleName) => roleName != RegularRole;

        // 사본(StageGeneratorSystem.KindOf): 게임은 상세 Data 형식으로 종류를 고릅니다. 상세 SO 넷이 상세 Data 넷과 하나씩 짝이라
        // 여기서는 SO 형식으로 고릅니다. 넷이 아니면 null이고, 게임도 그 경우 예외입니다(MG-P5).
        private static ChapterStageKind? KindOfDetail(Object detail)
        {
            switch (detail != null ? detail.GetType().Name : "")
            {
                case CombatType: return ChapterStageKind.Combat;
                case ShopDetailType: return ChapterStageKind.Shop;
                case EventDetailType: return ChapterStageKind.Recovery;
                case EnhancementDetailType: return ChapterStageKind.Enhancement;
                default: return null;
            }
        }

        // 맵 표에 든 맵 SO의 생성 규칙입니다(MG5). 맵 하나를 읽지 못하면 그 맵만 오류로 남기고 나머지는 읽습니다.
        internal static List<CombatStageSoMapRules> ReadMapRules()
        {
            var maps = new List<CombatStageSoMapRules>();
            foreach (Object map in RegisteredItems(MapTableType))
            {
                var values = new SerializedObject(map);
                int mapKey = values.FindProperty(MapKey)?.intValue ?? 0;
                try
                {
                    SerializedProperty connection = Require(values, MapConnection);
                    var distributions = DistributionFields.Select(field =>
                    {
                        SerializedProperty input = Require(values, field.Field);
                        return new CombatStageSoDistribution(field.Kind, Require(input, DistributionWeight).intValue, Require(input, DistributionMinimum).intValue,
                            Require(input, DistributionMinDepth).intValue);
                    }).ToList();
                    maps.Add(new CombatStageSoMapRules(mapKey, map.name, Require(values, MapDepth).intValue, Require(values, MapWidth).intValue,
                        Require(values, MapWidthMin).intValue, Require(values, MapSingleFirst).boolValue, Require(values, MapFirstStage).intValue, Require(values, MapSeed).intValue, NameAt(connection.enumNames, connection.enumValueIndex), Require(values, MapNearest).floatValue,
                        Require(values, MapOther).floatValue, Require(values, MapChance).floatValue, distributions));
                }
                catch (Exception exception) { maps.Add(CombatStageSoMapRules.Unreadable(mapKey, map.name, exception.Message)); }
            }
            return maps.OrderBy(map => map.MapKey).ToList();
        }

        internal static Dictionary<int, List<Object>> ViewsByStageKey()
        {
            var views = new Dictionary<int, List<Object>>();
            foreach (Object view in FindAll(ViewType))
            {
                int key = ViewStageKey(view);
                if (!views.TryGetValue(key, out var list)) views[key] = list = new List<Object>();
                list.Add(view);
            }
            return views;
        }

        internal static HashSet<Object> RegisteredItems(string tableType)
        {
            var items = new HashSet<Object>();
            foreach (Object table in FindAll(tableType))
                foreach (SerializedProperty element in Elements(Require(new SerializedObject(table), TableItems)))
                    if (element.objectReferenceValue != null) items.Add(element.objectReferenceValue);
            return items;
        }

        // 공유 SO마다 그것을 가리키는 전투 데이터(GUID)들입니다. 공용 카메라 설정은 이 목록에 포함하지 않습니다.
        // 저장된 에셋 기준이라, 작업본에서 바꾼 참조는 저장한 뒤에 반영됩니다.
        internal static Dictionary<string, HashSet<string>> ShareUsers()
        {
            var users = new Dictionary<string, HashSet<string>>();
            void Add(Object asset, string user)
            {
                string guid = GuidOf(asset);
                if (guid.Length == 0) return;
                if (!users.TryGetValue(guid, out var set)) users[guid] = set = new HashSet<string>();
                set.Add(user);
            }
            foreach (Object combat in FindAll(CombatType))
            {
                var values = new SerializedObject(combat);
                foreach (string name in new[] { Field, Roster, Economy, EnemyLoadout, Headquarters })
                    Add(Require(values, name).objectReferenceValue, GuidOf(combat));
            }
            return users;
        }

        internal static List<Object> FindAll(string typeName) =>
            AssetDatabase.FindAssets("t:" + typeName, new[] { "Assets" })
                .Select(found => AssetDatabase.GUIDToAssetPath(found)).Distinct()
                .Select(path => AssetDatabase.LoadMainAssetAtPath(path))
                .Where(asset => IsType(asset, typeName))
                .OrderBy(asset => asset.name, StringComparer.Ordinal).ToList();

        internal static string GuidOf(Object asset) =>
            asset != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _) ? guid : "";

        internal static Object Load(string guid) =>
            string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid));

        internal static string NameOf(string guid) =>
            string.IsNullOrEmpty(guid) ? "" : Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));

        internal static bool IsType(Object asset, string typeName) => asset != null && asset.GetType().Name == typeName;

        internal static string NameAt(string[] names, int index) =>
            names != null && index >= 0 && index < names.Length ? names[index] : $"(값 {index})";

        // 경로가 null인 값은 기본값(오프셋 0·배율 1·표면 높이 0)입니다. 같은 번호의 외형이 여럿이면 이름순 첫 에셋을 씁니다.
        // modePath는 지형 외형 방식 칸입니다. 열거 이름이 ReplaceTile이면 바닥 교체입니다(게임 TerrainVisualMode, 계획 P-UTT6).
        private static Dictionary<int, CombatStageSoVisual> VisualTable(string visualType, string keyPath, string prefabPath,
            string offsetPath, string scalePath, string surfacePath, string modePath)
        {
            var visuals = new Dictionary<int, CombatStageSoVisual>();
            foreach (Object visual in FindAll(visualType))
            {
                var values = new SerializedObject(visual);
                int id = Require(values, keyPath).intValue;
                if (visuals.ContainsKey(id)) continue;
                SerializedProperty mode = modePath == null ? null : Require(values, modePath);
                visuals.Add(id, new CombatStageSoVisual(
                    Require(values, prefabPath).objectReferenceValue as GameObject,
                    offsetPath == null ? Vector3.zero : Require(values, offsetPath).vector3Value,
                    scalePath == null ? Vector3.one : Require(values, scalePath).vector3Value,
                    surfacePath == null ? 0f : Require(values, surfacePath).floatValue,
                    mode != null && NameAt(mode.enumNames, mode.enumValueIndex) == ReplaceTileMode));
            }
            return visuals;
        }

        private static Dictionary<int, string> VisualNames(string visualType, string keyPath, string namePath)
        {
            var names = new Dictionary<int, string>();
            foreach (Object visual in FindAll(visualType))
            {
                var values = new SerializedObject(visual);
                int id = Require(values, keyPath).intValue;
                string name = Require(values, namePath).stringValue;
                // 같은 번호의 외형이 여럿이면 이름순 첫 에셋을 씁니다.
                if (!string.IsNullOrWhiteSpace(name) && !names.ContainsKey(id)) names.Add(id, name.Trim());
            }
            return names;
        }

        private static void ReadCombat(SerializedObject combat, CombatStageSoDocument document, CombatStageSoNames names)
        {
            document.timelineSeed = Require(combat, Seed).intValue;
            document.fieldGuid = Reference(combat, Field);
            document.rosterGuid = Reference(combat, Roster);
            document.economyGuid = Reference(combat, Economy);
            document.objectiveKind = EnumIndex(Require(combat, Objective), ref names.objective);
            document.enemyLoadoutGuid = Reference(combat, EnemyLoadout);
            document.enemyLevel = Require(combat, EnemyLevel).intValue;
            document.headquartersGuid = Reference(combat, Headquarters);
            document.cornerCutTicks = Require(combat, CornerCut).intValue;
            document.structureCatalog = Elements(Require(combat, StructureCatalog)).Select(e => GuidOf(e.objectReferenceValue)).ToList();
            document.enemyUnits = Elements(Require(combat, EnemyUnits)).Select(e => ReadUnit(e, names)).ToList();
            document.structures = Elements(Require(combat, Structures)).Select(e => ReadPiece(e, StructureDefinition, names)).ToList();
            document.mainCharacter = ReadPose(Require(combat, MainAnchor), Require(combat, MainRotation), names);
            document.enemyHeadquarters = ReadPose(Require(combat, HeadquartersAnchor), Require(combat, HeadquartersRotation), names);
            SerializedProperty environment = Require(combat, Environment);
            document.terrains = Elements(Require(environment, Terrains)).Select(e => ReadMarker(e, true, names)).ToList();
            document.obstacles = Elements(Require(environment, Obstacles)).Select(e => ReadPiece(e, Definition, names)).ToList();
            document.objects = Elements(Require(environment, Objects)).Select(e => ReadMarker(e, false, names)).ToList();
            document.platformSourceIds = Elements(Require(environment, Platforms))
                .Select(element => Require(element, SourceId).longValue).ToList();
            document.railwayEnabled = Require(combat, RailwayEnabled).boolValue;
            SerializedProperty railway = Require(combat, Railway);
            document.railCells = Elements(Require(railway, RailCells)).Select(ReadCell).ToList();
            document.railFirstActivation = Require(railway, RailFirst).intValue;
            document.railRepeatInterval = Require(railway, RailRepeat).intValue;
            ReadHaboob(combat, document);
            SerializedProperty rockfall = Require(combat, Rockfall);
            document.rockfall = new CombatStageSoRockfall
            {
                enabled = Require(rockfall, RockfallEnabled).boolValue,
                cooldownTurns = Require(rockfall, RockfallCooldown).intValue,
                warningTurns = Require(rockfall, RockfallWarning).intValue,
                minimumRocks = Require(rockfall, RockfallMinimum).intValue,
                maximumRocks = Require(rockfall, RockfallMaximum).intValue,
                maximumHealthLossPercent = Require(rockfall, RockfallHealthLoss).intValue,
                bindTurns = Require(rockfall, RockfallBind).intValue,
                footprintOffsets = Elements(Require(rockfall, RockfallFootprint)).Select(ReadCell).ToList()
            };
            document.playerDeploymentCells = Elements(Require(combat, PlayerDeploymentCells)).Select(ReadCell).ToList();
            document.legacyWallCount = Require(combat, LegacyWalls).arraySize;
            document.legacyTerrainSourceIds = Elements(Require(combat, LegacyTerrains)).Select(e => Require(e, SourceId).longValue).ToList();
        }

        private static void ReadHaboob(SerializedObject combat, CombatStageSoDocument document)
        {
            SerializedProperty haboob = Require(combat, Haboob);
            document.haboob = new CombatStageSoHaboob
            {
                enabled = Require(haboob, HaboobEnabled).boolValue,
                relocationIntervalTurns = Require(haboob, HaboobInterval).intValue,
                warningLeadTurns = Require(haboob, HaboobWarning).intValue,
                footprintOffsets = Elements(Require(haboob, HaboobFootprint)).Select(ReadCell).ToList(),
                fixedInitialAnchor = Require(haboob, HaboobFixedAnchor).boolValue,
                initialAnchor = ReadCell(Require(haboob, HaboobAnchor))
            };
        }

        // 검사용 복제본만 끕니다. 이미 모은 하붑 오류를 GetData에서 다시 보고하지 않게 합니다.
        internal static void DisableHaboob(Object combatCopy)
        {
            var values = new SerializedObject(combatCopy);
            Require(Require(values, Haboob), HaboobEnabled).boolValue = false;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        // 작업본 승리 조건이 본부 파괴인지 봅니다. combat은 열거 이름을 읽는 데만 씁니다. 원본은 Core BattleObjectiveKind입니다.
        internal static bool IsHeadquartersObjective(CombatStageSoDocument document, Object combat) =>
            NameAt(Require(new SerializedObject(combat), Objective).enumNames, document.objectiveKind) == "EnemyHeadquartersDestroyed";

        // 검사용 복제본의 낙석을 끕니다. 값 오류를 이미 알렸을 때 GetData가 같은 오류를 또 내지 않게 합니다(계획 P-RF1).
        internal static void DisableRockfall(Object combat)
        {
            var values = new SerializedObject(combat);
            Require(Require(values, Rockfall), RockfallEnabled).boolValue = false;
            values.ApplyModifiedPropertiesWithoutUndo();
        }

        // 작업본 배경 프리팹에서 그 이름의 오브젝트 수입니다(게임 StageViewPropPresenter.FindUniqueByName과 같이 비활성 포함 이름 일치).
        // 맵 SO 방식이거나 프리팹이 없으면 셀 수 없어 null입니다(Q-RF3 A, 계획 P-RF3). view는 배경 방식 이름을 읽는 데만 씁니다.
        internal static int? RockfallObjectCount(CombatStageSoDocument document, Object view, string objectName)
        {
            if (view == null) return null;
            string[] modes = Require(Require(new SerializedObject(view), Prop), ViewMode).enumNames;
            if (NameAt(modes, document.view.mode) == CombatStageSoBackground.MapDefinition) return null;
            var prefab = Load(document.view.gameObjectGuid) as GameObject;
            if (prefab == null) return null;
            return prefab.GetComponentsInChildren<Transform>(true).Count(child => child.name == objectName);
        }

        private static void ReadView(SerializedObject view, CombatStageSoView values, CombatStageSoNames names)
        {
            SerializedProperty prop = Require(view, Prop);
            values.mode = EnumIndex(Require(prop, ViewMode), ref names.viewMode);
            values.gameObjectGuid = GuidOf(Require(prop, PropObject).objectReferenceValue);
            values.mapGuid = GuidOf(Require(prop, PropMap).objectReferenceValue);
            values.disableRockfallTunnelExplosion = Require(prop, DisableRockfallTunnelExplosion).boolValue;
            values.positionOffset = Require(prop, PositionOffset).vector3Value;
            values.rotationOffset = Require(prop, RotationOffset).vector3Value;
            values.scaleOffset = Require(prop, ScaleOffset).vector3Value;
            values.tiles = ReadTiles(Require(prop, MapTiles));
            values.terrainRotations = Elements(Require(view, TerrainRotations)).Select(e => ReadRotation(e, names)).ToList();
            values.objectRotations = Elements(Require(view, ObjectRotations)).Select(e => ReadRotation(e, names)).ToList();
        }

        // 종류·회전은 열거 값(intValue)으로 읽습니다. 잘못 저장된 값(0 등)도 그대로 담아 저장 검사가 알리게 합니다.
        private static CombatStageSoTileLayout ReadTiles(SerializedProperty tiles) => new CombatStageSoTileLayout
        {
            rotations = Elements(Require(tiles, TileRotations)).Select(rotation => rotation.intValue).ToArray(),
            tileSetGuid = GuidOf(Require(tiles, TileSet).objectReferenceValue),
            columns = Require(tiles, TileColumns).intValue,
            rows = Require(tiles, TileRows).intValue,
            tileSize = CombatMapAuthoringMetrics.TileSize,
            offset = Require(tiles, TileOffset).vector3Value,
            anchorTile = Require(tiles, AnchorTile).vector2IntValue,
            kinds = Elements(Require(tiles, Tiles)).Select(kind => kind.intValue).ToArray(),
        };

        // 타일 세트의 종류 줄을 세트 순서대로 읽습니다(설계 §12 UT3). 세트가 없으면 빈 목록입니다. 검사는 저장 검사(게임 CreateVisuals)가 합니다.
        internal static List<CombatStageSoTileKind> ReadTileSet(string guid)
        {
            var kinds = new List<CombatStageSoTileKind>();
            Object set = Load(guid);
            if (set == null) return kinds;
            if (!IsType(set, TileSetType)) throw new InvalidOperationException($"{set.name}: 타일 세트({TileSetType})가 아닙니다.");
            foreach (SerializedProperty entry in Elements(Require(new SerializedObject(set), Tiles)))
            {
                SerializedProperty kind = Require(entry, Kind);
                string[] kindNames = kind.enumNames;
                int index = kind.enumValueIndex;
                string name = index >= 0 && index < kindNames.Length ? kindNames[index] : $"(값 {kind.intValue})";
                kinds.Add(new CombatStageSoTileKind(kind.intValue, name,
                    Elements(Require(entry, TilePrefabs)).Select(candidate => candidate.objectReferenceValue as GameObject).ToArray(),
                    Require(entry, VisualSurface).floatValue));
            }
            return kinds;
        }

        // 배경 방식 이름을 View 하나에서 읽습니다. 열린 스테이지가 없어도 방식 고르기가 이름을 쓰게 합니다(계획 P10).
        internal static void FillViewNames(Object view, CombatStageSoNames names)
        {
            if (view == null) return;
            names.viewMode = Require(Require(new SerializedObject(view), Prop), ViewMode).enumNames;
        }

        private static CombatStageSoUnit ReadUnit(SerializedProperty element, CombatStageSoNames names)
        {
            Vector2Int cell = ReadCell(Require(element, Anchor));
            return new CombatStageSoUnit
            {
                unitGuid = GuidOf(Require(element, UnitDefinition).objectReferenceValue),
                startingItemGuid = GuidOf(Require(element, StartingItem).objectReferenceValue),
                x = cell.x, y = cell.y,
                rotation = EnumIndex(Require(element, Rotation), ref names.rotation),
                isVictoryTarget = Require(element, VictoryTarget).boolValue,
            };
        }

        private static CombatStageSoPiece ReadPiece(SerializedProperty element, string definition, CombatStageSoNames names)
        {
            Vector2Int cell = ReadCell(Require(element, Anchor));
            return new CombatStageSoPiece
            {
                definitionGuid = GuidOf(Require(element, definition).objectReferenceValue),
                x = cell.x, y = cell.y,
                rotation = EnumIndex(Require(element, Rotation), ref names.rotation),
            };
        }

        private static CombatStageSoPose ReadPose(SerializedProperty anchor, SerializedProperty rotation, CombatStageSoNames names)
        {
            Vector2Int cell = ReadCell(anchor);
            return new CombatStageSoPose { x = cell.x, y = cell.y, rotation = EnumIndex(rotation, ref names.rotation) };
        }

        private static CombatStageSoMarker ReadMarker(SerializedProperty element, bool terrain, CombatStageSoNames names)
        {
            Vector2Int cell = ReadCell(Require(element, Cell));
            return new CombatStageSoMarker
            {
                definitionGuid = GuidOf(Require(element, Definition).objectReferenceValue),
                sourceId = Require(element, SourceId).longValue,
                x = cell.x, y = cell.y,
                knownTo = Elements(Require(element, KnownTo)).Select(e => EnumIndex(e, ref names.owner)).ToList(),
                // 무작위 시작은 지형 입력에만 있습니다.
                randomizeInitialPosition = terrain && Require(element, RandomStart).boolValue,
                tornadoHorizontalSign = terrain ? Require(element, TornadoSign).intValue : 0,
            };
        }

        // 공유 SO 넷만 load를 거칩니다. 편집할 수 있는 공유 SO가 이 넷뿐이라서입니다(계획 P4).
        private static void WriteCombat(SerializedObject combat, CombatStageSoDocument document, Func<string, Object> load)
        {
            Require(combat, Seed).intValue = document.timelineSeed;
            Require(combat, Field).objectReferenceValue = load(document.fieldGuid);
            Require(combat, Roster).objectReferenceValue = load(document.rosterGuid);
            Require(combat, Economy).objectReferenceValue = load(document.economyGuid);
            Require(combat, Objective).enumValueIndex = document.objectiveKind;
            Require(combat, EnemyLoadout).objectReferenceValue = load(document.enemyLoadoutGuid);
            Require(combat, EnemyLevel).intValue = document.enemyLevel;
            Require(combat, Headquarters).objectReferenceValue = Load(document.headquartersGuid);
            Require(combat, CornerCut).intValue = document.cornerCutTicks;
            WriteArray(Require(combat, StructureCatalog), document.structureCatalog, (element, guid) => element.objectReferenceValue = Load(guid));
            WriteArray(Require(combat, EnemyUnits), document.enemyUnits, (element, unit) =>
            {
                Require(element, UnitDefinition).objectReferenceValue = Load(unit.unitGuid);
                WriteCell(Require(element, Anchor), unit.x, unit.y);
                Require(element, Rotation).enumValueIndex = unit.rotation;
                Require(element, VictoryTarget).boolValue = unit.isVictoryTarget;
                Require(element, StartingItem).objectReferenceValue = Load(unit.startingItemGuid);
            });
            WriteArray(Require(combat, Structures), document.structures, (element, piece) => WritePiece(element, StructureDefinition, piece));
            WritePose(Require(combat, MainAnchor), Require(combat, MainRotation), document.mainCharacter);
            WritePose(Require(combat, HeadquartersAnchor), Require(combat, HeadquartersRotation), document.enemyHeadquarters);
            SerializedProperty environment = Require(combat, Environment);
            WriteArray(Require(environment, Terrains), document.terrains, (element, marker) => WriteMarker(element, marker, true));
            WriteArray(Require(environment, Obstacles), document.obstacles, (element, piece) => WritePiece(element, Definition, piece));
            WriteArray(Require(environment, Objects), document.objects, (element, marker) => WriteMarker(element, marker, false));
            Require(combat, RailwayEnabled).boolValue = document.railwayEnabled;
            SerializedProperty railway = Require(combat, Railway);
            WriteArray(Require(railway, RailCells), document.railCells, (element, cell) => WriteCell(element, cell.x, cell.y));
            Require(railway, RailFirst).intValue = document.railFirstActivation;
            Require(railway, RailRepeat).intValue = document.railRepeatInterval;
            SerializedProperty rockfall = Require(combat, Rockfall);
            Require(rockfall, RockfallEnabled).boolValue = document.rockfall.enabled;
            Require(rockfall, RockfallCooldown).intValue = document.rockfall.cooldownTurns;
            Require(rockfall, RockfallWarning).intValue = document.rockfall.warningTurns;
            Require(rockfall, RockfallMinimum).intValue = document.rockfall.minimumRocks;
            Require(rockfall, RockfallMaximum).intValue = document.rockfall.maximumRocks;
            Require(rockfall, RockfallHealthLoss).intValue = document.rockfall.maximumHealthLossPercent;
            Require(rockfall, RockfallBind).intValue = document.rockfall.bindTurns;
            WriteArray(Require(rockfall, RockfallFootprint), document.rockfall.footprintOffsets, (element, cell) => WriteCell(element, cell.x, cell.y));
            WriteHaboob(combat, document);
            // Core PlayerDeploymentZone.Of와 같은 y → x 순서로 써서 에셋 diff가 흔들리지 않게 합니다(설계 P-AZ1).
            var zoneCells = document.playerDeploymentCells.OrderBy(cell => cell.y).ThenBy(cell => cell.x).ToList();
            WriteArray(Require(combat, PlayerDeploymentCells), zoneCells, (element, cell) => WriteCell(element, cell.x, cell.y));
            combat.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WriteHaboob(SerializedObject combat, CombatStageSoDocument document)
        {
            SerializedProperty haboob = Require(combat, Haboob);
            Require(haboob, HaboobEnabled).boolValue = document.haboob.enabled;
            Require(haboob, HaboobInterval).intValue = document.haboob.relocationIntervalTurns;
            Require(haboob, HaboobWarning).intValue = document.haboob.warningLeadTurns;
            WriteArray(Require(haboob, HaboobFootprint), document.haboob.footprintOffsets, (element, cell) => WriteCell(element, cell.x, cell.y));
            Require(haboob, HaboobFixedAnchor).boolValue = document.haboob.fixedInitialAnchor;
            WriteCell(Require(haboob, HaboobAnchor), document.haboob.initialAnchor.x, document.haboob.initialAnchor.y);
        }

        private static void WriteView(SerializedObject view, CombatStageSoDocument document)
        {
            SerializedProperty prop = Require(view, Prop);
            Require(prop, StageKey).intValue = document.stageKey;
            Require(prop, ViewMode).enumValueIndex = document.view.mode;
            Require(prop, PropObject).objectReferenceValue = Load(document.view.gameObjectGuid);
            Require(prop, PropMap).objectReferenceValue = Load(document.view.mapGuid);
            Require(prop, DisableRockfallTunnelExplosion).boolValue = document.view.disableRockfallTunnelExplosion;
            Require(prop, PositionOffset).vector3Value = document.view.positionOffset;
            Require(prop, RotationOffset).vector3Value = document.view.rotationOffset;
            SerializedProperty mode = Require(prop, ViewMode);
            if (NameAt(mode.enumNames, mode.enumValueIndex) != CombatStageSoBackground.PrefabWithTiles)
                Require(prop, ScaleOffset).vector3Value = document.view.scaleOffset;
            else
                CombatMapAuthoringMetrics.RequireTileSize(document.view.tiles.tileSize, view.targetObject.name);
            WriteTiles(Require(prop, MapTiles), document.view.tiles);
            WriteArray(Require(view, TerrainRotations), document.view.terrainRotations, WriteRotation);
            WriteArray(Require(view, ObjectRotations), document.view.objectRotations, WriteRotation);
            view.ApplyModifiedPropertiesWithoutUndo();
        }

        // 타일 값을 모두 씁니다. 작업본 타일이 비어 있으면 에셋도 게임의 "타일 없음" 값(세트 없음·0 × 0·종류 없음)이 됩니다.
        private static void WriteTiles(SerializedProperty tiles, CombatStageSoTileLayout layout)
        {
            if (layout == null) throw new InvalidOperationException("작업본에 바닥 타일 값이 없습니다.");
            Require(tiles, TileSet).objectReferenceValue = Load(layout.tileSetGuid);
            Require(tiles, TileColumns).intValue = layout.columns;
            Require(tiles, TileRows).intValue = layout.rows;
            // 타일 크기는 공통 제작 규격이며 SO에 별도로 저장하지 않습니다.
            Require(tiles, TileOffset).vector3Value = layout.offset;
            Require(tiles, AnchorTile).vector2IntValue = layout.anchorTile;
            WriteArray(Require(tiles, Tiles), layout.kinds ?? Array.Empty<int>(), (element, kind) => element.intValue = kind);
            // 모두 0°면 빈 목록으로 씁니다(게임은 빈 목록 = 모두 R0, 계획 P-TTT1).
            WriteArray(Require(tiles, TileRotations), layout.HasRotations ? layout.rotations : Array.Empty<int>(),
                (element, rotation) => element.intValue = rotation);
        }

        private static void WritePiece(SerializedProperty element, string definition, CombatStageSoPiece piece)
        {
            Require(element, definition).objectReferenceValue = Load(piece.definitionGuid);
            WriteCell(Require(element, Anchor), piece.x, piece.y);
            Require(element, Rotation).enumValueIndex = piece.rotation;
        }

        private static void WritePose(SerializedProperty anchor, SerializedProperty rotation, CombatStageSoPose pose)
        {
            WriteCell(anchor, pose.x, pose.y);
            rotation.enumValueIndex = pose.rotation;
        }

        private static void WriteMarker(SerializedProperty element, CombatStageSoMarker marker, bool terrain)
        {
            Require(element, Definition).objectReferenceValue = Load(marker.definitionGuid);
            Require(element, SourceId).longValue = marker.sourceId;
            WriteCell(Require(element, Cell), marker.x, marker.y);
            WriteArray(Require(element, KnownTo), marker.knownTo, (owner, index) => owner.enumValueIndex = index);
            if (terrain)
            {
                Require(element, RandomStart).boolValue = marker.randomizeInitialPosition;
                Require(element, TornadoSign).intValue = marker.tornadoHorizontalSign;
            }
        }

        private static void WriteRotation(SerializedProperty element, CombatStageSoRotation rotation)
        {
            Require(element, SourceId).longValue = rotation.sourceId;
            Require(element, Rotation).enumValueIndex = rotation.rotation;
        }

        private static void WriteCell(SerializedProperty cell, int x, int y)
        {
            Require(cell, X).intValue = x;
            Require(cell, Y).intValue = y;
        }

        // 배열 크기를 늘리면 Unity가 앞 칸 값을 복사하므로, 칸마다 모든 필드를 씁니다.
        private static void WriteArray<T>(SerializedProperty array, IReadOnlyList<T> items, Action<SerializedProperty, T> write)
        {
            if (!array.isArray)
                throw new InvalidOperationException($"{array.serializedObject.targetObject.name}: SO 필드 '{array.propertyPath}'가 배열이 아닙니다.");
            array.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) write(array.GetArrayElementAtIndex(i), items[i]);
        }

        private static CombatStageSoRotation ReadRotation(SerializedProperty element, CombatStageSoNames names) =>
            new CombatStageSoRotation { sourceId = Require(element, SourceId).longValue, rotation = EnumIndex(Require(element, Rotation), ref names.rotation) };

        private static Vector2Int ReadCell(SerializedProperty cell) => new Vector2Int(Require(cell, X).intValue, Require(cell, Y).intValue);

        private static string Reference(SerializedObject owner, string name) => GuidOf(Require(owner, name).objectReferenceValue);

        private static int EnumIndex(SerializedProperty property, ref string[] names)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
                throw new InvalidOperationException($"{property.serializedObject.targetObject.name}: SO 필드 '{property.propertyPath}'가 열거형이 아닙니다.");
            names = property.enumNames;
            return property.enumValueIndex;
        }

        private static IEnumerable<SerializedProperty> Elements(SerializedProperty array)
        {
            if (!array.isArray)
                throw new InvalidOperationException($"{array.serializedObject.targetObject.name}: SO 필드 '{array.propertyPath}'가 배열이 아닙니다.");
            for (int i = 0; i < array.arraySize; i++) yield return array.GetArrayElementAtIndex(i);
        }

        // 필드가 없으면 그 이름을 담아 바로 멈춥니다. SO 쪽 이름이 바뀌어도 조용히 틀리지 않게 합니다(설계 SM3).
        private static SerializedProperty Require(SerializedObject owner, string name) =>
            owner.FindProperty(name) ?? throw Missing(owner.targetObject.name, name);

        private static SerializedProperty Require(SerializedProperty owner, string name) =>
            owner.FindPropertyRelative(name) ?? throw Missing(owner.serializedObject.targetObject.name, owner.propertyPath + "." + name);

        private static Exception Missing(string asset, string path) =>
            new InvalidOperationException($"{asset}: SO 필드 '{path}'를 찾을 수 없습니다. SO 코드의 필드 이름이 바뀌었다면 CombatStageSoAssets의 이름을 맞춰 주세요.");
    }

    // 열거 값의 이름입니다. 형식을 참조하지 않으려고 에셋의 SerializedProperty에서 읽습니다(계획 P10).
    internal sealed class CombatStageSoNames
    {
        public string[] rotation, objective, mapRole, owner, viewMode;

        public bool IsQuarterTurn(int rotationIndex)
        {
            string name = CombatStageSoAssets.NameAt(rotation, rotationIndex);
            return name == "R90" || name == "R270";
        }
    }

    // 참조 고르기 팝업과 정의 목록에 쓰는 같은 형식 에셋 목록입니다(계획 P9). Labels[0]은 "(없음)"입니다.
    // 한국어 이름이 있으면 목록·팝업은 "한국어 (짧은 에셋 이름)", 맵 칸은 한국어만 씁니다.
    internal sealed class CombatStageSoChoices
    {
        private readonly Dictionary<string, string> cellNames = new Dictionary<string, string>();
        public readonly List<string> Guids;
        public readonly string[] Labels;

        public CombatStageSoChoices(List<Object> assets, Func<Object, string> koreanName = null)
        {
            Guids = assets.Select(CombatStageSoAssets.GuidOf).ToList();
            var labels = new List<string> { "(없음)" };
            for (int i = 0; i < assets.Count; i++)
            {
                string shortName = CombatStageSoAssets.ShortAssetName(assets[i].name);
                string korean = koreanName?.Invoke(assets[i]);
                if (korean == null) labels.Add(assets[i].name);
                else labels.Add(string.Equals(korean, shortName, StringComparison.OrdinalIgnoreCase) ? korean : $"{korean} ({shortName})");
                cellNames[Guids[i]] = korean ?? shortName;
            }
            Labels = labels.ToArray();
        }

        public int Count => Guids.Count;

        // 이 목록에 없는 에셋이면 null입니다.
        public string CellName(string guid) => guid != null && cellNames.TryGetValue(guid, out string name) ? name : null;

        // 목록에 없는 에셋이면 -1입니다.
        public int IndexOf(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return 0;
            int index = Guids.IndexOf(guid);
            return index < 0 ? -1 : index + 1;
        }

        public string GuidAt(int index) => index <= 0 || index > Guids.Count ? "" : Guids[index - 1];
    }

    internal sealed class CombatStageSoField
    {
        public readonly int Columns, Rows, PlayerDepth, EnemyDepth, Reserve;

        public CombatStageSoField(int columns, int rows, int playerDepth, int enemyDepth, int reserve)
        {
            Columns = columns; Rows = rows; PlayerDepth = playerDepth; EnemyDepth = enemyDepth; Reserve = reserve;
        }
    }

    // 아군 배치 구역 하나입니다(설계 §11 AZ2). 칸 판정은 Core PlayerDeploymentZone·DeploymentZonePolicy를 그대로 부릅니다.
    // 적 구역은 없습니다. 적(고정 적 유닛·구조물·본부, 출현)은 아군 구역 밖이면 어디든 놓입니다.
    internal sealed class CombatStageSoZone
    {
        private readonly PlayerDeploymentZone _zone;
        private readonly DeploymentZonePolicy _policy;

        public CombatStageSoZone(PlayerDeploymentZone zone, int listedCount, int rowDepth)
        {
            _zone = zone ?? throw new ArgumentNullException(nameof(zone));
            _policy = new DeploymentZonePolicy(zone);
            ListedCount = listedCount;
            RowDepth = rowDepth;
        }

        // 칸 목록으로 정한 구역인지입니다. 아니면 필드 아군 줄 수 구역입니다.
        public bool IsListed => ListedCount > 0;
        public int ListedCount { get; }
        public int RowDepth { get; }

        public bool Contains(Vector2Int cell) => _zone.Contains(new CellCoord(cell.x, cell.y));

        // 적이 이 칸들에 놓일 수 있는지입니다. 하나라도 아군 구역 안이면 거짓입니다(Core 판정 그대로).
        public bool EnemyAllowed(IEnumerable<Vector2Int> cells) =>
            _policy.CanDeploy(Owner.Enemy, cells.Select(cell => new CellCoord(cell.x, cell.y)).ToArray());
    }

    internal sealed class CombatStageSoEntry
    {
        public readonly Object Root, Combat;
        public readonly string RootGuid, MapRole;
        public readonly int StageKey, MapKey;
        public readonly bool Registered;

        public CombatStageSoEntry(Object root, Object combat, int stageKey, int mapKey, string mapRole, bool registered)
        {
            Root = root; Combat = combat; RootGuid = CombatStageSoAssets.GuidOf(root);
            StageKey = stageKey; MapKey = mapKey; MapRole = mapRole; Registered = registered;
        }
    }

    // 화면이 쓰는 에셋 찾기 결과입니다. 새로고침 때만 다시 만듭니다.
    internal sealed class CombatStageSoCatalog
    {
        private readonly Dictionary<string, Vector2Int> footprints = new Dictionary<string, Vector2Int>();
        private readonly Dictionary<string, CombatStageSoField> fields = new Dictionary<string, CombatStageSoField>();
        private readonly Dictionary<string, bool> blockingTerrains = new Dictionary<string, bool>();
        private Dictionary<int, List<Object>> viewsByKey;
        private Dictionary<string, HashSet<string>> shares;

        public List<CombatStageSoEntry> Stages { get; private set; }
        public Dictionary<string, int> RootKeys { get; } = new Dictionary<string, int>();
        public HashSet<Object> RegisteredRoots { get; private set; }
        public HashSet<Object> RegisteredViews { get; private set; }
        // 적 유닛은 적 배치에 쓸 수 있는 정의만, 아군 유닛은 나머지입니다(CanBeEnemy). 스테이지 데이터에는 아군을 미리 놓는 칸이 없어
        // 아군 유닛은 놓을 수 없고 구분해 보이기·이름 찾기에만 씁니다.
        public CombatStageSoChoices EnemyUnits { get; private set; }
        public CombatStageSoChoices PlayerUnits { get; private set; }
        public CombatStageSoChoices Structures { get; private set; }
        public CombatStageSoChoices Terrains { get; private set; }
        public CombatStageSoChoices Obstacles { get; private set; }
        public CombatStageSoChoices Objects { get; private set; }
        public CombatStageSoChoices Fields { get; private set; }
        public CombatStageSoChoices Rosters { get; private set; }
        public CombatStageSoChoices Economies { get; private set; }
        public CombatStageSoChoices Loadouts { get; private set; }
        public CombatStageSoChoices Items { get; private set; }
        // 타일 세트는 표 없이 View가 직접 참조하는 SO입니다(계획 P-UTT5). 이름순입니다.
        public CombatStageSoChoices TileSets { get; private set; }
        // 이웃 모양을 쓰는 지형 종류입니다(EdgeShapeTerrainKinds). 화면용이고, 저장 검사는 새로 읽습니다(계획 P-MG1).
        public HashSet<int> EdgeShapeKinds { get; private set; }
        // 맵 생성 줄(병합 대응 설계 MG9)이 쓰는 표 등록 상태와 맵 규칙입니다. 등록·빼기·저장이 막을지는 새로 읽어 정합니다(MG-P7).
        public List<CombatStageSoChapterStage> ChapterStages { get; private set; }
        public List<CombatStageSoMapRules> MapRules { get; private set; }
        public CombatStageSoNames Names { get; } = new CombatStageSoNames();

        public static CombatStageSoCatalog Build()
        {
            Dictionary<int, string> unitNames = CombatStageSoAssets.UnitVisualNames();
            var catalog = new CombatStageSoCatalog
            {
                RegisteredRoots = CombatStageSoAssets.RegisteredItems(CombatStageSoAssets.RootTableType),
                RegisteredViews = CombatStageSoAssets.RegisteredItems(CombatStageSoAssets.ViewTableType),
                viewsByKey = CombatStageSoAssets.ViewsByStageKey(),
                shares = CombatStageSoAssets.ShareUsers(),
                EnemyUnits = Choices(CombatStageSoAssets.UnitType, unitNames, CombatStageSoAssets.CanBeEnemy),
                PlayerUnits = Choices(CombatStageSoAssets.UnitType, unitNames, unit => !CombatStageSoAssets.CanBeEnemy(unit)),
                Structures = Choices(CombatStageSoAssets.StructureType, unitNames),
                Terrains = Choices(CombatStageSoAssets.TerrainType, CombatStageSoAssets.TerrainVisualNames()),
                Obstacles = Choices(CombatStageSoAssets.ObstacleType, unitNames),
                Objects = Choices(CombatStageSoAssets.ObjectType, CombatStageSoAssets.ObjectVisualNames()),
                Fields = Choices(CombatStageSoAssets.FieldType),
                Rosters = Choices(CombatStageSoAssets.RosterType),
                Economies = Choices(CombatStageSoAssets.EconomyType),
                Loadouts = Choices(CombatStageSoAssets.LoadoutType),
                Items = Choices(CombatStageSoAssets.ItemType),
                TileSets = Choices(CombatStageSoAssets.TileSetType),
                EdgeShapeKinds = CombatStageSoAssets.EdgeShapeTerrainKinds(),
                ChapterStages = CombatStageSoAssets.ReadChapterStages(),
                MapRules = CombatStageSoAssets.ReadMapRules(),
            };
            catalog.Stages = CombatStageSoAssets.FindStages(catalog.RegisteredRoots, catalog.RootKeys);
            if (catalog.Stages.Count > 0) CombatStageSoAssets.FillNames(catalog.Stages[0].Root, catalog.Stages[0].Combat, catalog.Names);
            CombatStageSoAssets.FillViewNames(CombatStageSoAssets.FindAll(CombatStageSoAssets.ViewType).FirstOrDefault(), catalog.Names);
            return catalog;
        }

        private static CombatStageSoChoices Choices(string typeName, Dictionary<int, string> koreanNames = null, Func<Object, bool> where = null)
        {
            List<Object> assets = CombatStageSoAssets.FindAll(typeName);
            if (where != null) assets = assets.Where(where).ToList();
            if (koreanNames == null) return new CombatStageSoChoices(assets);
            return new CombatStageSoChoices(assets, asset =>
            {
                // 외형 표에 없는 정의(또는 Core가 모르는 장애물 종류)는 에셋 이름으로 둡니다.
                try { return koreanNames.TryGetValue(CombatStageSoAssets.VisualId(asset), out string name) ? name : null; }
                catch (ArgumentOutOfRangeException) { return null; }
            });
        }

        private IEnumerable<CombatStageSoChoices> Definitions => new[] { EnemyUnits, PlayerUnits, Structures, Terrains, Obstacles, Objects };

        // 맵 칸에 쓰는 정의 이름입니다. 한국어 이름이 없으면 짧은 에셋 이름입니다.
        public string CellName(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return "(없음)";
            foreach (var choices in Definitions)
            {
                string name = choices.CellName(guid);
                if (name != null) return name;
            }
            return CombatStageSoAssets.ShortAssetName(CombatStageSoAssets.NameOf(guid));
        }

        // 목록·팝업과 같은 정의 이름입니다.
        public string DefinitionLabel(string guid)
        {
            foreach (var choices in Definitions)
            {
                int index = choices.IndexOf(guid);
                if (index > 0) return choices.Labels[index];
            }
            return CombatStageSoAssets.NameOf(guid);
        }

        public IReadOnlyList<Object> ViewsFor(int stageKey) =>
            viewsByKey.TryGetValue(stageKey, out var views) ? views : (IReadOnlyList<Object>)Array.Empty<Object>();

        // 이 스테이지(self: 전투 데이터·View GUID)를 뺀 사용처 수입니다. 0이면 이 스테이지만 쓰는 SO라 바로 고칠 수 있습니다(계획 P4).
        public int OtherUsers(string guid, params string[] self) =>
            !string.IsNullOrEmpty(guid) && shares.TryGetValue(guid, out var users) ? users.Count(user => !self.Contains(user)) : 0;

        public Vector2Int Footprint(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return Vector2Int.one;
            if (!footprints.TryGetValue(guid, out var size))
                footprints[guid] = size = CombatStageSoAssets.Footprint(CombatStageSoAssets.Load(guid));
            return size;
        }

        // 벽·낭떠러지 지형 정의인지입니다(CombatStageSoAssets.BlocksEntry). 아군 구역 참고 수가 씁니다.
        public bool BlocksEntry(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return false;
            if (!blockingTerrains.TryGetValue(guid, out bool blocks))
                blockingTerrains[guid] = blocks = CombatStageSoAssets.BlocksEntry(CombatStageSoAssets.Load(guid));
            return blocks;
        }

        public CombatStageSoField Field(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            if (!fields.TryGetValue(guid, out var field))
                fields[guid] = field = CombatStageSoAssets.ReadField(CombatStageSoAssets.Load(guid));
            return field;
        }
    }
}
