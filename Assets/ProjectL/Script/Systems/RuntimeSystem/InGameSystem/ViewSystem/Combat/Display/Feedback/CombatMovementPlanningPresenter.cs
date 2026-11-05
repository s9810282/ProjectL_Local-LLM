using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>이동 초안·끝점과 범위 층, 원본 흐림·커서 고스트 표시를 조정합니다.</summary>
    internal sealed class CombatMovementPlanningPresenter
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

        internal CombatMovementPlanningPresenter(FieldView field)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
        }

        /// <param name="presentation">이동 원본 흐림(외형 층)을 빌릴 유닛 표시 창구입니다(설계 B13).</param>
        internal CombatMovementPlanningPresenter(FieldView field,
            CombatUnitPresentationWindow presentation, PlacementGhost ghost, StageFieldViewData config) : this(field)
        {
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            _ghost = ghost != null ? ghost : throw new ArgumentNullException(nameof(ghost));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>편집 시작 여부와 관계없이 받은 초안과 끝점의 유효 상태를 표시합니다.</summary>
        internal void ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate = null)
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
        internal void ClearDraft()
        {
            if (_released)
                return;

            _field.ClearLayer(FieldHighlightLayer.MovementDraft);
            _field.ClearHoverCell();
        }

        internal void SetPathPlanning(bool planning)
        {
            // 격자 아웃라인 모드는 경로 지정 중을 행동 중으로 보고 호버·층 테두리를 그린다(시작 칸을 고르기 전부터).
            if (!_released)
                _field.SetPathPlanning(planning);
        }

        internal void BeginEditing(EntityId actor, EntityKey key, Rotation4 rotation)
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

        internal void UpdatePointer(Vector3? point, CombatHoverState state, bool frozen = false)
        {
            if (_released || !_editing) return;
            _ghost.SetMovementTint(state);
            // 확정 직전 월드 자세를 유지한다. 카메라 이동도 동결 위치를 바꾸지 않는다.
            if (frozen) return;
            if (point.HasValue) _ghost.MoveTo(point.Value);
            _ghost.SetMovementVisible(point.HasValue);
        }

        internal void EndEditing()
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

        internal void ShowRange(MovementRangeResult range)
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

        internal void ClearRange()
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
