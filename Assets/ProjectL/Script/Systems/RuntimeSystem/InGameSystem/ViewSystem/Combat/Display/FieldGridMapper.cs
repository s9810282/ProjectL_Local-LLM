using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// Core 칸 좌표(CellCoord)와 필드 Transform 좌표를 바꿉니다. Core X → 로컬 X, Core Y → 로컬 Z이며,
    /// 필드 루트가 격자 전체의 중앙에 있다고 봅니다(옛 develop-4week FieldGridMapper 이식).
    /// </summary>
    internal sealed class FieldGridMapper
    {
        private readonly Transform _fieldRoot;
        private readonly float _cellWidth;
        private readonly float _cellLength;

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
        }

        internal int ColumnCount { get; }

        internal int RowCount { get; }

        internal float TotalWidth { get; }

        internal float TotalLength { get; }

        internal bool IsInside(CellCoord cell)
        {
            return cell.X >= 0 && cell.X < ColumnCount && cell.Y >= 0 && cell.Y < RowCount;
        }

        internal Vector3 CellCenterToLocal(CellCoord cell)
        {
            if (!IsInside(cell))
                throw new ArgumentOutOfRangeException(nameof(cell), $"격자 범위를 벗어난 칸입니다: {cell}");

            float localX = ((cell.X + 0.5f) * _cellWidth) - (TotalWidth * 0.5f);
            float localZ = ((cell.Y + 0.5f) * _cellLength) - (TotalLength * 0.5f);
            return new Vector3(localX, 0f, localZ);
        }

        internal Vector3 CellCenterToWorld(CellCoord cell)
        {
            return LocalToWorld(CellCenterToLocal(cell));
        }

        /// <summary>셀 중심 밖의 필드 외곽 좌표도 같은 기준으로 월드 좌표로 바꿉니다.</summary>
        internal Vector3 LocalToWorld(Vector3 localPosition)
        {
            return _fieldRoot.TransformPoint(localPosition);
        }

        /// <summary>Core 방향을 필드 기준 월드 회전으로 바꿉니다. R0은 필드의 +Z(Core +Y)를 봅니다.</summary>
        internal Quaternion RotationToWorld(Rotation4 rotation)
        {
            return _fieldRoot.rotation * Quaternion.Euler(0f, (int)rotation * 90f, 0f);
        }

        internal Vector2 WorldToGrid(Vector3 worldPosition)
        {
            var local = _fieldRoot.InverseTransformPoint(worldPosition);
            return new Vector2((local.x + TotalWidth * .5f) / _cellWidth,
                (local.z + TotalLength * .5f) / _cellLength);
        }

        internal bool TryWorldToCell(Vector3 worldPosition, out CellCoord cell)
        {
            Vector3 local = _fieldRoot.InverseTransformPoint(worldPosition);
            int x = Mathf.FloorToInt((local.x + (TotalWidth * 0.5f)) / _cellWidth);
            int y = Mathf.FloorToInt((local.z + (TotalLength * 0.5f)) / _cellLength);
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
