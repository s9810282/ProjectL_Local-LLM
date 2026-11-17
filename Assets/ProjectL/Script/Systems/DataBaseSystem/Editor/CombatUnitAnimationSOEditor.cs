#if UNITY_EDITOR
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Editor
{
    /// <summary>
    /// 클립 묶음의 인스펙터입니다. 공격 동작 시작 기준(클립 칸 다시 짜기 설계 CS11)이 WithLead일 때만 지연 칸을 보입니다.
    /// 나머지 칸(동작 칸의 단계·이벤트 목록·블렌드)은 기본 그리기입니다.
    /// </summary>
    [CustomEditor(typeof(CombatUnitAnimationSO)), CanEditMultipleObjects]
    internal sealed class CombatUnitAnimationSOEditor : UnityEditor.Editor
    {
        private const string StartField = "motionStart";
        private const string DelayField = "motionStartDelay";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == DelayField)
                    continue;
                using (new EditorGUI.DisabledScope(property.name == "m_Script"))
                    EditorGUILayout.PropertyField(property, true);
                if (property.name == StartField)
                    DrawStartDelay(property);
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStartDelay(SerializedProperty start)
        {
            if (start.hasMultipleDifferentValues || start.intValue != (int)CombatAttackMotionStart.WithLead)
                return;
            EditorGUILayout.PropertyField(serializedObject.FindProperty(DelayField), new GUIContent("시작 지연 (초)"));
            EditorGUILayout.HelpBox("이 공격의 선행 구간(공격 사건 앞의 발동·끌어오기·휩쓸기 등) 사건 중 연출이 처음 시작되는 순간부터 셉니다. "
                + "연출 없이 바로 끝나는 사건은 세지 않습니다. 맞는 순간의 지연(이벤트 줄)은 별도입니다.\n"
                + "음수면 그만큼 먼저 시작합니다: 선행 구간의 첫 사건이 닿는 순간 동작을 시작하고, 그 사건 연출(과 뒤 사건)이 그 초만큼 기다립니다. "
                + "이번 공격에 선행 구간이 없으면 앞당길 사건이 없어 공격과 함께 시작합니다.", MessageType.Info);
        }
    }
}
#endif
