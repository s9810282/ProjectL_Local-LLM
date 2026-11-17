#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// CatusCannon의 준비·발사·반동·무기 복귀와 곡사 도착을 기다리는 단일 공격 동작입니다. 착탄 순간 히트(대상 모두 해제)를 알립니다(히트 타이밍 설계 H6).
    /// 히트는 착탄 자리(<see cref="CombatProjectileContact"/>)를 함께 넘깁니다. 무엇을 낼지는 받는 쪽이 정합니다(투사체 접촉 이펙트 설계 IE4).
    /// 수명(한 번 시작·Tick·취소·놓기·정리 묶기)은 공통 기반 <see cref="CombatAttackMotion"/>이 맡고, 이 클래스는 무기 자세·발사·착탄만 맡습니다(소리 프리셋·곡사 수명 설계 MT2).
    /// </summary>
    /// <remarks>발사체 풀의 Tick은 호출자가 한 번 전달합니다. 이 동작은 규칙·Buffer·결과 완료를 다루지 않습니다.</remarks>
    internal sealed class CombatMortarAnimation : CombatAttackMotion
    {
        private readonly CombatAttackPresentationData _settings;
        private readonly FieldGridMapper _mapper;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly CombatPlaybackProfile _profile;

#endif
        private readonly CombatProjectilePool _pool;
        private readonly CatusCannonWeaponView _weapon;
        private readonly Action _fired;
        private readonly Action<CombatProjectileContact> _hit;
        private readonly Action _weaponDisabled;
        // 발사 순간(초)입니다. 끝 시각은 기반의 길이 하나입니다(곡사 수명 계획 TP-13).
        private readonly float _fireAt;
        private CombatProjectileLease _lease;
        private int _weaponVersion;
        private bool _hasFired;
        private bool _flightDone;

        /// <param name="emptyFacing">목표가 바로 위·아래일 때 볼 방향입니다. 다른 공격 종류와 같이 공격 방향을 넘깁니다.</param>
        /// <param name="hit">착탄 순간의 히트입니다. 착탄 자리를 받습니다. 없으면 알리지 않습니다.</param>
        internal CombatMortarAnimation(CombatAttackPresentationData settings, FieldGridMapper mapper,
            CombatProjectilePool pool, CatusCannonWeaponView weapon, ICombatAttackUnitPose unit,
            Vector3 target, Quaternion emptyFacing, Action fired, Action cancelOwner, Action<CombatProjectileContact> hit)
            : base(unit, target, mapper != null ? mapper.WorldUp : throw new ArgumentNullException(nameof(mapper)),
                emptyFacing, EndTimeOf(settings), cancelOwner)
        {
            _settings = settings;
            _mapper = mapper;
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _profile = pool.ProfilingProfile;
#endif
            _weapon = weapon != null ? weapon : throw new ArgumentNullException(nameof(weapon));
            _fired = fired ?? throw new ArgumentNullException(nameof(fired));
            _hit = hit;
            _weaponDisabled = HandleUnavailable;
            _fireAt = FireAtOf(settings);
        }

        public override bool IsComplete => IsRunning && PoseDone && _flightDone;

        // 유닛 쓸 수 있음은 기반이 봅니다. 발사 뒤 착탄 전에는 탄 대여가 살아 있어야 합니다.
        protected override bool ResourcesAvailable =>
            CanUseWeapon() && (!_hasFired || _flightDone || (_lease != null && _lease.IsActive));

        // 무기 자세를 빌리고(안에서 Validate) 무기 꺼짐 콜백을 겁니다.
        protected override void BeginResources()
        {
            _weaponVersion = _weapon.BeginPose();
            _weapon.BindCancellation(_weaponDisabled);
        }

        /// <summary>
        /// 경과 초로 그립니다. 기반이 부르는 두 자리(시작·Tick 끝)에서 <paramref name="progress"/>와 <c>Elapsed / 길이</c>가 같은 값이라,
        /// 초 단위인 <see cref="CombatAttackMotion.Elapsed"/>를 그대로 씁니다(곱셈으로 되돌리지 않아 옛 값과 같음, 곡사 수명 계획 TP-12).
        /// </summary>
        protected override void Render(float progress) => Pose(Elapsed);

        protected override void Advance(float previous, float next)
        {
            if (_hasFired || next < _fireAt) return;
            // 큰 프레임에서도 실제 발사 시점의 자세와 피벗을 먼저 샘플링합니다.
            Pose(_fireAt);
            Fire();
        }

        private void Fire()
        {
            if (_hasFired || !IsRunning)
                return;
            int version = Version;
            _weapon.SetReturnProgress(_weaponVersion, 0f, _settings.BucketPitch);
            Vector3 from = _weapon.MuzzlePosition;
            float height = CombatShotPath.ArcHeight(_mapper, _settings);
            // 착탄 자리는 경로가 정해지는 지금 한 번 만듭니다(투사체 접촉 이펙트 설계 IE3).
            CombatProjectileContact landing = CombatProjectileContact.Along(from, Target, 1f, Up, Facing);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _pool.ProfilingProfile = _profile;
#endif
            CombatProjectileLease lease = _pool.FireArc(from, Target, _settings.FlightDuration,
                height, Up, _settings.SpinDegreesPerSecond, () =>
                {
                    if (!IsCurrent(version))
                        return;
                    _lease = null;
                    _flightDone = true;
                    // 착탄이 히트입니다. 무기·몸 복귀는 계속 진행하며 공격 완료는 그 뒤입니다.
                    _hit?.Invoke(landing);
                }, _settings.ProjectileEasing, _settings.ArcRotationMode);
            // SetActive 재진입 취소가 FireArc 반환보다 먼저 와도 방금 만든 대여를 남기지 않습니다.
            if (!IsCurrent(version))
            {
                _pool.Recall(lease);
                return;
            }
            _lease = lease;
            _hasFired = true;
            _fired();
        }

        // 확인 자리 둘은 옛 곡사 그리기와 같습니다(설계 MT3): 그리기 앞에서 쓸 수 없으면 취소, 유닛 자세 뒤에서는 멈추기만 합니다.
        private void Pose(float elapsed)
        {
            if (!Unit.IsAvailable || !CanUseWeapon())
            {
                HandleUnavailable();
                return;
            }
            float shootProgress = Mathf.Clamp01((elapsed - _settings.PrepareDuration) / _settings.ShootDuration);
            float recoil = Recoil(shootProgress, _settings.FireTime);
            Unit.ApplyPose(Origin - Forward * (_settings.RecoilDistance * recoil),
                Facing * Quaternion.Euler(-_settings.RecoilAngle * recoil, 0f, 0f));
            if (!IsRunning || !Unit.IsAvailable || !CanUseWeapon())
                return;
            if (elapsed < _fireAt)
            {
                float prepare = _settings.PrepareDuration > 0f ? elapsed / _settings.PrepareDuration : 1f;
                _weapon.SetPrepareProgress(_weaponVersion, Mathf.Clamp01(prepare), _settings.BucketPitch);
            }
            else
            {
                float returning = _settings.ReturnDuration > 0f ? (elapsed - _fireAt) / _settings.ReturnDuration : 1f;
                _weapon.SetReturnProgress(_weaponVersion, Mathf.Clamp01(returning), _settings.BucketPitch);
            }
        }

        // 파괴된 무기를 먼저 거르는 Unity null 확인(_weapon != null)을 둡니다.
        private bool CanUseWeapon() => _weapon != null && _weapon.IsAvailable && _weapon.IsPoseCurrent(_weaponVersion);

        // 순서: 무기 꺼짐 콜백 풀기 → 탄 회수 → 무기 자세 되돌리기. 유닛 자세는 기반이 그 뒤에 되돌립니다(옛 정리 순서 그대로).
        protected override void ReleaseResources(ref List<Exception> errors)
        {
            CombatProjectileLease lease = _lease;
            _lease = null;
            CleanupErrors.Try(() =>
            {
                if (_weapon != null) _weapon.UnbindCancellation(_weaponDisabled);
            }, ref errors);
            CleanupErrors.Try(() => _pool.Recall(lease), ref errors);
            CleanupErrors.Try(() =>
            {
                if (_weapon != null && _weapon.IsPoseCurrent(_weaponVersion))
                    _weapon.ResetPose();
            }, ref errors);
        }

        // 끝 시각입니다. 기반 생성자 호출식 안에서 설정을 거르며 부릅니다(설정 null → 곡사 종류 → 합 계산).
        private static float EndTimeOf(CombatAttackPresentationData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (settings.Kind != CombatAttackPresentationKind.Mortar)
                throw new InvalidOperationException("[CombatMortarAnimation] 곡사 표시 설정이 필요합니다.");
            return SumSeconds(Math.Max((double)settings.PrepareDuration + settings.ShootDuration,
                (double)FireAtOf(settings) + settings.ReturnDuration));
        }

        // 발사 순간 식은 여기 하나입니다. 생성자와 끝 시각이 함께 부릅니다(곡사 수명 계획 TP-13).
        private static float FireAtOf(CombatAttackPresentationData settings) =>
            SumSeconds((double)settings.PrepareDuration + settings.ShootDuration * (double)settings.FireTime);

        private static float SumSeconds(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0d || seconds > float.MaxValue)
                throw new InvalidOperationException("[CombatMortarAnimation] 표시 시간의 합이 유한해야 합니다.");
            return (float)seconds;
        }
    }
}
