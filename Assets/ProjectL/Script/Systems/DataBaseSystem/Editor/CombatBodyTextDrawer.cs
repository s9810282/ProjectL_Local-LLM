#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Editor
{
    /// <summary>비활성 텍스트와 기본 이름의 불필요한 입력을 숨깁니다.</summary>
    [CustomPropertyDrawer(typeof(CombatBodyTextInput))]
    internal sealed class CombatBodyTextDrawer : PropertyDrawer
    {
        private static IEnumerable<SerializedProperty> Fields(SerializedProperty property)
        {
            yield return property.FindPropertyRelative("enabled");
            if (!property.FindPropertyRelative("enabled").boolValue)
                yield break;
            foreach (string name in new[]
            {
                "textSource",
                "overrideText",
                "target",
                "count",
                "color",
                "fontSize",
                "positionOffset",
                "prefabOverride",
                "holdSeconds",
                "fadeSeconds"
            }

            )
            {
                if (name == "overrideText" && property.FindPropertyRelative("textSource").enumValueIndex == 0)
                    continue;
                yield return property.FindPropertyRelative(name);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
                foreach (var field in Fields(property))
                    height += EditorGUI.GetPropertyHeight(field, true) + 2;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect row = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (var field in Fields(property))
                {
                    row.y += row.height + 2;
                    row.height = EditorGUI.GetPropertyHeight(field, true);
                    if (field.name == "count")
                        CombatFeedbackEditorSupport.DrawCount(row, field, CombatFeedbackEditorSupport.Profile(property));
                    else if (field.name == "prefabOverride")
                    {
                        EditorGUI.BeginChangeCheck();
                        var selected = EditorGUI.ObjectField(row, new GUIContent("Prefab Override", "비우면 공통 Body Text Prefab 사용. 폰트·배경·장식은 프리팹/Variant에서 편집합니다."), field.objectReferenceValue, typeof(GameObject), false);
                        if (EditorGUI.EndChangeCheck())
                            field.objectReferenceValue = selected;
                    }
                    else if (field.name == "overrideText")
                        EditorGUI.PropertyField(row, field, new GUIContent("Text", "Text Source가 Override일 때 사용할 직접 문구입니다."), true);
                    else if (field.name == "positionOffset")
                        EditorGUI.PropertyField(row, field, new GUIContent("Position Offset", "몸 중앙 기준 Canvas 좌표 보정. +X 오른쪽 / +Y 위쪽."), true);
                    else
                        EditorGUI.PropertyField(row, field, true);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }

    /// <summary>시점 정책의 지원 범위를 줄 바로 아래에서 안내합니다.</summary>
    [CustomPropertyDrawer(typeof(TraitActivationVisual))]
    internal sealed class TraitActivationVisualDrawer : PropertyDrawer
    {
        private static IEnumerable<SerializedProperty> Fields(SerializedProperty property)
        {
            foreach (string field in new[]
            {
                "kind",
                "direction",
                "cueTiming",
                "bodyText",
                "cues",
                "attackPresentation",
                "unitActions",
                "positionSwap"
            }

            )
                yield return property.FindPropertyRelative(field);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
            {
                foreach (var field in Fields(property))
                    height += EditorGUI.GetPropertyHeight(field, true) + 2;
                height += 60;
            }

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect row = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (var field in Fields(property))
                {
                    row.y += row.height + 2;
                    row.height = EditorGUI.GetPropertyHeight(field, true);
                    if (field.name == "cueTiming")
                        CombatFeedbackEditorSupport.DrawTiming(row, field, CombatFeedbackEditorSupport.Profile(property));
                    else
                        EditorGUI.PropertyField(row, field, true);
                }

                row.y += row.height + 2;
                row.height = 56;
                bool state = property.FindPropertyRelative("cueTiming").intValue == (int)CombatActivationCueTiming.StateApplied;
                bool supported = !state || CombatFeedbackEditorSupport.Profile(property).SupportsStateApplied;
                EditorGUI.HelpBox(row, !supported ? "이 줄은 StateApplied를 지원하지 않습니다. ActivationStart로 수정하세요." : state ? "HUD 상태 갱신 직후 시작합니다. 같은 묶음의 Activation → State가 필요합니다. 행동/공격 덮어쓰기는 비우세요." : "발동 순간 시작합니다(희생은 나가기 시작). Anchor=Start. 문구·색·크기·위치는 Body Text, 폰트·배경·장식은 프리팹에서 편집합니다.", supported ? MessageType.Info : MessageType.Error);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }
    }

    internal static class CombatFeedbackEditorSupport
    {
        internal static CombatTraitFeedbackProfile Profile(SerializedProperty property)
        {
            string path = property.propertyPath;
            if (path.EndsWith(".bodyText", StringComparison.Ordinal))
                path = path.Substring(0, path.Length - ".bodyText".Length);
            var key = (TraitKey)(property.serializedObject.FindProperty("traitVisualData.key")?.intValue ?? 0);
            var kind = (TraitActivationKind)(property.serializedObject.FindProperty(path + ".kind")?.intValue ?? 0);
            return CombatTraitFeedbackProfiles.Get(key, kind);
        }

        internal static void DrawCount(Rect rect, SerializedProperty field, CombatTraitFeedbackProfile profile)
        {
            int[] values = profile.Count == CombatBodyTextCount.None ? new[]
            {
                (int)CombatBodyTextCount.None
            }

            : new[]
            {
                (int)CombatBodyTextCount.None,
                (int)profile.Count
            };
            DrawChoice(rect, field, values, "Count");
        }

        internal static void DrawTiming(Rect rect, SerializedProperty field, CombatTraitFeedbackProfile profile) => DrawChoice(rect, field, profile.SupportsStateApplied ? new[] { (int)CombatActivationCueTiming.ActivationStart, (int)CombatActivationCueTiming.StateApplied } : new[] { (int)CombatActivationCueTiming.ActivationStart }, "Cue Timing");
        private static void DrawChoice(Rect rect, SerializedProperty field, int[] values, string label)
        {
            string[] names = new string[values.Length];
            for (int i = 0; i < values.Length; i++)
                names[i] = field.enumNames[values[i]];
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.Popup(rect, label, Array.IndexOf(values, field.intValue), names);
            if (EditorGUI.EndChangeCheck() && next >= 0)
                field.intValue = values[next];
        }
    }
}
#endif
