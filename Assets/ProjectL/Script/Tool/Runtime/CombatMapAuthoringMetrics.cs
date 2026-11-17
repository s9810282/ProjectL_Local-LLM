using System;
using UnityEngine;

namespace ProjectL.Tool.Runtime
{
    /// <summary>전투 배경·바닥 타일·지형의 공통 제작 규격입니다.</summary>
    public static class CombatMapAuthoringMetrics
    {
        public const float TileLength = 2f;
        public static Vector2 TileSize => Vector2.one * TileLength;

        public static float CellScale(float cellWidth, float cellLength, string source)
        {
            if (!float.IsFinite(cellWidth) || !float.IsFinite(cellLength) || cellWidth <= 0f || cellLength <= 0f
                || Mathf.Abs(cellWidth - cellLength) > 0.00001f)
                throw new InvalidOperationException($"{source}: 자동 배율에는 정사각 OutlineCellSpacing이 필요합니다. X={cellWidth}, Z={cellLength}");
            return cellWidth / TileLength;
        }

        public static Vector3 Scale(float cellWidth, float cellLength, Vector3 prefabScale, string source)
        {
            float scale = CellScale(cellWidth, cellLength, source);
            if (!Finite(prefabScale) || (prefabScale - Vector3.one).sqrMagnitude > 0.0000000001f)
                throw new InvalidOperationException($"{source}: 배경 원본 루트 배율은 (1, 1, 1)이어야 합니다. 현재 {prefabScale}. 화면 보존 이전을 먼저 확인하세요.");
            return Vector3.one * scale;
        }

        /// <summary>편집 작업본이 공통 제작 규격을 유지하는지 검사합니다. 규격을 작업본 값으로 덮어쓰지 않습니다.</summary>
        public static void RequireTileSize(Vector2 size, string source)
        {
            if (!float.IsFinite(size.x) || !float.IsFinite(size.y)
                || (size - TileSize).sqrMagnitude > 0.0000000001f)
                throw new InvalidOperationException($"{source}: 작업본 타일 크기 {size}는 공통 제작 규격 {TileSize}와 다릅니다.");
        }

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
