using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// GridOutline 모드 표시기입니다. 이동 경로는 <see cref="FieldMovementPathLine"/>(띠·직각 꺾임·끝 화살촉)으로, 다른 강조는 AreaOutline 테두리로 그립니다.
    /// 격자 데칼은 구성된 동안 늘 켭니다(2026-10-20 사용자 결정, 격자 아웃라인 설계 U4 바뀜). View DB의 showGridDecal이 꺼져 있으면 켜지 않습니다(2026-10-28).
    /// 평소에는 레일과 평소 층(<see cref="FieldHighlightLayers.IdleLayers"/>) 테두리, 행동 중(<see cref="FieldHighlightState.IsActing"/>: 행동 중 층·
    /// 배치 드래그·Q 경로 지정)에는 모든 층·호버, 배치 드래그 중이면 아군 배치 구역 테두리도 그립니다. 칸 간격은 View DB의 outlineCellSpacing입니다.
    /// View DB에 highlightFillMaterial이 있으면 행동 중에 배치 구역·채우는 층(<see cref="FieldHighlightLayers.FillsCell"/>)·호버 칸을 반투명하게 채우고
    /// 테두리는 그대로 둡니다. 한 칸에 채우기가 겹치면 위 채우기를 한 단계씩 작게, 모서리 둥근 판으로 안쪽에 그립니다(<see cref="FieldCellFillStack"/>, 겹친 강조 안쪽 축소 설계 OI1·2단계 설계 RC1).
    /// 겹친 칸(채우기 깊이 1 이상)은 그 층·호버 테두리 덩어리에서 빼고, 깊이마다 핸들 하나로 그 깊이 칸을 묶은 덩어리를 판과 같은 거리만큼
    /// 안쪽으로 줄여(맞닿은 칸 사이는 그리지 않음) 테두리가 판 둘레에 올라가게 합니다(2단계 3판 RC8, 4판 RC12).
    /// 덩어리 테두리의 볼록 모서리는 View DB outlineCornerRadius(칸 짧은 반변 대비 비율)로 둥글게 하고 오목 모서리는 직각입니다(3판 RC7, 4판 Q-RC13).
    /// View DB의 기본/겹침 방식(cellOutlineStyle·overlapCellOutlineStyle)을 깊이에 맞춰 씁니다. EachCell이면 칸마다 네 변을 모두 그립니다: 칸 크기가 1이고
    /// 모서리가 직각이면 맞닿은 변은 한 번만, 아니면 칸마다 줄이고 둥글게 한 고리를 따로 그리며, 겹친 칸도 칸마다 따로입니다. 고리 모양 식은 <see cref="FieldCellShape"/> 하나입니다.
    /// </summary>
    /// <remarks>
    /// 층마다 전투 동안 핸들 하나를 유지하고 숨길 때는 빈 칸 목록으로 갱신합니다. 칸 목록·색이 같으면 다시 쓰지 않습니다.
    /// 격자 높이(지형이 올린 높이, 고지대)가 있으면 층의 칸을 높이별로 나눠, 바닥(0)이 아닌 칸은 그 높이가 처음 생길 때 만든 핸들로 그 윗면 위에 그립니다.
    /// 테두리·채우기·경로선·호버·데칼 모두 격자 높이를 쓰고 바닥 타일 높이는 보지 않습니다(타일 높이 설계 TH4 개정 2).
    /// 이동 경로는 높이가 같은 연속 구간마다 띠 하나입니다(고지대 View 계획 P12). 무엇을·어느 높이에 그릴지는 여기서 정하고, 띠·이음·화살촉 모양은 부품이 정합니다(이동 경로 화살표 설계 MA1·P1).
    /// 높은 층이 위에 보이도록 테두리 칸마다 <see cref="FieldHighlightLayers.DrawOrder"/> 순서대로 정렬 순서(<see cref="FieldSortingOrders.Outline"/>)를 줍니다.
    /// 테두리 셰이더는 깊이를 쓰지 않아 높이로는 위아래가 정해지지 않습니다. 높이를 조금씩 올리는 것은 같은 자리 Z-fighting을 피하려는 것입니다.
    /// 이동 경로 띠·화살촉은 불투명 머티리얼이라 정렬 순서를 주지 않고, 모든 테두리 칸보다 한 단계 위 높이에 놓아 반투명 채우기·테두리가
    /// 그 위에 덧칠되지 않게 합니다(설계 P5, 겹친 강조 2단계 계획 §7.6). 그래서 경로 높이 값은 칸 테두리 높이 이상이어야 합니다(View DB 검사).
    /// 칸 테두리·채우기는 View DB의 테두리 두께·높이를, 이동 경로는 경로 값(두께·높이·머티리얼·화살촉)을 씁니다(2026-10-29 값 나눔). 칸 간격만 함께 씁니다.
    /// 테두리 두께·높이는 이 렌더러를 만들 때 한 번 읽습니다(<see cref="CanReuse"/>가 비교하지 않음, 2단계 계획 TP-14).
    /// 고리 모양 값(방식·크기 비율·모서리 비율)도 만들 때 한 번 읽고, 다르면 <see cref="CanReuse"/>가 거짓입니다(5판 계획 TP-27).
    /// 점선은 View DB의 점선 길이로 나뉘어 테두리 크기와 상관없이 같은 길이입니다.
    /// AreaOutline과 격자 데칼은 월드 XZ 기준이라 필드 루트가 회전돼 있으면 구성(<see cref="Build"/>) 때 예외로 막습니다(F11, 평가 C10-08 1단계).
    /// 옛 동작(경고 한 번 뒤 아무것도 그리지 않음)은 강조가 조용히 사라져 걷었습니다.
    /// </remarks>
    internal sealed class FieldOutlineRenderer : IFieldHighlightRenderer
    {
        // 층 사이 높이 차이입니다. 겹친 테두리의 Z-fighting만 피하면 됩니다.
        private const float LayerHeightStep = 0.002f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 강조 테두리 메시 GC 계측입니다(성능 계획 Q-FP3a 1단계). 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        // 핸들 Update 둘레의 마커는 핸들마다 처음 쓸 때 만듭니다(Slot.UpdateMarker). 동작은 바꾸지 않습니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileRefresh = new("Combat.View.Field.Outline.Refresh");
        private static readonly Unity.Profiling.ProfilerMarker _profileFillApply = new("Combat.View.Field.Fill.Apply");
        private static readonly Unity.Profiling.ProfilerMarker _profilePathApply = new("Combat.View.Field.Path.Apply");
#endif

        private readonly Transform _cellsRoot;
        private readonly IAreaOutlineSystemService _outlines;
        private readonly FieldMovementPathLine _path;
        private readonly FieldHighlightPalette _palette;
        private readonly FieldGridDecal _gridDecal;
        // 거짓이면 격자 데칼을 켜지 않으며, 그때 데칼 값(머티리얼 등)은 비어 있을 수 있습니다.
        private readonly bool _showGridDecal;
        private readonly Material _gridDecalMaterial;
        private readonly Vector2 _cellSpacing;
        // 설정에서 만든 칸 간격입니다. 만드는 규칙은 FieldCellSpacing.FromGridOutline 한 곳입니다.
        private readonly FieldCellSpacing _configuredSpacing;
        private readonly float _gridLineWidth;
        private readonly float _gridDecalDepth;
        private readonly uint _gridDecalRenderingLayers;
        // 칸 테두리·채우기 값입니다.
        private readonly float _thickness;
        private readonly float _yOffset;
        // 판·고리 모양 식(덩어리/칸마다, 안쪽 거리, 모서리 반지름)입니다. 칸 채우기 부품도 같은 값을 씁니다.
        private readonly FieldCellShape _shape;
        // 이동 경로를 띄우는 높이입니다. 칸 테두리 값과 따로입니다. 띠 두께·화살촉 값은 부품이 읽습니다.
        private readonly float _pathYOffset;
        private readonly float _dashLength;
        private readonly float _dashFill;
        private readonly Slot[] _slots;
        private readonly List<Vector2Int> _scratch = new();
        // 칸 채우기입니다. View DB에 머티리얼이 없으면 null이고 채우지 않습니다. 종류·겹침 깊이 규칙은 부품이 가집니다.
        private readonly FieldCellFillStack _fills;
        // 높이별로 나눌 때 쓰는 재사용 목록입니다(Refresh마다 할당하지 않음).
        private readonly List<List<Vector2Int>> _levelCells = new();
        private readonly List<Vector2Int> _noCells = new();
        // 이동 경로 높이 구간마다의 점(필드 로컬)입니다. 다시 씁니다.
        private readonly List<List<Vector3>> _pathRuns = new();
        // 덩어리에서 뺀 겹친 칸입니다. [k]가 깊이 k이고 [0]은 쓰지 않습니다. 다시 씁니다.
        private readonly List<List<Vector2Int>> _insetCells = new();
        private readonly HashSet<Vector2Int> _insetSet = new();

        private IFieldHighlightStateReader _state;
        private FieldGridMapper _mapper;
        private FieldCellSpacing _spacing;
        private Vector3 _gridOrigin;
        private bool _isBuilt;
        private bool _isVisible = true;
        private bool _isDecalShown;
        // 데칼을 켤 때 쓴 가장 높은 표면 높이와, 마지막으로 그린 높이 표 판입니다.
        private float _decalTop;
        private int _drawnSurfaceVersion;

        internal FieldOutlineRenderer(Transform cellsRoot, StageFieldViewData config, IAreaOutlineSystemService outlines,
            FieldHighlightPalette palette, IMeshSystemService meshes, IColorSystemService colors)
        {
            _cellsRoot = cellsRoot != null ? cellsRoot : throw new ArgumentNullException(nameof(cellsRoot));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _outlines = outlines ?? throw new ArgumentNullException(nameof(outlines));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            if (meshes == null)
                throw new ArgumentNullException(nameof(meshes));
            if (colors == null)
                throw new ArgumentNullException(nameof(colors));
            _showGridDecal = config.ShowGridDecal;
            _gridDecalMaterial = !_showGridDecal || config.GridDecalMaterial != null
                ? config.GridDecalMaterial
                : throw new InvalidOperationException("[FieldOutlineRenderer] 격자 데칼을 켜려면 격자 데칼 머티리얼이 필요합니다.");
            _cellSpacing = config.OutlineCellSpacing;
            _configuredSpacing = FieldCellSpacing.FromGridOutline(config);
            _gridLineWidth = config.GridLineWidth;
            _gridDecalDepth = config.GridDecalDepth;
            _gridDecalRenderingLayers = config.GridDecalRenderingLayers;
            _thickness = config.OutlineThickness;
            _yOffset = config.OutlineYOffset;
            _shape = new FieldCellShape(config, _configuredSpacing);
            _pathYOffset = config.MovementPathYOffset;
            _path = new FieldMovementPathLine(cellsRoot, config, meshes, colors);
            _dashLength = config.OutlineDashLength;
            _dashFill = config.OutlineDashFill;
            _gridDecal = new FieldGridDecal(cellsRoot);
            _fills = config.HighlightFillMaterial != null
                ? new FieldCellFillStack(cellsRoot, config, _shape, _palette, meshes, colors)
                : null;

            // 낮은 것부터: 레일 → 아군 배치 구역 → DrawOrder 층들 → 호버.
            IReadOnlyList<FieldHighlightLayer> order = FieldHighlightLayers.DrawOrder;
            _slots = new Slot[order.Count + 3];
            _slots[0] = new Slot("FieldOutline_Rail", SlotKind.Rail, FieldHighlightLayer.None);
            _slots[1] = new Slot("FieldOutline_PlayerZone", SlotKind.PlayerZone, FieldHighlightLayer.None);
            for (int i = 0; i < order.Count; i++)
                _slots[i + 2] = new Slot($"FieldOutline_{order[i]}", SlotKind.Layer, order[i]);
            _slots[_slots.Length - 1] = new Slot("FieldOutline_Hover", SlotKind.Hover, FieldHighlightLayer.None);
            // 층을 더해 테두리 정렬 순서가 낙석 느낌표(그 위의 궤적 점선 포함)까지 올라가면 그것들이 테두리 아래로 깔립니다(정렬 순서 표, 평가 C10-22).
            if (FieldSortingOrders.Outline(_slots.Length - 1) >= FieldSortingOrders.RockfallWarningMark)
                throw new InvalidOperationException("[FieldOutlineRenderer] 테두리 정렬 순서가 낙석 느낌표·궤적 점선 정렬 순서에 닿았습니다. 정렬 순서 표를 고쳐야 합니다.");
        }

        private enum SlotKind
        {
            Rail,
            PlayerZone,
            Layer,
            Hover
        }

        public bool CanReuse(StageFieldViewData config)
        {
            return config.DisplayMode == FieldDisplayMode.GridOutline
                && config.ShowGridDecal == _showGridDecal
                && config.GridDecalMaterial == _gridDecalMaterial
                && config.OutlineCellSpacing == _cellSpacing
                && (_fills?.Uses(config) ?? config.HighlightFillMaterial == null)
                && _shape.Uses(config)
                && _path.Uses(config);
        }

        public FieldCellSpacing MeasureSpacing()
        {
            return _configuredSpacing;
        }

        public void Build(FieldGridMapper mapper, IFieldHighlightStateReader state)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            // 돌린 필드는 테두리·데칼이 칸과 어긋나므로 핸들을 만들기 전에 막습니다(F11, 평가 C10-08 1단계).
            // 기준은 옛 경고와 같습니다(필드 루트 월드 회전 0.01° 초과). FieldView.Build가 받아 만든 것을 거두고 다시 던집니다.
            if (Quaternion.Angle(_cellsRoot.rotation, Quaternion.identity) > 0.01f)
                throw new InvalidOperationException(
                    $"[FieldOutlineRenderer] GridOutline 모드는 회전하지 않은 Cells Root만 지원합니다(지금 월드 회전 {_cellsRoot.rotation.eulerAngles}).");

            _mapper = mapper;
            _state = state;
            _drawnSurfaceVersion = mapper.Surface.Version;
            _spacing = MeasureSpacing();
            _gridOrigin = mapper.CellCenterToWorld(new CellCoord(0, 0));

            try
            {
                // 이동 초안 층 슬롯은 테두리 핸들 없이 경로 부품이 그립니다.
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (!IsPath(_slots[i]))
                        _slots[i].Handle = _outlines.Create();
                }

                _path.Build();
                _fills?.Build(mapper, state);
            }
            catch (Exception error)
            {
                // 만든 핸들까지 돌려준다. 돌려주다 실패하면 원래 원인과 함께 보고한다.
                List<Exception> errors = null;
                ReleaseHandles(ref errors);
                if (errors == null)
                    throw;

                errors.Insert(0, error);
                throw new AggregateException("[FieldOutlineRenderer] 테두리 핸들을 만들다 실패했고 정리도 실패했습니다.", errors);
            }

            _isBuilt = true;
            Refresh();
        }

        /// <summary>테두리는 칸 하나가 아니라 층 전체로 다시 만들어지므로 바뀐 칸 목록과 상관없이 전체를 맞춥니다.</summary>
        public void Repaint(IReadOnlyList<int> changedCells)
        {
            if (changedCells == null)
                throw new ArgumentNullException(nameof(changedCells));
            if (changedCells.Count > 0)
                Refresh();
        }

        public void SetVisible(bool visible)
        {
            _isVisible = visible;
            _cellsRoot.gameObject.SetActive(visible);
            Refresh();
        }

        public void Refresh()
        {
            if (!_isBuilt)
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileRefresh = _profileRefresh.Auto();
#endif
            // AreaOutline 핸들은 Global 렌더 풀 아래에 있어 필드 부모를 꺼도 남습니다.
            // 기존 Update 계약으로 빈 칸을 보내고, 다시 켜면 같은 상태에서 그립니다.
            if (!_isVisible)
            {
                for (int i = 0; i < _slots.Length; i++) HideArea(_slots[i], i);
                return;
            }

            ShowDecal();
            // 높이 표가 바뀌면 칸 목록이 같아도 그리는 높이가 달라지므로 모두 다시 그립니다(고지대 View 설계 HG-4a).
            if (_mapper.Surface.Version != _drawnSurfaceVersion)
            {
                ForgetDrawn();
                _drawnSurfaceVersion = _mapper.Surface.Version;
            }

            bool acting = _state.IsActing;
            // 채우기를 먼저 쌓아야 테두리가 겹친 칸의 깊이를 물을 수 있습니다(3판 RC8).
            if (_fills != null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var profileFillApply = _profileFillApply.Auto();
#endif
                _fills.Apply(acting);
            }
            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                _scratch.Clear();
                Color color = default;
                if (acting || slot.Kind == SlotKind.Rail || (slot.Layer & FieldHighlightLayers.IdleLayers) != 0)
                    color = Collect(slot);
                if (IsPath(slot))
                {
                    ApplyPath(slot, color);
                    continue;
                }

                // 깊이·높이로 나누기 전 전체 범위에서 같은 경계선을 정합니다.
                Vector2 cellSize = new(_spacing.Width, _spacing.Length);
                GridOutlineOpening opening = slot.Kind == SlotKind.Hover
                    ? FieldAttackOutlineOpening.ForHover(_state, _fills?.HoverOutlineLayer ?? FieldHighlightLayer.None, cellSize)
                    : FieldAttackOutlineOpening.Of(_state, slot.Layer, cellSize);
                SplitInsets(slot);
                ApplyArea(slot, i, color, opening);
                ApplyInsets(slot, i, color, opening);
            }
        }

        /// <summary>
        /// 겹친 칸(채우기 깊이 1 이상)을 덩어리 칸 목록(<see cref="_scratch"/>)에서 빼 깊이별 목록(<see cref="_insetCells"/>)으로 옮깁니다(3판 RC8).
        /// 층 테두리는 그 층 채우기의 깊이별 칸을, 호버 테두리는 <see cref="FieldCellFillStack.HoverOutlineDepth"/>·<see cref="FieldCellFillStack.HoverOutlineCells"/>를 씁니다.
        /// 깊이를 아는 곳은 채우기 부품 하나이고, 채우기가 없거나 행동 중이 아니면 아무것도 옮기지 않습니다. 레일·배치 구역(늘 깊이 0)은 그대로입니다.
        /// </summary>
        private void SplitInsets(Slot slot)
        {
            for (int d = 0; d < _insetCells.Count; d++)
                _insetCells[d].Clear();
            if (_fills == null || _scratch.Count == 0)
                return;

            if (slot.Kind == SlotKind.Hover)
            {
                int depth = _fills.HoverOutlineDepth;
                if (depth < 1)
                    return;
                // 호버가 아래 판과 합쳐졌으면 호버 칸이 든 덩어리 전체라 테두리가 그 판 둘레에 놓입니다(4판 검토 L1).
                InsetCellsOf(depth).AddRange(_fills.HoverOutlineCells);
                _scratch.Clear();
                return;
            }

            if (slot.Kind != SlotKind.Layer)
                return;
            int depths = _fills.DepthCountOf(slot.Layer);
            if (depths < 2)
                return;

            _insetSet.Clear();
            for (int d = 1; d < depths; d++)
            {
                IReadOnlyList<Vector2Int> cells = _fills.CellsOf(slot.Layer, d);
                List<Vector2Int> into = InsetCellsOf(d);
                for (int c = 0; c < cells.Count; c++)
                {
                    into.Add(cells[c]);
                    _insetSet.Add(cells[c]);
                }
            }
            if (_insetSet.Count == 0)
                return;

            int kept = 0;
            for (int c = 0; c < _scratch.Count; c++)
            {
                if (!_insetSet.Contains(_scratch[c]))
                    _scratch[kept++] = _scratch[c];
            }
            _scratch.RemoveRange(kept, _scratch.Count - kept);
        }

        // 그 깊이의 겹친 칸 목록입니다. 처음 쓰는 깊이면 만듭니다.
        private List<Vector2Int> InsetCellsOf(int depth)
        {
            while (_insetCells.Count <= depth)
                _insetCells.Add(new List<Vector2Int>());
            return _insetCells[depth];
        }

        /// <summary>
        /// 깊이마다 겹친 칸을 그 깊이 핸들에 판 모양(덩어리 방식은 덩어리를, 칸마다 방식은 칸을 안쪽으로 줄이고 볼록 모서리만 둥근 고리)으로 그립니다. 핸들은 처음 칸이 생길 때 만들고,
        /// 이번에 칸이 없는 깊이의 핸들은 비웁니다. 지형 높이별 나눔은 덩어리 테두리와 같습니다(<see cref="ApplyArea"/>).
        /// </summary>
        private void ApplyInsets(Slot slot, int slotIndex, Color color, GridOutlineOpening opening)
        {
            int count = Math.Max(slot.Insets.Count, _insetCells.Count - 1);
            for (int depth = 1; depth <= count; depth++)
            {
                List<Vector2Int> cells = depth < _insetCells.Count ? _insetCells[depth] : _noCells;
                if (slot.Insets.Count < depth)
                {
                    if (cells.Count == 0)
                        continue;
                    AddInsets(slot, depth);
                }

                _scratch.Clear();
                _scratch.AddRange(cells);
                ApplyArea(slot.Insets[depth - 1], slotIndex, color, opening);
            }
        }

        // 겹친 칸 테두리 핸들을 그 깊이까지 만듭니다. 만든 뒤에 목록에 넣어 만들기가 실패해도 해제할 것이 남지 않습니다.
        private void AddInsets(Slot slot, int depth)
        {
            while (slot.Insets.Count < depth)
            {
                int inset = slot.Insets.Count + 1;
                var added = new Slot($"{slot.Name}_Inset{inset}", slot.Kind, slot.Layer) { InsetDepth = inset };
                added.Handle = _outlines.Create();
                slot.Insets.Add(added);
            }
        }

        // 핸들과 그 높이 조각·깊이 판 핸들을 모두 빈 칸으로 그려 숨깁니다.
        private void HideArea(Slot slot, int slotIndex)
        {
            Apply(slot, slotIndex, slot.Height, _noCells, default, default);
            foreach (Slot part in slot.Parts) HideArea(part, slotIndex);
            foreach (Slot inset in slot.Insets) HideArea(inset, slotIndex);
        }

        // 테두리 고리 모양입니다. 핸들이 그리는 채우기 깊이의 판 모양 그대로입니다(덩어리 방식의 깊이 0은 칸 경계 그대로, 모양 식은 FieldCellShape 하나).
        private Vector2 LoopInsetOf(Slot slot) => _shape.InsetOf(slot.InsetDepth);

        private float CornerRadiusOf(Slot slot) => _shape.OutlineRadiusOf(slot.InsetDepth);

        /// <summary>
        /// 테두리 하나를 그립니다. 지형이 올린 칸이 없으면 지금처럼 핸들 하나로, 있으면 바닥 칸은 그 핸들로, 나머지 칸은 높이마다 따로 그립니다.
        /// 높이는 격자 높이(<see cref="FieldSurfaceHeights.OverlayHeightAt"/>, 지형만)라 바닥 타일 높이는 보지 않습니다(타일 높이 설계 TH4 개정 2).
        /// </summary>
        private void ApplyArea(Slot slot, int slotIndex, Color color, GridOutlineOpening opening)
        {
            FieldSurfaceHeights surface = _mapper.Surface;
            if (!surface.HasOverlayLevels && slot.Parts.Count == 0)
            {
                Apply(slot, slotIndex, 0f, _scratch, color, opening);
                return;
            }

            IReadOnlyList<float> levels = surface.OverlayLevels;
            while (_levelCells.Count < levels.Count)
                _levelCells.Add(new List<Vector2Int>());
            for (int k = 0; k < levels.Count; k++)
                _levelCells[k].Clear();

            int ground = 0;
            for (int c = 0; c < _scratch.Count; c++)
            {
                Vector2Int cell = _scratch[c];
                float height = surface.OverlayHeightAt(new CellCoord(cell.x, cell.y));
                if (height == 0f)
                    _scratch[ground++] = cell;
                else
                    _levelCells[IndexOf(levels, height)].Add(cell);
            }
            _scratch.RemoveRange(ground, _scratch.Count - ground);
            Apply(slot, slotIndex, 0f, _scratch, color, opening);

            for (int k = 0; k < levels.Count; k++)
            {
                Slot part = FindPart(slot, levels[k]);
                if (part == null)
                {
                    if (_levelCells[k].Count == 0)
                        continue;
                    part = AddPart(slot, levels[k]);
                }
                Apply(part, slotIndex, levels[k], _levelCells[k], color, opening);
            }

            // 지금 높이 목록에 없는 높이의 핸들은 비웁니다.
            for (int p = 0; p < slot.Parts.Count; p++)
            {
                if (IndexOf(levels, slot.Parts[p].Height) < 0)
                    Apply(slot.Parts[p], slotIndex, slot.Parts[p].Height, _noCells, color, opening);
            }
        }

        /// <summary>
        /// 이동 경로를 그립니다. 경로 칸을 격자 높이(지형만, 바닥 타일 높이는 보지 않음)가 같은 연속 구간으로 나눠, 구간마다 필드 로컬 점(칸 가운데 +
        /// 격자 높이 + 경로 높이 + 모든 테두리 칸 위 한 단계)을 부품에 넘깁니다. 띠·꺾임 이음·화살촉 모양은 부품이 정합니다(이동 경로 화살표 설계 MA1·P1).
        /// 구간마다 다음 구간의 첫 칸까지 그 구간 높이로 이어 그립니다. 그래야 한 칸짜리 구간(예: 한 칸짜리 고지대를 지나는 경로)에서도
        /// 띠가 끊기지 않습니다(타일 높이 계획 §8.7 H1, 설계 P2). 칸이 둘 미만이면 지웁니다. 칸 목록·색이 지난번과 같으면 다시 그리지 않고,
        /// 높이 표가 바뀌면 <see cref="ForgetDrawn"/>이 다시 그리게 합니다.
        /// </summary>
        private void ApplyPath(Slot slot, Color color)
        {
            if (slot.Drawn.SameAs(_scratch, color))
                return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profilePathApply = _profilePathApply.Auto();
#endif
            if (_scratch.Count < 2)
            {
                _path.Clear();
                slot.Drawn.Remember(_scratch, color);
                return;
            }

            FieldSurfaceHeights surface = _mapper.Surface;
            // 경로는 깊이를 쓰는 불투명 띠라, 모든 테두리 칸보다 한 단계 위에 두어 깊이를 쓰지 않는 채우기·테두리(호버 테두리 포함)가
            // 띠·화살촉 위에 덧칠되지 않게 합니다(겹친 강조 2단계 계획 §7.6). 경로 높이 값 ≥ 테두리 높이 값은 View DB가 검사합니다.
            float lift = _pathYOffset + (_slots.Length * LayerHeightStep);
            int run = 0;
            int start = 0;
            while (start < _scratch.Count)
            {
                float height = surface.OverlayHeightAt(CellOf(_scratch[start]));
                int end = start + 1;
                while (end < _scratch.Count && surface.OverlayHeightAt(CellOf(_scratch[end])) == height)
                    end++;

                List<Vector3> points = PathRun(run);
                for (int c = start; c < end; c++)
                    points.Add(PathPoint(_scratch[c], height + lift));
                if (end < _scratch.Count)
                    points.Add(PathPoint(_scratch[end], height + lift));
                run++;
                start = end;
            }

            Vector2Int last = _scratch[_scratch.Count - 1];
            Vector2Int before = _scratch[_scratch.Count - 2];
            Vector3 lastStep = _mapper.StepToLocal(new CellCoord(last.x - before.x, last.y - before.y));
            _path.Show(_pathRuns, run, lastStep, color);
            slot.Drawn.Remember(_scratch, color);
        }

        // 이동 초안 층 슬롯은 테두리 핸들 대신 경로 부품이 그립니다.
        private static bool IsPath(Slot slot) => slot.Layer == FieldHighlightLayer.MovementDraft;

        private static CellCoord CellOf(Vector2Int cell) => new(cell.x, cell.y);

        // 칸 가운데(필드 로컬)를 바닥면에서 lift만큼 올린 점입니다.
        private Vector3 PathPoint(Vector2Int cell, float lift) => _mapper.CellCenterToLocal(CellOf(cell)) + (Vector3.up * lift);

        // 높이 구간 하나의 점 목록입니다. 비워서 돌려주고, 처음 쓰는 구간 번호면 만듭니다.
        private List<Vector3> PathRun(int run)
        {
            while (_pathRuns.Count <= run)
                _pathRuns.Add(new List<Vector3>());
            List<Vector3> points = _pathRuns[run];
            points.Clear();
            return points;
        }

        private static int IndexOf(IReadOnlyList<float> levels, float height)
        {
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i] == height)
                    return i;
            }
            return -1;
        }

        private static Slot FindPart(Slot slot, float height)
        {
            for (int i = 0; i < slot.Parts.Count; i++)
            {
                if (slot.Parts[i].Height == height)
                    return slot.Parts[i];
            }
            return null;
        }

        // 바닥이 아닌 칸용 추가 테두리 핸들입니다. 만든 뒤에 목록에 넣어 만들기가 실패해도 해제할 것이 남지 않습니다.
        private Slot AddPart(Slot slot, float height)
        {
            var part = new Slot($"{slot.Name}_{slot.Parts.Count + 1}", slot.Kind, slot.Layer) { Height = height, InsetDepth = slot.InsetDepth };
            part.Handle = _outlines.Create();
            slot.Parts.Add(part);
            return part;
        }

        // 그린 값 기억을 모두 지워 다음 그리기가 모든 핸들·채우기를 다시 쓰게 합니다.
        private void ForgetDrawn()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                ForgetSlot(_slots[i]);
                for (int d = 0; d < _slots[i].Insets.Count; d++)
                    ForgetSlot(_slots[i].Insets[d]);
            }
            _fills?.Forget();
        }

        // 테두리 하나와 그 높이별 핸들의 그린 값 기억을 지웁니다.
        private static void ForgetSlot(Slot slot)
        {
            slot.Drawn.Forget();
            for (int p = 0; p < slot.Parts.Count; p++)
                slot.Parts[p].Drawn.Forget();
        }

        /// <summary>
        /// 핸들 반환 → 데칼 숨김을 각각 시도하고, 실패와 관계없이 상태를 비운 뒤 실패를 모아 한 번 던집니다(D5).
        /// </summary>
        public void Release()
        {
            List<Exception> errors = null;
            ReleaseHandles(ref errors);
            try
            {
                _gridDecal.Hide();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            _isDecalShown = false;
            _isBuilt = false;
            _state = null;
            _mapper = null;
            if (errors != null)
                throw new AggregateException("[FieldOutlineRenderer] 표시 해제 중 실패했습니다.", errors);
        }

        public void Dispose()
        {
            try
            {
                Release();
            }
            finally
            {
                _gridDecal.Dispose();
            }
        }

        /// <summary>칸 목록을 <see cref="_scratch"/>에 모으고 색을 돌려줍니다. 그릴 것이 없으면 목록이 빈 채로 남습니다.</summary>
        private Color Collect(Slot slot)
        {
            switch (slot.Kind)
            {
                case SlotKind.Rail:
                    for (int i = 0; i < _state.CellCount; i++)
                    {
                        if (_state.ZoneAt(i) == FieldStaticZone.Rail)
                            AddCell(i);
                    }

                    return _palette.Rail;

                case SlotKind.PlayerZone:
                    if (!_state.IsPlacementActive)
                        return default;
                    for (int i = 0; i < _state.CellCount; i++)
                    {
                        if (_state.ZoneAt(i) == FieldStaticZone.PlayerZone)
                            AddCell(i);
                    }

                    return _palette.PlayerZone;

                case SlotKind.Hover:
                    if (_state.HoverIndex < 0)
                        return default;
                    AddCell(_state.HoverIndex);
                    _palette.TryGet(FieldHighlightLayers.HoverColor(_state.HoverState, _state.HoverKind), FieldStaticZone.None,
                        out Color hover);
                    return hover;

                default:
                    if (slot.Layer == FieldHighlightLayer.MovementDraft)
                    {
                        IReadOnlyList<CellCoord> path = _state.MovementPath;
                        for (int i = 0; i < path.Count; i++)
                            _scratch.Add(new Vector2Int(path[i].X, path[i].Y));
                        return _palette.MovementDraft;
                    }
                    for (int i = 0; i < _state.CellCount; i++)
                    {
                        if ((_state.LayersAt(i) & slot.Layer) != 0)
                            AddCell(i);
                    }

                    _palette.TryGet(FieldHighlightLayers.ColorOf(slot.Layer), FieldStaticZone.None, out Color layer);
                    return layer;
            }
        }

        private void AddCell(int index)
        {
            int columns = _state.ColumnCount;
            _scratch.Add(new Vector2Int(index % columns, index / columns));
        }

        /// <summary>
        /// 테두리 핸들 하나에 칸 목록을 그 표면 높이 위로 그립니다. 칸 목록·색이 지난번과 같으면 메시를 다시 만들지 않습니다.
        /// 표면 높이 0이면 높이는 지금과 같습니다. 고리 모양(안쪽 거리·모서리)은 핸들마다 정해져 있습니다(<see cref="LoopInsetOf"/>·<see cref="CornerRadiusOf"/>).
        /// </summary>
        private void Apply(Slot slot, int slotIndex, float surfaceHeight, List<Vector2Int> cells, Color color, GridOutlineOpening opening)
        {
            float yOffset = _yOffset + SurfaceWorldHeight(surfaceHeight) + (slotIndex * LayerHeightStep);
            if (slot.Handle == null || (slot.Drawn.SameAs(cells, color) && slot.DrawnOpening.Equals(opening)))
                return;

            // 우리 칸 배열은 마커 밖에서 채워, 마커 안의 할당이 GlobalSystem 메시 만들기 몫만 되게 합니다.
            Vector2Int[] cellArray = slot.CellArrayOf(cells);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUpdate = slot.UpdateMarker(cells.Count == 0).Auto();
#endif
            slot.Handle.Update(new AreaOutlineData(
                slot.Name,
                cellArray,
                _gridOrigin,
                new Vector2(_spacing.Width, _spacing.Length),
                yOffset,
                _thickness,
                color,
                isDashed: true,
                dashFill: _dashFill,
                dashLength: _dashLength,
                sortingOrder: FieldSortingOrders.Outline(slotIndex),
                cornerRadius: CornerRadiusOf(slot),
                loopInset: LoopInsetOf(slot),
                eachCell: _shape.EachCellAt(slot.InsetDepth),
                opening: opening));
            slot.Drawn.Remember(cells, color);
            slot.DrawnOpening = opening;
        }

        // 필드 로컬 표면 높이를 월드 높이로 바꿉니다. 테두리는 회전하지 않은 필드에서만 그리므로 위 방향 성분만 씁니다.
        private float SurfaceWorldHeight(float surfaceHeight) =>
            surfaceHeight == 0f ? 0f : _mapper.SurfaceOffset(surfaceHeight).y;

        /// <summary>
        /// 격자 데칼을 켭니다. 끄는 것은 <see cref="Release"/>뿐입니다. 켠 뒤에만 기억해 실패하면 다음 Refresh가 다시 시도합니다.
        /// 가장 높은 격자 높이(지형)가 바뀌면 투영 상자를 그 높이까지 덮게 다시 켭니다(고지대 View 계획 P15). 평지면 한 번만 켭니다.
        /// 바닥 타일 높이는 보지 않습니다(타일 높이 설계 TH4 개정 2 — 상자 아래 끝은 늘 바닥면 아래 depth/2). 데칼은 상자 안의 실제 표면에 찍히므로
        /// 바닥면에서 depth/2 안으로 솟거나 들어간 타일 윗면에는 격자선이 그 윗면에 찍힙니다.
        /// View DB의 showGridDecal이 꺼져 있으면 아무것도 하지 않습니다(데칼 오브젝트도 만들지 않음).
        /// </summary>
        private void ShowDecal()
        {
            if (!_showGridDecal)
                return;

            float top = _mapper.Surface.MaxHeight;
            if (_isDecalShown && top == _decalTop)
                return;

            _gridDecal.Show(_mapper, _spacing, _gridLineWidth, _gridDecalMaterial, _gridDecalDepth, _gridDecalRenderingLayers, top);
            _isDecalShown = true;
            _decalTop = top;
        }

        /// <summary>
        /// 모든 슬롯의 핸들, 이동 경로 부품, 칸 채우기를 돌려줍니다. 하나가 실패해도 그 슬롯을 비우고 다음 슬롯을 계속하며, 실패는 모읍니다.
        /// 참조를 먼저 비우므로 같은 핸들을 두 번 돌려주지 않습니다.
        /// </summary>
        private void ReleaseHandles(ref List<Exception> errors)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                ReleaseWithParts(_slots[i], ref errors);
                // 겹친 칸 테두리 핸들도 돌려주고 목록을 비웁니다. 다음 구성에서 필요할 때 다시 만듭니다.
                for (int d = 0; d < _slots[i].Insets.Count; d++)
                    ReleaseWithParts(_slots[i].Insets[d], ref errors);
                _slots[i].Insets.Clear();
            }

            try
            {
                _path.Release();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            _fills?.Release(ref errors);
        }

        // 테두리 하나와 바닥이 아닌 칸용 추가 핸들을 돌려주고 추가 핸들 목록을 비웁니다. 다음 구성에서 필요할 때 다시 만듭니다.
        private static void ReleaseWithParts(Slot slot, ref List<Exception> errors)
        {
            ReleaseSlot(slot, ref errors);
            for (int p = 0; p < slot.Parts.Count; p++)
                ReleaseSlot(slot.Parts[p], ref errors);
            slot.Parts.Clear();
        }

        // 테두리 하나의 핸들을 돌려줍니다. 참조를 먼저 비워 같은 핸들을 두 번 돌려주지 않고, 실패는 모읍니다.
        private static void ReleaseSlot(Slot slot, ref List<Exception> errors)
        {
            IAreaOutlineHandle handle = slot.Handle;
            slot.Handle = null;
            slot.Drawn.Forget();
            try
            {
                handle?.Dispose();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }
        }

        /// <summary>테두리 하나의 핸들과 마지막으로 그린 값입니다.</summary>
        private sealed class Slot
        {
            internal Slot(string name, SlotKind kind, FieldHighlightLayer layer)
            {
                Name = name;
                Kind = kind;
                Layer = layer;
            }

            internal string Name { get; }
            internal SlotKind Kind { get; }
            internal FieldHighlightLayer Layer { get; }
            // 테두리 핸들입니다. 이동 초안 층 슬롯은 핸들 없이 경로 부품이 그리고, 이 슬롯은 그린 값 기억만 씁니다.
            internal IAreaOutlineHandle Handle { get; set; }
            // 바닥이 아닌 칸용 추가 테두리 핸들입니다(층 테두리는 높이마다 하나). 기본 핸들은 바닥 칸입니다.
            internal List<Slot> Parts { get; } = new();
            // 추가 핸들이 그리는 표면 높이입니다. 기본 핸들은 쓰지 않습니다.
            internal float Height { get; set; }
            // 겹친 칸 테두리 핸들입니다. [k - 1]이 채우기 깊이 k입니다(3판 RC8). 기본 핸들은 덩어리(깊이 0)입니다.
            internal List<Slot> Insets { get; } = new();
            // 이 핸들이 그리는 채우기 깊이입니다. 0이면 덩어리 테두리, 1 이상이면 그 깊이 덩어리를 판 모양으로 줄인 고리입니다.
            internal int InsetDepth { get; set; }
            // 마지막으로 그린 칸 목록·색입니다(채우기 부품과 같은 규칙).
            internal FieldDrawnCells Drawn { get; } = new();
            // 같은 칸·색이어도 공격자가 이동해 열린 변이 바뀌면 다시 그립니다.
            internal GridOutlineOpening DrawnOpening { get; set; }

            // 테두리 메시를 만들 때 넘기는 칸 배열입니다. 칸 수가 같으면 다시 씁니다(GC 끊김 설계 GH2).
            // GlobalSystem은 받은 배열로 그 자리에서 메시를 만들고 보관하지 않아(MeshSystem Update) 다시 써도 안전합니다.
            private Vector2Int[] _cellArray = Array.Empty<Vector2Int>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 이 핸들의 Update 계측 마커입니다(Q-FP3a 1단계). 이름은 핸들 이름에서 나오고, 그리기·비우기를 나눠 처음 쓸 때만 만듭니다.
            private Unity.Profiling.ProfilerMarker? _drawMarker;
            private Unity.Profiling.ProfilerMarker? _clearMarker;

            internal Unity.Profiling.ProfilerMarker UpdateMarker(bool clearing) => clearing
                ? (_clearMarker ??= new Unity.Profiling.ProfilerMarker("Combat.View.Field.Outline.Clear." + Name))
                : (_drawMarker ??= new Unity.Profiling.ProfilerMarker("Combat.View.Field.Outline.Draw." + Name));
#endif

            internal Vector2Int[] CellArrayOf(List<Vector2Int> cells)
            {
                if (_cellArray.Length != cells.Count)
                    _cellArray = new Vector2Int[cells.Count];
                cells.CopyTo(_cellArray);
                return _cellArray;
            }
        }
    }
}
