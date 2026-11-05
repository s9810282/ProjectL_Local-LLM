using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>칸의 정적 구역 종류입니다. 색은 표시기가 팔레트에서 고릅니다. 적 배치 구역은 없습니다(Core 규칙).</summary>
    internal enum FieldStaticZone : byte
    {
        None = 0,
        PlayerZone = 1,
        Rail = 3
    }

    /// <summary>
    /// 필드 강조 상태의 유일한 원본입니다: 입력 순서가 있는 이동 경로, 칸별 층 비트, 정적 구역, 호버, 배치 드래그 여부.
    /// 그리기는 모릅니다. 바꾸는 메서드는 다시 그릴 칸 index 목록을 돌려줍니다.
    /// </summary>
    /// <remarks>
    /// 돌려준 목록은 내부 버퍼라 다음 변경 호출 전까지만 유효합니다. Unity 타입을 쓰지 않습니다.
    /// 바깥(표시기·테스트)에는 읽기 계약 <see cref="IFieldHighlightStateReader"/>로만 노출하고, 바꾸는 쪽은 <see cref="FieldView"/>뿐입니다(D4).
    /// </remarks>
    internal sealed class FieldHighlightState : IFieldHighlightStateReader
    {
        private static readonly int[] NoCells = Array.Empty<int>();

        private FieldHighlightLayer[] _layers = Array.Empty<FieldHighlightLayer>();
        private FieldStaticZone[] _zones = Array.Empty<FieldStaticZone>();
        private bool[] _marks = Array.Empty<bool>();
        private readonly List<int> _changed = new();
        private readonly List<CellCoord> _movementPath = new();
        private readonly List<CellCoord> _hoverRangeSourceCells = new();

        public IReadOnlyList<CellCoord> MovementPath { get; }
        public IReadOnlyList<CellCoord> HoverRangeSourceCells { get; }

        internal FieldHighlightState()
        {
            MovementPath = _movementPath.AsReadOnly();
            HoverRangeSourceCells = _hoverRangeSourceCells.AsReadOnly();
        }

        public int ColumnCount { get; private set; }

        public int RowCount { get; private set; }

        public int CellCount => _layers.Length;

        /// <summary>호버 칸 index입니다. 없으면 -1입니다.</summary>
        public int HoverIndex { get; private set; } = -1;

        public CombatHoverState HoverState { get; private set; }

        public CombatHoverKind HoverKind { get; private set; }

        /// <summary>현재 공격 영향 영역의 닫힌 경계입니다. 영향 교체·해제·Reset과 같은 수명입니다.</summary>
        public bool IsAffectedOutlineClosed { get; private set; }

        /// <summary>손패 배치 드래그 중인지입니다. 셀 색 표시는 이 값을 쓰지 않습니다.</summary>
        public bool IsPlacementActive { get; private set; }

        /// <summary>Q로 이동 경로를 지정하는 중인지입니다(시작 칸 대기 포함). 셀 색 표시는 이 값을 쓰지 않습니다.</summary>
        public bool IsPathPlanning { get; private set; }

        /// <summary>
        /// 사용자가 행동 중인지입니다: 행동 중 층(<see cref="FieldHighlightLayers.ActingLayers"/>)에 칸이 있거나, 배치 드래그 중이거나, 경로 지정 중.
        /// 격자 아웃라인 모드는 이때만 격자와 층 아웃라인을 그립니다(격자 아웃라인 설계 U4·F5).
        /// </summary>
        public bool IsActing => IsPlacementActive || IsPathPlanning || HasAnyCell(FieldHighlightLayers.ActingLayers);

        /// <summary>새 보드 크기로 모든 상태를 비웁니다.</summary>
        internal void Reset(int columnCount, int rowCount)
        {
            if (columnCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(columnCount));
            if (rowCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(rowCount));

            int total = checked(columnCount * rowCount);
            ColumnCount = columnCount;
            RowCount = rowCount;
            _layers = new FieldHighlightLayer[total];
            _zones = new FieldStaticZone[total];
            _marks = new bool[total];
            ResetTransient();
        }

        /// <summary>보드가 없는 상태로 되돌립니다. 여러 번 불려도 안전합니다.</summary>
        internal void Clear()
        {
            ColumnCount = 0;
            RowCount = 0;
            _layers = Array.Empty<FieldHighlightLayer>();
            _zones = Array.Empty<FieldStaticZone>();
            _marks = Array.Empty<bool>();
            ResetTransient();
        }

        public FieldHighlightLayer LayersAt(int index) => _layers[index];

        public FieldStaticZone ZoneAt(int index) => _zones[index];

        /// <summary>주어진 층 중 하나라도 가진 칸이 있는지입니다.</summary>
        public bool HasAnyCell(FieldHighlightLayer mask)
        {
            for (int i = 0; i < _layers.Length; i++)
            {
                if ((_layers[i] & mask) != 0)
                    return true;
            }

            return false;
        }

        /// <summary>격자 안이면 칸 index를 돌려줍니다.</summary>
        public bool TryGetIndex(CellCoord cell, out int index)
        {
            if (cell.X < 0 || cell.Y < 0 || cell.X >= ColumnCount || cell.Y >= RowCount)
            {
                index = -1;
                return false;
            }

            index = (cell.Y * ColumnCount) + cell.X;
            return true;
        }

        /// <summary>
        /// 아군 배치 구역과 레일을 정합니다. 레일 &gt; 아군 배치 구역 순서입니다. 종류가 바뀐 칸만 돌려줍니다.
        /// </summary>
        /// <remarks>아군 구역인지는 Core 값 <see cref="PlayerDeploymentZone.Contains"/>에만 묻습니다. 줄 구역·칸 구역 모양은 여기서 모릅니다.</remarks>
        internal IReadOnlyList<int> SetStaticZones(PlayerDeploymentZone playerZone, IReadOnlyList<CellCoord> railCells)
        {
            if (playerZone == null)
                throw new ArgumentNullException(nameof(playerZone));
            if (railCells == null)
                throw new ArgumentNullException(nameof(railCells));

            for (int i = 0; i < railCells.Count; i++)
            {
                if (TryGetIndex(railCells[i], out int rail))
                    _marks[rail] = true;
            }

            _changed.Clear();
            for (int y = 0; y < RowCount; y++)
            {
                for (int x = 0; x < ColumnCount; x++)
                {
                    int index = (y * ColumnCount) + x;
                    FieldStaticZone zone = _marks[index] ? FieldStaticZone.Rail
                        : playerZone.Contains(new CellCoord(x, y)) ? FieldStaticZone.PlayerZone
                        : FieldStaticZone.None;
                    _marks[index] = false;
                    if (_zones[index] == zone)
                        continue;

                    _zones[index] = zone;
                    _changed.Add(index);
                }
            }

            return _changed;
        }

        /// <summary>호버 칸·상태·종류를 정합니다. 격자 밖이면 호버를 지웁니다. 이전 호버 칸과 새 칸을 돌려줍니다.</summary>
        internal IReadOnlyList<int> SetHover(CellCoord cell, CombatHoverState state, CombatHoverKind kind = CombatHoverKind.Default)
        {
            if (!TryGetIndex(cell, out int index))
                return ClearHover();
            if (index == HoverIndex && state == HoverState && kind == HoverKind)
                return NoCells;

            _changed.Clear();
            if (HoverIndex >= 0 && HoverIndex != index)
                _changed.Add(HoverIndex);
            _changed.Add(index);
            HoverIndex = index;
            HoverState = state;
            HoverKind = kind;
            return _changed;
        }

        internal IReadOnlyList<int> ClearHover()
        {
            if (HoverIndex < 0)
                return NoCells;

            _changed.Clear();
            _changed.Add(HoverIndex);
            HoverIndex = -1;
            HoverState = CombatHoverState.Neutral;
            HoverKind = CombatHoverKind.Default;
            return _changed;
        }

        /// <summary>한 층의 칸을 바꿉니다. 격자 밖은 무시합니다. 칸·경로 순서·영향 경계 변경을 알립니다. 닫힌 경계 인자는 영향 단일 층에서만 씁니다.</summary>
        internal IReadOnlyList<int> SetLayerCells(FieldHighlightLayer layer, IReadOnlyList<CellCoord> cells, bool closeAffectedOutline = false)
        {
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));
            if (closeAffectedOutline && layer != FieldHighlightLayer.Affected)
                throw new ArgumentException("[FieldHighlightState] 닫힌 공격 영향 경계는 Affected 단일 층에만 지정할 수 있습니다.", nameof(layer));

            bool pathChanged = layer == FieldHighlightLayer.MovementDraft && RememberMovementPath(cells);

            for (int i = 0; i < cells.Count; i++)
            {
                if (TryGetIndex(cells[i], out int index))
                    _marks[index] = true;
            }

            _changed.Clear();
            for (int i = 0; i < _layers.Length; i++)
            {
                bool want = _marks[i];
                _marks[i] = false;
                if (((_layers[i] & layer) != 0) == want)
                    continue;

                _layers[i] = want ? _layers[i] | layer : _layers[i] & ~layer;
                _changed.Add(i);
            }

            if ((layer & FieldHighlightLayer.Affected) != 0)
            {
                bool closed = closeAffectedOutline && HasAnyCell(FieldHighlightLayer.Affected);
                bool boundaryChanged = IsAffectedOutlineClosed != closed;
                IsAffectedOutlineClosed = closed;
                // 칸·색이 같아도 경계만 바뀌면 Outline과 채움 가장자리를 다시 그립니다.
                if (boundaryChanged && _changed.Count == 0)
                {
                    for (int i = 0; i < _layers.Length; i++)
                    {
                        if ((_layers[i] & FieldHighlightLayer.Affected) != 0)
                            _changed.Add(i);
                    }
                }
            }

            // 포함된 칸이 같아도 방문 순서가 달라지면 선을 다시 그립니다.
            if (pathChanged && _changed.Count == 0 && _movementPath.Count > 0)
            {
                TryGetIndex(_movementPath[0], out int index);
                _changed.Add(index);
            }
            return _changed;
        }

        private bool RememberMovementPath(IReadOnlyList<CellCoord> cells)
        {
            int count = 0;
            bool same = true;
            for (int i = 0; i < cells.Count; i++)
            {
                if (!TryGetIndex(cells[i], out _))
                    continue;
                if (count >= _movementPath.Count || !_movementPath[count].Equals(cells[i]))
                    same = false;
                count++;
            }
            if (same && count == _movementPath.Count)
                return false;

            _movementPath.Clear();
            for (int i = 0; i < cells.Count; i++)
            {
                if (TryGetIndex(cells[i], out _))
                    _movementPath.Add(cells[i]);
            }
            return true;
        }

        internal IReadOnlyList<int> ClearLayer(FieldHighlightLayer layer)
        {
            if ((layer & FieldHighlightLayer.Affected) != 0)
                IsAffectedOutlineClosed = false;
            if ((layer & FieldHighlightLayer.MovementDraft) != 0)
                _movementPath.Clear();
            _changed.Clear();
            for (int i = 0; i < _layers.Length; i++)
            {
                if ((_layers[i] & layer) == 0)
                    continue;

                _layers[i] &= ~layer;
                _changed.Add(i);
            }

            return _changed;
        }

        /// <summary>배치 드래그 여부를 정합니다. 값이 바뀌었으면 true입니다.</summary>
        internal bool SetPlacementActive(bool active)
        {
            if (IsPlacementActive == active)
                return false;

            IsPlacementActive = active;
            return true;
        }

        /// <summary>경로 지정 여부를 정합니다. 값이 바뀌었으면 true입니다.</summary>
        internal bool SetPathPlanning(bool planning)
        {
            if (IsPathPlanning == planning)
                return false;

            IsPathPlanning = planning;
            return true;
        }

        internal bool SetHoverRangeSourceCells(IReadOnlyList<CellCoord> cells)
        {
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));
            bool same = cells.Count == _hoverRangeSourceCells.Count;
            for (int i = 0; same && i < cells.Count; i++)
                same = cells[i] == _hoverRangeSourceCells[i];
            if (same)
                return false;
            _hoverRangeSourceCells.Clear();
            for (int i = 0; i < cells.Count; i++)
                _hoverRangeSourceCells.Add(cells[i]);
            return true;
        }

        private void ResetTransient()
        {
            IsAffectedOutlineClosed = false;
            _hoverRangeSourceCells.Clear();
            HoverIndex = -1;
            HoverState = CombatHoverState.Neutral;
            HoverKind = CombatHoverKind.Default;
            IsPlacementActive = false;
            IsPathPlanning = false;
            _movementPath.Clear();
            _changed.Clear();
        }
    }
}
