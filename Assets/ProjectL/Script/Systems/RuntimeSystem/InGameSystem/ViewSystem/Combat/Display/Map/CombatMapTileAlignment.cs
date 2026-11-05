using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 배경 바닥 타일을 필드에 맞추는 계산입니다(타일 맞춤 설계 TA3, 배경 방식 PrefabWithTiles). 상태가 없고 Transform을 고치지 않습니다.
    /// 값은 모두 필드 칸 루트 기준이고, 적용은 배경 표시(<see cref="StageViewPropPresenter"/>)가 배경과 타일 루트에 똑같이 합니다.
    /// </summary>
    /// <remarks>
    /// 타일 루트는 배경 아래 자식(회전 없음·배율 1)이라 타일 축은 배경 축입니다(2026-10-28 개정 5).
    /// 차례: <see cref="RequireLevel"/>(기울기) → <see cref="YawCorrection"/>(남은 각) → 돌린 뒤 <see cref="AnchorShift"/>(옮길 양).
    /// 놓는 자리는 기준점(게임 (0,0) 칸이 놓일 타일 칸, 필드 축 기준)이 정합니다(2026-10-28 개정 4, 옛 가운데·짝홀 규칙을 대신함).
    /// 축 대응(90°·270°면 가로 칸 수가 필드 Z축)은 이 클래스 한 곳이고,
    /// 필드 격자가 칸 루트의 어디에 놓이는지는 <see cref="FieldGridMapper.CornerLocal(int, int, float, float)"/>를 따릅니다(계획 P13).
    /// </remarks>
    internal static class CombatMapTileAlignment
    {
        // 허용 오차(계획 P4): 기울기·축 평행은 각도입니다.
        private const float LevelToleranceDegrees = 1f;
        private const float AxisToleranceDegrees = 1f;

        /// <summary>타일 격자 면이 필드 바닥면과 평행한지 봅니다. 아니면 경계를 맞출 수 없어 예외입니다.</summary>
        /// <param name="gridUp">타일 루트의 위 방향(칸 루트 기준)입니다.</param>
        internal static void RequireLevel(Vector3 gridUp)
        {
            float tilt = Vector3.Angle(gridUp, Vector3.up);
            if (tilt > LevelToleranceDegrees)
            {
                throw new InvalidOperationException(
                    $"[CombatMapTileAlignment] 바닥 타일 격자가 필드 바닥면과 평행하지 않습니다(기울기 {tilt:0.##}°). 타일 방식에서 배경 회전은 Y축만 씁니다.");
            }
        }

        /// <summary>
        /// 타일 가로축이 필드 X·Z축 중 가장 가까운 쪽과 평행해지도록 칸 루트 위 방향으로 돌릴 각(도, 최대 ±45)입니다.
        /// <c>Transform.RotateAround</c>에 그대로 넘기면 됩니다(양수면 위에서 보아 시계 방향).
        /// 어느 90° 단위로 맞출지는 지금 각에서 가장 가까운 쪽이고, 그 방향(화면 방향)은 배경 회전 값이 정합니다(설계 Q-TA3 A).
        /// </summary>
        /// <param name="columnStep">타일 가로 한 칸 벡터(칸 루트 기준)입니다.</param>
        internal static float YawCorrection(Vector3 columnStep)
        {
            float yaw = Mathf.Atan2(columnStep.z, columnStep.x) * Mathf.Rad2Deg;
            return yaw - (Mathf.Round(yaw / 90f) * 90f);
        }

        /// <summary>
        /// 배경과 타일 루트를 함께 옮길 수평 양(칸 루트 기준, 높이 0)입니다. 옮긴 뒤 기준 타일(<paramref name="anchorTile"/>)의 좌하단(필드 −X·−Z) 모서리가
        /// 게임 (0,0) 칸의 좌하단 모서리에 옵니다. 타일 크기는 칸 간격의 소수 배수도 허용합니다.
        /// </summary>
        /// <param name="gridCenter">돌린 뒤 타일 격자 가운데(칸 루트 기준)입니다.</param>
        /// <param name="columnStep">돌린 뒤 타일 가로 한 칸 벡터(칸 루트 기준)입니다.</param>
        /// <param name="rowStep">돌린 뒤 타일 세로 한 칸 벡터(칸 루트 기준)입니다.</param>
        /// <param name="anchorTile">게임 (0,0) 칸이 놓일 타일 칸입니다. 타일 맵 좌하단 타일이 (0,0)이고, x = 필드 X·y = 필드 Z 방향 타일 수입니다.</param>
        /// <param name="fieldExceedsTiles">필드가 타일 맵 밖으로 넘치는 칸이 있으면 true입니다(설계 Q-TA5 A, 알리기만 함).</param>
        /// <exception cref="InvalidOperationException">
        /// 타일 축이 필드 축과 평행하지 않거나, 타일 크기 비율이 양의 유한 값이 아니거나, 기준 타일이 타일 맵 밖일 때입니다.
        /// </exception>
        internal static Vector3 AnchorShift(Vector3 gridCenter, Vector3 columnStep, Vector3 rowStep, int columns, int rows,
            Vector2Int anchorTile, Vector2Int board, FieldCellSpacing spacing, out bool fieldExceedsTiles)
        {
            if (columns < 1)
                throw new ArgumentOutOfRangeException(nameof(columns));
            if (rows < 1)
                throw new ArgumentOutOfRangeException(nameof(rows));
            if (board.x < 1 || board.y < 1)
                throw new ArgumentOutOfRangeException(nameof(board));

            // 가로 칸이 필드 X축을 따르면 그대로, Z축을 따르면(90°·270°) 가로·세로가 필드 축과 바뀝니다.
            bool columnsAlongX = Mathf.Abs(columnStep.x) >= Mathf.Abs(columnStep.z);
            Vector3 stepX = columnsAlongX ? columnStep : rowStep;
            Vector3 stepZ = columnsAlongX ? rowStep : columnStep;
            int countX = columnsAlongX ? columns : rows;
            int countZ = columnsAlongX ? rows : columns;
            RequireAlong(stepX, Vector3.right, "X");
            RequireAlong(stepZ, Vector3.forward, "Z");
            if (anchorTile.x < 0 || anchorTile.x >= countX || anchorTile.y < 0 || anchorTile.y >= countZ)
            {
                throw new InvalidOperationException(
                    $"[CombatMapTileAlignment] 게임 (0,0)이 놓일 타일 칸 {anchorTile}이 타일 맵(화면 기준 {countX} × {countZ}) 밖입니다.");
            }
            float tileX = Flat(stepX).magnitude;
            float tileZ = Flat(stepZ).magnitude;
            float ratioX = CellRatio(tileX, spacing.Width, "X");
            float ratioZ = CellRatio(tileZ, spacing.Length, "Z");

            // 타일 맵 좌하단이 놓일 자리 = 게임 (0,0) 칸 좌하단에서 기준 타일 수만큼 −X·−Z로 간 자리입니다.
            Vector3 fieldCorner = FieldGridMapper.CornerLocal(board.x, board.y, spacing.Width, spacing.Length);
            float mapMinX = fieldCorner.x - (anchorTile.x * tileX);
            float mapMinZ = fieldCorner.z - (anchorTile.y * tileZ);
            float x = mapMinX - (gridCenter.x - (countX * tileX * 0.5f));
            float z = mapMinZ - (gridCenter.z - (countZ * tileZ * 0.5f));

            // 필드 좌하단은 기준 타일 좌하단이라 타일 맵 안이고, 넘칠 수 있는 쪽은 +X·+Z뿐입니다(칸 단위로 셈).
            fieldExceedsTiles = board.x > (countX - anchorTile.x) * ratioX
                || board.y > (countZ - anchorTile.y) * ratioZ;
            return new Vector3(x, 0f, z);
        }

        // 타일 한 장의 길이를 필드 칸 간격으로 나눈 실수 비율입니다. 1보다 작은 값도 허용합니다.
        private static float CellRatio(float tileLength, float cellSize, string axis)
        {
            float ratio = tileLength / cellSize;
            if (!float.IsFinite(tileLength) || tileLength <= 0f || !float.IsFinite(cellSize) || cellSize <= 0f
                || !float.IsFinite(ratio) || ratio <= 0f)
            {
                throw new InvalidOperationException(
                    $"[CombatMapTileAlignment] 필드 {axis}축 타일 크기({tileLength:0.###})와 칸 간격({cellSize:0.###}) 및 그 비율은 0보다 큰 유한 값이어야 합니다. tileSize·배율 보정을 맞춥니다.");
            }
            return ratio;
        }

        private static void RequireAlong(Vector3 step, Vector3 axis, string name)
        {
            Vector3 flat = Flat(step);
            float angle = Mathf.Min(Vector3.Angle(flat, axis), Vector3.Angle(flat, -axis));
            if (angle > AxisToleranceDegrees)
            {
                throw new InvalidOperationException(
                    $"[CombatMapTileAlignment] 바닥 타일 축이 필드 {name}축과 평행하지 않습니다({angle:0.##}°). 타일 가로·세로가 서로 수직인지 봅니다.");
            }
        }

        private static Vector3 Flat(Vector3 value) => new(value.x, 0f, value.z);
    }
}
