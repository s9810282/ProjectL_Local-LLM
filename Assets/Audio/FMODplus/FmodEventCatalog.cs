using System;
using System.Collections.Generic;
using System.IO;
using FMODUnity;
using TriInspector;
using UnityEngine;

/// FMOD bank를 스캔해서 얻은 이벤트 목록을 저장하는 카탈로그.
/// FMOD 프로젝트의 이벤트/파라미터 정보를 저장합니다.

[CreateAssetMenu(menuName = "Audio/FMOD Event Catalog")]
public class FmodEventCatalog : ScriptableObject
{
    private static readonly Comparison<string> RuntimeBankLoadComparison =
        CompareRuntimeBankLoadOrder;

    [Header("Scan Settings")]
    [Tooltip(
        "스캔할 bank 폴더의 절대 경로.\n" +
        "예: C:/FMOD/MyProject/Build/Desktop\n" +
        "비워두면 Assets/StreamingAssets 를 기본값으로 사용합니다."
    )]
    public string bankFolderOverride;

    [Header("Bank Filter")]
    [Tooltip(
        "이 Catalog가 사용할 이벤트 bank 목록입니다.\n" +
        "비워두면 기존처럼 모든 이벤트 bank를 스캔합니다.\n" +
        "Master/strings bank는 FMOD 의존성이므로 선택 여부와 관계없이 함께 로드됩니다."
    )]
    public List<string> includedBanks = new();

    [Header("Runtime Loading")]
    [Tooltip("Load bank sample data together with bank metadata when this catalog loads banks at runtime.")]
    public bool loadSampleData = true;

    [Header("Scan Result Info")]
    public string lastScanTime;
    public string lastScannedFolder;
    public int lastBankFileCount;
    public int lastEventCount;
    public int lastBusCount;

    [Header("Scanned Buses")]
    public List<FmodScannedBusEntry> buses = new();

    [Header("Scanned Events")]
    public List<FmodScannedEventEntry> events = new();

    [NonSerialized] private List<string> runtimeBankNamesWithRequired;
    [NonSerialized] private List<string> runtimeBankNamesWithoutRequired;
    [NonSerialized] private HashSet<string> runtimeBankNameScratch;
    [NonSerialized] private bool runtimeBankNamesDirty = true;

    private void OnEnable()
    {
        InvalidateRuntimeBankNames();
    }

    private void OnValidate()
    {
        InvalidateRuntimeBankNames();
    }

    public void Clear()
    {
        InvalidateRuntimeBankNames();
        buses ??= new List<FmodScannedBusEntry>();
        events ??= new List<FmodScannedEventEntry>();

        buses.Clear();
        events.Clear();
        lastBusCount = 0;
        lastEventCount = 0;
    }

    public FmodScannedBusEntry FindBusByPath(string busPath)
    {
        string normalizedPath = NormalizeBusPath(busPath);
        if (string.IsNullOrEmpty(normalizedPath) || buses == null)
            return null;

        for (int i = 0; i < buses.Count; i++)
        {
            FmodScannedBusEntry bus = buses[i];
            if (bus != null && string.Equals(
                    NormalizeBusPath(bus.path),
                    normalizedPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                return bus;
            }
        }

        return null;
    }

    public List<string> GetAllBusPaths()
    {
        List<string> result = new();
        if (buses == null)
            return result;

        HashSet<string> seenPaths = new(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < buses.Count; i++)
        {
            string path = NormalizeBusPath(buses[i]?.path);
            if (!string.IsNullOrEmpty(path) && seenPaths.Add(path))
                result.Add(path);
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    public FmodScannedEventEntry FindByPath(string eventPath)
    {
        if (events == null)
            return null;

        for (int i = 0; i < events.Count; i++)
        {
            FmodScannedEventEntry entry = events[i];
            if (entry != null && string.Equals(
                    entry.path,
                    eventPath,
                    StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public FmodScannedEventEntry FindByGuid(string guidString)
    {
        if (events == null)
            return null;

        for (int i = 0; i < events.Count; i++)
        {
            FmodScannedEventEntry entry = events[i];
            if (entry != null && string.Equals(
                    entry.guidString,
                    guidString,
                    StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public List<FmodScannedEventEntry> FindByFolder(string folderPath)
    {
        return events.FindAll(e => e.folderPath == folderPath);
    }

    public List<string> GetAllFolderPaths()
    {
        HashSet<string> uniqueFolders = new();

        foreach (var evt in events)
        {
            if (string.IsNullOrWhiteSpace(evt.folderPath))
                continue;

            uniqueFolders.Add(evt.folderPath);
        }

        List<string> result = new(uniqueFolders);
        result.Sort(StringComparer.Ordinal);
        return result;
    }

    public IReadOnlyList<string> GetRuntimeBankNames(bool includeRequiredBanks = true)
    {
        RebuildRuntimeBankNamesIfNeeded();
        return includeRequiredBanks
            ? runtimeBankNamesWithRequired
            : runtimeBankNamesWithoutRequired;
    }

    public void InvalidateRuntimeBankNames()
    {
        runtimeBankNamesDirty = true;
    }

    private void RebuildRuntimeBankNamesIfNeeded()
    {
        if (!runtimeBankNamesDirty)
            return;

        runtimeBankNamesDirty = false;
        runtimeBankNamesWithRequired ??= new List<string>();
        runtimeBankNamesWithoutRequired ??= new List<string>();
        runtimeBankNameScratch ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        runtimeBankNamesWithRequired.Clear();
        runtimeBankNamesWithoutRequired.Clear();
        runtimeBankNameScratch.Clear();

        if (UsesBankFilter)
        {
            for (int i = 0; i < includedBanks.Count; i++)
            {
                string bankName = NormalizeBankIdentifier(includedBanks[i]);
                if (!string.IsNullOrWhiteSpace(bankName) && !IsRequiredBank(bankName))
                    runtimeBankNameScratch.Add(bankName);
            }
        }
        else if (events != null)
        {
            for (int i = 0; i < events.Count; i++)
            {
                string bankName = NormalizeBankIdentifier(events[i]?.sourceBankPath);
                if (!string.IsNullOrWhiteSpace(bankName) && !IsRequiredBank(bankName))
                    runtimeBankNameScratch.Add(bankName);
            }
        }

        foreach (string bankName in runtimeBankNameScratch)
            runtimeBankNamesWithoutRequired.Add(bankName);

        runtimeBankNamesWithoutRequired.Sort(RuntimeBankLoadComparison);

        runtimeBankNamesWithRequired.Add("Master");
        runtimeBankNamesWithRequired.Add("Master.strings");
        runtimeBankNamesWithRequired.AddRange(runtimeBankNamesWithoutRequired);
        runtimeBankNamesWithRequired.Sort(RuntimeBankLoadComparison);
    }

    public void LoadBanks()
    {
        FmodCatalogBankRuntimeLoader.Load(this);
    }

    public void UnloadBanks()
    {
        FmodCatalogBankRuntimeLoader.Unload(this);
    }

    public bool UsesBankFilter => includedBanks != null && includedBanks.Count > 0;

    public bool AllowsBank(string bankIdentifier)
    {
        if (!UsesBankFilter)
            return true;

        string normalizedBank = NormalizeBankIdentifier(bankIdentifier);
        if (string.IsNullOrWhiteSpace(normalizedBank))
            return false;

        for (int i = 0; i < includedBanks.Count; i++)
        {
            if (string.Equals(
                    NormalizeBankIdentifier(includedBanks[i]),
                    normalizedBank,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public bool AllowsEvent(FmodScannedEventEntry entry)
    {
        if (entry == null)
            return false;

        return AllowsBank(entry.sourceBankPath);
    }

    public void SanitizeBankFilter()
    {
        InvalidateRuntimeBankNames();
        if (includedBanks == null)
        {
            includedBanks = new List<string>();
            return;
        }

        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        for (int i = includedBanks.Count - 1; i >= 0; i--)
        {
            string normalized = NormalizeBankIdentifier(includedBanks[i]);
            if (string.IsNullOrWhiteSpace(normalized) || !seen.Add(normalized))
            {
                includedBanks.RemoveAt(i);
                continue;
            }

            includedBanks[i] = normalized;
        }

        includedBanks.Sort(StringComparer.OrdinalIgnoreCase);
    }

    public static string NormalizeBankIdentifier(string bankIdentifier)
    {
        if (string.IsNullOrWhiteSpace(bankIdentifier))
            return string.Empty;

        string normalized = bankIdentifier.Trim().Replace("\\", "/");

        if (normalized.StartsWith("bank:/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("bank:/".Length);

        normalized = Path.GetFileName(normalized);

        if (normalized.EndsWith(".bank", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(0, normalized.Length - ".bank".Length);

        return normalized.Trim();
    }

    public static bool IsRequiredBank(string bankIdentifier)
    {
        string normalized = NormalizeBankIdentifier(bankIdentifier);

        return string.Equals(normalized, "Master", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalized, "Master.strings", StringComparison.OrdinalIgnoreCase);
    }

    public static string NormalizeBusPath(string busPath)
    {
        if (string.IsNullOrWhiteSpace(busPath))
            return string.Empty;

        string normalized = busPath.Trim().Replace("\\", "/");
        if (normalized.StartsWith("bus:/", StringComparison.OrdinalIgnoreCase))
            return normalized;

        if (normalized.StartsWith("/", StringComparison.Ordinal))
            return "bus:" + normalized;

        return "bus:/" + normalized.TrimStart('/');
    }

    private static int CompareRuntimeBankLoadOrder(string a, string b)
    {
        int orderA = GetRuntimeBankLoadOrder(a);
        int orderB = GetRuntimeBankLoadOrder(b);

        int orderCompare = orderA.CompareTo(orderB);
        return orderCompare != 0
            ? orderCompare
            : string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetRuntimeBankLoadOrder(string bankName)
    {
        string normalized = NormalizeBankIdentifier(bankName);

        if (string.Equals(normalized, "Master", StringComparison.OrdinalIgnoreCase))
            return 0;

        if (string.Equals(normalized, "Master.strings", StringComparison.OrdinalIgnoreCase))
            return 1;

        if (normalized.EndsWith(".strings", StringComparison.OrdinalIgnoreCase))
            return 2;

        return 3;
    }
}

public static class FmodCatalogBankRuntimeLoader
{
    private static readonly Dictionary<string, LoadedBankReference> loadedBanks = new(StringComparer.OrdinalIgnoreCase);

    public static void Load(FmodEventCatalog catalog)
    {
        if (catalog == null)
            return;

        IReadOnlyList<string> bankNames = catalog.GetRuntimeBankNames();
        for (int i = 0; i < bankNames.Count; i++)
            LoadBank(bankNames[i], catalog.loadSampleData);
    }

    public static void Unload(FmodEventCatalog catalog)
    {
        if (catalog == null)
            return;

        IReadOnlyList<string> bankNames = catalog.GetRuntimeBankNames();
        for (int i = bankNames.Count - 1; i >= 0; i--)
            UnloadBank(bankNames[i]);
    }

    public static bool IsLoaded(string bankName)
    {
        string normalized = FmodEventCatalog.NormalizeBankIdentifier(bankName);
        return !string.IsNullOrWhiteSpace(normalized)
            && loadedBanks.TryGetValue(normalized, out LoadedBankReference loaded)
            && loaded.RefCount > 0;
    }

    private static void LoadBank(string bankName, bool loadSampleData)
    {
        string normalized = bankName;
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        if (loadedBanks.TryGetValue(normalized, out LoadedBankReference loaded))
        {
            loaded.RefCount++;
            loadedBanks[normalized] = loaded;
            return;
        }

        RuntimeManager.LoadBank(normalized, loadSampleData);
        loadedBanks.Add(normalized, new LoadedBankReference { RefCount = 1 });
    }

    private static void UnloadBank(string bankName)
    {
        string normalized = bankName;
        if (string.IsNullOrWhiteSpace(normalized))
            return;

        if (!loadedBanks.TryGetValue(normalized, out LoadedBankReference loaded))
            return;

        loaded.RefCount--;
        if (loaded.RefCount > 0)
        {
            loadedBanks[normalized] = loaded;
            return;
        }

        RuntimeManager.UnloadBank(normalized);
        loadedBanks.Remove(normalized);
    }

    private struct LoadedBankReference
    {
        public int RefCount;
    }
}

[Serializable]
public sealed class FmodScannedBusEntry
{
    [Header("Path Info")]
    [ReadOnly] public string path;
    [ReadOnly] public string name;
    [ReadOnly] public string parentPath;

    [Header("Identity")]
    [HideInInspector] public string guidString;

    [Header("Source Banks")]
    [ReadOnly] public List<string> sourceBankPaths = new();
}

[Serializable]
public class FmodScannedEventEntry
{
    [Header("Path Info")]
    [HideInInspector] public string path;
    public string folderPath;
    public string eventName;
    [HideInInspector] public string displayName;
    [HideInInspector] public string sourceBankPath;

    [Header("Identity")]
    [HideInInspector] public string guidString;

    [Header("Event Info")]
    public bool isOneShot;
    public bool isSnapshot;
    public bool is3D;
    public bool hasSustainPoint;
    public int lengthMs;
    public float minDistance;
    public float maxDistance;

    [Header("Parameters")]
    public List<FmodParameterDefinition> parameters = new();
}

public enum FmodParameterKind
{
    Continuous,
    Discrete,
    Labeled
}

public enum FmodParameterScope
{
    Local,
    Global
}

[Serializable]
public class FmodParameterDefinition
{
    [Header("Identity")]
    [ReadOnly] public string name;
    [HideInInspector] public string idString;
    [ReadOnly] public int scanIndex;

    [Header("Meta")]
    [HideInInspector] public FmodParameterKind kind;
    [ReadOnly] public FmodParameterScope scope;
    [ReadOnly] public bool isReadOnly;
    [ReadOnly] public bool isAutomatic;

    [Header("Range")]
    [ReadOnly] public float min;
    [ReadOnly] public float max;
    [ReadOnly] public float defaultValue;

    [Header("Labels")]
    [ReadOnly] public List<string> labels = new();
}
