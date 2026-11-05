using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 칸마다의 높이(필드 로컬 단위, 바닥면 = 0) 표입니다(고지대 View 설계 HG-2, 타일 높이 설계 TH6). 높이는 두 가지입니다.
    /// <list type="bullet">
    /// <item>칸 높이(<see cref="HeightAt"/>) = 바탕(바닥 타일 윗면, 음수 가능) + 지형 높이. 그 칸에 서거나 놓이는 것(유닛·고스트·지형·오브젝트)이 씁니다.</item>
    /// <item>격자 높이(<see cref="OverlayHeightAt"/>) = 지형 높이만. GridOutline이 그리는 것(칸 테두리·채우기·경로선·호버·데칼 상자)과 포인터가 씁니다.
    /// 바닥 타일 높이는 보지 않습니다(타일 높이 설계 TH4 개정 2, 2026-10-28 사용자 "그리드 및 호버링 그려지는 위치는 SurfaceHeight 영향 받으면 안 된다").</item>
    /// </list>
    /// 바탕은 둘입니다. 타일 바탕은 표시 설치 때 한 번 넣고(<see cref="CombatMapTileSurface.ApplyBaseTo"/>), 이번 바탕은 다시 채울 때마다 타일 바탕에서 시작해
    /// 바닥 교체 지형이 바꿉니다(<see cref="Replace"/>, 타일 교체 설계 TR5). 지형 높이는 그 위로 올립니다(<see cref="Raise"/>). 둘 다 지형 표시
    /// (<see cref="FieldTerrainPresenter.SyncSurface"/>)가 Refresh마다 채웁니다. 바닥 교체는 "타일이 바뀐 것"이라 격자 높이를 바꾸지 않습니다.
    /// 배열은 생성 때 한 번 만들고 다시 채울 때 재사용합니다.
    /// </summary>
    internal sealed class FieldSurfaceHeights
    {
        private readonly float[] _heights;
        // 바닥 타일 윗면 높이(타일 바탕)입니다. 기본은 모두 0이고, 바뀐 값은 다음 다시 채우기(또는 Reset)부터 칸 높이에 나옵니다(타일 높이 계획 P1).
        private readonly float[] _tileBase;
        // 이번 다시 채우기의 바탕입니다. 타일 바탕에서 시작하고 바닥 교체 칸만 그 지형의 높이로 바뀝니다(타일 교체 설계 TR5).
        private readonly float[] _base;
        // 이번 다시 채우기에서 바닥 교체가 들어온 칸입니다. 첫 교체는 타일 바탕을 덮고, 그다음부터는 낮은 값이 남습니다(타일 교체 계획 P10).
        private readonly bool[] _replaced;
        // 지형이 올린 높이입니다(0 이상, 원본이 여럿이면 가장 높은 값). 칸 높이 = 바탕 + 이 값이고, 격자 높이는 이 값 그대로입니다.
        // 빼기로 다시 구하지 않고 따로 들어 높이별 묶음이 실수 오차로 갈리지 않게 합니다.
        private readonly float[] _terrain;
        private readonly float[] _previousTerrain;
        private readonly List<float> _overlayLevels = new();
        private bool _levelsDirty;
        // BeginRefill ~ EndRefill 사이입니다. 이 동안의 값 변경은 끝에서 한 번만 비교합니다.
        private bool _refilling;

        internal FieldSurfaceHeights(int columnCount, int rowCount)
        {
            if (columnCount <= 0) throw new ArgumentOutOfRangeException(nameof(columnCount));
            if (rowCount <= 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
            ColumnCount = columnCount;
            RowCount = rowCount;
            _heights = new float[checked(columnCount * rowCount)];
            _tileBase = new float[_heights.Length];
            _base = new float[_heights.Length];
            _replaced = new bool[_heights.Length];
            _terrain = new float[_heights.Length];
            _previousTerrain = new float[_heights.Length];
        }

        /// <summary>가로 칸 수입니다.</summary>
        internal int ColumnCount { get; }

        /// <summary>세로 칸 수입니다.</summary>
        internal int RowCount { get; }

        /// <summary>
        /// 격자 높이(지형이 올린 높이)가 바뀔 때마다 늘어납니다. 높이별로 그린 격자 표시(GridOutline)가 다시 그릴지 정합니다.
        /// 칸 높이만 바뀐 것(바닥 교체·타일 바탕)은 격자에 영향이 없어 올리지 않습니다(묶음 끝 검토 L8). 칸 높이를 기억해 두는 소비자는 없습니다.
        /// </summary>
        internal int Version { get; private set; }

        /// <summary>격자 높이가 바닥(0)이 아닌 칸(지형이 올린 칸)이 하나라도 있는지입니다. 없으면 격자 표시는 높이별로 나누지 않습니다.</summary>
        internal bool HasOverlayLevels => OverlayLevels.Count > 0;

        /// <summary>격자 높이 중 가장 큰 값입니다. 격자 데칼 상자의 위 끝이고, 지형이 올린 칸이 없으면 0입니다.</summary>
        internal float MaxHeight
        {
            get
            {
                IReadOnlyList<float> levels = OverlayLevels;
                return levels.Count > 0 ? levels[levels.Count - 1] : 0f;
            }
        }

        /// <summary>
        /// 바닥(0)이 아닌 격자 높이 목록입니다(오름차순, 중복 없음) — 지형이 올린 높이들입니다. 격자 표시가 높이별 손잡이를 나누고,
        /// 포인터가 바닥 평면보다 먼저 볼 평면들입니다(<see cref="FieldView.TryRayToSurface"/>).
        /// </summary>
        internal IReadOnlyList<float> OverlayLevels
        {
            get
            {
                if (_levelsDirty)
                    RebuildOverlayLevels();
                return _overlayLevels;
            }
        }

        /// <summary>타일 바탕을 모두 바닥(0)으로 되돌립니다. 칸 높이는 다음 다시 채우기에서 바뀝니다.</summary>
        internal void ClearBase() => Array.Clear(_tileBase, 0, _tileBase.Length);

        /// <summary>칸의 타일 바탕 높이(바닥 타일 윗면, 음수 가능)를 적습니다. 칸 높이는 다음 다시 채우기에서 바뀝니다.</summary>
        internal void SetBase(CellCoord cell, float height)
        {
            if (!float.IsFinite(height))
                throw new ArgumentOutOfRangeException(nameof(height), height, "바탕 높이는 유한해야 합니다.");
            _tileBase[IndexOf(cell)] = height;
        }

        /// <summary>
        /// 칸의 이번 바탕 높이입니다. 바닥 타일 윗면이고, 바닥 교체 지형이 있으면 그 지형의 높이입니다(타일 교체 설계 TR5).
        /// 지형 외형처럼 바탕 위에 놓는 것이 씁니다(타일 높이 설계 TH5).
        /// </summary>
        internal float BaseHeightAt(CellCoord cell) => _base[IndexOf(cell)];

        /// <summary>
        /// 칸의 타일 바탕 높이(바닥 타일 윗면, 바닥 교체와 상관없음)입니다. 전투 동안 바뀌지 않습니다.
        /// 바닥 교체로 바탕이 바뀌어도 자리를 옮기지 않아야 하는 것(토네이도 외형, 묶음 끝 검토 L4)이 씁니다.
        /// </summary>
        internal float TileBaseHeightAt(CellCoord cell) => _tileBase[IndexOf(cell)];

        /// <summary>
        /// 모든 칸을 타일 바탕으로 되돌립니다(바닥 교체·지형 높이 없음). 다시 채우는 중이면 그 채우기를 끝내고,
        /// 시작 전 격자 높이와 비교해 바뀌었으면 <see cref="Version"/>을 올립니다.
        /// </summary>
        internal void Reset()
        {
            float[] terrain = _refilling ? _previousTerrain : _terrain;
            bool changed = false;
            for (int i = 0; i < terrain.Length; i++)
                changed |= terrain[i] != 0f;
            StartFromTiles();
            _refilling = false;
            if (changed) Version++;
        }

        /// <summary>
        /// 다시 채우기를 시작합니다. 지금 격자 높이를 기억하고 모두 타일 바탕으로(바닥 교체·지형 높이 없음) 둡니다.
        /// <see cref="EndRefill"/>까지 <see cref="Version"/>을 바꾸지 않습니다.
        /// </summary>
        internal void BeginRefill()
        {
            Array.Copy(_terrain, _previousTerrain, _terrain.Length);
            StartFromTiles();
            _refilling = true;
        }

        /// <summary>다시 채우기를 끝냅니다. 시작 전과 한 칸이라도 격자 높이(지형 높이)가 다르면 <see cref="Version"/>을 올리고 true입니다.</summary>
        internal bool EndRefill()
        {
            _refilling = false;
            for (int i = 0; i < _terrain.Length; i++)
            {
                if (_terrain[i] == _previousTerrain[i]) continue;
                Version++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 칸의 이번 바탕을 바닥 교체 지형의 높이(바뀐 타일의 윗면, 필드 바닥면 기준, 음수 가능)로 바꿉니다(타일 교체 설계 TR5).
        /// 첫 교체는 타일 바탕을 덮고, 한 칸에 교체 원본이 여럿이면 낮은 값이 남습니다(구멍이 이김, 계획 P10).
        /// 칸 높이 = 이번 바탕 + 지형 높이라 <see cref="Raise"/>와 부르는 순서는 상관없습니다. 격자 높이·<see cref="Version"/>은 바꾸지 않습니다.
        /// </summary>
        internal void Replace(CellCoord cell, float height)
        {
            if (!float.IsFinite(height))
                throw new ArgumentOutOfRangeException(nameof(height), height, "바닥 교체 높이는 유한해야 합니다.");
            int index = IndexOf(cell);
            if (_replaced[index] && height >= _base[index]) return;
            _replaced[index] = true;
            _base[index] = height;
            _heights[index] = height + _terrain[index];
        }

        /// <summary>
        /// 칸을 바탕 위로 지형 높이만큼 올립니다(타일 높이 설계 Q-TH2 B, 계획 P2). 한 칸에 원본이 여럿이면 가장 높은 값이 남고,
        /// 높이 0(효과·벽 등)은 바탕 그대로라 바꾸지 않습니다.
        /// </summary>
        internal void Raise(CellCoord cell, float height)
        {
            if (!float.IsFinite(height) || height < 0f)
                throw new ArgumentOutOfRangeException(nameof(height), height, "지형 높이는 유한한 0 이상이어야 합니다.");
            int index = IndexOf(cell);
            if (height <= _terrain[index]) return;
            _terrain[index] = height;
            _heights[index] = _base[index] + height;
            Changed();
        }

        /// <summary>칸 높이(바탕 + 지형)입니다. 그 칸에 서거나 놓이는 것이 씁니다.</summary>
        internal float HeightAt(CellCoord cell) => _heights[IndexOf(cell)];

        /// <summary>
        /// 격자 높이(지형이 올린 높이만, 0 이상)입니다. GridOutline이 그리는 칸 테두리·채우기·경로선·호버와 포인터·커서 스냅이 씁니다.
        /// 바닥 타일 높이는 보지 않으므로 타일이 솟거나 들어가도 격자는 바닥면에 그대로 있고, 고지대처럼 지형이 올린 칸만 그 높이를 따릅니다
        /// (타일 높이 설계 TH4 개정 2).
        /// </summary>
        internal float OverlayHeightAt(CellCoord cell) => _terrain[IndexOf(cell)];

        private void RebuildOverlayLevels()
        {
            _overlayLevels.Clear();
            for (int i = 0; i < _terrain.Length; i++)
            {
                float height = _terrain[i];
                if (height != 0f && !_overlayLevels.Contains(height)) _overlayLevels.Add(height);
            }
            _overlayLevels.Sort();
            _levelsDirty = false;
        }

        // 모든 칸을 타일 바탕으로 둡니다(바닥 교체·지형 높이 없음). Reset과 다시 채우기 시작이 함께 씁니다.
        private void StartFromTiles()
        {
            Array.Copy(_tileBase, _base, _base.Length);
            Array.Copy(_tileBase, _heights, _heights.Length);
            Array.Clear(_terrain, 0, _terrain.Length);
            Array.Clear(_replaced, 0, _replaced.Length);
            _levelsDirty = true;
        }

        // 다시 채우는 중(Begin~End)에는 끝에서 한 번만 비교하므로 높이 목록만 낡았다고 표시합니다.
        private void Changed()
        {
            _levelsDirty = true;
            if (!_refilling) Version++;
        }

        private int IndexOf(CellCoord cell)
        {
            if (cell.X < 0 || cell.X >= ColumnCount || cell.Y < 0 || cell.Y >= RowCount)
                throw new ArgumentOutOfRangeException(nameof(cell), $"격자 범위를 벗어난 칸입니다: {cell}");
            return (cell.Y * ColumnCount) + cell.X;
        }
    }
}
