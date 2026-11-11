using System;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    internal sealed partial class CombatMapAuthoringRootEditor
    {
        private void DrawGrid(CombatMapAuthoringRoot root, FbxCombatStageContext context)
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("필드 그리드", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("전투 필드 칸 경계입니다. 타일 보정을 바꿔도 그리드는 필드 원점에 고정됩니다.",
                EditorStyles.wordWrappedMiniLabel);
            CombatMapAuthoringGridPreview grid = root.GetComponent<CombatMapAuthoringGridPreview>();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (grid == null)
                {
                    if (GUILayout.Button("필드 그리드 만들기")) RefreshGrid(root, context);
                    return;
                }

                EditorGUI.BeginChangeCheck();
                bool visible = EditorGUILayout.Toggle("그리드 표시", grid.enabled);
                if (EditorGUI.EndChangeCheck()) CombatMapAuthoringGridSetup.SetVisible(grid, visible);
                if (GUILayout.Button("운영 그리드 설정 다시 읽기")) RefreshGrid(root, context);
            }
            if (grid != null && !string.IsNullOrEmpty(grid.Problem))
                EditorGUILayout.HelpBox(grid.Problem, MessageType.Warning);
        }

        private void RefreshGrid(CombatMapAuthoringRoot root, FbxCombatStageContext context)
        {
            try
            {
                CombatMapAuthoringGridSetup.Ensure(root, context);
                _problem = null;
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                _problem = exception.Message;
            }
        }
    }
}
