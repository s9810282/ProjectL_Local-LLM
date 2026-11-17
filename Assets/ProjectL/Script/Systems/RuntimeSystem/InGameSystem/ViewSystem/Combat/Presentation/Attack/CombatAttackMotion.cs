using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 공격의 임시 유닛 자세와 외부 Tick·취소 수명을 공유합니다. 근접·직선·곡사 코드 모션이 상속합니다.
    /// 수명(한 번 시작·Tick·취소·놓기·정리 묶기)은 여기 하나이고, 종류마다 다른 것은 훅(시작 자원·쓸 수 있음·그리기·진행·자원 정리)만 채웁니다(소리 프리셋·곡사 수명 설계 MT1).
    /// </summary>
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
            if (!Finite(target))
                throw new ArgumentOutOfRangeException(nameof(target));
            if (!Finite(up) || up.sqrMagnitude < .000001f)
                throw new ArgumentOutOfRangeException(nameof(up));
            if (!(duration > 0f) || float.IsInfinity(duration))
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
                BeginResources();
                Render(0f);
                // 시작 그리기가 취소했으면 진행하지 않습니다(곡사 수명 설계 MT1).
                if (IsRunning) Advance(0f, 0f);
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
                HandleUnavailable();
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

        /// <summary>
        /// 유닛 자세를 그립니다. 기반은 시작(<c>Render(0)</c>)과 Tick 끝(<c>Render(Progress)</c>) 두 자리에서만 부르고, 두 자리 모두
        /// <paramref name="progress"/>와 <c>Elapsed / 길이</c>가 같은 값입니다. 진행률을 쓸지 경과 초(<see cref="Elapsed"/>)를 쓸지는 파생 클래스가 정합니다(곡사 수명 계획 TP-12).
        /// </summary>
        protected abstract void Render(float progress);

        /// <summary>
        /// 시간이 <paramref name="previous"/>에서 <paramref name="next"/>로 지날 때의 사건(발사·타격)입니다.
        /// 기반은 동작이 돌고 있을 때만 부릅니다(시작은 시작 그리기가 취소하지 않았을 때, Tick은 첫 확인을 지난 뒤). 파생 클래스는 진입에서 다시 확인하지 않습니다(곡사 수명 계획 TP-11).
        /// </summary>
        protected abstract void Advance(float previous, float next);

        /// <summary>시작 때 유닛 자세 말고 따로 빌리는 자원(무기 자세·콜백)입니다. 시작 표시를 세운 뒤, 시작 그리기 앞에서 한 번 부릅니다. 실패하면 정리한 뒤 던집니다.</summary>
        protected virtual void BeginResources()
        {
        }

        /// <summary>
        /// 유닛 자세 말고 빌린 자원을 놓습니다. 정리 때 유닛 자세를 되돌리기 전에 한 번 부릅니다. 단계마다 <see cref="CleanupErrors.Try"/>로
        /// 실패를 <paramref name="errors"/>에 모으고 던지지 않습니다. 모은 실패는 기반이 유닛 자세까지 되돌린 뒤 한 번 던집니다.
        /// </summary>
        protected virtual void ReleaseResources(ref List<Exception> errors)
        {
        }

        protected bool IsCurrent(int version) => IsRunning && version == _version;

        /// <summary>
        /// 유닛·자원을 더 쓸 수 없을 때 주인 쪽 취소를 알리고, 아직 정리되지 않았으면 취소합니다. 돌고 있지 않으면(시작 전·정리 뒤·놓음) 아무것도 하지 않습니다.
        /// Tick 첫 확인과, 곡사의 그리기 도중 확인·무기 꺼짐 콜백이 부릅니다.
        /// </summary>
        protected void HandleUnavailable()
        {
            if (!IsRunning) return;
            try
            {
                _cancelOwner?.Invoke();
            }
            finally
            {
                if (!_cleaned) Cancel();
            }
        }

        private void Cleanup()
        {
            if (_cleaned) return;
            _cleaned = true;
            // 실패 목록은 처음 실패할 때만 만듭니다(공용 정리 규칙).
            List<Exception> errors = null;
            ReleaseResources(ref errors);
            CleanupErrors.Try(Unit.RestorePose, ref errors);
            CleanupErrors.ThrowIfAny(errors, "[CombatAttackMotion] 발사체·유닛 자세 정리에 실패했습니다.");
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
