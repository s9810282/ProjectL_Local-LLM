using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>이동 초안·끝점과 범위 층, 원본 흐림·커서 고스트 표시를 조정합니다. 경로 입력·범위 추적이 쓰는 이동 계약의 주인입니다(씬 표시 나누기 설계 DS2).</summary>
    /// <remarks>
    /// 해제 뒤 규칙(D9)은 자기 해제 상태(<c>_released</c>)로 지킵니다(설계 P4, 계획 TP-5). 정리·해제는 표시의 목록이 <see cref="Clear"/>·<see cref="Release"/>로 부릅니다.
    /// 커서 고스트가 설 자리는 포인터 매퍼가 화면 좌표에서 구합니다(설계 P9).
    /// </remarks>
    internal sealed class CombatMovementPlanningPresenter : ICombatMovementFeedback
    {
        private readonly FieldView _field;
        private readonly List<CellCoord> _destinations = new List<CellCoord>();
        private bool _hasRange;
        private bool _released;
        private readonly CombatUnitPresentationWindow _presentation;
        private readonly PlacementGhost _ghost;
        private readonly StageFieldViewData _config;
        private bool _editing;
        // 이동 원본 흐림 대여입니다(외형 층, 연출 조립 설계 B13). 편집 시작 ~ 끝까지입니다.
        private ICombatUnitAppearance _originOpacity;
        private readonly CombatPointerMapper _pointer;

        /// <param name="presentation">이동 원본 흐림(외형 층)을 빌릴 유닛 표시 창구입니다(설계 B13).</param>
        /// <param name="pointer">커서 고스트가 설 자리를 화면 좌표에서 구하는 포인터 매퍼입니다(씬 표시 나누기 설계 P9).</param>
        /// <remarks>협력자는 모두 필수입니다. 옛 인자 하나 생성자(협력자 넷을 비워 두던 것)는 부르는 곳이 없어 지웠습니다(씬 표시 나누기 검토 L2).</remarks>
        internal CombatMovementPlanningPresenter(FieldView field,
            CombatUnitPresentationWindow presentation, PlacementGhost ghost, StageFieldViewData config, CombatPointerMapper pointer)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _ghost = ghost != null ? ghost : throw new ArgumentNullException(nameof(ghost));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
        }

        #region 이동 계약 (ICombatMovementFeedback)

        void ICombatMovementFeedback.ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate) =>
            ShowDraft(path, preview, pointerValid, candidate);

        void ICombatMovementFeedback.ClearDraft() => ClearDraft();

        void ICombatMovementFeedback.SetPathPlanning(bool planning) => SetPathPlanning(planning);

        void ICombatMovementRangeFeedback.ShowMovementRange(MovementRangeResult range) => ShowRange(range);

        void ICombatMovementRangeFeedback.ClearMovementRange()
        {
            if (!_released)
                ClearRange();
        }

        void ICombatMovementFeedback.BeginMovementPlanning(EntityId actor, EntityKey key, Rotation4 rotation) =>
            BeginEditing(actor, key, rotation);

        void ICombatMovementFeedback.UpdateMovementPointer(Vector2? screenPosition, bool? valid, bool frozen)
        {
            if (_released) return;
            // 이동 고스트는 포인터가 가리키는 칸에 설 자리를 따라갑니다(솟은 칸은 윗면, 들어간 칸은 타일 윗면, 고지대 View 계획 P14·타일 높이 계획 §8.7 L6).
            Vector3? point = !frozen && screenPosition.HasValue && _pointer.TryRayToStandingPoint(screenPosition.Value.x,
                screenPosition.Value.y, out Vector3 world) ? world : (Vector3?)null;
            UpdatePointer(point, !valid.HasValue ? CombatHoverState.Neutral
                : valid.Value ? CombatHoverState.Valid : CombatHoverState.Invalid, frozen);
        }

        void ICombatMovementFeedback.EndMovementPlanning()
        {
            if (!_released)
                EndEditing();
        }

        #endregion

        /// <summary>편집 시작 여부와 관계없이 받은 초안과 끝점의 유효 상태를 표시합니다.</summary>
        private void ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate)
        {
            if (_released)
                return;

            _field.SetLayerCells(FieldHighlightLayer.MovementDraft, path.Cells);
            CombatHoverState endpoint = !pointerValid ? CombatHoverState.Invalid
                : !preview.HasValue ? CombatHoverState.Neutral
                : preview.Value.CanMove ? CombatHoverState.Valid : CombatHoverState.Invalid;
            _field.SetHoverCell(candidate ?? path.Endpoint, endpoint);
        }

        /// <summary>초안·끝점만 지웁니다. 범위·체력 예상 표시와 편집 종료는 별도 단계입니다.</summary>
        private void ClearDraft()
        {
            if (_released)
                return;

            _field.ClearLayer(FieldHighlightLayer.MovementDraft);
            _field.ClearHoverCell();
        }

        private void SetPathPlanning(bool planning)
        {
            // 격자 아웃라인 모드는 경로 지정 중을 행동 중으로 보고 호버·층 테두리를 그린다(시작 칸을 고르기 전부터).
            if (!_released)
                _field.SetPathPlanning(planning);
        }

        private void BeginEditing(EntityId actor, EntityKey key, Rotation4 rotation)
        {
            if (_released || _field.Mapper == null) return;
            EndEditing();
            _editing = true;
            try
            {
                _originOpacity = _presentation.BeginMovementOpacity(actor, _config.MovementOriginalOpacity);
                _ghost.ShowMovement(key, FieldUnitPresenter.ModelRotation(Owner.Player, rotation, _field.Mapper));
            }
            catch (Exception error)
            {
                try
                {
                    EndEditing();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
        }

        private void UpdatePointer(Vector3? point, CombatHoverState state, bool frozen)
        {
            if (_released || !_editing) return;
            _ghost.SetMovementTint(state);
            // 확정 직전 월드 자세를 유지한다. 카메라 이동도 동결 위치를 바꾸지 않는다.
            if (frozen) return;
            if (point.HasValue) _ghost.MoveTo(point.Value);
            _ghost.SetMovementVisible(point.HasValue);
        }

        private void EndEditing()
        {
            if (!_editing) return;
            _editing = false;
            RunAll(RestoreOriginOpacity, _ghost.Hide);
        }

        private void RestoreOriginOpacity()
        {
            ICombatUnitAppearance opacity = _originOpacity;
            _originOpacity = null;
            opacity?.Restore();
        }

        internal void Clear() => RunAll(EndEditing, ClearRange);

        private void ShowRange(MovementRangeResult range)
        {
            if (_released)
                return;

            _destinations.Clear();
            if (range.Succeeded)
            {
                foreach (MovementDestination destination in range.Destinations.Span)
                    _destinations.Add(destination.TargetAnchor);
            }
            // 실패 응답도 빈 범위로 적용하며 실제 이동 승인은 계산하지 않는다.
            _hasRange = true;
            _field.SetLayerCells(FieldHighlightLayer.MoveRange, _destinations);
        }

        private void ClearRange()
        {
            if (!_hasRange)
                return;

            _hasRange = false;
            _destinations.Clear();
            _field.ClearLayer(FieldHighlightLayer.MoveRange);
        }

        internal void Release()
        {
            if (_released)
                return;

            _released = true;
            Clear();
        }

        private static void RunAll(params Action[] actions) =>
            CleanupErrors.RunAll("[CombatMovementPlanningPresenter] 이동 표시 정리에 실패했습니다.", actions);
    }
}
