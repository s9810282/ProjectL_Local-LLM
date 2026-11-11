using System;
using System.Collections.Generic;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tool.Editor
{
    /// <summary>편집 씬의 유닛 한 개를 운영 칸 크기와 타일 바탕 높이에 맞춰 세웁니다.</summary>
    internal static class CombatMapAuthoringUnitPreview
    {
        internal static (float Height, bool HasTile) Refresh(CombatMapAuthoringRoot root, CombatStageSoView view,
            FbxCombatStageContext context, IReadOnlyDictionary<int, CombatStageSoVisual> visuals)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (view == null) throw new ArgumentNullException(nameof(view));
            if (context == null || !context.IsReady) throw new InvalidOperationException("전투 필드 맥락이 없습니다.");
            if (visuals == null || !visuals.TryGetValue(root.PreviewUnitKey, out CombatStageSoVisual visual)
                || visual.Prefab == null)
                throw new InvalidOperationException("선택한 유닛의 등록된 외형 프리팹이 없습니다.");

            Vector2Int cell = root.PreviewUnitCell;
            if (cell.x < 0 || cell.y < 0 || cell.x >= context.Field.Columns || cell.y >= context.Field.Rows)
                throw new InvalidOperationException($"유닛 프리뷰 칸 ({cell.x}, {cell.y})이 필드 밖입니다.");

            float height = 0f;
            bool hasTile = false;
            if (context.Mode == CombatStageSoBackground.PrefabWithTiles)
            {
                var layout = new CombatStageSoTileLayout
                {
                    tileSetGuid = view.tiles.tileSetGuid,
                    columns = view.tiles.columns,
                    rows = view.tiles.rows,
                    tileSize = view.tiles.tileSize,
                    anchorTile = root.DraftAnchorTile,
                    kinds = view.tiles.kinds,
                    rotations = view.tiles.rotations,
                };
                var fit = CombatStageSoTileFit.Of(layout, !context.FieldView.Approximate,
                    root.DraftRotationOffset, root.PreviewMap.transform.localScale,
                    context.Field.Columns, context.Field.Rows,
                    context.FieldView.CellWidth, context.FieldView.CellLength);
                if (!fit.IsValid)
                    throw new InvalidOperationException("타일 맞춤이 성립하지 않습니다: " + string.Join(" ", fit.Problems));
                var kinds = new Dictionary<int, CombatStageSoTileKind>();
                foreach (CombatStageSoTileKind kind in CombatStageSoAssets.ReadTileSet(layout.tileSetGuid))
                    if (!kinds.TryAdd(kind.Value, kind))
                        throw new InvalidOperationException($"타일 세트 종류 {kind.Value}가 중복됩니다.");
                if (kinds.Count == 0) throw new InvalidOperationException("타일 세트의 종류가 없습니다.");
                var tiles = new CombatStageSoTileScene(fit, layout, kinds, context.StageKey);
                hasTile = fit.TryTileOfCell(cell, out int column, out int row);
                if (hasTile && !kinds.ContainsKey(layout.KindAt(column, row)))
                    throw new InvalidOperationException($"선택한 칸의 타일 종류 {layout.KindAt(column, row)}가 타일 세트에 없습니다.");
                height = tiles.BaseHeightAt(cell);
            }

            GameObject unit = root.PreviewUnit;
            if (unit == null || PrefabUtility.GetCorrespondingObjectFromSource(unit) != visual.Prefab)
            {
                GameObject replacement = PrefabUtility.InstantiatePrefab(visual.Prefab, root.gameObject.scene) as GameObject;
                if (replacement == null) throw new InvalidOperationException("유닛 프리뷰 프리팹을 생성하지 못했습니다.");
                Undo.RegisterCreatedObjectUndo(replacement, "표면 높이 유닛 프리뷰 만들기");
                Undo.SetTransformParent(replacement.transform, root.transform, "표면 높이 유닛 프리뷰 배치");
                replacement.name = "SurfaceUnitPreview";
                replacement.tag = "EditorOnly";
                foreach (MonoBehaviour behaviour in replacement.GetComponentsInChildren<MonoBehaviour>(true))
                    behaviour.enabled = false;
                foreach (Animator animator in replacement.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                foreach (Camera camera in replacement.GetComponentsInChildren<Camera>(true)) camera.enabled = false;
                foreach (AudioSource source in replacement.GetComponentsInChildren<AudioSource>(true)) source.enabled = false;
                foreach (AudioListener listener in replacement.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
                foreach (Canvas canvas in replacement.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
                if (unit != null) Undo.DestroyObjectImmediate(unit);
                Undo.RecordObject(root, "표면 높이 유닛 프리뷰 참조");
                root.SetUnitPreview(replacement, height);
                unit = replacement;
            }

            var space = new CombatStageSoSpace(context.Field.Columns, context.Field.Rows, context.FieldView);
            Vector3 position = space.CellCenter(cell) + Vector3.up * height;
            Vector3 scale = visual.Prefab.transform.localScale * context.FieldView.UnitScale;
            Quaternion rotation = visual.Prefab.transform.localRotation;
            if (unit.transform.localPosition != position || unit.transform.localRotation != rotation
                || unit.transform.localScale != scale || !unit.activeSelf || root.PreviewSurfaceHeight != height)
            {
                Undo.RecordObjects(new Object[] { root, unit, unit.transform }, "표면 높이 유닛 프리뷰 갱신");
                unit.transform.localPosition = position;
                unit.transform.localRotation = rotation;
                unit.transform.localScale = scale;
                unit.SetActive(true);
                root.SetUnitPreview(unit, height);
                EditorUtility.SetDirty(root);
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
            return (height, hasTile);
        }

        internal static void Hide(CombatMapAuthoringRoot root)
        {
            if (root?.PreviewUnit == null || !root.PreviewUnit.activeSelf) return;
            Undo.RecordObject(root.PreviewUnit, "표면 높이 유닛 프리뷰 숨기기");
            root.PreviewUnit.SetActive(false);
        }
    }
}
