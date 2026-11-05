using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item
{
    /// <summary>
    /// 스테이지 맵 프랍의 바닥 타일 배치 입력입니다. 칸마다 위치를 적지 않고 종류만 가로 한 줄씩 차례로 적습니다.
    /// 가로·세로 칸 수는 전투 구역(필드) 크기가 아니라 배경 구멍의 최대 크기이고, 타일 격자는 필드 칸과 따로 놓입니다.
    /// 모두 비우면 타일을 두지 않습니다.
    /// </summary>
    [Serializable]
    internal sealed class CombatMapTilesInput
    {
        [Tooltip("타일 종류별 프리팹입니다. 여러 스테이지가 하나를 함께 쓸 수 있습니다.")]
        [SerializeField] private CombatMapTileSetSO tileSet;
        [Tooltip("배경 구멍을 채우는 가로 칸 수입니다(필드 크기가 아니라 구멍의 최대 크기).")]
        [SerializeField, Min(0)] private int columns;
        [Tooltip("배경 구멍을 채우는 세로 칸 수입니다(필드 크기가 아니라 구멍의 최대 크기).")]
        [SerializeField, Min(0)] private int rows;
        [Tooltip("타일 한 칸의 크기(중심 사이 간격)입니다. x = 가로(MapTileRoot X), y = 세로(MapTileRoot Z). 0보다 큰 소수도 지원합니다. MapTileRoot 기준 값이고 타일 프리팹 크기와 같게 둡니다. 배경 자식이라 배경 배율 보정이 곱해집니다. 필드 칸 높이·바닥 교체 대상은 칸 중심이 놓인 타일 한 장으로 정합니다.")]
        [SerializeField] private Vector2 tileSize = new(2f, 2f);
        [Tooltip("MapTileRoot의 배경 기준 위치(x, y, z)입니다. MapTileRoot는 배경 아래 자식이라 이 값은 배경 로컬 좌표이고, 배경의 회전·배율을 같이 받습니다. 타일 격자 가운데(구멍 가운데)가 이 자리에 오고, y는 타일 바닥(피벗) 높이입니다.")]
        [SerializeField] private Vector3 offset;
        [Tooltip("게임 그리드 (0,0) 칸의 좌하단 모서리에 맞출 타일 칸 좌표입니다. 화면(필드) 기준으로 타일 맵 좌하단(필드 −X·−Z) 타일이 (0,0)이고, x = 게임 가로(필드 X)·y = 게임 세로(필드 Z) 방향 타일 수입니다. 배경 회전과 무관합니다. 기준 타일의 좌하단 모서리에 게임 (0,0) 칸의 좌하단 모서리를 맞춥니다. 타일이 작으면 칸 중심은 다른 타일에 놓일 수 있습니다. 타일 맵 밖이면 전투 진입 때 예외입니다.")]
        [SerializeField] private Vector2Int anchorTile;
        [Tooltip("칸마다 타일 종류입니다. 첫 줄은 MapTileRoot(배경) −Z 쪽 줄이고 줄 안에서는 −X 쪽부터 적습니다(배경 회전이 걸리기 전 방향이라 화면 방향과 다를 수 있습니다). 가로 한 줄(columns 개)을 다 적은 뒤 다음 줄로 넘어가고, 길이는 columns × rows입니다.")]
        [SerializeField] private CombatMapTileKind[] tiles = Array.Empty<CombatMapTileKind>();
        [Tooltip("칸마다 타일 회전입니다. 순서는 Tiles와 같고, 비우면 모두 R0입니다. 고정 각은 MapTileRoot 기준 시계 방향 90° 단위이며, Random은 생성 때 고릅니다. 정사각은 네 각, 가로 ≠ 세로는 R0·R180 중에서 고릅니다. 타일 피벗은 가운데여야 하고, 가로 ≠ 세로의 고정 R90·R270은 쓸 수 없습니다. 쓰면 길이는 columns × rows입니다.")]
        [SerializeField] private CombatMapTileRotation[] rotations = Array.Empty<CombatMapTileRotation>();

        /// <summary>입력을 검사하고 매번 새 불변 값을 만듭니다(DB 규칙 P2). 모두 비어 있으면 null(타일 없음)입니다.</summary>
        internal CombatMapTilesData CreateData(int stageKey)
        {
            CombatMapTileKind[] kinds = tiles ?? Array.Empty<CombatMapTileKind>();
            CombatMapTileRotation[] turns = rotations ?? Array.Empty<CombatMapTileRotation>();
            if (tileSet == null && columns == 0 && rows == 0 && kinds.Length == 0 && turns.Length == 0)
                return null;

            if (tileSet == null)
                throw new InvalidOperationException($"[CombatMapTilesInput] 타일 세트(tileSet)가 필요합니다. StageKey: {stageKey}");
            if (columns < 1 || rows < 1)
                throw new InvalidOperationException($"[CombatMapTilesInput] 가로·세로 칸 수는 1 이상이어야 합니다: {columns} × {rows}, StageKey: {stageKey}");
            if (kinds.Length != columns * rows)
            {
                throw new InvalidOperationException(
                    $"[CombatMapTilesInput] 타일 수가 가로 × 세로와 다릅니다: {kinds.Length} ≠ {columns} × {rows}, StageKey: {stageKey}");
            }
            if (!IsPositive(tileSize.x) || !IsPositive(tileSize.y))
                throw new InvalidOperationException($"[CombatMapTilesInput] 타일 크기는 0보다 커야 합니다: {tileSize}, StageKey: {stageKey}");
            if (!float.IsFinite(offset.x) || !float.IsFinite(offset.y) || !float.IsFinite(offset.z))
                throw new InvalidOperationException($"[CombatMapTilesInput] MapTileRoot 위치(offset)가 잘못되었습니다: {offset}, StageKey: {stageKey}");
            if (anchorTile.x < 0 || anchorTile.y < 0)
                throw new InvalidOperationException($"[CombatMapTilesInput] 게임 (0,0)이 놓일 타일 칸은 0 이상이어야 합니다: {anchorTile}, StageKey: {stageKey}");

            Dictionary<CombatMapTileKind, CombatMapTileVisual> visuals = tileSet.CreateVisuals();
            for (int i = 0; i < kinds.Length; i++)
            {
                if (!visuals.ContainsKey(kinds[i]))
                {
                    throw new InvalidOperationException(
                        $"[CombatMapTilesInput] 가로 {i % columns}, 세로 {i / columns} 칸의 타일 종류 {(int)kinds[i]}가 타일 세트에 없습니다. StageKey: {stageKey}");
                }
            }

            return new CombatMapTilesData(columns, rows, tileSize, offset, anchorTile, (CombatMapTileKind[])kinds.Clone(), visuals,
                CreateRotations(turns, stageKey));
        }

        // 회전 목록을 검사하고 칸 수만큼의 새 배열로 만듭니다. 비어 있으면 모두 R0입니다(설계 §13 TT1·TT3).
        private CombatMapTileRotation[] CreateRotations(CombatMapTileRotation[] turns, int stageKey)
        {
            var result = new CombatMapTileRotation[columns * rows];
            if (turns.Length == 0)
                return result;
            if (turns.Length != result.Length)
            {
                throw new InvalidOperationException(
                    $"[CombatMapTilesInput] 회전 수가 가로 × 세로와 다릅니다: {turns.Length} ≠ {columns} × {rows}(비우면 모두 R0), StageKey: {stageKey}");
            }

            // 가로·세로 크기가 다른 타일은 90°·270° 돌리면 칸에 맞지 않습니다(Q-TT3 A).
            bool square = Mathf.Approximately(tileSize.x, tileSize.y);
            for (int i = 0; i < turns.Length; i++)
            {
                if (!System.Enum.IsDefined(typeof(CombatMapTileRotation), turns[i]))
                {
                    throw new InvalidOperationException(
                        $"[CombatMapTilesInput] 가로 {i % columns}, 세로 {i / columns} 칸의 회전 {(int)turns[i]}이 잘못되었습니다. StageKey: {stageKey}");
                }
                if (!square && (turns[i] == CombatMapTileRotation.R90 || turns[i] == CombatMapTileRotation.R270))
                {
                    throw new InvalidOperationException(
                        $"[CombatMapTilesInput] 타일 크기가 가로 ≠ 세로({tileSize})라 가로 {i % columns}, 세로 {i / columns} 칸을 {turns[i]}로 돌릴 수 없습니다(R0·R180만). StageKey: {stageKey}");
                }
                result[i] = turns[i];
            }
            return result;
        }

        private static bool IsPositive(float value) => float.IsFinite(value) && value > 0f;
    }

    /// <summary>
    /// 검사를 마친 바닥 타일 배치입니다. 입력 순서로 칸을 정하는 규칙은 <see cref="KindAt"/>, 칸을 놓을 자리는 <see cref="CenterOf"/>,
    /// 격자 가운데는 <see cref="GridCenter"/>, 점이 놓인 칸은 <see cref="TryTileAt"/>이고, 모두 같은 모서리 계산 한 곳에서 나옵니다.
    /// 타일 루트(MapTileRoot)는 배경 아래 자식이고 배경 로컬 <see cref="Offset"/>에 놓이며, 격자 가운데는 타일 루트 원점입니다.
    /// 기준점(<see cref="AnchorTile"/>)은 게임 그리드 (0,0) 칸이 놓일 타일 칸이고, 맵을 그 자리에 맞추는 계산은 배경 표시가 합니다
    /// (타일 맞춤 설계 TA1·TA2, 2026-10-28 개정 5).
    /// 칸마다 회전 옵션(<see cref="RotationAt"/>)을 보관합니다. Random의 실제 각은 생성 시 고르고 프리팹 자기 회전 앞에 곱합니다.
    /// </summary>
    internal sealed class CombatMapTilesData
    {
        private readonly CombatMapTileKind[] _kinds;
        private readonly CombatMapTileRotation[] _rotations;
        private readonly Dictionary<CombatMapTileKind, CombatMapTileVisual> _visuals;

        internal CombatMapTilesData(int columns, int rows, Vector2 tileSize, Vector3 offset, Vector2Int anchorTile,
            CombatMapTileKind[] kinds, Dictionary<CombatMapTileKind, CombatMapTileVisual> visuals, CombatMapTileRotation[] rotations)
        {
            if (columns < 1)
                throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows < 1)
                throw new ArgumentOutOfRangeException(nameof(rows));
            if (!(tileSize.x > 0f) || !(tileSize.y > 0f))
                throw new ArgumentOutOfRangeException(nameof(tileSize));
            _kinds = kinds ?? throw new ArgumentNullException(nameof(kinds));
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            if (_kinds.Length != columns * rows)
                throw new ArgumentException("타일 수가 가로 × 세로와 다릅니다.", nameof(kinds));
            _rotations = rotations ?? throw new ArgumentNullException(nameof(rotations));
            if (_rotations.Length != columns * rows)
                throw new ArgumentException("회전 수가 가로 × 세로와 다릅니다.", nameof(rotations));
            if (anchorTile.x < 0 || anchorTile.y < 0)
                throw new ArgumentOutOfRangeException(nameof(anchorTile));

            Columns = columns;
            Rows = rows;
            TileSize = tileSize;
            Offset = offset;
            AnchorTile = anchorTile;
        }

        /// <summary>배경 구멍의 가로 칸 수입니다.</summary>
        public int Columns { get; }

        /// <summary>배경 구멍의 세로 칸 수입니다.</summary>
        public int Rows { get; }

        /// <summary>타일 한 칸의 크기(중심 사이 간격)입니다. x = 타일 루트 X, y = 타일 루트 Z이고, 배경 배율이 걸리기 전 값입니다.</summary>
        public Vector2 TileSize { get; }

        /// <summary>타일 루트(MapTileRoot)의 배경 로컬 위치입니다. 격자 가운데(구멍 가운데)가 이 자리에 오고, y는 타일 바닥 높이입니다.</summary>
        public Vector3 Offset { get; }

        /// <summary>
        /// 게임 그리드 (0,0) 칸이 놓일 타일 칸 좌표입니다. 화면(필드) 기준 타일 맵 좌하단 타일이 (0,0)이고 x = 필드 X·y = 필드 Z 방향 타일 수라,
        /// 타일 루트 순서(<see cref="CenterOf"/>·<see cref="KindAt"/>)와 달리 배경 회전과 무관합니다. 0 이상이고, 타일 맵 안인지는 맞춤 계산이 봅니다(회전에 따라 축이 바뀌어서).
        /// </summary>
        public Vector2Int AnchorTile { get; }

        /// <summary>
        /// 가로 <paramref name="column"/>, 세로 <paramref name="row"/> 칸 중심(타일 바닥 높이)의 타일 루트 기준 위치입니다.
        /// 칸 0은 −X·−Z 쪽 끝이고, 격자 가운데는 <see cref="GridCenter"/>입니다(예: 16칸·크기 2면 X는 −15 ~ 15).
        /// </summary>
        public Vector3 CenterOf(int column, int row)
        {
            if (column < 0 || column >= Columns)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (row < 0 || row >= Rows)
                throw new ArgumentOutOfRangeException(nameof(row));

            Vector3 corner = Corner;
            return new Vector3(corner.x + ((column + 0.5f) * TileSize.x), corner.y, corner.z + ((row + 0.5f) * TileSize.y));
        }

        /// <summary>격자 가운데(타일 바닥 높이)의 타일 루트 기준 위치입니다. 맞춤 계산이 회전 중심과 타일 맵 자리를 잴 때 씁니다(계획 P14).</summary>
        public Vector3 GridCenter
        {
            get
            {
                Vector3 corner = Corner;
                return new Vector3(corner.x + (Columns * TileSize.x * 0.5f), corner.y, corner.z + (Rows * TileSize.y * 0.5f));
            }
        }

        /// <summary>
        /// 타일 루트 기준 점이 놓인 타일 칸입니다(<see cref="CenterOf"/>의 짝, 높이는 보지 않음). 타일 맵 밖이면 false이고 칸 번호는 0입니다.
        /// 배경 표시가 필드 칸 가운데를 타일 루트 기준으로 바꿔 그 칸의 타일을 찾을 때 씁니다(타일 높이 설계 TH2).
        /// </summary>
        public bool TryTileAt(Vector3 rootLocal, out int column, out int row)
        {
            Vector3 corner = Corner;
            int x = TileIndex((rootLocal.x - corner.x) / TileSize.x);
            int z = TileIndex((rootLocal.z - corner.z) / TileSize.y);
            if (x < 0 || x >= Columns || z < 0 || z >= Rows)
            {
                column = 0;
                row = 0;
                return false;
            }

            column = x;
            row = z;
            return true;
        }

        // 소수 크기·회전 변환의 오차로 경계 위 점이 이웃 타일로 흔들리지 않게, 타일 한 장의 0.0001 이내인 경계에 맞춘 뒤 내립니다.
        // 에디터 사본 CombatStageSoTileFit.TileIndex도 같은 허용 오차와 경계 포함 방향을 씁니다.
        private static int TileIndex(float position)
        {
            const float boundaryTolerance = 0.0001f;
            float boundary = Mathf.Round(position);
            if (Mathf.Abs(position - boundary) <= boundaryTolerance) position = boundary;
            return Mathf.FloorToInt(position);
        }

        // 격자 −X·−Z 모서리(타일 (0,0) 칸의 좌하단)의 타일 루트 기준 위치입니다. 칸 자리·격자 가운데·점이 놓인 칸이 모두 여기서 나오며,
        // 격자 가운데를 타일 루트 원점에 두는 규칙은 이 한 줄입니다(자리·높이는 타일 루트의 위치 Offset이 정함).
        private Vector3 Corner => new(-Columns * TileSize.x * 0.5f, 0f, -Rows * TileSize.y * 0.5f);

        /// <summary>가로 <paramref name="column"/>, 세로 <paramref name="row"/> 칸의 타일 종류입니다. 입력의 <c>column + row × Columns</c>번째 값입니다.</summary>
        public CombatMapTileKind KindAt(int column, int row)
        {
            if (column < 0 || column >= Columns)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (row < 0 || row >= Rows)
                throw new ArgumentOutOfRangeException(nameof(row));

            return _kinds[column + (row * Columns)];
        }

        /// <summary>칸의 타일 회전 옵션입니다. Random은 생성하는 쪽이 확정하며, 입력이 비었으면 R0입니다.</summary>
        public CombatMapTileRotation RotationAt(int column, int row)
        {
            if (column < 0 || column >= Columns)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (row < 0 || row >= Rows)
                throw new ArgumentOutOfRangeException(nameof(row));

            return _rotations[column + (row * Columns)];
        }

        /// <summary>타일 종류의 외형(프리팹·윗면 높이)입니다. 배치에 쓰인 종류는 모두 있습니다. 칸의 종류는 <see cref="KindAt"/>입니다.</summary>
        public CombatMapTileVisual VisualOf(CombatMapTileKind kind)
        {
            if (_visuals.TryGetValue(kind, out CombatMapTileVisual visual))
                return visual;

            throw new KeyNotFoundException($"[CombatMapTilesData] 타일 세트에 없는 종류입니다: {kind}");
        }
    }
}
