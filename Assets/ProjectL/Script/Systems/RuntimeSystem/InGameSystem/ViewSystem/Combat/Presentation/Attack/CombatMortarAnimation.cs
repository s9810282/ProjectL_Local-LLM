#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
#endif
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// CatusCannon의 준비·발사·반동·무기 복귀와 곡사 도착을 기다리는 단일 공격 동작입니다. 착탄 순간 히트(대상 모두 해제)를 알립니다(히트 타이밍 설계 H6).
    /// 히트는 착탄 자리(<see cref="CombatProjectileContact"/>)를 함께 넘깁니다. 무엇을 낼지는 받는 쪽이 정합니다(투사체 접촉 이펙트 설계 IE4).
    /// </summary>
    /// <remarks>발사체 풀의 Tick은 호출자가 한 번 전달합니다. 이 동작은 규칙·Buffer·결과 완료를 다루지 않습니다.</remarks>
    internal sealed class CombatMortarAnimation : ICombatAttackAnimation
    {
        private readonly CombatAttackPresentationData _settings;
        private readonly FieldGridMapper _mapper;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly CombatPlaybackProfile _profile;

#endif
        private readonly CombatProjectilePool _pool;
        private readonly CatusCannonWeaponView _weapon;
        private readonly ICombatAttackUnitPose _unit;
        private readonly Vector3 _target;
        // 목표가 바로 위·아래일 때 볼 방향입니다(공격 방향, 클립 설계 §11 K1·결정 Q-AC18 A).
        private readonly Quaternion _emptyFacing;
        private readonly Action _fired;
        private readonly Action _cancelOwner;
        private readonly Action<CombatProjectileContact> _hit;
        private readonly Action _weaponDisabled;
        private readonly float _fireAt;
        private readonly float _poseEndsAt;
        private CombatProjectileLease _lease;
        private Vector3 _origin;
        private Vector3 _forward;
        private Vector3 _up;
        private Quaternion _facing;
        private float _elapsed;
        private int _weaponVersion;
        private int _version;
        private bool _begun;
        private bool _hasFired;
        private bool _flightDone;
        private bool _cancelled;
        private bool _released;
        private bool _cleanupDone;
        private bool _ticking;

        /// <param name="emptyFacing">목표가 바로 위·아래일 때 볼 방향입니다. 다른 공격 종류와 같이 공격 방향을 넘깁니다.</param>
        /// <param name="hit">착탄 순간의 히트입니다. 착탄 자리를 받습니다. 없으면 알리지 않습니다.</param>
        internal CombatMortarAnimation(CombatAttackPresentationData settings, FieldGridMapper mapper,
            CombatProjectilePool pool, CatusCannonWeaponView weapon, ICombatAttackUnitPose unit,
            Vector3 target, Quaternion emptyFacing, Action fired, Action cancelOwner, Action<CombatProjectileContact> hit)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _profile = pool.ProfilingProfile;
#endif
            _weapon = weapon != null ? weapon : throw new ArgumentNullException(nameof(weapon));
            _unit = unit ?? throw new ArgumentNullException(nameof(unit));
            _fired = fired ?? throw new ArgumentNullException(nameof(fired));
            if (settings.Kind != CombatAttackPresentationKind.Mortar)
                throw new InvalidOperationException("[CombatMortarAnimation] 곡사 표시 설정이 필요합니다.");
            RequireFinite(target, nameof(target));
            _target = target;
            _emptyFacing = emptyFacing;
            _cancelOwner = cancelOwner;
            _hit = hit;
            _weaponDisabled = HandleUnavailable;
            _fireAt = Duration((double)settings.PrepareDuration + settings.ShootDuration * (double)settings.FireTime);
            _poseEndsAt = Duration(Math.Max((double)settings.PrepareDuration + settings.ShootDuration,
                (double)_fireAt + settings.ReturnDuration));
        }

        public bool IsComplete => _begun && !_cancelled && !_released && _flightDone && _elapsed >= _poseEndsAt;

        public void Begin()
        {
            if (_begun || _cancelled || _released)
                throw new InvalidOperationException("[CombatMortarAnimation] 한 공격 동작은 한 번만 시작할 수 있습니다.");
            if (!_unit.IsAvailable)
                throw new InvalidOperationException("[CombatMortarAnimation] 현재 대여의 살아 있는 유닛 표시가 필요합니다.");
            _weapon.Validate();
            _origin = _unit.Position;
            RequireFinite(_origin, nameof(_origin));
            _up = _mapper.WorldUp;
            // 방향 계산은 다른 공격 종류·클립 공격과 같은 함수 하나입니다(클립 설계 E2, §11 K1).
            _facing = CombatAttackFacing.Toward(_origin, _target, _up.normalized, _emptyFacing);
            _forward = _facing * Vector3.forward;
            _begun = true;
            ++_version;
            try
            {
                _weaponVersion = _weapon.BeginPose();
                _weapon.BindCancellation(_weaponDisabled);
                Render(0f);
                if (_fireAt == 0f)
                    Fire();
            }
            catch (Exception error)
            {
                CleanupFailure(error);
                throw;
            }
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!_begun || _cancelled || _released)
                return;
            if (_ticking)
                throw new InvalidOperationException("[CombatMortarAnimation] Tick을 중첩 호출할 수 없습니다.");
            if (!CanRender() || (_hasFired && !_flightDone && (_lease == null || !_lease.IsActive)))
            {
                HandleUnavailable();
                return;
            }

            _ticking = true;
            try
            {
                float next = (float)Math.Min((double)_elapsed + Math.Max(0f, deltaTime), _poseEndsAt);
                if (!_hasFired && next >= _fireAt)
                {
                    // 큰 프레임에서도 실제 발사 시점의 자세와 피벗을 먼저 샘플링합니다.
                    Render(_fireAt);
                    Fire();
                    if (_cancelled || _released)
                        return;
                }
                _elapsed = next;
                Render(_elapsed);
            }
            catch (Exception error)
            {
                CleanupFailure(error);
                throw;
            }
            finally
            {
                _ticking = false;
            }
        }

        public void Cancel()
        {
            _cancelled = true;
            ++_version;
            Cleanup();
        }

        public void Release()
        {
            if (_released)
                return;
            _released = true;
            ++_version;
            Cleanup();
        }

        private void Fire()
        {
            if (_hasFired || _cancelled || _released)
                return;
            int version = _version;
            _weapon.SetReturnProgress(_weaponVersion, 0f, _settings.BucketPitch);
            Vector3 from = _weapon.MuzzlePosition;
            float height = CombatShotPath.ArcHeight(_mapper, _settings);
            // 착탄 자리는 경로가 정해지는 지금 한 번 만듭니다(투사체 접촉 이펙트 설계 IE3).
            CombatProjectileContact landing = CombatProjectileContact.Along(from, _target, 1f, _up.normalized, _facing);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _pool.ProfilingProfile = _profile;
#endif
            CombatProjectileLease lease = _pool.FireArc(from, _target, _settings.FlightDuration,
                height, _up, _settings.SpinDegreesPerSecond, () =>
                {
                    if (version != _version || _cancelled || _released)
                        return;
                    _lease = null;
                    _flightDone = true;
                    // 착탄이 히트입니다. 무기·몸 복귀는 계속 진행하며 공격 완료는 그 뒤입니다.
                    _hit?.Invoke(landing);
                }, _settings.ProjectileEasing, _settings.ArcRotationMode);
            // SetActive 재진입 취소가 FireArc 반환보다 먼저 와도 방금 만든 대여를 남기지 않습니다.
            if (version != _version || _cancelled || _released)
            {
                _pool.Recall(lease);
                return;
            }
            _lease = lease;
            _hasFired = true;
            _fired();
        }

        private void Render(float elapsed)
        {
            if (!CanRender())
            {
                HandleUnavailable();
                return;
            }
            float shootProgress = Mathf.Clamp01((elapsed - _settings.PrepareDuration) / _settings.ShootDuration);
            float recoil = CombatAttackMotion.Recoil(shootProgress, _settings.FireTime);
            _unit.ApplyPose(_origin - _forward * (_settings.RecoilDistance * recoil),
                _facing * Quaternion.Euler(-_settings.RecoilAngle * recoil, 0f, 0f));
            if (_cancelled || _released || !CanRender())
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

        private bool CanRender() => _unit.IsAvailable && _weapon != null && _weapon.IsAvailable && _weapon.IsPoseCurrent(_weaponVersion);

        private void HandleUnavailable()
        {
            if (_cancelled || _released)
                return;
            try
            {
                _cancelOwner?.Invoke();
            }
            finally
            {
                if (!_cleanupDone)
                    Cancel();
            }
        }

        private void Cleanup()
        {
            if (_cleanupDone)
                return;
            _cleanupDone = true;
            CombatProjectileLease lease = _lease;
            _lease = null;
            var errors = new List<Exception>();
            TryCleanup(() => { if (_weapon != null) _weapon.UnbindCancellation(_weaponDisabled); }, errors);
            TryCleanup(() => _pool.Recall(lease), errors);
            TryCleanup(() =>
            {
                if (_weapon != null && _weapon.IsPoseCurrent(_weaponVersion))
                    _weapon.ResetPose();
            }, errors);
            TryCleanup(_unit.RestorePose, errors);
            if (errors.Count > 0)
                throw new AggregateException("[CombatMortarAnimation] 공격 자세·발사체 정리에 실패했습니다.", errors);
        }

        private void CleanupFailure(Exception error)
        {
            try
            {
                Cancel();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException("[CombatMortarAnimation] 공격 동작과 정리 실패가 발생했습니다.", error, cleanup);
            }
        }

        private static void TryCleanup(Action cleanup, List<Exception> errors)
        {
            try
            {
                cleanup();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }

        private static float Duration(double duration)
        {
            if (double.IsNaN(duration) || double.IsInfinity(duration) || duration < 0d || duration > float.MaxValue)
                throw new InvalidOperationException("[CombatMortarAnimation] 표시 시간의 합이 유한해야 합니다.");
            return (float)duration;
        }

        private static void RequireFinite(Vector3 vector, string name)
        {
            if (float.IsNaN(vector.x) || float.IsInfinity(vector.x) ||
                float.IsNaN(vector.y) || float.IsInfinity(vector.y) ||
                float.IsNaN(vector.z) || float.IsInfinity(vector.z))
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
