#if UNITY_EDITOR
using System;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Editor
{
    /// <summary>기존 관리 참조 필드에서 특성 입력 타입을 선택하고 직렬화된 수치를 편집합니다.</summary>
    [CustomPropertyDrawer(typeof(UnitTraitAuthoring), true)]
    internal sealed class UnitTraitAuthoringDrawer : TraitReferenceDrawer<UnitTraitAuthoring> { }

    [CustomPropertyDrawer(typeof(TraitRankAuthoring), true)]
    internal sealed class TraitRankAuthoringDrawer : TraitReferenceDrawer<TraitRankAuthoring> { }

    internal abstract class TraitReferenceDrawer<T> : PropertyDrawer
    {
        private static readonly Type[] _types = TypeCache.GetTypesDerivedFrom<T>()
            .Where(t => !t.IsAbstract && t.IsSerializable).OrderBy(t => t.Name).ToArray();
        private static readonly string[] _names = new[] { "None" }.Concat(_types.Select(t => t.Name)).ToArray();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(new Rect(row.x, row.y, EditorGUIUtility.labelWidth, row.height), property.isExpanded, label, true);
            Type current = property.managedReferenceValue?.GetType();
            int selected = Array.IndexOf(_types, current) + 1;
            EditorGUI.BeginChangeCheck();
            int next = EditorGUI.Popup(new Rect(row.x + EditorGUIUtility.labelWidth, row.y,
                row.width - EditorGUIUtility.labelWidth, row.height), selected, _names);
            if (EditorGUI.EndChangeCheck())
            {
                property.managedReferenceValue = next == 0 ? null : Activator.CreateInstance(_types[next - 1], true);
                property.isExpanded = true;
            }
            if (property.isExpanded)
            {
                var child = property.Copy();
                var end = property.GetEndProperty();
                bool enter = true;
                while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
                {
                    enter = false;
                    row.y += row.height + EditorGUIUtility.standardVerticalSpacing;
                    row.height = EditorGUI.GetPropertyHeight(child, true);
                    EditorGUI.PropertyField(row, child, true);
                }
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded) return height;
            var child = property.Copy();
            var end = property.GetEndProperty();
            bool enter = true;
            while (child.NextVisible(enter) && !SerializedProperty.EqualContents(child, end))
            {
                enter = false;
                height += EditorGUI.GetPropertyHeight(child, true) + EditorGUIUtility.standardVerticalSpacing;
            }
            return height;
        }
    }
}
#endif
