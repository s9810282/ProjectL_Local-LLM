using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HaboobGimmickSpec = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.HaboobGimmickSpec;
using CellCoord = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CellCoord;

namespace ProjectL.Tool.Editor
{
    // SO 모드 작업본입니다. 에셋 참조는 GUID 문자열(계획 P8), 열거 값은 SerializedProperty.enumValueIndex로 둡니다.
    // 스테이지 키는 여기 하나만 두고 Root와 View 양쪽에 씁니다(설계 SM2).
    [Serializable]
    internal sealed class CombatStageSoDocument
    {
        // 작업본 형식 번호입니다(설계 §12 UT8). 작업본에 칸을 더하면 올립니다. 옛 형식의 초안·창에 남은 작업본은 새 칸이 빈 채라,
        // 그대로 저장하면 에셋 값(예: 바닥 타일)을 지웁니다. 그래서 번호가 다르면 복원하지 않습니다(P-UT6).
        // 필드 초기값을 두지 않습니다. JSON·창 직렬화에 이 칸이 없으면 0이 남아야 옛 형식으로 알아봅니다. 에셋에서 읽을 때만 채웁니다.
        // 2: 바닥 타일 칸별 회전(설계 §13 TT4).
        // 3: 토네이도 최초 가로 방향(토네이도 View 계획 TP-12).
        // 4: 낙석 값(설계 §17 RF1).
        // 5: 하붑 값(설계 §22 HBE).
        // 6: 스테이지별 낙석 터널 폭발 사용 여부(설계 §24 RTE).
        // 7: 하붑 시작 위치(설계 §25 HAT).
        public const int CurrentFormat = 7;

        public int format;
        public string rootGuid = "", combatGuid = "", viewGuid = "";
        public int stageKey, mapKey, mapRole;
        public int timelineSeed, objectiveKind, enemyLevel = 1, cornerCutTicks;
        public string fieldGuid = "", rosterGuid = "", economyGuid = "", enemyLoadoutGuid = "", headquartersGuid = "";
        public List<string> structureCatalog = new List<string>();
        public List<CombatStageSoUnit> enemyUnits = new List<CombatStageSoUnit>();
        public List<CombatStageSoPiece> structures = new List<CombatStageSoPiece>();
        // 메인 캐릭터 자리는 SO에 남아 있지만 전투가 읽지 않습니다(Core `4ae6a9b8`부터 첫 손패에서 시작). 맵에 그리지 않고 값만 보존합니다(추가 계획 P-A1).
        public CombatStageSoPose mainCharacter = new CombatStageSoPose();
        public CombatStageSoPose enemyHeadquarters = new CombatStageSoPose();
        public List<CombatStageSoMarker> terrains = new List<CombatStageSoMarker>();
        public List<CombatStageSoPiece> obstacles = new List<CombatStageSoPiece>();
        public List<CombatStageSoMarker> objects = new List<CombatStageSoMarker>();
        public bool railwayEnabled;
        public List<Vector2Int> railCells = new List<Vector2Int>();
        public int railFirstActivation, railRepeatInterval;
        public CombatStageSoRockfall rockfall = new CombatStageSoRockfall();
        public CombatStageSoHaboob haboob = new CombatStageSoHaboob();
        // 아군 배치 구역 칸입니다. 비면 게임은 필드의 아군 줄 수(아래부터)를 씁니다(설계 §11 AZ1·AZ2). 칸 목록은 집합으로 다룹니다(계획 P-AZT1).
        public List<Vector2Int> playerDeploymentCells = new List<Vector2Int>();
        public CombatStageSoView view = new CombatStageSoView();
        // 숨김 호환 필드는 편집하지 않고 경고와 sourceId 계산에만 씁니다(설계 SM10, 계획 P7).
        public int legacyWallCount;
        public List<long> legacyTerrainSourceIds = new List<long>();
        // 이 스테이지만 쓰는 공유 SO(필드·로스터·경제·적 로드아웃)의 저장 안 한 값입니다. 스테이지를 저장할 때 검사 뒤 씁니다(설계 SM7, 계획 P4).
        public List<CombatStageSoSharedEdit> sharedEdits = new List<CombatStageSoSharedEdit>();

        public bool IsOpen => !string.IsNullOrEmpty(rootGuid);
        public bool HasView => !string.IsNullOrEmpty(viewGuid);
        public bool HasHeadquarters => !string.IsNullOrEmpty(headquartersGuid);
        public bool HasLegacyPlacements => legacyWallCount > 0 || legacyTerrainSourceIds.Count > 0;

        public CombatStageSoSharedEdit SharedEdit(string guid) => sharedEdits.Find(edit => edit.guid == guid);

        public void SetSharedEdit(string guid, string json)
        {
            var edit = SharedEdit(guid);
            if (edit == null) sharedEdits.Add(new CombatStageSoSharedEdit { guid = guid, json = json });
            else edit.json = json;
        }

        // 지금 참조하는 공유 SO의 편집만 씁니다. 참조를 다른 SO로 바꾼 뒤 남은 편집은 버립니다.
        public List<CombatStageSoSharedEdit> ActiveSharedEdits() =>
            sharedEdits.Where(edit => edit.guid == fieldGuid || edit.guid == rosterGuid || edit.guid == economyGuid || edit.guid == enemyLoadoutGuid).ToList();

        public static string LayerName(CombatStageSoLayer layer)
        {
            switch (layer)
            {
                case CombatStageSoLayer.Rail: return "레일";
                case CombatStageSoLayer.Terrain: return "지형";
                case CombatStageSoLayer.Object: return "오브젝트";
                case CombatStageSoLayer.Obstacle: return "장애물";
                case CombatStageSoLayer.Structure: return "적 구조물";
                case CombatStageSoLayer.Headquarters: return "적 본부";
                default: return "적 유닛";
            }
        }

        // 칸이 있는 배치를 모두 냅니다. 무작위 시작 지형은 칸이 없어 빠집니다.
        public List<CombatStageSoPlaced> Placements(CombatStageSoGrid grid)
        {
            var placed = new List<CombatStageSoPlaced>();
            foreach (var rail in railCells) placed.Add(new CombatStageSoPlaced(CombatStageSoLayer.Rail, rail, new List<Vector2Int> { rail }));
            foreach (var terrain in terrains.Where(marker => !marker.randomizeInitialPosition))
                placed.Add(new CombatStageSoPlaced(CombatStageSoLayer.Terrain, terrain, CellsFor(terrain, terrain.x, terrain.y, 0, terrain.definitionGuid, grid)));
            foreach (var item in objects)
                placed.Add(new CombatStageSoPlaced(CombatStageSoLayer.Object, item, CellsFor(item, item.x, item.y, 0, item.definitionGuid, grid)));
            foreach (var obstacle in obstacles)
                placed.Add(new CombatStageSoPlaced(CombatStageSoLayer.Obstacle, obstacle, CellsAt(obstacle, obstacle.x, obstacle.y, obstacle.rotation, grid)));
            foreach (var structure in structures)
                placed.Add(new CombatStageSoPlaced(CombatStageSoLayer.Structure, structure, CellsAt(structure, structure.x, structure.y, structure.rotation, grid)));
            if (HasHeadquarters)
                placed.Add(new CombatStageSoPlaced(CombatStageSoLayer.Headquarters, enemyHeadquarters,
                    CellsAt(enemyHeadquarters, enemyHeadquarters.x, enemyHeadquarters.y, enemyHeadquarters.rotation, grid)));
            foreach (var unit in enemyUnits)
                placed.Add(new CombatStageSoPlaced(CombatStageSoLayer.Unit, unit, CellsAt(unit, unit.x, unit.y, unit.rotation, grid)));
            return placed;
        }

        // 한 칸의 배치를 위 층부터 냅니다(유닛 → … → 레일).
        public List<CombatStageSoPlaced> PlacementsAt(Vector2Int cell, CombatStageSoGrid grid) =>
            Placements(grid).Where(placed => placed.Cells.Contains(cell)).OrderByDescending(placed => placed.Layer).ToList();

        // 바로 막는 것은 맵 밖과 같은 층 겹침뿐입니다(설계 SM6). 다른 배치 규칙은 저장 검사와 전투 진입이 판정합니다(계획 P1).
        // 구조물과 본부는 같은 층입니다. 지형·오브젝트는 같은 정의끼리만 겹침으로 봅니다.
        public string PlacementError(CombatStageSoLayer layer, object self, string definitionGuid, List<Vector2Int> cells, CombatStageSoGrid grid)
        {
            if (cells.Any(cell => !grid.Contains(cell))) return "맵 밖에는 놓을 수 없습니다.";
            foreach (var other in Placements(grid))
            {
                if (ReferenceEquals(other.Item, self) || Group(other.Layer) != Group(layer)) continue;
                if ((layer == CombatStageSoLayer.Terrain || layer == CombatStageSoLayer.Object) && DefinitionOf(other.Item) != definitionGuid) continue;
                if (other.Cells.Any(cells.Contains)) return $"같은 칸에 이미 있습니다: {LayerName(other.Layer)}";
            }
            return null;
        }

        public List<Vector2Int> CellsForNew(CombatStageSoLayer layer, string definitionGuid, Vector2Int cell, CombatStageSoGrid grid)
        {
            switch (layer)
            {
                case CombatStageSoLayer.Unit:
                case CombatStageSoLayer.Structure:
                case CombatStageSoLayer.Obstacle:
                    return grid.Cells(cell.x, cell.y, definitionGuid, 0);
                case CombatStageSoLayer.Headquarters:
                    return CellsAt(enemyHeadquarters, cell.x, cell.y, enemyHeadquarters.rotation, grid);
                default:
                    return new List<Vector2Int> { cell };
            }
        }

        public List<Vector2Int> CellsAt(object item, int x, int y, int rotation, CombatStageSoGrid grid) =>
            CellsFor(item, x, y, rotation, DefinitionOf(item), grid);

        // 크기는 유닛·구조물·장애물·본부 정의에서 읽고, 지형·오브젝트·레일은 한 칸입니다.
        public List<Vector2Int> CellsFor(object item, int x, int y, int rotation, string definitionGuid, CombatStageSoGrid grid)
        {
            bool sized = item is CombatStageSoUnit || item is CombatStageSoPiece || ReferenceEquals(item, enemyHeadquarters);
            return sized ? grid.Cells(x, y, definitionGuid, rotation) : new List<Vector2Int> { new Vector2Int(x, y) };
        }

        public CombatStageSoLayer LayerOf(object item)
        {
            switch (item)
            {
                case CombatStageSoUnit _: return CombatStageSoLayer.Unit;
                case CombatStageSoPiece piece: return structures.Contains(piece) ? CombatStageSoLayer.Structure : CombatStageSoLayer.Obstacle;
                case CombatStageSoMarker marker: return terrains.Contains(marker) ? CombatStageSoLayer.Terrain : CombatStageSoLayer.Object;
                case CombatStageSoPose _: return CombatStageSoLayer.Headquarters;
                default: return CombatStageSoLayer.Rail;
            }
        }

        public string DefinitionOf(object item)
        {
            switch (item)
            {
                case CombatStageSoUnit unit: return unit.unitGuid;
                case CombatStageSoPiece piece: return piece.definitionGuid;
                case CombatStageSoMarker marker: return marker.definitionGuid;
                case CombatStageSoPose pose: return ReferenceEquals(pose, enemyHeadquarters) ? headquartersGuid : "";
                default: return "";
            }
        }

        // 지형과 오브젝트의 sourceId는 같은 번호 공간이고 숨김 호환 지형도 포함합니다(계획 P7).
        public long NextSourceId() =>
            terrains.Select(marker => marker.sourceId).Concat(objects.Select(marker => marker.sourceId))
                .Concat(legacyTerrainSourceIds).DefaultIfEmpty(0).Max() + 1;

        // "배치 비우고 만들기"용입니다. 메인 캐릭터·본부 자리와 참조·스테이지 값은 틀 그대로 둡니다(계획 P2).
        // 아군 배치 구역 칸도 비워 필드 줄 수로 시작합니다(설계 P-AZ5).
        public void ClearPlacements()
        {
            enemyUnits.Clear(); structures.Clear();
            terrains.Clear(); obstacles.Clear(); objects.Clear();
            railCells.Clear(); railwayEnabled = false;
            playerDeploymentCells.Clear();
            view.terrainRotations.Clear(); view.objectRotations.Clear();
            legacyWallCount = 0; legacyTerrainSourceIds.Clear();
        }

        // 아군 구역 칸을 칠하거나(erase 거짓) 지웁니다(설계 §11 AZ4). 칸 목록은 집합으로 다루고(계획 P-AZT1), 판 밖 칸은 건너뜁니다(P-AZ4).
        // 칸 목록이 비면 구역은 필드 줄 수입니다. 실제로 바뀌는 칸이 있을 때만 지금 보이는 구역 칸을 y → x로 칸 목록에 옮긴 뒤 편집합니다(Q-AZ1 A, P-AZT6·P-AZT7).
        // 지운 뒤 0칸이 되면 하지 않고 거절 문구를 냅니다(Q-AZ2 A). 인스펙터로 넣은 중복은 고치지 않습니다(저장 검사가 막음).
        // 지금 구역은 CombatStageSoAssets.ZoneOf 하나로 해석합니다. Done은 바뀐 칸 수입니다.
        public (int Done, string Error) PaintZone(IEnumerable<Vector2Int> cells, bool erase, CombatStageSoField field, CombatStageSoGrid grid)
        {
            var zone = CombatStageSoAssets.ZoneOf(playerDeploymentCells, field);
            var targets = cells.Where(grid.Contains).Distinct().Where(cell => zone.Contains(cell) == erase).ToList();
            if (targets.Count == 0) return (0, null);
            var shown = playerDeploymentCells.Count > 0 ? playerDeploymentCells.Distinct().ToList() : ZoneCells(zone, grid);
            if (erase && shown.All(targets.Contains))
                return (0, "마지막 칸은 지울 수 없습니다. 필드 줄 수로 돌아가려면 '칸 목록 비우기'를 쓰세요.");
            if (playerDeploymentCells.Count == 0) playerDeploymentCells.AddRange(shown);
            if (erase) playerDeploymentCells.RemoveAll(targets.Contains);
            else playerDeploymentCells.AddRange(targets);
            return (targets.Count, null);
        }

        private static List<Vector2Int> ZoneCells(CombatStageSoZone zone, CombatStageSoGrid grid)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < grid.Rows; y++)
                for (int x = 0; x < grid.Columns; x++)
                    if (zone.Contains(new Vector2Int(x, y))) cells.Add(new Vector2Int(x, y));
            return cells;
        }

        // 메인 캐릭터와 본부는 하나뿐이라 놓기가 곧 옮기기입니다.
        public void Add(CombatStageSoLayer layer, string definitionGuid, Vector2Int cell, string[] ownerNames)
        {
            switch (layer)
            {
                case CombatStageSoLayer.Unit: enemyUnits.Add(new CombatStageSoUnit { unitGuid = definitionGuid, x = cell.x, y = cell.y }); break;
                case CombatStageSoLayer.Structure: structures.Add(new CombatStageSoPiece { definitionGuid = definitionGuid, x = cell.x, y = cell.y }); break;
                case CombatStageSoLayer.Obstacle: obstacles.Add(new CombatStageSoPiece { definitionGuid = definitionGuid, x = cell.x, y = cell.y }); break;
                case CombatStageSoLayer.Terrain:
                case CombatStageSoLayer.Object:
                    (layer == CombatStageSoLayer.Terrain ? terrains : objects).Add(NewMarker(definitionGuid, cell, ownerNames));
                    break;
                case CombatStageSoLayer.Rail: railCells.Add(cell); break;
                case CombatStageSoLayer.Headquarters: Move(enemyHeadquarters, cell); break;
            }
        }

        // 새로 놓는 배치만 양 진영에 공개합니다. 읽기·복제는 기존 공개 목록을 보존합니다.
        private CombatStageSoMarker NewMarker(string definitionGuid, Vector2Int cell, string[] ownerNames)
        {
            int player = ownerNames == null ? -1 : Array.IndexOf(ownerNames, "Player");
            int enemy = ownerNames == null ? -1 : Array.IndexOf(ownerNames, "Enemy");
            if (player < 0 || enemy < 0)
                throw new InvalidOperationException("새 배치의 공개 대상을 설정할 수 없습니다: 공개 대상 열거에 Player 또는 Enemy가 없습니다.");
            return new CombatStageSoMarker
            {
                definitionGuid = definitionGuid, sourceId = NextSourceId(), x = cell.x, y = cell.y,
                knownTo = new List<int> { player, enemy }
            };
        }

        public void Move(object item, Vector2Int cell)
        {
            switch (item)
            {
                case CombatStageSoUnit unit: unit.x = cell.x; unit.y = cell.y; break;
                case CombatStageSoPiece piece: piece.x = cell.x; piece.y = cell.y; break;
                case CombatStageSoMarker marker: marker.x = cell.x; marker.y = cell.y; break;
                case CombatStageSoPose pose: pose.x = cell.x; pose.y = cell.y; break;
                default: throw new InvalidOperationException("옮길 수 없는 배치입니다.");
            }
        }

        public void SetRotation(object item, int rotation)
        {
            switch (item)
            {
                case CombatStageSoUnit unit: unit.rotation = rotation; break;
                case CombatStageSoPiece piece: piece.rotation = rotation; break;
                case CombatStageSoPose pose: pose.rotation = rotation; break;
                default: throw new InvalidOperationException("회전할 수 없는 배치입니다.");
            }
        }

        // 지형·오브젝트를 지우면 같은 sourceId의 View 외형 회전도 지워 짝을 맞춥니다(설계 SM2).
        public void Remove(object item)
        {
            switch (item)
            {
                case CombatStageSoUnit unit: enemyUnits.Remove(unit); break;
                case CombatStageSoPiece piece:
                    if (!structures.Remove(piece)) obstacles.Remove(piece);
                    break;
                case CombatStageSoMarker marker:
                    if (terrains.Remove(marker)) view.terrainRotations.RemoveAll(rotation => rotation.sourceId == marker.sourceId);
                    else if (objects.Remove(marker)) view.objectRotations.RemoveAll(rotation => rotation.sourceId == marker.sourceId);
                    break;
                case Vector2Int rail: railCells.Remove(rail); break;
                // 정의만 비웁니다. 게임은 정의가 없으면 본부를 두지 않고, 자리 값은 다시 정의를 고를 때 그대로 씁니다(설계 §18).
                case CombatStageSoPose pose when ReferenceEquals(pose, enemyHeadquarters): headquartersGuid = ""; break;
                default: throw new InvalidOperationException("지울 수 없는 배치입니다. 메인 캐릭터는 옮기기만 할 수 있습니다.");
            }
        }

        private static CombatStageSoLayer Group(CombatStageSoLayer layer) =>
            layer == CombatStageSoLayer.Headquarters ? CombatStageSoLayer.Structure : layer;

        public Vector2Int AnchorOf(object item)
        {
            switch (item)
            {
                case CombatStageSoUnit unit: return new Vector2Int(unit.x, unit.y);
                case CombatStageSoPiece piece: return new Vector2Int(piece.x, piece.y);
                case CombatStageSoMarker marker: return new Vector2Int(marker.x, marker.y);
                case CombatStageSoPose pose: return new Vector2Int(pose.x, pose.y);
                case Vector2Int rail: return rail;
                default: throw new InvalidOperationException("알 수 없는 배치입니다.");
            }
        }

        // 지형·오브젝트·레일은 방향이 없어 0입니다.
        public int RotationOf(object item)
        {
            switch (item)
            {
                case CombatStageSoUnit unit: return unit.rotation;
                case CombatStageSoPiece piece: return piece.rotation;
                case CombatStageSoPose pose: return pose.rotation;
                default: return 0;
            }
        }

        public static bool Rotates(object item) => item is CombatStageSoUnit || item is CombatStageSoPiece || item is CombatStageSoPose;

        // 상자 안 배치입니다. 기준 칸(왼쪽 아래)이 상자 안에 있으면 들어갑니다.
        public List<CombatStageSoPlaced> PlacementsIn(RectInt box, CombatStageSoGrid grid) =>
            Placements(grid).Where(placed => box.Contains(AnchorOf(placed.Item))).ToList();

        // 아직 이 작업본 안에 있는 배치인지 봅니다. Undo로 작업본이 바뀌면 끌던 배치가 사라질 수 있습니다.
        // 배치 객체는 참조로, 레일 칸은 값으로 같은지 봅니다(둘 다 object.Equals 기본 동작).
        public bool Contains(object item, CombatStageSoGrid grid) => Placements(grid).Any(placed => Equals(placed.Item, item));

        // 아래 묶음 연산(추가 설계 AD3·AD4)은 같은 작업본이면 늘 같은 결과를 냅니다.
        // 창은 작업본 복제본에서 먼저 돌려 결과를 보고, 쓸 만할 때만 Undo를 남기며 실제 작업본에 다시 돌립니다.

        public bool TryAdd(CombatStageSoLayer layer, string definitionGuid, Vector2Int cell, CombatStageSoGrid grid, string[] ownerNames)
        {
            object self = layer == CombatStageSoLayer.Headquarters ? enemyHeadquarters : null;
            if (PlacementError(layer, self, definitionGuid, CellsForNew(layer, definitionGuid, cell, grid), grid) != null) return false;
            Add(layer, definitionGuid, cell, ownerNames);
            return true;
        }

        // 칸에서 지울 맨 위 배치입니다. 본부도 다른 배치처럼 지웁니다(설계 §18).
        public CombatStageSoPlaced TopErasable(Vector2Int cell, CombatStageSoGrid grid, Func<object, bool> allowed = null) =>
            PlacementsAt(cell, grid).FirstOrDefault(placed => allowed == null || allowed(placed.Item));

        public bool EraseTop(Vector2Int cell, CombatStageSoGrid grid, Func<object, bool> allowed = null)
        {
            object item = TopErasable(cell, grid, allowed).Item;
            if (item == null) return false;
            Remove(item);
            return true;
        }

        // 상자 칸마다 같은 놓기(layer)나 지우기(layer 없음)를 합니다. 막힌 칸은 건너뜁니다.
        public (int Done, int Skipped) PaintBox(RectInt box, CombatStageSoLayer? layer, string definitionGuid, CombatStageSoGrid grid, string[] ownerNames, Func<object, bool> allowed = null)
        {
            int done = 0, skipped = 0;
            for (int y = box.yMin; y < box.yMax; y++)
            {
                for (int x = box.xMin; x < box.xMax; x++)
                {
                    var cell = new Vector2Int(x, y);
                    bool ok = layer.HasValue ? TryAdd(layer.Value, definitionGuid, cell, grid, ownerNames) : EraseTop(cell, grid, allowed);
                    if (ok) done++;
                    else skipped++;
                }
            }
            return (done, skipped);
        }

        // 배치 묶음을 origin 기준 상대 자리로 복사합니다. 본부는 하나뿐이라 빠지고, 지형·오브젝트의 외형 회전도 함께 담습니다.
        public CombatStageSoClip Copy(IEnumerable<CombatStageSoPlaced> placements, Vector2Int origin)
        {
            var clip = new CombatStageSoClip();
            Vector2Int back = Vector2Int.zero - origin;
            foreach (var placed in placements)
            {
                switch (placed.Item)
                {
                    case CombatStageSoUnit unit: clip.enemyUnits.Add(Shifted(unit, back)); break;
                    case CombatStageSoPiece piece: (structures.Contains(piece) ? clip.structures : clip.obstacles).Add(Shifted(piece, back)); break;
                    case CombatStageSoMarker marker:
                        bool terrain = terrains.Contains(marker);
                        (terrain ? clip.terrains : clip.objects).Add(Shifted(marker, back));
                        var rotation = (terrain ? view.terrainRotations : view.objectRotations).Find(entry => entry.sourceId == marker.sourceId);
                        if (rotation != null) (terrain ? clip.terrainRotations : clip.objectRotations).Add(Clone(rotation));
                        break;
                    case Vector2Int rail: clip.railCells.Add(rail - origin); break;
                }
            }
            return clip;
        }

        // 복사본을 origin만큼 옮겨 놓습니다. 하나씩 바로 막기를 거쳐 막히면 건너뜁니다.
        // 지형·오브젝트는 새 sourceId를 받고, 외형 회전도 새 sourceId로 옮깁니다.
        public (int Done, int Skipped) Paste(CombatStageSoClip clip, Vector2Int origin, CombatStageSoGrid grid)
        {
            int done = 0, skipped = 0;
            void Count(bool ok)
            {
                if (ok) done++;
                else skipped++;
            }
            foreach (var source in clip.enemyUnits) Count(TryInsert(CombatStageSoLayer.Unit, Shifted(source, origin), enemyUnits, grid));
            foreach (var source in clip.structures) Count(TryInsert(CombatStageSoLayer.Structure, Shifted(source, origin), structures, grid));
            foreach (var source in clip.obstacles) Count(TryInsert(CombatStageSoLayer.Obstacle, Shifted(source, origin), obstacles, grid));
            foreach (var source in clip.terrains) Count(TryInsertMarker(CombatStageSoLayer.Terrain, Shifted(source, origin), clip.terrainRotations, grid));
            foreach (var source in clip.objects) Count(TryInsertMarker(CombatStageSoLayer.Object, Shifted(source, origin), clip.objectRotations, grid));
            foreach (var source in clip.railCells)
            {
                var cell = source + origin;
                bool ok = PlacementError(CombatStageSoLayer.Rail, null, "", new List<Vector2Int> { cell }, grid) == null;
                if (ok) railCells.Add(cell);
                Count(ok);
            }
            return (done, skipped);
        }

        // 맵 가운데 세로줄 기준 좌우 대칭 자리입니다. 기준 칸 x' = 가로 − 1 − (오른쪽 끝 x), 회전은 mirrorRotation(R90 ↔ R270)을 거칩니다(Q-AD4).
        public (Vector2Int Anchor, int Rotation) MirrorOf(object item, CombatStageSoGrid grid, Func<int, int> mirrorRotation)
        {
            Vector2Int anchor = AnchorOf(item);
            int rotation = RotationOf(item);
            int right = CellsAt(item, anchor.x, anchor.y, rotation, grid).Max(cell => cell.x);
            return (new Vector2Int(grid.Columns - 1 - right, anchor.y), Rotates(item) ? mirrorRotation(rotation) : rotation);
        }

        // 좌우 뒤집기: 대상(상자 안, 없으면 전체)을 모두 대칭 자리로 옮긴 뒤 하나라도 바로 막기에 걸리면 오류를 냅니다.
        // 지형·오브젝트의 외형 회전도 대칭으로 바꿉니다.
        public string MirrorFlip(RectInt? box, CombatStageSoGrid grid, Func<int, int> mirrorRotation, Func<object, bool> allowed = null)
        {
            var targets = (box.HasValue ? PlacementsIn(box.Value, grid) : Placements(grid)).Where(p => allowed == null || allowed(p.Item)).ToList();
            if (targets.Count == 0) return "뒤집을 배치가 없습니다.";
            var poses = targets.Select(placed => MirrorOf(placed.Item, grid, mirrorRotation)).ToList();
            var rails = new List<Vector2Int>();
            for (int i = 0; i < targets.Count; i++)
            {
                object item = targets[i].Item;
                if (item is Vector2Int rail)
                {
                    railCells.Remove(rail);
                    rails.Add(poses[i].Anchor);
                    continue;
                }
                Move(item, poses[i].Anchor);
                if (Rotates(item)) SetRotation(item, poses[i].Rotation);
                if (item is CombatStageSoMarker marker)
                {
                    MirrorVisualRotation(marker, mirrorRotation);
                    marker.tornadoHorizontalSign = -marker.tornadoHorizontalSign;
                }
            }
            railCells.AddRange(rails);
            return MovedError(targets, grid);
        }

        // 좌우 대칭 복사: 원본은 두고 대칭 자리에 사본을 더합니다. 본부는 하나뿐이라 빠지고, 막힌 사본은 건너뜁니다.
        public (int Done, int Skipped) MirrorCopy(RectInt? box, CombatStageSoGrid grid, Func<int, int> mirrorRotation, Func<object, bool> allowed = null)
        {
            var targets = (box.HasValue ? PlacementsIn(box.Value, grid) : Placements(grid))
                .Where(placed => placed.Layer != CombatStageSoLayer.Headquarters && (allowed == null || allowed(placed.Item))).ToList();
            var clip = Copy(targets, Vector2Int.zero);
            clip.Transform(item => MirrorOf(item, grid, mirrorRotation), mirrorRotation);
            return Paste(clip, Vector2Int.zero, grid);
        }

        // 상자 안 배치를 같은 거리만큼 옮깁니다. 하나라도 막히면 오류를 냅니다(Q-AD5).
        public string MoveGroup(RectInt box, Vector2Int offset, CombatStageSoGrid grid, Func<object, bool> allowed = null)
        {
            var targets = PlacementsIn(box, grid).Where(p => allowed == null || allowed(p.Item)).ToList();
            if (targets.Count == 0) return "옮길 배치가 없습니다.";
            var rails = new List<Vector2Int>();
            foreach (var placed in targets)
            {
                if (placed.Item is Vector2Int rail)
                {
                    railCells.Remove(rail);
                    rails.Add(rail + offset);
                }
                else Move(placed.Item, AnchorOf(placed.Item) + offset);
            }
            railCells.AddRange(rails);
            return MovedError(targets, grid);
        }

        // 옮긴 배치가 바로 막기(맵 밖·같은 층 겹침)에 걸리는지 봅니다. 레일은 값이라 따로 겹침을 셉니다.
        private string MovedError(List<CombatStageSoPlaced> moved, CombatStageSoGrid grid)
        {
            foreach (var placed in moved)
            {
                if (placed.Item is Vector2Int) continue;
                Vector2Int anchor = AnchorOf(placed.Item);
                var cells = CellsAt(placed.Item, anchor.x, anchor.y, RotationOf(placed.Item), grid);
                string error = PlacementError(placed.Layer, placed.Item, DefinitionOf(placed.Item), cells, grid);
                if (error != null) return $"{LayerName(placed.Layer)} ({anchor.x}, {anchor.y}): {error}";
            }
            if (railCells.Any(cell => !grid.Contains(cell))) return "레일 칸이 맵 밖으로 나갑니다.";
            if (railCells.Distinct().Count() != railCells.Count) return "레일 칸이 겹칩니다.";
            return null;
        }

        private bool TryInsert<T>(CombatStageSoLayer layer, T item, List<T> list, CombatStageSoGrid grid)
        {
            Vector2Int anchor = AnchorOf(item);
            if (PlacementError(layer, null, DefinitionOf(item), CellsFor(item, anchor.x, anchor.y, RotationOf(item), DefinitionOf(item), grid), grid) != null)
                return false;
            list.Add(item);
            return true;
        }

        private bool TryInsertMarker(CombatStageSoLayer layer, CombatStageSoMarker marker, List<CombatStageSoRotation> clipRotations, CombatStageSoGrid grid)
        {
            long copiedId = marker.sourceId;
            marker.sourceId = NextSourceId();
            if (!TryInsert(layer, marker, layer == CombatStageSoLayer.Terrain ? terrains : objects, grid)) return false;
            var rotation = clipRotations.Find(entry => entry.sourceId == copiedId);
            if (rotation != null)
                (layer == CombatStageSoLayer.Terrain ? view.terrainRotations : view.objectRotations)
                    .Add(new CombatStageSoRotation { sourceId = marker.sourceId, rotation = rotation.rotation });
            return true;
        }

        private void MirrorVisualRotation(CombatStageSoMarker marker, Func<int, int> mirrorRotation)
        {
            var rotation = (terrains.Contains(marker) ? view.terrainRotations : view.objectRotations).Find(entry => entry.sourceId == marker.sourceId);
            if (rotation != null) rotation.rotation = mirrorRotation(rotation.rotation);
        }

        private static T Shifted<T>(T source, Vector2Int offset)
        {
            T copy = Clone(source);
            switch (copy)
            {
                case CombatStageSoUnit unit: unit.x += offset.x; unit.y += offset.y; break;
                case CombatStageSoPiece piece: piece.x += offset.x; piece.y += offset.y; break;
                case CombatStageSoMarker marker: marker.x += offset.x; marker.y += offset.y; break;
            }
            return copy;
        }

        internal static T Clone<T>(T source) => JsonUtility.FromJson<T>(JsonUtility.ToJson(source));
    }

    // 값이 클수록 위 층입니다. 지우기는 위 층부터 지웁니다.
    internal enum CombatStageSoLayer { Rail = 1, Terrain, Object, Obstacle, Structure, Headquarters, Unit }

    internal readonly struct CombatStageSoPlaced
    {
        public readonly CombatStageSoLayer Layer;
        public readonly object Item;
        public readonly List<Vector2Int> Cells;

        public CombatStageSoPlaced(CombatStageSoLayer layer, object item, List<Vector2Int> cells)
        {
            Layer = layer; Item = item; Cells = cells;
        }
    }

    // 칸 계산에 쓰는 맵 크기와 정의 크기입니다. R90·R270이면 정의 크기의 가로·세로를 바꿉니다.
    internal sealed class CombatStageSoGrid
    {
        private readonly Func<string, Vector2Int> footprint;
        private readonly Func<int, bool> quarterTurn;
        public readonly int Columns, Rows;

        public CombatStageSoGrid(int columns, int rows, Func<string, Vector2Int> footprint, Func<int, bool> quarterTurn)
        {
            Columns = columns; Rows = rows;
            this.footprint = footprint ?? throw new ArgumentNullException(nameof(footprint));
            this.quarterTurn = quarterTurn ?? throw new ArgumentNullException(nameof(quarterTurn));
        }

        public bool Contains(Vector2Int cell) => cell.x >= 0 && cell.y >= 0 && cell.x < Columns && cell.y < Rows;

        public List<Vector2Int> Cells(int x, int y, string definitionGuid, int rotation)
        {
            Vector2Int size = string.IsNullOrEmpty(definitionGuid) ? Vector2Int.one : footprint(definitionGuid);
            if (quarterTurn(rotation)) size = new Vector2Int(size.y, size.x);
            var cells = new List<Vector2Int>(size.x * size.y);
            for (int dy = 0; dy < size.y; dy++)
                for (int dx = 0; dx < size.x; dx++)
                    cells.Add(new Vector2Int(x + dx, y + dy));
            return cells;
        }
    }

    [Serializable]
    internal sealed class CombatStageSoPose
    {
        public int x, y, rotation;
    }

    [Serializable]
    internal sealed class CombatStageSoPiece
    {
        public string definitionGuid = "";
        public int x, y, rotation;
    }

    [Serializable]
    internal sealed class CombatStageSoUnit
    {
        public string unitGuid = "", startingItemGuid = "";
        public int x, y, rotation;
        public bool isVictoryTarget;
    }

    // 지형과 오브젝트처럼 sourceId와 공개 대상을 가진 배치입니다. 무작위 시작은 토네이도 지형만 씁니다.
    [Serializable]
    internal sealed class CombatStageSoMarker
    {
        public string definitionGuid = "";
        public long sourceId;
        public int x, y;
        public List<int> knownTo = new List<int>();
        public bool randomizeInitialPosition;
        // 토네이도의 최초 가로 방향입니다(0 자동, -1 왼쪽, 1 오른쪽). 다른 지형은 쓰지 않습니다.
        public int tornadoHorizontalSign;
    }

    // 낙석 기믹 값입니다(설계 §17 RF1). 값 규칙은 Core RockfallGimmickSpec이 원본이라 여기서 검사하지 않습니다(RF4).
    [Serializable]
    internal sealed class CombatStageSoRockfall
    {
        public bool enabled;
        public int cooldownTurns, warningTurns, minimumRocks, maximumRocks, maximumHealthLossPercent, bindTurns;
        public List<Vector2Int> footprintOffsets = new List<Vector2Int>();
    }

    [Serializable]
    internal sealed class CombatStageSoHaboob
    {
        public bool enabled;
        public int relocationIntervalTurns, warningLeadTurns;
        public List<Vector2Int> footprintOffsets;
        // 전투 시작 위치입니다(설계 §25 HAT1). 켜면 모양의 상대 좌표 (0,0)을 initialAnchor 칸에 놓고, 끄면 무작위입니다. 꺼도 칸 값은 남깁니다.
        public bool fixedInitialAnchor;
        public Vector2Int initialAnchor;

        // 기본 주기와 모양은 Core 공개 계약에서 가져오고 작업본별 목록으로 복사합니다.
        public CombatStageSoHaboob()
        {
            var defaults = new HaboobGimmickSpec();
            relocationIntervalTurns = defaults.RelocationIntervalControllerTurns;
            warningLeadTurns = defaults.WarningLeadControllerTurns;
            footprintOffsets = defaults.FootprintOffsets.Select(cell => new Vector2Int(cell.X, cell.Y)).ToList();
        }

        // 지정한 시작 위치에서 구름이 덮는 칸입니다(설계 §25 HAT2). 칸 = 시작 위치 + 상대 좌표이고(Core HaboobGimmickRule.CellsAt),
        // 모양은 Core 공개 계약이 검사한 목록을 씁니다. 하붑이 꺼졌거나 무작위거나, 모양·시작 위치가 그 검사를 통과하지 못하면 null입니다.
        // 잘못된 값은 저장 검사가 "하붑 값:"으로 알립니다. 판 밖 칸도 그대로 돌려주고, 판 안인지는 부르는 쪽이 봅니다.
        public List<Vector2Int> StartCells()
        {
            if (!enabled || !fixedInitialAnchor) return null;
            HaboobGimmickSpec spec;
            try
            {
                spec = new HaboobGimmickSpec(footprintOffsets: footprintOffsets.Select(cell => new CellCoord(cell.x, cell.y)).ToList(),
                    initialAnchor: new CellCoord(initialAnchor.x, initialAnchor.y));
            }
            catch (ArgumentException) { return null; }
            return spec.FootprintOffsets.Select(cell => new Vector2Int(initialAnchor.x + cell.X, initialAnchor.y + cell.Y)).ToList();
        }

        // Core 공개 계약이 검사한 모양 칸 목록입니다(각 축 최솟값 0). 배치 도구가 누른 칸에서 시작 위치를 구할 때 씁니다(설계 §26 Q-GP1).
        // 모양이 잘못되면 null이고, 잘못된 값은 저장 검사가 "하붑 값:"으로 알립니다.
        public List<Vector2Int> ShapeOffsets()
        {
            try
            {
                return new HaboobGimmickSpec(footprintOffsets: footprintOffsets.Select(cell => new CellCoord(cell.x, cell.y)).ToList())
                    .FootprintOffsets.Select(cell => new Vector2Int(cell.X, cell.Y)).ToList();
            }
            catch (ArgumentException) { return null; }
        }
    }

    [Serializable]
    internal sealed class CombatStageSoView
    {
        // 배경 방식(StageViewPropMode)의 순번입니다. 방식이 쓰지 않는 칸(맵 SO·맵 프리팹·타일)도 지우지 않고 그대로 둡니다(설계 §12 UT2, P-UT4).
        public int mode;
        public string gameObjectGuid = "", mapGuid = "";
        public bool disableRockfallTunnelExplosion;
        public Vector3 positionOffset, rotationOffset, scaleOffset = Vector3.one;
        public CombatStageSoTileLayout tiles = new CombatStageSoTileLayout();
        public List<CombatStageSoRotation> terrainRotations = new List<CombatStageSoRotation>();
        public List<CombatStageSoRotation> objectRotations = new List<CombatStageSoRotation>();
    }

    [Serializable]
    internal sealed class CombatStageSoRotation
    {
        public long sourceId;
        public int rotation;
    }

    // 복사한 배치 묶음입니다(추가 계획 P-A3). 자리는 복사한 상자의 왼쪽 아래 기준 상대 값이고, 창이 들고 있어 다른 스테이지에도 붙일 수 있습니다.
    [Serializable]
    internal sealed class CombatStageSoClip
    {
        public List<CombatStageSoUnit> enemyUnits = new List<CombatStageSoUnit>();
        public List<CombatStageSoPiece> structures = new List<CombatStageSoPiece>();
        public List<CombatStageSoPiece> obstacles = new List<CombatStageSoPiece>();
        public List<CombatStageSoMarker> terrains = new List<CombatStageSoMarker>();
        public List<CombatStageSoMarker> objects = new List<CombatStageSoMarker>();
        public List<Vector2Int> railCells = new List<Vector2Int>();
        public List<CombatStageSoRotation> terrainRotations = new List<CombatStageSoRotation>();
        public List<CombatStageSoRotation> objectRotations = new List<CombatStageSoRotation>();

        public int Count => enemyUnits.Count + structures.Count + obstacles.Count + terrains.Count + objects.Count + railCells.Count;

        // 담긴 배치마다 새 자리·회전을 매기고 외형 회전도 바꿉니다(좌우 대칭 복사).
        public void Transform(Func<object, (Vector2Int Anchor, int Rotation)> pose, Func<int, int> visualRotation)
        {
            foreach (var unit in enemyUnits)
            {
                var next = pose(unit);
                unit.x = next.Anchor.x; unit.y = next.Anchor.y; unit.rotation = next.Rotation;
            }
            foreach (var piece in structures.Concat(obstacles))
            {
                var next = pose(piece);
                piece.x = next.Anchor.x; piece.y = next.Anchor.y; piece.rotation = next.Rotation;
            }
            foreach (var marker in terrains.Concat(objects))
            {
                var next = pose(marker);
                marker.x = next.Anchor.x; marker.y = next.Anchor.y;
                marker.tornadoHorizontalSign = -marker.tornadoHorizontalSign;
            }
            for (int i = 0; i < railCells.Count; i++) railCells[i] = pose(railCells[i]).Anchor;
            foreach (var rotation in terrainRotations.Concat(objectRotations)) rotation.rotation = visualRotation(rotation.rotation);
        }
    }

    // 공유 SO 한 개의 저장 안 한 값입니다. 값은 EditorJsonUtility JSON이라 형식을 몰라도 담고 되돌릴 수 있습니다.
    [Serializable]
    internal sealed class CombatStageSoSharedEdit
    {
        public string guid = "", json = "";
    }
}
