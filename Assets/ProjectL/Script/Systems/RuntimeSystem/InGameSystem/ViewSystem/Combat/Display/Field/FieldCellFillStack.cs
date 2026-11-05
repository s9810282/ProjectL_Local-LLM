using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// GridOutline 모드의 칸 채우기를 겹침 깊이별로 그립니다(겹친 강조 안쪽 축소 설계 OI1·OI2, 2단계 4판 RC11·RC13).
    /// 일부 칸이 아래 종류와 겹치면 같은 종류의 모든 칸을 같은 깊이에 놓습니다. 깊이는 겹치는 아래 종류의 최대 깊이 + 1입니다.
    /// 명시적 받침 관계도 같은 계산에 참여합니다. 같은 깊이의 같은 종류 칸 묶음이 덩어리 하나이고,
    /// 깊이 k ≥ 1 덩어리는 바깥 둘레를 변마다 칸 × (1 − <c>overlapInsetScale</c>^k) / 2만큼 안쪽으로 줄인 판입니다(맞닿은 칸 사이는 이어짐).
    /// 겹침/받침 관계가 없는 종류는 깊이 0입니다. 층 표의 받침 층이 있는 층(호버한 유닛의 공격 범위 → 그 유닛의 이동 칸)은
    /// 받침 층이 보이는 동안 받침 층 밖 칸도 받침 층 위 칸과 같은 깊이에 놓아 크기가 같습니다(6판 RC18).
    /// 깊이 0은 View DB cellOutlineStyle, 깊이 1 이상은 overlapCellOutlineStyle을 따릅니다. 칸마다 방식이면 덩어리로 잇지 않고 칸마다 줄인 판이며,
    /// 깊이 0도 밑 층 크기(cellOutlineScale)·반지름으로 깎습니다. 아래 판의 구멍은 다음 깊이의 방식으로 깎습니다.
    /// 깊이별 크기·안쪽 거리·반지름 식은 <see cref="FieldCellShape"/> 하나이고 표시기와 함께 씁니다.
    /// </summary>
    /// <remarks>
    /// 종류 순서(아래부터)는 아군 배치 구역(배치 드래그 중) → 채우는 층(<see cref="FieldHighlightLayers.DrawOrder"/> 낮은 것부터,
    /// <see cref="FieldHighlightLayers.FillsCell"/>) → 호버 칸입니다. 호버 칸 색이 그 칸 바로 아래 채우기 색과 같으면(공격 조준 칸의 영향 칸 등)
    /// 같은 색 판을 한 번 더 넣지 않습니다(계획 §7.6 L6). 이 순서와 깊이 규칙은 이 부품 한 곳에만 있습니다.
    /// 판(<see cref="FieldCellFill"/>)은 종류·깊이마다 하나이고 칸마다 칸 전체 크기 상자를 놓습니다. 모양은 칸 채우기 셰이더가 깎습니다:
    /// 깊이마다 칸 지도(칸 수 크기 텍스처, 값 = 그 칸 그 깊이의 종류 번호 + 1)를 칸을 쌓을 때 채우고 바뀌었을 때만 올리며,
    /// 셰이더가 그 지도로 같은 종류 이웃을 보고 덩어리 모양을 깎고, 실제 바로 위 종류의 구멍 지도/모양으로 비웁니다(빈 깊이를 건너뜀).
    /// 격자 높이(지형이 올린 높이)가 다른 이웃은 높이 지도로 가려 잇지 않습니다. 칸 테두리가 높이별 핸들로 나뉘는 것과 같습니다.
    /// 판과 그 재질 사본(판마다 하나)·칸 지도는 그 종류·깊이에 칸이 처음 쌓일 때 만들어 <see cref="Release"/> 때 지웁니다.
    /// 정렬 순서는 깊이마다 <see cref="FieldSortingOrders.Fill"/>이라 위 채우기가 늘 아래 채우기 뒤에 그려집니다.
    /// 판 모양 식(<see cref="FieldCellShape"/>)은 칸 테두리도 써서, 겹친 칸 테두리가 판 둘레에 그대로 올라갑니다(3판 RC8, 4판 RC12, 5판 RC15).
    /// 판 높이는 칸 테두리 높이(View DB outlineYOffset, 가장 낮은 테두리인 레일과 같음)이고 정렬 순서로 테두리 아래에 보입니다.
    /// 언제 채울지(행동 중)와 높이 기준(지형 높이만, <see cref="FieldGridMapper.CellOverlayToWorld"/>)은 표시기가 정한 그대로 받습니다.
    /// 축소 비율·모서리 반지름 범위의 원본은 View DB입니다(<see cref="StageFieldViewData"/>). 셰이더 값 이름과 머티리얼 검사는 이 부품에 있습니다.
    /// 셰이더는 칸을 오브젝트 좌표(칸 루트 로컬)로 찾으므로, 판은 칸 루트 바로 아래 단위 변환으로 놓입니다(<see cref="FieldCellFill"/>).
    /// 한계(검토 M1, 덩어리 방식만): 깊이 2 이상에서 이어진 두 칸의 바로 아래 깊이 종류가 다르면 위 덩어리가 아래 덩어리 밖으로 나갑니다.
    /// 지금 층 조합에서는 생기지 않아 처음 생길 때 경고 한 번으로 알립니다(<see cref="WarnBrokenNestingOnce"/>).
    /// </remarks>
    internal sealed class FieldCellFillStack
    {
        // 아군 배치 구역은 첫 종류, 호버는 마지막 종류입니다(생성자).
        private const int PlayerZoneKind = 0;
        // 칸 채우기 셰이더(ProjectL/Combat/CellFill)의 값 이름입니다. 구성 때 검사와 사본 값 넣기가 함께 씁니다.
        private const string GridCornerProperty = "_GridCorner";
        private const string CellSizeProperty = "_CellSize";
        private const string GridSizeProperty = "_GridSize";
        private const string RegionMapProperty = "_RegionMap";
        private const string RegionKindProperty = "_RegionKind";
        private const string InsetProperty = "_Inset";
        private const string CornerRadiusProperty = "_CornerRadius";
        private const string HoleMapProperty = "_HoleMap";
        private const string HoleInsetProperty = "_HoleInset";
        private const string HoleRadiusProperty = "_HoleRadius";
        private const string HeightMapProperty = "_HeightMap";
        private const string JoinCellsProperty = "_JoinCells";
        private const string HoleJoinCellsProperty = "_HoleJoinCells";
        private const string UseHoleShapesProperty = "_UseHoleShapes";
        private const string HoleShapesProperty = "_HoleShapes";
        private const string EdgeFadeWidthProperty = "_EdgeFadeWidth";
        private const string CenterOpacityProperty = "_CenterOpacity";
        private const string OpenEdgeProperty = "_OpenEdge";
        private const string ClearAreaProperty = "_ClearArea";
        private static readonly string[] MaterialProperties =
        {
            GridCornerProperty, CellSizeProperty, GridSizeProperty, RegionMapProperty, RegionKindProperty,
            InsetProperty, CornerRadiusProperty, HoleMapProperty, HoleInsetProperty, HoleRadiusProperty, HeightMapProperty,
            JoinCellsProperty, HoleJoinCellsProperty, UseHoleShapesProperty,
            EdgeFadeWidthProperty, CenterOpacityProperty, OpenEdgeProperty, ClearAreaProperty,
        };

        private static readonly IReadOnlyList<Vector2Int> NoCells = Array.Empty<Vector2Int>();
        // 셰이더 SameKindNeighbors의 비트 순서와 같습니다: +X, -X, +Z, -Z, +X+Z, -X+Z, +X-Z, -X-Z.
        private static readonly Vector2Int[] HoleNeighborOffsets =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)
        };

        private readonly Transform _cellsRoot;
        private readonly Material _material;
        private readonly IMeshSystemService _meshes;
        private readonly IColorSystemService _colors;
        private readonly FieldHighlightPalette _palette;
        // 판 모양 식(덩어리/칸마다, 깊이별 안쪽 거리·반지름)입니다. 표시기와 같은 값입니다.
        private readonly FieldCellShape _shape;
        private readonly float _yOffset;
        private readonly Kind[] _kinds;
        // 이번 Apply의 종류별 채우기 색입니다(행동 중일 때만 채움).
        private readonly Color[] _kindColors;
        // 이번 표시의 종류별 통일 깊이(-1 = 없음)와, 위 종류가 겹치는 아래 종류 관계입니다. 기존 칸 상태에서 매번 파생합니다.
        private readonly int[] _kindDepths;
        private readonly bool[,] _underKinds;
        // 구멍 지도의 종류 번호가 참조할 실제 판 모양입니다(x/y = 안쪽 거리, z = 반지름).
        private readonly Vector4[] _holeShapes;
        private readonly List<Vector3> _centers = new();
        // 칸 하나에 쌓인 종류(아래부터)입니다. 칸마다 다시 씁니다.
        private readonly List<int> _cellKinds = new();
        // 깊이별 칸 지도와, 바로 위 실제 종류/그 종류의 이웃 비트를 묶은 구멍 지도입니다. 빈 깊이는 건너뜁니다.
        private readonly List<CellMap<byte>> _maps = new();
        private readonly List<CellMap<float>> _holeMaps = new();
        // 판마다 만든 재질 사본입니다. 판을 거둔 뒤 지웁니다.
        private readonly List<Material> _materials = new();
        // 이번 Apply의 호버 칸 테두리 칸과, 덩어리를 찾을 때 쓰는 줄·방문 표입니다.
        private readonly List<Vector2Int> _hoverOutlineCells = new();
        private readonly Queue<int> _clusterQueue = new();
        // 칸별 격자 높이(지형이 올린 높이) 지도입니다. 높이가 다른 이웃은 덩어리로 잇지 않습니다(칸 테두리가 높이별로 나뉘는 것과 같음).
        private CellMap<float> _heights;
        private bool[] _clusterSeen;
        // 이번 Apply에서 호버 색이 바로 아래 채우기 색과 같아 호버 판을 따로 넣지 않았는지입니다.
        private bool _hoverMerged;
        private bool _warnedNesting;
        private IFieldHighlightStateReader _state;
        private FieldGridMapper _mapper;
        private Vector2 _gridCorner;
        private int _columns;
        private int _rows;

        internal FieldCellFillStack(Transform cellsRoot, StageFieldViewData config, FieldCellShape shape, FieldHighlightPalette palette,
            IMeshSystemService meshes, IColorSystemService colors)
        {
            _cellsRoot = cellsRoot != null ? cellsRoot : throw new ArgumentNullException(nameof(cellsRoot));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _material = config.HighlightFillMaterial != null
                ? config.HighlightFillMaterial
                : throw new InvalidOperationException("[FieldCellFillStack] 칸 채우기 머티리얼이 없습니다.");
            _shape = shape ?? throw new ArgumentNullException(nameof(shape));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _meshes = meshes ?? throw new ArgumentNullException(nameof(meshes));
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
            _yOffset = config.OutlineYOffset;

            var kinds = new List<Kind> { new("FieldFill_PlayerZone", FieldHighlightLayer.None) };
            IReadOnlyList<FieldHighlightLayer> order = FieldHighlightLayers.DrawOrder;
            for (int i = 0; i < order.Count; i++)
            {
                if (FieldHighlightLayers.FillsCell(order[i]))
                    kinds.Add(new Kind($"FieldFill_{order[i]}", order[i]));
            }
            kinds.Add(new Kind("FieldFill_Hover", FieldHighlightLayer.None));
            _kinds = kinds.ToArray();
            _kindColors = new Color[_kinds.Length];
            _kindDepths = new int[_kinds.Length];
            _underKinds = new bool[_kinds.Length, _kinds.Length];
            _holeShapes = new Vector4[_kinds.Length];

            // 칸 지도는 한 칸에 종류 번호 + 1을 1바이트로 담습니다.
            if (_kinds.Length >= byte.MaxValue)
                throw new InvalidOperationException("[FieldCellFillStack] 채우기 종류가 칸 지도 한 바이트에 들어가지 않습니다.");
            // 한 칸에 모든 종류가 겹쳐도 가장 깊은 채우기가 칸 테두리 아래에 그려져야 합니다(정렬 순서 표).
            if (FieldSortingOrders.Fill(_kinds.Length - 1) >= FieldSortingOrders.OutlineBase)
                throw new InvalidOperationException("[FieldCellFillStack] 가장 깊은 채우기 정렬 순서가 칸 테두리에 닿았습니다. 정렬 순서 표를 고쳐야 합니다.");
        }

        // 호버 칸 채우기는 마지막 종류입니다(생성자).
        private int HoverKind => _kinds.Length - 1;

        /// <summary>
        /// 이번 <see cref="Apply"/>에서 호버 칸 테두리를 그릴 깊이입니다. 호버 채우기의 깊이이고, 호버 색이 바로 아래 채우기 색과 같아 합쳐졌으면
        /// 그 칸 맨 위 채우기의 깊이입니다(보이는 맨 위 판, 3판 RC8). 호버 칸이 없거나 행동 중이 아니면 -1입니다.
        /// </summary>
        internal int HoverOutlineDepth { get; private set; } = -1;

        /// <summary>호버가 아래 채움과 합쳐졌으면 그 층입니다. 독립된 호버 표시는 None입니다.</summary>
        internal FieldHighlightLayer HoverOutlineLayer => _hoverMerged && HoverOutlineDepth >= 0
            ? _kinds[_maps[HoverOutlineDepth].Values[_state.HoverIndex] - 1].Layer
            : FieldHighlightLayer.None;

        /// <summary>
        /// 이번 <see cref="Apply"/>에서 호버 칸 테두리로 그릴 칸입니다(깊이 <see cref="HoverOutlineDepth"/>). 보통 호버 칸 하나이고,
        /// 덩어리 방식에서 호버가 아래 판과 합쳐졌고 그 판이 안쪽 판(깊이 1 이상)이면 호버 칸이 든 그 덩어리 칸 전부라 테두리가 판 둘레에 놓입니다(4판 검토 L1).
        /// 칸마다 방식은 판이 칸마다라 늘 호버 칸 하나입니다.
        /// </summary>
        internal IReadOnlyList<Vector2Int> HoverOutlineCells => _hoverOutlineCells;

        /// <summary>같은 머티리얼이면 이 부품을 다시 씁니다. 판 모양 값은 표시기가 모양 값(<see cref="FieldCellShape.Uses"/>)으로 비교합니다.</summary>
        internal bool Uses(StageFieldViewData config) => config.HighlightFillMaterial == _material;

        /// <summary>
        /// 채우기 머티리얼이 칸 채우기 셰이더의 값을 갖는지 확인하고, 그릴 필드(칸 좌표·높이 기준·강조 상태)를 받아 높이 지도를 만듭니다.
        /// 칸 크기는 판 모양 값(<see cref="FieldCellShape.CellSize"/>)입니다. 종류마다 깊이 0 판(재질 사본·칸 지도 포함)은 여기서 미리 만들고
        /// (GC 끊김 설계 GH6 — 강조가 처음 나오는 프레임에 만들지 않게, 칸이 없으면 그리지 않음), 깊이 1 이상은 칸이 처음 쌓일 때 만듭니다.
        /// 다시 구성하려면 먼저 <see cref="Release"/>합니다. 실패하면 원인을 전파하고, 거두기는 표시기가 <see cref="Release"/>로 합니다.
        /// </summary>
        internal void Build(FieldGridMapper mapper, IFieldHighlightStateReader state)
        {
            if (_heights != null)
                throw new InvalidOperationException("[FieldCellFillStack] 이미 구성돼 있습니다. Release 뒤에 다시 구성합니다.");
            // 재질 속성은 렌더 쪽 사실이라 View DB 검사가 아니라 그리는 부품이 구성 때 봅니다(이동 경로 부품과 같은 규칙, 2단계 계획 TP-3).
            for (int i = 0; i < MaterialProperties.Length; i++)
            {
                if (!_material.HasProperty(MaterialProperties[i]))
                    throw new InvalidOperationException(
                        $"[FieldCellFillStack] 칸 채우기 머티리얼 '{_material.name}'에 {MaterialProperties[i]}가 없습니다. ProjectL/Combat/CellFill 셰이더 머티리얼이어야 합니다.");
            }
            float fadeWidth = _material.GetFloat(EdgeFadeWidthProperty);
            float centerOpacity = _material.GetFloat(CenterOpacityProperty);
            if (!float.IsFinite(fadeWidth) || fadeWidth < 0f || fadeWidth > 0.45f
                || !float.IsFinite(centerOpacity) || centerOpacity < 0f || centerOpacity > 1f)
                throw new InvalidOperationException(
                    $"[FieldCellFillStack] 채움 머티리얼 '{_material.name}'의 {EdgeFadeWidthProperty}는 [0, 0.45], {CenterOpacityProperty}는 [0, 1]이어야 합니다: {fadeWidth}, {centerOpacity}.");
            if (!SystemInfo.SupportsTextureFormat(TextureFormat.R8) || !SystemInfo.SupportsTextureFormat(TextureFormat.RFloat))
                throw new InvalidOperationException("[FieldCellFillStack] 이 기기는 칸 지도 텍스처 형식(R8·RFloat)을 지원하지 않습니다.");

            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            // 칸 지도 자리는 강조 상태의 칸 번호(행 × 가로 + 열)이고, 셰이더는 같은 자리를 격자 좌표로 읽습니다.
            if (state.ColumnCount != mapper.ColumnCount || state.CellCount != mapper.ColumnCount * mapper.RowCount)
                throw new InvalidOperationException("[FieldCellFillStack] 강조 상태와 칸 좌표의 격자 크기가 다릅니다.");

            _columns = mapper.ColumnCount;
            _rows = mapper.RowCount;
            Vector2 cellSize = _shape.CellSize;
            Vector3 corner = FieldGridMapper.CornerLocal(_columns, _rows, cellSize.x, cellSize.y);
            _gridCorner = new Vector2(corner.x, corner.z);
            _clusterSeen = new bool[_columns * _rows];
            _heights = new CellMap<float>("FieldFillHeightMap", _columns, _rows, TextureFormat.RFloat);
            // 깊이 0 판은 강조가 나오면 늘 쓰므로 구성 때 만듭니다(GC 끊김 계획 TP-6). 깊이 1 이상은 겹칠 때만 생겨 그대로 둡니다.
            for (int kind = 0; kind < _kinds.Length; kind++)
                PlateOf(kind, 0);
        }

        /// <summary>
        /// 행동 중이면 칸마다 있는 채우기 종류를 아래부터 깊이에 놓아 칠합니다. 행동 중이 아니면 평소에도 채우는 층(<see cref="FieldHighlightLayers.IdleFillLayers"/>)만 칠합니다.
        /// 판마다 칸 목록·색이 지난번과 같으면 메시를 다시 만들지 않고, 칸 지도는 바뀐 깊이만 올립니다.
        /// 칸 테두리가 이 결과를 묻으므로 테두리보다 먼저 부릅니다(3판 RC8).
        /// </summary>
        internal void Apply(bool acting)
        {
            for (int k = 0; k < _kinds.Length; k++)
            {
                List<Plate> depths = _kinds[k].Depths;
                for (int d = 0; d < depths.Count; d++)
                    depths[d]?.Cells.Clear();
            }
            for (int d = 0; d < _maps.Count; d++)
                Array.Clear(_maps[d].Values, 0, _maps[d].Values.Length);
            for (int d = 0; d < _holeMaps.Count; d++)
                Array.Clear(_holeMaps[d].Values, 0, _holeMaps[d].Values.Length);
            for (int k = 0; k < _kindDepths.Length; k++)
                _kindDepths[k] = -1;
            HoverOutlineDepth = -1;
            _hoverMerged = false;
            _hoverOutlineCells.Clear();

            FieldHighlightLayer paintedLayers = FieldHighlightLayer.None;
            for (int index = 0; index < _state.CellCount; index++)
                paintedLayers |= _state.LayersAt(index);
            bool paints = acting || (paintedLayers & FieldHighlightLayers.IdleFillLayers) != 0;

            if (paints)
            {
                // 호버 깊이를 넣을지 색으로 정하므로 칸을 쌓기 전에 색을 정합니다.
                for (int k = 0; k < _kinds.Length; k++)
                    _kindColors[k] = ColorOf(k);
                ResolveKindDepths(acting);
                int columns = _state.ColumnCount;
                for (int index = 0; index < _state.CellCount; index++)
                {
                    var cell = new Vector2Int(index % columns, index / columns);
                    _heights.Values[index] = _mapper.Surface.OverlayHeightAt(new CellCoord(cell.x, cell.y));
                    Stack(index, cell, acting);
                }

                if (acting)
                {
                    // 덩어리는 모든 칸을 쌓은 뒤에야 압니다.
                    CollectHoverOutline();
                    WarnBrokenNestingOnce();
                }
            }

            RefreshHoleMaps();
            _heights.Upload();
            for (int d = 0; d < _maps.Count; d++)
                _maps[d].Upload();
            for (int d = 0; d < _holeMaps.Count; d++)
                _holeMaps[d].Upload();
            bool holeShapesChanged = RefreshHoleShapes();

            for (int k = 0; k < _kinds.Length; k++)
            {
                Color color = paints ? _kindColors[k] : default;
                GridOutlineOpening opening = paints
                    ? k == HoverKind ? FieldAttackOutlineOpening.ForHover(_state, FieldHighlightLayer.None, _shape.CellSize)
                        : FieldAttackOutlineOpening.Of(_state, _kinds[k].Layer, _shape.CellSize)
                    : default;
                List<Plate> depths = _kinds[k].Depths;
                for (int d = 0; d < depths.Count; d++)
                {
                    if (depths[d] != null)
                        ApplyPlate(depths[d], color, opening, holeShapesChanged);
                }
            }
        }

        /// <summary>
        /// 채우는 층이 지금까지 쓴 깊이 수입니다(깊이 0 포함, 판이 아직 없는 깊이도 셈). 그 층을 채우지 않으면 0입니다.
        /// 칸 테두리가 겹친 칸을 안쪽 판 둘레에 그릴 때 <see cref="CellsOf"/>와 함께 씁니다(<see cref="Apply"/> 뒤).
        /// </summary>
        internal int DepthCountOf(FieldHighlightLayer layer)
        {
            int kind = KindOf(layer);
            return kind < 0 ? 0 : _kinds[kind].Depths.Count;
        }

        /// <summary>이번 <see cref="Apply"/>에서 채우는 층이 그 깊이에 놓인 칸입니다(그 깊이 덩어리들의 칸 전부, 판이 없으면 빈 목록).</summary>
        internal IReadOnlyList<Vector2Int> CellsOf(FieldHighlightLayer layer, int depth)
        {
            int kind = KindOf(layer);
            if (kind < 0)
                throw new ArgumentOutOfRangeException(nameof(layer), layer, "[FieldCellFillStack] 채우지 않는 층입니다.");
            Plate plate = _kinds[kind].Depths[depth];
            return plate != null ? plate.Cells : NoCells;
        }

        /// <summary>그린 값 기억을 모두 지워 다음 <see cref="Apply"/>가 모든 채우기를 다시 쓰게 합니다(높이 표가 바뀔 때).</summary>
        internal void Forget()
        {
            for (int k = 0; k < _kinds.Length; k++)
            {
                List<Plate> depths = _kinds[k].Depths;
                for (int d = 0; d < depths.Count; d++)
                    depths[d]?.Drawn.Forget();
            }
        }

        /// <summary>
        /// 모든 판을 거두고, 그 뒤 재질 사본과 칸 지도·높이 지도 텍스처를 지웁니다. 하나가 실패해도 다음을 계속하고 실패는 모읍니다.
        /// 다음 구성에서 칸이 쌓이면 다시 만듭니다.
        /// </summary>
        internal void Release(ref List<Exception> errors)
        {
            for (int k = 0; k < _kinds.Length; k++)
            {
                List<Plate> depths = _kinds[k].Depths;
                for (int d = 0; d < depths.Count; d++)
                {
                    Plate plate = depths[d];
                    if (plate == null)
                        continue;
                    plate.Drawn.Forget();
                    try
                    {
                        plate.Fill.Release();
                    }
                    catch (Exception exception)
                    {
                        (errors ??= new List<Exception>()).Add(exception);
                    }
                }
                depths.Clear();
            }

            // 사본·텍스처를 쓰던 렌더러를 먼저 거둔 뒤 지웁니다.
            for (int i = 0; i < _materials.Count; i++)
                DestroySafely(_materials[i], ref errors);
            _materials.Clear();
            for (int d = 0; d < _maps.Count; d++)
                DestroySafely(_maps[d].Texture, ref errors);
            _maps.Clear();
            for (int d = 0; d < _holeMaps.Count; d++)
                DestroySafely(_holeMaps[d].Texture, ref errors);
            _holeMaps.Clear();
            if (_heights != null)
                DestroySafely(_heights.Texture, ref errors);
            _heights = null;
            _clusterSeen = null;

            HoverOutlineDepth = -1;
            _hoverMerged = false;
            _hoverOutlineCells.Clear();
            _state = null;
            _mapper = null;
        }

        /// <summary>
        /// 실제 겹치는 종류와 명시적 받침 관계를 모아, 아래 종류부터 전체 칸에 쓸 깊이를 정합니다.
        /// 위 종류는 겹치는 아래 종류의 최대 깊이보다 하나 높습니다. 칸별 계산을 반복해 수렴시키지 않습니다.
        /// </summary>
        private void ResolveKindDepths(bool acting)
        {
            Array.Clear(_underKinds, 0, _underKinds.Length);
            for (int index = 0; index < _state.CellCount; index++)
            {
                CollectKinds(index, acting);
                for (int i = 0; i < _cellKinds.Count; i++)
                {
                    int kind = _cellKinds[i];
                    _kindDepths[kind] = 0;
                    for (int j = 0; j < i; j++)
                        _underKinds[kind, _cellKinds[j]] = true;
                }
            }

            // 종류는 기존 층 표의 아래부터 순서입니다. 받침 층도 항상 먼저 나오도록 표가 검사합니다.
            for (int kind = PlayerZoneKind; kind < HoverKind; kind++)
            {
                if (_kindDepths[kind] < 0)
                    continue;
                int under = KindOf(_kinds[kind].StacksOn);
                if (under >= 0 && _kindDepths[under] >= 0)
                    _underKinds[kind, under] = true;
                for (int lower = PlayerZoneKind; lower < kind; lower++)
                {
                    if (_underKinds[kind, lower])
                        _kindDepths[kind] = Math.Max(_kindDepths[kind], _kindDepths[lower] + 1);
                }
            }
        }

        /// <summary>이번 칸에 실제로 칠하는 종류를 기존 그리기 순서로 모읍니다. 깊이 계산과 실제 쌓기가 같은 필터를 씁니다.</summary>
        private void CollectKinds(int index, bool acting)
        {
            _cellKinds.Clear();
            if (_state.IsPlacementActive && _state.ZoneAt(index) == FieldStaticZone.PlayerZone)
                _cellKinds.Add(PlayerZoneKind);

            FieldHighlightLayer layers = _state.LayersAt(index);
            for (int k = PlayerZoneKind + 1; k < HoverKind; k++)
            {
                if ((layers & _kinds[k].Layer) == 0)
                    continue;
                // 행동 중이 아니면 평소에도 채우는 층만 쌓습니다(낙석 View 계획 RP-17).
                if (!acting && (_kinds[k].Layer & FieldHighlightLayers.IdleFillLayers) == 0)
                    continue;
                _cellKinds.Add(k);
            }
        }

        /// <summary>각 종류를 통일된 깊이에 놓고 칸 지도를 채웁니다. 호버는 이 칸 맨 위 판의 깊이를 따릅니다.</summary>
        private void Stack(int index, Vector2Int cell, bool acting)
        {
            CollectKinds(index, acting);
            if (acting && index == _state.HoverIndex)
            {
                int top = _cellKinds.Count - 1;
                _hoverMerged = top >= 0 && _kindColors[_cellKinds[top]] == _kindColors[HoverKind];
                int depth = top < 0 ? 0 : _kindDepths[_cellKinds[top]];
                if (!_hoverMerged)
                {
                    if (top >= 0)
                        depth++;
                    _kindDepths[HoverKind] = depth;
                    _cellKinds.Add(HoverKind);
                }
                HoverOutlineDepth = depth;
            }

            for (int i = 0; i < _cellKinds.Count; i++)
            {
                int kind = _cellKinds[i];
                int depth = _kindDepths[kind];
                PlateOf(kind, depth).Cells.Add(cell);
                _maps[depth].Values[index] = (byte)(kind + 1);
            }
        }

        /// <summary>
        /// 숫자상 다음 깊이가 아니라 실제 바로 위 종류를 찾습니다. 아래 판이 없는 이웃에서도 위 판의 실제 연결 상태를 보존합니다.
        /// 종류 번호(하위 8비트)와 위 판 자체의 같은 높이 이웃 비트(상위 8비트)를 RFloat에 정수로 담습니다(최대 65535, 정확히 표현됨).
        /// </summary>
        private void RefreshHoleMaps()
        {
            for (int index = 0; index < _state.CellCount; index++)
            {
                int upperKind = 0;
                int upperNeighbors = 0;
                for (int depth = _maps.Count - 1; depth >= 0; depth--)
                {
                    if (depth < _holeMaps.Count)
                        _holeMaps[depth].Values[index] = upperKind + (upperNeighbors << 8);
                    int kind = _maps[depth].Values[index];
                    if (kind == 0)
                        continue;
                    upperKind = kind;
                    upperNeighbors = HoleNeighborsAt(index, kind, depth);
                }
            }
        }

        // 구멍 선택 지도 대신 위 판 자체의 칸 지도에서 이웃을 봅니다. 옆 칸에 다른 판이 더 있어도 위 판 모양은 바뀌지 않습니다.
        private int HoleNeighborsAt(int index, int kind, int depth)
        {
            if (_shape.EachCellAt(depth))
                return 0;
            int x = index % _columns;
            int y = index / _columns;
            int neighbors = 0;
            byte[] map = _maps[depth].Values;
            for (int i = 0; i < HoleNeighborOffsets.Length; i++)
            {
                Vector2Int offset = HoleNeighborOffsets[i];
                int nextX = x + offset.x;
                int nextY = y + offset.y;
                if (nextX < 0 || nextY < 0 || nextX >= _columns || nextY >= _rows)
                    continue;
                int next = nextY * _columns + nextX;
                if (map[next] == kind && Mathf.Abs(_heights.Values[next] - _heights.Values[index]) < 0.0001f)
                    neighbors |= 1 << i;
            }
            return neighbors;
        }

        // 구멍 지도 종류가 가리키는 실제 모양입니다. 깊이가 바뀌면 기존 판 재질도 새 모양을 받습니다.
        private bool RefreshHoleShapes()
        {
            bool changed = false;
            for (int kind = 0; kind < _kinds.Length; kind++)
            {
                int depth = _kindDepths[kind];
                Vector2 inset = depth < 0 ? Vector2.zero : _shape.InsetOf(depth);
                Vector4 shape = depth < 0 ? default : new Vector4(inset.x, inset.y, _shape.PlateRadiusOf(depth), 0f);
                if (_holeShapes[kind].Equals(shape))
                    continue;
                _holeShapes[kind] = shape;
                changed = true;
            }
            // 일부 재질 갱신이 실패해도 다음 Apply에서 그 재질부터 다시 시도합니다.
            if (changed)
            {
                for (int kind = 0; kind < _kinds.Length; kind++)
                {
                    List<Plate> depths = _kinds[kind].Depths;
                    for (int depth = 0; depth < depths.Count; depth++)
                    {
                        if (depths[depth] != null)
                            depths[depth].HasHoleShapes = false;
                    }
                }
            }
            return changed;
        }

        /// <summary>
        /// 호버 칸 테두리 칸을 모읍니다. 칸을 다 쌓은 뒤(지도가 채워진 뒤) 부릅니다. 덩어리 방식에서 호버가 아래 판과 합쳐졌고 그 판이 안쪽 판이면,
        /// 셰이더가 잇는 규칙(같은 깊이·같은 종류·같은 격자 높이, 상하좌우로 맞닿음)대로 호버 칸이 든 덩어리를 찾습니다. 칸마다 방식은 호버 칸 하나입니다.
        /// </summary>
        private void CollectHoverOutline()
        {
            if (HoverOutlineDepth < 0)
                return;

            int hover = _state.HoverIndex;
            if (!_hoverMerged || HoverOutlineDepth < 1 || _shape.EachCellAt(HoverOutlineDepth))
            {
                _hoverOutlineCells.Add(new Vector2Int(hover % _columns, hover / _columns));
                return;
            }

            byte[] map = _maps[HoverOutlineDepth].Values;
            Array.Clear(_clusterSeen, 0, _clusterSeen.Length);
            _clusterQueue.Clear();
            _clusterSeen[hover] = true;
            _clusterQueue.Enqueue(hover);
            while (_clusterQueue.Count > 0)
            {
                int index = _clusterQueue.Dequeue();
                int column = index % _columns;
                _hoverOutlineCells.Add(new Vector2Int(column, index / _columns));
                if (column > 0)
                    VisitCluster(index, index - 1, map);
                if (column + 1 < _columns)
                    VisitCluster(index, index + 1, map);
                if (index >= _columns)
                    VisitCluster(index, index - _columns, map);
                if (index + _columns < map.Length)
                    VisitCluster(index, index + _columns, map);
            }
        }

        // 맞닿은 칸이 같은 덩어리면 줄에 넣습니다.
        private void VisitCluster(int from, int to, byte[] map)
        {
            if (_clusterSeen[to] || map[to] != map[from] || _heights.Values[to] != _heights.Values[from])
                return;
            _clusterSeen[to] = true;
            _clusterQueue.Enqueue(to);
        }

        /// <summary>
        /// 4판 한계(검토 M1)를 처음 만나면 한 번 경고합니다. 깊이 k ≥ 2에서 이어진 두 칸의 실제 바로 아래 종류가 다르면 위 덩어리가
        /// 아래 덩어리 밖으로 나가거나 아래 판 테두리가 위 판을 가로지를 수 있습니다. 빈 깊이는 건너 실제 받침을 비교합니다.
        /// 칸마다 방식은 덩어리가 없어 보지 않습니다.
        /// </summary>
        private void WarnBrokenNestingOnce()
        {
            if (_warnedNesting)
                return;

            for (int depth = 2; depth < _maps.Count; depth++)
            {
                if (_shape.EachCellAt(depth))
                    continue;
                byte[] upper = _maps[depth].Values;
                for (int index = 0; index < upper.Length; index++)
                {
                    if (upper[index] == 0)
                        continue;
                    int right = index % _columns + 1 < _columns ? index + 1 : -1;
                    int up = index + _columns < upper.Length ? index + _columns : -1;
                    if (!BreaksNesting(index, right, upper, depth) && !BreaksNesting(index, up, upper, depth))
                        continue;

                    _warnedNesting = true;
                    Debug.LogWarning(
                        $"[FieldCellFillStack] 겹침 깊이 {depth}에서 이어진 칸 ({index % _columns}, {index / _columns}) 옆의 바로 아래 깊이 종류가 달라 " +
                        "위 판이 아래 판 밖으로 나갑니다(겹친 강조 2단계 계획 §7.6 M1). 이 층 조합은 4판 규칙의 한계입니다.");
                    return;
                }
            }
        }

        // 두 칸이 같은 종류·같은 높이로 이어지는데 실제 바로 아래 종류가 다른지입니다. b가 -1이면 판 밖입니다.
        private bool BreaksNesting(int a, int b, byte[] upper, int depth) =>
            b >= 0 && upper[a] == upper[b] && KindBelow(a, depth) != KindBelow(b, depth) && _heights.Values[a] == _heights.Values[b];

        private byte KindBelow(int index, int depth)
        {
            for (int below = depth - 1; below >= 0; below--)
            {
                byte kind = _maps[below].Values[index];
                if (kind != 0)
                    return kind;
            }
            return 0;
        }

        /// <summary>
        /// 종류의 그 깊이 판입니다. 처음 쓰는 깊이면 재질 사본을 만들고 판을 구성합니다(아래 깊이 판은 만들지 않음, 4판 검토 L5).
        /// 구성에 성공한 뒤에 목록에 넣고, 실패하면 그 사본을 바로 지워 거둘 것이 남지 않습니다.
        /// </summary>
        private Plate PlateOf(int kind, int depth)
        {
            List<Plate> depths = _kinds[kind].Depths;
            while (depths.Count <= depth)
                depths.Add(null);
            if (depths[depth] != null)
                return depths[depth];

            string name = depth == 0 ? _kinds[kind].Name : $"{_kinds[kind].Name}_{depth}";
            Material material = ShapedCopy(kind, depth);
            var fill = new FieldCellFill(_cellsRoot, name, material, _meshes, _colors, FieldSortingOrders.Fill(depth));
            try
            {
                fill.Build();
            }
            catch
            {
                // 채우기 객체는 Build가 스스로 거둡니다. 다음 Apply가 다시 만듭니다.
                _materials.Remove(material);
                DestroyNow(material);
                throw;
            }

            depths[depth] = new Plate(fill, material);
            return depths[depth];
        }

        // 그 깊이의 칸 지도입니다. 처음 쓰면 만듭니다.
        private CellMap<byte> MapOf(int depth)
        {
            while (_maps.Count <= depth)
                _maps.Add(new CellMap<byte>($"FieldFillKindMap_{_maps.Count}", _columns, _rows, TextureFormat.R8));
            return _maps[depth];
        }

        // 이 깊이 판의 칸마다 실제 바로 위 종류를 담습니다. 다음 숫자 깊이가 비어 있어도 위 판을 찾습니다.
        private CellMap<float> HoleMapOf(int depth)
        {
            while (_holeMaps.Count <= depth)
                _holeMaps.Add(new CellMap<float>($"FieldFillHoleMap_{_holeMaps.Count}", _columns, _rows, TextureFormat.RFloat));
            return _holeMaps[depth];
        }

        /// <summary>
        /// 판 하나의 재질 사본에 격자·높이 지도, 그 깊이 칸 지도·종류 번호·판 모양, 실제 바로 위 종류 지도(구멍), 이웃을 이을지(방식)를 한 번 넣습니다.
        /// 모양 값은 <see cref="FieldCellShape"/>의 판 식입니다(덩어리 방식의 깊이 0은 안쪽 거리·반지름 0 = 칸 전체).
        /// 지도 텍스처는 판보다 오래 살고 값만 바뀌므로 다시 넣지 않습니다. 구멍 모양 배열은 ApplyPlate에서 갱신합니다.
        /// 구성 끝(깊이 0 미리 만들기)이나 Apply 안에서만 부릅니다.
        /// </summary>
        private Material ShapedCopy(int kind, int depth)
        {
            CellMap<byte> region = MapOf(depth);
            CellMap<float> hole = HoleMapOf(depth);
            var material = new Material(_material) { name = $"{_material.name}_{_kinds[kind].Name}_{depth}", hideFlags = HideFlags.DontSave };
            _materials.Add(material);
            material.SetVector(GridCornerProperty, _gridCorner);
            material.SetVector(CellSizeProperty, _shape.CellSize);
            material.SetVector(GridSizeProperty, new Vector2(_columns, _rows));
            material.SetTexture(HeightMapProperty, _heights.Texture);
            material.SetTexture(RegionMapProperty, region.Texture);
            material.SetFloat(RegionKindProperty, kind + 1);
            material.SetVector(InsetProperty, _shape.InsetOf(depth));
            material.SetFloat(CornerRadiusProperty, _shape.PlateRadiusOf(depth));
            material.SetTexture(HoleMapProperty, hole.Texture);
            material.SetVector(HoleInsetProperty, _shape.InsetOf(depth + 1));
            material.SetFloat(HoleRadiusProperty, _shape.PlateRadiusOf(depth + 1));
            material.SetFloat(JoinCellsProperty, _shape.EachCellAt(depth) ? 0f : 1f);
            material.SetFloat(HoleJoinCellsProperty, _shape.EachCellAt(depth + 1) ? 0f : 1f);
            material.SetFloat(UseHoleShapesProperty, 1f);
            SetOpening(material, default);
            return material;
        }

        /// <summary>Outline과 같은 열린 변을 채움의 투명도 전이에 전달합니다. 기본값은 네 변입니다.</summary>
        internal static void SetOpening(Material material, GridOutlineOpening opening)
        {
            Vector2 direction = opening.Side switch
            {
                GridOutlineSide.Left => Vector2.left,
                GridOutlineSide.Right => Vector2.right,
                GridOutlineSide.Down => Vector2.down,
                GridOutlineSide.Up => Vector2.up,
                _ => Vector2.zero
            };
            material.SetVector(OpenEdgeProperty, new Vector4(direction.x, direction.y, opening.GridLine, 0f));
            RectInt area = opening.ClearArea;
            material.SetVector(ClearAreaProperty, new Vector4(area.xMin, area.yMin, area.xMax, area.yMax));
        }

        private static void DestroySafely(UnityEngine.Object target, ref List<Exception> errors)
        {
            try
            {
                DestroyNow(target);
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }
        }

        private static void DestroyNow(UnityEngine.Object target)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(target);
            else
                UnityEngine.Object.DestroyImmediate(target);
        }

        // 채우는 층의 종류 번호입니다. 채우지 않는 층이면 -1입니다.
        private int KindOf(FieldHighlightLayer layer)
        {
            for (int k = PlayerZoneKind + 1; k < HoverKind; k++)
            {
                if (_kinds[k].Layer == layer)
                    return k;
            }
            return -1;
        }

        /// <summary>
        /// 채우기 색입니다. 층은 <see cref="FieldHighlightLayers.FillColorOf"/>, 배치 구역은 구역 색, 호버 칸은 호버 상태·종류 색을 쓰고,
        /// 알파는 강조 색 알파 × 공용 채우기 알파입니다(<see cref="FieldHighlightPalette.ToFill"/>).
        /// </summary>
        private Color ColorOf(int kind)
        {
            Color color;
            if (kind == PlayerZoneKind)
                color = _palette.PlayerZone;
            else if (kind == HoverKind)
                _palette.TryGet(FieldHighlightLayers.HoverColor(_state.HoverState, _state.HoverKind), FieldStaticZone.None, out color);
            else
                _palette.TryGet(FieldHighlightLayers.FillColorOf(_kinds[kind].Layer, _state.HoverState), FieldStaticZone.None, out color);

            return _palette.ToFill(color);
        }

        /// <summary>
        /// 판 하나의 칸들을 칸 전체 크기로 그립니다(모양은 셰이더가 깎음). 칸 목록·색이 지난번과 같으면 메시를 다시 만들지 않고,
        /// 그린 뒤에만 기억해 실패한 갱신을 다음 <see cref="Apply"/>가 다시 시도합니다. 판 높이는 지형이 올린 높이만 따릅니다(타일 높이 설계 TH4 개정 2).
        /// </summary>
        private void ApplyPlate(Plate plate, Color color, GridOutlineOpening opening, bool holeShapesChanged)
        {
            if (holeShapesChanged || !plate.HasHoleShapes)
            {
                plate.Material.SetVectorArray(HoleShapesProperty, _holeShapes);
                plate.HasHoleShapes = true;
            }
            if (!plate.Opening.Equals(opening))
            {
                SetOpening(plate.Material, opening);
                plate.Opening = opening;
            }
            if (plate.Drawn.SameAs(plate.Cells, color))
                return;

            _centers.Clear();
            for (int i = 0; i < plate.Cells.Count; i++)
            {
                var cell = new CellCoord(plate.Cells[i].x, plate.Cells[i].y);
                Vector3 world = _mapper.CellOverlayToWorld(cell);
                _centers.Add(_cellsRoot.InverseTransformPoint(world) + (Vector3.up * _yOffset));
            }

            plate.Fill.Update(_centers, _shape.CellSize, color);
            plate.Drawn.Remember(plate.Cells, color);
        }

        /// <summary>채우기 종류 하나와 깊이별 판입니다. [d]가 깊이 d이고, 칸이 쌓인 적 없는 깊이는 null입니다.</summary>
        private sealed class Kind
        {
            internal Kind(string name, FieldHighlightLayer layer)
            {
                Name = name;
                Layer = layer;
                StacksOn = FieldHighlightLayers.StacksOn(layer);
            }

            internal string Name { get; }
            /// <summary>채우는 층입니다. 배치 구역·호버 칸은 None입니다.</summary>
            internal FieldHighlightLayer Layer { get; }
            /// <summary>층 표의 받침 층입니다(<see cref="FieldHighlightLayers.StacksOn"/>). 없거나 배치 구역·호버 칸이면 None입니다.</summary>
            internal FieldHighlightLayer StacksOn { get; }
            internal List<Plate> Depths { get; } = new();
        }

        /// <summary>채우기 객체 하나, 이번에 칠할 칸(그 종류 그 깊이 덩어리들의 칸 전부), 마지막으로 그린 값입니다.</summary>
        private sealed class Plate
        {
            internal Plate(FieldCellFill fill, Material material)
            {
                Fill = fill;
                Material = material;
            }

            internal FieldCellFill Fill { get; }
            internal Material Material { get; }
            internal bool HasHoleShapes { get; set; }
            internal GridOutlineOpening Opening { get; set; }
            internal List<Vector2Int> Cells { get; } = new();
            internal FieldDrawnCells Drawn { get; } = new();
        }

        /// <summary>
        /// 칸마다 값 하나인 지도와 그것을 올린 텍스처입니다. 값은 칸 번호(행 × 가로 + 열) 자리에 있고 텍스처 화소 (열, 행)과 같은 자리입니다.
        /// 깊이별 종류 지도(그 칸 그 깊이의 종류 번호 + 1, 비면 0)와 격자 높이 지도가 씁니다. 텍스처는 올린 값과 다를 때만 다시 올립니다.
        /// </summary>
        private sealed class CellMap<T> where T : struct, IEquatable<T>
        {
            private readonly T[] _uploaded;

            internal CellMap(string name, int columns, int rows, TextureFormat format)
            {
                Values = new T[columns * rows];
                _uploaded = new T[columns * rows];
                Texture = new Texture2D(columns, rows, format, false, true)
                {
                    name = name,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.DontSave,
                };
                Texture.SetPixelData(_uploaded, 0);
                Texture.Apply(false, false);
            }

            /// <summary>이번 <see cref="FieldCellFillStack.Apply"/>에서 채운 값입니다.</summary>
            internal T[] Values { get; }
            internal Texture2D Texture { get; }

            internal void Upload()
            {
                bool same = true;
                for (int i = 0; i < Values.Length; i++)
                {
                    if (!Values[i].Equals(_uploaded[i]))
                    {
                        same = false;
                        break;
                    }
                }
                if (same)
                    return;

                Array.Copy(Values, _uploaded, Values.Length);
                Texture.SetPixelData(_uploaded, 0);
                Texture.Apply(false, false);
            }
        }
    }
}
