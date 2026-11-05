using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>배치 중 고스트·격자와 필드 원본의 표시 의도를 함께 적용합니다.</summary>
    /// <remarks>
    /// 원본 대상과 고스트 자원은 기존 View가 소유하며, 공개 호출 방어와 최종 해제는 Display가 맡습니다.
    /// 손패 유닛은 유지 덱 카드로 고르고 그 카드는 숨기지 않으므로, 원본 숨김은 필드 유닛만 합니다(View 단계 설계 W4).
    /// </remarks>
    internal sealed class CombatPlacementFeedback
    {
        private readonly FieldView _field;
        private readonly PlacementGhost _ghost;
        private readonly FieldUnitPresenter _units;

        internal CombatPlacementFeedback(FieldView field, PlacementGhost ghost, FieldUnitPresenter units)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _ghost = ghost != null ? ghost : throw new ArgumentNullException(nameof(ghost));
            _units = units != null ? units : throw new ArgumentNullException(nameof(units));
        }

        internal void ShowGhost(EntityKey entityKey)
        {
            _ghost.Show(entityKey);
            // 고스트가 보이는 동안이 배치 드래그다(격자 아웃라인 설계 F6).
            _field.SetPlacementActive(true);
        }

        internal void MoveGhostToCell(CellCoord cell)
        {
            FieldGridMapper mapper = _field.Mapper;
            if (mapper != null && mapper.IsInside(cell))
                _ghost.MoveTo(mapper.CellSurfaceToWorld(cell));
        }

        internal void MoveGhostToWorld(Vector3 point) => _ghost.MoveTo(point);

        internal void HideGhost()
        {
            _ghost.Hide();
            _field.SetPlacementActive(false);
        }

        internal void HideOriginal(EntityId entityId)
        {
            // 필드에 그 개체가 있을 때만 실제로 숨긴다. 호출자는 필드·손패 위치를 구분하지 않는다.
            _units.SetHiddenEntity(entityId);
        }

        internal void ShowOriginal() => _units.ClearHiddenEntity();

        internal void SetGhostPlaceable(bool placeable) => _ghost.SetPlaceable(placeable);
    }
}
