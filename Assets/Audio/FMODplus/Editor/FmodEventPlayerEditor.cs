#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FmodEventPlayer), true)]
public class FmodEventPlayerEditor : Editor
{
    private readonly Dictionary<int, bool> slotFoldouts = new();
    private readonly Dictionary<string, bool> sectionFoldouts = new();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        FmodEventPlayer player = (FmodEventPlayer)target;
        if (player == null)
        {
            serializedObject.ApplyModifiedProperties();
            return;
        }

        DrawProperty("catalog");
        DrawProperty("loadCatalogBanksOnEnable");
        DrawProperty("unloadCatalogBanksOnDisable");
        DrawProperty("stopAllSlotsOnDisable");
        DrawProperty("stopAllSlotsOnDestroy");

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Audio Manager", EditorStyles.boldLabel);
        DrawProperty("playerKey", "Player ID");

        SerializedProperty legacyPlayerId = serializedObject.FindProperty("legacyPlayerId");
        if (legacyPlayerId != null && !string.IsNullOrWhiteSpace(legacyPlayerId.stringValue))
        {
            EditorGUILayout.HelpBox(
                $"Legacy Player ID '{legacyPlayerId.stringValue}'가 남아 있습니다. " +
                "AudioManager Inspector에서 Legacy IDs를 가져오세요.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(6f);
        DrawToolbar(player);
        EditorGUILayout.Space(6f);
        DrawSlots(player);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawToolbar(FmodEventPlayer player)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Player Tools", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Slot", GUILayout.MinHeight(22f)))
        {
            Undo.RecordObject(player, "Add FMOD Event Slot");
            player.EventSlots.Add(new FmodEventSlot { slotName = $"Event Slot {player.EventSlots.Count}" });
            EditorUtility.SetDirty(player);
        }

        if (GUILayout.Button("Sync All Slots", GUILayout.MinHeight(22f)))
        {
            Undo.RecordObject(player, "Sync All FMOD Slots");
            player.SyncAllSlotsWithSelectedEvents();
            EditorUtility.SetDirty(player);
        }

        if (player.Catalog != null && GUILayout.Button("Open Event Browser", GUILayout.MinHeight(22f)))
            FmodEventBrowserWindow.OpenWindow();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawSlots(FmodEventPlayer player)
    {
        List<FmodEventSlot> slots = player.EventSlots;
        if (slots == null || slots.Count == 0)
        {
            EditorGUILayout.HelpBox("현재 이벤트 슬롯이 비어있습니다.", MessageType.Info);
            return;
        }

        for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
        {
            FmodEventSlot slot = slots[slotIndex];
            if (slot == null)
                continue;

            if (!slotFoldouts.ContainsKey(slotIndex))
                slotFoldouts[slotIndex] = true;

            string slotName = string.IsNullOrWhiteSpace(slot.slotName) ? $"Slot {slotIndex}" : slot.slotName;
            string eventName = string.IsNullOrWhiteSpace(slot.displayName) ? "(No Event)" : slot.displayName;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            slotFoldouts[slotIndex] = EditorGUILayout.Foldout(
                slotFoldouts[slotIndex],
                $"[{slotIndex}] {slotName} · {slot.playbackMode} · {eventName}",
                true);

            if (slotFoldouts[slotIndex])
            {
                DrawSlotToolbar(player, slot, slotIndex);
                DrawSlotIdentity(player, slot);
                DrawSlotEventPicker(player, slot, slotIndex);
                DrawPlaybackSettings(player, slot, slotIndex);
                DrawParameterOverrides(player, slot, slotIndex);
                DrawParameterPresets(player, slot, slotIndex);
                DrawRuntimeButtons(player, slot, slotIndex);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }

    private void DrawSlotToolbar(FmodEventPlayer player, FmodEventSlot slot, int slotIndex)
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("▲", GUILayout.Width(28f)) && slotIndex > 0)
        {
            Undo.RecordObject(player, "Move FMOD Slot Up");
            Swap(player.EventSlots, slotIndex, slotIndex - 1);
            EditorUtility.SetDirty(player);
            return;
        }

        if (GUILayout.Button("▼", GUILayout.Width(28f)) && slotIndex < player.EventSlots.Count - 1)
        {
            Undo.RecordObject(player, "Move FMOD Slot Down");
            Swap(player.EventSlots, slotIndex, slotIndex + 1);
            EditorUtility.SetDirty(player);
            return;
        }

        if (GUILayout.Button("Duplicate", GUILayout.Width(80f)))
        {
            Undo.RecordObject(player, "Duplicate FMOD Slot");
            FmodEventSlot clone = CloneSlot(slot);
            clone.slotName = $"{slot.slotName} Copy";
            player.EventSlots.Insert(slotIndex + 1, clone);
            EditorUtility.SetDirty(player);
            return;
        }

        if (GUILayout.Button("Delete", GUILayout.Width(70f)))
        {
            Undo.RecordObject(player, "Delete FMOD Slot");
            player.EventSlots.RemoveAt(slotIndex);
            EditorUtility.SetDirty(player);
            return;
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4f);
    }

    private void DrawSlotIdentity(FmodEventPlayer player, FmodEventSlot slot)
    {
        EditorGUI.BeginChangeCheck();
        string newName = EditorGUILayout.TextField("Slot Name", slot.slotName);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(player, "Rename FMOD Slot");
            slot.slotName = newName;
            EditorUtility.SetDirty(player);
        }

        EditorGUILayout.LabelField("Event Name", string.IsNullOrWhiteSpace(slot.displayName) ? "-" : slot.displayName);
    }

    private void DrawSlotEventPicker(FmodEventPlayer player, FmodEventSlot slot, int slotIndex)
    {
        if (!FoldoutSection(slotIndex, "Event", "Event Selection"))
            return;

        FmodEventCatalog catalog = player.Catalog;
        if (catalog == null)
        {
            EditorGUILayout.HelpBox("Catalog가 연결되지 않았습니다.", MessageType.Info);
            return;
        }

        if (catalog.events == null || catalog.events.Count == 0)
        {
            EditorGUILayout.HelpBox("Catalog에 스캔된 이벤트가 없습니다.", MessageType.Warning);
        }

        FmodScannedEventEntry currentEvent = player.GetSelectedEventEntry(slotIndex);
        if (currentEvent == null)
        {
            EditorGUILayout.HelpBox("이 슬롯에 선택된 이벤트가 없습니다.", MessageType.Info);
            if (GUILayout.Button("Select Event", GUILayout.MinHeight(22f)))
                OpenEventPicker(player, slotIndex, catalog, false, slot.selectedEventPath);
            return;
        }

        EditorGUILayout.LabelField("Selected Event", string.IsNullOrWhiteSpace(currentEvent.displayName) ? currentEvent.path : currentEvent.displayName);
        if (!string.IsNullOrWhiteSpace(currentEvent.folderPath))
            EditorGUILayout.LabelField("Folder", currentEvent.folderPath);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("ReSelect", GUILayout.Width(80f)))
            OpenEventPicker(player, slotIndex, catalog, true, slot.selectedEventPath);

        if (GUILayout.Button("Clear", GUILayout.Width(60f)))
        {
            Undo.RecordObject(player, "Clear FMOD Event");
            player.ClearSelectedEvent(slotIndex);
            EditorUtility.SetDirty(player);
        }

        if (GUILayout.Button("Resync", GUILayout.Width(70f)))
        {
            Undo.RecordObject(player, "Resync FMOD Slot");
            player.SyncSlotWithSelectedEvent(slotIndex);
            EditorUtility.SetDirty(player);
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawPlaybackSettings(FmodEventPlayer player, FmodEventSlot slot, int slotIndex)
    {
        if (!FoldoutSection(slotIndex, "Playback", "Playback Settings"))
            return;

        Undo.RecordObject(player, "Edit FMOD Playback Settings");
        EditorGUI.BeginChangeCheck();

        slot.playbackMode = (FmodPlaybackMode)EditorGUILayout.EnumPopup("Playback Mode", slot.playbackMode);
        slot.playOnStart = EditorGUILayout.Toggle("Play On Start", slot.playOnStart);
        slot.attachToGameObject = EditorGUILayout.Toggle("Attach To GameObject", slot.attachToGameObject);
        slot.stopMode = (FMOD.Studio.STOP_MODE)EditorGUILayout.EnumPopup("Stop Mode", slot.stopMode);

        if (HasAutomaticParameters(slot) && !slot.attachToGameObject)
        {
            EditorGUILayout.HelpBox(
                "Automatic/Built-In parameter가 포함된 슬롯인데 Attach To GameObject가 꺼져 있습니다. 위치 기반 값 갱신이 어긋날 수 있습니다.",
                MessageType.Warning);
        }

        if (slot.playbackMode == FmodPlaybackMode.SingleInstance)
        {
            slot.restartIfAlreadyPlaying = EditorGUILayout.Toggle("Restart If Already Playing", slot.restartIfAlreadyPlaying);
        }
        else
        {
            slot.maxInstances = Mathf.Max(1, EditorGUILayout.IntField("Max Instances", slot.maxInstances));
            slot.stealOldestInstance = EditorGUILayout.Toggle("Steal Oldest", slot.stealOldestInstance);
        }

        if (EditorGUI.EndChangeCheck())
        {
            slot.Sanitize();
            EditorUtility.SetDirty(player);
        }
    }

    private void DrawParameterOverrides(FmodEventPlayer player, FmodEventSlot slot, int slotIndex)
    {
        int count = slot.parameterOverrides != null ? slot.parameterOverrides.Count : 0;
        if (!FoldoutSection(slotIndex, "Overrides", $"Parameter Overrides ({count})"))
            return;

        if (slot.parameterOverrides == null || slot.parameterOverrides.Count == 0)
        {
            EditorGUILayout.HelpBox("현재 선택 이벤트에 파라미터가 없거나 아직 동기화되지 않았습니다.", MessageType.Info);
            return;
        }

        if (HasAutomaticParameters(slot))
        {
            EditorGUILayout.HelpBox(
                "Built-In / Automatic parameter는 이 GUI에서 직접 제어하지 않고 런타임 드라이버에서 처리하는 구성이 더 안전합니다.",
                MessageType.Info);
        }

        Undo.RecordObject(player, "Edit FMOD Parameter Overrides");
        EditorGUI.BeginChangeCheck();

        for (int i = 0; i < slot.parameterOverrides.Count; i++)
        {
            FmodParameterOverride parameter = slot.parameterOverrides[i];
            if (parameter == null)
                continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(parameter.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Scope", parameter.scope.ToString());
            EditorGUILayout.LabelField("Range", $"{parameter.min} ~ {parameter.max}");
            EditorGUILayout.LabelField("Default", parameter.defaultValue.ToString());

            bool isAutomatic = GetBoolField(parameter, "isAutomatic");
            bool isReadOnly = GetBoolField(parameter, "isReadOnly");
            bool isEditable = IsEditableParameter(parameter);

            if (isAutomatic)
            {
                EditorGUILayout.HelpBox(
                    "Built-In / Automatic parameter입니다. 수동 Override를 비활성화합니다.",
                    MessageType.None);
            }
            else if (isReadOnly)
            {
                EditorGUILayout.HelpBox(
                    "Read-Only parameter입니다. 수동 Override를 비활성화합니다.",
                    MessageType.None);
            }

            using (new EditorGUI.DisabledScope(!isEditable))
            {
                parameter.useOverride = EditorGUILayout.Toggle("Use Override", parameter.useOverride);
                using (new EditorGUI.DisabledScope(!parameter.useOverride))
                    DrawParameterValueControl(parameter);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUILayout.Width(70f)))
            {
                parameter.useOverride = false;
                parameter.value = parameter.defaultValue;
            }

            using (new EditorGUI.DisabledScope(!Application.isPlaying || !isEditable))
            {
                if (GUILayout.Button("Apply To Slot", GUILayout.Width(100f)))
                    ApplyParameterNow(player, slotIndex, parameter);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(player);
    }

    private void DrawParameterPresets(FmodEventPlayer player, FmodEventSlot slot, int slotIndex)
    {
        int presetCount = slot.parameterPresets != null ? slot.parameterPresets.Count : 0;
        if (!FoldoutSection(slotIndex, "Presets", $"Parameter Presets ({presetCount})"))
            return;

        slot.parameterPresets ??= new List<FmodParameterPreset>();
        SanitizePresets(slot);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Preset", GUILayout.Width(110f)))
        {
            Undo.RecordObject(player, "Add FMOD Parameter Preset");
            slot.parameterPresets.Add(CreateDefaultPreset(slot));
            slot.Sanitize();
            EditorUtility.SetDirty(player);
            return;
        }

        if (slot.parameterPresets.Count > 0)
        {
            string[] options = BuildPresetPopupOptions(slot.parameterPresets);
            int current = Mathf.Clamp(slot.currentPresetIndex, -1, slot.parameterPresets.Count - 1);
            int displayIndex = Mathf.Clamp(current + 1, 0, slot.parameterPresets.Count);

            List<string> popup = new() { "(None)" };
            popup.AddRange(options);
            int newDisplayIndex = EditorGUILayout.Popup("Current Parameter Preset", displayIndex, popup.ToArray());
            int newCurrent = newDisplayIndex - 1;
            if (newCurrent != slot.currentPresetIndex)
            {
                Undo.RecordObject(player, "Change Current FMOD Parameter Preset");
                slot.currentPresetIndex = newCurrent;
                EditorUtility.SetDirty(player);
            }
        }
        EditorGUILayout.EndHorizontal();

        if (slot.parameterPresets.Count == 0)
        {
            EditorGUILayout.HelpBox("등록된 Parameter Preset이 없습니다.", MessageType.Info);
            return;
        }

        Undo.RecordObject(player, "Edit FMOD Parameter Preset");
        EditorGUI.BeginChangeCheck();

        for (int i = 0; i < slot.parameterPresets.Count; i++)
        {
            FmodParameterPreset preset = slot.parameterPresets[i];
            if (preset == null)
                continue;

            preset.EnsureId();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Parameter Preset {i}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Duplicate", GUILayout.Width(80f)))
            {
                slot.parameterPresets.Insert(i + 1, ClonePreset(preset));
                EditorUtility.SetDirty(player);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            if (GUILayout.Button("Delete", GUILayout.Width(70f)))
            {
                slot.parameterPresets.RemoveAt(i);
                if (slot.currentPresetIndex >= slot.parameterPresets.Count)
                    slot.currentPresetIndex = slot.parameterPresets.Count - 1;
                EditorUtility.SetDirty(player);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();

            preset.enabled = EditorGUILayout.Toggle("Enabled", preset.enabled);
            DrawPresetTarget(slot, preset);

            FmodParameterOverride target = FindParameter(slot, preset.targetParameterName);
            bool isLabeledTarget = target != null && target.kind == FmodParameterKind.Labeled;
            bool canEditParameter = target != null && IsEditableParameter(target);

            if (isLabeledTarget)
            {
                preset.applyMode = FmodParameterApplyMode.OneShotApply;
                preset.randomizeStart = false;
                preset.loop = false;

                EditorGUILayout.HelpBox(
                    "Labeled 파라미터는 Preset에서 고정 상태값으로만 적용됩니다. labelValue를 우선 저장하고 labelIndex는 캐시로 사용합니다.",
                    MessageType.None);

                using (new EditorGUI.DisabledScope(!canEditParameter))
                {
                    DrawPresetLabelField(target, preset);
                }
            }
            else
            {
                preset.applyMode = (FmodParameterApplyMode)EditorGUILayout.EnumPopup("Apply Mode", preset.applyMode);
                preset.curveMode = (FmodParameterCurveMode)EditorGUILayout.EnumPopup("Curve", preset.curveMode);
                preset.randomizeStart = EditorGUILayout.Toggle("Randomize Start", preset.randomizeStart);

                using (new EditorGUI.DisabledScope(!canEditParameter))
                {
                    if (preset.applyMode == FmodParameterApplyMode.OneShotApply)
                    {
                        preset.oneShotValue = target != null
                            ? DrawPresetValueField(target, "One Shot Value", preset.oneShotValue)
                            : EditorGUILayout.FloatField("One Shot Value", preset.oneShotValue);
                    }
                    else
                    {
                        preset.durationSeconds = Mathf.Max(0.01f, EditorGUILayout.FloatField("Duration", preset.durationSeconds));
                        preset.updateInterval = Mathf.Max(0.01f, EditorGUILayout.FloatField("Update Interval", preset.updateInterval));
                        preset.minValue = target != null
                            ? DrawPresetValueField(target, "Min Value", preset.minValue)
                            : EditorGUILayout.FloatField("Min Value", preset.minValue);
                        preset.maxValue = target != null
                            ? DrawPresetValueField(target, "Max Value", preset.maxValue)
                            : EditorGUILayout.FloatField("Max Value", preset.maxValue);
                        preset.loop = EditorGUILayout.Toggle("Loop", preset.loop);
                    }
                }
            }

            preset.onlyWhenPlaying = EditorGUILayout.Toggle("Only When Playing", preset.onlyWhenPlaying);
            preset.applyToPlayingInstances = EditorGUILayout.Toggle("Apply To Playing Instances", preset.applyToPlayingInstances);

            EditorGUILayout.EndVertical();
        }

        if (EditorGUI.EndChangeCheck())
        {
            SanitizePresets(slot);
            EditorUtility.SetDirty(player);
        }
    }

    private void DrawRuntimeButtons(FmodEventPlayer player, FmodEventSlot slot, int slotIndex)
    {
        if (!FoldoutSection(slotIndex, "Runtime", "Runtime Test"))
            return;

        using (new EditorGUI.DisabledScope(!Application.isPlaying || string.IsNullOrWhiteSpace(slot.selectedEventPath)))
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play Now", GUILayout.MinHeight(22f)))
                player.Play(slotIndex);
            if (GUILayout.Button("Stop Immediate", GUILayout.MinHeight(22f)))
                player.StopImmediate(slotIndex);
            if (GUILayout.Button("Stop Fadeout", GUILayout.MinHeight(22f)))
                player.StopAllowFadeout(slotIndex);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Restart Current Parameter Preset On Playing Instances", GUILayout.MinHeight(22f)))
                player.RestartCurrentPresetOnPlayingInstances(slotIndex);
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("재생 버튼은 Runtime에서만 동작합니다.", MessageType.None);
    }

    private void ApplyParameterNow(FmodEventPlayer player, int slotIndex, FmodParameterOverride parameter)
    {
        if (player == null || parameter == null)
            return;

        int currentPresetIndex = player.GetCurrentPresetIndex(slotIndex);
        if (currentPresetIndex >= 0)
        {
            // preset이 현재 같은 파라미터를 건드릴 수 있으니 경고만 주고 적용은 진행.
            Debug.Log($"[FmodEventPlayerEditor] Parameter '{parameter.name}' applied while Parameter Preset index {currentPresetIndex} is selected.");
        }

        // 현재 플레이어의 공개 API가 없다면 Sync만 하고, 런타임 반영은 다음 Play부터 적용된다.
        // 사용자가 런타임 즉시 반영 API를 추가했다면 이 자리에 연결.
        player.SyncSlotWithSelectedEvent(slotIndex);
    }

    private void DrawParameterValueControl(FmodParameterOverride parameter)
    {
        switch (parameter.kind)
        {
            case FmodParameterKind.Continuous:
                parameter.value = EditorGUILayout.Slider("Value", parameter.value, parameter.min, parameter.max);
                break;

            case FmodParameterKind.Discrete:
                parameter.value = EditorGUILayout.IntSlider(
                    "Value",
                    Mathf.RoundToInt(parameter.value),
                    Mathf.RoundToInt(parameter.min),
                    Mathf.RoundToInt(parameter.max));
                break;

            case FmodParameterKind.Labeled:
                string[] options = parameter.labels != null && parameter.labels.Count > 0
                    ? parameter.labels.ToArray()
                    : BuildFallbackLabels(Mathf.RoundToInt(parameter.min), Mathf.RoundToInt(parameter.max));
                int baseIndex = Mathf.RoundToInt(parameter.min);
                int currentIndex = Mathf.Clamp(
                    Mathf.RoundToInt(parameter.value) - baseIndex,
                    0,
                    Mathf.Max(0, options.Length - 1));
                currentIndex = EditorGUILayout.Popup("Value", currentIndex, options);
                parameter.value = baseIndex + currentIndex;
                break;
        }
    }

    private float DrawPresetValueField(FmodParameterOverride parameter, string label, float currentValue)
    {
        switch (parameter.kind)
        {
            case FmodParameterKind.Continuous:
                return EditorGUILayout.Slider(label, currentValue, parameter.min, parameter.max);
            case FmodParameterKind.Discrete:
                return EditorGUILayout.IntSlider(label, Mathf.RoundToInt(currentValue), Mathf.RoundToInt(parameter.min), Mathf.RoundToInt(parameter.max));
            default:
                return currentValue;
        }
    }

    private void DrawPresetLabelField(FmodParameterOverride parameter, FmodParameterPreset preset)
    {
        if (parameter == null || preset == null)
            return;

        string[] options = parameter.labels != null && parameter.labels.Count > 0
            ? parameter.labels.ToArray()
            : BuildFallbackLabels(Mathf.RoundToInt(parameter.min), Mathf.RoundToInt(parameter.max));

        int baseIndex = Mathf.RoundToInt(parameter.min);
        int resolvedIndex = FmodEventPlayer.ResolvePresetLabelIndex(parameter, preset);
        int currentOptionIndex = Mathf.Clamp(
            (resolvedIndex >= 0 ? resolvedIndex : Mathf.RoundToInt(parameter.defaultValue)) - baseIndex,
            0,
            Mathf.Max(0, options.Length - 1));

        int selectedOptionIndex = EditorGUILayout.Popup("Label", currentOptionIndex, options);
        int selectedLabelIndex = baseIndex + selectedOptionIndex;

        preset.labelIndex = selectedLabelIndex;
        preset.labelValue = options.Length > 0 ? options[selectedOptionIndex] : string.Empty;
        preset.oneShotValue = selectedLabelIndex;

        EditorGUILayout.LabelField("Resolved Index", selectedLabelIndex.ToString());
    }

    private void DrawPresetTarget(FmodEventSlot slot, FmodParameterPreset preset)
    {
        List<FmodParameterOverride> candidates = GetPresetCandidates(slot);
        if (candidates.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Parameter Preset에 사용할 수 있는 파라미터가 없습니다. Built-In / ReadOnly 파라미터는 제외됩니다.",
                MessageType.Warning);
            preset.targetParameterName = string.Empty;
            return;
        }

        string[] names = candidates.ConvertAll(p => p.name).ToArray();
        int currentIndex = Mathf.Max(0, Array.IndexOf(names, preset.targetParameterName));
        int newIndex = EditorGUILayout.Popup("Target Parameter", currentIndex, names);
        preset.targetParameterName = names[newIndex];

        FmodParameterOverride target = candidates[newIndex];
        EditorGUILayout.LabelField("Kind", target.kind.ToString());
        EditorGUILayout.LabelField("Range", $"{target.min} ~ {target.max}");
        EditorGUILayout.LabelField("Default", target.defaultValue.ToString());

        if (target.kind == FmodParameterKind.Labeled)
        {
            int labelCount = target.labels != null ? target.labels.Count : 0;
            EditorGUILayout.LabelField("Labels", labelCount > 0 ? string.Join(", ", target.labels) : "(No labels scanned)");
        }
    }

    private void OpenEventPicker(FmodEventPlayer player, int slotIndex, FmodEventCatalog catalog, bool reselection, string currentPath)
    {
        FmodEventBrowserWindow.OpenPicker(
            catalog,
            selected =>
            {
                if (selected == null)
                    return;

                Undo.RecordObject(player, reselection ? "ReSelect FMOD Event" : "Select FMOD Event");
                player.SetSelectedEvent(slotIndex, selected);
                EditorUtility.SetDirty(player);
            },
            reselection ? $"ReSelect FMOD Event - Slot {slotIndex}" : $"Select FMOD Event - Slot {slotIndex}",
            currentPath);
    }

    private static bool HasAutomaticParameters(FmodEventSlot slot)
    {
        if (slot?.parameterOverrides == null)
            return false;

        for (int i = 0; i < slot.parameterOverrides.Count; i++)
        {
            if (GetBoolField(slot.parameterOverrides[i], "isAutomatic"))
                return true;
        }

        return false;
    }

    private static bool IsEditableParameter(FmodParameterOverride parameter)
    {
        if (parameter == null)
            return false;

        return !GetBoolField(parameter, "isAutomatic") && !GetBoolField(parameter, "isReadOnly");
    }

    private static bool GetBoolField(object target, string fieldName)
    {
        if (target == null)
            return false;

        var field = target.GetType().GetField(fieldName);
        if (field == null || field.FieldType != typeof(bool))
            return false;

        return (bool)field.GetValue(target);
    }

    private static List<FmodParameterOverride> GetPresetCandidates(FmodEventSlot slot)
    {
        List<FmodParameterOverride> result = new();
        if (slot?.parameterOverrides == null)
            return result;

        for (int i = 0; i < slot.parameterOverrides.Count; i++)
        {
            FmodParameterOverride parameter = slot.parameterOverrides[i];
            if (parameter == null)
                continue;
            if (!IsEditableParameter(parameter))
                continue;
            result.Add(parameter);
        }

        return result;
    }

    private static string ResolveDefaultPresetLabel(FmodParameterOverride parameter)
    {
        if (parameter == null || parameter.labels == null || parameter.labels.Count == 0)
            return string.Empty;

        int listIndex = Mathf.RoundToInt(parameter.defaultValue) - Mathf.RoundToInt(parameter.min);
        return listIndex >= 0 && listIndex < parameter.labels.Count
            ? parameter.labels[listIndex]
            : string.Empty;
    }

    private static FmodParameterPreset CreateDefaultPreset(FmodEventSlot slot)
    {
        List<FmodParameterOverride> candidates = GetPresetCandidates(slot);
        FmodParameterOverride parameter = candidates.Count > 0 ? candidates[0] : null;

        FmodParameterPreset preset = new FmodParameterPreset
        {
            enabled = false,
            applyMode = FmodParameterApplyMode.OneShotApply,
            durationSeconds = 1f,
            updateInterval = 0.05f,
            loop = true,
            onlyWhenPlaying = true,
            applyToPlayingInstances = true,
            randomizeStart = false,
            curveMode = FmodParameterCurveMode.Linear,
            targetParameterName = parameter != null ? parameter.name : string.Empty,
            oneShotValue = parameter != null ? parameter.defaultValue : 0f,
            minValue = parameter != null ? parameter.min : 0f,
            maxValue = parameter != null ? parameter.max : 1f,
            labelIndex = parameter != null ? Mathf.RoundToInt(parameter.defaultValue) : -1,
            labelValue = ResolveDefaultPresetLabel(parameter)
        };

        preset.EnsureId();
        if (parameter != null)
            preset.SanitizeFor(parameter);

        return preset;
    }

    private static FmodParameterPreset ClonePreset(FmodParameterPreset source)
    {
        FmodParameterPreset clone = new FmodParameterPreset
        {
            targetParameterName = source.targetParameterName,
            enabled = source.enabled,
            applyMode = source.applyMode,
            durationSeconds = source.durationSeconds,
            updateInterval = source.updateInterval,
            loop = source.loop,
            onlyWhenPlaying = source.onlyWhenPlaying,
            applyToPlayingInstances = source.applyToPlayingInstances,
            randomizeStart = source.randomizeStart,
            curveMode = source.curveMode,
            oneShotValue = source.oneShotValue,
            minValue = source.minValue,
            maxValue = source.maxValue,
            labelValue = source.labelValue,
            labelIndex = source.labelIndex
        };
        clone.EnsureId();
        return clone;
    }

    private static void SanitizePresets(FmodEventSlot slot)
    {
        if (slot?.parameterPresets == null)
            return;

        for (int i = 0; i < slot.parameterPresets.Count; i++)
        {
            FmodParameterPreset preset = slot.parameterPresets[i];
            if (preset == null)
                continue;

            preset.EnsureId();
            FmodParameterOverride parameter = FindParameter(slot, preset.targetParameterName);
            if (parameter != null)
                preset.SanitizeFor(parameter);
        }
    }

    private static FmodParameterOverride FindParameter(FmodEventSlot slot, string parameterName)
    {
        if (slot?.parameterOverrides == null || string.IsNullOrWhiteSpace(parameterName))
            return null;

        return slot.parameterOverrides.Find(p => p != null && p.name == parameterName);
    }

    private string[] BuildPresetPopupOptions(List<FmodParameterPreset> presets)
    {
        List<string> options = new();
        for (int i = 0; i < presets.Count; i++)
        {
            FmodParameterPreset preset = presets[i];
            string targetName = preset == null || string.IsNullOrWhiteSpace(preset.targetParameterName)
                ? "(No Target)"
                : preset.targetParameterName;
            options.Add($"Parameter Preset {i} · {targetName}");
        }
        return options.ToArray();
    }

    private bool FoldoutSection(int slotIndex, string key, string label)
    {
        string fullKey = $"{slotIndex}:{key}";
        if (!sectionFoldouts.ContainsKey(fullKey))
            sectionFoldouts[fullKey] = true;

        sectionFoldouts[fullKey] = EditorGUILayout.Foldout(sectionFoldouts[fullKey], label, true);
        return sectionFoldouts[fullKey];
    }

    private void DrawProperty(string propertyName, string displayName = null)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            if (string.IsNullOrEmpty(displayName))
                EditorGUILayout.PropertyField(property);
            else
                EditorGUILayout.PropertyField(property, new GUIContent(displayName));
        }
    }

    private static void Swap<T>(List<T> list, int a, int b)
    {
        (list[a], list[b]) = (list[b], list[a]);
    }

    private static FmodEventSlot CloneSlot(FmodEventSlot original)
    {
        FmodEventSlot clone = new FmodEventSlot
        {
            slotName = original.slotName,
            displayName = original.displayName,
            selectedEventPath = original.selectedEventPath,
            selectedEventGuid = original.selectedEventGuid,
            playbackMode = original.playbackMode,
            playOnStart = original.playOnStart,
            attachToGameObject = original.attachToGameObject,
            stopMode = original.stopMode,
            restartIfAlreadyPlaying = original.restartIfAlreadyPlaying,
            maxInstances = original.maxInstances,
            stealOldestInstance = original.stealOldestInstance,
            currentPresetIndex = original.currentPresetIndex,
            parameterOverrides = new List<FmodParameterOverride>(),
            parameterAutomations = new List<FmodParameterAutomation>(),
            parameterPresets = new List<FmodParameterPreset>()
        };

        foreach (FmodParameterOverride parameter in original.parameterOverrides)
        {
            FmodParameterOverride copied = new FmodParameterOverride
            {
                name = parameter.name,
                idString = parameter.idString,
                kind = parameter.kind,
                scope = parameter.scope,
                min = parameter.min,
                max = parameter.max,
                defaultValue = parameter.defaultValue,
                useOverride = parameter.useOverride,
                value = parameter.value,
                labels = parameter.labels != null ? new List<string>(parameter.labels) : new List<string>()
            };

            CopyOptionalBoolField(parameter, copied, "isReadOnly");
            CopyOptionalBoolField(parameter, copied, "isAutomatic");
            clone.parameterOverrides.Add(copied);
        }

        foreach (FmodParameterPreset preset in original.parameterPresets)
            clone.parameterPresets.Add(ClonePreset(preset));

        return clone;
    }

    private static void CopyOptionalBoolField(object src, object dst, string fieldName)
    {
        var srcField = src.GetType().GetField(fieldName);
        var dstField = dst.GetType().GetField(fieldName);
        if (srcField == null || dstField == null)
            return;
        if (srcField.FieldType != typeof(bool) || dstField.FieldType != typeof(bool))
            return;

        dstField.SetValue(dst, srcField.GetValue(src));
    }

    private static string[] BuildFallbackLabels(int min, int max)
    {
        List<string> labels = new();
        for (int i = min; i <= max; i++)
            labels.Add($"Label {i}");
        return labels.ToArray();
    }
}
#endif
