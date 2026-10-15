using System;
using System.Collections.Generic;
using FMOD.Studio;
using UnityEngine;
using UnityEngine.Serialization;
using RuntimeManager = FMODUnity.RuntimeManager;

public enum AudioPauseReason
{
    Manual,
    GamePause,
    PauseAll,
    Cutscene,
    ApplicationFocus
}

/// <summary>
/// AudioManager에 등록된 한 FmodEventPlayer를 현재 등록 수명 동안 식별하는 키.
/// default(0)는 유효하지 않으며, 해제 후 같은 플레이어가 재등록되어도 이전 키는 재사용하지 않는다.
/// </summary>
public readonly struct AudioPlayerHandle : IEquatable<AudioPlayerHandle>
{
    public ulong Value { get; }
    public bool IsValid => Value != 0;

    public AudioPlayerHandle(ulong value)
    {
        Value = value;
    }

    public bool Equals(AudioPlayerHandle other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object obj)
    {
        return obj is AudioPlayerHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return IsValid ? Value.ToString() : "Invalid";
    }

    public static bool operator ==(AudioPlayerHandle left, AudioPlayerHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AudioPlayerHandle left, AudioPlayerHandle right)
    {
        return !left.Equals(right);
    }
}

[Serializable]
public sealed class AudioBusPauseSetting
{
    [SerializeField, HideInInspector] private string busGuid;
    [SerializeField, HideInInspector] private string busPath;
    [SerializeField] private bool pauseOnGamePause;

    public string BusGuid => busGuid;
    public string BusPath => busPath;
    public bool PauseOnGamePause => pauseOnGamePause;

    public AudioBusPauseSetting(
        FmodScannedBusEntry busInfo,
        bool pauseOnGamePause = false)
    {
        this.pauseOnGamePause = pauseOnGamePause;
        SynchronizeIdentity(busInfo);
    }

    internal bool Matches(FmodScannedBusEntry busInfo)
    {
        if (busInfo == null)
            return false;

        if (!string.IsNullOrWhiteSpace(busGuid) &&
            !string.IsNullOrWhiteSpace(busInfo.guidString))
        {
            return string.Equals(
                busGuid,
                busInfo.guidString,
                StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            FmodEventCatalog.NormalizeBusPath(busPath),
            FmodEventCatalog.NormalizeBusPath(busInfo.path),
            StringComparison.OrdinalIgnoreCase);
    }

    internal void SynchronizeIdentity(FmodScannedBusEntry busInfo)
    {
        if (busInfo == null)
            return;

        busGuid = busInfo.guidString ?? string.Empty;
        busPath = FmodEventCatalog.NormalizeBusPath(busInfo.path);
    }
}

/// <summary>
/// 활성화된 FmodEventPlayer를 한곳에서 조회하고 제어하는 전역 오디오 매니저.
/// 씬에는 하나만 배치하며, 최초 인스턴스는 씬 전환 후에도 유지된다.
/// </summary>
[DefaultExecutionOrder(-1000)]
[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    private static ulong nextHandleValue;

    public static AudioManager Instance { get; private set; }

    private readonly HashSet<FmodEventPlayer> players = new();
    private readonly Dictionary<AudioPlayerHandle, FmodEventPlayer> playersByHandle = new();
    private readonly Dictionary<FmodEventPlayer, AudioPlayerHandle> handlesByPlayer = new();
    private readonly Dictionary<AudioPlayerHandle, AudioPlayerId> playerIdsByHandle = new();
    private readonly Dictionary<AudioPlayerId, HashSet<AudioPlayerHandle>> handlesByPlayerId = new();

    private readonly Dictionary<string, HashSet<AudioPauseReason>> pauseReasonsByBus =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Bus> busesByPath =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> warnedBusPaths =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<AudioPlayerId> warnedAmbiguousIds = new();
    private readonly HashSet<AudioPlayerId> warnedUnknownIds = new();
    private readonly List<AudioPlayerHandle> staleHandleScratch = new();
    private readonly List<FmodEventPlayer> stalePlayerScratch = new();
    private readonly List<AudioPlayerHandle> registrationHandleScratch = new();
    private readonly HashSet<string> gamePauseBusScratch =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> previouslyGamePausedBusScratch = new();
    private readonly List<AudioBusPauseSetting> synchronizedBusSettingsScratch = new();
    private readonly HashSet<AudioBusPauseSetting> usedBusSettingsScratch = new();
    private readonly HashSet<string> pendingBusPausePaths =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> pendingBusPauseScratch = new();
    private readonly List<string> knownBusPaths = new();
    private readonly HashSet<string> knownBusPathSet =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, FmodScannedBusEntry> knownBusInfoByPath =
        new(StringComparer.OrdinalIgnoreCase);
    private bool knownBusCacheDirty = true;
    private bool pauseAllActive;
    private bool gamePauseActive;
    private float nextBusPauseRetryTime;

    private const float BusPauseRetryIntervalSeconds = 1f;

    /// <summary>현재 활성화되어 매니저에 등록된 모든 플레이어.</summary>
    public IReadOnlyCollection<FmodEventPlayer> Players => players;
    public int PlayerCount => players.Count;
    public IReadOnlyList<AudioPlayerIdDefinition> PlayerIdDefinitions => playerIdDefinitions;
    public IReadOnlyList<AudioBusPauseSetting> BusPauseSettings => busPauseSettings;
    public bool IsGamePaused => gamePauseActive;

    /// <summary>
    /// AudioManager에 연결된 Event Catalog의 Bank scan 결과에서 가져온 Bus 경로 목록.
    /// </summary>
    public IReadOnlyList<string> KnownBusPaths
    {
        get
        {
            RebuildKnownBusCacheIfNeeded();
            return knownBusPaths;
        }
    }

    public int KnownBusCount
    {
        get
        {
            RebuildKnownBusCacheIfNeeded();
            return knownBusPaths.Count;
        }
    }

    public bool IsKnownPlayerId(AudioPlayerId playerId)
    {
        if (!playerId.IsValid || playerIdDefinitions == null)
            return false;

        for (int i = 0; i < playerIdDefinitions.Count; i++)
        {
            AudioPlayerIdDefinition definition = playerIdDefinitions[i];
            if (definition != null && definition.Id == playerId)
                return true;
        }

        return false;
    }

    public string GetPlayerIdName(AudioPlayerId playerId)
    {
        if (playerIdDefinitions != null)
        {
            for (int i = 0; i < playerIdDefinitions.Count; i++)
            {
                AudioPlayerIdDefinition definition = playerIdDefinitions[i];
                if (definition != null && definition.Id == playerId)
                    return definition.DisplayName;
            }
        }

        return playerId.IsValid ? "Missing ID (" + playerId.Key + ")" : "None";
    }

    internal bool TryFindPlayerIdByName(
        string displayName,
        out AudioPlayerId playerId)
    {
        playerId = default;
        if (string.IsNullOrWhiteSpace(displayName) || playerIdDefinitions == null)
            return false;

        string normalizedName = displayName.Trim();
        for (int i = 0; i < playerIdDefinitions.Count; i++)
        {
            AudioPlayerIdDefinition definition = playerIdDefinitions[i];
            if (definition == null ||
                !string.Equals(
                    definition.DisplayName,
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            playerId = definition.Id;
            return true;
        }

        return false;
    }

    internal AudioPlayerId GetOrCreatePlayerId(string preferredName)
    {
        EnsurePlayerIdDefinitions();

        if (TryFindPlayerIdByName(preferredName, out AudioPlayerId existingId))
            return existingId;

        return CreatePlayerId(preferredName);
    }

    internal AudioPlayerId CreatePlayerId(string preferredName)
    {
        EnsurePlayerIdDefinitions();

        string uniqueName = CreateUniquePlayerIdName(preferredName);
        AudioPlayerIdDefinition definition = new(
            Guid.NewGuid().ToString("N"),
            uniqueName);
        playerIdDefinitions.Add(definition);
        return definition.Id;
    }

    internal bool RemovePlayerId(AudioPlayerId playerId)
    {
        if (!playerId.IsValid || playerIdDefinitions == null)
            return false;

        for (int i = 0; i < playerIdDefinitions.Count; i++)
        {
            AudioPlayerIdDefinition definition = playerIdDefinitions[i];
            if (definition == null || definition.Id != playerId)
                continue;

            playerIdDefinitions.RemoveAt(i);
            warnedUnknownIds.Remove(playerId);
            warnedAmbiguousIds.Remove(playerId);
            return true;
        }

        return false;
    }

    internal bool EnsurePlayerIdDefinitions()
    {
        bool changed = false;
        playerIdDefinitions ??= new List<AudioPlayerIdDefinition>();

        HashSet<string> usedKeys = new(StringComparer.Ordinal);
        for (int i = 0; i < playerIdDefinitions.Count; i++)
        {
            if (playerIdDefinitions[i] == null)
            {
                playerIdDefinitions[i] = new AudioPlayerIdDefinition(
                    Guid.NewGuid().ToString("N"),
                    "Unnamed Player ID");
                changed = true;
            }

            changed |= playerIdDefinitions[i].EnsureValid(usedKeys);
        }

        return changed;
    }

    private string CreateUniquePlayerIdName(string preferredName)
    {
        string baseName = string.IsNullOrWhiteSpace(preferredName)
            ? "New Player ID"
            : preferredName.Trim();
        string candidate = baseName;
        int suffix = 2;

        while (HasPlayerIdName(candidate))
            candidate = baseName + " " + suffix++;

        return candidate;
    }

    private bool HasPlayerIdName(string displayName)
    {
        if (playerIdDefinitions == null)
            return false;

        for (int i = 0; i < playerIdDefinitions.Count; i++)
        {
            AudioPlayerIdDefinition definition = playerIdDefinitions[i];
            if (definition != null && string.Equals(
                    definition.DisplayName,
                    displayName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// playerId가 정확히 하나의 플레이어를 가리킬 때 해당 플레이어를 반환한다.
    /// 같은 ID가 여러 플레이어를 가리키면 모호하므로 null을 반환한다.
    /// </summary>
    public FmodEventPlayer this[AudioPlayerId playerId] => GetPlayer(playerId);

    public event Action<FmodEventPlayer> PlayerRegistered;
    public event Action<FmodEventPlayer> PlayerUnregistered;
    public event Action<FmodEventPlayer, AudioPlayerHandle> PlayerRegisteredWithHandle;
    public event Action<FmodEventPlayer, AudioPlayerHandle> PlayerUnregisteredWithHandle;

    [Header("Player IDs")]
    [SerializeField] private List<AudioPlayerIdDefinition> playerIdDefinitions = new();

    [Header("Bus Catalog Sources")]
    [Tooltip(
        "FMOD Bank scan 결과에서 Bus 구조를 가져올 Catalog입니다. " +
        "AudioManager의 Bus 목록과 Game Pause 정책은 이 Catalog들로부터 자동 동기화됩니다.")]
    [SerializeField] private List<FmodEventCatalog> busCatalogs = new();

    [FormerlySerializedAs("busBindings")]
    [SerializeField, HideInInspector]
    private List<AudioBusPauseSetting> busPauseSettings = new();

    [Header("Debug")]
    [SerializeField] private bool logRegister;
    [SerializeField] private bool logWarnings = true;

    private void OnValidate()
    {
        EnsurePlayerIdDefinitions();
        knownBusCacheDirty = true;
        SynchronizeBusPauseSettings();

        if (Application.isPlaying && gamePauseActive)
            ApplyGamePauseSettings();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsurePlayerIdDefinitions();
        SynchronizeBusPauseSettings();

        // 실행 순서나 런타임 생성 순서 때문에 OnEnable 등록을 놓친 플레이어도 수집한다.
        RegisterExistingPlayers();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        Instance = null;
        ClearAllRegistrations();
        ReleaseManagedBusPauses();
        pauseReasonsByBus.Clear();
    }

    private void Update()
    {
        if (pendingBusPausePaths.Count == 0 ||
            Time.unscaledTime < nextBusPauseRetryTime)
        {
            return;
        }

        nextBusPauseRetryTime = Time.unscaledTime + BusPauseRetryIntervalSeconds;
        pendingBusPauseScratch.Clear();
        pendingBusPauseScratch.AddRange(pendingBusPausePaths);

        for (int i = 0; i < pendingBusPauseScratch.Count; i++)
            ApplyBusPauseState(pendingBusPauseScratch[i]);
    }

    /// <summary>이미 활성화되어 있는 FmodEventPlayer를 찾아 등록한다.</summary>
    public void RegisterExistingPlayers()
    {
        FmodEventPlayer[] existingPlayers = FindObjectsByType<FmodEventPlayer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < existingPlayers.Length; i++)
        {
            FmodEventPlayer player = existingPlayers[i];
            if (player != null && player.isActiveAndEnabled)
                TryRegister(player, out _);
        }
    }

    /// <summary>
    /// 하위 호환용 등록 API. 등록 Handle이 필요하면 TryRegister를 사용한다.
    /// </summary>
    public bool Register(FmodEventPlayer player)
    {
        return TryRegister(player, out _);
    }

    public bool TryRegister(
        FmodEventPlayer player,
        out AudioPlayerHandle handle)
    {
        handle = default;
        if (player == null)
            return false;

        CleanupNullPlayers();

        if (handlesByPlayer.TryGetValue(player, out AudioPlayerHandle existingHandle) &&
            playersByHandle.TryGetValue(existingHandle, out FmodEventPlayer existingPlayer) &&
            existingPlayer == player)
        {
            handle = existingHandle;
            player.AssignRegistrationHandle(handle);
            player.SetManagerPaused(pauseAllActive);
            return true;
        }

        handlesByPlayer.Remove(player);

        handle = CreateHandle();
        players.Add(player);
        playersByHandle.Add(handle, player);
        handlesByPlayer[player] = handle;

        player.TryMigrateLegacyPlayerId(this);
        AudioPlayerId id = player.PlayerId;
        if (id.IsValid && !IsKnownPlayerId(id))
        {
            WarnUnknownId(id, player);
            id = default;
        }

        playerIdsByHandle[handle] = id;

        if (id.IsValid)
        {
            if (!handlesByPlayerId.TryGetValue(id, out HashSet<AudioPlayerHandle> idHandles))
            {
                idHandles = new HashSet<AudioPlayerHandle>();
                handlesByPlayerId.Add(id, idHandles);
            }

            idHandles.Add(handle);
            if (idHandles.Count > 1)
                WarnAmbiguousId(id, idHandles.Count, player);
        }

        player.AssignRegistrationHandle(handle);
        player.SetManagerPaused(pauseAllActive);

        if (logRegister)
        {
            Debug.Log(
                "[AudioManager] Registered: " + player.name + " (Handle: " + handle + ")",
                player);
        }

        PlayerRegisteredWithHandle?.Invoke(player, handle);
        PlayerRegistered?.Invoke(player);
        return true;
    }

    public bool Unregister(FmodEventPlayer player)
    {
        if (player == null ||
            !handlesByPlayer.TryGetValue(player, out AudioPlayerHandle handle))
        {
            return false;
        }

        return Unregister(player, handle);
    }

    public bool Unregister(
        FmodEventPlayer player,
        AudioPlayerHandle handle)
    {
        if (player == null || !handle.IsValid)
            return false;

        if (!playersByHandle.TryGetValue(handle, out FmodEventPlayer registeredPlayer) ||
            registeredPlayer != player)
        {
            return false;
        }

        RemoveRegistration(handle, registeredPlayer, true);
        return true;
    }

    public bool TryGetHandle(
        FmodEventPlayer player,
        out AudioPlayerHandle handle)
    {
        handle = default;
        if (player == null)
            return false;

        if (!handlesByPlayer.TryGetValue(player, out handle) ||
            !playersByHandle.TryGetValue(handle, out FmodEventPlayer registeredPlayer) ||
            registeredPlayer != player)
        {
            handle = default;
            return false;
        }

        return true;
    }

    public FmodEventPlayer GetPlayer(AudioPlayerHandle handle)
    {
        return TryGetPlayer(handle, out FmodEventPlayer player) ? player : null;
    }

    public bool TryGetPlayer(
        AudioPlayerHandle handle,
        out FmodEventPlayer player)
    {
        player = null;
        if (!handle.IsValid)
            return false;

        if (!playersByHandle.TryGetValue(handle, out player) || player == null)
        {
            CleanupNullPlayers();
            player = null;
            return false;
        }

        return true;
    }

    public FmodEventPlayer GetPlayer(AudioPlayerId playerId)
    {
        return TryGetPlayer(playerId, out FmodEventPlayer player) ? player : null;
    }

    public bool TryGetPlayer(AudioPlayerId playerId, out FmodEventPlayer player)
    {
        player = null;
        if (!playerId.IsValid)
            return false;

        CleanupNullPlayers();

        if (!handlesByPlayerId.TryGetValue(playerId, out HashSet<AudioPlayerHandle> handles) ||
            handles.Count == 0)
        {
            warnedAmbiguousIds.Remove(playerId);
            return false;
        }

        if (handles.Count != 1)
        {
            WarnAmbiguousId(playerId, handles.Count, null);
            return false;
        }

        foreach (AudioPlayerHandle handle in handles)
            return TryGetPlayer(handle, out player);

        return false;
    }

    /// <summary>요청 객체의 등록된 Player와 ID가 일치할 때만 대상을 반환한다.</summary>
    public bool TryGetPlayer(
        AudioPlayerId playerId,
        GameObject requesterObject,
        out FmodEventPlayer player)
    {
        player = null;
        if (!playerId.IsValid || requesterObject == null ||
            !requesterObject.TryGetComponent(out FmodEventPlayer requesterPlayer) ||
            !TryGetHandle(requesterPlayer, out AudioPlayerHandle handle) ||
            !playerIdsByHandle.TryGetValue(handle, out AudioPlayerId registeredId) ||
            registeredId != playerId)
        {
            return false;
        }

        return TryGetPlayer(handle, out player);
    }

    public bool Stop(AudioPlayerId playerId, GameObject requesterObject, int slotIndex,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(playerId, requesterObject, out FmodEventPlayer player) &&
               player.Stop(slotIndex, stopMode);
    }

    public bool Stop(AudioPlayerId playerId, GameObject requesterObject, string slotName,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(playerId, requesterObject, out FmodEventPlayer player) &&
               player.Stop(slotName, stopMode);
    }

    public IReadOnlyList<FmodEventPlayer> GetPlayers(AudioPlayerId playerId)
    {
        List<FmodEventPlayer> result = new();
        GetPlayersNonAlloc(playerId, result);
        return result;
    }

    public int GetPlayersNonAlloc(
        AudioPlayerId playerId,
        List<FmodEventPlayer> result)
    {
        if (result == null)
            throw new ArgumentNullException(nameof(result));

        result.Clear();
        if (!playerId.IsValid)
            return 0;

        CleanupNullPlayers();

        if (!handlesByPlayerId.TryGetValue(playerId, out HashSet<AudioPlayerHandle> handles))
            return 0;

        foreach (AudioPlayerHandle handle in handles)
        {
            if (playersByHandle.TryGetValue(handle, out FmodEventPlayer player) &&
                player != null)
            {
                result.Add(player);
            }
        }

        return result.Count;
    }

    public bool TryGetPlayer(
        FmodEventPlayer target,
        out FmodEventPlayer player)
    {
        player = null;
        if (!TryGetHandle(target, out AudioPlayerHandle handle))
            return false;

        return TryGetPlayer(handle, out player);
    }

    public bool TryGetPlayer(GameObject targetObj, out FmodEventPlayer player)
    {
        player = null;

        if (targetObj == null ||
            !targetObj.TryGetComponent(out FmodEventPlayer target))
        {
            return false;
        }

        return TryGetPlayer(target, out player);
    }

    public bool Play(AudioPlayerId playerId, int slotIndex)
    {
        return TryGetPlayer(playerId, out FmodEventPlayer player) &&
               Play(player, slotIndex);
    }

    public bool Play(AudioPlayerId playerId, string slotName)
    {
        return TryGetPlayer(playerId, out FmodEventPlayer player) &&
               Play(player, slotName);
    }

    public bool Play(
        AudioPlayerId playerId,
        GameObject requesterObject,
        int slotIndex)
    {
        return TryGetPlayer(playerId, requesterObject, out FmodEventPlayer player) &&
               Play(player, slotIndex);
    }

    public bool Play(
        AudioPlayerId playerId,
        GameObject requesterObject,
        string slotName)
    {
        return TryGetPlayer(playerId, requesterObject, out FmodEventPlayer player) &&
               Play(player, slotName);
    }

    public bool Play(AudioPlayerHandle handle, int slotIndex)
    {
        return TryGetPlayer(handle, out FmodEventPlayer player) &&
               Play(player, slotIndex);
    }

    public bool Play(AudioPlayerHandle handle, string slotName)
    {
        return TryGetPlayer(handle, out FmodEventPlayer player) &&
               Play(player, slotName);
    }

    public bool Play(FmodEventPlayer target, int slotIndex)
    {
        if (!TryGetPlayer(target, out FmodEventPlayer player))
            return false;

        return player.TryPlay(slotIndex);
    }

    public bool Play(FmodEventPlayer target, string slotName)
    {
        if (!TryGetPlayer(target, out FmodEventPlayer player) ||
            player.FindSlotIndex(slotName) < 0)
        {
            return false;
        }

        return player.TryPlay(slotName);
    }

    public bool Play(GameObject targetObj, int slotIndex)
    {
        return TryGetPlayer(targetObj, out FmodEventPlayer player) &&
               Play(player, slotIndex);
    }

    public bool Play(GameObject targetObj, string slotName)
    {
        return TryGetPlayer(targetObj, out FmodEventPlayer player) &&
               Play(player, slotName);
    }

    public int PlayAll(AudioPlayerId playerId, int slotIndex)
    {
        if (!playerId.IsValid)
            return 0;

        CleanupNullPlayers();
        if (!handlesByPlayerId.TryGetValue(
                playerId,
                out HashSet<AudioPlayerHandle> handles))
        {
            return 0;
        }

        int playedCount = 0;
        foreach (AudioPlayerHandle handle in handles)
        {
            if (!playersByHandle.TryGetValue(handle, out FmodEventPlayer player) ||
                player == null)
            {
                continue;
            }

            if (player.TryPlay(slotIndex))
                playedCount++;
        }

        return playedCount;
    }

    public int PlayAll(AudioPlayerId playerId, string slotName)
    {
        if (!playerId.IsValid)
            return 0;

        CleanupNullPlayers();
        if (!handlesByPlayerId.TryGetValue(
                playerId,
                out HashSet<AudioPlayerHandle> handles))
        {
            return 0;
        }

        int playedCount = 0;

        foreach (AudioPlayerHandle handle in handles)
        {
            if (!playersByHandle.TryGetValue(handle, out FmodEventPlayer player) ||
                player == null ||
                player.FindSlotIndex(slotName) < 0)
            {
                continue;
            }

            if (player.TryPlay(slotName))
                playedCount++;
        }

        return playedCount;
    }

    public bool Stop(
        AudioPlayerId playerId,
        int slotIndex,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(playerId, out FmodEventPlayer player) &&
               player.Stop(slotIndex, stopMode);
    }

    public bool Stop(
        AudioPlayerId playerId,
        string slotName,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(playerId, out FmodEventPlayer player) &&
               player.Stop(slotName, stopMode);
    }

    public bool Stop(
        AudioPlayerHandle handle,
        int slotIndex,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(handle, out FmodEventPlayer player) &&
               player.Stop(slotIndex, stopMode);
    }

    public bool Stop(
        AudioPlayerHandle handle,
        string slotName,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(handle, out FmodEventPlayer player) &&
               player.Stop(slotName, stopMode);
    }

    public bool Stop(
        FmodEventPlayer target,
        int slotIndex,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(target, out FmodEventPlayer player) &&
               player.Stop(slotIndex, stopMode);
    }

    public bool Stop(
        FmodEventPlayer target,
        string slotName,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(target, out FmodEventPlayer player) &&
               player.Stop(slotName, stopMode);
    }

    public bool Stop(
        GameObject targetObj,
        int slotIndex,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(targetObj, out FmodEventPlayer player) &&
               player.Stop(slotIndex, stopMode);
    }

    public bool Stop(
        GameObject targetObj,
        string slotName,
        STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        return TryGetPlayer(targetObj, out FmodEventPlayer player) &&
               player.Stop(slotName, stopMode);
    }

    public void StopAll(STOP_MODE stopMode = STOP_MODE.ALLOWFADEOUT)
    {
        CleanupNullPlayers();
        foreach (FmodEventPlayer player in players)
        {
            if (player != null)
                player.StopAll(stopMode);
        }
    }

    /// <summary>모든 Player와 Master Bus를 일시정지하거나 해제한다.</summary>
    public void PauseAll(bool paused)
    {
        pauseAllActive = paused;
        SetBusPaused("bus:/", AudioPauseReason.PauseAll, paused);
        CleanupNullPlayers();
        foreach (FmodEventPlayer player in players)
        {
            if (player != null)
                player.SetManagerPaused(paused);
        }
    }

    /// <summary>Catalog Bus 설정에서 선택된 Bus에만 GamePause 사유를 적용한다.</summary>
    public void GamePause(bool paused)
    {
        SetGamePaused(paused);
    }

    public void SetGamePaused(bool paused)
    {
        gamePauseActive = paused;
        ApplyGamePauseSettings();
    }

    public void PauseBus(string busPath, bool paused)
    {
        SetBusPaused(busPath, AudioPauseReason.Manual, paused);
    }

    public void SetBusPaused(
        string busPath,
        AudioPauseReason reason,
        bool paused)
    {
        string path = NormalizeBusPath(busPath);
        if (string.IsNullOrEmpty(path))
            return;

        bool wasPaused = IsBusPaused(path);

        if (!pauseReasonsByBus.TryGetValue(
                path,
                out HashSet<AudioPauseReason> reasons))
        {
            if (!paused)
                return;

            reasons = new HashSet<AudioPauseReason>();
            pauseReasonsByBus.Add(path, reasons);
        }

        if (paused)
            reasons.Add(reason);
        else
            reasons.Remove(reason);

        bool isPaused = IsBusPaused(path);
        if (wasPaused == isPaused)
            return;

        ApplyBusPauseState(path);
    }

    public bool IsBusPaused(string busPath)
    {
        string path = NormalizeBusPath(busPath);
        return !string.IsNullOrEmpty(path) &&
               pauseReasonsByBus.TryGetValue(
                   path,
                   out HashSet<AudioPauseReason> reasons) &&
               reasons.Count > 0;
    }

    public bool IsBusPaused(
        string busPath,
        AudioPauseReason reason)
    {
        string path = NormalizeBusPath(busPath);
        return !string.IsNullOrEmpty(path) &&
               pauseReasonsByBus.TryGetValue(
                   path,
                   out HashSet<AudioPauseReason> reasons) &&
               reasons.Contains(reason);
    }

    public void CleanupNullPlayers()
    {
        staleHandleScratch.Clear();

        foreach (KeyValuePair<AudioPlayerHandle, FmodEventPlayer> pair in playersByHandle)
        {
            if (pair.Value != null)
                continue;

            staleHandleScratch.Add(pair.Key);
        }

        for (int i = 0; i < staleHandleScratch.Count; i++)
        {
            AudioPlayerHandle handle = staleHandleScratch[i];
            playersByHandle.TryGetValue(handle, out FmodEventPlayer stalePlayer);
            RemoveRegistration(handle, stalePlayer, false);
        }

        stalePlayerScratch.Clear();
        foreach (FmodEventPlayer player in players)
        {
            if (player == null)
                stalePlayerScratch.Add(player);
        }

        for (int i = 0; i < stalePlayerScratch.Count; i++)
            players.Remove(stalePlayerScratch[i]);
    }

    /// <summary>Catalog 또는 Binding 변경 뒤 알려진 Bus 목록을 즉시 다시 계산한다.</summary>
    public void RefreshKnownBuses()
    {
        SynchronizeBusPauseSettings();
    }

    /// <summary>
    /// Catalog의 Bus 목록과 Pause 설정을 GUID 기준으로 맞춘다.
    /// Bus 경로가 변경되어도 동일 GUID의 설정값은 유지된다.
    /// </summary>
    public bool SynchronizeBusPauseSettings()
    {
        knownBusCacheDirty = true;
        RebuildKnownBusCacheIfNeeded();

        busPauseSettings ??= new List<AudioBusPauseSetting>();
        synchronizedBusSettingsScratch.Clear();
        usedBusSettingsScratch.Clear();
        if (synchronizedBusSettingsScratch.Capacity < knownBusPaths.Count)
            synchronizedBusSettingsScratch.Capacity = knownBusPaths.Count;

        bool changed = busPauseSettings.Count != knownBusPaths.Count;

        for (int i = 0; i < knownBusPaths.Count; i++)
        {
            string path = knownBusPaths[i];
            if (!knownBusInfoByPath.TryGetValue(path, out FmodScannedBusEntry busInfo))
                continue;

            AudioBusPauseSetting matched = null;
            for (int j = 0; j < busPauseSettings.Count; j++)
            {
                AudioBusPauseSetting candidate = busPauseSettings[j];
                if (candidate == null ||
                    usedBusSettingsScratch.Contains(candidate) ||
                    !candidate.Matches(busInfo))
                {
                    continue;
                }

                matched = candidate;
                break;
            }

            if (matched == null)
            {
                matched = new AudioBusPauseSetting(busInfo);
                changed = true;
            }
            else
            {
                if (!string.Equals(
                        matched.BusGuid,
                        busInfo.guidString,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        matched.BusPath,
                        NormalizeBusPath(busInfo.path),
                        StringComparison.OrdinalIgnoreCase))
                {
                    changed = true;
                }

                matched.SynchronizeIdentity(busInfo);
            }

            usedBusSettingsScratch.Add(matched);
            synchronizedBusSettingsScratch.Add(matched);

            if (i >= busPauseSettings.Count || !ReferenceEquals(busPauseSettings[i], matched))
                changed = true;
        }

        if (changed)
            busPauseSettings = new List<AudioBusPauseSetting>(synchronizedBusSettingsScratch);

        return changed;
    }

    public bool IsKnownBus(string busPath)
    {
        string path = NormalizeBusPath(busPath);
        if (string.IsNullOrEmpty(path))
            return false;

        RebuildKnownBusCacheIfNeeded();
        return knownBusPathSet.Contains(path);
    }

    public bool TryGetKnownBusInfo(
        string busPath,
        out FmodScannedBusEntry busInfo)
    {
        string path = NormalizeBusPath(busPath);
        if (string.IsNullOrEmpty(path))
        {
            busInfo = null;
            return false;
        }

        RebuildKnownBusCacheIfNeeded();
        return knownBusInfoByPath.TryGetValue(path, out busInfo);
    }

    private void RemoveRegistration(
        AudioPlayerHandle handle,
        FmodEventPlayer player,
        bool notify)
    {
        playersByHandle.Remove(handle);

        if (!ReferenceEquals(player, null))
        {
            handlesByPlayer.Remove(player);
            players.Remove(player);
        }

        if (playerIdsByHandle.TryGetValue(handle, out AudioPlayerId id))
        {
            playerIdsByHandle.Remove(handle);

            if (id.IsValid &&
                handlesByPlayerId.TryGetValue(id, out HashSet<AudioPlayerHandle> idHandles))
            {
                idHandles.Remove(handle);

                if (idHandles.Count <= 1)
                    warnedAmbiguousIds.Remove(id);
            }
        }

        if (player == null)
            return;

        player.ClearRegistrationHandle(handle);
        player.SetManagerPaused(false);

        if (logRegister)
        {
            Debug.Log(
                "[AudioManager] Unregistered: " + player.name + " (Handle: " + handle + ")",
                player);
        }

        if (!notify)
            return;

        PlayerUnregisteredWithHandle?.Invoke(player, handle);
        PlayerUnregistered?.Invoke(player);
    }

    private void ClearAllRegistrations()
    {
        registrationHandleScratch.Clear();
        foreach (AudioPlayerHandle handle in playersByHandle.Keys)
            registrationHandleScratch.Add(handle);

        for (int i = 0; i < registrationHandleScratch.Count; i++)
        {
            AudioPlayerHandle handle = registrationHandleScratch[i];
            playersByHandle.TryGetValue(handle, out FmodEventPlayer player);
            RemoveRegistration(handle, player, false);
        }

        players.Clear();
        playersByHandle.Clear();
        handlesByPlayer.Clear();
        playerIdsByHandle.Clear();
        handlesByPlayerId.Clear();
        warnedAmbiguousIds.Clear();
        warnedUnknownIds.Clear();
    }

    private void RebuildKnownBusCacheIfNeeded()
    {
        if (!knownBusCacheDirty)
            return;

        knownBusCacheDirty = false;
        knownBusPaths.Clear();
        knownBusPathSet.Clear();
        knownBusInfoByPath.Clear();

        if (busCatalogs != null)
        {
            for (int i = 0; i < busCatalogs.Count; i++)
                AddKnownBusesFromCatalog(busCatalogs[i]);
        }

        knownBusPaths.Sort(StringComparer.OrdinalIgnoreCase);
    }

    private void AddKnownBusesFromCatalog(FmodEventCatalog catalog)
    {
        if (catalog == null || catalog.buses == null)
            return;

        for (int i = 0; i < catalog.buses.Count; i++)
        {
            FmodScannedBusEntry busInfo = catalog.buses[i];
            if (busInfo != null)
                AddKnownBusPath(busInfo.path, busInfo);
        }
    }

    private void AddKnownBusPath(
        string busPath,
        FmodScannedBusEntry busInfo)
    {
        string path = NormalizeBusPath(busPath);
        if (string.IsNullOrEmpty(path))
            return;

        if (knownBusPathSet.Add(path))
            knownBusPaths.Add(path);

        if (busInfo != null && !knownBusInfoByPath.ContainsKey(path))
            knownBusInfoByPath.Add(path, busInfo);
    }

    private void ApplyGamePauseSettings()
    {
        gamePauseBusScratch.Clear();
        if (gamePauseActive && busPauseSettings != null)
        {
            for (int i = 0; i < busPauseSettings.Count; i++)
            {
                AudioBusPauseSetting setting = busPauseSettings[i];
                if (setting != null && setting.PauseOnGamePause)
                    gamePauseBusScratch.Add(NormalizeBusPath(setting.BusPath));
            }
        }

        previouslyGamePausedBusScratch.Clear();
        foreach (KeyValuePair<string, HashSet<AudioPauseReason>> pair in pauseReasonsByBus)
        {
            if (pair.Value.Contains(AudioPauseReason.GamePause))
                previouslyGamePausedBusScratch.Add(pair.Key);
        }

        for (int i = 0; i < previouslyGamePausedBusScratch.Count; i++)
        {
            string path = previouslyGamePausedBusScratch[i];
            if (!gamePauseBusScratch.Contains(path))
                SetBusPaused(path, AudioPauseReason.GamePause, false);
        }

        foreach (string path in gamePauseBusScratch)
            SetBusPaused(path, AudioPauseReason.GamePause, true);
    }

    private void ApplyBusPauseState(string busPath)
    {
        string path = NormalizeBusPath(busPath);
        if (string.IsNullOrEmpty(path))
            return;

        if (!TryGetBus(path, out Bus bus))
        {
            pendingBusPausePaths.Add(path);
            return;
        }

        FMOD.RESULT result = bus.setPaused(IsBusPaused(path));
        if (result == FMOD.RESULT.OK)
        {
            pendingBusPausePaths.Remove(path);
            return;
        }

        pendingBusPausePaths.Add(path);
        if (logWarnings)
        {
            Debug.LogWarning(
                "[AudioManager] Failed to set bus pause state: " +
                path + " (" + result + ")",
                this);
        }
    }

    public bool TryGetBus(string path, out Bus bus)
    {
        bus = default;
        path = NormalizeBusPath(path);
        if (string.IsNullOrEmpty(path))
            return false;

        if (busesByPath.TryGetValue(path, out Bus cachedBus))
        {
            if (cachedBus.isValid())
            {
                bus = cachedBus;
                return true;
            }

            busesByPath.Remove(path);
        }

        try
        {
            FMOD.RESULT result = RuntimeManager.StudioSystem.getBus(path, out bus);
            if (result == FMOD.RESULT.OK && bus.isValid())
            {
                busesByPath[path] = bus;
                warnedBusPaths.Remove(path);
                return true;
            }

            WarnMissingBus(path, result.ToString());
        }
        catch (Exception exception)
        {
            WarnMissingBus(path, exception.GetType().Name);
        }

        bus = default;
        return false;
    }

    private void WarnMissingBus(string path, string reason)
    {
        if (!logWarnings || !warnedBusPaths.Add(path))
            return;

        Debug.LogWarning(
            "[AudioManager] FMOD bus is not available: " +
            path + " (" + reason + ").",
            this);
    }

    private void ReleaseManagedBusPauses()
    {
        foreach (KeyValuePair<string, HashSet<AudioPauseReason>> pair in pauseReasonsByBus)
        {
            if (pair.Value.Count == 0 ||
                !busesByPath.TryGetValue(pair.Key, out Bus bus) ||
                !bus.isValid())
            {
                continue;
            }

            if (bus.isValid())
                bus.setPaused(false);
        }

        busesByPath.Clear();
        warnedBusPaths.Clear();
        pendingBusPausePaths.Clear();
        pendingBusPauseScratch.Clear();
        pauseReasonsByBus.Clear();
    }

    private void WarnAmbiguousId(
        AudioPlayerId id,
        int count,
        FmodEventPlayer context)
    {
        if (!logWarnings || !warnedAmbiguousIds.Add(id))
            return;

        UnityEngine.Object logContext = context != null ? context : this;
        Debug.LogWarning(
            "[AudioManager] PlayerId '" + GetPlayerIdName(id) + "' refers to " + count +
            " active players. Use Play(playerId, requesterObject, ...) with a " +
            "matching FmodEventPlayer, an AudioPlayerHandle, a direct player reference, " +
            "or GetPlayers/PlayAll for an explicit group operation.",
            logContext);
    }

    private void WarnUnknownId(AudioPlayerId id, FmodEventPlayer context)
    {
        if (!logWarnings || !warnedUnknownIds.Add(id))
            return;

        Debug.LogWarning(
            "[AudioManager] '" + context.name + "' uses a Player ID that is no longer " +
            "registered in this AudioManager. Select a valid ID in the Inspector.",
            context);
    }

    private static AudioPlayerHandle CreateHandle()
    {
        do
        {
            unchecked
            {
                nextHandleValue++;
            }
        }
        while (nextHandleValue == 0);

        return new AudioPlayerHandle(nextHandleValue);
    }

    private static string NormalizeBusPath(string busPath)
    {
        return FmodEventCatalog.NormalizeBusPath(busPath);
    }
}
