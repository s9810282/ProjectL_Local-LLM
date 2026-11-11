using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // 외형 하나입니다. 유닛 외형은 프리팹만 쓰고 오프셋 0·배율 1·표면 높이 0입니다.
    // ReplacesTile은 지형 외형 방식이 바닥 교체(ReplaceTile: 약한 지반·낭떠러지)인지입니다(계획 P-UTT6). 다른 외형은 거짓입니다.
    internal sealed class CombatStageSoVisual
    {
        public readonly GameObject Prefab;
        public readonly Vector3 LocalOffset, LocalScale;
        public readonly float SurfaceHeight;
        public readonly bool ReplacesTile;

        public CombatStageSoVisual(GameObject prefab, Vector3 localOffset, Vector3 localScale, float surfaceHeight, bool replacesTile)
        {
            Prefab = prefab; LocalOffset = localOffset; LocalScale = localScale; SurfaceHeight = surfaceHeight; ReplacesTile = replacesTile;
        }
    }

    // 필드 보기 값입니다. Approximate면 칸 크기를 1×1로 근사했다는 뜻입니다(추가 계획 P-A10).
    internal sealed class CombatStageSoFieldView
    {
        public readonly float CellWidth, CellLength, UnitScale;
        public readonly bool Approximate;

        public CombatStageSoFieldView(float cellWidth, float cellLength, float unitScale, bool approximate)
        {
            CellWidth = cellWidth > 0f ? cellWidth : 1f;
            CellLength = cellLength > 0f ? cellLength : 1f;
            UnitScale = unitScale > 0f ? unitScale : 1f;
            Approximate = approximate;
        }
    }

    // 3D 미리보기가 쓰는 외형 표입니다(추가 설계 AD1). 목록 새로고침 때 다시 만듭니다.
    internal sealed class CombatStageSoVisuals
    {
        private readonly Dictionary<int, CombatStageSoVisual> units, terrains, objects;

        private CombatStageSoVisuals(Dictionary<int, CombatStageSoVisual> units, Dictionary<int, CombatStageSoVisual> terrains,
            Dictionary<int, CombatStageSoVisual> objects, CombatStageSoFieldView fieldView)
        {
            this.units = units; this.terrains = terrains; this.objects = objects; FieldView = fieldView;
        }

        public CombatStageSoFieldView FieldView { get; }

        // 잘못된 입력은 공개로 간주하지 않되, 편집 UI가 원인을 보여 주고 복구할 수 있게 합니다.
        internal static bool TryIsShown(object item, bool playerOnly, string[] owners, out string problem)
        {
            problem = null;
            try { return IsShown(item, playerOnly, owners); }
            catch (InvalidOperationException exception)
            {
                problem = exception.Message;
                return false;
            }
        }

        // CombatEnvironmentFacts.IsKnownToPlayer 사본. 열거 숫자가 아닌 이름으로 판정합니다.
        internal static bool IsShown(object item, bool playerOnly, string[] owners)
        {
            if (!playerOnly || !(item is CombatStageSoMarker marker)) return true;
            if (CombatStageSoAssets.Load(marker.definitionGuid) == null)
                throw new InvalidOperationException("공개 대상 판정에 필요한 정의가 없습니다: " + marker.definitionGuid);
            if (marker.knownTo == null)
                throw new InvalidOperationException("공개 대상 목록이 없습니다.");
            bool shown = false;
            foreach (int index in marker.knownTo)
            {
                if (owners == null || index < 0 || index >= owners.Length ||
                    (owners[index] != "None" && owners[index] != "Player" && owners[index] != "Enemy" && owners[index] != "Neutral"))
                    throw new InvalidOperationException("공개 대상 열거 이름이 잘못되었습니다.");
                if (owners[index] == "Player") shown = true;
            }
            return shown;
        }


        public static CombatStageSoVisuals Build() =>
            new CombatStageSoVisuals(CombatStageSoAssets.UnitVisuals(), CombatStageSoAssets.TerrainVisuals(),
                CombatStageSoAssets.ObjectVisuals(), CombatStageSoAssets.ReadRegisteredFieldView());

        // 유닛·구조물·장애물·본부는 유닛 외형 표에서, 지형·오브젝트는 각자 외형 표에서 찾습니다. 없으면 null입니다.
        public CombatStageSoVisual Of(CombatStageSoLayer layer, string definitionGuid)
        {
            UnityEngine.Object definition = CombatStageSoAssets.Load(definitionGuid);
            if (definition == null) return null;
            int id;
            try { id = CombatStageSoAssets.VisualId(definition); }
            catch (ArgumentOutOfRangeException) { return null; }
            var table = layer == CombatStageSoLayer.Terrain ? terrains : layer == CombatStageSoLayer.Object ? objects : units;
            return table.TryGetValue(id, out var visual) ? visual : null;
        }
    }

    // 3D 미리보기의 칸 공간과 배치 식입니다. 게임 View 코드의 식을 그대로 옮긴 사본이고(Q-AD1 A), 식마다 원본 자리를 적었습니다.
    // 게임 식이 바뀌면 여기도 맞춰야 합니다. 공간은 게임의 필드 로컬(맵 프랍과 칸이 같이 놓이는 CellsRoot 아래)과 같습니다.
    internal sealed class CombatStageSoSpace
    {
        // FieldUnitPresenter.EnemyFacing: 적 모델은 기준 방향에서 반 바퀴 돕니다.
        public static readonly Quaternion EnemyFacing = Quaternion.Euler(0f, 180f, 0f);

        // 칸 높이는 게임 FieldSurfaceHeights와 같은 두 층입니다(설계 §12 UT6): 바탕(타일 윗면, 바닥 교체 칸은 그 높이)과 그 위 지형 높이.
        private readonly Dictionary<Vector2Int, float> replacedBases = new Dictionary<Vector2Int, float>();
        private readonly Dictionary<Vector2Int, float> terrains = new Dictionary<Vector2Int, float>();
        private readonly CombatStageSoTileScene tiles;

        // tiles는 맞춤이 성립한 바닥 타일입니다. 없으면 바탕은 0(바닥면)입니다.
        public CombatStageSoSpace(int columns, int rows, CombatStageSoFieldView fieldView, CombatStageSoTileScene tiles = null)
        {
            Columns = columns; Rows = rows;
            CellWidth = fieldView.CellWidth; CellLength = fieldView.CellLength;
            this.tiles = tiles;
        }

        public int Columns { get; }
        public int Rows { get; }
        public float CellWidth { get; }
        public float CellLength { get; }
        public CombatStageSoTileScene Tiles => tiles;
        public string Signature => $"{Columns}x{Rows}|{CellWidth}x{CellLength}|{tiles?.Signature}";

        // FieldTerrainPresenter.FillSurface + FieldSurfaceHeights.Replace·Raise: 바닥 교체(ReplaceTile)는 프리팹이 없어도 그 칸 바탕을 외형 높이로 바꾸고
        // (한 칸에 여럿이면 낮은 값), 타일 위(OnTile)는 바탕 위로 외형 높이만큼 올립니다(한 칸에 여럿이면 높은 값, 0 이하는 무시).
        // 게임은 Player에게 보이는 지형만 셉니다. 도구는 현재 보기의 표시 집합만 셉니다.
        public void RaiseSurfaces(CombatStageSoDocument document, CombatStageSoVisuals visuals, Func<object, bool> isShown = null)
        {
            replacedBases.Clear();
            terrains.Clear();
            foreach (var terrain in document.terrains.Where(marker => !marker.randomizeInitialPosition))
            {
                if (isShown != null && !isShown(terrain)) continue;
                var visual = visuals.Of(CombatStageSoLayer.Terrain, terrain.definitionGuid);
                if (visual == null) continue;
                var cell = new Vector2Int(terrain.x, terrain.y);
                if (visual.ReplacesTile)
                {
                    if (!replacedBases.TryGetValue(cell, out float replaced) || visual.SurfaceHeight < replaced) replacedBases[cell] = visual.SurfaceHeight;
                }
                else if (visual.Prefab != null && visual.SurfaceHeight > OverlayAt(cell)) terrains[cell] = visual.SurfaceHeight;
            }
        }

        // 칸 바탕입니다(FieldSurfaceHeights.BaseHeightAt): 바닥 교체 칸은 그 높이, 아니면 타일 윗면(타일 밖은 0).
        public float BaseAt(Vector2Int cell) => replacedBases.TryGetValue(cell, out float height) ? height : tiles?.BaseHeightAt(cell) ?? 0f;

        // 지형이 올린 높이입니다(FieldSurfaceHeights.OverlayHeightAt). 칸 위 덧그리기는 이 높이만 따릅니다.
        public float OverlayAt(Vector2Int cell) => terrains.TryGetValue(cell, out float height) ? height : 0f;

        // 칸 표면 높이 = 바탕 + 지형입니다(FieldSurfaceHeights.HeightAt).
        public float SurfaceAt(Vector2Int cell) => BaseAt(cell) + OverlayAt(cell);

        // FieldTornadoPresenter.BodyPosition: 칸은 중심 기준 정사각형이고, 높이는 덮는 칸의 교체 전 타일 바탕 최대값입니다.
        // 게임은 판 밖으로 나가는 중심을 받지 않습니다. 도구는 저장 검사로 알리고 그리기는 판 안 칸만 봅니다.
        internal Vector3 TornadoPosition(Vector2Int center, int sideLength)
        {
            int radius = sideLength / 2;
            float height = float.NegativeInfinity;
            for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                {
                    var cell = center + new Vector2Int(dx, dy);
                    if (cell.x < 0 || cell.y < 0 || cell.x >= Columns || cell.y >= Rows) continue;
                    height = Mathf.Max(height, tiles?.BaseHeightAt(cell) ?? 0f);
                }
            if (float.IsNegativeInfinity(height)) height = 0f;
            return CellCenter(center) + Vector3.up * height;
        }

        public bool ReplacesBase(Vector2Int cell) => replacedBases.ContainsKey(cell);

        // FieldGridMapper.CellCenterToLocal: 필드 가운데가 원점이고 칸 중심은 높이 0입니다.
        public Vector3 CellCenter(Vector2Int cell) =>
            new Vector3((cell.x + .5f) * CellWidth - Columns * CellWidth * .5f, 0f, (cell.y + .5f) * CellLength - Rows * CellLength * .5f);

        // FieldGridMapper.RotationToWorld: 회전 값 × 90°(Y축). R0은 필드 +Z를 봅니다.
        public static Quaternion Facing(int rotation) => Quaternion.Euler(0f, rotation * 90f, 0f);

        // FieldUnitPresenter.CenterOf + FieldGridMapper.SurfaceHeightOf: 점유 칸 중심 평균 + 점유 칸 표면 높이 중 가장 높은 값입니다.
        // 높이는 첫 점유 칸에서 시작합니다(들어간 타일만 점유하면 바닥면에 뜨지 않게).
        public Vector3 StandingPoint(IReadOnlyList<Vector2Int> cells)
        {
            Vector3 sum = Vector3.zero;
            float height = cells.Count > 0 ? SurfaceAt(cells[0]) : 0f;
            foreach (var cell in cells)
            {
                sum += CellCenter(cell);
                height = Mathf.Max(height, SurfaceAt(cell));
            }
            return sum / Mathf.Max(1, cells.Count) + Vector3.up * height;
        }

        // FieldGridMapper.WorldToGrid: 필드 로컬 위치를 칸으로 바꿉니다. 맵 밖이면 null입니다.
        public Vector2Int? CellAt(Vector3 local)
        {
            Vector2Int cell = RawCellAt(local);
            return cell.x >= 0 && cell.y >= 0 && cell.x < Columns && cell.y < Rows ? cell : (Vector2Int?)null;
        }

        public Vector2Int ClampedCellAt(Vector3 local)
        {
            Vector2Int cell = RawCellAt(local);
            return new Vector2Int(Mathf.Clamp(cell.x, 0, Columns - 1), Mathf.Clamp(cell.y, 0, Rows - 1));
        }

        // 칸 사각형의 네 모서리(바닥 위로 lift만큼)입니다. 덧그리기가 씁니다.
        public Vector3[] Corners(RectInt cells, float lift) => Corners(new Rect(cells.x, cells.y, cells.width, cells.height), lift);

        // 타일 테두리는 소수 칸 범위도 그대로 그립니다.
        public Vector3[] Corners(Rect cells, float lift)
        {
            float left = cells.xMin * CellWidth - Columns * CellWidth * .5f, right = cells.xMax * CellWidth - Columns * CellWidth * .5f;
            float bottom = cells.yMin * CellLength - Rows * CellLength * .5f, top = cells.yMax * CellLength - Rows * CellLength * .5f;
            // 덧그리기는 타일 높이를 보지 않고 지형이 올린 높이만 따릅니다(게임 격자·호버와 같음, 설계 UT6).
            bool singleCell = cells.width == 1f && cells.height == 1f && cells.x == Mathf.Floor(cells.x) && cells.y == Mathf.Floor(cells.y);
            float y = lift + (singleCell ? OverlayAt(new Vector2Int((int)cells.x, (int)cells.y)) : 0f);
            return new[] { new Vector3(left, y, bottom), new Vector3(right, y, bottom), new Vector3(right, y, top), new Vector3(left, y, top) };
        }

        // 판 밖도 내는 칸 번호입니다. 3D '타일 회전' 도구가 판 밖 타일을 고를 때 씁니다(설계 §14 TO2).
        public Vector2Int RawCellAt(Vector3 local) =>
            new Vector2Int(Mathf.FloorToInt((local.x + Columns * CellWidth * .5f) / CellWidth), Mathf.FloorToInt((local.z + Rows * CellLength * .5f) / CellLength));
    }
}
