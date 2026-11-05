using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 필드 칸마다 그 칸 아래 바닥 타일(윗면 높이·타일 오브젝트)을 적은 표입니다(타일 높이 설계 TH2, 타일 교체 설계 TR2).
    /// 배경 표시가 타일 맞춤을 끝낸 직후(씬 로드) 한 번 만들고, 타일 맵 밖 칸은 높이 0(바닥)·타일 없음입니다.
    /// 전투 표시가 설치 때 필드 높이 표의 바탕 층으로 넣고(<see cref="ApplyBaseTo"/>), 지형 표시가 바닥 교체 칸의 타일을 찾습니다(<see cref="TryGetTile"/>).
    /// </summary>
    /// <remarks>
    /// 칸 가운데를 타일 루트 기준으로 바꿔 타일 칸을 찾으므로 배경 회전·타일 한 장 = 칸 k개는 변환이 맡습니다.
    /// 높이와 타일 오브젝트는 같은 대응(칸 가운데가 놓인 타일) 한 번에서 나옵니다.
    /// 필드 격자가 칸 루트의 어디에 놓이는지는 <see cref="FieldGridMapper.CornerLocal(int, int, float, float)"/>, 칸 가운데는
    /// <see cref="FieldGridMapper.CenterLocal"/>, 점이 놓인 타일 칸은
    /// <see cref="CombatMapTilesData.TryTileAt"/>, 종류의 높이는 <see cref="CombatMapTileVisual.SurfaceHeight"/>를 따릅니다.
    /// 표는 만든 뒤 바뀌지 않습니다. 타일 오브젝트의 켜짐은 표가 아니라 지형 표시가 맡습니다.
    /// </remarks>
    internal sealed class CombatMapTileSurface
    {
        private readonly float[,] _heights;
        // 칸마다 그 칸 가운데가 놓인 타일입니다. 타일 맵 밖 칸은 null입니다.
        private readonly Transform[,] _tiles;

        private CombatMapTileSurface(float[,] heights, Transform[,] tiles)
        {
            _heights = heights;
            _tiles = tiles;
        }

        /// <summary>필드 가로 칸 수입니다.</summary>
        internal int Columns => _heights.GetLength(0);

        /// <summary>필드 세로 칸 수입니다.</summary>
        internal int Rows => _heights.GetLength(1);

        /// <summary>
        /// 이 표를 필드 높이 표의 바탕 층으로 적습니다(타일 높이 설계 TH6, 계획 P1). 모든 칸을 적으므로 바탕을 먼저 비우지 않아도 됩니다.
        /// 칸 높이는 다음 다시 채우기에서 바뀝니다. 크기가 다르면 같은 전투의 표가 아니라 조립 오류라 예외입니다(계획 P11).
        /// </summary>
        internal void ApplyBaseTo(FieldSurfaceHeights surface)
        {
            if (surface == null)
                throw new ArgumentNullException(nameof(surface));
            if (surface.ColumnCount != Columns || surface.RowCount != Rows)
            {
                throw new InvalidOperationException(
                    $"[CombatMapTileSurface] 타일 높이 표({Columns} × {Rows})와 필드 높이 표({surface.ColumnCount} × {surface.RowCount})의 크기가 다릅니다.");
            }

            for (int row = 0; row < Rows; row++)
            {
                for (int column = 0; column < Columns; column++)
                    surface.SetBase(new CellCoord(column, row), _heights[column, row]);
            }
        }

        /// <summary>
        /// 칸 아래 바닥 타일입니다(타일 교체 설계 TR2). 타일 맵 밖 칸이거나 타일이 이미 파괴됐으면(배경이 먼저 없어짐) false입니다.
        /// </summary>
        internal bool TryGetTile(CellCoord cell, out Transform tile)
        {
            if (cell.X < 0 || cell.X >= Columns || cell.Y < 0 || cell.Y >= Rows)
                throw new ArgumentOutOfRangeException(nameof(cell), $"격자 범위를 벗어난 칸입니다: {cell}");
            tile = _tiles[cell.X, cell.Y];
            if (tile != null)
                return true;
            tile = null;
            return false;
        }

        /// <summary>
        /// 맞춤이 끝난 타일 루트로 필드 칸마다의 타일 윗면 높이와 타일 오브젝트를 만듭니다.
        /// </summary>
        /// <param name="cellsRoot">필드 칸 루트입니다(필드 격자 가운데가 원점).</param>
        /// <param name="tileRoot">맞춤이 끝난 타일 루트입니다.</param>
        /// <param name="placed">타일 루트 아래 만든 타일입니다(<see cref="StageMapTiles.Build"/>, 타일 맵 칸 [가로, 세로]).</param>
        /// <param name="board">이 전투의 보드 크기(가로·세로 칸 수)입니다.</param>
        /// <param name="spacing">필드 칸 간격입니다.</param>
        /// <param name="sharesTiles">타일 한 장에 필드 칸이 둘 이상 놓였는지입니다. 그러면 한 칸의 바닥 교체가 그 타일 전체를 비웁니다(타일 교체 설계 Q-TR3).</param>
        internal static CombatMapTileSurface Build(Transform cellsRoot, Transform tileRoot, CombatMapTilesData tiles, Transform[,] placed,
            Vector2Int board, FieldCellSpacing spacing, out bool sharesTiles)
        {
            if (cellsRoot == null)
                throw new ArgumentNullException(nameof(cellsRoot));
            if (tileRoot == null)
                throw new ArgumentNullException(nameof(tileRoot));
            if (tiles == null)
                throw new ArgumentNullException(nameof(tiles));
            if (placed == null)
                throw new ArgumentNullException(nameof(placed));
            if (placed.GetLength(0) != tiles.Columns || placed.GetLength(1) != tiles.Rows)
                throw new ArgumentException($"만든 타일 수({placed.GetLength(0)} × {placed.GetLength(1)})가 타일 맵({tiles.Columns} × {tiles.Rows})과 다릅니다.", nameof(placed));
            if (board.x < 1 || board.y < 1)
                throw new ArgumentOutOfRangeException(nameof(board));

            Vector3 corner = FieldGridMapper.CornerLocal(board.x, board.y, spacing.Width, spacing.Length);
            var heights = new float[board.x, board.y];
            var cellTiles = new Transform[board.x, board.y];
            var used = new bool[tiles.Columns, tiles.Rows];
            sharesTiles = false;
            for (int row = 0; row < board.y; row++)
            {
                for (int column = 0; column < board.x; column++)
                {
                    Vector3 center = FieldGridMapper.CenterLocal(corner, column, row, spacing.Width, spacing.Length);
                    Vector3 tileLocal = tileRoot.InverseTransformPoint(cellsRoot.TransformPoint(center));
                    if (!tiles.TryTileAt(tileLocal, out int tileColumn, out int tileRow))
                        continue;

                    heights[column, row] = tiles.VisualOf(tiles.KindAt(tileColumn, tileRow)).SurfaceHeight;
                    cellTiles[column, row] = placed[tileColumn, tileRow];
                    sharesTiles |= used[tileColumn, tileRow];
                    used[tileColumn, tileRow] = true;
                }
            }
            return new CombatMapTileSurface(heights, cellTiles);
        }
    }
}
