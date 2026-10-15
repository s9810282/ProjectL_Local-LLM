using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item
{
    /// <summary>
    /// 전투 필드 표시와 배치 고스트 설정입니다. 표시 모드에 따라 칸 간격의 출처가 다릅니다.
    /// CellPrefab: 프리팹 크기 × cellOffsetSize + cellPaddingX/Y. GridOutline: outlineCellSpacing.
    /// 카메라 설정은 다른 작업자가 맡습니다.
    /// </summary>
    [Serializable]
    internal sealed class StageFieldViewData : IData
    {
        [Header("Display")]
        [Tooltip("CellPrefab: 셀 프리팹과 셀 색. GridOutline: 셀 없이 행동 중에만 격자 데칼과 칸 아웃라인.")]
        [SerializeField] private FieldDisplayMode fieldDisplayMode = FieldDisplayMode.CellPrefab;

        [Header("Cell (CellPrefab 모드)")]
        [SerializeField] private GameObject cellPrefab;
        [Tooltip("번갈아 놓을 두 번째 셀 프리팹입니다. 비워 두면 cellPrefab만 씁니다.")]
        [SerializeField] private GameObject alternateCellPrefab;
        [Tooltip("필드 축 기준 셀 크기 배율입니다. (1, 1, 1)은 원래 크기이며 X/Z는 셀 간격과 클릭 판정에도 반영됩니다.")]
        [SerializeField] private Vector3 cellOffsetSize = Vector3.one;
        [Tooltip("셀 사이 X(열) 방향 간격입니다.")]
        [SerializeField, Min(0f)] private float cellPaddingX;
        [Tooltip("셀 사이 Y(행) 방향 간격입니다.")]
        [SerializeField, Min(0f)] private float cellPaddingY;

        [Header("Grid Outline (GridOutline 모드)")]
        [Tooltip("칸 중심 사이 간격입니다(X = 열, Y = 행). 유닛 위치와 클릭 판정도 이 간격을 따릅니다.")]
        [SerializeField] private Vector2 outlineCellSpacing = Vector2.one;
        [Tooltip("격자 선 두께입니다. 칸 간격보다 작아야 합니다.")]
        [SerializeField, Min(0f)] private float gridLineWidth = 0.05f;
        [Tooltip("셀 격자 선을 그리는 데칼 머티리얼(GridDecal 셰이더)입니다.")]
        [SerializeField] private Material gridDecalMaterial;
        [Tooltip("격자 데칼이 필드 평면 위아래로 투영하는 전체 깊이입니다.")]
        [SerializeField, Min(0.01f)] private float gridDecalDepth = 1f;
        [Tooltip("칸 아웃라인 선 두께입니다.")]
        [SerializeField, Min(0f)] private float outlineThickness = 0.06f;
        [Tooltip("필드 평면 위로 아웃라인을 띄우는 높이입니다. 층마다 이 값에서 조금씩 더 올립니다.")]
        [SerializeField, Min(0f)] private float outlineYOffset = 0.02f;
        [Tooltip("아웃라인 점선 하나(점+틈)의 월드 길이입니다. 테두리 크기와 상관없이 이 길이로 나뉩니다.")]
        [SerializeField, Min(0.01f)] private float outlineDashLength = 0.25f;
        [Tooltip("점선 한 칸에서 선이 차지하는 비율입니다. 1이면 실선입니다.")]
        [SerializeField, Range(0.05f, 1f)] private float outlineDashFill = 0.65f;

        [Header("Picking")]
        [SerializeField] private LayerMask handUnitLayerMask = 1 << 8;

        [Header("Placement Ghost")]
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Color validGhostTint = new(0.45f, 1f, 0.55f, 0.5f);
        [SerializeField] private Color invalidGhostTint = new(1f, 0.4f, 0.4f, 0.5f);
        [SerializeField] private Vector3 ghostWorldOffset = new(0f, 0.05f, 0f);
        [SerializeField, Min(0.01f)] private float ghostScale = 0.5f;
        [SerializeField, Min(0.01f)] private float fieldUnitScale = 0.5f;

        public FieldDisplayMode DisplayMode => fieldDisplayMode;
        /// <summary>CellPrefab 모드에서만 쓰며, GridOutline 모드에서는 null일 수 있습니다.</summary>
        public GameObject CellPrefab => cellPrefab;
        /// <summary>비어 있으면 null입니다.</summary>
        public GameObject AlternateCellPrefab => alternateCellPrefab;
        public Vector3 CellOffsetSize => cellOffsetSize;
        public float CellPaddingX => cellPaddingX;
        public float CellPaddingY => cellPaddingY;
        public Vector2 OutlineCellSpacing => outlineCellSpacing;
        public float GridLineWidth => gridLineWidth;
        /// <summary>GridOutline 모드에서는 반드시 있습니다. CellPrefab 모드에서는 쓰지 않습니다.</summary>
        public Material GridDecalMaterial => gridDecalMaterial;
        public float GridDecalDepth => gridDecalDepth;
        public float OutlineThickness => outlineThickness;
        public float OutlineYOffset => outlineYOffset;
        public float OutlineDashLength => outlineDashLength;
        public float OutlineDashFill => outlineDashFill;
        public LayerMask HandUnitLayerMask => handUnitLayerMask;
        public Material GhostMaterial => ghostMaterial;
        public Color ValidGhostTint => validGhostTint;
        public Color InvalidGhostTint => invalidGhostTint;
        public Vector3 GhostWorldOffset => ghostWorldOffset;
        public float GhostScale => ghostScale;
        public float FieldUnitScale => fieldUnitScale;

        internal void Validate()
        {
            switch (fieldDisplayMode)
            {
                case FieldDisplayMode.CellPrefab:
                    ValidateCellPrefab();
                    break;
                case FieldDisplayMode.GridOutline:
                    ValidateGridOutline();
                    break;
                default:
                    throw new InvalidOperationException(
                        $"[StageFieldViewData] Unknown field display mode: {fieldDisplayMode}.");
            }

            if (!(ghostScale > 0f) || !(fieldUnitScale > 0f))
                throw new InvalidOperationException("[StageFieldViewData] Scale values must be positive.");
        }

        private void ValidateCellPrefab()
        {
            if (cellPrefab == null)
                throw new InvalidOperationException("[StageFieldViewData] Cell prefab is required.");
            if (cellPrefab.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException("[StageFieldViewData] Cell prefab requires a Renderer.");
            if (alternateCellPrefab != null && alternateCellPrefab.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException("[StageFieldViewData] Alternate cell prefab requires a Renderer.");
            if (!float.IsFinite(cellOffsetSize.x) || !float.IsFinite(cellOffsetSize.y) || !float.IsFinite(cellOffsetSize.z)
                || cellOffsetSize.x <= 0f || cellOffsetSize.y <= 0f || cellOffsetSize.z <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Cell offset size must have positive finite components.");
            if (!float.IsFinite(cellPaddingX) || !float.IsFinite(cellPaddingY) || cellPaddingX < 0f || cellPaddingY < 0f)
                throw new InvalidOperationException("[StageFieldViewData] Cell padding must be a finite value of 0 or more.");
        }

        private void ValidateGridOutline()
        {
            if (gridDecalMaterial == null)
                throw new InvalidOperationException("[StageFieldViewData] GridOutline mode requires a grid decal material.");
            if (!float.IsFinite(outlineCellSpacing.x) || !float.IsFinite(outlineCellSpacing.y)
                || outlineCellSpacing.x <= 0f || outlineCellSpacing.y <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Outline cell spacing must be positive.");
            if (!float.IsFinite(gridLineWidth) || gridLineWidth <= 0f
                || gridLineWidth >= Mathf.Min(outlineCellSpacing.x, outlineCellSpacing.y))
                throw new InvalidOperationException("[StageFieldViewData] Grid line width must be positive and smaller than the cell spacing.");
            if (!float.IsFinite(gridDecalDepth) || gridDecalDepth <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Grid decal depth must be positive.");
            if (!float.IsFinite(outlineThickness) || outlineThickness <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Outline thickness must be positive.");
            if (!float.IsFinite(outlineYOffset) || outlineYOffset < 0f)
                throw new InvalidOperationException("[StageFieldViewData] Outline Y offset must be a finite value of 0 or more.");
            if (!float.IsFinite(outlineDashLength) || outlineDashLength <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Outline dash length must be positive.");
            if (!float.IsFinite(outlineDashFill) || outlineDashFill <= 0f || outlineDashFill > 1f)
                throw new InvalidOperationException("[StageFieldViewData] Outline dash fill must be in (0, 1].");
        }
    }

    [CreateAssetMenu(fileName = "NewStageFieldViewData", menuName = "ProjectL/Database/View/StageFieldViewData")]
    internal sealed class StageFieldViewDataSO : DatabaseItem
    {
        [SerializeField] private StageFieldViewData stageFieldViewData = new();

        public override IData GetData() => stageFieldViewData;
    }
}
