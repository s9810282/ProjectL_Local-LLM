using System;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>운영 Field View 설정을 편집용 그리드 컴포넌트에 연결합니다.</summary>
    internal static class CombatMapAuthoringGridSetup
    {
        internal static CombatMapAuthoringGridPreview Ensure(CombatMapAuthoringRoot root, FbxCombatStageContext context)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (context == null || !context.IsReady || context.FieldView.Approximate)
                throw new InvalidOperationException("편집용 필드 그리드에는 운영 GridOutline 칸 간격이 필요합니다.");
            var source = CombatStageSoAssets.ReadRegisteredGridDecal();
            if (source.LineWidth >= Mathf.Min(context.FieldView.CellWidth, context.FieldView.CellLength))
                throw new InvalidOperationException("격자 선 폭이 필드 칸 간격보다 작아야 합니다.");

            CombatMapAuthoringGridPreview grid = root.GetComponent<CombatMapAuthoringGridPreview>();
            if (grid == null) grid = Undo.AddComponent<CombatMapAuthoringGridPreview>(root.gameObject);
            Undo.RecordObject(grid, "전투 편집 격자 설정");
            grid.Configure(source.Material, context.Field.Columns, context.Field.Rows,
                context.FieldView.CellWidth, context.FieldView.CellLength, source.LineWidth, source.Depth,
                source.RenderingLayers | 1u); // 현재 타일 프리팹의 Default 렌더링 레이어도 편집 화면에서 받습니다.
            if (!string.IsNullOrEmpty(grid.Problem))
                throw new InvalidOperationException(grid.Problem);
            EditorUtility.SetDirty(grid);
            EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
            return grid;
        }

        internal static void SetVisible(CombatMapAuthoringGridPreview grid, bool visible)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));
            Undo.RecordObject(grid, "전투 편집 격자 표시");
            grid.enabled = visible;
            EditorUtility.SetDirty(grid);
            EditorSceneManager.MarkSceneDirty(grid.gameObject.scene);
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
    }
}
