#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using FMOD;
using FMOD.Studio;

public static class FmodBankScanner
{
    public static void ScanIntoCatalog(FmodEventCatalog catalog)
    {
        if (catalog == null)
        {
            UnityEngine.Debug.LogError("[FMOD Scanner] Catalog is null.");
            return;
        }

        catalog.SanitizeBankFilter();

        string bankRoot = ResolveBankRoot(catalog);
        UnityEngine.Debug.Log($"[FMOD Scanner] Resolved bank root: {bankRoot}");

        if (string.IsNullOrWhiteSpace(bankRoot))
        {
            UnityEngine.Debug.LogError("[FMOD Scanner] Bank root is empty.");
            return;
        }

        if (!Directory.Exists(bankRoot))
        {
            UnityEngine.Debug.LogError($"[FMOD Scanner] Bank root does not exist: {bankRoot}");
            return;
        }

        string[] discoveredBankFiles = Directory.GetFiles(bankRoot, "*.bank", SearchOption.TopDirectoryOnly);
        string[] bankFiles = FilterBankFilesForCatalog(catalog, discoveredBankFiles);
        Array.Sort(bankFiles, CompareBankLoadOrder);

        UnityEngine.Debug.Log(
            $"[FMOD Scanner] Bank file count: {bankFiles.Length} / Discovered: {discoveredBankFiles.Length} / Filter: {(catalog.UsesBankFilter ? string.Join(", ", catalog.includedBanks) : "(all)")}"
        );
        foreach (string bankFile in bankFiles)
        {
            FileInfo info = new FileInfo(bankFile);
            UnityEngine.Debug.Log(
                $"[FMOD Scanner] Found bank: {bankFile} / Size: {info.Length} bytes / Modified: {info.LastWriteTime}"
            );
        }

        catalog.Clear();
        catalog.lastScannedFolder = bankRoot;
        catalog.lastBankFileCount = bankFiles.Length;
        catalog.lastScanTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (bankFiles.Length == 0)
        {
            UnityEngine.Debug.LogWarning("[FMOD Scanner] No .bank files found. Catalog was cleared.");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return;
        }

        FMOD.Studio.System studioSystem;
        RESULT createResult = FMOD.Studio.System.create(out studioSystem);
        Check(createResult, "System.create");

        try
        {
            RESULT initResult = studioSystem.initialize(
                1024,
                FMOD.Studio.INITFLAGS.NORMAL,
                FMOD.INITFLAGS.NORMAL,
                IntPtr.Zero
            );
            Check(initResult, "studioSystem.initialize");

            // Bus 구조는 Catalog의 event bank 필터와 무관한 프로젝트 전역 정보다.
            // 모든 Bank를 로드한 뒤 Event만 아래에서 Catalog 필터로 걸러낸다.
            LoadBanks(studioSystem, discoveredBankFiles);
            Check(studioSystem.update(), "studioSystem.update");

            List<FmodScannedBusEntry> scannedBuses = CollectBusesFromLoadedBanks(studioSystem);
            scannedBuses.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));

            List<FmodScannedEventEntry> scannedEvents = CollectEventsFromLoadedBanks(studioSystem, catalog);
            scannedEvents.Sort((a, b) => string.CompareOrdinal(a.path, b.path));

            catalog.buses = scannedBuses;
            catalog.lastBusCount = scannedBuses.Count;
            catalog.events = scannedEvents;
            catalog.lastEventCount = scannedEvents.Count;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            UnityEngine.Debug.Log(
                $"[FMOD Scanner] Scan completed. Total buses: {catalog.lastBusCount}, " +
                $"total events: {catalog.lastEventCount}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }
        finally
        {
            try
            {
                studioSystem.unloadAll();
                studioSystem.update();
                studioSystem.release();
            }
            catch (Exception cleanupException)
            {
                UnityEngine.Debug.LogException(cleanupException);
            }
        }
    }

    [MenuItem("FMOD/Plus/Scan Selected Catalog")]
    public static void ScanSelectedCatalogMenu()
    {
        if (Selection.activeObject is not FmodEventCatalog catalog)
        {
            UnityEngine.Debug.LogWarning("[FMOD Scanner] Select a FmodEventCatalog asset first.");
            return;
        }

        ScanIntoCatalog(catalog);
    }

    public static string ResolveBankRoot(FmodEventCatalog catalog)
    {
        // 1. 사용자가 직접 지정한 폴더가 있으면 최우선 사용.
        // 단, 해당 폴더 안에 bank가 없으면 자동 탐색으로 넘어간다.
        if (catalog != null && !string.IsNullOrWhiteSpace(catalog.bankFolderOverride))
        {
            string overrideRoot = NormalizePath(catalog.bankFolderOverride);
            string resolvedOverrideRoot = ResolveExistingBankFolder(overrideRoot);

            if (!string.IsNullOrWhiteSpace(resolvedOverrideRoot))
            {
                UnityEngine.Debug.Log($"[FMOD Scanner] Using bank folder override: {resolvedOverrideRoot}");
                return resolvedOverrideRoot;
            }

            UnityEngine.Debug.LogWarning(
                $"[FMOD Scanner] Bank folder override is invalid or contains no .bank files. Fallback scan will be used. Override: {overrideRoot}"
            );
        }

        // 2. Assets 전체에서 가장 적절한 Bank Root를 찾는다.
        // 기존 코드처럼 StreamingAssets에 bank가 하나라도 있다고 바로 확정하지 않는다.
        // 이래야 StreamingAssets에 오래된 1KB bank가 있고 Build/Desktop에 최신 bank가 있을 때 Build 쪽을 잡을 수 있다.
        string bestRoot = FindBestBankRootUnderAssets();

        if (!string.IsNullOrWhiteSpace(bestRoot))
            return bestRoot;

        // 3. 아무것도 못 찾으면 기존 기본 경로를 반환해서 이후 에러 로그가 명확히 나오게 한다.
        return NormalizePath(Path.Combine(Application.dataPath, "StreamingAssets"));
    }

    private static string ResolveExistingBankFolder(string root)
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            return null;

        if (HasBankFiles(root))
            return NormalizePath(root);

        return FindBestBankRootInDirectory(root);
    }

    private static string FindBestBankRootUnderAssets()
    {
        string assetsRoot = NormalizePath(Application.dataPath);
        return FindBestBankRootInDirectory(assetsRoot);
    }

    private static string FindBestBankRootInDirectory(string searchRoot)
    {
        if (string.IsNullOrWhiteSpace(searchRoot) || !Directory.Exists(searchRoot))
            return null;

        string[] bankFiles;

        try
        {
            bankFiles = Directory.GetFiles(searchRoot, "*.bank", SearchOption.AllDirectories);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[FMOD Scanner] Failed to search bank files under '{searchRoot}'. {e.Message}");
            return null;
        }

        if (bankFiles == null || bankFiles.Length == 0)
            return null;

        Dictionary<string, BankFolderCandidate> candidates = new();

        foreach (string bankFile in bankFiles)
        {
            string folder = NormalizePath(Path.GetDirectoryName(bankFile));
            if (string.IsNullOrWhiteSpace(folder))
                continue;

            if (!candidates.ContainsKey(folder))
                candidates.Add(folder, new BankFolderCandidate(folder));

            candidates[folder].AddBank(bankFile);
        }

        BankFolderCandidate bestCandidate = null;

        foreach (BankFolderCandidate candidate in candidates.Values)
        {
            candidate.CalculateScore();

            UnityEngine.Debug.Log(
                $"[FMOD Scanner] Candidate: {candidate.folder} / Score: {candidate.score} / Banks: {candidate.bankCount} / TotalSize: {candidate.totalSizeBytes} bytes / HasMaster: {candidate.hasMasterBank} / HasStrings: {candidate.hasStringsBank}"
            );

            if (bestCandidate == null || candidate.score > bestCandidate.score)
                bestCandidate = candidate;
        }

        if (bestCandidate == null)
            return null;

        UnityEngine.Debug.Log($"[FMOD Scanner] Auto-detected best bank folder: {bestCandidate.folder}");
        return bestCandidate.folder;
    }

    private sealed class BankFolderCandidate
    {
        public readonly string folder;

        public int bankCount;
        public long totalSizeBytes;
        public DateTime latestWriteTimeUtc;

        public bool hasMasterBank;
        public bool hasStringsBank;
        public bool hasEventBank;

        public int score;

        public BankFolderCandidate(string folder)
        {
            this.folder = NormalizePath(folder);
            latestWriteTimeUtc = DateTime.MinValue;
        }

        public void AddBank(string bankFile)
        {
            if (string.IsNullOrWhiteSpace(bankFile))
                return;

            FileInfo info = new FileInfo(bankFile);
            string fileName = Path.GetFileName(bankFile).ToLowerInvariant();

            bankCount++;
            totalSizeBytes += info.Exists ? info.Length : 0;

            if (info.Exists && info.LastWriteTimeUtc > latestWriteTimeUtc)
                latestWriteTimeUtc = info.LastWriteTimeUtc;

            if (fileName == "master.bank")
                hasMasterBank = true;

            if (fileName == "master.strings.bank" || fileName.EndsWith(".strings.bank"))
                hasStringsBank = true;

            if (fileName != "master.bank" && !fileName.EndsWith(".strings.bank"))
                hasEventBank = true;
        }

        public void CalculateScore()
        {
            string normalizedFolder = folder.ToLowerInvariant();

            score = 0;

            // 실제 FMOD 빌드 결과물일 가능성이 높은 폴더를 우선한다.
            if (normalizedFolder.Contains("/build")) score += 200;
            if (normalizedFolder.Contains("/banks")) score += 120;
            if (normalizedFolder.Contains("/fmod")) score += 80;
            if (normalizedFolder.Contains("/desktop")) score += 60;

            // StreamingAssets도 유효하지만, 구버전 bank가 남아있을 수 있으므로 Build보다 낮게 둔다.
            if (normalizedFolder.Contains("/streamingassets")) score += 40;

            // FMOD Bank Root로서의 핵심 조건.
            if (hasMasterBank) score += 300;
            if (hasStringsBank) score += 300;
            if (hasEventBank) score += 150;

            // 같은 폴더에 bank가 여러 개 모여 있으면 root일 가능성이 높다.
            score += bankCount * 20;

            // 1KB짜리 빈 bank만 있는 폴더를 피하기 위한 보정.
            if (totalSizeBytes >= 1024 * 100) score += 100;
            if (totalSizeBytes >= 1024 * 500) score += 150;
            if (totalSizeBytes >= 1024 * 1000) score += 200;

            // 너무 작은 bank 묶음은 의심.
            if (totalSizeBytes < 1024 * 10) score -= 200;

            // 세트가 불완전하면 감점.
            if (!hasMasterBank) score -= 150;
            if (!hasStringsBank) score -= 150;
            if (!hasEventBank) score -= 100;
        }
    }

    private static bool HasBankFiles(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return false;

        try
        {
            return Directory.GetFiles(folder, "*.bank", SearchOption.TopDirectoryOnly).Length > 0;
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return path;

        return Path.GetFullPath(path).Replace("\\", "/");
    }

    private static string[] FilterBankFilesForCatalog(FmodEventCatalog catalog, string[] bankFiles)
    {
        if (bankFiles == null || bankFiles.Length == 0)
            return Array.Empty<string>();

        if (catalog == null || !catalog.UsesBankFilter)
            return bankFiles;

        List<string> result = new();
        List<string> skipped = new();

        foreach (string bankFile in bankFiles)
        {
            if (IsRequiredDependencyBank(bankFile) || catalog.AllowsBank(bankFile))
            {
                result.Add(bankFile);
                continue;
            }

            skipped.Add(Path.GetFileName(bankFile));
        }

        if (skipped.Count > 0)
            UnityEngine.Debug.Log($"[FMOD Scanner] Skipped banks by catalog filter: {string.Join(", ", skipped)}");

        return result.ToArray();
    }

    private static bool IsRequiredDependencyBank(string bankFile)
    {
        string fileName = Path.GetFileName(bankFile);

        return string.Equals(fileName, "Master.bank", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".strings.bank", StringComparison.OrdinalIgnoreCase);
    }

    private static void LoadBanks(FMOD.Studio.System studioSystem, string[] bankFiles)
    {
        if (bankFiles == null || bankFiles.Length == 0)
            return;

        Array.Sort(bankFiles, CompareBankLoadOrder);

        foreach (string bankFile in bankFiles)
        {
            UnityEngine.Debug.Log($"[FMOD Scanner] Loading bank: {bankFile}");

            Bank bank;
            RESULT loadResult = studioSystem.loadBankFile(bankFile, LOAD_BANK_FLAGS.NORMAL, out bank);

            UnityEngine.Debug.Log($"[FMOD Scanner] Load result: {loadResult}, Bank: {bankFile}");

            if (loadResult == RESULT.ERR_EVENT_ALREADY_LOADED)
            {
                UnityEngine.Debug.LogWarning($"[FMOD Scanner] Bank already loaded, skipping duplicate load: {bankFile}");
                continue;
            }

            Check(loadResult, $"loadBankFile({bankFile})");
        }
    }

    private static int CompareBankLoadOrder(string a, string b)
    {
        int orderA = GetBankLoadOrder(a);
        int orderB = GetBankLoadOrder(b);

        int orderCompare = orderA.CompareTo(orderB);
        if (orderCompare != 0)
            return orderCompare;

        return string.CompareOrdinal(a, b);
    }

    private static int GetBankLoadOrder(string path)
    {
        string fileName = Path.GetFileName(path).ToLowerInvariant();

        if (fileName == "master.bank")
            return 0;

        if (fileName == "master.strings.bank")
            return 1;

        if (fileName.EndsWith(".strings.bank"))
            return 2;

        return 3;
    }

    private static List<FmodScannedBusEntry> CollectBusesFromLoadedBanks(
        FMOD.Studio.System studioSystem)
    {
        List<FmodScannedBusEntry> result = new();
        Dictionary<string, FmodScannedBusEntry> entriesByPath =
            new(StringComparer.OrdinalIgnoreCase);

        Bank[] loadedBanks;
        Check(studioSystem.getBankList(out loadedBanks), "studioSystem.getBankList");

        foreach (Bank bank in loadedBanks)
        {
            string bankPath = "(path unavailable)";
            if (bank.getPath(out string resolvedBankPath) == RESULT.OK &&
                !string.IsNullOrEmpty(resolvedBankPath))
            {
                bankPath = resolvedBankPath;
            }

            RESULT countResult = bank.getBusCount(out int busCount);
            UnityEngine.Debug.Log(
                $"[FMOD Scanner] Bank bus count. Bank: {bankPath}, " +
                $"Result: {countResult}, Count: {busCount}");

            if (countResult != RESULT.OK || busCount <= 0)
                continue;

            RESULT listResult = bank.getBusList(out Bus[] bankBuses);
            if (listResult != RESULT.OK || bankBuses == null)
                continue;

            for (int i = 0; i < bankBuses.Length; i++)
            {
                Bus bus = bankBuses[i];
                RESULT pathResult = bus.getPath(out string busPath);
                busPath = FmodEventCatalog.NormalizeBusPath(busPath);

                if (pathResult != RESULT.OK || string.IsNullOrEmpty(busPath))
                    continue;

                if (entriesByPath.TryGetValue(busPath, out FmodScannedBusEntry existingEntry))
                {
                    AddSourceBank(existingEntry, bankPath);
                    continue;
                }

                RESULT idResult = bus.getID(out FMOD.GUID guid);
                SplitBusPath(busPath, out string busName, out string parentPath);

                FmodScannedBusEntry entry = new()
                {
                    path = busPath,
                    name = busName,
                    parentPath = parentPath,
                    guidString = idResult == RESULT.OK ? guid.ToString() : string.Empty,
                    sourceBankPaths = new List<string>()
                };

                AddSourceBank(entry, bankPath);
                entriesByPath.Add(busPath, entry);
                result.Add(entry);
            }
        }

        return result;
    }

    private static void AddSourceBank(FmodScannedBusEntry entry, string bankPath)
    {
        if (entry == null || string.IsNullOrWhiteSpace(bankPath))
            return;

        entry.sourceBankPaths ??= new List<string>();
        if (!entry.sourceBankPaths.Exists(path =>
                string.Equals(path, bankPath, StringComparison.OrdinalIgnoreCase)))
        {
            entry.sourceBankPaths.Add(bankPath);
        }
    }

    private static void SplitBusPath(
        string busPath,
        out string busName,
        out string parentPath)
    {
        busName = "Master";
        parentPath = string.Empty;

        string normalizedPath = FmodEventCatalog.NormalizeBusPath(busPath);
        if (string.IsNullOrEmpty(normalizedPath) ||
            string.Equals(normalizedPath, "bus:/", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        int lastSlash = normalizedPath.LastIndexOf('/');
        busName = lastSlash >= 0 && lastSlash < normalizedPath.Length - 1
            ? normalizedPath.Substring(lastSlash + 1)
            : normalizedPath;
        parentPath = lastSlash > "bus:".Length
            ? normalizedPath.Substring(0, lastSlash)
            : "bus:/";
    }

    private static List<FmodScannedEventEntry> CollectEventsFromLoadedBanks(
        FMOD.Studio.System studioSystem,
        FmodEventCatalog catalog)
    {
        List<FmodScannedEventEntry> result = new();
        HashSet<string> seenPaths = new();

        int bankCount;
        Check(studioSystem.getBankCount(out bankCount), "studioSystem.getBankCount");

        UnityEngine.Debug.Log($"[FMOD Scanner] Loaded bank count: {bankCount}");

        if (bankCount <= 0)
            return result;

        Bank[] loadedBanks;
        Check(studioSystem.getBankList(out loadedBanks), "studioSystem.getBankList");

        foreach (Bank bank in loadedBanks)
        {
            string bankPath = "(unknown bank path)";
            if (bank.getPath(out bankPath) != RESULT.OK || string.IsNullOrEmpty(bankPath))
                bankPath = "(path unavailable)";

            int eventCount;
            RESULT countResult = bank.getEventCount(out eventCount);

            UnityEngine.Debug.Log(
                $"[FMOD Scanner] Bank event count. Bank: {bankPath}, Result: {countResult}, Count: {eventCount}"
            );

            if (countResult != RESULT.OK || eventCount <= 0)
                continue;

            EventDescription[] eventDescriptions;
            RESULT listResult = bank.getEventList(out eventDescriptions);

            UnityEngine.Debug.Log(
                $"[FMOD Scanner] Event list result. Bank: {bankPath}, Result: {listResult}, ListNull: {eventDescriptions == null}"
            );

            if (listResult != RESULT.OK || eventDescriptions == null)
                continue;

            foreach (EventDescription eventDescription in eventDescriptions)
            {
                FmodScannedEventEntry entry = BuildEventEntry(bankPath, eventDescription);
                if (entry == null)
                    continue;

                if (catalog != null && catalog.UsesBankFilter && !catalog.AllowsEvent(entry))
                {
                    UnityEngine.Debug.Log($"[FMOD Scanner] Event skipped by catalog bank filter: {entry.path} / {entry.sourceBankPath}");
                    continue;
                }

                if (!seenPaths.Add(entry.path))
                {
                    UnityEngine.Debug.LogWarning($"[FMOD Scanner] Duplicate event path skipped: {entry.path}");
                    continue;
                }

                result.Add(entry);
            }
        }

        return result;
    }

    private static FmodScannedEventEntry BuildEventEntry(string sourceBankPath, EventDescription eventDescription)
    {
        string eventPath;
        RESULT pathResult = eventDescription.getPath(out eventPath);

        if (pathResult != RESULT.OK || string.IsNullOrEmpty(eventPath))
        {
            UnityEngine.Debug.LogWarning(
                $"[FMOD Scanner] Failed to read event path from bank '{sourceBankPath}'. Result: {pathResult}"
            );
            return null;
        }

        FMOD.GUID guid;
        RESULT guidResult = eventDescription.getID(out guid);
        if (guidResult != RESULT.OK)
        {
            UnityEngine.Debug.LogWarning(
                $"[FMOD Scanner] Failed to read GUID for event '{eventPath}'. Result: {guidResult}"
            );
            return null;
        }

        SplitEventPath(eventPath, out string folderPath, out string eventName);

        bool isOneShot = false;
        bool isSnapshot = false;
        bool is3D = false;
        bool hasSustainPoint = false;
        int lengthMs = 0;
        float minDistance = 0f;
        float maxDistance = 0f;

        eventDescription.isOneshot(out isOneShot);
        eventDescription.isSnapshot(out isSnapshot);
        eventDescription.is3D(out is3D);
        eventDescription.hasSustainPoint(out hasSustainPoint);
        eventDescription.getLength(out lengthMs);
        eventDescription.getMinMaxDistance(out minDistance, out maxDistance);

        List<FmodParameterDefinition> parameters = ScanParameters(eventDescription);

        UnityEngine.Debug.Log($"[FMOD Scanner] Scanned event: {eventPath} / Params: {parameters.Count}");

        return new FmodScannedEventEntry
        {
            path = eventPath,
            folderPath = folderPath,
            eventName = eventName,
            displayName = eventName,
            sourceBankPath = sourceBankPath,
            guidString = guid.ToString(),

            isOneShot = isOneShot,
            isSnapshot = isSnapshot,
            is3D = is3D,
            hasSustainPoint = hasSustainPoint,
            lengthMs = lengthMs,
            minDistance = minDistance,
            maxDistance = maxDistance,

            parameters = parameters
        };
    }

    private static List<FmodParameterDefinition> ScanParameters(EventDescription eventDescription)
    {
        List<FmodParameterDefinition> result = new();

        int parameterCount = 0;
        RESULT countResult = eventDescription.getParameterDescriptionCount(out parameterCount);
        if (countResult != RESULT.OK || parameterCount <= 0)
            return result;

        for (int i = 0; i < parameterCount; i++)
        {
            PARAMETER_DESCRIPTION desc;
            RESULT descResult = eventDescription.getParameterDescriptionByIndex(i, out desc);
            if (descResult != RESULT.OK)
                continue;

            FmodParameterDefinition parameter = new FmodParameterDefinition
            {
                name = desc.name,
                idString = $"{desc.id.data1}:{desc.id.data2}",
                scanIndex = i,

                kind = ConvertParameterKind(desc),
                scope = ConvertParameterScope(desc),
                isReadOnly = (desc.flags & PARAMETER_FLAGS.READONLY) != 0,
                isAutomatic = (desc.flags & PARAMETER_FLAGS.AUTOMATIC) != 0,

                min = desc.minimum,
                max = desc.maximum,
                defaultValue = desc.defaultvalue,
                labels = new List<string>()
            };

            if (parameter.kind == FmodParameterKind.Labeled)
            {
                parameter.labels = ReadParameterLabels(eventDescription, desc);
            }

            result.Add(parameter);
        }

        return result;
    }

    private static FmodParameterKind ConvertParameterKind(PARAMETER_DESCRIPTION desc)
    {
        if ((desc.flags & PARAMETER_FLAGS.LABELED) != 0)
            return FmodParameterKind.Labeled;

        if ((desc.flags & PARAMETER_FLAGS.DISCRETE) != 0)
            return FmodParameterKind.Discrete;

        return FmodParameterKind.Continuous;
    }

    private static FmodParameterScope ConvertParameterScope(PARAMETER_DESCRIPTION desc)
    {
        return (desc.flags & PARAMETER_FLAGS.GLOBAL) != 0
            ? FmodParameterScope.Global
            : FmodParameterScope.Local;
    }

    private static List<string> ReadParameterLabels(EventDescription eventDescription, PARAMETER_DESCRIPTION desc)
    {
        List<string> labels = new();

        int minIndex = Mathf.RoundToInt(desc.minimum);
        int maxIndex = Mathf.RoundToInt(desc.maximum);

        for (int labelIndex = minIndex; labelIndex <= maxIndex; labelIndex++)
        {
            string label;
            RESULT result = eventDescription.getParameterLabelByID(desc.id, labelIndex, out label);

            if (result == RESULT.OK && !string.IsNullOrEmpty(label))
                labels.Add(label);
            else
                labels.Add($"Label {labelIndex}");
        }

        return labels;
    }

    private static void SplitEventPath(string eventPath, out string folderPath, out string eventName)
    {
        folderPath = "";
        eventName = "";

        if (string.IsNullOrWhiteSpace(eventPath))
            return;

        string normalized = eventPath
            .Replace("event:/", "")
            .Replace("snapshot:/", "");

        int lastSlash = normalized.LastIndexOf('/');

        if (lastSlash < 0)
        {
            eventName = normalized;
            return;
        }

        folderPath = normalized.Substring(0, lastSlash);
        eventName = normalized.Substring(lastSlash + 1);
    }

    private static void Check(RESULT result, string context)
    {
        if (result != RESULT.OK)
            throw new Exception($"[FMOD Scanner] {context} failed: {result}");
    }
}

/// <summary>
/// FMOD Bank 파일이 바뀌면 모든 Event Catalog를 다시 스캔해 Bus/Event 미러를 최신 상태로 유지한다.
/// 에디터 세션 시작 시에도 한 번 동기화한다.
/// </summary>
[InitializeOnLoad]
internal static class FmodBusCatalogAutoSync
{
    private const string InitialSyncSessionKey =
        "FMODPlus.FmodBusCatalogAutoSync.InitialSyncCompleted";

    private static bool syncQueued;
    private static bool syncInProgress;

    static FmodBusCatalogAutoSync()
    {
        if (SessionState.GetBool(InitialSyncSessionKey, false))
            return;

        SessionState.SetBool(InitialSyncSessionKey, true);
        QueueSync();
    }

    [MenuItem("FMOD/Plus/Scan All Catalogs")]
    public static void ScanAllCatalogsMenu()
    {
        QueueSync();
    }

    public static void NotifyBankAssetsChanged()
    {
        QueueSync();
    }

    private static void QueueSync()
    {
        if (syncQueued || syncInProgress)
            return;

        syncQueued = true;
        EditorApplication.delayCall += RunSyncWhenReady;
    }

    private static void RunSyncWhenReady()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += RunSyncWhenReady;
            return;
        }

        syncQueued = false;
        if (syncInProgress)
            return;

        syncInProgress = true;
        try
        {
            string[] catalogGuids = AssetDatabase.FindAssets("t:FmodEventCatalog");
            for (int i = 0; i < catalogGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(catalogGuids[i]);
                FmodEventCatalog catalog =
                    AssetDatabase.LoadAssetAtPath<FmodEventCatalog>(assetPath);

                if (catalog != null)
                    FmodBankScanner.ScanIntoCatalog(catalog);
            }

            if (catalogGuids.Length > 0)
            {
                UnityEngine.Debug.Log(
                    $"[FMOD Bus Sync] Updated {catalogGuids.Length} catalog(s) from Bank files.");
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogException(exception);
        }
        finally
        {
            syncInProgress = false;
        }
    }
}

internal sealed class FmodBankAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        if (ContainsBank(importedAssets) ||
            ContainsBank(deletedAssets) ||
            ContainsBank(movedAssets) ||
            ContainsBank(movedFromAssetPaths))
        {
            FmodBusCatalogAutoSync.NotifyBankAssetsChanged();
        }
    }

    private static bool ContainsBank(string[] assetPaths)
    {
        if (assetPaths == null)
            return false;

        for (int i = 0; i < assetPaths.Length; i++)
        {
            if (assetPaths[i].EndsWith(".bank", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
#endif
