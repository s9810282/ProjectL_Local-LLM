using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;
using STOP_MODE = FMOD.Studio.STOP_MODE;
using TriInspector;
using UnityEngine.Serialization;

public class FmodEventPlayer : MonoBehaviour
{
    [Header("Catalog Source")]
    [SerializeField] private FmodEventCatalog catalog;

    [Header("Player Options")]
    [SerializeField] private bool loadCatalogBanksOnEnable = true;
    [SerializeField] private bool unloadCatalogBanksOnDisable = true;
    [SerializeField] private bool stopAllSlotsOnDisable = true;
    [SerializeField] private bool stopAllSlotsOnDestroy = true;

    [Header("Event Slots")]
    [SerializeField] private List<FmodEventSlot> eventSlots = new();

    [NonSerialized] private List<FmodEventSlotRuntime> runtimeSlots;
    [NonSerialized] private Stack<FmodParameterPresetRuntime> presetRuntimePool;
    [NonSerialized] private AudioPlayerHandle registrationHandle;
    [NonSerialized] private bool managerPaused;
    [NonSerialized] private bool localPaused;
    [NonSerialized] private float nextInstanceCleanupTime;
    private readonly HashSet<string> warnedPlayFailures = new();

    // FMOD playback-state queries cross the managed/native boundary. Polling at
    // this cadence is sufficient to release completed one-shots while avoiding
    // a query for every slot on every frame.
    private const float InstanceCleanupIntervalSeconds = 0.1f;

    public FmodEventCatalog Catalog => catalog;
    public List<FmodEventSlot> EventSlots => eventSlots;

    [SerializeField] private AudioPlayerId playerKey;
    [SerializeField, HideInInspector, FormerlySerializedAs("playerId")]
    private string legacyPlayerId;

    public AudioPlayerId PlayerId => playerKey;
    public AudioPlayerHandle RegistrationHandle => registrationHandle;
    public bool IsRegistered => registrationHandle.IsValid;
    public bool IsPaused => managerPaused || localPaused;

    internal void AssignRegistrationHandle(AudioPlayerHandle handle)
    {
        registrationHandle = handle;
    }

    internal void ClearRegistrationHandle(AudioPlayerHandle expectedHandle)
    {
        if (!expectedHandle.IsValid || registrationHandle == expectedHandle)
            registrationHandle = default;
    }

    internal string LegacyPlayerId => legacyPlayerId ?? string.Empty;

    internal bool TryMigrateLegacyPlayerId(AudioManager manager)
    {
        if (playerKey.IsValid ||
            string.IsNullOrWhiteSpace(legacyPlayerId) ||
            manager == null ||
            !manager.TryFindPlayerIdByName(legacyPlayerId, out AudioPlayerId migratedId))
        {
            return false;
        }

        playerKey = migratedId;
        legacyPlayerId = string.Empty;
        return true;
    }

    internal void SetPlayerId(AudioPlayerId playerId)
    {
        playerKey = playerId;
        legacyPlayerId = string.Empty;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && !playerKey.IsValid &&
            !string.IsNullOrWhiteSpace(legacyPlayerId))
        {
            AudioManager manager = FindFirstObjectByType<AudioManager>(
                FindObjectsInactive.Include);
            TryMigrateLegacyPlayerId(manager);
        }

        if (Application.isPlaying)
            return;

        if (eventSlots == null)
            eventSlots = new List<FmodEventSlot>();

        for (int i = 0; i < eventSlots.Count; i++)
        {
            eventSlots[i] ??= new FmodEventSlot();
            eventSlots[i].Sanitize();
            SyncSlotWithSelectedEvent(i);
        }
    }
    #region AudioManager 등록 관련 함수
    private bool catalogBanksLoaded;

    private void OnEnable()
    {
        TryLoadCatalogBanks();
        TryRegisterToAudioManager();
    }

    private void TryLoadCatalogBanks()
    {
        if (catalogBanksLoaded)
            return;

        if (!loadCatalogBanksOnEnable || catalog == null)
            return;

        catalog.LoadBanks();
        catalogBanksLoaded = true;
    }

    private void TryUnloadCatalogBanks()
    {
        if (!catalogBanksLoaded)
            return;

        if (catalog != null)
            catalog.UnloadBanks();

        catalogBanksLoaded = false;
    }

    private void TryRegisterToAudioManager()
    {
        AudioManager manager = AudioManager.Instance;
        if (manager == null)
            return;

        if (registrationHandle.IsValid &&
            manager.TryGetPlayer(
                registrationHandle,
                out FmodEventPlayer registeredPlayer) &&
            registeredPlayer == this)
        {
            return;
        }

        registrationHandle = default;
        manager.TryRegister(this, out _);
    }

    private void TryUnregisterFromAudioManager()
    {
        AudioPlayerHandle handle = registrationHandle;
        if (!handle.IsValid)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.Unregister(this, handle);

        ClearRegistrationHandle(handle);
        SetManagerPaused(false);
    }
    #endregion
    private void Awake()
    {
        eventSlots ??= new List<FmodEventSlot>();
        runtimeSlots = new List<FmodEventSlotRuntime>(
            eventSlots != null ? eventSlots.Count : 0);
        presetRuntimePool = new Stack<FmodParameterPresetRuntime>(
            CalculatePresetRuntimeCapacity());
        EnsureRuntimeSlots();
    }

    private void Start()
    {
        EnsureRuntimeSlots();

        for (int i = 0; i < eventSlots.Count; i++)
        {
            if (eventSlots[i] != null && eventSlots[i].playOnStart)
                Play(i);
        }
        TryRegisterToAudioManager(); // AudioManager 관련 함수
    }

    private void Update()
    {
        EnsureRuntimeSlots();

        if (eventSlots == null || eventSlots.Count == 0)
            return;

        float deltaTime = Time.deltaTime;

        for (int i = 0; i < eventSlots.Count; i++)
        {
            FmodEventSlot slot = eventSlots[i];
            FmodEventSlotRuntime runtime = runtimeSlots[i];

            if (slot != null && runtime != null)
                TickSlotParameterPresets(slot, runtime, deltaTime);
        }

        if (Time.unscaledTime < nextInstanceCleanupTime)
            return;

        nextInstanceCleanupTime =
            Time.unscaledTime + InstanceCleanupIntervalSeconds;

        for (int i = 0; i < eventSlots.Count; i++)
        {
            if (eventSlots[i] != null && runtimeSlots[i] != null)
                CleanupReleasedInstances(i);
        }
    }

    private void OnDisable()
    {
        // A player cannot safely retain its EventInstances after unloading the
        // catalog banks that created them. Prefer a safe stop for this option
        // combination over leaving orphaned native handles behind.
        if (stopAllSlotsOnDisable ||
            (unloadCatalogBanksOnDisable && catalogBanksLoaded))
            StopAllImmediate();
        TryUnregisterFromAudioManager(); // AudioManager 관련 함수

        if (unloadCatalogBanksOnDisable)
            TryUnloadCatalogBanks();
    }

    private void OnDestroy()
    {
        TryUnregisterFromAudioManager();

        // TryUnloadCatalogBanks always runs during destruction, so native
        // instances from an owned catalog must be stopped first.
        if (stopAllSlotsOnDestroy || catalogBanksLoaded)
            StopAllImmediate();

        TryUnloadCatalogBanks();
    }

    /// <summary>기존 UnityEvent/Inspector 호출 호환용 재생 API.</summary>
    public void Play(int slotIndex)
    {
        TryPlay(slotIndex);
    }

    /// <summary>실제 EventInstance 시작 성공 여부를 반환하는 재생 API.</summary>
    public bool TryPlay(int slotIndex)
    {
        TryLoadCatalogBanks();

        if (!TryGetSlot(slotIndex, out FmodEventSlot slot, out FmodEventSlotRuntime runtime))
            return false;

        if (catalog != null && GetSelectedEventEntry(slot) == null)
        {
            Debug.LogWarning(
                $"[FmodEventPlayer] Slot {slotIndex} event is not available in catalog '{catalog.name}' or is outside the catalog bank filter: {slot.selectedEventPath}",
                this
            );
            return false;
        }

        if (string.IsNullOrWhiteSpace(slot.selectedEventPath))
        {
            Debug.LogWarning($"[FmodEventPlayer] Slot {slotIndex} has no selected event on '{name}'.");
            return false;
        }

        switch (slot.playbackMode)
        {
            case FmodPlaybackMode.SingleInstance:
                return PlaySingleInstance(slotIndex, slot, runtime);

            case FmodPlaybackMode.MultiInstance:
                return PlayMultiInstance(slotIndex, slot, runtime);

            default:
                Debug.LogWarning(
                    $"[FmodEventPlayer] Slot {slotIndex} on '{name}' has an unsupported playback mode: {slot.playbackMode}.",
                    this);
                return false;
        }
    }

    /// <summary>기존 UnityEvent/Inspector 호출 호환용 재생 API.</summary>
    public void Play(string slotName)
    {
        TryPlay(slotName);
    }

    /// <summary>실제 EventInstance 시작 성공 여부를 반환하는 재생 API.</summary>
    public bool TryPlay(string slotName)
    {
        int slotIndex = FindSlotIndex(slotName);
        return slotIndex >= 0 && TryPlay(slotIndex);
    }


    /// <summary>
    /// 슬롯에 설정된 stopMode를 사용해 해당 슬롯에서 재생 중인 이벤트를 정지한다.
    /// SingleInstance는 현재 단일 인스턴스, MultiInstance는 해당 슬롯의 모든 인스턴스를 정지한다.
    /// </summary>
    public bool Stop(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out FmodEventSlot slot, out _))
            return false;

        return Stop(slotIndex, slot.stopMode);
    }

    /// <summary>
    /// 지정한 STOP_MODE로 해당 슬롯에서 재생 중인 이벤트를 정지한다.
    /// </summary>
    public bool Stop(int slotIndex, STOP_MODE stopMode)
    {
        if (!TryGetSlot(slotIndex, out _, out FmodEventSlotRuntime runtime))
            return false;

        StopSlotRuntime(runtime, stopMode);
        return true;
    }

    /// <summary>
    /// 슬롯 이름으로 해당 슬롯의 재생을 정지한다.
    /// </summary>
    public bool Stop(string slotName)
    {
        int slotIndex = FindSlotIndex(slotName);
        return slotIndex >= 0 && Stop(slotIndex);
    }

    /// <summary>
    /// 슬롯 이름으로 해당 슬롯의 재생을 지정한 STOP_MODE로 정지한다.
    /// </summary>
    public bool Stop(string slotName, STOP_MODE stopMode)
    {
        int slotIndex = FindSlotIndex(slotName);
        return slotIndex >= 0 && Stop(slotIndex, stopMode);
    }

    /// <summary>
    /// 선택된 FMOD Event Path가 일치하는 슬롯들의 재생을 모두 정지한다.
    /// 예: StopByEventPath("event:/BGM/Boss/ProgSisters", STOP_MODE.ALLOWFADEOUT)
    /// </summary>
    public int StopByEventPath(string eventPath, STOP_MODE stopMode = STOP_MODE.IMMEDIATE)
    {
        if (string.IsNullOrWhiteSpace(eventPath))
            return 0;

        EnsureRuntimeSlots();

        int stoppedCount = 0;
        for (int i = 0; i < eventSlots.Count; i++)
        {
            FmodEventSlot slot = eventSlots[i];
            if (slot == null)
                continue;

            if (!string.Equals(slot.selectedEventPath, eventPath, StringComparison.OrdinalIgnoreCase))
                continue;

            if (i >= runtimeSlots.Count || runtimeSlots[i] == null)
                continue;

            StopSlotRuntime(runtimeSlots[i], stopMode);
            stoppedCount++;
        }

        return stoppedCount;
    }

    /// <summary>
    /// 선택된 FMOD Event GUID가 일치하는 슬롯들의 재생을 모두 정지한다.
    /// </summary>
    public int StopByEventGuid(string eventGuid, STOP_MODE stopMode = STOP_MODE.IMMEDIATE)
    {
        if (string.IsNullOrWhiteSpace(eventGuid))
            return 0;

        EnsureRuntimeSlots();

        int stoppedCount = 0;
        for (int i = 0; i < eventSlots.Count; i++)
        {
            FmodEventSlot slot = eventSlots[i];
            if (slot == null)
                continue;

            if (!string.Equals(slot.selectedEventGuid, eventGuid, StringComparison.OrdinalIgnoreCase))
                continue;

            if (i >= runtimeSlots.Count || runtimeSlots[i] == null)
                continue;

            StopSlotRuntime(runtimeSlots[i], stopMode);
            stoppedCount++;
        }

        return stoppedCount;
    }

    /// <summary>
    /// MultiInstance 슬롯에서 가장 최근에 재생된 인스턴스 하나만 정지한다.
    /// SingleInstance 슬롯이면 현재 단일 인스턴스를 정지한다.
    /// </summary>
    public bool StopLatestInstance(int slotIndex, STOP_MODE stopMode = STOP_MODE.IMMEDIATE)
    {
        if (!TryGetSlot(slotIndex, out FmodEventSlot slot, out FmodEventSlotRuntime runtime))
            return false;

        if (slot.playbackMode == FmodPlaybackMode.SingleInstance)
        {
            StopSingleHandle(ref runtime.singleInstance, stopMode);
            runtime.singlePresetIndex = -1;
            ReleasePresetRuntime(ref runtime.singlePresetRuntime);
            return true;
        }

        int lastIndex = runtime.multiInstances.Count - 1;
        if (lastIndex < 0)
            return false;

        StopHandle(runtime.multiInstances[lastIndex], stopMode);
        runtime.multiInstances[lastIndex].clearHandle();
        runtime.multiInstances.RemoveAt(lastIndex);
        if (lastIndex < runtime.multiPresetIndices.Count) runtime.multiPresetIndices.RemoveAt(lastIndex);
        ReleaseMultiPresetRuntimeAt(runtime, lastIndex);
        return true;
    }
    /// <summary>
    /// MultiInstance 슬롯에서 가장 오래전에 재생된 인스턴스 하나만 정지한다.
    /// SingleInstance 슬롯이면 현재 단일 인스턴스를 정지한다.
    /// </summary>
    public bool StopOldestInstance(int slotIndex, STOP_MODE stopMode = STOP_MODE.IMMEDIATE)
    {
        if (!TryGetSlot(slotIndex, out FmodEventSlot slot, out FmodEventSlotRuntime runtime))
            return false;

        if (slot.playbackMode == FmodPlaybackMode.SingleInstance)
        {
            StopSingleHandle(ref runtime.singleInstance, stopMode);
            runtime.singlePresetIndex = -1;
            ReleasePresetRuntime(ref runtime.singlePresetRuntime);
            return true;
        }

        if (runtime.multiInstances == null || runtime.multiInstances.Count == 0)
            return false;

        StopHandle(runtime.multiInstances[0], stopMode);
        runtime.multiInstances[0].clearHandle();

        runtime.multiInstances.RemoveAt(0);

        if (runtime.multiPresetIndices.Count > 0)
            runtime.multiPresetIndices.RemoveAt(0);

        ReleaseMultiPresetRuntimeAt(runtime, 0);

        return true;
    }
    public bool SetCurrentPresetIndex(int slotIndex, int presetIndex)
    {
        if (!TryGetSlotConfig(slotIndex, out FmodEventSlot slot))
            return false;

        slot.Sanitize();

        if (slot.parameterPresets == null || slot.parameterPresets.Count == 0)
        {
            slot.currentPresetIndex = -1;
            return false;
        }

        if (presetIndex < 0 || presetIndex >= slot.parameterPresets.Count)
        {
            slot.currentPresetIndex = -1;
            return false;
        }

        slot.currentPresetIndex = presetIndex;

        FmodParameterPreset preset = slot.parameterPresets[presetIndex];
        if (preset != null && preset.enabled && preset.applyToPlayingInstances)
            RestartCurrentPresetOnPlayingInstances(slotIndex);

        return true;
    }

    public int GetCurrentPresetIndex(int slotIndex)
    {
        return TryGetSlotConfig(slotIndex, out FmodEventSlot slot) ? slot.currentPresetIndex : -1;
    }

    public bool SelectNextPreset(int slotIndex, bool wrap = true)
    {
        if (!TryGetSlotConfig(slotIndex, out FmodEventSlot slot) || slot.parameterPresets == null || slot.parameterPresets.Count == 0)
            return false;

        int next = slot.currentPresetIndex + 1;
        if (next >= slot.parameterPresets.Count)
            next = wrap ? 0 : slot.parameterPresets.Count - 1;

        return SetCurrentPresetIndex(slotIndex, next);
    }

    public bool SelectPreviousPreset(int slotIndex, bool wrap = true)
    {
        if (!TryGetSlotConfig(slotIndex, out FmodEventSlot slot) || slot.parameterPresets == null || slot.parameterPresets.Count == 0)
            return false;

        int prev = slot.currentPresetIndex - 1;
        if (prev < 0)
            prev = wrap ? slot.parameterPresets.Count - 1 : 0;

        return SetCurrentPresetIndex(slotIndex, prev);
    }

    public void ClearCurrentPresetSelection(int slotIndex)
    {
        if (TryGetSlotConfig(slotIndex, out FmodEventSlot slot))
            slot.currentPresetIndex = -1;
    }


    public void StopImmediate(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out _, out FmodEventSlotRuntime runtime))
            return;

        StopSlotRuntime(runtime, STOP_MODE.IMMEDIATE);
    }

    public void StopAllowFadeout(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out _, out FmodEventSlotRuntime runtime))
            return;

        StopSlotRuntime(runtime, STOP_MODE.ALLOWFADEOUT);
    }

    public void StopAllImmediate()
    {
        EnsureRuntimeSlots();

        for (int i = 0; i < runtimeSlots.Count; i++)
            StopSlotRuntime(runtimeSlots[i], STOP_MODE.IMMEDIATE);
    }

    public void StopAllAllowFadeout()
    {
        EnsureRuntimeSlots();

        for (int i = 0; i < runtimeSlots.Count; i++)
            StopSlotRuntime(runtimeSlots[i], STOP_MODE.ALLOWFADEOUT);
    }

    public bool IsPlaying(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out _, out FmodEventSlotRuntime runtime))
            return false;

        if (IsHandlePlaying(runtime.singleInstance))
            return true;

        for (int i = runtime.multiInstances.Count - 1; i >= 0; i--)
        {
            if (IsHandlePlaying(runtime.multiInstances[i]))
                return true;
        }

        return false;
    }

    public void SetSelectedEvent(int slotIndex, FmodScannedEventEntry entry)
    {
        if (!TryGetSlotConfig(slotIndex, out FmodEventSlot slot) || entry == null)
            return;

        if (catalog != null && !catalog.AllowsEvent(entry))
        {
            Debug.LogWarning(
                $"[FmodEventPlayer] Event is outside catalog '{catalog.name}' bank filter and cannot be selected: {entry.path}",
                this
            );
            return;
        }

        bool eventChanged = !string.Equals(
                slot.selectedEventGuid,
                entry.guidString,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                slot.selectedEventPath,
                entry.path,
                StringComparison.OrdinalIgnoreCase);

        if (eventChanged)
            Stop(slotIndex, STOP_MODE.IMMEDIATE);

        slot.selectedEventPath = entry.path;
        slot.selectedEventGuid = entry.guidString;
        slot.displayName = entry.displayName;

        SyncSlotWithSelectedEvent(slotIndex);
    }

    public void ClearSelectedEvent(int slotIndex)
    {
        if (!TryGetSlotConfig(slotIndex, out FmodEventSlot slot))
            return;

        Stop(slotIndex, STOP_MODE.IMMEDIATE);
        slot.Sanitize();
        slot.selectedEventPath = string.Empty;
        slot.selectedEventGuid = string.Empty;
        slot.displayName = string.Empty;
        slot.parameterOverrides.Clear();
        slot.parameterAutomations.Clear();
        slot.parameterPresets.Clear();
        slot.currentPresetIndex = -1;
    }

    public void SyncAllSlotsWithSelectedEvents()
    {
        if (eventSlots == null)
            return;

        for (int i = 0; i < eventSlots.Count; i++)
            SyncSlotWithSelectedEvent(i);
    }

    public void SyncSlotWithSelectedEvent(int slotIndex)
    {
        if (!TryGetSlotConfig(slotIndex, out FmodEventSlot slot))
            return;

        slot.Sanitize();

        FmodScannedEventEntry entry = GetSelectedEventEntry(slot);
        if (entry == null)
        {
            slot.parameterOverrides.Clear();
            slot.parameterAutomations.Clear();
            slot.parameterPresets.Clear();
            slot.currentPresetIndex = -1;
            return;
        }

        slot.displayName = string.IsNullOrWhiteSpace(entry.displayName) ? entry.eventName : entry.displayName;

        List<FmodParameterOverride> newOverrides = new();
        foreach (FmodParameterDefinition def in entry.parameters)
        {
            FmodParameterOverride existing = FindParameterOverride(slot, def.name);
            if (existing != null)
            {
                existing.CopyMetadataFrom(def);
                existing.value = ClampValueByKind(existing.value, existing);
                newOverrides.Add(existing);
            }
            else
            {
                newOverrides.Add(FmodParameterOverride.CreateFrom(def));
            }
        }

        slot.parameterOverrides = newOverrides;

        slot.parameterPresets ??= new List<FmodParameterPreset>();
        for (int i = 0; i < slot.parameterPresets.Count; i++)
        {
            FmodParameterPreset Preset = slot.parameterPresets[i];
            if (Preset == null)
                continue;

            Preset.EnsureId();

            FmodParameterOverride parameter = FindParameterOverride(slot, Preset.targetParameterName);
            if (parameter != null)
                Preset.SanitizeFor(parameter);
        }

        slot.parameterAutomations ??= new List<FmodParameterAutomation>(); // legacy UI compatibility

        if (slot.parameterPresets.Count == 0)
            slot.currentPresetIndex = -1;
        else
            slot.currentPresetIndex = Mathf.Clamp(slot.currentPresetIndex, -1, slot.parameterPresets.Count - 1);
    }

    public void SetParameterValue(int slotIndex, string parameterName, float value, bool applyNow = true)
    {
        if (!TryGetSlot(slotIndex, out FmodEventSlot slot, out FmodEventSlotRuntime runtime))
            return;

        FmodParameterOverride parameter = FindParameterOverride(slot, parameterName);
        if (parameter == null)
            return;

        if (!parameter.IsEditableInGui)
        {
            Debug.LogWarning($"[FmodEventPlayer] Parameter '{parameter.name}' is automatic/read-only and cannot be overridden.");
            return;
        }

        parameter.useOverride = true;
        parameter.value = ClampValueByKind(value, parameter);

        if (!applyNow)
            return;

        if (slot.playbackMode == FmodPlaybackMode.SingleInstance)
        {
            ApplySingleParameterToHandle(runtime.singleInstance, parameter);
        }
        else
        {
            for (int i = runtime.multiInstances.Count - 1; i >= 0; i--)
                ApplySingleParameterToHandle(runtime.multiInstances[i], parameter);
        }
    }
    public void ApplyAllParameterOverrides(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out FmodEventSlot slot, out FmodEventSlotRuntime runtime))
            return;

        if (slot.playbackMode == FmodPlaybackMode.SingleInstance)
        {
            ApplyAllOverridesToHandle(slot, runtime.singleInstance);
        }
        else
        {
            for (int i = runtime.multiInstances.Count - 1; i >= 0; i--)
                ApplyAllOverridesToHandle(slot, runtime.multiInstances[i]);
        }
    }

    public void ConfigureParameterAutomation(
        int slotIndex,
        string parameterName,
        bool enabled,
        float intervalSeconds,
        float deltaPerStep,
        bool loop,
        bool pingPong,
        bool onlyWhenPlaying,
        bool applyToPlayingInstances)
    {
        if (!TryGetSlotConfig(slotIndex, out FmodEventSlot slot))
            return;

        FmodParameterAutomation automation = FindParameterAutomation(slot, parameterName);
        FmodParameterOverride parameter = FindParameterOverride(slot, parameterName);

        if (automation == null || parameter == null)
            return;

        automation.enabled = enabled;
        automation.intervalSeconds = Mathf.Max(0.01f, intervalSeconds);
        automation.deltaPerStep = deltaPerStep;
        automation.loop = loop;
        automation.pingPong = pingPong;
        automation.onlyWhenPlaying = onlyWhenPlaying;
        automation.applyToPlayingInstances = applyToPlayingInstances;
        automation.SanitizeFor(parameter);
    }

    public bool RestartCurrentPresetOnPlayingInstances(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out FmodEventSlot slot, out FmodEventSlotRuntime runtime))
            return false;

        if (slot.currentPresetIndex < 0 ||
            slot.parameterPresets == null ||
            slot.currentPresetIndex >= slot.parameterPresets.Count)
        {
            return false;
        }

        FmodParameterPreset selectedPreset = slot.parameterPresets[slot.currentPresetIndex];
        if (selectedPreset == null || !selectedPreset.enabled)
            return false;

        bool applied = false;

        if (runtime.singleInstance.isValid() && IsHandlePlaying(runtime.singleInstance))
        {
            runtime.singlePresetIndex = slot.currentPresetIndex;
            ReleasePresetRuntime(ref runtime.singlePresetRuntime);
            runtime.singlePresetRuntime = CreatePresetRuntime(slot, slot.currentPresetIndex);
            if (runtime.singlePresetRuntime != null)
            {
                ApplyCurrentPresetValuesToHandle(slot, runtime.singlePresetRuntime, runtime.singleInstance);
                applied = true;
            }
        }

        for (int i = 0; i < runtime.multiInstances.Count; i++)
        {
            if (!runtime.multiInstances[i].isValid() || !IsHandlePlaying(runtime.multiInstances[i]))
                continue;

            while (runtime.multiPresetIndices.Count <= i) runtime.multiPresetIndices.Add(-1);
            while (runtime.multiPresetRuntimes.Count <= i) runtime.multiPresetRuntimes.Add(null);

            runtime.multiPresetIndices[i] = slot.currentPresetIndex;
            FmodParameterPresetRuntime previousRuntime = runtime.multiPresetRuntimes[i];
            ReleasePresetRuntime(ref previousRuntime);
            runtime.multiPresetRuntimes[i] = CreatePresetRuntime(slot, slot.currentPresetIndex);
            if (runtime.multiPresetRuntimes[i] != null)
            {
                ApplyCurrentPresetValuesToHandle(slot, runtime.multiPresetRuntimes[i], runtime.multiInstances[i]);
                applied = true;
            }
        }

        return applied;
    }

    private int CalculatePresetRuntimeCapacity()
    {
        if (eventSlots == null || eventSlots.Count == 0)
            return 1;

        int capacity = 0;
        for (int i = 0; i < eventSlots.Count; i++)
        {
            FmodEventSlot slot = eventSlots[i];
            if (slot == null)
                continue;

            capacity += slot.playbackMode == FmodPlaybackMode.SingleInstance
                ? 1
                : Mathf.Max(1, slot.maxInstances);
        }

        return Mathf.Max(1, capacity);
    }

    private FmodParameterPresetRuntime RentPresetRuntime()
    {
        presetRuntimePool ??= new Stack<FmodParameterPresetRuntime>();
        return presetRuntimePool.Count > 0
            ? presetRuntimePool.Pop()
            : new FmodParameterPresetRuntime();
    }

    private void ReleasePresetRuntime(ref FmodParameterPresetRuntime presetRuntime)
    {
        if (presetRuntime == null)
            return;

        presetRuntime.Reset();
        presetRuntimePool ??= new Stack<FmodParameterPresetRuntime>();
        presetRuntimePool.Push(presetRuntime);
        presetRuntime = null;
    }

    private void ReleaseMultiPresetRuntimeAt(
        FmodEventSlotRuntime runtime,
        int index)
    {
        if (runtime == null ||
            index < 0 ||
            index >= runtime.multiPresetRuntimes.Count)
        {
            return;
        }

        FmodParameterPresetRuntime presetRuntime = runtime.multiPresetRuntimes[index];
        ReleasePresetRuntime(ref presetRuntime);
        runtime.multiPresetRuntimes.RemoveAt(index);
    }

    private static FmodParameterOverride FindParameterOverride(
        FmodEventSlot slot,
        string parameterName)
    {
        if (slot?.parameterOverrides == null || string.IsNullOrEmpty(parameterName))
            return null;

        for (int i = 0; i < slot.parameterOverrides.Count; i++)
        {
            FmodParameterOverride parameter = slot.parameterOverrides[i];
            if (parameter != null && string.Equals(
                    parameter.name,
                    parameterName,
                    StringComparison.Ordinal))
            {
                return parameter;
            }
        }

        return null;
    }

    private static FmodParameterAutomation FindParameterAutomation(
        FmodEventSlot slot,
        string parameterName)
    {
        if (slot?.parameterAutomations == null || string.IsNullOrEmpty(parameterName))
            return null;

        for (int i = 0; i < slot.parameterAutomations.Count; i++)
        {
            FmodParameterAutomation automation = slot.parameterAutomations[i];
            if (automation != null && string.Equals(
                    automation.parameterName,
                    parameterName,
                    StringComparison.Ordinal))
            {
                return automation;
            }
        }

        return null;
    }

    public int FindSlotIndex(string slotName)
    {
        if (string.IsNullOrWhiteSpace(slotName) || eventSlots == null)
            return -1;

        for (int i = 0; i < eventSlots.Count; i++)
        {
            FmodEventSlot slot = eventSlots[i];
            if (slot != null && string.Equals(
                    slot.slotName,
                    slotName,
                    StringComparison.OrdinalIgnoreCase))
            return i;
        }

        return -1;
    }

    public FmodScannedEventEntry GetSelectedEventEntry(int slotIndex)
    {
        return TryGetSlotConfig(slotIndex, out FmodEventSlot slot) ? GetSelectedEventEntry(slot) : null;
    }

    private bool PlaySingleInstance(int slotIndex, FmodEventSlot slot, FmodEventSlotRuntime runtime)
    {
        bool hasValidHandle = runtime.singleInstance.isValid();
        bool isPlaying = hasValidHandle && IsHandlePlaying(runtime.singleInstance);

        if (isPlaying && !slot.restartIfAlreadyPlaying)
        {
            ApplyAllOverridesToHandle(slot, runtime.singleInstance);
            ApplyCurrentPresetValuesToHandle(slot, runtime.singlePresetRuntime, runtime.singleInstance);
            return true;
        }

        if (hasValidHandle)
            StopSingleHandle(ref runtime.singleInstance, slot.stopMode);

        ReleasePresetRuntime(ref runtime.singlePresetRuntime);
        runtime.singlePresetIndex = -1;

        if (!TryCreateEventInstance(slot.selectedEventPath, out EventInstance instance))
            return false;

        FmodParameterPresetRuntime presetRuntime = CreatePresetRuntime(
            slot,
            slot.currentPresetIndex);
        AttachIfNeeded(slot, instance);
        ApplyAllOverridesToHandle(slot, instance);
        ApplyCurrentPresetValuesToHandle(slot, presetRuntime, instance);
        ApplyPauseStateToHandle(instance);

        if (!TryStartEventInstance(ref instance, slot.selectedEventPath))
        {
            ReleasePresetRuntime(ref presetRuntime);
            return false;
        }

        runtime.singleInstance = instance;
        runtime.singlePresetIndex = slot.currentPresetIndex;
        runtime.singlePresetRuntime = presetRuntime;
        return true;
    }

    private bool PlayMultiInstance(int slotIndex, FmodEventSlot slot, FmodEventSlotRuntime runtime)
    {
        CleanupReleasedInstances(slotIndex);

        if (slot.maxInstances > 0 && runtime.multiInstances.Count >= slot.maxInstances)
        {
            if (!slot.stealOldestInstance)
                return false;
        }

        if (!TryCreateEventInstance(slot.selectedEventPath, out EventInstance instance))
            return false;

        FmodParameterPresetRuntime presetRuntime = CreatePresetRuntime(
            slot,
            slot.currentPresetIndex);
        AttachIfNeeded(slot, instance);
        ApplyAllOverridesToHandle(slot, instance);
        ApplyCurrentPresetValuesToHandle(slot, presetRuntime, instance);
        ApplyPauseStateToHandle(instance);

        if (!TryStartEventInstance(ref instance, slot.selectedEventPath))
        {
            ReleasePresetRuntime(ref presetRuntime);
            return false;
        }

        if (slot.maxInstances > 0 && runtime.multiInstances.Count >= slot.maxInstances)
        {
            EventInstance oldest = runtime.multiInstances[0];
            StopHandle(oldest, slot.stopMode);
            oldest.clearHandle();
            runtime.multiInstances.RemoveAt(0);
            ReleaseMultiPresetRuntimeAt(runtime, 0);
            if (runtime.multiPresetIndices.Count > 0)
                runtime.multiPresetIndices.RemoveAt(0);
        }

        runtime.multiInstances.Add(instance);
        runtime.multiPresetIndices.Add(slot.currentPresetIndex);
        runtime.multiPresetRuntimes.Add(presetRuntime);
        return true;
    }

    private bool TryCreateEventInstance(string eventPath, out EventInstance instance)
    {
        instance = default;

        try
        {
            instance = RuntimeManager.CreateInstance(eventPath);
        }
        catch (Exception exception)
        {
            WarnPlayFailure(eventPath, exception.GetType().Name);
            return false;
        }

        if (instance.isValid())
            return true;

        WarnPlayFailure(eventPath, "CreateInstance returned an invalid handle");
        return false;
    }

    private bool TryStartEventInstance(ref EventInstance instance, string eventPath)
    {
        FMOD.RESULT result = instance.start();
        if (result == FMOD.RESULT.OK)
        {
            warnedPlayFailures.Remove(eventPath);
            return true;
        }

        WarnPlayFailure(eventPath, "start failed: " + result);
        instance.release();
        instance.clearHandle();
        return false;
    }

    private void WarnPlayFailure(string eventPath, string reason)
    {
        string key = string.IsNullOrWhiteSpace(eventPath) ? "<empty>" : eventPath;
        if (!warnedPlayFailures.Add(key))
            return;

        Debug.LogWarning(
            "[FmodEventPlayer] Failed to play '" + key + "' on '" + name + "': " + reason + ".",
            this);
    }

    private void TickSlotParameterPresets(
        FmodEventSlot slot,
        FmodEventSlotRuntime runtime,
        float deltaTime)
    {
        if (slot.playbackMode == FmodPlaybackMode.SingleInstance)
        {
            if (runtime.singleInstance.isValid())
                TickSinglePlaybackPreset(slot, runtime.singleInstance, runtime.singlePresetIndex, runtime.singlePresetRuntime, deltaTime);
            return;
        }

        for (int i = runtime.multiInstances.Count - 1; i >= 0; i--)
        {
            if (!runtime.multiInstances[i].isValid())
                continue;

            FmodParameterPresetRuntime presetRuntime = i < runtime.multiPresetRuntimes.Count ? runtime.multiPresetRuntimes[i] : null;
            int presetIndex = i < runtime.multiPresetIndices.Count ? runtime.multiPresetIndices[i] : -1;
            TickSinglePlaybackPreset(slot, runtime.multiInstances[i], presetIndex, presetRuntime, deltaTime);
        }
    }

    private void TickSinglePlaybackPreset(FmodEventSlot slot, EventInstance instance, int presetIndex, FmodParameterPresetRuntime presetRuntime, float deltaTime)
    {
        if (presetRuntime == null || presetIndex < 0 || slot.parameterPresets == null || presetIndex >= slot.parameterPresets.Count)
            return;

        FmodParameterPreset preset = slot.parameterPresets[presetIndex];
        if (preset == null ||
            !preset.enabled ||
            string.IsNullOrWhiteSpace(preset.targetParameterName))
            return;

        if (preset.applyMode == FmodParameterApplyMode.OneShotApply)
            return;

        FmodParameterOverride parameter = presetRuntime.parameter;
        if (parameter == null)
        {
            parameter = FindParameterOverride(slot, preset.targetParameterName);
            presetRuntime.parameter = parameter;
        }
        if (parameter == null)
            return;

        // Labeled parameters represent fixed states, so animated preset modes do not update them.
        // They are applied once when playback starts or when the current preset is restarted.
        if (parameter.kind == FmodParameterKind.Labeled)
            return;

        presetRuntime.elapsed += deltaTime;
        float interval = Mathf.Max(0.01f, preset.updateInterval);
        if (presetRuntime.elapsed < interval)
            return;

        presetRuntime.elapsed -= interval;

        // Query FMOD only when the preset is actually due to write a value.
        // Completed one-shots are released by the periodic cleanup below.
        if (!IsHandlePlaying(instance))
            return;

        StepParameterPreset(parameter, preset, presetRuntime);
        ApplyParameterValueToHandleOnly(instance, parameter, presetRuntime.currentValue);
    }

    private void StepParameterPreset(
        FmodParameterOverride parameter,
        FmodParameterPreset preset,
        FmodParameterPresetRuntime presetRuntime)
    {
        float duration = Mathf.Max(0.01f, preset.durationSeconds);
        float delta = preset.updateInterval / duration;

        switch (preset.applyMode)
        {
            case FmodParameterApplyMode.Sweep:
                presetRuntime.progress += delta;
                if (presetRuntime.progress >= 1f)
                    presetRuntime.progress = preset.loop ? 0f : 1f;
                break;

            case FmodParameterApplyMode.PingPong:
                presetRuntime.progress += delta * presetRuntime.direction;

                if (presetRuntime.progress >= 1f)
                {
                    presetRuntime.progress = 1f;
                    presetRuntime.direction = -1f;
                }
                else if (presetRuntime.progress <= 0f)
                {
                    presetRuntime.progress = 0f;
                    presetRuntime.direction = preset.loop ? 1f : 0f;
                }
                break;
        }

        presetRuntime.progress = Mathf.Clamp01(presetRuntime.progress);
        float curveT = EvaluatePresetCurve(preset.curveMode, presetRuntime.progress);
        presetRuntime.currentValue = ClampValueByKind(Mathf.Lerp(preset.minValue, preset.maxValue, curveT), parameter);
    }

    private FmodParameterPresetRuntime CreatePresetRuntime(FmodEventSlot slot, int presetIndex)
    {
        if (slot == null || slot.parameterPresets == null || presetIndex < 0 || presetIndex >= slot.parameterPresets.Count)
            return null;

        FmodParameterPreset preset = slot.parameterPresets[presetIndex];
        if (preset == null ||
            !preset.enabled ||
            string.IsNullOrWhiteSpace(preset.targetParameterName))
            return null;

        FmodParameterOverride parameter = FindParameterOverride(slot, preset.targetParameterName);
        if (parameter == null || !parameter.IsEditableInGui)
            return null;

        preset.EnsureId();

        float initialProgress = preset.randomizeStart && preset.applyMode != FmodParameterApplyMode.OneShotApply
            ? UnityEngine.Random.Range(0f, 1f)
            : 0f;

        float initialValue;

        if (parameter.kind == FmodParameterKind.Labeled)
        {
            // Prefer the label string, then fall back to the cached label index.
            initialValue = ResolvePresetLabelIndex(parameter, preset);
            if (initialValue < 0f)
                return null;
        }
        else
        {
            initialValue = preset.applyMode == FmodParameterApplyMode.OneShotApply
                ? ClampValueByKind(preset.oneShotValue, parameter)
                : ClampValueByKind(Mathf.Lerp(preset.minValue, preset.maxValue, EvaluatePresetCurve(preset.curveMode, initialProgress)), parameter);
        }

        FmodParameterPresetRuntime runtime = RentPresetRuntime();
        runtime.Configure(
            preset.presetId,
            preset.targetParameterName,
            parameter,
            initialProgress,
            initialValue,
            preset.applyMode == FmodParameterApplyMode.OneShotApply);
        return runtime;
    }
    public bool TryGetCurrentInstance(int slotIndex, out EventInstance instance)
    {
        instance = default;

        if (!TryGetSlot(slotIndex, out FmodEventSlot slot, out FmodEventSlotRuntime runtime))
            return false;

        if (slot.playbackMode == FmodPlaybackMode.SingleInstance)
        {
            instance = runtime.singleInstance;
            return instance.isValid();
        }

        if (runtime.multiInstances != null && runtime.multiInstances.Count > 0)
        {
            instance = runtime.multiInstances[runtime.multiInstances.Count - 1];
            return instance.isValid();
        }

        return false;
    }
    private void ApplyCurrentPresetValuesToHandle(FmodEventSlot slot, FmodParameterPresetRuntime presetRuntime, EventInstance instance)
    {
        if (slot.parameterPresets == null || presetRuntime == null)
            return;

        FmodParameterOverride parameter = presetRuntime.parameter;
        if (parameter == null)
        {
            parameter = FindParameterOverride(slot, presetRuntime.targetParameterName);
            presetRuntime.parameter = parameter;
        }
        if (parameter == null)
            return;

        ApplyParameterValueToHandleOnly(instance, parameter, presetRuntime.currentValue);
    }

    private void ApplyAllOverridesToHandle(FmodEventSlot slot, EventInstance instance)
    {
        if (!instance.isValid())
            return;

        for (int i = 0; i < slot.parameterOverrides.Count; i++)
        {
            FmodParameterOverride parameter = slot.parameterOverrides[i];
            if (!parameter.useOverride)
                continue;

            if (!parameter.IsEditableInGui)
                continue;

            ApplySingleParameterToHandle(instance, parameter);
        }
    }

    private void ApplySingleParameterToHandle(EventInstance instance, FmodParameterOverride parameter)
    {
        if (parameter == null || string.IsNullOrWhiteSpace(parameter.name))
            return;

        if (!parameter.IsEditableInGui)
            return;

        float clampedValue = ClampValueByKind(parameter.value, parameter);

        if (parameter.scope == FmodParameterScope.Global)
        {
            if (parameter.TryGetParameterId(out PARAMETER_ID parameterId))
                RuntimeManager.StudioSystem.setParameterByID(parameterId, clampedValue);
            else
                RuntimeManager.StudioSystem.setParameterByName(parameter.name, clampedValue);
            return;
        }

        if (instance.isValid())
        {
            if (parameter.TryGetParameterId(out PARAMETER_ID parameterId))
                instance.setParameterByID(parameterId, clampedValue);
            else
                instance.setParameterByName(parameter.name, clampedValue);
        }
    }

    private void ApplyParameterValueToHandleOnly(EventInstance instance, FmodParameterOverride parameter, float value)
    {
        if (parameter == null || string.IsNullOrWhiteSpace(parameter.name))
            return;

        if (!parameter.IsEditableInGui)
            return;

        float clampedValue = ClampValueByKind(value, parameter);

        if (parameter.scope == FmodParameterScope.Global)
        {
            if (parameter.TryGetParameterId(out PARAMETER_ID parameterId))
                RuntimeManager.StudioSystem.setParameterByID(parameterId, clampedValue);
            else
                RuntimeManager.StudioSystem.setParameterByName(parameter.name, clampedValue);
            return;
        }

        if (instance.isValid())
        {
            if (parameter.TryGetParameterId(out PARAMETER_ID parameterId))
                instance.setParameterByID(parameterId, clampedValue);
            else
                instance.setParameterByName(parameter.name, clampedValue);
        }
    }
    public static float EvaluatePresetCurve(FmodParameterCurveMode curveMode, float normalizedTime)
    {
        normalizedTime = Mathf.Clamp01(normalizedTime);

        switch (curveMode)
        {
            case FmodParameterCurveMode.Sine:
                return 0.5f - (0.5f * Mathf.Cos(normalizedTime * Mathf.PI));

            case FmodParameterCurveMode.Linear:
            default:
                return normalizedTime;
        }
    }

    private void AttachIfNeeded(FmodEventSlot slot, EventInstance instance)
    {
        if (!slot.attachToGameObject || !instance.isValid())
            return;

        // A catalog entry mirrors FMOD EventDescription.is3D(). FMOD 2D
        // events do not consume 3D attributes, so registering them with the
        // RuntimeManager only creates an unnecessary AttachedInstance.
        FmodScannedEventEntry selectedEvent = GetSelectedEventEntry(slot);
        if (selectedEvent != null && !selectedEvent.is3D)
            return;

        RuntimeManager.AttachInstanceToGameObject(instance, gameObject);
    }

    private void CleanupReleasedInstances(int slotIndex)
    {
        if (!TryGetSlot(slotIndex, out _, out FmodEventSlotRuntime runtime))
            return;

        for (int i = runtime.multiInstances.Count - 1; i >= 0; i--)
        {
            if (!runtime.multiInstances[i].isValid())
            {
                runtime.multiInstances.RemoveAt(i);
                if (i < runtime.multiPresetIndices.Count) runtime.multiPresetIndices.RemoveAt(i);
                ReleaseMultiPresetRuntimeAt(runtime, i);
                continue;
            }

            PLAYBACK_STATE state;
            if (runtime.multiInstances[i].getPlaybackState(out state) != FMOD.RESULT.OK || state == PLAYBACK_STATE.STOPPED)
            {
                runtime.multiInstances[i].release();
                runtime.multiInstances[i].clearHandle();
                runtime.multiInstances.RemoveAt(i);
                if (i < runtime.multiPresetIndices.Count) runtime.multiPresetIndices.RemoveAt(i);
                ReleaseMultiPresetRuntimeAt(runtime, i);
            }
        }

        if (runtime.singleInstance.isValid())
        {
            PLAYBACK_STATE state;
            if (runtime.singleInstance.getPlaybackState(out state) != FMOD.RESULT.OK ||
                state == PLAYBACK_STATE.STOPPED)
            {
                runtime.singleInstance.release();
                runtime.singleInstance.clearHandle();
                runtime.singlePresetIndex = -1;
                ReleasePresetRuntime(ref runtime.singlePresetRuntime);
            }
        }
    }

    private bool TryGetSlotConfig(int slotIndex, out FmodEventSlot slot)
    {
        slot = null;

        if (eventSlots == null || slotIndex < 0 || slotIndex >= eventSlots.Count)
            return false;

        slot = eventSlots[slotIndex];
        return slot != null;
    }

    private bool TryGetSlot(int slotIndex, out FmodEventSlot slot, out FmodEventSlotRuntime runtime)
    {
        slot = null;
        runtime = null;

        EnsureRuntimeSlots();

        if (!TryGetSlotConfig(slotIndex, out slot))
            return false;

        if (slotIndex < 0 || slotIndex >= runtimeSlots.Count)
            return false;

        runtime = runtimeSlots[slotIndex];
        return runtime != null;
    }

    private FmodScannedEventEntry GetSelectedEventEntry(FmodEventSlot slot)
    {
        if (catalog == null ||
            slot == null ||
            (string.IsNullOrWhiteSpace(slot.selectedEventPath) &&
             string.IsNullOrWhiteSpace(slot.selectedEventGuid)))
            return null;

        FmodScannedEventEntry entry = catalog.FindByPath(slot.selectedEventPath);
        if ((entry == null ||
             (!string.IsNullOrWhiteSpace(slot.selectedEventGuid) &&
              !string.Equals(
                  entry.guidString,
                  slot.selectedEventGuid,
                  StringComparison.OrdinalIgnoreCase))) &&
            !string.IsNullOrWhiteSpace(slot.selectedEventGuid))
        {
            entry = catalog.FindByGuid(slot.selectedEventGuid);
        }

        if (entry == null || !catalog.AllowsEvent(entry))
            return null;

        // FMOD event paths can change when an event is renamed or moved. Keep the
        // serialized path current while the GUID remains the stable identity.
        slot.selectedEventPath = entry.path;
        slot.selectedEventGuid = entry.guidString;
        slot.displayName = string.IsNullOrWhiteSpace(entry.displayName)
            ? entry.eventName
            : entry.displayName;

        return entry;
    }

    private void EnsureRuntimeSlots()
    {
        int slotCount = eventSlots?.Count ?? 0;

        // Event slots are authored before Play Mode. The early return keeps
        // runtime slot validation out of the per-frame hot path while still
        // supporting runtime additions/removals by count.
        if (runtimeSlots != null && runtimeSlots.Count == slotCount)
            return;

        if (runtimeSlots == null)
        {
            runtimeSlots = new List<FmodEventSlotRuntime>(
                slotCount);
        }

        while (runtimeSlots.Count < slotCount)
        {
            int slotIndex = runtimeSlots.Count;
            FmodEventSlotRuntime runtime = new FmodEventSlotRuntime();
            FmodEventSlot slot = eventSlots[slotIndex];
            runtime.EnsureCapacity(slot != null ? slot.maxInstances : 1);
            runtimeSlots.Add(runtime);
        }

        while (runtimeSlots.Count > slotCount)
        {
            int lastIndex = runtimeSlots.Count - 1;
            StopSlotRuntime(runtimeSlots[lastIndex], STOP_MODE.IMMEDIATE);
            runtimeSlots.RemoveAt(lastIndex);
        }

    }

    public static float ClampValueByKind(float value, FmodParameterOverride parameter)
    {
        float clamped = Mathf.Clamp(value, parameter.min, parameter.max);
        return parameter.kind == FmodParameterKind.Discrete || parameter.kind == FmodParameterKind.Labeled
            ? Mathf.Round(clamped)
            : clamped;
    }

    public static int ResolvePresetLabelIndex(FmodParameterOverride parameter, FmodParameterPreset preset)
    {
        if (parameter == null || preset == null)
            return -1;

        int minIndex = Mathf.RoundToInt(parameter.min);
        int maxIndex = Mathf.RoundToInt(parameter.max);

        if (parameter.labels != null && parameter.labels.Count > 0)
        {
            if (!string.IsNullOrWhiteSpace(preset.labelValue))
            {
                int listIndex = parameter.labels.IndexOf(preset.labelValue);
                if (listIndex >= 0)
                {
                    preset.labelIndex = minIndex + listIndex;
                    return preset.labelIndex;
                }
            }

            if (preset.labelIndex >= minIndex && preset.labelIndex <= maxIndex)
            {
                int listIndex = preset.labelIndex - minIndex;
                if (listIndex >= 0 && listIndex < parameter.labels.Count)
                {
                    preset.labelValue = parameter.labels[listIndex];
                    return preset.labelIndex;
                }
            }
        }

        int fallbackIndex = Mathf.RoundToInt(preset.oneShotValue);
        if (fallbackIndex >= minIndex && fallbackIndex <= maxIndex)
        {
            preset.labelIndex = fallbackIndex;

            if (parameter.labels != null)
            {
                int listIndex = fallbackIndex - minIndex;
                if (listIndex >= 0 && listIndex < parameter.labels.Count)
                    preset.labelValue = parameter.labels[listIndex];
            }

            return fallbackIndex;
        }

        return -1;
    }

    private static bool IsHandlePlaying(EventInstance instance)
    {
        if (!instance.isValid())
            return false;

        return instance.getPlaybackState(out PLAYBACK_STATE state) == FMOD.RESULT.OK
            && state != PLAYBACK_STATE.STOPPED;
    }

    private void StopSlotRuntime(FmodEventSlotRuntime runtime, STOP_MODE stopMode)
    {
        if (runtime == null)
            return;

        StopSingleHandle(ref runtime.singleInstance, stopMode);
        runtime.singlePresetIndex = -1;
        ReleasePresetRuntime(ref runtime.singlePresetRuntime);

        StopMultiHandles(runtime.multiInstances, stopMode);
        runtime.multiPresetIndices.Clear();

        for (int i = runtime.multiPresetRuntimes.Count - 1; i >= 0; i--)
            ReleaseMultiPresetRuntimeAt(runtime, i);
    }

    private static void StopSingleHandle(ref EventInstance instance, STOP_MODE stopMode)
    {
        if (!instance.isValid())
            return;

        StopHandle(instance, stopMode);
        instance.clearHandle();
    }

    private static void StopMultiHandles(List<EventInstance> instances, STOP_MODE stopMode)
    {
        if (instances == null)
            return;

        for (int i = instances.Count - 1; i >= 0; i--)
        {
            StopHandle(instances[i], stopMode);
            instances[i].clearHandle();
        }

        instances.Clear();
    }

    private static void StopHandle(EventInstance instance, STOP_MODE stopMode)
    {
        if (!instance.isValid())
            return;

        instance.stop(stopMode);
        instance.release();
    }

    public void StopAll(FMOD.Studio.STOP_MODE stopMode)
    {
        EnsureRuntimeSlots();

        for (int i = 0; i < runtimeSlots.Count; i++)
        {
            FmodEventSlotRuntime runtime = runtimeSlots[i];
            if (runtime == null)
                continue;

            StopSlotRuntime(runtime, stopMode);
        }
    }

    public void SetPausedAll(bool paused)
    {
        localPaused = paused;
        ApplyPauseStateToAllInstances();
    }

    internal void SetManagerPaused(bool paused)
    {
        managerPaused = paused;
        ApplyPauseStateToAllInstances();
    }

    private void ApplyPauseStateToAllInstances()
    {
        EnsureRuntimeSlots();
        bool paused = IsPaused;

        for (int i = 0; i < runtimeSlots.Count; i++)
        {
            FmodEventSlotRuntime runtime = runtimeSlots[i];
            if (runtime == null)
                continue;

            if (runtime.singleInstance.isValid())
                runtime.singleInstance.setPaused(paused);

            for (int j = runtime.multiInstances.Count - 1; j >= 0; j--)
            {
                EventInstance instance = runtime.multiInstances[j];
                if (instance.isValid())
                    instance.setPaused(paused);
            }
        }
    }

    private void ApplyPauseStateToHandle(EventInstance instance)
    {
        if (instance.isValid())
            instance.setPaused(IsPaused);
    }
}

public enum FmodPlaybackMode
{
    SingleInstance,
    MultiInstance
}

[Serializable]
public class FmodEventSlot
{
    [Header("Identity")]
    public string slotName = "Event Slot";

    [Header("Event")]
    [ReadOnly] public string displayName;
    [ReadOnly] public string selectedEventPath;
    [ReadOnly] public string selectedEventGuid;

    [Header("Playback")]
    public FmodPlaybackMode playbackMode = FmodPlaybackMode.SingleInstance;
    public bool playOnStart;
    [Tooltip("Only applies to FMOD 3D events. Catalog-confirmed 2D events are not attached.")]
    public bool attachToGameObject = true;
    public STOP_MODE stopMode = STOP_MODE.IMMEDIATE;
    public bool restartIfAlreadyPlaying = true;

    [Header("Multi Instance")]
    [Min(1)] public int maxInstances = 8;
    public bool stealOldestInstance = true;

    [Header("Parameters")]
    public List<FmodParameterOverride> parameterOverrides = new();
    public List<FmodParameterAutomation> parameterAutomations = new(); // legacy
    public List<FmodParameterPreset> parameterPresets = new();
    public int currentPresetIndex = -1;

    public void Sanitize()
    {
        if (string.IsNullOrWhiteSpace(slotName))
            slotName = "Event Slot";

        if (maxInstances <= 0)
            maxInstances = 1;

        parameterOverrides ??= new List<FmodParameterOverride>();
        parameterAutomations ??= new List<FmodParameterAutomation>();
        parameterPresets ??= new List<FmodParameterPreset>();

        if (parameterPresets.Count == 0)
            currentPresetIndex = -1;
        else
            currentPresetIndex = Mathf.Clamp(currentPresetIndex, -1, parameterPresets.Count - 1);
    }
    public bool HasAutomaticParameters()
    {
        if (parameterOverrides == null)
            return false;

        for (int i = 0; i < parameterOverrides.Count; i++)
        {
            if (parameterOverrides[i].isAutomatic)
                return true;
        }

        return false;
    }
}

[Serializable]
public class FmodParameterOverride
{
    [Header("Identity")]
    [ReadOnly] public string name;
    [ReadOnly] public string idString;

    [Header("Meta")]
    [ReadOnly] public FmodParameterKind kind;
    [ReadOnly] public FmodParameterScope scope;
    [ReadOnly] public bool isReadOnly;
    [ReadOnly] public bool isAutomatic;

    [Header("Range")]
    [ReadOnly] public float min;
    [ReadOnly] public float max;
    [ReadOnly] public float defaultValue;

    [Header("Current Value")]
    public bool useOverride;
    public float value;

    [Header("Labels")]
    [ReadOnly] public List<string> labels = new();

    [NonSerialized] private bool parameterIdInitialized;
    [NonSerialized] private bool hasParameterId;
    [NonSerialized] private PARAMETER_ID parameterId;

    public bool IsEditableInGui => !isReadOnly && !isAutomatic;
    public bool IsBuiltInLike => isAutomatic || isReadOnly;

    public bool TryGetParameterId(out PARAMETER_ID id)
    {
        if (!parameterIdInitialized)
        {
            parameterIdInitialized = true;
            hasParameterId = TryParseParameterId(idString, out parameterId);
        }

        id = parameterId;
        return hasParameterId;
    }

    public static FmodParameterOverride CreateFrom(FmodParameterDefinition definition)
    {
        return new FmodParameterOverride
        {
            name = definition.name,
            idString = definition.idString,
            kind = definition.kind,
            scope = definition.scope,
            isReadOnly = definition.isReadOnly,
            isAutomatic = definition.isAutomatic,
            min = definition.min,
            max = definition.max,
            defaultValue = definition.defaultValue,
            value = definition.defaultValue,
            useOverride = false,
            labels = new List<string>(definition.labels)
        };
    }

    public void CopyMetadataFrom(FmodParameterDefinition definition)
    {
        name = definition.name;
        idString = definition.idString;
        kind = definition.kind;
        scope = definition.scope;
        isReadOnly = definition.isReadOnly;
        isAutomatic = definition.isAutomatic;
        min = definition.min;
        max = definition.max;
        defaultValue = definition.defaultValue;
        labels = new List<string>(definition.labels);
        parameterIdInitialized = false;
        hasParameterId = false;
        parameterId = default;

        if (!IsEditableInGui)
            useOverride = false;
    }

    private static bool TryParseParameterId(string value, out PARAMETER_ID id)
    {
        id = default;
        if (string.IsNullOrEmpty(value))
            return false;

        int separatorIndex = value.IndexOf(':');
        if (separatorIndex <= 0 || separatorIndex >= value.Length - 1)
            return false;

        if (!uint.TryParse(
                value.AsSpan(0, separatorIndex),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint data1) ||
            !uint.TryParse(
                value.AsSpan(separatorIndex + 1),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out uint data2))
        {
            return false;
        }

        id = new PARAMETER_ID
        {
            data1 = data1,
            data2 = data2
        };
        return true;
    }
}
[Serializable]
public class FmodParameterAutomation
{
    [Header("Identity")]
    [ReadOnly] public string parameterName;

    [Header("Behavior")]
    public bool enabled;
    [Min(0.01f)] public float intervalSeconds = 0.2f;
    public float deltaPerStep = 1f;
    public bool loop = true;
    public bool pingPong;
    public bool onlyWhenPlaying = true;
    public bool applyToPlayingInstances = true;

    public static FmodParameterAutomation CreateFor(FmodParameterOverride parameter)
    {
        return new FmodParameterAutomation
        {
            parameterName = parameter.name,
            enabled = false,
            intervalSeconds = 0.2f,
            deltaPerStep = parameter.kind == FmodParameterKind.Discrete ? 1f : Mathf.Max(0.01f, (parameter.max - parameter.min) * 0.05f),
            loop = true,
            pingPong = false,
            onlyWhenPlaying = true,
            applyToPlayingInstances = true
        };
    }


    public void SanitizeFor(FmodParameterOverride parameter)
    {
        parameterName = parameter.name;
        intervalSeconds = Mathf.Max(0.01f, intervalSeconds);

        if (parameter.kind == FmodParameterKind.Discrete && Mathf.Approximately(deltaPerStep, 0f))
            deltaPerStep = 1f;
    }
}

[Serializable]
public class FmodParameterPreset
{
    [Header("Identity")]
    [SerializeField, HideInInspector] public string presetId;
    public string targetParameterName;

    [Header("Behavior")]
    public bool enabled;
    public FmodParameterApplyMode applyMode = FmodParameterApplyMode.OneShotApply;
    [Min(0.01f)] public float durationSeconds = 1f;
    [Min(0.01f)] public float updateInterval = 0.05f;
    public bool loop = true;
    public bool onlyWhenPlaying = true;
    public bool applyToPlayingInstances = true;
    public bool randomizeStart;
    public FmodParameterCurveMode curveMode = FmodParameterCurveMode.Linear;

    [Header("Values")]
    public float oneShotValue;
    public float minValue;
    public float maxValue;

    [Header("Labeled Value")]
    public string labelValue;
    public int labelIndex = -1;

    public void EnsureId()
    {
        if (string.IsNullOrWhiteSpace(presetId))
            presetId = System.Guid.NewGuid().ToString("N");
    }

    public static FmodParameterPreset CreateFor(FmodParameterOverride parameter)
    {
        return new FmodParameterPreset
        {
            presetId = System.Guid.NewGuid().ToString("N"),
            targetParameterName = parameter.name,
            enabled = false,
            applyMode = FmodParameterApplyMode.OneShotApply,
            durationSeconds = 1f,
            updateInterval = 0.05f,
            loop = true,
            onlyWhenPlaying = true,
            applyToPlayingInstances = true,
            randomizeStart = false,
            curveMode = FmodParameterCurveMode.Linear,
            oneShotValue = parameter.defaultValue,
            minValue = parameter.min,
            maxValue = parameter.max,
            labelIndex = Mathf.RoundToInt(parameter.defaultValue),
            labelValue = ResolveDefaultLabel(parameter)
        };
    }

    private static string ResolveDefaultLabel(FmodParameterOverride parameter)
    {
        if (parameter == null || parameter.labels == null || parameter.labels.Count == 0)
            return string.Empty;

        int listIndex = Mathf.RoundToInt(parameter.defaultValue) - Mathf.RoundToInt(parameter.min);
        return listIndex >= 0 && listIndex < parameter.labels.Count
            ? parameter.labels[listIndex]
            : string.Empty;
    }

    public void SanitizeFor(FmodParameterOverride parameter)
    {
        EnsureId();
        targetParameterName = parameter.name;
        durationSeconds = Mathf.Max(0.01f, durationSeconds);
        updateInterval = Mathf.Max(0.01f, updateInterval);
        if (parameter.kind == FmodParameterKind.Labeled)
        {
            applyMode = FmodParameterApplyMode.OneShotApply;
            randomizeStart = false;
            loop = false;

            int resolvedIndex = FmodEventPlayer.ResolvePresetLabelIndex(parameter, this);
            if (resolvedIndex < 0)
            {
                labelIndex = Mathf.RoundToInt(parameter.defaultValue);
                labelValue = ResolveDefaultLabel(parameter);
                oneShotValue = FmodEventPlayer.ClampValueByKind(parameter.defaultValue, parameter);
            }
            else
            {
                oneShotValue = resolvedIndex;
            }

            minValue = parameter.min;
            maxValue = parameter.max;
            return;
        }

        oneShotValue = FmodEventPlayer.ClampValueByKind(oneShotValue, parameter);
        minValue = FmodEventPlayer.ClampValueByKind(minValue, parameter);
        maxValue = FmodEventPlayer.ClampValueByKind(maxValue, parameter);

        if (minValue > maxValue)
            (minValue, maxValue) = (maxValue, minValue);
    }
}

public enum FmodParameterApplyMode
{
    OneShotApply,
    Sweep,
    PingPong
}

public enum FmodParameterCurveMode
{
    Linear,
    Sine
}

[Serializable]
public class FmodEventSlotRuntime
{
    public EventInstance singleInstance;
    public int singlePresetIndex = -1;
    public FmodParameterPresetRuntime singlePresetRuntime;

    public List<EventInstance> multiInstances = new();
    public List<int> multiPresetIndices = new();
    public List<FmodParameterPresetRuntime> multiPresetRuntimes = new();

    public void EnsureCapacity(int capacity)
    {
        capacity = Mathf.Max(1, capacity);

        if (multiInstances.Capacity < capacity)
            multiInstances.Capacity = capacity;
        if (multiPresetIndices.Capacity < capacity)
            multiPresetIndices.Capacity = capacity;
        if (multiPresetRuntimes.Capacity < capacity)
            multiPresetRuntimes.Capacity = capacity;
    }
}

[Serializable]
public class FmodParameterPresetRuntime
{
    public string PresetId;
    public string targetParameterName;
    public FmodParameterOverride parameter;
    public float elapsed;
    public float direction = 1f;
    public float progress;
    public float currentValue;
    public bool oneShotApplied;

    public void Configure(
        string presetId,
        string parameterName,
        FmodParameterOverride parameterOverride,
        float initialProgress,
        float initialValue,
        bool isOneShotApplied)
    {
        PresetId = presetId;
        targetParameterName = parameterName;
        parameter = parameterOverride;
        elapsed = 0f;
        direction = 1f;
        progress = initialProgress;
        currentValue = initialValue;
        oneShotApplied = isOneShotApplied;
    }

    public void Reset()
    {
        PresetId = null;
        targetParameterName = null;
        parameter = null;
        elapsed = 0f;
        direction = 1f;
        progress = 0f;
        currentValue = 0f;
        oneShotApplied = false;
    }
}
