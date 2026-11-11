using System;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>편집기가 읽은 운영 필드 값과 프리팹을 공통 배율 계산으로 전달합니다.</summary>
    internal static class CombatMapScaleEditor
    {
        internal static Vector3 Resolve(GameObject prefab, CombatStageSoFieldView field, CombatStageSoTileLayout tiles)
        {
            if (prefab == null) throw new InvalidOperationException("자동 배율을 계산할 배경 프리팹이 없습니다.");
            if (field == null || field.Approximate)
                throw new InvalidOperationException("자동 배율에는 운영 GridOutline 필드 설정이 필요합니다. 근사 간격을 사용하지 않습니다.");
            CombatMapAuthoringMetrics.RequireTileSize(tiles.tileSize, prefab.name);
            return CombatMapAuthoringMetrics.Scale(field.CellWidth, field.CellLength, prefab.transform.localScale, prefab.name);
        }

        internal static void Draw(GameObject prefab, CombatStageSoFieldView field, CombatStageSoTileLayout tiles)
        {
            try
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Vector3Field("자동 배율", Resolve(prefab, field, tiles));
                EditorGUILayout.LabelField("계산 기준", $"필드 간격 {field.CellWidth:0.###} / 제작 규격 {CombatMapAuthoringMetrics.TileLength}");
            }
            catch (Exception exception) { EditorGUILayout.HelpBox(exception.Message, MessageType.Error); }
        }
    }

    [CustomPropertyDrawer(typeof(CombatMapScaleAttribute))]
    internal sealed class CombatMapScaleDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string path = property.propertyPath;
            int separator = path.LastIndexOf('.');
            string prefix = separator >= 0 ? path.Substring(0, separator + 1) : "";
            SerializedProperty mode = property.serializedObject.FindProperty(prefix + "mode");
            if (mode != null && mode.hasMultipleDifferentValues)
            {
                EditorGUI.LabelField(position, "배율", "배경 방식이 같은 에셋끼리 선택하세요.");
                return;
            }
            bool tiles = mode != null && !mode.hasMultipleDifferentValues && mode.enumValueIndex >= 0
                && mode.enumValueIndex < mode.enumNames.Length && mode.enumNames[mode.enumValueIndex] == "PrefabWithTiles";
            if (tiles)
                EditorGUI.LabelField(position, "자동 배율", $"OutlineCellSpacing / {CombatMapAuthoringMetrics.TileLength} (공통 XYZ)");
            else
                EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);
    }
}
