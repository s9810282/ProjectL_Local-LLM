using System;
using UnityEngine;

namespace ProjectL.Tool.Runtime
{
    /// <summary>전투 필드와 편집 씬 데칼의 칸 경계·투영 상자 계산입니다.</summary>
    public readonly struct CombatGridDecalLayout
    {
        private static readonly int CellSizeId = Shader.PropertyToID("_CellSize");
        private static readonly int GridSizeId = Shader.PropertyToID("_GridSize");
        private static readonly int GridEdgeSizeId = Shader.PropertyToID("_GridEdgeSize");
        private static readonly int GridOriginId = Shader.PropertyToID("_GridOrigin");
        private static readonly int MaskCenterId = Shader.PropertyToID("_MaskCenter");

        private readonly Vector4 _cellSize;
        private readonly float _edge;
        private readonly Vector4 _origin;
        private readonly Vector4 _maskCenter;

        private CombatGridDecalLayout(Vector4 cellSize, float edge, Vector4 origin, Vector4 maskCenter,
            Vector3 localPosition, Vector3 size, Vector3 pivot)
        {
            _cellSize = cellSize;
            _edge = edge;
            _origin = origin;
            _maskCenter = maskCenter;
            LocalPosition = localPosition;
            Size = size;
            Pivot = pivot;
        }

        public Vector3 LocalPosition { get; }
        public Vector3 Size { get; }
        public Vector3 Pivot { get; }

        public static CombatGridDecalLayout Create(Transform fieldRoot, int columns, int rows,
            float cellWidth, float cellLength, float lineWidth, float depth, float surfaceTop = 0f)
        {
            if (fieldRoot == null) throw new ArgumentNullException(nameof(fieldRoot));
            if (columns < 1 || rows < 1) throw new ArgumentOutOfRangeException(nameof(columns));
            if (!float.IsFinite(cellWidth) || !float.IsFinite(cellLength) || cellWidth <= 0f || cellLength <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellWidth));
            if (!float.IsFinite(lineWidth) || lineWidth <= 0f || lineWidth >= Mathf.Min(cellWidth, cellLength))
                throw new ArgumentOutOfRangeException(nameof(lineWidth));
            if (!float.IsFinite(depth) || depth <= 0f || !float.IsFinite(surfaceTop))
                throw new ArgumentOutOfRangeException(nameof(depth));

            float totalWidth = columns * cellWidth;
            float totalLength = rows * cellLength;
            Vector3 corner = fieldRoot.TransformPoint(new Vector3(-totalWidth * 0.5f, 0f, -totalLength * 0.5f));
            Vector3 center = fieldRoot.position;
            float lineRatio = lineWidth / Mathf.Max(cellWidth, cellLength);
            float edge = Mathf.Clamp01(1f - (lineRatio * 0.5f));
            float top = Mathf.Max(0f, surfaceTop);
            float height = depth + top;
            return new CombatGridDecalLayout(new Vector4(cellWidth, cellLength, 0f, 0f), edge,
                new Vector4(corner.x, corner.z, 0f, 0f), new Vector4(center.x, center.z, 0f, 0f),
                new Vector3(0f, (depth * 0.5f) + top, 0f),
                new Vector3(totalWidth, totalLength, height), new Vector3(0f, 0f, height * 0.5f));
        }

        public void ApplyMaterial(Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            material.SetVector(CellSizeId, _cellSize);
            material.SetFloat(GridSizeId, 1f);
            material.SetFloat(GridEdgeSizeId, _edge);
            material.SetVector(GridOriginId, _origin);
            material.SetVector(MaskCenterId, _maskCenter);
        }
    }
}
