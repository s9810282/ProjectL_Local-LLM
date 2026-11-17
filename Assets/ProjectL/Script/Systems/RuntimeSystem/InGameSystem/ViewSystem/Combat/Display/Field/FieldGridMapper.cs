using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using ProjectL.Tool.Runtime;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// Core 칸 좌표(CellCoord)와 필드 Transform 좌표를 바꿉니다. Core X → 로컬 X, Core Y → 로컬 Z이며,
    /// 필드 루트가 격자 전체의 중앙에 있다고 봅니다(옛 develop-4week FieldGridMapper 이식). 그 자리 규칙은 <see cref="CornerLocal(int, int, float, float)"/> 한 곳입니다.
    /// </summary>
    internal sealed class FieldGridMapper
    {
        private readonly Transform _fieldRoot;
        private readonly float _cellWidth;
        private readonly float _cellLength;
        private float? _terrainCellScale;
        // 격자 −X·−Z 모서리(필드 (0,0) 칸의 좌하단)의 필드 로컬 자리입니다.
        private readonly Vector3 _corner;

        internal FieldGridMapper(Transform fieldRoot, int columnCount, int rowCount, float cellWidth, float cellLength)
        {
            if (fieldRoot == null)
                throw new ArgumentNullException(nameof(fieldRoot));
            if (columnCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(columnCount));
            if (rowCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(rowCount));
            if (!(cellWidth > 0f) || float.IsInfinity(cellWidth))
                throw new ArgumentOutOfRangeException(nameof(cellWidth));
            if (!(cellLength > 0f) || float.IsInfinity(cellLength))
                throw new ArgumentOutOfRangeException(nameof(cellLength));

            _fieldRoot = fieldRoot;
            ColumnCount = columnCount;
            RowCount = rowCount;
            _cellWidth = cellWidth;
            _cellLength = cellLength;
            TotalWidth = columnCount * cellWidth;
            TotalLength = rowCount * cellLength;
            _corner = CornerLocal(columnCount, rowCount, cellWidth, cellLength);
            Surface = new FieldSurfaceHeights(columnCount, rowCount);
        }

        /// <summary>
        /// 격자 −X·−Z 모서리(필드 (0,0) 칸의 좌하단)의 필드 로컬 자리입니다. 격자 가운데를 필드 루트 원점에 두는 규칙은 이 함수 한 곳이고,
        /// 이 Mapper와 필드가 만들어지기 전의 배경 바닥 타일 맞춤(<c>CombatMapTileAlignment</c>)이 함께 부릅니다(타일 맞춤 계획 P13).
        /// </summary>
        internal static Vector3 CornerLocal(int columnCount, int rowCount, float cellWidth, float cellLength)
        {
            return new Vector3(-(columnCount * cellWidth) * 0.5f, 0f, -(rowCount * cellLength) * 0.5f);
        }

        /// <summary>
        /// 격자 모서리(<see cref="CornerLocal"/>)에서 본 칸 가운데의 필드 로컬 자리(높이 0)입니다. 칸 가운데 규칙은 이 함수 한 곳이고,
        /// 이 Mapper(<see cref="CellCenterToLocal"/>)와 필드가 만들어지기 전의 바닥 타일 높이 표(<c>CombatMapTileSurface.Build</c>)가 함께 부릅니다.
        /// </summary>
        internal static Vector3 CenterLocal(Vector3 corner, int column, int row, float cellWidth, float cellLength)
        {
            return new Vector3(corner.x + ((column + 0.5f) * cellWidth), 0f, corner.z + ((row + 0.5f) * cellLength));
        }

        /// <summary>
        /// 이 격자의 칸 표면 높이 표입니다. 기본은 바닥(0)이고, 바탕(바닥 타일)은 표시 설치 때, 지형은 Refresh 맨 앞에서 채웁니다
        /// (고지대 View 설계 P8, 타일 높이 설계 TH6).
        /// </summary>
        internal FieldSurfaceHeights Surface { get; }

        internal int ColumnCount { get; }

        internal int RowCount { get; }

        internal float TotalWidth { get; }

        internal float TotalLength { get; }

        /// <summary>타일 위 지형이 사용하는 제작 규격 대비 칸 배율입니다. 같은 격자에서는 한 번만 구합니다.</summary>
        internal float TerrainCellScale =>
            _terrainCellScale ??= CombatMapAuthoringMetrics.CellScale(_cellWidth, _cellLength, nameof(FieldGridMapper));

        /// <summary>기울어진 필드에서도 같은 표시 평면의 월드 위 방향입니다.</summary>
        internal Vector3 WorldUp => _fieldRoot.up;

        /// <summary>필드 Transform 배율을 반영한 한 칸의 월드 폭입니다. 곡사 높이의 표시 단위입니다.</summary>
        internal float WorldCellWidth => _fieldRoot.TransformVector(Vector3.right * _cellWidth).magnitude;

        internal bool IsInside(CellCoord cell)
        {
            return cell.X >= 0 && cell.X < ColumnCount && cell.Y >= 0 && cell.Y < RowCount;
        }

        internal Vector3 CellCenterToLocal(CellCoord cell)
        {
            if (!IsInside(cell))
                throw new ArgumentOutOfRangeException(nameof(cell), $"격자 범위를 벗어난 칸입니다: {cell}");

            return CenterLocal(_corner, cell.X, cell.Y, _cellWidth, _cellLength);
        }

        /// <summary>
        /// 한 칸 방향(각 성분 -1·0·1)을 필드 로컬 변위로 바꿉니다. 이웃 칸 중심의 차이와 같고, 다음 칸이 판 밖이어도 됩니다
        /// (넉백 미리보기 계획 §2.2).
        /// </summary>
        internal Vector3 StepToLocal(CellCoord step) => new(step.X * _cellWidth, 0f, step.Y * _cellLength);

        /// <summary>
        /// 두 칸 사이 변위(칸 좌표 차이, 크기 제한 없음)의 월드 벡터입니다. 판 안 두 칸이면 두 칸 중심 월드 위치의 차이와 같고(칸 중심 식이 선형),
        /// 한쪽이 판 밖이어도 됩니다. 맞은 방향(<c>CombatHitDirection</c>)이 씁니다 — 작렬이 맵 끝에 부딪히면 피해 근원 칸이 판 밖입니다(작렬 View 계획 §4.3 H1).
        /// </summary>
        internal Vector3 CellOffsetToWorld(CellCoord offset) => _fieldRoot.TransformVector(StepToLocal(offset));

        /// <summary>칸 하나를 접촉 계산의 몸(칸 크기 사각형, 필드 로컬)으로 돌려줍니다. 기차의 필드 진입 시각이 씁니다(카메라 연출 조각 설계 K9).</summary>
        internal CombatBody CellBody(CellCoord cell) =>
            new(CombatBody.Flatten(CellCenterToLocal(cell)), new Vector2(_cellWidth * 0.5f, _cellLength * 0.5f));

        /// <summary>칸 중심의 바닥면(높이 0) 위치입니다. 레일·데칼처럼 바닥 타일과 상관없이 바닥면에 놓는 것이 씁니다.</summary>
        internal Vector3 CellCenterToWorld(CellCoord cell)
        {
            return LocalToWorld(CellCenterToLocal(cell));
        }

        /// <summary>
        /// 칸 중심의 바탕 위치(바닥면 + 그 칸 바탕 높이, 필드 로컬)입니다. 바탕은 바닥 타일 윗면이고, 바닥 교체 지형이 있으면 그 지형의 높이입니다
        /// (<see cref="FieldSurfaceHeights.BaseHeightAt"/>, 타일 교체 설계 TR5). 지형 외형처럼 바탕 위에 놓는 것이 씁니다(타일 높이 설계 TH5).
        /// 바닥 타일·바닥 교체가 없으면 <see cref="CellCenterToLocal"/>과 같습니다.
        /// </summary>
        internal Vector3 CellBaseToLocal(CellCoord cell)
        {
            return CellCenterToLocal(cell) + (Vector3.up * Surface.BaseHeightAt(cell));
        }

        /// <summary>
        /// 칸 중심의 표면 위치(바닥면 + 그 칸 표면 높이 = 바탕 + 지형, 필드 로컬)입니다. 그 위에 서는 것이 씁니다(고지대 View 설계 P1, 타일 높이 설계 TH5).
        /// </summary>
        internal Vector3 CellSurfaceToLocal(CellCoord cell)
        {
            return CellCenterToLocal(cell) + (Vector3.up * Surface.HeightAt(cell));
        }

        /// <summary>
        /// 칸 중심의 표면 위치(<see cref="CellSurfaceToLocal"/>)의 월드 좌표입니다. 유닛·배치 고스트·강조 채우기가 씁니다.
        /// </summary>
        internal Vector3 CellSurfaceToWorld(CellCoord cell)
        {
            return LocalToWorld(CellSurfaceToLocal(cell));
        }

        /// <summary>
        /// 격자 위에 그리는 표시(GridOutline 채우기·커서 스냅)의 칸 중심 월드 위치입니다. 고지대 칸은 블록 윗면, 그 밖은 바닥면이고
        /// 바닥 타일 높이는 보지 않습니다(<see cref="FieldSurfaceHeights.OverlayHeightAt"/>, 타일 높이 설계 TH4 개정 2).
        /// </summary>
        internal Vector3 CellOverlayToWorld(CellCoord cell)
        {
            return LocalToWorld(CellCenterToLocal(cell) + (Vector3.up * Surface.OverlayHeightAt(cell)));
        }

        /// <summary>
        /// 여러 칸을 점유한 대상이 설 높이입니다. 점유 칸 표면 높이 중 가장 높은 값이라 묻히지 않습니다(고지대 View 설계 P2).
        /// 바닥보다 들어간 칸만 점유하면 그 음수 높이입니다(타일 높이 계획 P5). 칸 목록이 비면 기준 칸의 높이입니다.
        /// </summary>
        internal float SurfaceHeightOf(IReadOnlyList<CellCoord> cells, CellCoord anchor)
        {
            if (cells == null || cells.Count == 0)
                return Surface.HeightAt(anchor);

            float height = Surface.HeightAt(cells[0]);
            for (int i = 1; i < cells.Count; i++)
                height = Mathf.Max(height, Surface.HeightAt(cells[i]));
            return height;
        }

        /// <summary>필드 로컬 높이를 월드 변위(필드 위 방향, 필드 배율 반영)로 바꿉니다.</summary>
        internal Vector3 SurfaceOffset(float height) => _fieldRoot.TransformVector(Vector3.up * height);

        /// <summary>셀 중심 밖의 필드 외곽 좌표도 같은 기준으로 월드 좌표로 바꿉니다.</summary>
        internal Vector3 LocalToWorld(Vector3 localPosition)
        {
            return _fieldRoot.TransformPoint(localPosition);
        }

        /// <summary>기존 표시 중심을 이동 경로와 같은 필드 로컬 좌표로 바꿉니다.</summary>
        internal Vector3 WorldToLocal(Vector3 worldPosition) => _fieldRoot.InverseTransformPoint(worldPosition);

        /// <summary>
        /// 월드 점을 그 점이 놓인 칸의 표면 높이로 옮깁니다. 가로 자리는 그대로이고, 격자 밖이면 점 그대로입니다.
        /// 포인터 점(격자 높이)을 따라가는 고스트가 그 칸에 실제로 설 높이(바닥 타일 + 지형)에 놓이게 합니다(타일 높이 계획 §8.7 L6).
        /// </summary>
        internal Vector3 OnSurface(Vector3 worldPosition)
        {
            if (!TryWorldToCell(worldPosition, out CellCoord cell))
                return worldPosition;

            Vector3 local = WorldToLocal(worldPosition);
            local.y = Surface.HeightAt(cell);
            return LocalToWorld(local);
        }

        /// <summary>Core 방향을 필드 기준 월드 회전으로 바꿉니다. R0은 필드의 +Z(Core +Y)를 봅니다.</summary>
        internal Quaternion RotationToWorld(Rotation4 rotation)
        {
            return _fieldRoot.rotation * Quaternion.Euler(0f, (int)rotation * 90f, 0f);
        }

        internal Vector2 WorldToGrid(Vector3 worldPosition)
        {
            var local = _fieldRoot.InverseTransformPoint(worldPosition);
            return new Vector2((local.x - _corner.x) / _cellWidth,
                (local.z - _corner.z) / _cellLength);
        }

        internal bool TryWorldToCell(Vector3 worldPosition, out CellCoord cell)
        {
            Vector3 local = _fieldRoot.InverseTransformPoint(worldPosition);
            int x = Mathf.FloorToInt((local.x - _corner.x) / _cellWidth);
            int y = Mathf.FloorToInt((local.z - _corner.z) / _cellLength);
            var candidate = new CellCoord(x, y);

            if (!IsInside(candidate))
            {
                cell = default;
                return false;
            }

            cell = candidate;
            return true;
        }
    }
}
