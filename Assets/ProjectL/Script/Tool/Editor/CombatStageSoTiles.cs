using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Tool.Runtime;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // '타일' 보기에서 복사한 타일입니다(설계 §19 TC1). 창이 들고 있어 다른 스테이지에도 붙일 수 있고, 작업본에는 들어가지 않습니다.
    [Serializable]
    internal sealed class CombatStageSoTileClip
    {
        public List<CombatStageSoTileClipCell> cells = new List<CombatStageSoTileClipCell>();
    }

    // 배경 방향이 다른 스테이지에서도 보이는 모양이 같도록 자리와 각을 화면 기준으로 담습니다.
    [Serializable]
    internal sealed class CombatStageSoTileClipCell
    {
        // 복사한 묶음의 왼쪽 아래에서 본 화면 자리입니다.
        public Vector2Int offset;
        public int kind;
        // 화면에서 본 90° 단위 각(0~3) 또는 RandomRotation입니다.
        public int rotation;
    }

    // 작업본의 바닥 타일 배치입니다(설계 §12 UT1). View `stageViewPropData.mapTiles`(게임 CombatMapTilesInput)의 칸을 그대로 담습니다.
    // 세트는 GUID, 종류는 열거 값(intValue)입니다. 열거의 표시 순서가 바뀌어도 저장한 종류 값은 유지합니다.
    // 종류 배열은 에셋과 같은 MapTileRoot 순서(−Z 줄부터, 줄 안은 −X부터, 길이 = 가로 × 세로)입니다. 화면 방향 대응은 맞춤 계산이 맡습니다.
    [Serializable]
    internal sealed class CombatStageSoTileLayout
    {
        // 게임 표시 입력 CombatMapTileRotation.Random과 같은 직렬화 값입니다.
        public const int RandomRotation = 4;
        public string tileSetGuid = "";
        public int columns, rows;
        public Vector2 tileSize = new Vector2(2f, 2f);
        public Vector3 offset;
        public Vector2Int anchorTile;
        public int[] kinds = Array.Empty<int>();
        // 칸마다 회전(CombatMapTileRotation 값 0~4, 4 = Random)입니다. 순서는 종류와 같고, 비어 있으면 모두 0입니다.
        public int[] rotations = Array.Empty<int>();

        // 게임 규칙(CombatMapTilesInput.CreateData)과 같이 세트·가로·세로·종류·회전이 모두 비면 타일이 없습니다.
        public bool IsEmpty => string.IsNullOrEmpty(tileSetGuid) && columns == 0 && rows == 0 && (kinds == null || kinds.Length == 0)
                               && (rotations == null || rotations.Length == 0);

        // 종류 수가 가로 × 세로와 같은지입니다. 다르면 저장 검사가 거절합니다(게임 CombatMapTilesInput.CreateData).
        public bool HasFullKinds => kinds != null && columns > 0 && rows > 0 && kinds.Length == columns * rows;

        // 회전 목록 길이가 게임이 받는 값(0 또는 가로 × 세로)인지입니다. 아니면 저장 검사가 거절합니다.
        public bool HasValidRotations => rotations == null || rotations.Length == 0 || HasFullRotations;

        // 0°가 아닌 칸이 있는지입니다. 없으면 저장 때 빈 목록으로 씁니다(계획 P-TTT1).
        public bool HasRotations => HasFullRotations && rotations.Any(rotation => rotation != 0);

        private bool HasFullRotations => rotations != null && columns > 0 && rows > 0 && rotations.Length == columns * rows;

        public int KindAt(int column, int row) => kinds[Index(column, row)];

        public void SetKind(int column, int row, int kind) => kinds[Index(column, row)] = kind;

        // 칸의 회전입니다. 목록이 비었거나 길이가 틀리면 0으로 봅니다(길이 문제는 저장 검사가 알림).
        public int RotationAt(int column, int row)
        {
            int index = Index(column, row);
            return HasFullRotations ? rotations[index] : 0;
        }

        // 칸의 회전을 바꿉니다. 목록이 비었거나 길이가 틀리면 가로 × 세로 0 목록을 새로 만든 뒤 바꿉니다.
        public void SetRotation(int column, int row, int rotation)
        {
            if (rotation < 0 || rotation > RandomRotation) throw new ArgumentOutOfRangeException(nameof(rotation));
            int index = Index(column, row);
            if (!HasFullRotations) rotations = new int[columns * rows];
            rotations[index] = rotation;
        }

        // 저장 옵션을 바꾸지 않고 창의 표시 시드로 예시 각만 계산합니다. 2D 정보·타일 앞쪽 띠·3D가 같은 값을 씁니다.
        public int ResolvedRotationAt(int seed, int column, int row)
        {
            int rotation = RotationAt(column, row);
            if (rotation == RandomRotation)
                return MapTileRandomSelection.RotationQuarterTurns(seed, column, row, KindAt(column, row), Mathf.Approximately(tileSize.x, tileSize.y));
            if (rotation < 0 || rotation > 3)
                throw new InvalidOperationException($"바닥 타일 ({column}, {row})의 회전 값 {rotation}이 잘못되었습니다.");
            return rotation;
        }

        // 가로·세로를 바꿉니다. 같은 (가로, 세로) 배열 칸의 종류·회전은 지키고 새 칸은 fillKind·0입니다. 줄이면 밖의 칸은 버립니다(설계 P-UT2·TT4).
        // 회전 목록이 비어 있으면 그대로 비우고, 길이가 틀리면 손대지 않습니다(저장 검사가 알림).
        public void Resize(int nextColumns, int nextRows, int fillKind)
        {
            if (nextColumns < 1) throw new ArgumentOutOfRangeException(nameof(nextColumns));
            if (nextRows < 1) throw new ArgumentOutOfRangeException(nameof(nextRows));
            bool keepRotations = HasFullRotations;
            var next = new int[nextColumns * nextRows];
            var nextRotations = keepRotations ? new int[nextColumns * nextRows] : rotations;
            for (int row = 0; row < nextRows; row++)
            {
                for (int column = 0; column < nextColumns; column++)
                {
                    int old = column + row * columns;
                    bool kept = column < columns && row < rows && kinds != null && old < kinds.Length;
                    next[column + row * nextColumns] = kept ? kinds[old] : fillKind;
                    if (keepRotations && column < columns && row < rows) nextRotations[column + row * nextColumns] = rotations[old];
                }
            }
            columns = nextColumns; rows = nextRows; kinds = next; rotations = nextRotations;
        }

        // '타일 만들기' 기본값입니다(설계 P-UT3). 타일 한 장이 필드 칸 하나가 되도록 크기를 칸 간격 ÷ 배경 배율로 두고,
        // 타일 가로가 필드 세로(Z)를 따르는 배경 회전이면 가로·세로와 크기를 바꿉니다. 위치·기준 타일은 0이고, 구멍에 맞추는 값은 기획자가 고칩니다.
        public static CombatStageSoTileLayout CreateDefault(string tileSetGuid, int fieldColumns, int fieldRows, float cellWidth, float cellLength,
            Vector3 backdropScale, Vector3 rotationOffset, int fillKind)
        {
            if (fieldColumns < 1) throw new ArgumentOutOfRangeException(nameof(fieldColumns));
            if (fieldRows < 1) throw new ArgumentOutOfRangeException(nameof(fieldRows));
            bool swapped = !CombatStageSoTileFit.ColumnsAlongFieldX(rotationOffset, backdropScale);
            var layout = new CombatStageSoTileLayout
            {
                tileSetGuid = tileSetGuid ?? "",
                columns = swapped ? fieldRows : fieldColumns,
                rows = swapped ? fieldColumns : fieldRows,
                tileSize = new Vector2(Unscaled(swapped ? cellLength : cellWidth, backdropScale.x), Unscaled(swapped ? cellWidth : cellLength, backdropScale.z)),
            };
            layout.kinds = new int[layout.columns * layout.rows];
            for (int i = 0; i < layout.kinds.Length; i++) layout.kinds[i] = fillKind;
            return layout;
        }

        private int Index(int column, int row)
        {
            if (column < 0 || column >= columns) throw new ArgumentOutOfRangeException(nameof(column));
            if (row < 0 || row >= rows) throw new ArgumentOutOfRangeException(nameof(row));
            return column + row * columns;
        }

        private static float Unscaled(float length, float scale) => Mathf.Abs(scale) > 1e-5f ? length / Mathf.Abs(scale) : length;
    }

    // 3D 미리보기와 칸 높이가 쓰는 바닥 타일 묶음입니다(설계 §12 UT5·UT6). 맞춤이 성립할 때만 만듭니다(설계 P-UT13).
    // 칸 → 타일 대응은 2D 띠와 같은 맞춤 계산 하나(CombatStageSoTileFit.TryTileOfCell)를 씁니다(설계 P-UT12).
    internal sealed class CombatStageSoTileScene
    {
        private readonly Dictionary<int, CombatStageSoTileKind> kinds;

        public CombatStageSoTileScene(CombatStageSoTileFit fit, CombatStageSoTileLayout layout, Dictionary<int, CombatStageSoTileKind> kinds, int seed)
        {
            Fit = fit ?? throw new ArgumentNullException(nameof(fit));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            this.kinds = kinds ?? throw new ArgumentNullException(nameof(kinds));
            Seed = seed;
            if (!fit.IsValid) throw new ArgumentException("맞춤이 성립하지 않는 바닥 타일입니다.", nameof(fit));
            // 맵 다시 만들기 열쇠입니다. 배치 값과 세트 종류(프리팹·높이)가 바뀌면 달라집니다.
            Signature = JsonUtility.ToJson(layout) + "|" + seed + "|" + string.Join(",",
                kinds.Values.OrderBy(kind => kind.Value).Select(kind => $"{kind.Value}:{kind.PrefabSignature}:{kind.SurfaceHeight}"));
        }

        public CombatStageSoTileFit Fit { get; }
        public CombatStageSoTileLayout Layout { get; }
        public string Signature { get; }
        public int Seed { get; }

        public GameObject PrefabAt(int column, int row) => kinds.TryGetValue(Layout.KindAt(column, row), out var known)
            ? known.PrefabFor(Seed, column, row) : null;

        // CombatMapTileSurface.Build: 칸 가운데가 놓인 타일의 윗면 높이입니다. 타일 맵 밖 칸은 0(바닥면)입니다.
        public float BaseHeightAt(Vector2Int cell) =>
            Fit.TryTileOfCell(cell, out int column, out int row) && kinds.TryGetValue(Layout.KindAt(column, row), out var kind) ? kind.SurfaceHeight : 0f;
    }

    // 타일 세트의 종류 한 줄입니다(게임 CombatMapTileSetSO.Entry). 이름은 열거 이름이고, 열거에 없는 값이면 "(값 n)"입니다.
    internal sealed class CombatStageSoTileKind
    {
        public readonly int Value;
        public readonly string Name;
        private readonly GameObject[] _prefabs;
        public readonly float SurfaceHeight;

        public CombatStageSoTileKind(int value, string name, GameObject[] prefabs, float surfaceHeight)
        {
            Value = value; Name = name;
            _prefabs = (GameObject[])(prefabs ?? throw new ArgumentNullException(nameof(prefabs))).Clone();
            SurfaceHeight = surfaceHeight;
        }

        public int PrefabCount => _prefabs.Length;
        public GameObject PrefabAt(int index) => _prefabs[index];
        public string PrefabSignature => string.Join("/", _prefabs.Select(prefab => prefab != null ? prefab.GetInstanceID() : 0));

        // 잘못된 세트도 수정할 수 있도록 빈 후보는 미리보기에서만 비웁니다. 저장 검사는 게임의 CreateVisuals가 거절합니다.
        public GameObject PrefabFor(int seed, int column, int row) => _prefabs.Length == 0 ? null
            : _prefabs[MapTileRandomSelection.PrefabIndex(seed, column, row, Value, _prefabs.Length)];
    }

    // 배경 방식 규칙의 사본입니다(설계 §12 UT2). 원본은 StageViewPropData.GameObject·Map·Validate이고, 방식은 열거 이름으로 봅니다(계획 P-UTT6과 같은 이름 방식).
    // 이름을 모르면(열거 이름을 아직 읽지 못함) Known이 거짓이고, 부르는 쪽은 맵 SO·맵 프리팹 칸을 모두 보입니다.
    internal static class CombatStageSoBackground
    {
        public const string Prefab = "Prefab", MapDefinition = "MapDefinition", PrefabWithTiles = "PrefabWithTiles";

        public static bool Known(string mode) => mode == Prefab || mode == MapDefinition || mode == PrefabWithTiles;

        // 맵 SO의 프리팹으로 세우는 방식인지입니다. 다른 방식은 맵 프리팹(gameObject)을 씁니다.
        public static bool UsesMap(string mode) => mode == MapDefinition;

        public static bool UsesTiles(string mode) => mode == PrefabWithTiles;

        public static string Label(string mode)
        {
            switch (mode)
            {
                case Prefab: return "프리팹";
                case MapDefinition: return "맵 SO";
                case PrefabWithTiles: return "프리팹 + 바닥 타일";
                default: return mode;
            }
        }
    }

    // 바닥 타일을 필드에 맞추는 계산의 사본입니다(설계 §12 UT4·UT7·UT10). 상태가 없는 값이고 Transform을 만들지 않습니다.
    // 원본: StageViewPropPresenter.ShowTiles(차례), CombatMapTileAlignment(RequireLevel·YawCorrection·AnchorShift·CellRatio),
    // FieldCellSpacing.FromGridOutline(GridOutline 전제, 설계 P-UT13), 칸 → 타일 대응 CombatMapTileSurface.Build.
    // 화면 좌표는 anchorTile과 같은 필드 축 기준(좌하단 = 필드 −X·−Z, 설계 P-UT1)이고, 배열 좌표는 에셋 순서(MapTileRoot 기준)입니다.
    internal sealed class CombatStageSoTileFit
    {
        // 게임 CombatMapTileAlignment와 같은 허용 오차입니다.
        private const float LevelToleranceDegrees = 1f;

        private readonly int columns, rows;
        private readonly bool columnsAlongX, columnPositive, rowPositive;
        private readonly Vector2Int board;

        private CombatStageSoTileFit(CombatStageSoTileLayout layout, Vector3 rotationOffset, Vector3 backdropScale, Vector2Int board)
        {
            columns = layout.columns; rows = layout.rows; this.board = board;
            Anchor = layout.anchorTile;
            HasOrientation = columns > 0 && rows > 0;
            Axes(rotationOffset, backdropScale, out columnsAlongX, out columnPositive, out rowPositive);
            ScreenColumns = columnsAlongX ? columns : rows;
            ScreenRows = columnsAlongX ? rows : columns;
        }

        // 전투에 들어갈 때 멈추는 원인입니다(게임 예외). 하나라도 있으면 필드와 타일의 대응(필드 자리·2D 띠·칸 → 타일)을 내지 않습니다.
        public List<string> Problems { get; } = new List<string>();
        // 게임이 경고만 하는 것입니다(필드가 타일 맵 밖으로 넘침, 타일 한 장에 칸 여럿).
        public List<string> Warnings { get; } = new List<string>();
        public bool IsValid => Problems.Count == 0;
        // 가로·세로가 1 이상이라 화면 방향 대응을 낼 수 있는지입니다. 맞춤 문제가 있어도 타일 보기는 그릴 수 있습니다.
        public bool HasOrientation { get; }
        public int ScreenColumns { get; }
        public int ScreenRows { get; }
        public Vector2Int Anchor { get; }
        // 타일 한 장의 필드 칸 간격에 대한 실수 비율입니다(필드 X·Z축). IsValid일 때 양의 유한 값입니다.
        public float CellsPerTileX { get; private set; }
        public float CellsPerTileZ { get; private set; }
        public bool FieldExceedsTiles { get; private set; }

        // 필드가 놓이는 타일 칸(화면 좌표)입니다. 타일 맵 밖으로 넘칠 수 있습니다. IsValid일 때만 뜻이 있습니다.
        public Rect FieldTiles => new Rect(Anchor.x, Anchor.y, board.x / CellsPerTileX, board.y / CellsPerTileZ);

        public static CombatStageSoTileFit Of(CombatStageSoTileLayout layout, bool isGridOutline, Vector3 rotationOffset, Vector3 backdropScale,
            int boardColumns, int boardRows, float cellWidth, float cellLength)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            var fit = new CombatStageSoTileFit(layout, rotationOffset, backdropScale, new Vector2Int(boardColumns, boardRows));
            if (!fit.HasOrientation || !layout.HasFullKinds)
            {
                fit.Problems.Add("바닥 타일 칸 종류 수가 가로 × 세로와 다릅니다. 저장 검사가 막습니다.");
                return fit;
            }
            if (!isGridOutline)
                fit.Problems.Add("필드 표시 방식이 GridOutline이 아니라 바닥 타일을 칸에 맞출 수 없어 전투에 들어갈 때 멈춥니다(필드 보기 SO의 Field Display Mode).");
            // RequireLevel: 타일 격자 면(배경 위 방향)이 필드 바닥면과 평행해야 합니다. 축 평행(RequireAlong)은 기울기가 없으면 늘 성립합니다.
            float tilt = Vector3.Angle(Quaternion.Euler(rotationOffset) * Vector3.up, Vector3.up);
            if (tilt > LevelToleranceDegrees)
                fit.Problems.Add($"배경 회전 보정의 X·Z 때문에 타일 격자가 {tilt:0.##}° 기울어 전투에 들어갈 때 멈춥니다. 타일 방식은 Y 회전만 씁니다.");
            if (!float.IsFinite(layout.tileSize.x) || !(layout.tileSize.x > 0f)
                || !float.IsFinite(layout.tileSize.y) || !(layout.tileSize.y > 0f))
            {
                fit.Problems.Add("타일 크기는 0보다 큰 유한 값이어야 합니다. 저장 검사가 막습니다.");
                return fit;
            }
            // 타일 한 장의 필드 길이 = 타일 크기 × 배경 배율(타일 루트는 배경 자식, 배율 1). 가로가 필드 Z를 따르면 축이 바뀝니다(AnchorShift).
            float alongX = fit.columnsAlongX ? layout.tileSize.x * Mathf.Abs(backdropScale.x) : layout.tileSize.y * Mathf.Abs(backdropScale.z);
            float alongZ = fit.columnsAlongX ? layout.tileSize.y * Mathf.Abs(backdropScale.z) : layout.tileSize.x * Mathf.Abs(backdropScale.x);
            float ratioX = CellRatio(alongX, cellWidth, "X", fit.Problems);
            float ratioZ = CellRatio(alongZ, cellLength, "Z", fit.Problems);
            if (fit.Anchor.x < 0 || fit.Anchor.x >= fit.ScreenColumns || fit.Anchor.y < 0 || fit.Anchor.y >= fit.ScreenRows)
                fit.Problems.Add($"기준 타일 ({fit.Anchor.x}, {fit.Anchor.y})이 타일 맵(화면 기준 {fit.ScreenColumns} × {fit.ScreenRows}) 밖이라 전투에 들어갈 때 멈춥니다.");
            if (!fit.IsValid) return fit;

            fit.CellsPerTileX = ratioX; fit.CellsPerTileZ = ratioZ;
            // AnchorShift: 필드 좌하단은 기준 타일 좌하단이라, 넘칠 수 있는 쪽은 +X·+Z뿐입니다(칸 단위로 셈).
            fit.FieldExceedsTiles = boardColumns > (fit.ScreenColumns - fit.Anchor.x) * ratioX
                                    || boardRows > (fit.ScreenRows - fit.Anchor.y) * ratioZ;
            if (fit.FieldExceedsTiles)
                fit.Warnings.Add($"필드({boardColumns} × {boardRows})가 기준 타일 ({fit.Anchor.x}, {fit.Anchor.y})에서 타일 맵 밖으로 넘쳐, 넘친 칸은 배경 위에 놓입니다.");
            if (fit.SharesTiles())
                fit.Warnings.Add("바닥 타일 한 장에 필드 칸이 둘 이상 놓입니다. 바닥 교체(약한 지반·낭떠러지) 칸은 그 타일 전체를 비웁니다.");
            return fit;
        }

        // 타일 가로(MapTileRoot X)가 필드 X축을 따르는지입니다(AnchorShift의 columnsAlongX). 게임은 맞춤 때 남은 각만큼 배경을 돌려
        // 타일 가로축을 가장 가까운 필드 축에 맞추므로(YawCorrection), 회전 보정을 적용한 가로축이 어느 축에 가까운지로 정합니다(계획 P-UTT10).
        public static bool ColumnsAlongFieldX(Vector3 rotationOffset, Vector3 backdropScale)
        {
            Axes(rotationOffset, backdropScale, out bool alongX, out _, out _);
            return alongX;
        }

        // 칸(필드 좌표)이 놓인 타일의 배열 좌표입니다(CombatMapTileSurface.Build의 "칸 가운데가 놓인 타일"과 같은 대응, 설계 P-UT11).
        // 맞춤 문제가 있거나 타일 맵 밖 칸이면 false입니다.
        public bool TryTileOfCell(Vector2Int cell, out int column, out int row)
        {
            column = row = 0;
            if (!TryScreenTileOfCell(cell, out Vector2Int screen)) return false;
            ScreenToArray(screen.x, screen.y, out column, out row);
            return true;
        }

        // 칸(필드 좌표)의 중심이 놓인 타일의 화면 좌표입니다. 소수 비율에서도 런타임과 같은 중심을 씁니다.
        // 판 밖 칸(음수·판 크기 이상)도 같은 식으로 셉니다(3D 판 밖 타일 고르기, 설계 §14 TO1). 판 안 칸의 결과는 그대로이고,
        // 원본 CombatMapTileSurface.Build는 판 안 칸만 묻습니다. 결과가 타일 맵 밖이면 false입니다.
        public bool TryScreenTileOfCell(Vector2Int cell, out Vector2Int screen)
        {
            screen = default;
            if (!IsValid) return false;
            screen = ScreenTileOfCell(cell);
            return screen.x >= 0 && screen.y >= 0 && screen.x < ScreenColumns && screen.y < ScreenRows;
        }

        // 칸이 놓인 타일의 화면 좌표입니다(타일 맵 밖일 수 있음, IsValid일 때만 뜻이 있음).
        public Vector2Int ScreenTileOfCell(Vector2Int cell) =>
            new Vector2Int(Anchor.x + TileIndex((cell.x + .5f) / CellsPerTileX, columnsAlongX ? columnPositive : rowPositive),
                Anchor.y + TileIndex((cell.y + .5f) / CellsPerTileZ, columnsAlongX ? rowPositive : columnPositive));

        // 화면 타일이 덮는 필드 칸 사각형입니다(자르지 않음, 판 밖일 수 있음). IsValid일 때만 뜻이 있습니다.
        public Rect CellsOfScreenTile(Vector2Int screen) =>
            new Rect((screen.x - Anchor.x) * CellsPerTileX, (screen.y - Anchor.y) * CellsPerTileZ, CellsPerTileX, CellsPerTileZ);

        // 로컬 좌표를 내림하는 CombatMapTilesData.TryTileAt와 같은 경계 포함 방향입니다. 반전 축은 화면에서 작은 쪽 타일을 고릅니다.
        private static int TileIndex(float position, bool positive)
        {
            const float boundaryTolerance = 0.0001f;
            float boundary = Mathf.Round(position);
            if (Mathf.Abs(position - boundary) <= boundaryTolerance) position = boundary;
            return positive ? Mathf.FloorToInt(position) : Mathf.CeilToInt(position) - 1;
        }

        // CombatMapTileSurface.Build처럼 실제 칸 중심 대응으로 공유 여부를 셉니다. 소수 비율의 면적만으로는 공유 여부를 알 수 없습니다.
        private bool SharesTiles()
        {
            var used = new HashSet<Vector2Int>();
            for (int y = 0; y < board.y; y++)
                for (int x = 0; x < board.x; x++)
                    if (TryScreenTileOfCell(new Vector2Int(x, y), out Vector2Int tile) && !used.Add(tile)) return true;
            return false;
        }

        // 화면 칸 → 배열 칸입니다. 배열 순서는 MapTileRoot 기준이고 배경 회전·배율 부호에 따라 축과 방향이 바뀝니다.
        public void ScreenToArray(int screenX, int screenY, out int column, out int row)
        {
            if (columnsAlongX)
            {
                column = columnPositive ? screenX : columns - 1 - screenX;
                row = rowPositive ? screenY : rows - 1 - screenY;
            }
            else
            {
                row = rowPositive ? screenX : rows - 1 - screenX;
                column = columnPositive ? screenY : columns - 1 - screenY;
            }
        }

        // 배열 칸 → 화면 칸입니다(ScreenToArray의 반대).
        public Vector2Int ArrayToScreen(int column, int row)
        {
            int columnScreen = columnPositive ? column : columns - 1 - column;
            int rowScreen = rowPositive ? row : rows - 1 - row;
            return columnsAlongX ? new Vector2Int(columnScreen, rowScreen) : new Vector2Int(rowScreen, columnScreen);
        }

        // 배열 칸 타일이 덮는 필드 칸 사각형(필드 밖은 잘라냄)입니다. 맞춤 문제가 있거나 필드와 겹치지 않으면 false입니다(설계 TT9 표시).
        public bool TryFieldCellsOfTile(int column, int row, out Rect cells)
        {
            cells = default;
            if (!IsValid) return false;
            return TryClipToField(CellsOfScreenTile(ArrayToScreen(column, row)), out cells);
        }

        // 칸 사각형을 판 안으로 자릅니다. 판과 겹치지 않으면 false입니다(2D 테두리).
        public bool TryClipToField(Rect cells, out Rect clipped)
        {
            float xMin = Mathf.Max(0f, cells.xMin), yMin = Mathf.Max(0f, cells.yMin);
            float xMax = Mathf.Min(board.x, cells.xMax), yMax = Mathf.Min(board.y, cells.yMax);
            clipped = xMax > xMin && yMax > yMin ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : default;
            return xMax > xMin && yMax > yMin;
        }

        // 타일 회전의 화면 방향입니다(설계 §13 TT7). 저장 회전은 타일 루트 위에서 본 시계 방향(Euler Y +90 = +Z가 +X 쪽으로)이고,
        // 화면은 x 오른쪽 = 필드 +X, y 위 = 필드 +Z입니다. 타일 루트 축은 위 ScreenToArray와 같은 대응으로 화면 축이 됩니다.
        // 회전 보정·배율 부호만으로 정해져 맞춤 문제가 있어도 계산합니다(계획 P-TTT7).

        // 화면에서 본 시계 방향이 저장 값에서도 시계 방향이면 +1, 배경이 뒤집혀(축 대응 행렬식 < 0) 반대면 −1입니다.
        public int ScreenTurnSign
        {
            get
            {
                int columnSign = columnPositive ? 1 : -1, rowSign = rowPositive ? 1 : -1;
                return columnsAlongX ? columnSign * rowSign : -columnSign * rowSign;
            }
        }

        // 저장 회전 rotation인 타일의 원래 앞쪽(프리팹 +Z)이 화면에서 가리키는 쪽입니다((0,1) 위, (1,0) 오른쪽, (0,−1) 아래, (−1,0) 왼쪽).
        public Vector2Int ScreenFrontOf(int rotation)
        {
            // 타일 루트에서 앞쪽(+Z)을 시계 방향으로 rotation × 90° 돌린 방향입니다(x = 루트 X, y = 루트 Z).
            Vector2Int local = Mod4(rotation) switch { 0 => new Vector2Int(0, 1), 1 => new Vector2Int(1, 0), 2 => new Vector2Int(0, -1), _ => new Vector2Int(-1, 0) };
            int columnSign = columnPositive ? 1 : -1, rowSign = rowPositive ? 1 : -1;
            return columnsAlongX
                ? new Vector2Int(columnSign * local.x, rowSign * local.y)
                : new Vector2Int(rowSign * local.y, columnSign * local.x);
        }

        // 화면에서 본 돌림 각(0·90·180·270, 시계 방향)입니다. 저장 0 = 화면 0°이고, 프리팹 원래 모습에서 얼마나 돌았는지입니다.
        public int ScreenAngleOf(int rotation) => Mod4(ScreenTurnSign * rotation) * 90;

        // 화면에서 본 90° 단위 각(0~3)을 저장 값으로 바꿉니다(ScreenAngleOf의 반대).
        public int StoredOf(int screenQuarter) => Mod4(ScreenTurnSign * screenQuarter);

        // 저장 값을 화면에서 본 시계 방향으로 quarters × 90°만큼 돌린 저장 값입니다.
        public int Turned(int rotation, int quarters) => rotation == CombatStageSoTileLayout.RandomRotation
            ? rotation : Mod4(rotation + ScreenTurnSign * quarters);

        private static int Mod4(int value) => ((value % 4) + 4) % 4;

        // CombatMapTileAlignment.YawCorrection: 타일 가로축이 필드 X·Z축 중 가까운 쪽과 평행해지도록 위 방향으로 돌릴 각(도, 최대 ±45)입니다.
        public static float YawCorrection(Vector3 columnStep)
        {
            float yaw = Mathf.Atan2(columnStep.z, columnStep.x) * Mathf.Rad2Deg;
            return yaw - Mathf.Round(yaw / 90f) * 90f;
        }

        // CombatMapTileAlignment.AnchorShift: 배경을 옮길 수평 양(필드 로컬)입니다. 옮긴 뒤 기준 타일 좌하단이 게임 (0,0) 칸 좌하단에 옵니다.
        // 검사(축·크기 비율·기준 타일 범위)는 Of가 이미 했으므로 여기서는 하지 않습니다. 필드 모서리는 FieldGridMapper.CornerLocal(필드 가운데가 원점)입니다.
        public static Vector3 AnchorShift(Vector3 gridCenter, Vector3 columnStep, Vector3 rowStep, int columns, int rows, Vector2Int anchor,
            Vector2Int board, float cellWidth, float cellLength)
        {
            bool alongX = Mathf.Abs(columnStep.x) >= Mathf.Abs(columnStep.z);
            Vector3 stepX = alongX ? columnStep : rowStep, stepZ = alongX ? rowStep : columnStep;
            int countX = alongX ? columns : rows, countZ = alongX ? rows : columns;
            float tileX = new Vector3(stepX.x, 0f, stepX.z).magnitude, tileZ = new Vector3(stepZ.x, 0f, stepZ.z).magnitude;
            float mapMinX = -board.x * cellWidth * .5f - anchor.x * tileX;
            float mapMinZ = -board.y * cellLength * .5f - anchor.y * tileZ;
            return new Vector3(mapMinX - (gridCenter.x - countX * tileX * .5f), 0f, mapMinZ - (gridCenter.z - countZ * tileZ * .5f));
        }

        // 배경 회전(타일 루트는 회전 없는 배경 자식)과 배율 부호로 타일 가로·세로가 필드 어느 축·방향을 따르는지 정합니다.
        private static void Axes(Vector3 rotationOffset, Vector3 backdropScale, out bool alongX, out bool columnPositive, out bool rowPositive)
        {
            Quaternion rotation = Quaternion.Euler(rotationOffset);
            Vector3 column = rotation * new Vector3(Mathf.Sign(backdropScale.x), 0f, 0f);
            Vector3 row = rotation * new Vector3(0f, 0f, Mathf.Sign(backdropScale.z));
            alongX = Mathf.Abs(column.x) >= Mathf.Abs(column.z);
            columnPositive = alongX ? column.x > 0f : column.z > 0f;
            rowPositive = alongX ? row.z > 0f : row.x > 0f;
        }

        // CombatMapTileAlignment.CellRatio: 양의 유한한 소수 비율도 허용합니다.
        private static float CellRatio(float tileLength, float cellSize, string axis, List<string> problems)
        {
            float ratio = tileLength / cellSize;
            if (float.IsFinite(tileLength) && tileLength > 0f && float.IsFinite(cellSize) && cellSize > 0f
                && float.IsFinite(ratio) && ratio > 0f) return ratio;
            problems.Add($"필드 {axis}축 타일 크기({tileLength:0.###})와 칸 간격({cellSize:0.###}) 및 그 비율은 0보다 큰 유한 값이어야 합니다. 타일 크기·배율 보정을 맞추세요.");
            return 0f;
        }
    }
}
