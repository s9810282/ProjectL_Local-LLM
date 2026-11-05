using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 바닥 교체 지형이 보이는 칸의 바닥 타일을 끄고, 정리 때 다시 켭니다(타일 교체 설계 TR4). 주인은 지형 표시 <see cref="FieldTerrainPresenter"/> 하나입니다.
    /// </summary>
    /// <remarks>
    /// 타일은 배경이 만들고 그 뒤 켜짐을 건드리지 않습니다. 이 부품은 자기가 끈 타일만 기억하고 그것만 다시 켭니다.
    /// 끌 칸은 그리기마다 <see cref="BeginMarks"/> → <see cref="Mark"/> → <see cref="Apply"/> 순서로 다시 정하고, 켜짐이 바뀌는 타일만 건드립니다(타일 교체 계획 P13).
    /// 타일 한 장을 칸 여럿이 나누면 그중 한 칸만 표시돼도 그 타일을 끕니다(타일 교체 설계 Q-TR3).
    /// 배경이 먼저 없어져 파괴된 타일은 건너뜁니다.
    /// </remarks>
    internal sealed class FieldTileReplacement
    {
        // 이 부품이 끈 타일입니다. 이것만 다시 켭니다.
        private readonly HashSet<Transform> _hidden = new();
        // Apply 동안 이번에 끌 타일을 모읍니다. 다시 씁니다.
        private readonly HashSet<Transform> _wanted = new();
        private readonly List<Transform> _scratch = new();
        private CombatMapTileSurface _tiles;
        // 이번 그리기에서 끌 칸입니다(칸 번호 = 세로 × 가로 수 + 가로). 타일 표가 없으면 null입니다.
        private bool[] _marked;

        /// <summary>
        /// 칸 ↔ 타일 표를 정합니다. 타일이 없는 배경이면 null이고, 그러면 모든 칸이 "타일 없음"입니다.
        /// 다른 표로 바뀌면 옛 표에서 끈 타일을 먼저 다시 켭니다. 같은 표면 아무것도 하지 않습니다.
        /// </summary>
        internal void Bind(CombatMapTileSurface tiles)
        {
            if (ReferenceEquals(tiles, _tiles)) return;
            Restore();
            _tiles = tiles;
            _marked = tiles == null ? null : new bool[tiles.Columns * tiles.Rows];
        }

        /// <summary>칸의 바닥 타일입니다. 타일 표가 없거나, 타일 맵 밖 칸이거나, 타일이 파괴됐으면 false입니다.</summary>
        internal bool TryGetTile(CellCoord cell, out Transform tile)
        {
            if (_tiles != null) return _tiles.TryGetTile(cell, out tile);
            tile = null;
            return false;
        }

        /// <summary>이번 그리기의 끌 칸을 비웁니다.</summary>
        internal void BeginMarks()
        {
            if (_marked != null) Array.Clear(_marked, 0, _marked.Length);
        }

        /// <summary>칸의 타일을 이번 그리기에서 끌 칸으로 표시합니다. 타일 표가 없으면 아무것도 하지 않습니다.</summary>
        internal void Mark(CellCoord cell)
        {
            if (_marked == null) return;
            if (cell.X < 0 || cell.X >= _tiles.Columns || cell.Y < 0 || cell.Y >= _tiles.Rows)
                throw new ArgumentOutOfRangeException(nameof(cell), $"격자 범위를 벗어난 칸입니다: {cell}");
            _marked[(cell.Y * _tiles.Columns) + cell.X] = true;
        }

        /// <summary>
        /// 표시한 칸의 타일을 끄고, 지난번에 껐지만 이번에 표시되지 않은 타일을 다시 켭니다. 켜짐이 바뀌는 타일만 건드립니다.
        /// 원래 꺼져 있던 타일은 기억하지 않아 나중에 켜지도 않습니다.
        /// </summary>
        internal void Apply()
        {
            _wanted.Clear();
            if (_marked != null)
            {
                for (int index = 0; index < _marked.Length; index++)
                {
                    if (_marked[index] && _tiles.TryGetTile(new CellCoord(index % _tiles.Columns, index / _tiles.Columns), out Transform tile))
                        _wanted.Add(tile);
                }
            }

            _scratch.Clear();
            foreach (Transform tile in _hidden)
                if (!_wanted.Contains(tile)) _scratch.Add(tile);
            foreach (Transform tile in _scratch)
            {
                _hidden.Remove(tile);
                if (tile != null) tile.gameObject.SetActive(true);
            }

            foreach (Transform tile in _wanted)
            {
                if (_hidden.Contains(tile) || !tile.gameObject.activeSelf) continue;
                tile.gameObject.SetActive(false);
                _hidden.Add(tile);
            }
            _wanted.Clear();
            _scratch.Clear();
        }

        /// <summary>이 부품이 끈 타일을 모두 다시 켭니다. 파괴된 타일은 건너뜁니다. 여러 번 불려도 안전합니다.</summary>
        internal void Restore()
        {
            foreach (Transform tile in _hidden)
                if (tile != null) tile.gameObject.SetActive(true);
            _hidden.Clear();
        }
    }
}
