using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist
{
    /// <summary>환경 설명·현재 영역과 클릭한 토네이도 도착 영역의 표시입니다. 칸 집기와 표시 수명을 공유하며 규칙·Buffer를 변경하지 않습니다.</summary>
    internal sealed class CombatEnvironmentHover
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionEnvironmentHoverPointerMoved = new("Combat.Action.Interaction.EnvironmentHover.PointerMoved");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionEnvironmentHoverPin = new("Combat.Action.Interaction.EnvironmentHover.Pin");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionEnvironmentHoverUnpin = new("Combat.Action.Interaction.EnvironmentHover.Unpin");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionEnvironmentHoverPoll = new("Combat.Action.Interaction.EnvironmentHover.Poll");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionEnvironmentHoverClear = new("Combat.Action.Interaction.EnvironmentHover.Clear");
#endif

        private readonly ICombatPointerPicker _picker;
        private readonly ICombatPointerOcclusion _occlusion;
        private readonly CombatEnvironmentText _formatter;
        private readonly IUITextPresentation _text;
        private readonly ICombatGimmickRangeFeedback _range;
        private readonly CombatNexusRangeHover _nexusRange;
        private readonly List<CellCoord> _rangeCells = new(16);
        private readonly List<CellCoord> _selectedCells = new(16);
        private CellCoord? _pendingClickCell;
        private CombatStateData _selectionState;
        private int _selectionSequence;
        private bool _selectionShown;
        private float _screenX;
        private float _screenY;
        // 설명 창에 마지막으로 넘긴 화면 위치. 보이는 동안(_shown)만 뜻이 있다.
        private float _movedX;
        private float _movedY;
        private bool _hasPointer;
        private bool _cached;
        private bool _shown;
        private bool _rangeShown;
        private CombatStateData _cachedState;
        private CellCoord _cell;
        private int _sequence;
        private uint _observedHideVersion;
        private uint _pointerHideVersion;

        internal CombatEnvironmentHover(ICombatPointerPicker picker, ICombatPointerOcclusion occlusion,
            CombatEnvironmentText formatter, IUITextPresentation text, ICombatGimmickRangeFeedback range,
            CombatNexusRangeHover nexusRange)
        {
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _occlusion = occlusion ?? throw new ArgumentNullException(nameof(occlusion));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _range = range ?? throw new ArgumentNullException(nameof(range));
            _nexusRange = nexusRange ?? throw new ArgumentNullException(nameof(nexusRange));
        }

        internal void PointerMoved(float screenX, float screenY)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionEnvironmentHoverPointerMovedSample = _profileInteractionEnvironmentHoverPointerMoved.Auto();
#endif

            _screenX = screenX;
            _screenY = screenY;
            _hasPointer = true;
            _pointerHideVersion = _text.HoverHideVersion;
        }

        /// <summary>기존 행동과 유닛 선택 뒤 남은 빈 칸 클릭만 받습니다. 현재 사실은 다음 Refresh에서 읽습니다.</summary>
        internal void SelectPressed(float screenX, float screenY, IReadOnlyList<CharacterData> characters, bool canSelect)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionEnvironmentHoverPinSample = _profileInteractionEnvironmentHoverPin.Auto();
#endif

            if (characters == null) throw new ArgumentNullException(nameof(characters));
            CancelSelection();
            PointerMoved(screenX, screenY);
            _cached = false;
            if (!canSelect || _occlusion.IsPointerOverUi(screenX, screenY)
                || !_picker.TryPickCell(screenX, screenY, out CellCoord cell)
                || CombatCharacterQueries.FindAnyAt(characters, cell) != null)
                return;
            _pendingClickCell = cell;
        }

        internal void CancelSelection()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionEnvironmentHoverUnpinSample = _profileInteractionEnvironmentHoverUnpin.Auto();
#endif

            if (_pendingClickCell.HasValue || _selectedCells.Count > 0) _cached = false;
            _pendingClickCell = null;
            _selectedCells.Clear();
            _selectionState = null;
            _selectionShown = false;
        }

        internal void Refresh(CombatStateData state, int resultSequence, bool allowed, bool canSelect = true)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionEnvironmentHoverPollSample = _profileInteractionEnvironmentHoverPoll.Auto();
#endif

            if (!allowed || state == null)
            {
                CancelSelection();
                Hide();
                return;
            }
            if (!canSelect || (_selectionState != null
                && (!ReferenceEquals(_selectionState, state) || _selectionSequence != resultSequence)))
                CancelSelection();
            if (_pendingClickCell.HasValue)
            {
                CombatTornadoDestinationCells.Collect(state, _pendingClickCell.Value, _selectedCells);
                _pendingClickCell = null;
                if (_selectedCells.Count > 0)
                {
                    _selectionState = state;
                    _selectionSequence = resultSequence;
                }
                _cached = false;
            }
            uint hideVersion = _text.HoverHideVersion;
            if (_observedHideVersion != hideVersion)
            {
                _observedHideVersion = hideVersion;
                _cached = false;
                _cachedState = null;
                _shown = false;
                _selectionShown = false;
                ShowSelectionOrHideRange();
            }
            // 외부 UnHover 뒤에는 그 이후에 도착한 포인터 입력만 사용합니다.
            if (_hasPointer && _pointerHideVersion != hideVersion) _hasPointer = false;
            if (!_hasPointer || _occlusion.IsPointerOverUi(_screenX, _screenY)
                || !_picker.TryPickCell(_screenX, _screenY, out CellCoord cell))
            {
                Hide();
                return;
            }
            if (_cached && ReferenceEquals(_cachedState, state) && _cell == cell && _sequence == resultSequence)
            {
                // 포인터가 멈춰 있으면 옮기지 않는다. 창은 넘겨받은 위치를 스스로 기억해 보정한다.
                if (_shown && (_movedX != _screenX || _movedY != _screenY)) MoveHover();
                return;
            }

            var content = _formatter.Build(state, cell);
            if (content.Description.Length == 0) HideText();
            else
            {
                _text.ShowHover(content.Name, content.Description);
                _shown = true;
                MoveHover();
            }
            // 클릭 고정은 호버 이동과 독립적입니다. 설명·유닛·조준 표시는 기존 규칙을 유지합니다.
            if (_selectedCells.Count > 0) ShowSelectionOrHideRange();
            else
            {
                CombatGimmickHoverCells.Collect(state.ReadTornadoAreas(Owner.Player), state.Gimmicks.Entries, cell, _rangeCells);
                CombatDustScreenHoverCells.Append(state.Environment.Tiles, cell, _rangeCells);
                if (_rangeCells.Count == 0) HideRange();
                else
                {
                    _range.ShowGimmickRange(_rangeCells);
                    _rangeShown = true;
                    _selectionShown = false;
                }
            }
            _nexusRange.Refresh(state, cell);
            _cell = cell;
            _sequence = resultSequence;
            _cachedState = state;
            _cached = true;
        }

        /// <summary>UnHover·포커스 손실·씬 해제입니다. 이후 새 포인터 입력 전에는 다시 열지 않습니다.</summary>
        internal void Clear()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionEnvironmentHoverClearSample = _profileInteractionEnvironmentHoverClear.Auto();
#endif

            CancelSelection();
            _hasPointer = false;
            Hide();
        }

        private void MoveHover()
        {
            _text.MoveHover(new Vector2(_screenX, _screenY));
            _movedX = _screenX;
            _movedY = _screenY;
        }

        private void Hide()
        {
            _cached = false;
            _cachedState = null;
            _nexusRange.Clear();
            ShowSelectionOrHideRange();
            HideText();
        }

        private void ShowSelectionOrHideRange()
        {
            if (_selectedCells.Count == 0) HideRange();
            else if (!_selectionShown)
            {
                _range.ShowGimmickRange(_selectedCells);
                _rangeShown = true;
                _selectionShown = true;
            }
        }

        private void HideRange()
        {
            _rangeCells.Clear();
            _selectionShown = false;
            if (!_rangeShown) return;
            _range.ClearGimmickRange();
            _rangeShown = false;
        }

        private void HideText()
        {
            if (!_shown) return;
            _text.UnHover();
            _shown = false;
            // 내부의 일시 숨김은 포인터를 유지해 연출 종료 뒤 다시 표시할 수 있게 합니다.
            _observedHideVersion = _text.HoverHideVersion;
            _pointerHideVersion = _observedHideVersion;
        }
    }
}
