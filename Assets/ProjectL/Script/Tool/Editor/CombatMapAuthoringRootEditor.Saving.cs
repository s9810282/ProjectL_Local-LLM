using System;
using System.Collections.Generic;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    internal sealed partial class CombatMapAuthoringRootEditor
    {
        private void DrawSaving(CombatMapAuthoringRoot root, GameObject prefab, CombatStageSoView view,
            FbxCombatStageContext context)
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("씬 배경 보정", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("값을 바꾸면 이 씬 프리뷰에 적용됩니다. SO 저장 버튼을 누르면 운영 ViewStage에 기록됩니다.",
                EditorStyles.wordWrappedMiniLabel);
            bool tiles = context.Mode == "PrefabWithTiles";
            Vector3 expectedScale;
            try
            {
                expectedScale = tiles ? CombatMapScaleEditor.Resolve(prefab, context.FieldView, view.tiles)
                    : Vector3.Scale(prefab.transform.localScale, root.DraftScaleOffset);
            }
            catch (Exception exception)
            {
                EditorGUILayout.HelpBox(exception.Message, MessageType.Error);
                return;
            }
            EditorGUI.BeginChangeCheck();
            Vector3 position = root.DraftPositionOffset;
            if (tiles)
                position.y = EditorGUILayout.FloatField("배경 높이 Y", position.y);
            else
                position = EditorGUILayout.Vector3Field("위치", position);
            Vector3 rotation = EditorGUILayout.Vector3Field("회전", root.DraftRotationOffset);
            Vector3 scale = root.DraftScaleOffset;
            if (tiles) CombatMapScaleEditor.Draw(prefab, context.FieldView, view.tiles);
            else scale = EditorGUILayout.Vector3Field("배율", scale);
            Vector3 tileOffset = root.DraftTileOffset;
            Vector2Int anchor = root.DraftAnchorTile;
            if (tiles)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField("타일 오프셋 X (읽기 전용)", tileOffset.x);
                tileOffset.y = EditorGUILayout.FloatField("타일 오프셋 Y", tileOffset.y);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.FloatField("타일 오프셋 Z (읽기 전용)", tileOffset.z);
                anchor = EditorGUILayout.Vector2IntField("기준 타일", anchor);
            }
            if (EditorGUI.EndChangeCheck())
                EditDraft(root, prefab, view, context, position, rotation, scale, tileOffset, anchor);

            if (tiles)
            {
                Vector3 world = root.PreviewMap.transform.position;
                EditorGUI.BeginChangeCheck();
                float worldX = EditorGUILayout.FloatField("배경 월드 X", CombatMapAuthoringPlacement.NormalizeNearZero(world.x));
                float worldZ = EditorGUILayout.FloatField("배경 월드 Z", CombatMapAuthoringPlacement.NormalizeNearZero(world.z));
                if (EditorGUI.EndChangeCheck())
                {
                    Vector3 adjusted = CombatMapAuthoringPlacement.TileOffsetForBackdropPosition(
                        root.PreviewMap.transform, root.DraftTileOffset, new Vector2(worldX, worldZ));
                    EditDraft(root, prefab, view, context, root.DraftPositionOffset, root.DraftRotationOffset,
                        root.DraftScaleOffset, adjusted, root.DraftAnchorTile);
                }
                EditorGUILayout.LabelField("배경 X/Z 입력은 타일 오프셋에 저장됩니다. 타일과 필드 그리드는 고정됩니다.",
                    EditorStyles.wordWrappedMiniLabel);
                DrawTileSpacing(root, view, context);
            }

            bool unsaved = root.DraftPositionOffset != view.positionOffset
                || root.DraftRotationOffset != view.rotationOffset || (!tiles && root.DraftScaleOffset != view.scaleOffset)
                || (tiles && (root.DraftTileOffset != view.tiles.offset || root.DraftAnchorTile != view.tiles.anchorTile));
            if (unsaved)
                EditorGUILayout.HelpBox("현재 보정 작업 값이 운영 ViewStage SO와 다릅니다.", MessageType.Info);
            bool rootMoved = root.PreviewMap.transform.localPosition != root.DraftPositionOffset
                || root.PreviewMap.transform.localRotation != Quaternion.Euler(root.DraftRotationOffset)
                || root.PreviewMap.transform.localScale != expectedScale;
            if (rootMoved)
                EditorGUILayout.HelpBox("맵 루트 Transform이 작업 값과 다릅니다. 보정은 위 입력 칸에서 조정하고 '작업 값 다시 적용'으로 맞추세요.", MessageType.Warning);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("작업 값 다시 적용"))
                    EditDraft(root, prefab, view, context, root.DraftPositionOffset, root.DraftRotationOffset,
                        root.DraftScaleOffset, root.DraftTileOffset, root.DraftAnchorTile);
                if (GUILayout.Button("보정 SO 저장"))
                {
                    try
                    {
                        CombatMapAuthoringSave.SaveOffsets(root, prefab, context.Mode);
                        _contextDirty = true;
                        _problem = null;
                        Debug.Log($"[CombatMapAuthoring] Stage {context.StageKey} 보정 SO를 저장했습니다.", root);
                    }
                    catch (Exception exception) when (!(exception is ExitGUIException)) { _problem = exception.Message; }
                }
                if (GUILayout.Button("프랍 프리팹 저장"))
                {
                    try
                    {
                        int changes = CombatMapAuthoringSave.SavePrefab(root, prefab, view, context);
                        _contextDirty = true;
                        _problem = null;
                        Debug.Log($"[CombatMapAuthoring] '{prefab.name}' 프리팹 변경 {changes}건을 적용했습니다.", root);
                        EditorApplication.QueuePlayerLoopUpdate();
                    }
                    catch (Exception exception) when (!(exception is ExitGUIException)) { _problem = exception.Message; }
                }
            }
        }

        private static void DrawTileSpacing(CombatMapAuthoringRoot root, CombatStageSoView view,
            FbxCombatStageContext context)
        {
            Transform tileRoot = CombatMapAuthoringPlacement.FindTileRoot(root.PreviewContainer.transform);
            if (tileRoot == null)
            {
                EditorGUILayout.HelpBox("표시 타일 루트가 없습니다. 스테이지 배경을 다시 불러오세요.", MessageType.Warning);
                return;
            }

            Vector2 tile = CombatMapAuthoringPlacement.TileSpacing(tileRoot, view.tiles);
            Vector2 field = new Vector2(context.FieldView.CellWidth, context.FieldView.CellLength);
            EditorGUILayout.LabelField("실제 타일 간격 X/Z", $"{tile.x:0.###} × {tile.y:0.###}");
            EditorGUILayout.LabelField("운영 필드 간격 X/Z", $"{field.x:0.###} × {field.y:0.###}");
            if (Mathf.Abs(tile.x - field.x) > 0.001f || Mathf.Abs(tile.y - field.y) > 0.001f)
                EditorGUILayout.HelpBox("타일 간격과 운영 필드 그리드 간격이 다릅니다. 제작 규격을 확인하고 '작업 값 다시 적용'으로 자동 배율을 적용하세요. 씬의 작업 값은 자동 저장하지 않습니다.",
                    MessageType.Warning);
        }

        private void EditDraft(CombatMapAuthoringRoot root, GameObject prefab, CombatStageSoView view,
            FbxCombatStageContext context, Vector3 position, Vector3 rotation, Vector3 scale,
            Vector3 tileOffset, Vector2Int anchor)
        {
            Transform container = root.PreviewContainer.transform;
            Transform map = root.PreviewMap.transform;
            Transform tileRoot = CombatMapAuthoringPlacement.FindTileRoot(container);
            Vector3 oldPosition = root.DraftPositionOffset, oldRotation = root.DraftRotationOffset;
            Vector3 oldScale = root.DraftScaleOffset, oldTileOffset = root.DraftTileOffset;
            Vector2Int oldAnchor = root.DraftAnchorTile;
            Vector3 containerPosition = container.localPosition, containerScale = container.localScale;
            Quaternion containerRotation = container.localRotation;
            Vector3 mapPosition = map.localPosition, mapScale = map.localScale;
            Quaternion mapRotation = map.localRotation;
            Vector3 tilePosition = tileRoot != null ? tileRoot.localPosition : Vector3.zero;
            Vector3 tileScale = tileRoot != null ? tileRoot.localScale : Vector3.one;
            Quaternion tileRotation = tileRoot != null ? tileRoot.localRotation : Quaternion.identity;
            var objects = new List<Object> { root, container, map };
            if (tileRoot != null) objects.Add(tileRoot);
            Undo.RecordObjects(objects.ToArray(), "전투 배경 보정 작업 값");
            try
            {
                root.SetDraftOffsets(position, rotation, scale, tileOffset, anchor);
                CombatMapAuthoringPlacement.Reposition(container, map, root.transform, prefab, view, context,
                    position, rotation, scale, tileOffset, anchor);
                _unitDirty = true;
                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
                EditorApplication.QueuePlayerLoopUpdate();
                _problem = null;
            }
            catch (Exception exception) when (!(exception is ExitGUIException))
            {
                root.SetDraftOffsets(oldPosition, oldRotation, oldScale, oldTileOffset, oldAnchor);
                container.localPosition = containerPosition;
                container.localRotation = containerRotation;
                container.localScale = containerScale;
                map.localPosition = mapPosition;
                map.localRotation = mapRotation;
                map.localScale = mapScale;
                if (tileRoot != null)
                {
                    tileRoot.localPosition = tilePosition;
                    tileRoot.localRotation = tileRotation;
                    tileRoot.localScale = tileScale;
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(map);
                _problem = exception.Message;
            }
        }
    }
}
