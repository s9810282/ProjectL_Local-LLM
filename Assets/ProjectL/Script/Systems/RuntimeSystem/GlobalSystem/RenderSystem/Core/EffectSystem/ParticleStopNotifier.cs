using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem
{
    [RequireComponent(typeof(ParticleSystem))]
    public sealed class ParticleStopNotifier : MonoBehaviour
    {
        private ParticleSystem _particleSystem;
        private Action _onStopped;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();

            ParticleSystem.MainModule main = _particleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
        }

        /// <summary>
        /// 종료 콜백 없이 파티클을 처음부터 재생한다.
        /// 루프 파티클처럼 Scope가 직접 종료를 관리할 때 사용한다.
        /// </summary>
        public void Play()
        {
            Play(null);
        }

        /// <summary>
        /// 파티클을 처음부터 재생하고,
        /// 자연 종료되면 등록된 콜백을 한 번 호출한다.
        /// </summary>
        public void Play(Action onStopped)
        {
            _onStopped = onStopped;

            _particleSystem.Stop(
                withChildren: true,
                ParticleSystemStopBehavior.StopEmittingAndClear);

            _particleSystem.Play(withChildren: true);
        }

        /// <summary>
        /// 자연스럽게 잔여 파티클이 사라지도록 방출만 중단한다.
        /// 파티클이 모두 사라지면 종료 콜백이 호출될 수 있다.
        /// </summary>
        public void StopEmitting()
        {
            if (_particleSystem != null) 
            {
                _particleSystem.Stop(
                    withChildren: true,
                    ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// 콜백을 취소하고 파티클을 즉시 제거한다.
        /// Scope 강제 종료 및 풀 반환 전에 사용한다.
        /// </summary>
        public void StopImmediately()
        {
            _onStopped = null;

            if (_particleSystem != null) 
            {
                _particleSystem.Stop(
                    withChildren: true,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        public void ClearCallback()
        {
            _onStopped = null;
        }

        private void OnParticleSystemStopped()
        {
            Action callback = _onStopped;
            _onStopped = null;

            callback?.Invoke();
        }
    }
}