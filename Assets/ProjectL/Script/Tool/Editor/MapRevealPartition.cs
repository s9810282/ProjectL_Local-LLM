using System;
using System.Collections.Generic;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Tool.Editor
{
    /// <summary>Prefab Mode의 배경 구역 변경을 하나의 Undo 작업으로 적용합니다.</summary>
    internal static class MapRevealPartition
    {
        private const string UndoName = "맵 배경 구역 분할";

        internal static void Open(GameObject prefab)
        {
            RequireEditorReady();
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (!EditorUtility.IsPersistent(prefab) || PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.Regular)
                throw new ArgumentException("Project 창의 일반 .prefab을 지정하세요. FBX와 Variant는 지원하지 않습니다.", nameof(prefab));
            if (PrefabStageUtility.OpenPrefab(AssetDatabase.GetAssetPath(prefab)) == null)
                throw new InvalidOperationException("맵 프리팹을 Prefab Mode로 열지 못했습니다.");
        }

        internal static Transform CurrentRoot()
        {
            RequireEditorReady();
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null || stage.prefabContentsRoot == null)
                throw new InvalidOperationException("먼저 맵 프리팹을 Prefab Mode로 여세요.");
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(stage.assetPath);
            if (asset == null || PrefabUtility.GetPrefabAssetType(asset) != PrefabAssetType.Regular)
                throw new InvalidOperationException("일반 맵 프리팹만 분할할 수 있습니다. FBX와 Variant는 지원하지 않습니다.");
            Transform root = stage.prefabContentsRoot.transform.Find(MapRevealAreaLayout.RootPath);
            if (root == null)
                throw new InvalidOperationException($"맵에 배경 경로가 없습니다: {MapRevealAreaLayout.RootPath}");
            return root;
        }

        internal static void Apply(MapRevealPartitionPlan preview)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            Transform root = CurrentRoot();
            if (root != preview.Root)
                throw new InvalidOperationException("열린 프리팹이 바뀌었습니다. 다시 미리보기 하세요.");
            MapRevealPartitionPlan current = MapRevealPartitionPlan.Create(root, preview.Size, preview.ContainerNames);
            if (!preview.SameAs(current))
                throw new InvalidOperationException("미리보기 이후 배치 또는 계층이 바뀌었습니다. 다시 미리보기 하세요.");
            if (current.ItemCount == 0)
                throw new InvalidOperationException("분할할 배경 소품이 없습니다.");

            var existing = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (Transform area in current.PreviousAreas) existing.Add(area.name, area);
            var used = new HashSet<Transform>();
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            try
            {
                foreach (MapRevealPartitionPlan.Item retained in current.Retained)
                    Move(retained, root, Vector3.zero);
                foreach (MapRevealPartitionPlan.Area area in current.Areas)
                {
                    if (!existing.TryGetValue(area.Name, out Transform parent))
                    {
                        var created = new GameObject(area.Name) { layer = root.gameObject.layer };
                        SceneManager.MoveGameObjectToScene(created, root.gameObject.scene);
                        Undo.RegisterCreatedObjectUndo(created, UndoName);
                        parent = created.transform;
                        Undo.SetTransformParent(parent, root, UndoName);
                    }
                    used.Add(parent);
                    Undo.RecordObject(parent, UndoName);
                    parent.localPosition = area.Center;
                    parent.localRotation = Quaternion.identity;
                    parent.localScale = Vector3.one;
                    foreach (MapRevealPartitionPlan.Item item in area.Items) Move(item, parent, area.Center);
                }
                foreach (Transform old in current.PreviousAreas)
                {
                    if (used.Contains(old)) continue;
                    if (old.childCount != 0)
                        throw new InvalidOperationException($"사용하지 않는 이전 구역에 소품이 남아 있습니다: {old.name}");
                    Undo.DestroyObjectImmediate(old.gameObject);
                }
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
            }
            catch
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
            finally
            {
                Undo.IncrementCurrentGroup();
            }
        }

        private static void Move(MapRevealPartitionPlan.Item item, Transform parent, Vector3 center)
        {
            if (item.Body.parent != parent) Undo.SetTransformParent(item.Body, parent, UndoName);
            Undo.RecordObject(item.Body, UndoName);
            // 풀어낸 부모는 모두 항등 회전/크기이므로 원본 루트의 회전·크기를 그대로 유지합니다.
            item.Body.localPosition = item.Position - center;
            item.Body.localRotation = item.Rotation;
            item.Body.localScale = item.Scale;
            if (PrefabUtility.IsPartOfPrefabInstance(item.Body.gameObject))
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.Body);
        }

        private static void RequireEditorReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("재생/컴파일이 끝난 뒤 편집 모드에서 사용하세요.");
        }
    }
}
