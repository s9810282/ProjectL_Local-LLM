using System;
using UnityEngine;

namespace ProjectL.Tool.Runtime
{
    /// <summary>필드 좌표계에서 배경과 타일에 함께 적용할 강체 보정입니다. 부모 구조와 생성·저장은 소비자가 맡습니다.</summary>
    public readonly struct CombatMapPlacementLayout
    {
        private CombatMapPlacementLayout(Vector3 position, Quaternion rotation, bool fieldExceedsTiles)
        {
            Position = position;
            Rotation = rotation;
            FieldExceedsTiles = fieldExceedsTiles;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool FieldExceedsTiles { get; }
        public Vector3 TransformPoint(Vector3 point) => Position + Rotation * point;

        /// <summary>타일 중심 기준 축 보정 후 기준 타일을 필드 (0,0) 칸의 모서리에 맞춥니다.</summary>
        public static CombatMapPlacementLayout Align(Vector3 center, Vector3 columnStep, Vector3 rowStep,
            Vector3 up, int columns, int rows, Vector2Int anchor, Vector2Int board, float cellWidth, float cellLength)
        {
            if (!Finite(center) || !Finite(columnStep) || !Finite(rowStep) || !Finite(up) || up.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException("전투 배경 배치의 중심·축·바닥 방향은 유효한 유한 값이어야 합니다.");
            CombatTileAlignmentMath.RequireLevel(up);
            Quaternion rotation = Quaternion.AngleAxis(CombatTileAlignmentMath.YawCorrection(columnStep), Vector3.up);
            Vector3 shift = CombatTileAlignmentMath.AnchorShift(center, rotation * columnStep, rotation * rowStep,
                columns, rows, anchor, board, cellWidth, cellLength, out bool exceeds);
            return new CombatMapPlacementLayout(center - rotation * center + shift, rotation, exceeds);
        }

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
