using System;
using System.Collections.Generic;
using FMOD.Studio;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem
{
    public enum AudioStopMode
    {
        AllowFadeout,
        Immediate
    }

    #region 서비스

    public interface IAudioSystemService : IServiceModule
    {
        bool IsGamePaused { get; }

        /// <summary>동적 생성 후 등록하고 풀 반환 시 해제한다.</summary>
        void RegisterConsumer(AudioBase consumer);
        void UnregisterConsumer(AudioBase consumer);

        bool Stop(AudioPlayerId playerId, GameObject requesterObject, int slotIndex,
            AudioStopMode stopMode = AudioStopMode.AllowFadeout);
        bool Stop(AudioPlayerId playerId, GameObject requesterObject, string slotName,
            AudioStopMode stopMode = AudioStopMode.AllowFadeout);

        bool Play(AudioPlayerId playerId, int slotIndex);
        bool Play(AudioPlayerId playerId, string slotName);

        /// <summary>
        /// 요청 오브젝트에 부착되어 있고 Player ID가 일치하는
        /// FmodEventPlayer를 선택하여 재생한다.
        /// </summary>
        bool Play(
            AudioPlayerId playerId,
            GameObject requesterObject,
            int slotIndex);

        /// <summary>
        /// 요청 오브젝트에 부착되어 있고 Player ID가 일치하는
        /// FmodEventPlayer를 선택하여 재생한다.
        /// </summary>
        bool Play(
            AudioPlayerId playerId,
            GameObject requesterObject,
            string slotName);

        int PlayAll(AudioPlayerId playerId, int slotIndex);
        int PlayAll(AudioPlayerId playerId, string slotName);

        bool Stop(
            AudioPlayerId playerId,
            int slotIndex,
            AudioStopMode stopMode = AudioStopMode.AllowFadeout);

        bool Stop(
            AudioPlayerId playerId,
            string slotName,
            AudioStopMode stopMode = AudioStopMode.AllowFadeout);

        void StopAll(
            AudioStopMode stopMode = AudioStopMode.AllowFadeout);

        void SetPaused(bool paused);
        void SetGamePaused(bool paused);
    }

    #endregion

    #region 시스템 모듈

    internal sealed class AudioSystem :
        IAudioSystemService,
        ISystemBindScene,
        ISystemPostBindService,
        ISystemInitialize,
        ISystemShutdown
    {
        private AudioManager _audioManager;
        private AudioBase[] _sceneAudioConsumers = Array.Empty<AudioBase>();
        private readonly HashSet<AudioBase> _boundAudioConsumers = new();
        private bool _isShutdown;

        private bool _isServiceBound;
        private bool _isInitialized;

        public bool IsGamePaused
        {
            get
            {
                EnsureInitialized();
                return _audioManager.IsGamePaused;
            }
        }

        public void BindScene()
        {
            ThrowIfShutdown();
            _audioManager = AudioManager.Instance;

            if (_audioManager == null)
            {
                _audioManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>(
                    FindObjectsInactive.Include);
            }

            if (_audioManager == null)
            {
                Debug.LogWarning(
                    $"[{nameof(AudioSystem)}] 씬에서 {nameof(AudioManager)}를 찾지 못했습니다.");
            }
            else
            {
                _audioManager.RegisterExistingPlayers();
            }

            _sceneAudioConsumers =
                UnityEngine.Object.FindObjectsByType<AudioBase>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (_isServiceBound)
                BindUnboundAudioConsumers();
        }

        public void PostBindService(
            ISystemServiceStoreReader systemServiceResolver)
        {
            if (systemServiceResolver == null)
                throw new ArgumentNullException(nameof(systemServiceResolver));

            ThrowIfShutdown();
            if (_isServiceBound)
                throw new InvalidOperationException("Audio services are already bound.");
            BindScene();
            BindUnboundAudioConsumers();

            _isServiceBound = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public void Initialize()
        {
            ThrowIfShutdown();
            if (_isInitialized)
                throw new InvalidOperationException("Audio is already initialized.");
            if (_audioManager == null)
            {
                throw new InvalidOperationException(
                    $"[{nameof(AudioSystem)}] {nameof(BindScene)}에서 " +
                    $"{nameof(AudioManager)}를 연결해야 합니다.");
            }

            if (!_isServiceBound)
            {
                throw new InvalidOperationException(
                    $"[{nameof(AudioSystem)}] 서비스 바인딩이 완료되지 않았습니다.");
            }

            _isInitialized = true;
            foreach (AudioBase consumer in new List<AudioBase>(_boundAudioConsumers))
            {
                if (consumer != null)
                    consumer.NotifyAudioReady();
            }
        }

        public void Shutdown()
        {
            if (_isShutdown)
                return;
            _isShutdown = true;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _isInitialized = false;
            _isServiceBound = false;
            try
            {
                // Runtime owns the playback session; the scene owns the manager object.
                if (_audioManager != null)
                {
                    _audioManager.StopAll(STOP_MODE.IMMEDIATE);
                    _audioManager.PauseAll(false);
                    _audioManager.SetGamePaused(false);
                }
            }
            finally
            {
                foreach (AudioBase consumer in new List<AudioBase>(_boundAudioConsumers))
                {
                    if (consumer == null) continue;
                    try { consumer.UnbindAudioService(this); }
                    catch (Exception exception) { Debug.LogException(exception, consumer); }
                }
                _audioManager = null;
                _sceneAudioConsumers = Array.Empty<AudioBase>();
                _boundAudioConsumers.Clear();
            }
        }

        public bool Play(AudioPlayerId playerId, int slotIndex)
        {
            EnsureInitialized();
            return _audioManager.Play(playerId, slotIndex);
        }

        public bool Play(AudioPlayerId playerId, string slotName)
        {
            EnsureInitialized();
            return _audioManager.Play(playerId, slotName);
        }

        public bool Play(
            AudioPlayerId playerId,
            GameObject requesterObject,
            int slotIndex)
        {
            EnsureInitialized();
            return _audioManager.Play(playerId, requesterObject, slotIndex);
        }

        public bool Play(
            AudioPlayerId playerId,
            GameObject requesterObject,
            string slotName)
        {
            EnsureInitialized();
            return _audioManager.Play(playerId, requesterObject, slotName);
        }

        public int PlayAll(AudioPlayerId playerId, int slotIndex)
        {
            EnsureInitialized();
            return _audioManager.PlayAll(playerId, slotIndex);
        }

        public int PlayAll(AudioPlayerId playerId, string slotName)
        {
            EnsureInitialized();
            return _audioManager.PlayAll(playerId, slotName);
        }

        public bool Stop(
            AudioPlayerId playerId,
            int slotIndex,
            AudioStopMode stopMode = AudioStopMode.AllowFadeout)
        {
            EnsureInitialized();
            return _audioManager.Stop(
                playerId,
                slotIndex,
                ConvertStopMode(stopMode));
        }

        public bool Stop(
            AudioPlayerId playerId,
            string slotName,
            AudioStopMode stopMode = AudioStopMode.AllowFadeout)
        {
            EnsureInitialized();
            return _audioManager.Stop(
                playerId,
                slotName,
                ConvertStopMode(stopMode));
        }

        public void StopAll(
            AudioStopMode stopMode = AudioStopMode.AllowFadeout)
        {
            EnsureInitialized();
            _audioManager.StopAll(ConvertStopMode(stopMode));
        }

        public void SetPaused(bool paused)
        {
            EnsureInitialized();
            _audioManager.PauseAll(paused);
        }

        public void SetGamePaused(bool paused)
        {
            EnsureInitialized();
            _audioManager.SetGamePaused(paused);
        }

        public bool Stop(AudioPlayerId playerId, GameObject requesterObject, int slotIndex,
            AudioStopMode stopMode = AudioStopMode.AllowFadeout)
        {
            EnsureInitialized();
            return _audioManager.Stop(playerId, requesterObject, slotIndex, ConvertStopMode(stopMode));
        }

        public bool Stop(AudioPlayerId playerId, GameObject requesterObject, string slotName,
            AudioStopMode stopMode = AudioStopMode.AllowFadeout)
        {
            EnsureInitialized();
            return _audioManager.Stop(playerId, requesterObject, slotName, ConvertStopMode(stopMode));
        }

        public void RegisterConsumer(AudioBase consumer)
        {
            ThrowIfShutdown();
            if (consumer == null)
                throw new ArgumentNullException(nameof(consumer));
            if (!_boundAudioConsumers.Contains(consumer))
            {
                consumer.BindAudioService(this);
                _boundAudioConsumers.Add(consumer);
            }
            if (_isInitialized)
                consumer.NotifyAudioReady();
        }

        public void UnregisterConsumer(AudioBase consumer)
        {
            if (ReferenceEquals(consumer, null)) return;
            if (_boundAudioConsumers.Remove(consumer) && consumer != null)
                consumer.UnbindAudioService(this);
        }

        private void ThrowIfShutdown()
        {
            if (_isShutdown)
                throw new ObjectDisposedException(nameof(AudioSystem));
        }

        private static STOP_MODE ConvertStopMode(AudioStopMode stopMode)
        {
            return stopMode switch
            {
                AudioStopMode.AllowFadeout => STOP_MODE.ALLOWFADEOUT,
                AudioStopMode.Immediate => STOP_MODE.IMMEDIATE,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(stopMode),
                    stopMode,
                    null)
            };
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized || _audioManager == null)
            {
                throw new InvalidOperationException(
                    $"[{nameof(AudioSystem)}] Initialize 이후에 오디오를 조작할 수 있습니다.");
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (!_isServiceBound)
                return;

            BindScene();
        }

        private void BindUnboundAudioConsumers()
        {
            _boundAudioConsumers.RemoveWhere(consumer => consumer == null);

            for (int i = 0; i < _sceneAudioConsumers.Length; i++)
            {
                AudioBase consumer = _sceneAudioConsumers[i];
                if (consumer != null)
                    RegisterConsumer(consumer);
            }
        }
    }

    #endregion
}
