#if UNITY_EDITOR
using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.DataBaseSystem.Editor
{
    /// <summary>기존 테이블 목록을 역할별 연결 칸으로 편집합니다. 참조 원본은 목록 하나입니다.</summary>
    [CustomEditor(typeof(LobbyVisualDataTableSO))]
    internal sealed class LobbyVisualDataTableSOEditor : UnityEditor.Editor
    {
        private static readonly Type[] _roleTypes =
        {
            typeof(LobbyModelDataSO), typeof(LobbyBoardDataSO),
            typeof(LobbyMotionDataSO), typeof(LobbyCharacterPresentationDataSO)
        };

        private static readonly string[] _roleLabels =
        {
            "모델", "배치판·정착 칸", "이동 연출", "캐릭터 표시 보정"
        };

        private SerializedProperty _items;

        private void OnEnable()
        {
            _items = serializedObject.FindProperty("databaseItems");
        }

        public override void OnInspectorGUI()
        {
            // CanEditMultipleObjects를 선언하지 않으며, 직접 호출되는 경우에도 일괄 변경을 막습니다.
            if (targets.Length != 1 || serializedObject.isEditingMultipleObjects)
            {
                EditorGUILayout.HelpBox("로비 표시 테이블은 하나씩 선택하여 편집하세요.", MessageType.Info);
                return;
            }

            serializedObject.Update();
            if (_items == null || !_items.isArray)
                throw new InvalidOperationException("[LobbyVisualDataTableSOEditor] databaseItems 목록이 필요합니다.");

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            }

            if (!TryReadRoles(out Object[] roles, out string error))
            {
                EditorGUILayout.HelpBox(error + " 원본 연결 목록에서 수정하세요.", MessageType.Error);
                // 중복 중 첫 항목을 유효한 연결처럼 보여주지 않습니다.
                Array.Clear(roles, 0, roles.Length);
                using (new EditorGUI.DisabledScope(true))
                {
                    DrawRoles(roles);
                }
                EditorGUILayout.PropertyField(_items, new GUIContent("원본 연결 목록 (오류 수정)"), true);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (DrawRoles(roles))
            {
                WriteRoles(roles);
                serializedObject.ApplyModifiedProperties();
            }

            string missing = string.Empty;
            for (int i = 0; i < roles.Length; i++)
            {
                if (roles[i] == null)
                    missing += (missing.Length == 0 ? string.Empty : ", ") + _roleLabels[i];
            }
            if (missing.Length > 0)
                EditorGUILayout.HelpBox($"로비에 필요한 SO를 연결하세요: {missing}", MessageType.Warning);
        }

        private bool TryReadRoles(out Object[] roles, out string error)
        {
            roles = new Object[_roleTypes.Length];
            error = null;
            for (int i = 0; i < _items.arraySize; i++)
            {
                Object item = _items.GetArrayElementAtIndex(i).objectReferenceValue;
                if (item == null)
                {
                    error = $"{i + 1}번째 연결이 비어 있거나 Missing입니다.";
                    return false;
                }

                int role = Array.IndexOf(_roleTypes, item.GetType());
                if (role < 0)
                {
                    error = $"{i + 1}번째 연결은 지원하지 않는 SO입니다: {item.GetType().Name}";
                    return false;
                }
                if (roles[role] != null)
                {
                    error = $"{_roleLabels[role]} SO가 중복 연결되어 있습니다.";
                    return false;
                }
                roles[role] = item;
            }
            return true;
        }

        private static bool DrawRoles(Object[] roles)
        {
            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < roles.Length; i++)
                roles[i] = EditorGUILayout.ObjectField(_roleLabels[i], roles[i], _roleTypes[i], false);
            return EditorGUI.EndChangeCheck();
        }

        private void WriteRoles(Object[] roles)
        {
            int count = 0;
            for (int i = 0; i < roles.Length; i++)
            {
                if (roles[i] != null) count++;
            }

            // 사용자 변경 때만 순서를 맞춥니다. 빈 역할은 null 행 대신 목록에서 제외합니다.
            _items.arraySize = count;
            int index = 0;
            for (int i = 0; i < roles.Length; i++)
            {
                if (roles[i] != null)
                    _items.GetArrayElementAtIndex(index++).objectReferenceValue = roles[i];
            }
        }
    }
}
#endif
