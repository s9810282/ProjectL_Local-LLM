using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>공격 표시 하나의 시작·Tick·완료·취소 수명입니다. 결과 재생과 확인은 기존 Playback의 책임입니다.</summary>
    /// <remarks>설정·좌표·이미 빌린 View를 받는 종류별 동작은 연결 계층에서 조립합니다.</remarks>
    internal sealed class CombatAttackPresenter
    {
        private ICombatAttackAnimation _animation;
        private Action _completed;
        private int _version;
        private bool _isCleaning;
        private bool _isReleased;

        internal bool IsPlaying => _animation != null;

        internal void Begin(ICombatAttackAnimation animation, Action completed)
        {
            if (_isReleased)
                throw new InvalidOperationException("[CombatAttackPresenter] 해제된 공격 표시입니다.");
            if (_animation != null || _isCleaning)
                throw new InvalidOperationException("[CombatAttackPresenter] 이전 공격 표시를 정리해야 합니다.");
            if (animation == null)
                throw new ArgumentNullException(nameof(animation));
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));

            int version = ++_version;
            _animation = animation;
            _completed = completed;
            try
            {
                animation.Begin();
                CompleteIfReady(animation, version);
            }
            catch (Exception error)
            {
                CleanupFailure(animation, version, error);
                throw;
            }
        }

        internal void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            var animation = _animation;
            if (animation == null)
                return;

            int version = _version;
            try
            {
                animation.Tick(Math.Max(0f, deltaTime));
                CompleteIfReady(animation, version);
            }
            catch (Exception error)
            {
                CleanupFailure(animation, version, error);
                throw;
            }
        }

        /// <summary>콜백을 먼저 무효화하고 취소·자원 정리를 모두 시도합니다. 완료를 통지하지 않습니다.</summary>
        internal void Cancel()
        {
            ++_version;
            var animation = _animation;
            _animation = null;
            _completed = null;
            if (animation == null)
                return;

            _isCleaning = true;
            try
            {
                List<Exception> failures = null;
                try
                {
                    animation.Cancel();
                }
                catch (Exception error)
                {
                    (failures ??= new List<Exception>()).Add(error);
                }
                try
                {
                    animation.Release();
                }
                catch (Exception error)
                {
                    (failures ??= new List<Exception>()).Add(error);
                }
                if (failures != null)
                    throw new AggregateException("[CombatAttackPresenter] 공격 표시 정리에 실패했습니다.", failures);
            }
            finally
            {
                _isCleaning = false;
            }
        }

        internal void Clear() => Cancel();

        internal void Release()
        {
            if (_isReleased)
                return;
            _isReleased = true;
            Cancel();
        }

        private void CompleteIfReady(ICombatAttackAnimation animation, int version)
        {
            if (!IsCurrent(animation, version))
                return;
            bool complete = animation.IsComplete;
            if (!complete || !IsCurrent(animation, version))
                return;

            var completed = _completed;
            _animation = null;
            _completed = null;
            _isCleaning = true;
            try
            {
                animation.Release();
            }
            finally
            {
                _isCleaning = false;
            }

            // Unity 자원 해제의 재진입이 취소·Release를 요청했다면 이전 완료를 되살리지 않습니다.
            if (version == _version && !_isReleased)
                completed();
        }

        private bool IsCurrent(ICombatAttackAnimation animation, int version)
            => version == _version && ReferenceEquals(_animation, animation);

        private void CleanupFailure(ICombatAttackAnimation animation, int version, Exception error)
        {
            // 완료 콜백에서 시작한 다음 공격이나 이미 취소한 동작은 건드리지 않습니다.
            if (!IsCurrent(animation, version))
                return;
            try
            {
                Cancel();
            }
            catch (Exception cleanup)
            {
                throw new AggregateException("[CombatAttackPresenter] 공격 표시 실패와 정리 실패가 발생했습니다.", error, cleanup);
            }
        }
    }
}
