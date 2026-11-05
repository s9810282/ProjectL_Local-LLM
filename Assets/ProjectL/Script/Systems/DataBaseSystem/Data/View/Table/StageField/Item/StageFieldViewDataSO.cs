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
        // 겹침 축소 비율 범위의 원본입니다. 인스펙터 슬라이더와 검사가 함께 씁니다(계획 §7.6 L2).
        private const float MinOverlapInsetScale = 0.05f;
        private const float MaxOverlapInsetScale = 0.95f;
        // 모서리 반지름 비율(칸 테두리·겹친 칸 안쪽 판) 범위의 원본입니다(2단계 계획 TP-2, 3판 RC7).
        private const float MinCornerRadiusRatio = 0f;
        private const float MaxCornerRadiusRatio = 1f;
        // 칸마다 방식의 밑 층 칸 크기 비율 범위의 원본입니다(2단계 5판 RC14). 1이면 칸 전체입니다.
        private const float MinCellOutlineScale = 0.05f;
        private const float MaxCellOutlineScale = 1f;

        [Header("Display")]
        [Tooltip("CellPrefab: 셀 프리팹과 셀 색. GridOutline: 셀 없이 바닥 격자 데칼(showGridDecal로 켜고 끔)과 칸 아웃라인.")]
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

        [Header("Grid (GridOutline 모드 공통)")]
        [Tooltip("칸 중심 사이 간격입니다(X = 열, Y = 행). 격자 데칼·칸 테두리·칸 채우기·이동 경로선·유닛 위치·클릭 판정이 모두 이 간격을 따릅니다(격자 자체라 나누지 않음).")]
        [SerializeField] private Vector2 outlineCellSpacing = Vector2.one;

        [Header("Grid Decal (GridOutline 바닥 격자 선)")]
        [Tooltip("바닥 격자 선(격자 데칼)을 그릴지입니다. 끄면 아래 격자 데칼 값 넷(선 두께·머티리얼·깊이·Rendering Layer)은 쓰지도 검사하지도 않고, 칸 아웃라인·채우기는 그대로 그립니다. 바꾼 값은 다음 Combat 진입부터 적용됩니다.")]
        [SerializeField] private bool showGridDecal = true;
        [Tooltip("격자 선 두께입니다. 칸 간격보다 작아야 합니다.")]
        [SerializeField, Min(0f)] private float gridLineWidth = 0.05f;
        [Tooltip("셀 격자 선을 그리는 데칼 머티리얼(GridDecal 셰이더)입니다.")]
        [SerializeField] private Material gridDecalMaterial;
        [Tooltip("격자 데칼이 필드 평면 위아래로 투영하는 전체 깊이입니다.")]
        [SerializeField, Min(0.01f)] private float gridDecalDepth = 1f;
        // RenderingLayerMask 형식은 TriInspector가 직렬화 불가로 오인해(UAC1010) 필드를 비활성으로 그리므로 이름으로 받습니다.
        [Tooltip("격자 데칼을 받는 표면의 Rendering Layer 이름입니다(Project Settings > Tags and Layers > Rendering Layers). 바닥 Renderer에도 같은 레이어를 켜야 찍힙니다. Decal Renderer Feature의 Use Rendering Layers가 꺼져 있으면 무시되고 모든 표면에 찍힙니다.")]
        [SerializeField] private string gridDecalRenderingLayer = "Default";

        [Header("Cell Outline (GridOutline 칸 점선 테두리·채우기)")]
        [Tooltip("칸 테두리(레일·배치 구역·범위 층·호버) 점선 두께입니다. 이동 경로선은 아래 Movement Path 값을 씁니다.")]
        [SerializeField, Min(0f)] private float outlineThickness = 0.06f;
        [Tooltip("필드 평면 위로 칸 테두리와 칸 채우기를 띄우는 높이입니다. 층마다 이 값에서 조금씩 더 올립니다. 이동 경로선은 아래 Movement Path 값을 씁니다.")]
        [SerializeField, Min(0f)] private float outlineYOffset = 0.02f;
        [Tooltip("아웃라인 점선 하나(점+틈)의 월드 길이입니다. 테두리 크기와 상관없이 이 길이로 나뉩니다.")]
        [SerializeField, Min(0.01f)] private float outlineDashLength = 0.25f;
        [Tooltip("점선 한 칸에서 선이 차지하는 비율입니다. 1이면 실선입니다.")]
        [SerializeField, Range(0.05f, 1f)] private float outlineDashFill = 0.65f;
        [Tooltip("겹치지 않은 기본 강조(깊이 0)의 칸 테두리·채우기 판을 묶는 방식입니다. Merged: 맞닿은 칸을 한 덩어리로 묶어 바깥 둘레만 그립니다. EachCell: 칸마다 네 변을 모두 그립니다. 칸 크기(cellOutlineScale)가 1이고 모서리가 직각(outlineCornerRadius 0)이면 맞닿은 변은 한 번만, 아니면 칸마다 따로 그립니다. 레일·배치 구역·범위 층·호버의 깊이 0에 적용하고, 겹친 강조는 overlapCellOutlineStyle을 따릅니다. 바꾼 값은 다음 Combat 진입부터 적용됩니다.")]
        [SerializeField] private FieldCellOutlineStyle cellOutlineStyle = FieldCellOutlineStyle.Merged;
        [Tooltip("기본 강조 위에 겹친 강조(깊이 1 이상)의 칸 테두리·채우기 판을 묶는 방식입니다. Merged: 같은 종류·같은 깊이·같은 높이의 맞닿은 칸을 한 덩어리로 잇습니다. EachCell: 칸마다 따로 그립니다. 겹친 호버도 실제 깊이의 방식을 따릅니다. 크기·축소 비율·모서리 값은 기존 값을 함께 쓰며, 바꾼 값은 다음 Combat 진입부터 적용됩니다.")]
        [SerializeField] private FieldCellOutlineStyle overlapCellOutlineStyle = FieldCellOutlineStyle.Merged;
        [Tooltip("EachCell 방식에서 겹치지 않은 칸(맨 아래)의 테두리·채우기 크기입니다(칸 대비 비율). 1이면 칸 전체이고 맞닿은 변을 한 번만 그립니다. 1보다 작으면 칸마다 가운데로 줄인 네모를 따로 그립니다(1에 가까워 줄어드는 거리가 테두리 두께 반보다 작으면 이웃 칸 테두리 띠가 겹침). 겹친 칸은 이 크기 × overlapInsetScale^k입니다. Merged 방식에서는 쓰지 않습니다. 바꾼 값은 다음 Combat 진입부터 적용됩니다.")]
        [SerializeField, Range(MinCellOutlineScale, MaxCellOutlineScale)] private float cellOutlineScale = 1f;
        [Tooltip("겹치지 않은 칸 테두리의 바깥으로 튀어나온 모서리 반지름입니다(크기 짧은 반변 대비 비율). Merged는 덩어리(칸 크기)의 볼록 모서리만 둥글고 안으로 꺾인 모서리는 늘 직각이며, 맨 아래 채우기는 늘 네모입니다. EachCell은 칸(칸 × cellOutlineScale)마다 네 모서리가 둥글고 맨 아래 채우기도 같은 모양입니다(칸 크기 1에서 0보다 크면 칸마다 고리라 맞닿은 곧은 변이 두 번 겹쳐 진하게 보일 수 있음). 0이면 모두 직각, 1이면 한 칸짜리가 원(정사각 칸)입니다. 겹친 칸 안쪽 판의 테두리는 overlapCornerRadius를 씁니다. 바꾼 값은 다음 Combat 진입부터 적용됩니다.")]
        [SerializeField, Range(MinCornerRadiusRatio, MaxCornerRadiusRatio)] private float outlineCornerRadius;
        [Tooltip("행동 중 배치 구역·이동/공격 범위·공격 영향·호버 칸을 반투명하게 채우는 머티리얼(ProjectL/Combat/CellFill 셰이더)입니다. 비워 두면 채우지 않습니다. 채우기 알파는 강조 색 알파 × FieldView fillAlpha입니다.")]
        [SerializeField] private Material highlightFillMaterial;
        [Tooltip("한 칸에 채우기가 겹치면 위 채우기를 한 단계마다 이 비율로 줄여 안쪽에 그립니다(아래부터 k번째 = 기본 칸 크기 × 비율^k, 기본 방식이 EachCell이면 cellOutlineScale도 곱함). 겹친 강조는 overlapCellOutlineStyle이 Merged면 같은 층끼리 한 덩어리로 이어지고 바깥 둘레만 그만큼 안쪽으로 들어가며, EachCell이면 칸마다 따로 줄어듭니다. 0.95에 가까우면 안쪽으로 드는 거리가 테두리 두께보다 작아 칸 테두리와 겹친 칸 테두리 띠가 겹칠 수 있습니다. 범위는 슬라이더와 같습니다. 바꾼 값은 다음 Combat 진입부터 적용됩니다.")]
        [SerializeField, Range(MinOverlapInsetScale, MaxOverlapInsetScale)] private float overlapInsetScale = 0.7f;
        [Tooltip("겹친 칸 안쪽 판(Merged는 덩어리, EachCell은 칸)과 그 둘레 테두리의 바깥으로 튀어나온 모서리 반지름입니다(판 짧은 반변 대비 비율). 안으로 꺾인 모서리는 늘 직각입니다. 0이면 네모, 1이면 짧은 변이 반원입니다. 맨 아래 채우기·테두리는 outlineCornerRadius를 씁니다. 바꾼 값은 다음 Combat 진입부터 적용됩니다.")]
        [SerializeField, Range(MinCornerRadiusRatio, MaxCornerRadiusRatio)] private float overlapCornerRadius = 0.35f;

        [Header("Movement Path (GridOutline 이동 경로 띠·화살촉)")]
        [Tooltip("이동 Drag 때 칸 중심을 잇는 경로 띠의 두께(월드 길이)입니다. 띠·화살촉 색은 Combat 씬 FieldView의 이동 초안 색입니다.")]
        [SerializeField, Min(0f)] private float movementPathThickness = 0.06f;
        [Tooltip("필드 평면 위로 이동 경로 띠와 화살촉을 띄우는 높이입니다. 칸 테두리 높이(outlineYOffset) 이상이어야 합니다. 그 위에 모든 칸 테두리보다 한 단계 더 올려 반투명 채우기·테두리가 경로 위에 덧칠되지 않게 하고, 지형(고지대) 칸에서는 그 높이를 더합니다.")]
        [SerializeField, Min(0f)] private float movementPathYOffset = 0.02f;
        [Tooltip("이동 경로 띠와 경로 끝 화살촉을 함께 그리는 머티리얼입니다. _BaseColor가 있어야 하며, 색은 이동 초안 색으로 덮어씁니다. GridOutline 모드에서 필수입니다.")]
        [SerializeField] private Material movementPathMaterial;
        [Tooltip("경로 끝 화살촉의 길이(진행 방향, 월드 길이)입니다. 칸 간격 이하여야 합니다.")]
        [SerializeField, Min(0f)] private float movementPathArrowLength = 0.5f;
        [Tooltip("화살촉 밑변의 너비(월드 길이)입니다. 경로 띠 두께의 두 배 이상이어야 띠 끝이 화살촉 안에 듭니다.")]
        [SerializeField, Min(0f)] private float movementPathArrowWidth = 0.8f;
        [Tooltip("화살촉 꼭지 자리입니다(월드 길이). 0이면 도착 칸 가운데이고, 양수면 진행 방향 앞으로, 음수면 뒤로 옮깁니다. 칸 간격의 절반 안이어야 하고, 화살촉 길이의 절반에서 이 값을 뺀 것이 칸 간격보다 짧아야 합니다(마지막 걸음의 띠가 남도록).")]
        [SerializeField] private float movementPathArrowTipOffset;

        [Header("Attack Trajectory (공격 궤적 설계 TJ8)")]
        [Tooltip("공격 모드 호버 궤적 점선을 그리는 머티리얼(AreaOutline 셰이더)입니다. 필수입니다.")]
        [SerializeField] private Material trajectoryMaterial;
        [Tooltip("궤적 점선의 폭(월드 길이)입니다.")]
        [SerializeField, Min(0f)] private float trajectoryThickness = 0.08f;
        [Tooltip("점과 점 사이 간격(월드 길이)입니다. 경로를 이 간격에 가장 가까운 같은 길이 구간으로 나눠 구간마다 점 하나를 찍습니다.")]
        [SerializeField, Min(0f)] private float trajectoryDotSpacing = 0.3f;
        [Tooltip("간격 한 칸에서 점이 차지하는 비율입니다.")]
        [SerializeField, Range(0.05f, 1f)] private float trajectoryDotFill = 0.45f;
        [Tooltip("점이 목표 쪽으로 흐르는 속도입니다. 0이면 멈춰 있습니다.")]
        [SerializeField, Min(0f)] private float trajectoryFlowSpeed = 0.6f;
        [Tooltip("벽에 막혔을 때 끊긴 자리에 띄우는 표지(X) 프리팹입니다. 비워 두면 표지 없이 궤적만 끊깁니다. 있으면 Renderer가 있어야 합니다.")]
        [SerializeField] private GameObject trajectoryBlockedMarker;

        [Header("Knockback Preview (넉백 미리보기 설계 KB6)")]
        [Tooltip("공격 호버 때 대상 앞에 놓는 공용 삼각형 화살촉의 재질입니다. 필수이며 _BaseColor를 밀림·막힘·면역 색으로 덮어씁니다.")]
        [SerializeField] private Material knockbackArrowMaterial;
        [Tooltip("넉백 화살촉의 진행 방향 길이(필드 로컬 단위)입니다. 이동 경로와 같은 삼각형 모양입니다.")]
        [SerializeField, Min(0f)] private float knockbackArrowLength = 0.5f;
        [Tooltip("넉백 화살촉 밑변의 너비(필드 로컬 단위)입니다.")]
        [SerializeField, Min(0f)] private float knockbackArrowWidth = 0.8f;
        [Tooltip("화살표를 대상 칸 표면 위로 띄우는 높이입니다.")]
        [SerializeField, Min(0f)] private float knockbackArrowLift = 0.05f;

        [Header("Picking")]
        [SerializeField] private LayerMask handUnitLayerMask = 1 << 8;

        [Header("Placement Ghost")]
        [SerializeField] private Material ghostMaterial;
        [SerializeField] private Color validGhostTint = new(0.45f, 1f, 0.55f, 0.5f);
        [SerializeField] private Color invalidGhostTint = new(1f, 0.4f, 0.4f, 0.5f);
        [SerializeField] private Vector3 ghostWorldOffset = new(0f, 0.05f, 0f);
        [SerializeField, Min(0.01f)] private float ghostScale = 0.5f;
        [SerializeField, Min(0.01f)] private float fieldUnitScale = 0.5f;
        [Header("Movement Planning")]
        [SerializeField, Range(0.01f, 1f)] private float movementOriginalOpacity = 0.65f;
        [SerializeField] private Color neutralGhostTint = new(0.8f, 0.8f, 0.8f, 0.5f);

        public FieldDisplayMode DisplayMode => fieldDisplayMode;
        /// <summary>CellPrefab 모드에서만 쓰며, GridOutline 모드에서는 null일 수 있습니다.</summary>
        public GameObject CellPrefab => cellPrefab;
        /// <summary>비어 있으면 null입니다.</summary>
        public GameObject AlternateCellPrefab => alternateCellPrefab;
        public Vector3 CellOffsetSize => cellOffsetSize;
        public float CellPaddingX => cellPaddingX;
        public float CellPaddingY => cellPaddingY;
        public Vector2 OutlineCellSpacing => outlineCellSpacing;
        /// <summary>GridOutline 모드에서 바닥 격자 데칼을 그릴지입니다. 거짓이면 격자 데칼 값(선 두께·머티리얼·깊이·Rendering Layer)을 쓰지 않습니다.</summary>
        public bool ShowGridDecal => showGridDecal;
        public float GridLineWidth => gridLineWidth;
        /// <summary>GridOutline 모드에서 격자 데칼을 켜면(<see cref="ShowGridDecal"/>) 반드시 있습니다. 그 밖에는 쓰지 않으며 null일 수 있습니다.</summary>
        public Material GridDecalMaterial => gridDecalMaterial;
        public float GridDecalDepth => gridDecalDepth;
        /// <summary>
        /// 격자 데칼의 Rendering Layer 마스크(레이어 하나의 비트)입니다. 표면 Renderer의 Rendering Layer와 겹쳐야 그 표면에 찍힙니다.
        /// GridOutline 모드에서 격자 데칼을 켜면 <see cref="Validate"/>가 이름이 정의돼 있음을 확인합니다. 정의되지 않은 이름이면 0입니다.
        /// </summary>
        public uint GridDecalRenderingLayers
        {
            get
            {
                int index = string.IsNullOrEmpty(gridDecalRenderingLayer) ? -1 : RenderingLayerMask.NameToRenderingLayer(gridDecalRenderingLayer);
                return index < 0 ? 0u : 1u << index;
            }
        }
        public float OutlineThickness => outlineThickness;
        public float OutlineYOffset => outlineYOffset;
        public float OutlineDashLength => outlineDashLength;
        public float OutlineDashFill => outlineDashFill;
        /// <summary>GridOutline 모드의 기본 강조(깊이 0)를 덩어리로 묶을지, 칸마다 따로 그릴지입니다.</summary>
        public FieldCellOutlineStyle CellOutlineStyle => cellOutlineStyle;
        /// <summary>GridOutline 모드의 겹친 강조(깊이 1 이상)를 덩어리로 묶을지, 칸마다 따로 그릴지입니다.</summary>
        public FieldCellOutlineStyle OverlapCellOutlineStyle => overlapCellOutlineStyle;
        /// <summary><see cref="FieldCellOutlineStyle.EachCell"/> 방식의 겹치지 않은 칸 크기(칸 대비 비율, (0, 1])입니다. Merged에서는 쓰지 않습니다.</summary>
        public float CellOutlineScale => cellOutlineScale;
        /// <summary>겹치지 않은 칸 테두리(Merged는 덩어리, EachCell은 칸) 모서리 반지름입니다(크기 짧은 반변 대비 비율, 0 = 직각).</summary>
        public float OutlineCornerRadius => outlineCornerRadius;
        /// <summary>GridOutline 모드의 칸 채우기 머티리얼입니다. 비어 있으면 null이며 채우지 않습니다.</summary>
        public Material HighlightFillMaterial => highlightFillMaterial;
        /// <summary>
        /// GridOutline 모드에서 한 칸에 겹친 채우기를 한 단계마다 줄이는 비율입니다(겹친 강조 안쪽 축소 설계 OI1·OI3). 0보다 크고 1보다 작습니다.
        /// </summary>
        public float OverlapInsetScale => overlapInsetScale;
        /// <summary>겹친 칸 안쪽 판과 그 둘레 테두리의 모서리 반지름입니다(판 짧은 반변 대비 비율).</summary>
        public float OverlapCornerRadius => overlapCornerRadius;
        /// <summary>GridOutline 모드 이동 경로 띠의 두께입니다. 칸 테두리 두께(<see cref="OutlineThickness"/>)와 따로입니다.</summary>
        public float MovementPathThickness => movementPathThickness;
        /// <summary>GridOutline 모드 이동 경로 띠·화살촉을 띄우는 높이입니다. 칸 테두리 높이(<see cref="OutlineYOffset"/>)와 따로입니다.</summary>
        public float MovementPathYOffset => movementPathYOffset;
        /// <summary>
        /// GridOutline 모드 이동 경로 띠·화살촉 머티리얼입니다. GridOutline 모드면 <see cref="Validate"/>가 반드시 있게 하고, 그 밖에는 null일 수 있습니다.
        /// _BaseColor가 있는지는 그리기 부품이 필드 구성 때 봅니다(이동 경로 화살표 계획 TP-6).
        /// </summary>
        public Material MovementPathMaterial => movementPathMaterial;
        /// <summary>경로 끝 화살촉의 길이(진행 방향)입니다.</summary>
        public float MovementPathArrowLength => movementPathArrowLength;
        /// <summary>화살촉 밑변의 너비입니다. 경로 띠 두께의 두 배 이상입니다.</summary>
        public float MovementPathArrowWidth => movementPathArrowWidth;
        /// <summary>화살촉 꼭지 자리입니다. 도착 칸 가운데에서 진행 방향으로 앞(양수)·뒤(음수)로 옮긴 거리입니다.</summary>
        public float MovementPathArrowTipOffset => movementPathArrowTipOffset;
        /// <summary>
        /// 궤적 점선 머티리얼입니다. DB 표는 <see cref="Validate"/>로 반드시 있게 하지만, 시험이 직접 만든 설정에서는 null일 수 있으며
        /// 그때 필드는 궤적 부품을 만들지 않습니다(공격 궤적 계획 P1).
        /// </summary>
        public Material TrajectoryMaterial => trajectoryMaterial;
        public float TrajectoryThickness => trajectoryThickness;
        public float TrajectoryDotSpacing => trajectoryDotSpacing;
        public float TrajectoryDotFill => trajectoryDotFill;
        public float TrajectoryFlowSpeed => trajectoryFlowSpeed;
        /// <summary>막힘 표지 프리팹입니다. 비어 있으면 null이며 표지를 띄우지 않습니다(공격 궤적 계획 P2).</summary>
        public GameObject TrajectoryBlockedMarker => trajectoryBlockedMarker;
        /// <summary>
        /// 넉백 화살촉 재질입니다. DB 표는 <see cref="Validate"/>로 반드시 있게 하지만, 시험이 직접 만든 설정에서는 null일 수 있으며
        /// 그때 필드는 넉백 표지 부품을 만들지 않습니다(넉백 미리보기 설계 P7).
        /// </summary>
        public Material KnockbackArrowMaterial => knockbackArrowMaterial;
        public float KnockbackArrowLength => knockbackArrowLength;
        public float KnockbackArrowWidth => knockbackArrowWidth;
        public float KnockbackArrowLift => knockbackArrowLift;
        public LayerMask HandUnitLayerMask => handUnitLayerMask;
        public Material GhostMaterial => ghostMaterial;
        public Color ValidGhostTint => validGhostTint;
        public Color InvalidGhostTint => invalidGhostTint;
        public Vector3 GhostWorldOffset => ghostWorldOffset;
        public float GhostScale => ghostScale;
        public float FieldUnitScale => fieldUnitScale;
        public float MovementOriginalOpacity => movementOriginalOpacity;
        public Color NeutralGhostTint => neutralGhostTint;

        internal void Validate()
        {
            if (!float.IsFinite(movementOriginalOpacity) || movementOriginalOpacity <= 0f || movementOriginalOpacity > 1f)
                throw new InvalidOperationException("[StageFieldViewData] 이동 원본 불투명도는 유한한 (0, 1]이어야 합니다.");
            if (!float.IsFinite(neutralGhostTint.r) || !float.IsFinite(neutralGhostTint.g) || !float.IsFinite(neutralGhostTint.b) ||
                !float.IsFinite(neutralGhostTint.a) || neutralGhostTint.a < 0f || neutralGhostTint.a > 1f)
                throw new InvalidOperationException("[StageFieldViewData] 중립 고스트 색은 유한하고 알파는 [0, 1]이어야 합니다.");
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
            ValidateTrajectory();
            ValidateKnockbackPreview();
        }

        /// <summary>공격 궤적 값 조건의 원본입니다. 표시 모드와 상관없이 검사합니다(공격 궤적 설계 TJ8).</summary>
        private void ValidateTrajectory()
        {
            if (trajectoryMaterial == null)
                throw new InvalidOperationException("[StageFieldViewData] Attack trajectory material is required.");
            if (!float.IsFinite(trajectoryThickness) || trajectoryThickness <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Attack trajectory thickness must be positive.");
            if (!float.IsFinite(trajectoryDotSpacing) || trajectoryDotSpacing <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Attack trajectory dot spacing must be positive.");
            if (!float.IsFinite(trajectoryDotFill) || trajectoryDotFill <= 0f || trajectoryDotFill > 1f)
                throw new InvalidOperationException("[StageFieldViewData] Attack trajectory dot fill must be in (0, 1].");
            if (!float.IsFinite(trajectoryFlowSpeed) || trajectoryFlowSpeed < 0f)
                throw new InvalidOperationException("[StageFieldViewData] Attack trajectory flow speed must be a finite value of 0 or more.");
            if (trajectoryBlockedMarker != null && trajectoryBlockedMarker.GetComponentInChildren<Renderer>(true) == null)
                throw new InvalidOperationException("[StageFieldViewData] Attack trajectory blocked marker requires a Renderer.");
        }

        /// <summary>
        /// 넉백 미리보기 값 조건의 원본입니다. 표시 모드와 상관없이 검사합니다(넉백 미리보기 설계 KB6).
        /// 재질의 색 덮어쓰기 속성은 렌더 쪽 사실이라 여기서 보지 않고 표지 부품이 필드 구성 때 봅니다(계획 TP-3).
        /// </summary>
        private void ValidateKnockbackPreview()
        {
            if (knockbackArrowMaterial == null)
                throw new InvalidOperationException("[StageFieldViewData] 넉백 화살촉 재질이 필요합니다.");
            if (!float.IsFinite(knockbackArrowLength) || knockbackArrowLength <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] 넉백 화살촉 길이는 유한한 양수여야 합니다.");
            if (!float.IsFinite(knockbackArrowWidth) || knockbackArrowWidth <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] 넉백 화살촉 너비는 유한한 양수여야 합니다.");
            if (!float.IsFinite(knockbackArrowLift) || knockbackArrowLift < 0f)
                throw new InvalidOperationException("[StageFieldViewData] Knockback arrow lift must be a finite value of 0 or more.");
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
            if (!float.IsFinite(outlineCellSpacing.x) || !float.IsFinite(outlineCellSpacing.y)
                || outlineCellSpacing.x <= 0f || outlineCellSpacing.y <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Outline cell spacing must be positive.");
            if (showGridDecal)
                ValidateGridDecal();
            if (!float.IsFinite(outlineThickness) || outlineThickness <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Outline thickness must be positive.");
            if (!float.IsFinite(outlineYOffset) || outlineYOffset < 0f)
                throw new InvalidOperationException("[StageFieldViewData] Outline Y offset must be a finite value of 0 or more.");
            if (!float.IsFinite(outlineDashLength) || outlineDashLength <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Outline dash length must be positive.");
            if (!float.IsFinite(outlineDashFill) || outlineDashFill <= 0f || outlineDashFill > 1f)
                throw new InvalidOperationException("[StageFieldViewData] Outline dash fill must be in (0, 1].");
            if (!System.Enum.IsDefined(typeof(FieldCellOutlineStyle), cellOutlineStyle))
                throw new InvalidOperationException($"[StageFieldViewData] Unknown cell outline style: {cellOutlineStyle}.");
            if (!System.Enum.IsDefined(typeof(FieldCellOutlineStyle), overlapCellOutlineStyle))
                throw new InvalidOperationException($"[StageFieldViewData] Unknown overlap cell outline style: {overlapCellOutlineStyle}.");
            if (!float.IsFinite(cellOutlineScale) || cellOutlineScale < MinCellOutlineScale || cellOutlineScale > MaxCellOutlineScale)
                throw new InvalidOperationException(
                    $"[StageFieldViewData] Cell outline scale must be in [{MinCellOutlineScale}, {MaxCellOutlineScale}].");
            if (!float.IsFinite(overlapInsetScale) || overlapInsetScale < MinOverlapInsetScale || overlapInsetScale > MaxOverlapInsetScale)
                throw new InvalidOperationException(
                    $"[StageFieldViewData] Overlap inset scale must be in [{MinOverlapInsetScale}, {MaxOverlapInsetScale}].");
            if (!float.IsFinite(outlineCornerRadius) || outlineCornerRadius < MinCornerRadiusRatio || outlineCornerRadius > MaxCornerRadiusRatio)
                throw new InvalidOperationException(
                    $"[StageFieldViewData] Outline corner radius must be in [{MinCornerRadiusRatio}, {MaxCornerRadiusRatio}].");
            if (!float.IsFinite(overlapCornerRadius) || overlapCornerRadius < MinCornerRadiusRatio || overlapCornerRadius > MaxCornerRadiusRatio)
                throw new InvalidOperationException(
                    $"[StageFieldViewData] Overlap corner radius must be in [{MinCornerRadiusRatio}, {MaxCornerRadiusRatio}].");
            // 채우기 머티리얼의 셰이더 값은 렌더 쪽 사실이라 여기서 보지 않고 채우기 부품이 구성 때 봅니다(이동 경로와 같은 규칙, 2단계 계획 TP-3 바뀜).
            ValidateMovementPath();
        }

        /// <summary>
        /// 이동 경로 띠·화살촉 값 조건의 원본입니다. GridOutline 모드에서만 검사합니다(칸 간격은 먼저 검사됨, 이동 경로 화살표 설계 MA5).
        /// 머티리얼의 색 덮어쓰기 속성(_BaseColor)은 렌더 쪽 사실이라 여기서 보지 않고 그리기 부품이 필드 구성 때 봅니다(계획 TP-6).
        /// </summary>
        private void ValidateMovementPath()
        {
            if (!float.IsFinite(movementPathThickness) || movementPathThickness <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Movement path thickness must be positive.");
            if (!float.IsFinite(movementPathYOffset) || movementPathYOffset < 0f)
                throw new InvalidOperationException("[StageFieldViewData] Movement path Y offset must be a finite value of 0 or more.");
            // 경로는 깊이를 쓰는 불투명 띠라 칸 채우기·테두리보다 높아야 그것들이 띠·화살촉 위에 덧칠되지 않는다(표시기가 테두리 칸 수만큼 더 올림).
            if (movementPathYOffset < outlineYOffset)
                throw new InvalidOperationException("[StageFieldViewData] Movement path Y offset must not be lower than the outline Y offset.");
            if (movementPathMaterial == null)
                throw new InvalidOperationException("[StageFieldViewData] Movement path material is required.");
            if (!float.IsFinite(movementPathArrowLength) || movementPathArrowLength <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Movement path arrow length must be positive.");
            // 띠 끝은 화살촉 가운데이고 그 자리의 화살촉 너비는 밑변의 절반이라, 밑변이 띠 두께의 두 배 이상이어야 띠가 화살촉 안에 든다.
            if (!float.IsFinite(movementPathArrowWidth) || movementPathArrowWidth < movementPathThickness * 2f)
                throw new InvalidOperationException("[StageFieldViewData] Movement path arrow width must be at least twice the path thickness.");

            float cell = Mathf.Min(outlineCellSpacing.x, outlineCellSpacing.y);
            // 화살촉이 한 칸보다 길면 밑변이 앞 칸의 꺾임 이음을 넘어 띠와 겹친다.
            if (movementPathArrowLength > cell)
                throw new InvalidOperationException("[StageFieldViewData] Movement path arrow length must not exceed a cell.");
            if (!float.IsFinite(movementPathArrowTipOffset) || Mathf.Abs(movementPathArrowTipOffset) > cell * 0.5f)
                throw new InvalidOperationException("[StageFieldViewData] Movement path arrow tip offset must stay within half a cell.");
            // 마지막 걸음의 띠는 앞 칸 가운데에서 (칸 간격 + 꼭지 자리 - 화살촉 길이 / 2)만큼이라 0보다 길어야 한다.
            if (movementPathArrowLength * 0.5f - movementPathArrowTipOffset >= cell)
                throw new InvalidOperationException(
                    "[StageFieldViewData] Movement path arrow is too long for one step: half its length minus the tip offset must be shorter than a cell.");
        }

        /// <summary>격자 데칼 값 조건의 원본입니다. GridOutline 모드에서 격자 데칼을 켤 때만 검사합니다(칸 간격은 먼저 검사됨).</summary>
        private void ValidateGridDecal()
        {
            if (gridDecalMaterial == null)
                throw new InvalidOperationException("[StageFieldViewData] Grid decal requires a grid decal material.");
            if (!float.IsFinite(gridLineWidth) || gridLineWidth <= 0f
                || gridLineWidth >= Mathf.Min(outlineCellSpacing.x, outlineCellSpacing.y))
                throw new InvalidOperationException("[StageFieldViewData] Grid line width must be positive and smaller than the cell spacing.");
            if (!float.IsFinite(gridDecalDepth) || gridDecalDepth <= 0f)
                throw new InvalidOperationException("[StageFieldViewData] Grid decal depth must be positive.");
            if (GridDecalRenderingLayers == 0u)
                throw new InvalidOperationException(
                    $"[StageFieldViewData] Grid decal rendering layer '{gridDecalRenderingLayer}' is not defined in Tags and Layers > Rendering Layers.");
        }
    }

    [CreateAssetMenu(fileName = "NewStageFieldViewData", menuName = "ProjectL/Database/View/StageFieldViewData")]
    internal sealed class StageFieldViewDataSO : DatabaseItem
    {
        [SerializeField] private StageFieldViewData stageFieldViewData = new();

        public override IData GetData() => stageFieldViewData;
    }
}
