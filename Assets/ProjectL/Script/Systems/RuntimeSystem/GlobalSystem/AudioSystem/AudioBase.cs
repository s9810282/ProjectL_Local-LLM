using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem
{
    /// <summary>Audio 서비스를 사용하는 선택적 씬 소비자 기반 클래스.</summary>
    public abstract class AudioBase : MonoBehaviour
    {
        protected IAudioSystemService AudioService { get; private set; }
        private bool _isAudioReady;

        internal void BindAudioService(IAudioSystemService service)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));
            if (AudioService != null && !ReferenceEquals(AudioService, service))
                throw new InvalidOperationException("Audio consumer already belongs to another service.");
            if (ReferenceEquals(AudioService, service))
                return;
            AudioService = service;
            try { OnAudioServiceBound(); }
            catch { AudioService = null; throw; }
        }

        internal void NotifyAudioReady()
        {
            if (_isAudioReady || AudioService == null || !isActiveAndEnabled)
                return;
            _isAudioReady = true;
            try { OnAudioServiceReady(); }
            catch { _isAudioReady = false; throw; }
        }

        internal void UnbindAudioService(IAudioSystemService service)
        {
            if (!ReferenceEquals(AudioService, service))
                return;
            AudioService = null;
            _isAudioReady = false;
            OnAudioServiceUnbound();
        }

        /// <summary>참조 연결 단계. 재생은 OnAudioServiceReady 이후에 요청한다.</summary>
        protected virtual void OnAudioServiceBound() { }
        /// <summary>오디오 초기화 완료. 등록 수명마다 한 번 호출한다.</summary>
        protected virtual void OnAudioServiceReady() { }
        protected virtual void OnAudioServiceUnbound() { }

        protected virtual void OnEnable()
        {
            // Scene binding includes inactive consumers; defer their ready callback until enabled.
            AudioService?.RegisterConsumer(this);
        }

        protected virtual void OnDestroy()
        {
            AudioService?.UnregisterConsumer(this);
        }
    }
}
