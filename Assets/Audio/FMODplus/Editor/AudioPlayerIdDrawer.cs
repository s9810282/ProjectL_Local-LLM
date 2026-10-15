#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AudioPlayerId))]
public sealed class AudioPlayerIdDrawer : PropertyDrawer
{
    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        SerializedProperty keyProperty = property.FindPropertyRelative("key");
        if (keyProperty == null)
        {
            EditorGUI.LabelField(position, label.text, "Invalid AudioPlayerId property");
            return;
        }

        AudioManager manager = FindManager(property.serializedObject.targetObject);
        List<GUIContent> options = new() { new GUIContent("None") };
        List<string> keys = new() { string.Empty };

        if (manager != null)
        {
            IReadOnlyList<AudioPlayerIdDefinition> definitions = manager.PlayerIdDefinitions;
            for (int i = 0; i < definitions.Count; i++)
            {
                AudioPlayerIdDefinition definition = definitions[i];
                if (definition == null || !definition.Id.IsValid)
                    continue;

                string displayName = string.IsNullOrWhiteSpace(definition.DisplayName)
                    ? "Unnamed Player ID"
                    : definition.DisplayName;
                options.Add(new GUIContent(displayName));
                keys.Add(definition.Id.Key);
            }
        }

        string currentKey = keyProperty.stringValue;
        int currentIndex = keys.IndexOf(currentKey);
        if (currentIndex < 0)
        {
            string missingLabel = manager == null
                ? "Missing AudioManager"
                : "Missing Player ID";
            options.Add(new GUIContent(missingLabel));
            keys.Add(currentKey);
            currentIndex = keys.Count - 1;
        }

        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
        EditorGUI.BeginChangeCheck();
        int selectedIndex = EditorGUI.Popup(
            position,
            label,
            currentIndex,
            options.ToArray());

        if (EditorGUI.EndChangeCheck())
            keyProperty.stringValue = keys[selectedIndex];

        EditorGUI.showMixedValue = false;
        EditorGUI.EndProperty();
    }

    private static AudioManager FindManager(Object inspectedObject)
    {
        if (AudioManager.Instance != null)
            return AudioManager.Instance;

        Component inspectedComponent = inspectedObject as Component;
        AudioManager fallback = null;
        AudioManager[] managers = Resources.FindObjectsOfTypeAll<AudioManager>();

        for (int i = 0; i < managers.Length; i++)
        {
            AudioManager manager = managers[i];
            if (manager == null ||
                EditorUtility.IsPersistent(manager) ||
                !manager.gameObject.scene.IsValid())
            {
                continue;
            }

            if (inspectedComponent != null &&
                manager.gameObject.scene == inspectedComponent.gameObject.scene)
            {
                return manager;
            }

            fallback ??= manager;
        }

        return fallback;
    }
}
#endif
