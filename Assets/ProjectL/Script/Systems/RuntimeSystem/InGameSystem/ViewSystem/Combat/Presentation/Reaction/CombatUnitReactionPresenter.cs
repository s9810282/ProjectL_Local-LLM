#if UNITY_EDITOR || DEVELOPMENT_BUILD
using CombatPlaybackProfile = global::ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support.CombatPlaybackProfile;
#endif
using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Reaction
{
    /// <summary>한 대상의 피격·사망 자세, 외부 Tick, 모델 유지권과 완료/취소 수명입니다.</summary>
    internal sealed class CombatUnitReactionPresenter
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly CombatPlaybackProfile.ByOwner _hitProfiles = new("Reaction.Hit");
        private static readonly CombatPlaybackProfile.ByOwner _deathProfiles = new("Reaction.Death");
        private CombatPlaybackProfile _profile = _hitProfiles.Unknown;

#endif
        private ICombatUnitPose _pose;
        private CombatUnitReactionPresentationData _settings;
        private Vector3 _direction;
        private Vector3 _tiltAxis;
        private float _distance;
        private float _elapsed;
        private float _duration;
        private bool _death;
        private bool _usesRagdoll;
        private Action _releaseModel;
        private Action _completed;
        private Action _unavailable;
        private bool _ticking;
        private bool _cleaning;
        private bool _released;
        private int _version;

        internal bool IsPlaying => _pose != null;
        internal int Version => _version;

        internal void BeginHit(CombatUnitReactionPresentationData settings, ICombatUnitPose pose,
            CombatCharacterEvent damage, FieldGridMapper mapper, Action completed, Action unavailable)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _profile = _hitProfiles.Of((damage?.Expected ?? damage?.Final)?.Owner);
            using var beginSample = _profile.Begin.Auto();
#endif

            ValidateStart(settings, pose, completed, unavailable);
            if (damage == null) throw new ArgumentNullException(nameof(damage));
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));

            // 밀림 방향 = 맞은 방향(피격 이펙트 회전과 같은 원본, 피격 이펙트 설계 HE2)
            Vector3 direction = CombatHitDirection.Resolve(damage, mapper, pose.Rotation);
            float distance = mapper.WorldCellWidth * settings.HitCellFraction;
            if (float.IsInfinity(distance)) throw new InvalidOperationException("피격 표시 거리가 유효하지 않습니다.");
            _direction = direction;
            _tiltAxis = CombatHitLean.AxisOf(direction, mapper.WorldUp);
            _distance = distance;
            _death = false;
            _duration = settings.HitDuration;
            Begin(settings, pose, completed, unavailable);
        }

        internal void BeginDeath(CombatUnitReactionPresentationData settings, ICombatUnitPose pose,
            CombatCharacterEvent death, FieldGridMapper mapper, bool launchRagdoll, Action releaseModel, Action completed, Action unavailable)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _profile = _deathProfiles.Of((death?.Expected ?? death?.Final)?.Owner);
            using var beginSample = _profile.Begin.Auto();
#endif

            ValidateStart(settings, pose, completed, unavailable);
            if (mapper == null) throw new ArgumentNullException(nameof(mapper));
            if (releaseModel == null) throw new ArgumentNullException(nameof(releaseModel));
            Vector3 forward = Vector3.ProjectOnPlane(pose.Rotation * Vector3.forward, mapper.WorldUp);
            if (forward.sqrMagnitude < .000001f) forward = mapper.RotationToWorld(Rotation4.R0) * Vector3.forward;
            _tiltAxis = Vector3.Cross(mapper.WorldUp, forward).normalized;
            _usesRagdoll = pose is ICombatUnitRagdollPose ragdoll &&
                ragdoll.TryBeginDeathRagdoll(launchRagdoll ? CombatHitDirection.Resolve(death, mapper, pose.Rotation) : Vector3.zero,
                    mapper.WorldUp, mapper.WorldCellWidth);
            _death = true;
            _duration = settings.DeathDuration + settings.DeathRestDuration;
            _releaseModel = releaseModel;
            Begin(settings, pose, completed, unavailable);
        }

        private void ValidateStart(CombatUnitReactionPresentationData settings, ICombatUnitPose pose,
            Action completed, Action unavailable)
        {
            if (_released || IsPlaying || _cleaning)
                throw new InvalidOperationException("[CombatUnitReactionPresenter] 이전 반응을 정리한 활성 표시가 필요합니다.");
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (pose == null) throw new ArgumentNullException(nameof(pose));
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (unavailable == null) throw new ArgumentNullException(nameof(unavailable));
            if (!pose.IsAvailable) throw new InvalidOperationException("반응 대상의 표시 대여가 유효하지 않습니다.");
        }

        private void Begin(CombatUnitReactionPresentationData settings, ICombatUnitPose pose, Action completed, Action unavailable)
        {
            _settings = settings;
            _pose = pose;
            _completed = completed;
            _unavailable = unavailable;
            _elapsed = 0f;
            ++_version;
        }

        internal void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsPlaying) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var tickSample = _profile.Tick.Auto();
#endif
            if (_ticking) throw new InvalidOperationException("반응 Tick을 중첩 호출할 수 없습니다.");
            int version = _version;
            _ticking = true;
            try
            {
                if (!_pose.IsAvailable)
                {
                    var unavailable = _unavailable;
                    Cancel();
                    unavailable();
                    return;
                }
                _elapsed = (float)Math.Min(_duration, (double)_elapsed + Math.Max(0f, deltaTime));
                if (_death)
                {
                    // 랙돌이 쓰러지는 동안에는 원래 외형을 유지하고, 뒤의 머묾 시간에 디졸브합니다.
                    float dissolveStart = _usesRagdoll ? _settings.DeathDuration : 0f;
                    if (_elapsed >= _duration)
                        _pose.SetDeathDissolve(1f);
                    else if (_elapsed > dissolveStart)
                    {
                        float progress = (_elapsed - dissolveStart) / (_duration - dissolveStart);
                        _pose.SetDeathDissolve(Mathf.SmoothStep(0f, 1f, progress));
                    }
                }
                if (_elapsed < _duration)
                {
                    if (_death)
                    {
                        if (!_usesRagdoll)
                        {
                            float tilt = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / _settings.DeathDuration));
                            _pose.ApplyPose(_pose.Position, Quaternion.AngleAxis(-90f * tilt, _tiltAxis) * _pose.Rotation);
                        }
                    }
                    else
                    {
                        float amount = Mathf.Sin(Mathf.PI * (_elapsed / _settings.HitDuration));
                        _pose.ApplyPose(_pose.Position + _direction * (_distance * amount),
                            CombatHitLean.Apply(_pose.Rotation, _tiltAxis, _settings.HitAngle, amount));
                    }
                    return;
                }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                using var endSample = _profile.End.Auto();
#endif
                Action completed = _completed;
                ClosePose(keepDeathDissolve: _death);
                if (version == _version && !_released) completed();
            }
            catch (Exception error)
            {
                // 완료 콜백이 시작한 다음 반응은 이전 실패의 정리 대상이 아닙니다.
                if (version == _version && IsPlaying)
                {
                    try
                    {
                        Cancel();
                    }
                    catch (Exception cleanup)
                    {
                        throw new AggregateException(error, cleanup);
                    }
                }
                throw;
            }
            finally
            {
                _ticking = false;
            }
        }

        /// <summary>완료 콜백을 무효화하고 원래 자세를 복원합니다. 성공 통지는 하지 않습니다.</summary>
        internal void Cancel()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var cancelSample = IsPlaying ? _profile.Cancel.Auto() : default;
#endif

            ++_version;
            ClosePose(keepDeathDissolve: false);
        }

        internal void Release()
        {
            if (_released) return;
            _released = true;
            Cancel();
        }

        private void ClosePose(bool keepDeathDissolve)
        {
            var pose = _pose;
            var releaseModel = _releaseModel;
            _pose = null;
            _completed = null;
            _unavailable = null;
            _settings = null;
            _releaseModel = null;
            if (pose == null) return;
            _cleaning = true;
            try
            {
                Exception failure = null;
                try
                {
                    if (!keepDeathDissolve)
                        pose.ClearDeathDissolve();
                    pose.RestorePose();
                }
                catch (Exception error)
                {
                    failure = error;
                }
                try
                {
                    releaseModel?.Invoke();
                }
                catch (Exception error)
                {
                    if (failure != null) throw new AggregateException(failure, error);
                    throw;
                }
                if (failure != null) throw failure;
            }
            finally
            {
                _cleaning = false;
            }
        }
    }
}
