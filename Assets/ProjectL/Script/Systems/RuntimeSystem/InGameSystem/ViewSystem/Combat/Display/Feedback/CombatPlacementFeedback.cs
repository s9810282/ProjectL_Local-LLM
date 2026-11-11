using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>배치 중 고스트·격자와 필드 원본의 표시 의도를 함께 적용합니다. 배치 Drag가 쓰는 계약의 주인입니다(씬 표시 나누기 설계 DS2).</summary>
    /// <remarks>
    /// 원본 대상과 고스트 자원은 기존 View가 소유하며, 최종 해제는 표시의 해제 목록이 고스트·유닛 표시에 직접 맡깁니다.
    /// 해제 뒤 규칙(D9)은 공유 수명(<see cref="CombatDisplayLifetime"/>)이 해제되면 아무것도 하지 않는 것으로 지킵니다(계획 TP-3).
    /// 손패 유닛은 유지 덱 카드로 고르고 그 카드는 숨기지 않으므로, 원본 숨김은 필드 유닛만 합니다(View 단계 설계 W4).
    /// </remarks>
    internal sealed class CombatPlacementFeedback : ICombatPlacementFeedback
    {
        private readonly FieldView _field;
        private readonly PlacementGhost _ghost;
        private readonly FieldUnitPresenter _units;
        private readonly CombatPointerMapper _pointer;
        private readonly CombatDisplayLifetime _lifetime;

        /// <param name="pointer">칸 밖 포인터를 고스트가 설 자리로 바꿉니다.</param>
        internal CombatPlacementFeedback(FieldView field, PlacementGhost ghost, FieldUnitPresenter units, CombatPointerMapper pointer,
            CombatDisplayLifetime lifetime)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _ghost = ghost != null ? ghost : throw new ArgumentNullException(nameof(ghost));
            _units = units != null ? units : throw new ArgumentNullException(nameof(units));
            _pointer = pointer ?? throw new ArgumentNullException(nameof(pointer));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        void ICombatPlacementFeedback.ShowGhost(EntityKey entityKey)
        {
            if (_lifetime.IsReleased)
                return;

            _ghost.Show(entityKey);
            // 고스트가 보이는 동안이 배치 드래그다(격자 아웃라인 설계 F6).
            _field.SetPlacementActive(true);
        }

        void ICombatPlacementFeedback.MoveGhostToCell(CellCoord cell)
        {
            if (_lifetime.IsReleased)
                return;

            FieldGridMapper mapper = _field.Mapper;
            if (mapper != null && mapper.IsInside(cell))
                _ghost.MoveTo(mapper.CellSurfaceToWorld(cell));
        }

        void ICombatPlacementFeedback.MoveGhostToPointer(float screenX, float screenY)
        {
            // 칸에 붙은 고스트와 같은 높이로, 포인터가 가리키는 칸에 설 자리를 따라갑니다(타일 높이 계획 §8.7 L6).
            if (!_lifetime.IsReleased && _pointer.TryRayToStandingPoint(screenX, screenY, out Vector3 point))
                _ghost.MoveTo(point);
        }

        void ICombatPlacementFeedback.HideGhost()
        {
            if (_lifetime.IsReleased)
                return;

            _ghost.Hide();
            _field.SetPlacementActive(false);
        }

        void ICombatPlacementFeedback.HideOriginal(EntityId entityId)
        {
            // 필드에 그 개체가 있을 때만 실제로 숨긴다. 호출자는 필드·손패 위치를 구분하지 않는다.
            if (!_lifetime.IsReleased)
                _units.SetHiddenEntity(entityId);
        }

        void ICombatPlacementFeedback.ShowOriginal()
        {
            if (!_lifetime.IsReleased)
                _units.ClearHiddenEntity();
        }

        void ICombatPlacementFeedback.SetGhostPlaceable(bool placeable)
        {
            if (!_lifetime.IsReleased)
                _ghost.SetPlaceable(placeable);
        }
    }
}
