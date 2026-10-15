using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 배치 단계의 Drag 입력입니다(설계 §7, 계획 E.2·E.3, 구조 개선 VR3).
    /// 손패·필드의 아군 유닛을 집어 칸에 놓거나 손패로 되돌리고, 고스트·원본 숨김·배치 호버와 배치 미리보기 요청 번호를 소유합니다.
    /// </summary>
    /// <remarks>
    /// 배치 가능 여부는 Core 미리보기 응답만 따르며 계산하지 않습니다. 명령은 Gate로 제출하고 대기는 기록하지 않습니다.
    /// 공격 조준·경로·시작/턴 종료 대기는 다루지 않습니다. 미리보기는 Buffer의 최신 요청이 자기 번호일 때만 비웁니다.
    /// </remarks>
    internal sealed class PlacementDragInteraction
    {
        // 누른 자리에서 이만큼(픽셀) 안에서 떼면 클릭으로 보고, 집은 채로 다음 누름·뗌에서 놓는다(옛 배치 입력과 같음).
        private const float ClickMovementThreshold = 5f;

        private readonly ICombatViewBufferWriteSink _input;
        private readonly ICombatViewBufferReadSink _requests;
        private readonly ICombatPointerPicker _picker;
        private readonly ICombatInteractionFeedback _feedback;
        private readonly CombatCommandGate _gate;
        private readonly CombatHoverTracker _hover;

        private bool _dragging;
        private EntityId _dragEntity;
        private bool _dragIsMain;
        private bool _dragFromField;
        private Rotation4 _dragRotation;
        private bool _pressStartedDrag;
        private float _pressX;
        private float _pressY;
        private int _previewRequestNumber;

        internal PlacementDragInteraction(
            ICombatViewBufferWriteSink input,
            ICombatViewBufferReadSink requests,
            ICombatPointerPicker picker,
            ICombatInteractionFeedback feedback,
            CombatCommandGate gate)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _hover = new CombatHoverTracker(feedback);
        }

        /// <summary>유닛을 집고 있는지입니다.</summary>
        internal bool IsDragging => _dragging;

        internal void PointerMoved(in CombatInputFacts facts, float screenX, float screenY)
        {
            if (_dragging)
            {
                UpdateDragHover(facts, screenX, screenY);
                return;
            }

            // 규칙이 필요 없는 호버 칸 강조는 바로 그린다(설계 §5.4).
            if (_picker.TryPickCell(screenX, screenY, out CellCoord cell))
                _hover.Set(cell, CombatHoverState.Neutral);
            else
                _hover.Clear();
        }

        internal void SelectPressed(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            _pressX = screenX;
            _pressY = screenY;
            _pressStartedDrag = false;

            // 클릭으로 집은 상태면 이어지는 뗌에서 놓는다.
            if (_dragging)
                return;

            TryBeginPlacement(facts, characters, screenX, screenY);
        }

        internal void SelectReleased(in CombatInputFacts facts, float screenX, float screenY)
        {
            if (!_dragging)
                return;

            if (_pressStartedDrag && IsClick(screenX, screenY))
            {
                _pressStartedDrag = false;
                return;
            }

            CommitDrag(facts, screenX, screenY);
            EndDrag();
        }

        /// <summary>
        /// 자기 배치 미리보기 요청의 응답이면 호버 가능 여부와 고스트 색을 바꾸고 true를 돌려줍니다. 아니면 false입니다.
        /// </summary>
        internal bool PreviewAnswered(CombatPreviewResult answer)
        {
            if (_previewRequestNumber == 0 || answer.RequestNumber != _previewRequestNumber)
                return false;

            if (_hover.HasHover && answer.Placement.HasValue)
            {
                bool canPlace = answer.Placement.Value.CanPlace;
                _hover.SetStateIfHovered(canPlace ? CombatHoverState.Valid : CombatHoverState.Invalid);
                if (_dragging)
                    _feedback.SetGhostPlaceable(canPlace);
            }

            return true;
        }

        /// <summary>결과 후처리: 맞지 않게 된 Drag는 끝내고, 남은 Drag는 자기 미리보기를 새 번호로 다시 요청합니다.</summary>
        internal void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            if (!_dragging)
                return;

            if (!IsDragStillValid(facts, characters))
            {
                EndDrag();
                return;
            }

            ClearOwnedPreview();
            if (_hover.HasHover)
                RequestDragPreview(facts, _hover.Cell);
        }

        /// <summary>Drag 중이면 놓지 않고 끝냅니다(취소 키·선택 취소).</summary>
        internal void Cancel()
        {
            EndDrag();
        }

        /// <summary>Drag·고스트·원본 숨김·배치 호버·자기 미리보기를 정리합니다. 정리할 것이 없으면 아무것도 하지 않습니다.</summary>
        internal void CancelPresentation()
        {
            EndDrag();
            _hover.Clear();
        }

        private void TryBeginPlacement(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            CharacterData picked = null;
            bool fromField = false;

            if (_picker.TryPickHandUnit(screenX, screenY, out EntityId handId))
                picked = CombatCharacterQueries.FindPlayerInArea(characters, handId, CharacterRuntimeArea.Hand);

            if (picked == null && _picker.TryPickCell(screenX, screenY, out CellCoord cell))
            {
                picked = CombatCharacterQueries.FindPlayerAt(characters, cell);
                fromField = picked != null;
            }

            if (picked == null)
                return;

            bool isMain = picked.OwnershipRole == CharacterOwnershipRole.Main;
            CombatViewPreviewKind kind = isMain ? CombatViewPreviewKind.MainCharacterPlacement : CombatViewPreviewKind.UnitPlacement;
            if (!CombatInputPolicy.CanPreview(facts, _gate.HasPending, kind))
                return;

            BeginDrag(picked, isMain, fromField);
            UpdateDragHover(facts, screenX, screenY);
        }

        private void BeginDrag(CharacterData character, bool isMain, bool fromField)
        {
            _dragging = true;
            _dragEntity = character.EntityId;
            _dragIsMain = isMain;
            _dragFromField = fromField;
            _dragRotation = character.Rotation ?? Rotation4.R0;
            _pressStartedDrag = true;
            _hover.Clear();

            _feedback.ShowGhost(character.CharacterKey);
            _feedback.SetGhostPlaceable(true);
            _feedback.HideOriginal(character.EntityId);
        }

        private void UpdateDragHover(in CombatInputFacts facts, float screenX, float screenY)
        {
            if (!_picker.TryPickCell(screenX, screenY, out CellCoord cell))
            {
                _feedback.MoveGhostToPointer(screenX, screenY);
                _hover.Clear();
                return;
            }

            _feedback.MoveGhostToCell(cell);
            if (_hover.HasHover && _hover.Cell == cell)
                return;

            // 칸이 바뀔 때만 미리보기를 요청한다(계획 E.3).
            _hover.Set(cell, CombatHoverState.Neutral);
            RequestDragPreview(facts, cell);
        }

        private void RequestDragPreview(in CombatInputFacts facts, CellCoord cell)
        {
            CombatViewPreviewData preview = _dragIsMain
                ? CombatViewPreviewData.MainCharacterPlacement(cell, _dragRotation)
                : CombatViewPreviewData.UnitPlacement(_dragEntity, cell, _dragRotation);
            _feedback.SetGhostPlaceable(true);

            if (CombatInputPolicy.CanPreview(facts, _gate.HasPending, preview.Kind))
                _previewRequestNumber = _input.SetPreview(preview);
        }

        private void CommitDrag(in CombatInputFacts facts, float screenX, float screenY)
        {
            // 필드에서 집은 일반 유닛만 손패로 되돌릴 수 있다. 메인 캐릭터는 손패로 가지 않는다.
            if (_dragFromField && !_dragIsMain && _picker.IsOverHandDropZone(screenX, screenY))
            {
                _gate.TrySubmit(facts, CombatViewCommandData.ReturnUnit(_dragEntity));
                return;
            }

            if (!_picker.TryPickCell(screenX, screenY, out CellCoord cell))
                return;

            _gate.TrySubmit(facts, _dragIsMain
                ? CombatViewCommandData.PlaceMainCharacter(cell, _dragRotation)
                : CombatViewCommandData.PlaceUnit(_dragEntity, cell, _dragRotation));
        }

        private void EndDrag()
        {
            if (!_dragging)
                return;

            _dragging = false;
            _dragEntity = EntityId.Empty;
            _dragIsMain = false;
            _dragFromField = false;
            _pressStartedDrag = false;

            _feedback.HideGhost();
            _feedback.ShowOriginal();
            _hover.Clear();
            ClearOwnedPreview();
        }

        /// <summary>Buffer의 최신 미리보기 요청이 자기 번호일 때만 비웁니다. 다른 소유자의 요청은 지우지 않습니다.</summary>
        private void ClearOwnedPreview()
        {
            if (_previewRequestNumber != 0 && _requests.HasPreview && _requests.Preview.Number == _previewRequestNumber)
                _input.ClearPreview();
            _previewRequestNumber = 0;
        }

        private bool IsDragStillValid(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            CharacterData dragged = CombatCharacterQueries.Find(characters, _dragEntity);
            if (dragged == null || dragged.Health.IsDead)
                return false;

            return facts.CurrentLayer == CombatInputPolicy.LayerOf(StagePhase.Deployment)
                && dragged.Area == (_dragFromField ? CharacterRuntimeArea.Field : CharacterRuntimeArea.Hand);
        }

        private bool IsClick(float screenX, float screenY)
        {
            float dx = screenX - _pressX;
            float dy = screenY - _pressY;
            return (dx * dx) + (dy * dy) <= ClickMovementThreshold * ClickMovementThreshold;
        }
    }
}
