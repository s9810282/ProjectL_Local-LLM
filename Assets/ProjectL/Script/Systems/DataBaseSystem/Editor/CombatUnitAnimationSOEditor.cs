#if UNITY_EDITOR
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Editor
{
    /// <summary>공격 클립의 시작 모드에 필요한 입력만 표시합니다. Kind 목록은 런타임 지원 분류를 공유합니다.</summary>
    [CustomEditor(typeof(CombatUnitAnimationSO)), CanEditMultipleObjects]
    internal sealed class CombatUnitAnimationSOEditor : UnityEditor.Editor
    {
        private GUIContent[] _kindLabels;
        private int[] _kindValues;

        private void OnEnable()
        {
            var choices = CombatPreAttackTraitKinds.Choices;
            _kindLabels = new GUIContent[choices.Count];
            _kindValues = new int[choices.Count];
            for (int i = 0; i < choices.Count; i++)
            {
                _kindLabels[i] = new GUIContent(choices[i].ToString());
                _kindValues[i] = (int)choices[i];
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == "attackAnimationStartKind" || property.name == "attackAnimationStartDelaySeconds")
                    continue;
                using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                    EditorGUILayout.PropertyField(property, true);
                if (property.name == "attackAnimationStartMode")
                    DrawStartSettings(property);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStartSettings(SerializedProperty mode)
        {
            if (mode.hasMultipleDifferentValues || mode.intValue != (int)CombatAttackAnimationStartMode.FromSelectedTraitPresentationStart)
                return;
            SerializedProperty kind = serializedObject.FindProperty("attackAnimationStartKind");
            Rect rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(rect, new GUIContent("기준 Kind"), kind);
            EditorGUI.showMixedValue = kind.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            int value = EditorGUI.IntPopup(rect, new GUIContent("기준 Kind"), kind.intValue, _kindLabels, _kindValues);
            if (EditorGUI.EndChangeCheck())
                kind.intValue = value;
            EditorGUI.showMixedValue = false;
            EditorGUI.EndProperty();
            if (!kind.hasMultipleDifferentValues && !CombatPreAttackTraitKinds.SupportsKind(
                (ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitActivationKind)kind.intValue))
                EditorGUILayout.HelpBox("지원하지 않는 Kind입니다. 목록에서 다시 선택하세요.", MessageType.Error);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attackAnimationStartDelaySeconds"), new GUIContent("시작 지연 (초)"));
            EditorGUILayout.HelpBox("지정 Kind 연출의 실제 실행 시작 기준입니다. 타격 시점(Hit Delay)은 별도입니다.", MessageType.Info);
        }
    }
}
#endif
