using System;
using System.Collections.Generic;
using FMOD.Studio;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem
{
    // Boot에 구성된 각 Player의 슬롯 순서입니다.
    public enum AmbgmEvent { BGM = 0, AMB = 1, UIClick = 2, UIHover = 3, UIReward = 4, GameStart = 5, StageChanger = 7 }
    public enum UnitEvent { UnitPick = 0, UnitCancel = 1, UnitMovement = 2, UnitDead = 3, UnitAttack = 4, ObsDamaged = 5, ObsBroken = 6, BulletFall = 12 }
    /// <summary>Boot의 UnitMovement 슬롯에 등록한 기본 이동 프리셋 인덱스입니다.</summary>
    public enum UnitMovementPreset { Base = 0 }
    public enum SfxEvent { Tornado = 0, Tornado_Ended = 1, MineExplosion = 2, FallStone = 3, Haboob = 4, WeakTerrian = 5, Mark = 6, TrainCall = 7, TrainBell = 8 }

    public enum AudioStopMode
    {
        AllowFadeout,
        Immediate
    }

    #region 서비스

    public interface IAudioSystemService : IServiceModule
    {
        // 재생·프리셋 변경은 성공 시 true를 반환하고, 잘못된 요청·구성·실제 재생 실패는 예외로 알립니다.
        // presetIndex = -1은 프리셋을 바꾸지 않고 재생한다는 뜻입니다.
        bool Play(AmbgmEvent slot, int presetIndex = -1);
        bool Play(UnitEvent slot, int presetIndex = -1);
        bool Play(SfxEvent slot, int presetIndex = -1);
        bool SetPreset(AmbgmEvent slot, int presetIndex);
        bool IsPlaying(AmbgmEvent slot);
        bool Stop(AmbgmEvent slot, AudioStopMode stopMode = AudioStopMode.AllowFadeout);

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

        /// <summary>Bus의 사용자 볼륨을 선형 배율로 읽습니다. 1은 0dB, 0은 무음입니다.</summary>
        bool TryGetBusVolume(string busPath, out float volume);

        /// <summary>Bus의 사용자 볼륨을 0~1 선형 배율로 설정합니다.</summary>
        bool TrySetBusVolume(string busPath, float volume);
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
        private FmodEventPlayer _ambgm;
        private FmodEventPlayer _unit;
        private FmodEventPlayer _sfx;
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
                _ambgm = null;
                _unit = null;
                _sfx = null;
                foreach (var definition in _audioManager.PlayerIdDefinitions)
                {
                    if (definition.DisplayName == "AMBGM") _audioManager.TryGetPlayer(definition.Id, out _ambgm);
                    if (definition.DisplayName == "Unit") _audioManager.TryGetPlayer(definition.Id, out _unit);
                    if (definition.DisplayName == "SFX") _audioManager.TryGetPlayer(definition.Id, out _sfx);
                }
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
                _ambgm = null;
                _unit = null;
                _sfx = null;
                _sceneAudioConsumers = Array.Empty<AudioBase>();
                _boundAudioConsumers.Clear();
            }
        }

        public bool Play(AudioPlayerId playerId, int slotIndex)
        {
            return PlayRequired(RequirePlayer(playerId), slotIndex);
        }

        public bool Play(AudioPlayerId playerId, string slotName)
        {
            FmodEventPlayer player = RequirePlayer(playerId);
            return PlayRequired(player, RequireSlotIndex(player, slotName));
        }

        public bool Play(AmbgmEvent slot, int presetIndex = -1) => PlayCue(_ambgm, "AMBGM", (int)slot, presetIndex);
        public bool Play(UnitEvent slot, int presetIndex = -1) => PlayCue(_unit, "Unit", (int)slot, presetIndex);
        public bool Play(SfxEvent slot, int presetIndex = -1) => PlayCue(_sfx, "SFX", (int)slot, presetIndex);

        public bool SetPreset(AmbgmEvent slot, int presetIndex)
        {
            int index = (int)slot;
            RequireCue(_ambgm, "AMBGM", index);
            ApplyPreset(_ambgm, index, presetIndex, skipIfCurrent: true);
            return true;
        }

        public bool IsPlaying(AmbgmEvent slot) => TryCue(_ambgm, (int)slot) && _ambgm.IsPlaying((int)slot);

        public bool Stop(AmbgmEvent slot, AudioStopMode stopMode = AudioStopMode.AllowFadeout) =>
            TryCue(_ambgm, (int)slot) && _audioManager.Stop(_ambgm.PlayerId, (int)slot, ConvertStopMode(stopMode));

        private bool PlayCue(FmodEventPlayer player, string group, int slot, int presetIndex)
        {
            RequireCue(player, group, slot);
            if (presetIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(presetIndex), presetIndex,
                    $"[AudioSystem] {Describe(player, slot)}: 프리셋은 -1(변경 없음) 이상이어야 합니다.");
            if (presetIndex >= 0)
                ApplyPreset(player, slot, presetIndex, skipIfCurrent: false);
            return PlayRequired(player, slot);
        }

        private bool TryCue(FmodEventPlayer player, int slot) =>
            _isInitialized && !_isShutdown && _audioManager != null && player != null &&
            player.EventSlots != null && slot >= 0 && slot < player.EventSlots.Count && player.EventSlots[slot] != null &&
            !string.IsNullOrWhiteSpace(player.EventSlots[slot].selectedEventPath);

        private void RequireCue(FmodEventPlayer player, string group, int slot)
        {
            if (!_isInitialized || _isShutdown || _audioManager == null)
                throw new InvalidOperationException(
                    $"[AudioSystem] {group}, Slot={slot}: 초기화가 완료된 활성 AudioSystem에서만 재생·프리셋 변경을 요청할 수 있습니다.");
            if (player == null)
                throw new InvalidOperationException($"[AudioSystem] {group}, Slot={slot}: 연결된 FmodEventPlayer가 없습니다.");
            RequireSlot(player, slot);
        }

        private FmodEventPlayer RequirePlayer(AudioPlayerId playerId)
        {
            EnsureInitialized();
            ValidatePlayerId(playerId);
            if (!_audioManager.TryGetPlayer(playerId, out FmodEventPlayer player))
                throw new InvalidOperationException(
                    $"[AudioSystem] PlayerId={playerId}: 등록된 재생 대상이 없거나 여러 개입니다. 객체 지정 또는 PlayAll을 사용하세요.");
            return player;
        }

        private FmodEventPlayer RequirePlayer(AudioPlayerId playerId, GameObject requesterObject)
        {
            EnsureInitialized();
            ValidatePlayerId(playerId);
            if (requesterObject == null)
                throw new ArgumentNullException(nameof(requesterObject));
            if (!_audioManager.TryGetPlayer(playerId, requesterObject, out FmodEventPlayer player))
                throw new InvalidOperationException(
                    $"[AudioSystem] PlayerId={playerId}, Object='{requesterObject.name}': 객체에 부착된 등록 Player의 ID가 일치해야 합니다.");
            return player;
        }

        private static void ValidatePlayerId(AudioPlayerId playerId)
        {
            if (!playerId.IsValid)
                throw new ArgumentException("[AudioSystem] 재생할 PlayerId가 비어 있습니다.", nameof(playerId));
        }

        private static FmodEventSlot RequireSlot(FmodEventPlayer player, int slotIndex)
        {
            if (player.EventSlots == null || slotIndex < 0 || slotIndex >= player.EventSlots.Count)
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex,
                    $"[AudioSystem] PlayerId={player.PlayerId}, Player='{player.name}': 등록된 슬롯 범위를 벗어났습니다.");
            FmodEventSlot slot = player.EventSlots[slotIndex];
            if (slot == null || string.IsNullOrWhiteSpace(slot.selectedEventPath))
                throw new InvalidOperationException(
                    $"[AudioSystem] PlayerId={player.PlayerId}, Player='{player.name}', Slot={slotIndex} ('{slot?.slotName}'): 이벤트 경로가 연결되지 않았습니다.");
            return slot;
        }

        private static int RequireSlotIndex(FmodEventPlayer player, string slotName)
        {
            if (slotName == null) throw new ArgumentNullException(nameof(slotName));
            if (string.IsNullOrWhiteSpace(slotName))
                throw new ArgumentException("[AudioSystem] 슬롯 이름이 비어 있습니다.", nameof(slotName));
            int index = player.FindSlotIndex(slotName);
            if (index < 0)
                throw new InvalidOperationException(
                    $"[AudioSystem] PlayerId={player.PlayerId}, Player='{player.name}', Slot='{slotName}': 이름과 일치하는 슬롯이 없습니다.");
            return index;
        }

        private static void ApplyPreset(FmodEventPlayer player, int slotIndex, int presetIndex, bool skipIfCurrent)
        {
            var presets = RequireSlot(player, slotIndex).parameterPresets;
            if (presets == null || presetIndex < 0 || presetIndex >= presets.Count)
                throw new ArgumentOutOfRangeException(nameof(presetIndex), presetIndex,
                    $"[AudioSystem] {Describe(player, slotIndex)}: 등록된 프리셋 범위를 벗어났습니다.");
            if (presets[presetIndex] == null || !presets[presetIndex].enabled)
                throw new InvalidOperationException(
                    $"[AudioSystem] {Describe(player, slotIndex)}, Preset={presetIndex}: 프리셋이 없거나 비활성 상태입니다.");
            if (skipIfCurrent && player.GetCurrentPresetIndex(slotIndex) == presetIndex) return;
            if (!player.SetCurrentPresetIndex(slotIndex, presetIndex))
                throw new InvalidOperationException(
                    $"[AudioSystem] {Describe(player, slotIndex)}, Preset={presetIndex}: 프리셋 변경에 실패했습니다.");
        }

        private bool PlayRequired(FmodEventPlayer player, int slotIndex)
        {
            RequireSlot(player, slotIndex);
            if (!_audioManager.Play(player, slotIndex))
                throw new InvalidOperationException(
                    $"[AudioSystem] {Describe(player, slotIndex)}: 이벤트 재생에 실패했습니다. Player 등록 상태·Catalog/Bank·재생 모드·인스턴스 제한·FMOD 오류를 확인하세요.");
            return true;
        }

        private static string Describe(FmodEventPlayer player, int slotIndex) =>
            $"PlayerId={player.PlayerId}, Player='{player.name}', Slot={slotIndex} ('{player.EventSlots[slotIndex].slotName}'), Event='{player.EventSlots[slotIndex].selectedEventPath}', Preset={player.EventSlots[slotIndex].currentPresetIndex}";

        public bool Play(
            AudioPlayerId playerId,
            GameObject requesterObject,
            int slotIndex)
        {
            return PlayRequired(RequirePlayer(playerId, requesterObject), slotIndex);
        }

        public bool Play(
            AudioPlayerId playerId,
            GameObject requesterObject,
            string slotName)
        {
            FmodEventPlayer player = RequirePlayer(playerId, requesterObject);
            return PlayRequired(player, RequireSlotIndex(player, slotName));
        }

        public int PlayAll(AudioPlayerId playerId, int slotIndex)
        {
            return PlayAllRequired(playerId, slotIndex, null);
        }

        public int PlayAll(AudioPlayerId playerId, string slotName)
        {
            if (slotName == null) throw new ArgumentNullException(nameof(slotName));
            if (string.IsNullOrWhiteSpace(slotName))
                throw new ArgumentException("[AudioSystem] 슬롯 이름이 비어 있습니다.", nameof(slotName));
            return PlayAllRequired(playerId, null, slotName);
        }

        private int PlayAllRequired(AudioPlayerId playerId, int? slotIndex, string slotName)
        {
            EnsureInitialized();
            ValidatePlayerId(playerId);
            IReadOnlyList<FmodEventPlayer> players = _audioManager.GetPlayers(playerId);
            if (players.Count == 0)
                throw new InvalidOperationException(
                    $"[AudioSystem] PlayAll PlayerId={playerId}, Slot='{slotName ?? slotIndex.ToString()}': 등록된 재생 대상이 없습니다.");
            // 일부를 재생한 뒤 설정 누락을 발견하지 않도록 전체 슬롯을 먼저 검사합니다.
            for (int i = 0; i < players.Count; i++)
                RequireSlot(players[i], slotIndex ?? RequireSlotIndex(players[i], slotName));
            // 실제 재생 실패도 즉시 예외로 알립니다. 앞서 시작한 소리는 되돌리지 않습니다.
            for (int i = 0; i < players.Count; i++)
                PlayRequired(players[i], slotIndex ?? RequireSlotIndex(players[i], slotName));
            return players.Count;
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

        public bool TryGetBusVolume(string busPath, out float volume)
        {
            EnsureInitialized();
            volume = 0f;
            return _audioManager.TryGetBus(busPath, out Bus bus)
                && bus.getVolume(out volume) == FMOD.RESULT.OK;
        }

        public bool TrySetBusVolume(string busPath, float volume)
        {
            EnsureInitialized();
            if (float.IsNaN(volume) || float.IsInfinity(volume) || volume < 0f || volume > 1f)
                throw new ArgumentOutOfRangeException(nameof(volume), "Bus 볼륨은 0~1이어야 합니다.");
            return _audioManager.TryGetBus(busPath, out Bus bus)
                && bus.setVolume(volume) == FMOD.RESULT.OK;
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

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_isServiceBound) BindScene();
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
