#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FmodEventCatalog), true)]
public class FmodEventCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();

        if (target is not FmodEventCatalog catalog)
        {
            EditorGUILayout.HelpBox(
                $"Expected FmodEventCatalog but got: {target?.GetType().FullName}",
                MessageType.Error
            );
            return;
        }

        string resolvedFolder = ResolveBankRootForDisplay(catalog);

        EditorGUILayout.LabelField("Resolved Bank Folder", resolvedFolder);

        bool folderExists = Directory.Exists(resolvedFolder);
        EditorGUILayout.HelpBox(
            folderExists
                ? "Bank folder exists."
                : "Bank folder does not exist. Check bankFolderOverride.",
            folderExists ? MessageType.Info : MessageType.Warning
        );

        EditorGUILayout.Space();
        DrawBankFilterTools(catalog, resolvedFolder);
        EditorGUILayout.Space();

        if (GUILayout.Button("Scan Banks Into Catalog"))
        {
            FmodBankScanner.ScanIntoCatalog(catalog);
        }
    }

    private static void DrawBankFilterTools(FmodEventCatalog catalog, string resolvedFolder)
    {
        catalog.SanitizeBankFilter();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Catalog Bank Filter", EditorStyles.boldLabel);

        bool folderExists = Directory.Exists(resolvedFolder);
        if (!folderExists)
        {
            EditorGUILayout.HelpBox("Bank folder를 찾을 수 없어 선택 가능한 bank 목록을 표시할 수 없습니다.", MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        List<string> selectableBanks = GetSelectableBanks(resolvedFolder);
        if (selectableBanks.Count == 0)
        {
            EditorGUILayout.HelpBox("선택 가능한 이벤트 bank가 없습니다.", MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        string filterSummary = catalog.UsesBankFilter
            ? string.Join(", ", catalog.includedBanks)
            : "All event banks";
        EditorGUILayout.LabelField("Active Filter", filterSummary);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Use All Banks"))
        {
            Undo.RecordObject(catalog, "Use All FMOD Banks");
            catalog.includedBanks.Clear();
            EditorUtility.SetDirty(catalog);
        }

        if (GUILayout.Button("Select All Found"))
        {
            Undo.RecordObject(catalog, "Select All FMOD Banks");
            catalog.includedBanks = new List<string>(selectableBanks);
            catalog.SanitizeBankFilter();
            EditorUtility.SetDirty(catalog);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2f);

        for (int i = 0; i < selectableBanks.Count; i++)
        {
            string bank = selectableBanks[i];
            bool included = !catalog.UsesBankFilter || catalog.AllowsBank(bank);
            bool nextIncluded = EditorGUILayout.ToggleLeft(bank, included);

            if (nextIncluded == included)
                continue;

            Undo.RecordObject(catalog, "Edit FMOD Bank Filter");

            if (!catalog.UsesBankFilter)
                catalog.includedBanks = new List<string>(selectableBanks);

            if (nextIncluded)
            {
                if (!catalog.AllowsBank(bank))
                    catalog.includedBanks.Add(bank);
            }
            else
            {
                catalog.includedBanks.RemoveAll(item =>
                    string.Equals(
                        FmodEventCatalog.NormalizeBankIdentifier(item),
                        FmodEventCatalog.NormalizeBankIdentifier(bank),
                        System.StringComparison.OrdinalIgnoreCase));
            }

            catalog.SanitizeBankFilter();
            EditorUtility.SetDirty(catalog);
        }

        List<string> missingBanks = GetMissingSelectedBanks(catalog, selectableBanks);
        if (missingBanks.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "현재 폴더에서 찾을 수 없는 선택 bank: " + string.Join(", ", missingBanks),
                MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private static List<string> GetSelectableBanks(string folder)
    {
        List<string> banks = new();
        string[] bankFiles = Directory.GetFiles(folder, "*.bank", SearchOption.TopDirectoryOnly);

        for (int i = 0; i < bankFiles.Length; i++)
        {
            string fileName = Path.GetFileName(bankFiles[i]);
            if (fileName.EndsWith(".strings.bank", System.StringComparison.OrdinalIgnoreCase))
                continue;

            banks.Add(FmodEventCatalog.NormalizeBankIdentifier(fileName));
        }

        banks.Sort(System.StringComparer.OrdinalIgnoreCase);
        return banks;
    }

    private static List<string> GetMissingSelectedBanks(FmodEventCatalog catalog, List<string> selectableBanks)
    {
        List<string> missing = new();
        if (catalog == null || !catalog.UsesBankFilter)
            return missing;

        for (int i = 0; i < catalog.includedBanks.Count; i++)
        {
            string selected = FmodEventCatalog.NormalizeBankIdentifier(catalog.includedBanks[i]);
            bool found = false;

            for (int j = 0; j < selectableBanks.Count; j++)
            {
                if (string.Equals(
                        selected,
                        FmodEventCatalog.NormalizeBankIdentifier(selectableBanks[j]),
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
                missing.Add(selected);
        }

        return missing;
    }

    private static string ResolveBankRootForDisplay(FmodEventCatalog catalog)
    {
        return FmodBankScanner.ResolveBankRoot(catalog);
    }
}

[CustomEditor(typeof(AudioManager))]
[CanEditMultipleObjects]
public sealed class AudioManagerEditor : Editor
{
    private SerializedProperty playerIdDefinitions;
    private SerializedProperty busCatalogs;
    private SerializedProperty busPauseSettings;
    private SerializedProperty logRegister;
    private SerializedProperty logWarnings;

    private void OnEnable()
    {
        playerIdDefinitions = serializedObject.FindProperty("playerIdDefinitions");
        busCatalogs = serializedObject.FindProperty("busCatalogs");
        busPauseSettings = serializedObject.FindProperty("busPauseSettings");
        logRegister = serializedObject.FindProperty("logRegister");
        logWarnings = serializedObject.FindProperty("logWarnings");
        EnsurePlayerIdTargets(false);
        SynchronizeTargets(false);
    }

    public override void OnInspectorGUI()
    {
        EnsurePlayerIdTargets(false);
        SynchronizeTargets(false);
        serializedObject.Update();

        DrawPlayerIds();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("FMOD Bus Source", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(busCatalogs, true);
        bool catalogsChanged = EditorGUI.EndChangeCheck();

        if (catalogsChanged)
        {
            serializedObject.ApplyModifiedProperties();
            SynchronizeTargets(false);
            serializedObject.Update();
        }

        EditorGUILayout.Space(6f);
        DrawBusPauseSettings();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(logRegister);
        EditorGUILayout.PropertyField(logWarnings);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPlayerIds()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Player IDs", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(targets.Length != 1))
        {
            if (GUILayout.Button("Add", GUILayout.Width(64f)))
            {
                serializedObject.ApplyModifiedProperties();
                AudioManager manager = (AudioManager)target;
                Undo.RecordObject(manager, "Add Audio Player ID");
                manager.CreatePlayerId("New Player ID");
                EditorUtility.SetDirty(manager);
                serializedObject.Update();
            }
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(
            "enum처럼 사용할 Player ID를 관리합니다. 이름을 바꿔도 기존 참조는 유지됩니다.",
            MessageType.Info);

        if (targets.Length != 1)
        {
            EditorGUILayout.HelpBox(
                "Player ID 목록은 AudioManager 하나를 선택했을 때 편집할 수 있습니다.",
                MessageType.Info);
            EditorGUILayout.EndVertical();
            return;
        }

        int deleteIndex = -1;
        HashSet<string> usedNames = new(System.StringComparer.OrdinalIgnoreCase);

        if (playerIdDefinitions == null || playerIdDefinitions.arraySize == 0)
        {
            EditorGUILayout.LabelField("등록된 Player ID가 없습니다.", EditorStyles.miniLabel);
        }
        else
        {
            for (int i = 0; i < playerIdDefinitions.arraySize; i++)
            {
                SerializedProperty definition = playerIdDefinitions.GetArrayElementAtIndex(i);
                SerializedProperty nameProperty = definition.FindPropertyRelative("displayName");

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(22f));
                EditorGUILayout.PropertyField(nameProperty, GUIContent.none);
                if (GUILayout.Button("Delete", GUILayout.Width(64f)))
                    deleteIndex = i;
                EditorGUILayout.EndHorizontal();

                string normalizedName = nameProperty.stringValue.Trim();
                if (!string.IsNullOrEmpty(normalizedName) && !usedNames.Add(normalizedName))
                {
                    EditorGUILayout.HelpBox(
                        $"'{normalizedName}' 이름이 중복되었습니다. 드롭다운 구분을 위해 고유한 이름을 사용하세요.",
                        MessageType.Warning);
                }
            }
        }

        AudioManager inspectedManager = (AudioManager)target;
        int legacyCount = CountLegacyPlayerIds(inspectedManager);
        if (legacyCount > 0 && GUILayout.Button($"Import Legacy IDs ({legacyCount})"))
            ImportLegacyPlayerIds(inspectedManager);

        EditorGUILayout.EndVertical();

        if (deleteIndex >= 0)
            DeletePlayerId(inspectedManager, deleteIndex);
    }

    private void DeletePlayerId(AudioManager manager, int index)
    {
        if (playerIdDefinitions == null ||
            index < 0 ||
            index >= playerIdDefinitions.arraySize)
        {
            return;
        }

        SerializedProperty definition = playerIdDefinitions.GetArrayElementAtIndex(index);
        string key = definition.FindPropertyRelative("key").stringValue;
        string displayName = definition.FindPropertyRelative("displayName").stringValue;
        AudioPlayerId id = new(key);
        int referenceCount = CountPlayerReferences(manager, id);

        string message = referenceCount > 0
            ? $"'{displayName}' ID를 삭제하면 현재 Scene의 FmodEventPlayer {referenceCount}개가 None으로 변경됩니다."
            : $"'{displayName}' ID를 삭제할까요?";

        if (!EditorUtility.DisplayDialog("Delete Audio Player ID", message, "Delete", "Cancel"))
            return;

        serializedObject.ApplyModifiedProperties();
        ClearPlayerReferences(manager, id);
        Undo.RecordObject(manager, "Delete Audio Player ID");
        manager.RemovePlayerId(id);
        EditorUtility.SetDirty(manager);
        serializedObject.Update();
    }

    private void ImportLegacyPlayerIds(AudioManager manager)
    {
        serializedObject.ApplyModifiedProperties();
        Undo.RecordObject(manager, "Import Legacy Audio Player IDs");

        FmodEventPlayer[] scenePlayers = GetScenePlayers(manager);
        for (int i = 0; i < scenePlayers.Length; i++)
        {
            FmodEventPlayer player = scenePlayers[i];
            if (string.IsNullOrWhiteSpace(player.LegacyPlayerId))
                continue;

            AudioPlayerId id = manager.GetOrCreatePlayerId(player.LegacyPlayerId);
            Undo.RecordObject(player, "Migrate Audio Player ID");
            player.SetPlayerId(id);
            EditorUtility.SetDirty(player);
        }

        EditorUtility.SetDirty(manager);
        serializedObject.Update();
    }

    private static int CountLegacyPlayerIds(AudioManager manager)
    {
        FmodEventPlayer[] players = GetScenePlayers(manager);
        int count = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(players[i].LegacyPlayerId))
                count++;
        }

        return count;
    }

    private static int CountPlayerReferences(AudioManager manager, AudioPlayerId id)
    {
        FmodEventPlayer[] players = GetScenePlayers(manager);
        int count = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].PlayerId == id)
                count++;
        }

        return count;
    }

    private static void ClearPlayerReferences(AudioManager manager, AudioPlayerId id)
    {
        FmodEventPlayer[] players = GetScenePlayers(manager);
        for (int i = 0; i < players.Length; i++)
        {
            FmodEventPlayer player = players[i];
            if (player.PlayerId != id)
                continue;

            Undo.RecordObject(player, "Clear Audio Player ID");
            player.SetPlayerId(default);
            EditorUtility.SetDirty(player);
        }
    }

    private static FmodEventPlayer[] GetScenePlayers(AudioManager manager)
    {
        List<FmodEventPlayer> result = new();
        FmodEventPlayer[] players = Resources.FindObjectsOfTypeAll<FmodEventPlayer>();

        for (int i = 0; i < players.Length; i++)
        {
            FmodEventPlayer player = players[i];
            if (player != null &&
                !EditorUtility.IsPersistent(player) &&
                player.gameObject.scene == manager.gameObject.scene)
            {
                result.Add(player);
            }
        }

        return result.ToArray();
    }

    private void EnsurePlayerIdTargets(bool recordUndo)
    {
        foreach (UnityEngine.Object inspectedTarget in targets)
        {
            if (inspectedTarget is not AudioManager manager)
                continue;

            if (recordUndo)
                Undo.RecordObject(manager, "Repair Audio Player IDs");

            if (manager.EnsurePlayerIdDefinitions())
                EditorUtility.SetDirty(manager);
        }
    }

    private void DrawBusPauseSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Game Pause Policy", EditorStyles.boldLabel);
        if (GUILayout.Button("Sync From Catalog", GUILayout.Width(130f)))
        {
            SynchronizeTargets(true);
            serializedObject.Update();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Bus path와 GUID는 Event Catalog에서 자동으로 가져옵니다. " +
            "여기서는 Game Pause 시 멈출 Bus만 선택합니다.",
            MessageType.Info);

        if (busCatalogs == null || busCatalogs.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "Bus 정보를 제공할 FmodEventCatalog를 연결하세요.",
                MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        if (busPauseSettings == null || busPauseSettings.arraySize == 0)
        {
            EditorGUILayout.HelpBox(
                "연결된 Catalog에 스캔된 Bus가 없습니다. Catalog의 Bank scan을 확인하세요.",
                MessageType.Warning);
            EditorGUILayout.EndVertical();
            return;
        }

        Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        Rect headerToggleRect = headerRect;
        headerToggleRect.xMin = headerToggleRect.xMax - 88f;
        Rect headerBusRect = headerRect;
        headerBusRect.xMax = headerToggleRect.xMin - 6f;
        EditorGUI.LabelField(headerBusRect, "Catalog Bus", EditorStyles.miniBoldLabel);
        EditorGUI.LabelField(headerToggleRect, "Game Pause", EditorStyles.miniBoldLabel);

        for (int i = 0; i < busPauseSettings.arraySize; i++)
        {
            SerializedProperty setting = busPauseSettings.GetArrayElementAtIndex(i);
            SerializedProperty pathProperty = setting.FindPropertyRelative("busPath");
            SerializedProperty guidProperty = setting.FindPropertyRelative("busGuid");
            SerializedProperty pauseProperty = setting.FindPropertyRelative("pauseOnGamePause");

            string path = pathProperty.stringValue;
            Rect rowRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 2f);
            Rect toggleRect = rowRect;
            toggleRect.xMin = toggleRect.xMax - 88f;
            Rect labelRect = rowRect;
            labelRect.xMax = toggleRect.xMin - 6f;
            labelRect.xMin += GetBusDepth(path) * 14f;

            GUIContent busLabel = new(
                GetBusLabel(path),
                string.IsNullOrEmpty(guidProperty.stringValue)
                    ? path
                    : path + "\n" + guidProperty.stringValue);

            EditorGUI.LabelField(labelRect, busLabel);
            pauseProperty.boolValue = EditorGUI.Toggle(toggleRect, pauseProperty.boolValue);
        }

        EditorGUILayout.EndVertical();
    }

    private void SynchronizeTargets(bool recordUndo)
    {
        foreach (UnityEngine.Object inspectedTarget in targets)
        {
            if (inspectedTarget is not AudioManager manager)
                continue;

            if (recordUndo)
                Undo.RecordObject(manager, "Sync FMOD Bus Pause Settings");

            if (manager.SynchronizeBusPauseSettings())
                EditorUtility.SetDirty(manager);
        }
    }

    private static int GetBusDepth(string path)
    {
        if (string.IsNullOrEmpty(path) ||
            string.Equals(path, "bus:/", System.StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        int depth = 1;
        for (int i = "bus:/".Length; i < path.Length; i++)
        {
            if (path[i] == '/')
                depth++;
        }

        return depth;
    }

    private static string GetBusLabel(string path)
    {
        return string.Equals(path, "bus:/", System.StringComparison.OrdinalIgnoreCase)
            ? "Master  (bus:/)"
            : path;
    }
}
#endif
