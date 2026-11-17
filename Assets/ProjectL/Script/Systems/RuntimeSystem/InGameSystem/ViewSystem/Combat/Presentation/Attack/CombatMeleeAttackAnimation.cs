using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 원본 박치기의 준비·타격·복귀 자세를 재생합니다. 타격 순간(진행 55%)에 효과음과 히트(대상 모두 해제)를 알립니다(히트 타이밍 설계 H6).
    /// 공격 완료는 복귀까지 끝난 뒤입니다. 피해 표시는 히트를 받은 호출자가 진행합니다.
    /// </summary>
    internal sealed class CombatMeleeAttackAnimation : CombatAttackMotion
    {
        private readonly CombatAttackPresentationData _settings;
        private readonly Action _impact;
        private readonly Action _hit;
        private bool _impacted;

        /// <param name="impact">타격 순간의 효과음입니다.</param>
        /// <param name="hit">타격 순간의 히트입니다. 없으면(포효) 알리지 않습니다.</param>
        internal CombatMeleeAttackAnimation(CombatAttackPresentationData settings, FieldGridMapper mapper,
            ICombatAttackUnitPose unit, Vector3 target, Quaternion emptyFacing, Action impact, Action cancelOwner,
            Action hit)
            : base(unit, target, mapper != null ? mapper.WorldUp : throw new ArgumentNullException(nameof(mapper)),
                emptyFacing, settings != null ? settings.MeleeDuration : throw new ArgumentNullException(nameof(settings)), cancelOwner)
        {
            if (settings.Kind != CombatAttackPresentationKind.Melee)
                throw new InvalidOperationException("[CombatMeleeAttackAnimation] 근접 박치기 프로필이 필요합니다.");
            _settings = settings;
            _impact = impact ?? throw new ArgumentNullException(nameof(impact));
            _hit = hit;
        }

        public override bool IsComplete => IsRunning && PoseDone && _impacted;
        protected override void Advance(float previous, float next)
        {
            if (_impacted || next < _settings.MeleeDuration * .55f) return;
            Render(.55f);
            _impacted = true;
            int version = Version;
            _impact();
            // 효과음 콜백이 취소했으면 히트를 알리지 않습니다.
            if (IsCurrent(version)) _hit?.Invoke();
        }

        protected override void Render(float progress)
        {
            float pitch;
            float offset;
            if (progress < .3f)
            {
                float ease = Mathf.SmoothStep(0f, 1f, progress / .3f);
                pitch = -_settings.MeleeWindupAngle * ease;
                offset = -_settings.MeleePullback * ease;
            }
            else if (progress < .55f)
            {
                float ease = Mathf.SmoothStep(0f, 1f, (progress - .3f) / .25f);
                pitch = Mathf.Lerp(-_settings.MeleeWindupAngle, _settings.MeleeImpactAngle, ease);
                offset = Mathf.Lerp(-_settings.MeleePullback, _settings.MeleeLunge, ease);
            }
            else
            {
                float ease = Mathf.SmoothStep(0f, 1f, (progress - .55f) / .45f);
                pitch = Mathf.Lerp(_settings.MeleeImpactAngle, 0f, ease);
                offset = Mathf.Lerp(_settings.MeleeLunge, 0f, ease);
            }
            Unit.ApplyPose(Origin + Forward * offset, Facing * Quaternion.Euler(pitch, 0f, 0f));
        }
    }
}
