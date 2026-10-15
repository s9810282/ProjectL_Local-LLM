using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.ColorSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// CellPrefab 모드 표시기입니다. 칸마다 셀 프리팹을 빌려 깔고, 강조는 층 우선순위(<see cref="FieldHighlightLayers"/>)로
    /// 고른 색 하나를 셀에 칠합니다. 칸 간격은 셀 프리팹 크기 × cellOffsetSize + padding입니다.
    /// </summary>
    /// <remarks>
    /// alternateCellPrefab이 있으면 체스판처럼 번갈아 놓습니다. 셀은 파괴하지 않고 풀에 돌려줍니다(FW-014).
    /// </remarks>
    internal sealed class FieldCellColorRenderer : IFieldHighlightRenderer
    {
        private readonly FieldCellPool _primaryPool;
        private readonly FieldCellPool _alternatePool;
        private readonly Vector3 _cellOffsetSize;
        private readonly float _paddingX;
        private readonly float _paddingY;
        private readonly FieldHighlightPalette _palette;
        private FieldCellHandle[] _cells = Array.Empty<FieldCellHandle>();
        private IFieldHighlightStateReader _state;

        internal FieldCellColorRenderer(Transform cellsRoot, StageFieldViewData config, IColorSystemService colors,
            FieldHighlightPalette palette)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _palette = palette ?? throw new ArgumentNullException(nameof(palette));
            _primaryPool = new FieldCellPool(cellsRoot, config.CellPrefab, colors);
            if (config.AlternateCellPrefab != null)
                _alternatePool = new FieldCellPool(cellsRoot, config.AlternateCellPrefab, colors);
            _cellOffsetSize = config.CellOffsetSize;
            _paddingX = config.CellPaddingX;
            _paddingY = config.CellPaddingY;
        }

        public bool CanReuse(StageFieldViewData config)
        {
            return config.DisplayMode == FieldDisplayMode.CellPrefab
                && _primaryPool.Prefab == config.CellPrefab
                && _alternatePool?.Prefab == config.AlternateCellPrefab;
        }

        public FieldCellSpacing MeasureSpacing()
        {
            _primaryPool.EnsureMeasured();
            float footprintWidth = _primaryPool.FootprintWidth;
            float footprintLength = _primaryPool.FootprintLength;
            if (_alternatePool != null)
            {
                _alternatePool.EnsureMeasured();
                footprintWidth = Mathf.Max(footprintWidth, _alternatePool.FootprintWidth);
                footprintLength = Mathf.Max(footprintLength, _alternatePool.FootprintLength);
            }

            // 풀은 원본 크기를 보관하고, 필드 축 배율을 표시와 격자 계산에 함께 적용한다.
            return FieldCellSpacing.Calculate(
                footprintWidth * _cellOffsetSize.x, footprintLength * _cellOffsetSize.z, _paddingX, _paddingY);
        }

        public void Build(FieldGridMapper mapper, IFieldHighlightStateReader state)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            _state = state ?? throw new ArgumentNullException(nameof(state));

            int width = mapper.ColumnCount;
            int height = mapper.RowCount;
            int total = checked(width * height);
            int alternateCount = _alternatePool == null ? 0 : total / 2;
            _primaryPool.Rent(total - alternateCount);
            _alternatePool?.Rent(alternateCount);

            _cells = new FieldCellHandle[total];
            int primaryIndex = 0;
            int alternateIndex = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool alternate = _alternatePool != null && ((x + y) & 1) != 0;
                    FieldCellHandle cell = alternate
                        ? _alternatePool.GetActive(alternateIndex++)
                        : _primaryPool.GetActive(primaryIndex++);
                    cell.ApplyLayout(mapper.CellCenterToLocal(new CellCoord(x, y)), _cellOffsetSize);
                    cell.SetVisible(true);
                    _cells[(y * width) + x] = cell;
                }
            }
        }

        public void Repaint(IReadOnlyList<int> changedCells)
        {
            if (changedCells == null)
                throw new ArgumentNullException(nameof(changedCells));

            for (int i = 0; i < changedCells.Count; i++)
                Paint(changedCells[i]);
        }

        /// <summary>셀 색은 배치 드래그·경로 지정 여부를 쓰지 않으므로 할 일이 없습니다.</summary>
        public void Refresh()
        {
        }

        /// <summary>
        /// 기본·대체 풀 반환을 각각 시도하고, 실패와 관계없이 상태를 비운 뒤 실패를 모아 한 번 던집니다(D5).
        /// </summary>
        public void Release()
        {
            List<Exception> errors = null;
            try
            {
                _primaryPool.ReturnAll();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            try
            {
                _alternatePool?.ReturnAll();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }

            _cells = Array.Empty<FieldCellHandle>();
            _state = null;
            if (errors != null)
                throw new AggregateException("[FieldCellColorRenderer] 표시 해제 중 실패했습니다.", errors);
        }

        public void Dispose()
        {
            Release();
        }

        /// <summary>칸 하나의 셀입니다(테스트에서 칠한 색을 확인할 때 씁니다).</summary>
        internal FieldCellHandle CellAt(int index)
        {
            return _cells[index];
        }

        /// <summary>층 우선순위로 칸 하나의 색을 정해 칠합니다. 같은 색이면 셀이 다시 쓰지 않습니다.</summary>
        private void Paint(int index)
        {
            if (_state == null || index < 0 || index >= _cells.Length)
                return;

            FieldStaticZone zone = _state.ZoneAt(index);
            FieldHighlightColor slot = FieldHighlightLayers.Resolve(
                index == _state.HoverIndex, _state.HoverState, _state.LayersAt(index), zone != FieldStaticZone.None);

            FieldCellHandle cell = _cells[index];
            if (_palette.TryGet(slot, zone, out Color color))
                cell.SetHighlight(color);
            else
                cell.ClearHighlight();
        }
    }
}
