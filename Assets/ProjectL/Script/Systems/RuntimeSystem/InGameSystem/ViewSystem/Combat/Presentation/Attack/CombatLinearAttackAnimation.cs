#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
#endif
using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 일반 총알의 발사 시점·반동과 실제 종점 도착을 함께 기다립니다. 총알이 접촉 진행률을 지나면 히트(대상 모두 해제)를 한 번 알립니다(히트 타이밍 설계 H6).
    /// 접촉 진행률은 발사 순간 경로가 정해질 때 호출자가 한 번 계산해 줍니다(몸·기하·대체 정책은 호출자 소유). 없으면 종점 도착입니다.
    /// 히트는 그 접촉 자리(<see cref="CombatProjectileContact"/>)를 함께 넘깁니다(투사체 접촉 이펙트 설계 IE4).
    /// </summary>
    internal sealed class CombatLinearAttackAnimation : CombatAttackMotion
    {
        private readonly CombatAttackPresentationData _settings;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly CombatPlaybackProfile _profile;

#endif
        private readonly CombatProjectilePool _pool;
        private readonly Action _fired;
        private readonly Action<CombatProjectileContact> _hit;
        private readonly Func<Vector3, Vector3, float> _contactProgress;
        private readonly float _fireAt;
        private CombatProjectileLease _lease;
        private float _hitAt = 1f;
        // 발사 때 정한 접촉 자리입니다. 히트가 넘깁니다.
        private CombatProjectileContact _contact;
        private bool _hasFired;
        private bool _flightDone;
        private bool _hitSent;

        /// <param name="fired">발사 순간의 효과음입니다.</param>
        /// <param name="hit">총알이 대상에 닿는 순간의 히트입니다. 접촉 자리를 받습니다. 없으면 알리지 않습니다.</param>
        /// <param name="contactProgress">발사 경로(월드 from, to)에서 히트할 정규화 진행률입니다. 없으면 1(종점)입니다.</param>
        internal CombatLinearAttackAnimation(CombatAttackPresentationData settings, FieldGridMapper mapper,
            CombatProjectilePool pool, ICombatAttackUnitPose unit, Vector3 target, Quaternion emptyFacing,
            Action fired, Action cancelOwner, Action<CombatProjectileContact> hit, Func<Vector3, Vector3, float> contactProgress)
            : base(unit, target, mapper != null ? mapper.WorldUp : throw new ArgumentNullException(nameof(mapper)),
                emptyFacing, settings != null ? settings.ShootDuration : throw new ArgumentNullException(nameof(settings)), cancelOwner)
        {
            if (settings.Kind != CombatAttackPresentationKind.Linear)
                throw new InvalidOperationException("[CombatLinearAttackAnimation] 직선 총알 프로필이 필요합니다.");
            _settings = settings;
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _profile = pool.ProfilingProfile;
#endif
            _fired = fired ?? throw new ArgumentNullException(nameof(fired));
            _hit = hit;
            _contactProgress = contactProgress;
            _fireAt = settings.ShootDuration * settings.FireTime;
        }

        public override bool IsComplete => IsRunning && PoseDone && _flightDone;
        protected override bool ResourcesAvailable => !_hasFired || _flightDone || (_lease != null && _lease.IsActive);

        protected override void Render(float progress)
        {
            float recoil = Recoil(progress, _settings.FireTime);
            Unit.ApplyPose(Origin - Forward * (_settings.RecoilDistance * recoil),
                Facing * Quaternion.Euler(-_settings.RecoilAngle * recoil, 0f, 0f));
        }

        protected override void Advance(float previous, float next)
        {
            if (_hasFired || next < _fireAt || !IsRunning) return;
            Render(_settings.FireTime);
            int version = Version;
            // 발사 높이·출발 오프셋 원본은 발사 기하 한 곳입니다(궤적 미리보기와 공유, 공격 궤적 설계 TJ3).
            Vector3 to = CombatShotPath.Raise(Target, Up, _settings.ProjectileHeight);
            Vector3 from = CombatShotPath.LinearStart(CombatShotPath.Raise(Origin, Up, _settings.ProjectileHeight), to);
            if (_contactProgress != null)
            {
                float contact = _contactProgress(from, to);
                if (!IsCurrent(version)) return;
                if (!float.IsFinite(contact))
                    throw new InvalidOperationException("[CombatLinearAttackAnimation] 히트 진행률이 유한하지 않습니다.");
                _hitAt = Mathf.Clamp01(contact);
            }
            // 접촉 자리는 경로가 정해지는 지금 한 번 만듭니다(투사체 접촉 이펙트 설계 IE3).
            _contact = CombatProjectileContact.Along(from, to, _hitAt, Up, Facing);
            // 진행률 알림이 있는 발사입니다. 방향 기준 위쪽은 기존 직선 발사와 같은 월드 위쪽입니다(표시 회전 유지).
            // 진행률은 완급 곡선을 지난 경로 진행률이라 접촉 진행률(_hitAt)과 그대로 비교합니다(투사체 완급 설계 EZ4).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _pool.ProfilingProfile = _profile;
#endif
            var lease = _pool.FireLinear(from, to, _settings.ProjectileSpeed, Vector3.up, progress =>
            {
                if (IsCurrent(version) && progress >= _hitAt) SendHit();
            }, () =>
            {
                if (!IsCurrent(version)) return;
                _lease = null;
                _flightDone = true;
                SendHit();
            }, _settings.ProjectileEasing);
            if (!IsCurrent(version))
            {
                _pool.Recall(lease);
                return;
            }
            _lease = lease;
            _hasFired = true;
            _fired();
        }

        private void SendHit()
        {
            if (_hitSent) return;
            _hitSent = true;
            _hit?.Invoke(_contact);
        }

        protected override void ReleaseResources()
        {
            var lease = _lease;
            _lease = null;
            _pool.Recall(lease);
        }
    }
}
