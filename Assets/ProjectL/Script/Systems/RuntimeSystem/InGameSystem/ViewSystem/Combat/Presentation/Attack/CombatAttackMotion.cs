using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>공격의 임시 유닛 자세와 외부 Tick·취소 수명을 공유합니다.</summary>
    internal abstract class CombatAttackMotion : ICombatAttackAnimation
    {
        private readonly Action _cancelOwner;
        private readonly float _duration;
        private bool _begun;
        private bool _cleaned;
        private bool _released;
        private bool _ticking;
        private int _version;

        protected CombatAttackMotion(ICombatAttackUnitPose unit, Vector3 target, Vector3 up,
            Quaternion emptyFacing, float duration, Action cancelOwner)
        {
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            if (!Finite(target) || !Finite(up) || up.sqrMagnitude < .000001f ||
                !(duration > 0f) || float.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration));
            Target = target;
            Up = up.normalized;
            EmptyFacing = emptyFacing;
            _duration = duration;
            _cancelOwner = cancelOwner;
        }

        protected ICombatAttackUnitPose Unit { get; }
        protected Vector3 Target { get; }
        protected Vector3 Up { get; }
        protected Vector3 Origin { get; private set; }
        protected Vector3 Forward { get; private set; }
        protected Quaternion Facing { get; private set; }
        private Quaternion EmptyFacing { get; }
        protected float Elapsed { get; private set; }
        protected float Progress => Mathf.Clamp01(Elapsed / _duration);
        protected int Version => _version;
        protected bool IsRunning => _begun && !_cleaned && !_released;
        protected bool PoseDone => Elapsed >= _duration;
        public abstract bool IsComplete { get; }

        public void Begin()
        {
            if (_begun || _cleaned || _released)
                throw new InvalidOperationException("[CombatAttackMotion] 한 동작은 한 번만 시작할 수 있습니다.");
            if (!Unit.IsAvailable || !Finite(Unit.Position))
                throw new InvalidOperationException("[CombatAttackMotion] 현재 활성 대여의 유닛 자세가 필요합니다.");
            Origin = Unit.Position;
            // 방향 계산은 클립 공격과 같은 함수 하나입니다(클립 설계 E2).
            Facing = CombatAttackFacing.Toward(Origin, Target, Up, EmptyFacing);
            Forward = Facing * Vector3.forward;
            _begun = true;
            ++_version;
            try
            {
                Render(0f);
                Advance(0f, 0f);
            }
            catch (Exception error)
            {
                Fail(error);
                throw;
            }
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsRunning) return;
            if (_ticking) throw new InvalidOperationException("[CombatAttackMotion] Tick을 중첩 호출할 수 없습니다.");
            if (!Unit.IsAvailable || !ResourcesAvailable)
            {
                try
                {
                    _cancelOwner?.Invoke();
                }
                finally
                {
                    if (!_cleaned) Cancel();
                }
                return;
            }
            _ticking = true;
            try
            {
                float previous = Elapsed;
                Elapsed = (float)Math.Min((double)_duration, (double)Elapsed + Math.Max(0f, deltaTime));
                Advance(previous, Elapsed);
                if (IsRunning) Render(Progress);
            }
            catch (Exception error)
            {
                Fail(error);
                throw;
            }
            finally
            {
                _ticking = false;
            }
        }

        public virtual void Cancel()
        {
            ++_version;
            Cleanup();
        }

        public void Release()
        {
            if (_released) return;
            _released = true;
            ++_version;
            Cleanup();
        }

        protected virtual bool ResourcesAvailable => true;
        protected abstract void Render(float progress);
        protected abstract void Advance(float previous, float next);
        protected virtual void ReleaseResources()
        {
        }
        protected bool IsCurrent(int version) => IsRunning && version == _version;

        private void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            var errors = new List<Exception>();
            try
            {
                ReleaseResources();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            try
            {
                Unit.RestorePose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            if (errors.Count > 0)
                throw new AggregateException("[CombatAttackMotion] 발사체·유닛 자세 정리에 실패했습니다.", errors);
        }

        private void Fail(Exception error)
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

        internal static float Recoil(float progress, float fireTime)
        {
            if (progress < fireTime || fireTime >= 1f) return 0f;
            float peak = fireTime + (1f - fireTime) * (0.15f / 0.7f);
            return progress < peak
                ? Mathf.SmoothStep(0f, 1f, (progress - fireTime) / (peak - fireTime))
                : 1f - Mathf.SmoothStep(0f, 1f, (progress - peak) / (1f - peak));
        }

        private static bool Finite(Vector3 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
            !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
