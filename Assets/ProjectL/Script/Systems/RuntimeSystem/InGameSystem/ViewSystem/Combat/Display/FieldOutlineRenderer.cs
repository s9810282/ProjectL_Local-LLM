using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.AreaOutlineSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// GridOutline 모드 표시기입니다(격자 아웃라인 설계 F5). 셀을 만들지 않고, 강조를 층마다 AreaOutline 테두리 하나로 그립니다.
    /// 평소에는 현재 행위자 테두리만, 행동 중(<see cref="FieldHighlightState.IsActing"/>: 범위·영향·경로·차단 벽 층,
    /// 배치 드래그, Q 경로 지정)에는 격자 데칼과 모든 층·호버, 배치 드래그 중이면 아군 배치 구역 테두리도 그립니다. 칸 간격은 View DB의 outlineCellSpacing입니다.
    /// </summary>
    /// <remarks>
    /// 층마다 전투 동안 핸들 하나를 유지하고 숨길 때는 빈 칸 목록으로 갱신합니다. 칸 목록·색이 같으면 다시 쓰지 않습니다.
    /// 높은 층이 위에 보이도록 <see cref="FieldHighlightLayers.DrawOrder"/> 순서대로 높이를 조금씩 올립니다.
    /// 점선은 View DB의 점선 길이로 나뉘어 테두리 크기와 상관없이 같은 길이입니다.
    /// AreaOutline과 격자 데칼은 월드 XZ 기준이라 필드 루트가 회전돼 있으면 경고 한 번 뒤 그리지 않습니다(F11).
    /// </remarks>
    internal sealed class FieldOutlineRenderer : IFieldHighlightRenderer
    {
        // 층 사이 높이 차이입니다. 겹친 테두리의 Z-fighting만 피하면 됩니다.
        private const float LayerHeightStep = 0.002f;

        private readonly Transform _cellsRoot;
        private readonly IAreaOutlineSystemService _outlines;
        private readonly FieldHighlightPalette _palette;
        private readonly FieldGridDecal _gridDecal;
        private readonly Material _gridDecalMaterial;
        private readonly Vector2 _cellSpacing;
        private readonly float _gridLineWidth;
        private readonly float _gridDecalDepth;
        private readonly float _thickness;
        private readonly float _yOffset;
        private readonly float _dashLength;
        private readonly float _dashFill;
        private readonly Slot[] _slots;
        private readonly List<Vector2Int> _scratch = new();

        private IFieldHighlightStateReader _state;
        private FieldGridMapper _mapper;
        private FieldCellSpacing _spacing;
        private Vector3 _gridOrigin;
        private bool _isBuilt;
        private bool _isRotated;
        private bool _warnedRotation;
        private bool _isDecalShown;

        internal FieldOutlineRenderer(Transform cellsRoot, StageFieldViewData config, IAreaOutlineSystemService outlines,
            FieldHighlightPalette palette)
        {
            _cellsRoot = cellsRoot != null ? cellsRoot : throw new ArgumentNullException(nameof(cellsRoot));
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _outlines = outlines ?? throw new ArgumentNullException(nameof(outlines));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _gridDecalMaterial = config.GridDecalMaterial != null
                ? config.GridDecalMaterial
                : throw new InvalidOperationException("[FieldOutlineRenderer] GridOutline 모드에는 격자 데칼 머티리얼이 필요합니다.");
            _cellSpacing = config.OutlineCellSpacing;
            _gridLineWidth = config.GridLineWidth;
            _gridDecalDepth = config.GridDecalDepth;
            _thickness = config.OutlineThickness;
            _yOffset = config.OutlineYOffset;
            _dashLength = config.OutlineDashLength;
            _dashFill = config.OutlineDashFill;
            _gridDecal = new FieldGridDecal(cellsRoot);

            // 낮은 것부터: 아군 배치 구역 → DrawOrder 층들 → 호버.
            IReadOnlyList<FieldHighlightLayer> order = FieldHighlightLayers.DrawOrder;
            _slots = new Slot[order.Count + 2];
            _slots[0] = new Slot("FieldOutline_PlayerZone", SlotKind.PlayerZone, FieldHighlightLayer.None);
            for (int i = 0; i < order.Count; i++)
                _slots[i + 1] = new Slot($"FieldOutline_{order[i]}", SlotKind.Layer, order[i]);
            _slots[_slots.Length - 1] = new Slot("FieldOutline_Hover", SlotKind.Hover, FieldHighlightLayer.None);
        }

        private enum SlotKind
        {
            PlayerZone,
            Layer,
            Hover
        }

        public bool CanReuse(StageFieldViewData config)
        {
            return config.DisplayMode == FieldDisplayMode.GridOutline
                && config.GridDecalMaterial == _gridDecalMaterial
                && config.OutlineCellSpacing == _cellSpacing;
        }

        public FieldCellSpacing MeasureSpacing()
        {
            return FieldCellSpacing.FromCenterSpacing(_cellSpacing.x, _cellSpacing.y);
        }

        public void Build(FieldGridMapper mapper, IFieldHighlightStateReader state)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _spacing = MeasureSpacing();
            _gridOrigin = mapper.CellCenterToWorld(new CellCoord(0, 0));
            _isRotated = Quaternion.Angle(_cellsRoot.rotation, Quaternion.identity) > 0.01f;
            if (_isRotated && !_warnedRotation)
            {
                Debug.LogWarning("[FieldOutlineRenderer] 격자 아웃라인은 회전하지 않은 필드에서만 그립니다.", _cellsRoot);
                _warnedRotation = true;
            }

            try
            {
                for (int i = 0; i < _slots.Length; i++)
                    _slots[i].Handle = _outlines.Create();
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

        public void Refresh()
        {
            if (!_isBuilt || _isRotated)
                return;

            bool acting = _state.IsActing;
            SetDecalVisible(acting);
            for (int i = 0; i < _slots.Length; i++)
            {
                Slot slot = _slots[i];
                _scratch.Clear();
                Color color = default;
                if (acting || slot.Layer == FieldHighlightLayer.ActiveActor)
                    color = Collect(slot);
                Apply(slot, i, color);
            }
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
                    _palette.TryGet(FieldHighlightLayers.HoverColor(_state.HoverState), FieldStaticZone.None, out Color hover);
                    return hover;

                default:
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

        /// <summary>칸 목록·색이 지난번과 같으면 메시를 다시 만들지 않습니다.</summary>
        private void Apply(Slot slot, int heightIndex, Color color)
        {
            if (slot.Handle == null || slot.SameAs(_scratch, color))
                return;

            slot.Remember(_scratch, color);
            slot.Handle.Update(new AreaOutlineData(
                slot.Name,
                _scratch.ToArray(),
                _gridOrigin,
                new Vector2(_spacing.Width, _spacing.Length),
                _yOffset + (heightIndex * LayerHeightStep),
                _thickness,
                color,
                isDashed: true,
                dashFill: _dashFill,
                dashLength: _dashLength));
        }

        private void SetDecalVisible(bool visible)
        {
            if (_isDecalShown == visible)
                return;

            _isDecalShown = visible;
            if (visible)
                _gridDecal.Show(_mapper, _spacing, _gridLineWidth, _gridDecalMaterial, _gridDecalDepth);
            else
                _gridDecal.Hide();
        }

        /// <summary>
        /// 모든 슬롯의 핸들을 돌려줍니다. 하나가 실패해도 그 슬롯을 비우고 다음 슬롯을 계속하며, 실패는 모읍니다.
        /// 참조를 먼저 비우므로 같은 핸들을 두 번 돌려주지 않습니다.
        /// </summary>
        private void ReleaseHandles(ref List<Exception> errors)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                IAreaOutlineHandle handle = _slots[i].Handle;
                _slots[i].Handle = null;
                _slots[i].Forget();
                try
                {
                    handle?.Dispose();
                }
                catch (Exception exception)
                {
                    (errors ??= new List<Exception>()).Add(exception);
                }
            }
        }

        /// <summary>테두리 하나의 핸들과 마지막으로 그린 값입니다.</summary>
        private sealed class Slot
        {
            private readonly List<Vector2Int> _shownCells = new();
            private Color _shownColor;
            private bool _hasShown;

            internal Slot(string name, SlotKind kind, FieldHighlightLayer layer)
            {
                Name = name;
                Kind = kind;
                Layer = layer;
            }

            internal string Name { get; }
            internal SlotKind Kind { get; }
            internal FieldHighlightLayer Layer { get; }
            internal IAreaOutlineHandle Handle { get; set; }

            internal bool SameAs(List<Vector2Int> cells, Color color)
            {
                if (!_hasShown || _shownCells.Count != cells.Count)
                    return false;
                // 빈 테두리는 색과 상관없이 같은 모습이다.
                if (cells.Count > 0 && _shownColor != color)
                    return false;
                for (int i = 0; i < cells.Count; i++)
                {
                    if (_shownCells[i] != cells[i])
                        return false;
                }

                return true;
            }

            internal void Remember(List<Vector2Int> cells, Color color)
            {
                _shownCells.Clear();
                _shownCells.AddRange(cells);
                _shownColor = color;
                _hasShown = true;
            }

            internal void Forget()
            {
                _shownCells.Clear();
                _hasShown = false;
            }
        }
    }
}
