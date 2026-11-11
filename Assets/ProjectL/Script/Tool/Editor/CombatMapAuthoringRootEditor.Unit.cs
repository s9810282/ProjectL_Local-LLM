using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    internal sealed partial class CombatMapAuthoringRootEditor
    {
        private Dictionary<int, CombatStageSoVisual> _unitVisuals;
        private int[] _unitKeys;
        private string[] _unitLabels;
        private string _unitProblem;
        private bool _unitDirty = true;
        private bool _unitHasTile;

        private void DrawUnit(CombatMapAuthoringRoot root, CombatStageSoView view, FbxCombatStageContext context)
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("유닛 표면 높이 프리뷰", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("타일의 SurfaceHeight에 선 유닛 한 개를 표시합니다. 지형의 추가 높이는 포함하지 않습니다.",
                EditorStyles.wordWrappedMiniLabel);
            if (_unitVisuals == null) ReadUnitVisuals();
            if (_unitKeys.Length == 0)
            {
                CombatMapAuthoringUnitPreview.Hide(root);
                EditorGUILayout.HelpBox("프리팹이 등록된 유닛 외형이 없습니다.", MessageType.Warning);
                return;
            }

            if (root.PreviewUnitKey == 0)
            {
                int preferred = (int)EntityKey.Unit_MainCharacter;
                int key = _unitVisuals.ContainsKey(preferred) && _unitVisuals[preferred].Prefab != null ? preferred : _unitKeys[0];
                Vector2Int center = new Vector2Int(context.Field.Columns / 2, context.Field.Rows / 2);
                Undo.RecordObject(root, "유닛 프리뷰 기본 선택");
                root.SetUnitPreviewSelection(key, center);
                _unitDirty = true;
            }

            int index = Array.IndexOf(_unitKeys, root.PreviewUnitKey);
            if (index < 0)
            {
                Undo.RecordObject(root, "유닛 프리뷰 외형 갱신");
                root.SetUnitPreviewSelection(_unitKeys[0], root.PreviewUnitCell);
                index = 0;
                _unitDirty = true;
            }
            EditorGUI.BeginChangeCheck();
            int nextIndex = EditorGUILayout.Popup("유닛 외형", index, _unitLabels);
            Vector2Int nextCell = EditorGUILayout.Vector2IntField("필드 칸 (X/Z)", root.PreviewUnitCell);
            if (EditorGUI.EndChangeCheck())
            {
                nextCell.x = Mathf.Clamp(nextCell.x, 0, context.Field.Columns - 1);
                nextCell.y = Mathf.Clamp(nextCell.y, 0, context.Field.Rows - 1);
                Undo.RecordObject(root, "유닛 프리뷰 선택");
                root.SetUnitPreviewSelection(_unitKeys[nextIndex], nextCell);
                _unitDirty = true;
            }
            if (_unitDirty || root.PreviewUnit == null) RefreshUnit(root, view, context);
            if (!string.IsNullOrEmpty(_unitProblem))
                EditorGUILayout.HelpBox(_unitProblem, MessageType.Warning);
            else
            {
                EditorGUILayout.LabelField("타일 SurfaceHeight", $"{root.PreviewSurfaceHeight:0.###}");
                if (context.Mode == CombatStageSoBackground.PrefabWithTiles && !_unitHasTile)
                    EditorGUILayout.HelpBox("선택한 필드 칸은 타일 맵 밖이라 운영과 같이 표면 높이 0을 사용합니다.", MessageType.Info);
            }
        }

        private void ReadUnitVisuals()
        {
            _unitVisuals = CombatStageSoAssets.UnitVisuals();
            Dictionary<int, string> names = CombatStageSoAssets.UnitVisualNames();
            _unitKeys = _unitVisuals.Where(pair => pair.Value?.Prefab != null)
                .Select(pair => pair.Key).OrderBy(key => key).ToArray();
            _unitLabels = _unitKeys.Select(key => $"{(names.TryGetValue(key, out string name) ? name : "유닛")} ({key})").ToArray();
        }

        private void RefreshUnit(CombatMapAuthoringRoot root, CombatStageSoView view, FbxCombatStageContext context)
        {
            _unitDirty = false;
            _unitProblem = null;
            try
            {
                (_, _unitHasTile) = CombatMapAuthoringUnitPreview.Refresh(root, view, context, _unitVisuals);
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                CombatMapAuthoringUnitPreview.Hide(root);
                _unitProblem = exception.Message;
            }
        }
    }
}
