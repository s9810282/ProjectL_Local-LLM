#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatInputProfiler = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.CombatInputProfiler;
#endif
using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 배치 단계의 Drag 입력입니다(설계 §7, 계획 E.2·E.3, 구조 개선 VR3).
    /// 필드에서 집거나 유지 덱 카드로 고른 아군 유닛을 칸에 놓고, 고스트·원본 숨김·배치 호버와 배치 미리보기 요청 번호를 소유합니다.
    /// 손패로 되돌리기는 우클릭(<see cref="TryReturnUnderPointer"/>)입니다.
    /// </summary>
    /// <remarks>
    /// 배치 가능 여부는 Core 미리보기 응답만 따르며 계산하지 않습니다. 명령은 Gate로 제출하고 대기는 기록하지 않습니다.
    /// 공격 조준·경로·시작/턴 종료 대기는 다루지 않습니다. 미리보기는 Buffer의 최신 요청이 자기 번호일 때만 비웁니다.
    /// </remarks>
    internal sealed class PlacementDragInteraction
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionPlacementSelect = new("Combat.Action.Interaction.Placement.Select");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionPlacementDeselect = new("Combat.Action.Interaction.Placement.Deselect");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionPlacementDrag = new("Combat.Action.Interaction.Placement.Drag");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionPlacementPick = new("Combat.Action.Interaction.Placement.Pick");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionPlacementRelease = new("Combat.Action.Interaction.Placement.Release");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionPlacementCancel = new("Combat.Action.Interaction.Placement.Cancel");
        private static readonly Unity.Profiling.ProfilerMarker _profileInteractionPlacementReturn = new("Combat.Action.Interaction.Placement.Return");
#endif

        // 누른 자리에서 이만큼(픽셀) 안에서 떼면 클릭으로 보고, 집은 채로 다음 누름·뗌에서 놓는다(옛 배치 입력과 같음).
        private const float ClickMovementThreshold = 5f;

        private readonly ICombatViewBufferWriteSink _input;
        private readonly ICombatPointerPicker _picker;
        // 고스트·원본 숨김 계약입니다. 호버 칸은 호버 추적이 따로 받습니다(씬 표시 나누기 설계 DS2, 결정 Q-DS2 A).
        private readonly ICombatPlacementFeedback _feedback;
        private readonly CombatCommandGate _gate;
        private readonly CombatHoverTracker _hover;
        // 선택·취소 효과음 창구입니다(View 3라운드 W9-2 검토). 필수입니다(평가 C5-02).
        private readonly CombatInputSounds _sounds;
        private int _dragPreset = CombatSoundPresets.None;

        private bool _dragging;
        private EntityId _dragEntity;
        private bool _dragIsMain;
        private bool _dragFromField;
        private Rotation4 _dragRotation;
        private bool _pressStartedDrag;
        private bool _deckSelection;
        private float _pressX;
        private float _pressY;
        private int _previewRequestNumber;
        // 배치 Layer에서 마지막으로 받은 포인터 위치입니다. 우클릭(취소) 입력은 좌표가 없어 이 위치로 대상을 고릅니다.
        private bool _hasPointer;
        private float _pointerX;
        private float _pointerY;

        internal PlacementDragInteraction(
            ICombatViewBufferWriteSink input,
            ICombatPointerPicker picker,
            ICombatPlacementFeedback feedback,
            ICombatHoverCellFeedback hover,
            CombatCommandGate gate, CombatInputSounds sounds)
        {
            // 자기 요청 비우기는 Buffer의 번호 있는 비우기가 판단하므로 읽기 Sink는 받지 않는다(View 2라운드 W5-4, 3라운드 W12-2).
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _picker = picker ?? throw new ArgumentNullException(nameof(picker));
            _feedback = feedback ?? throw new ArgumentNullException(nameof(feedback));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _hover = new CombatHoverTracker(hover, CombatHoverKind.Default);
            _sounds = sounds ?? throw new ArgumentNullException(nameof(sounds));
            InputOwner = new OwnerAdapter(this);
        }

        /// <summary>입력 기능 묶음의 공통 수명에 참여하는 창구입니다(View 2라운드 W5-1).</summary>
        internal CombatInputOwner InputOwner { get; }

        /// <summary>유닛을 집고 있는지입니다.</summary>
        internal bool IsDragging => _dragging;

        /// <summary>덱의 메인·손패 서브 유닛에 기존 배치 입력 정책을 적용합니다. 실제 배치 규칙은 Core가 판정합니다.</summary>
        internal bool CanSelectDeckUnit(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CharacterInstanceId ownedId)
        {
            CharacterData picked = FindDeckUnit(characters, ownedId);
            if (picked == null) return false;
            CombatViewPreviewKind kind = picked.OwnershipRole == CharacterOwnershipRole.Main
                ? CombatViewPreviewKind.MainCharacterPlacement : CombatViewPreviewKind.UnitPlacement;
            return CombatInputPolicy.CanPreview(facts, _gate.HasPending, kind);
        }

        internal void SelectDeckUnit(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, CharacterInstanceId ownedId)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionPlacementSelectSample = _profileInteractionPlacementSelect.Auto();
#endif

            if (!CanSelectDeckUnit(facts, characters, ownedId)) return;
            CharacterData picked = FindDeckUnit(characters, ownedId);
            EndDrag();
            BeginDrag(picked, picked.OwnershipRole == CharacterOwnershipRole.Main,
                picked.Area == CharacterRuntimeArea.Field);
            _deckSelection = true;
            // 카드의 클릭은 이미 끝났으므로 다음 필드 클릭의 뗌으로 확정합니다.
            _pressStartedDrag = false;
        }

        internal void CancelDeckSelection()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionPlacementDeselectSample = _profileInteractionPlacementDeselect.Auto();
#endif

            if (_deckSelection) Cancel();
        }

        private static CharacterData FindDeckUnit(IReadOnlyList<CharacterData> characters, CharacterInstanceId ownedId)
        {
            if (ownedId.IsEmpty) return null;
            for (int i = 0; i < characters.Count; i++)
            {
                CharacterData unit = characters[i];
                if (unit.OwnedId != ownedId || unit.Owner != Owner.Player || unit.Health.IsDead)
                    continue;
                if (unit.OwnershipRole == CharacterOwnershipRole.Main &&
                    (unit.Area == CharacterRuntimeArea.Field || unit.Area == CharacterRuntimeArea.Hand) ||
                    unit.OwnershipRole == CharacterOwnershipRole.Sub && unit.Area == CharacterRuntimeArea.Hand)
                    return unit;
            }
            return null;
        }

        internal void PointerMoved(in CombatInputFacts facts, float screenX, float screenY)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionPlacementDragSample = _profileInteractionPlacementDrag.Auto();
#endif

            RememberPointer(screenX, screenY);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionPlacementPickSample = _profileInteractionPlacementPick.Auto();
#endif

            RememberPointer(screenX, screenY);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionPlacementReleaseSample = _profileInteractionPlacementRelease.Auto();
#endif

            if (!_dragging)
                return;

            if (_pressStartedDrag && IsClick(screenX, screenY))
            {
                _pressStartedDrag = false;
                return;
            }

            // 앞 결과가 재생되는 동안(P < S)에는 배치 명령이 거절된다. 칸 위에서 놓았으면 놓지 않고 손에 남겨,
            // 클릭으로 집은 상태처럼 재생이 끝난 뒤 다음 누름·뗌에서 놓게 한다(평가 G3-01). 칸 밖에서 놓으면 지금처럼 취소다.
            if (facts.IsPresenting && _picker.TryPickCell(screenX, screenY, out _))
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var profilePreview = CombatInputProfiler.Apply(_dragIsMain ? CombatViewPreviewKind.MainCharacterPlacement : CombatViewPreviewKind.UnitPlacement).Auto();
#endif

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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionPlacementCancelSample = _profileInteractionPlacementCancel.Auto();
#endif

            if (_dragging) _sounds.Play(UnitEvent.UnitCancel, _dragPreset);
            EndDrag();
        }

        /// <summary>
        /// 배치 단계 우클릭: Drag 중이 아니면 마지막 포인터 위치의 필드 일반 유닛을 손패로 되돌리는 명령을 제출합니다.
        /// 메인 캐릭터는 손패로 가지 않습니다. 대상이 있어 제출을 시도했으면 true입니다.
        /// 제출 가능 여부(국면·대기·종료)는 Gate의 Policy가 판단합니다.
        /// </summary>
        internal bool TryReturnUnderPointer(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileInteractionPlacementReturnSample = _profileInteractionPlacementReturn.Auto();
#endif

            if (_dragging || !_hasPointer || characters == null)
                return false;
            if (!_picker.TryPickCell(_pointerX, _pointerY, out CellCoord cell))
                return false;

            CharacterData unit = CombatCharacterQueries.FindPlayerAt(characters, cell);
            if (unit == null || unit.OwnershipRole == CharacterOwnershipRole.Main)
                return false;

            _gate.TrySubmit(facts, CombatViewCommandData.ReturnUnit(unit.EntityId));
            return true;
        }

        /// <summary>마지막으로 받은 포인터 위치입니다(우클릭 회수가 쓰는 같은 값, 가방 설계 P6). 받은 적이 없으면 false입니다.</summary>
        internal bool TryGetLastPointer(out float screenX, out float screenY)
        {
            screenX = _pointerX;
            screenY = _pointerY;
            return _hasPointer;
        }

        private void RememberPointer(float screenX, float screenY)
        {
            _hasPointer = true;
            _pointerX = screenX;
            _pointerY = screenY;
        }

        /// <summary>Drag·고스트·원본 숨김·배치 호버·자기 미리보기를 정리합니다. 정리할 것이 없으면 아무것도 하지 않습니다.</summary>
        internal void CancelPresentation()
        {
            EndDrag();
            _hover.Clear();
        }

        // 포인터로 집는 것은 필드 유닛뿐입니다. 손패 유닛은 유지 덱 카드(SelectDeckUnit)로 고릅니다(View 단계 설계 W4).
        private void TryBeginPlacement(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            if (!_picker.TryPickCell(screenX, screenY, out CellCoord cell))
                return;

            CharacterData picked = CombatCharacterQueries.FindPlayerAt(characters, cell);
            if (picked == null)
                return;

            bool isMain = picked.OwnershipRole == CharacterOwnershipRole.Main;
            CombatViewPreviewKind kind = isMain ? CombatViewPreviewKind.MainCharacterPlacement : CombatViewPreviewKind.UnitPlacement;
            if (!CombatInputPolicy.CanPreview(facts, _gate.HasPending, kind))
                return;

            BeginDrag(picked, isMain, fromField: true);
            UpdateDragHover(facts, screenX, screenY);
        }

        private void BeginDrag(CharacterData character, bool isMain, bool fromField)
        {
            _dragging = true;
            _dragEntity = character.EntityId;
            _dragPreset = _sounds.PresetOf(character);
            _dragIsMain = isMain;
            _dragFromField = fromField;
            _dragRotation = character.Rotation ?? Rotation4.R0;
            _pressStartedDrag = true;
            _hover.Clear();

            _feedback.ShowGhost(character.CharacterKey);
            _feedback.SetGhostPlaceable(true);
            _feedback.HideOriginal(character.EntityId);
            _sounds.Play(UnitEvent.UnitPick, _dragPreset);
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
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                _previewRequestNumber = CombatInputProfiler.SetPreview(_input, preview);
#else
                _previewRequestNumber = _input.SetPreview(preview);
#endif
            else
                ClearOwnedPreview(); // 옛 칸의 늦은 답이 새 칸에 칠해지지 않게 합니다(평가 C10-14).
        }

        private void CommitDrag(in CombatInputFacts facts, float screenX, float screenY)
        {
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
            _deckSelection = false;
            _dragEntity = EntityId.Empty;
            _dragIsMain = false;
            _dragFromField = false;
            _pressStartedDrag = false;

            // 깃발을 먼저 내렸으므로 단계 하나가 실패해도 나머지 정리를 모두 시도한 뒤 모아 던집니다(평가 G3-02).
            CleanupErrors.RunAll("[PlacementDragInteraction] 배치 Drag 정리 중 실패했습니다.",
                _feedback.HideGhost, _feedback.ShowOriginal, _hover.Clear, ClearOwnedPreview);
        }

        /// <summary>Buffer의 최신 미리보기 요청이 자기 번호일 때만 비웁니다. 다른 소유자의 요청은 지우지 않습니다(판단은 Buffer).</summary>
        private void ClearOwnedPreview()
        {
            _input.ClearPreview(_previewRequestNumber);
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

        /// <summary>공통 수명의 단계마다 배치 입력의 어느 메서드인지만 답합니다. 결과 재생 전·확인 뒤·감시에는 할 일이 없습니다.</summary>
        private sealed class OwnerAdapter : CombatInputOwner
        {
            private readonly PlacementDragInteraction _placement;

            internal OwnerAdapter(PlacementDragInteraction placement) => _placement = placement;

            internal override CombatInputRouter.PointerTarget Target => CombatInputRouter.PointerTarget.Placement;
            internal override bool IsActive => _placement.IsDragging;

            internal override void PointerMoved(in CombatPointerInput input) =>
                _placement.PointerMoved(input.Facts, input.ScreenX, input.ScreenY);

            internal override void SelectPressed(in CombatPointerInput input) =>
                _placement.SelectPressed(input.Facts, input.Characters, input.ScreenX, input.ScreenY);

            internal override void SelectReleased(in CombatPointerInput input) =>
                _placement.SelectReleased(input.Facts, input.ScreenX, input.ScreenY);

            internal override void SelectCanceled() => _placement.Cancel();

            internal override bool PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
                _placement.PreviewAnswered(facts.PreviewAnswer);

            internal override void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) =>
                _placement.AfterResult(facts, characters);

            internal override void CancelPresentation() => _placement.CancelPresentation();
        }
    }
}
