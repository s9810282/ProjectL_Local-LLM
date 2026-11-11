using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 밀리는 동안 젖힌 자세입니다(넉백 피격 연출, 임시 — 2026-11-11 사용자 결정). 걸음 레인 하나가 하나를 갖고, 밀림 걸음을 시작할 때마다 부릅니다.
    /// 젖힘은 표시 회전(<see cref="CombatUnitPresentationWindow.SetFacing"/>)으로 씁니다. 걸음 배치는 위치만 쓰므로 둘이 겹치지 않습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 첫 밀림 걸음에서 피격 반응 곡선의 앞 절반으로 젖히기 시작합니다(<see cref="CombatHitLean.RiseAmount"/>). 피해를 받고 밀리면 그 피격은
    /// 자세를 빌리지 않는 "밀림과 함께 가는 피격"이라(캐릭터 처리기) 맞는 순간 이 걸음이 시작되고, 젖힘이 피격 반응의 젖힘을 대신합니다(2026-11-11 사용자 "피격과 동시에 밀림").
    /// 오르는 동안 걸음이 끝나도 다음 밀림 걸음이 이어서 오릅니다. 이미 표시 회전이 있으면(앞 걸음의 젖힘) 그대로 둡니다.
    /// </para>
    /// <para>
    /// 펴기는 경로 끝 방향 복귀가 합니다. 이동 표시가 밀린 경로 끝에 표시 회전이 남았으면 레인의 펴기(<see cref="CombatUnitMotionPlayback.BeginStraighten"/>)를 부릅니다.
    /// 반응 값이 없거나 Hit Angle이 0인 유닛, 밀림 방향이 없는 걸음(같은 칸)은 젖히지 않습니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatPushLean
    {
        private readonly CombatUnitPresentationWindow _presentation;
        private CombatUnitReactionPresentationData _settings;
        private EntityId _unit;
        private Quaternion _upright;
        private Vector3 _axis;
        private float _elapsed;
        private bool _rising;

        internal CombatPushLean(CombatUnitPresentationWindow presentation) =>
            _presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));

        /// <summary>
        /// 밀림 걸음 하나를 시작합니다. 오르는 중이면 그대로 잇고, 이미 표시 회전이 있으면(앞 걸음의 젖힘) 그대로 두며,
        /// 둘 다 아니면 지금 회전을 똑바른 자세로 삼아 젖히기 시작합니다. 걸음 시작 배치를 그린 뒤 부릅니다(지금 회전이 그 유닛의 사실 회전).
        /// </summary>
        internal void BeginStep(CharacterCombatFacts from, CharacterCombatFacts to, FieldGridMapper mapper)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (_rising && _unit == from.EntityId) return;
            _rising = false;
            EntityId id = from.EntityId;
            if (_presentation.TryGetFacing(id, out _) || !from.Anchor.HasValue || !to.Anchor.HasValue ||
                !_presentation.TryGetReactionSettings(id, out CombatUnitReactionPresentationData settings) || settings.HitAngle <= 0f ||
                !_presentation.TryGetRotation(id, out Quaternion upright))
                return;
            Vector3 direction = Vector3.ProjectOnPlane(
                FieldUnitPresenter.CenterOf(to.OccupiedCells, to.Anchor.Value, mapper)
                - FieldUnitPresenter.CenterOf(from.OccupiedCells, from.Anchor.Value, mapper), mapper.WorldUp);
            if (direction.sqrMagnitude < .000001f) return;
            _settings = settings;
            _unit = id;
            _upright = upright;
            _axis = CombatHitLean.AxisOf(direction.normalized, mapper.WorldUp);
            _elapsed = 0f;
            _rising = true;
            Apply();
        }

        /// <summary>오르는 중이면 걸음이 진행한 시간만큼 더 젖힙니다. 가장 크게 젖히면 멈추고 그 자세를 쥡니다.</summary>
        internal void Tick(float deltaTime)
        {
            if (!_rising) return;
            _elapsed += Math.Max(0f, deltaTime);
            Apply();
            if (_elapsed >= CombatHitLean.RiseSeconds(_settings))
                _rising = false;
        }

        /// <summary>오르기를 멈춥니다. 쓴 표시 회전은 지우지 않습니다(펴기·취소 정리가 지움).</summary>
        internal void Stop()
        {
            _rising = false;
            _settings = null;
        }

        private void Apply() =>
            _presentation.SetFacing(_unit, CombatHitLean.Apply(_upright, _axis, _settings.HitAngle, CombatHitLean.RiseAmount(_elapsed, _settings)));
    }
}
