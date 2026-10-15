using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using FMODUnity;
using UnityEngine;

public class FmodBankLoadDebug : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private FmodEventCatalog catalog;

    [Header("Options")]
    [SerializeField] private bool loadCatalogBanksOnStart = true;
    [SerializeField] private bool unloadCatalogBanksOnDestroy;
    [SerializeField] private bool logAfterLoadOnStart = true;
    [SerializeField] private bool logEveryUpdate;
    [SerializeField, Min(0.1f)] private float logIntervalSeconds = 1f;

    private float logTimer;
    private bool loadedByThis;

    private IEnumerator Start()
    {
        if (loadCatalogBanksOnStart && catalog != null)
        {
            catalog.LoadBanks();
            loadedByThis = true;
        }

        if (!logAfterLoadOnStart)
            yield break;

        yield return WaitUntilBankLoadingFinished();
        LogBankState("Start");
    }

    private void Update()
    {
        if (!logEveryUpdate)
            return;

        logTimer += Time.unscaledDeltaTime;
        if (logTimer < logIntervalSeconds)
            return;

        logTimer = 0f;
        LogBankState("Update");
    }

    private void OnDestroy()
    {
        if (loadedByThis && unloadCatalogBanksOnDestroy && catalog != null)
            catalog.UnloadBanks();
    }

    [ContextMenu("Log FMOD Bank State")]
    public void LogBankStateFromContextMenu()
    {
        LogBankState("ContextMenu");
    }

    private static IEnumerator WaitUntilBankLoadingFinished()
    {
        while (!RuntimeManager.HaveAllBanksLoaded)
            yield return null;
    }

    private void LogBankState(string reason)
    {
        IReadOnlyList<string> expectedBanks = catalog != null
            ? catalog.GetRuntimeBankNames()
            : Array.Empty<string>();

        Dictionary<string, int> loadedBanks = GetLoadedBankRefCounts();

        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[FmodBankLoadDebug] Bank state ({reason})");
        builder.AppendLine($"Catalog: {(catalog != null ? catalog.name : "(None)")}");
        builder.AppendLine($"FMOD HaveAllBanksLoaded: {RuntimeManager.HaveAllBanksLoaded}");
        builder.AppendLine("Expected banks from catalog:");

        if (expectedBanks.Count == 0)
        {
            builder.AppendLine("  (none)");
        }
        else
        {
            for (int i = 0; i < expectedBanks.Count; i++)
            {
                string bankName = expectedBanks[i];
                bool loaded = loadedBanks.ContainsKey(bankName);
                builder.AppendLine($"  - {bankName}: {(loaded ? "LOADED" : "MISSING")}");
            }
        }

        builder.AppendLine("Actual RuntimeManager loaded banks:");

        if (loadedBanks.Count == 0)
        {
            builder.AppendLine("  (none)");
        }
        else
        {
            foreach (KeyValuePair<string, int> pair in loadedBanks)
                builder.AppendLine($"  - {pair.Key}: RefCount={pair.Value}");
        }

        Debug.Log(builder.ToString(), this);
    }

    private static Dictionary<string, int> GetLoadedBankRefCounts()
    {
        Dictionary<string, int> result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        Type runtimeManagerType = typeof(RuntimeManager);
        PropertyInfo instanceProperty = runtimeManagerType.GetProperty(
            "Instance",
            BindingFlags.Static | BindingFlags.NonPublic);

        object instance = instanceProperty?.GetValue(null);
        if (instance == null)
            return result;

        FieldInfo loadedBanksField = runtimeManagerType.GetField(
            "loadedBanks",
            BindingFlags.Instance | BindingFlags.NonPublic);

        object loadedBanksObject = loadedBanksField?.GetValue(instance);
        if (loadedBanksObject is not IDictionary loadedBanks)
            return result;

        foreach (DictionaryEntry entry in loadedBanks)
        {
            string bankName = entry.Key as string;
            if (string.IsNullOrWhiteSpace(bankName))
                continue;

            int refCount = GetLoadedBankRefCount(entry.Value);
            result[bankName] = refCount;
        }

        return result;
    }

    private static int GetLoadedBankRefCount(object loadedBank)
    {
        if (loadedBank == null)
            return -1;

        FieldInfo refCountField = loadedBank.GetType().GetField(
            "RefCount",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (refCountField == null)
            return -1;

        object value = refCountField.GetValue(loadedBank);
        return value is int refCount ? refCount : -1;
    }
}
