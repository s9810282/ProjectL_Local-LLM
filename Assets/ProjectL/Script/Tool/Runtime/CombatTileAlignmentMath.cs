using System;
using UnityEngine;

namespace ProjectL.Tool.Runtime
{
    /// <summary>전투 바닥 타일을 필드 중심 격자에 맞추는 순수 계산입니다.</summary>
    public static class CombatTileAlignmentMath
    {
        public const float LevelToleranceDegrees = 1f;
        private const float AxisToleranceDegrees = 1f;

        public static void RequireLevel(Vector3 gridUp)
        {
            float tilt = Vector3.Angle(gridUp, Vector3.up);
            if (tilt > LevelToleranceDegrees)
                throw new InvalidOperationException($"바닥 타일 격자가 필드 바닥면과 평행하지 않습니다(기울기 {tilt:0.##}°).");
        }

        public static float YawCorrection(Vector3 columnStep)
        {
            float yaw = Mathf.Atan2(columnStep.z, columnStep.x) * Mathf.Rad2Deg;
            return yaw - Mathf.Round(yaw / 90f) * 90f;
        }

        public static Vector3 AnchorShift(Vector3 gridCenter, Vector3 columnStep, Vector3 rowStep,
            int columns, int rows, Vector2Int anchorTile, Vector2Int board, float cellWidth, float cellLength,
            out bool fieldExceedsTiles)
        {
            if (columns < 1) throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows < 1) throw new ArgumentOutOfRangeException(nameof(rows));
            if (board.x < 1 || board.y < 1) throw new ArgumentOutOfRangeException(nameof(board));

            bool columnsAlongX = Mathf.Abs(columnStep.x) >= Mathf.Abs(columnStep.z);
            Vector3 stepX = columnsAlongX ? columnStep : rowStep;
            Vector3 stepZ = columnsAlongX ? rowStep : columnStep;
            int countX = columnsAlongX ? columns : rows;
            int countZ = columnsAlongX ? rows : columns;
            RequireAlong(stepX, Vector3.right, "X");
            RequireAlong(stepZ, Vector3.forward, "Z");
            if (anchorTile.x < 0 || anchorTile.x >= countX || anchorTile.y < 0 || anchorTile.y >= countZ)
                throw new InvalidOperationException($"기준 타일 {anchorTile}이 타일 맵({countX} × {countZ}) 밖입니다.");

            float tileX = Flat(stepX).magnitude;
            float tileZ = Flat(stepZ).magnitude;
            float ratioX = CellRatio(tileX, cellWidth, "X");
            float ratioZ = CellRatio(tileZ, cellLength, "Z");
            float fieldCornerX = -(board.x * cellWidth) * .5f;
            float fieldCornerZ = -(board.y * cellLength) * .5f;
            float mapMinX = fieldCornerX - anchorTile.x * tileX;
            float mapMinZ = fieldCornerZ - anchorTile.y * tileZ;
            fieldExceedsTiles = board.x > (countX - anchorTile.x) * ratioX
                || board.y > (countZ - anchorTile.y) * ratioZ;
            return new Vector3(mapMinX - (gridCenter.x - countX * tileX * .5f), 0f,
                mapMinZ - (gridCenter.z - countZ * tileZ * .5f));
        }

        public static float CellRatio(float tileLength, float cellSize, string axis)
        {
            float ratio = tileLength / cellSize;
            if (!float.IsFinite(tileLength) || tileLength <= 0f || !float.IsFinite(cellSize) || cellSize <= 0f
                || !float.IsFinite(ratio) || ratio <= 0f)
                throw new InvalidOperationException($"필드 {axis}축 타일 크기({tileLength:0.###})와 칸 간격({cellSize:0.###})은 양의 유한 값이어야 합니다.");
            return ratio;
        }

        private static void RequireAlong(Vector3 step, Vector3 axis, string name)
        {
            Vector3 flat = Flat(step);
            float angle = Mathf.Min(Vector3.Angle(flat, axis), Vector3.Angle(flat, -axis));
            if (angle > AxisToleranceDegrees)
                throw new InvalidOperationException($"바닥 타일 축이 필드 {name}축과 평행하지 않습니다({angle:0.##}°).");
        }

        private static Vector3 Flat(Vector3 value) => new Vector3(value.x, 0f, value.z);
    }
}
