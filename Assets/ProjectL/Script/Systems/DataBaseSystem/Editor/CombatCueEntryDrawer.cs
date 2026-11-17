#if UNITY_EDITOR
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Editor
{
    /// <summary>
    /// 연출 큐 항목(이펙트·흔들림·플래시)과 유닛 외형의 소리 줄을 목록 안에서 구분되게 그립니다. 목록 항목이면 위에 여백과 옅은 구분선을 두고,
    /// 칸은 기본 인스펙터와 같은 순서·툴팁으로 그립니다. 목록이 아닌 한 칸(피격 플래시 등)은 기본과 같습니다.
    /// 값·검사는 바꾸지 않습니다(그리기만).
    /// </summary>
    [CustomPropertyDrawer(typeof(CombatEffectCueEntry))]
    [CustomPropertyDrawer(typeof(CombatShakeCueEntry))]
    [CustomPropertyDrawer(typeof(CombatFlashCueEntry))]
    [CustomPropertyDrawer(typeof(CombatUnitSoundCueEntry))]
    internal sealed class CombatCueEntryDrawer : PropertyDrawer
    {
        // 목록 항목 위 여백(구분선 포함)입니다.
        private const float ElementGap = 10f;
        private static readonly Color SeparatorColor = new(0.5f, 0.5f, 0.5f, 0.45f);

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
                foreach (SerializedProperty child in Children(property))
                    height += EditorGUIUtility.standardVerticalSpacing + EditorGUI.GetPropertyHeight(child, true);
            return IsListElement(property) ? height + ElementGap : height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            if (IsListElement(property))
            {
                // 구분선은 목록 칸 가로 전체에 긋고, 항목은 여백 아래부터 그립니다.
                EditorGUI.DrawRect(new Rect(position.x, position.y + ElementGap * 0.5f - 0.5f, position.width, 1f), SeparatorColor);
                position.yMin += ElementGap;
            }

            Rect row = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (SerializedProperty child in Children(property))
                {
                    row.y += row.height + EditorGUIUtility.standardVerticalSpacing;
                    row.height = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(row, child, true);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        // 배열 항목의 경로는 "...Array.data[n]"으로 끝납니다.
        private static bool IsListElement(SerializedProperty property) => property.propertyPath.EndsWith("]");

        // 바로 아래 칸들을 직렬화 순서대로 돕니다(기본 인스펙터와 같은 순서, HideInInspector는 빠짐).
        private static IEnumerable<SerializedProperty> Children(SerializedProperty property)
        {
            SerializedProperty child = property.Copy();
            SerializedProperty end = property.GetEndProperty();
            bool enter = true;
            while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
            {
                enter = false;
                yield return child.Copy();
            }
        }
    }
}
#endif
