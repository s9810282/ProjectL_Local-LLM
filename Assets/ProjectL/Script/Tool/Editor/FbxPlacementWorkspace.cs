using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Tool.Editor
{
    /// <summary>가상 씬의 수명과 에셋 배치, 맵 프리팹 입출력을 담당합니다.</summary>
    public static class FbxPlacementWorkspace
    {
        public static GameObject CreateRoot()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = EditorUtility.CreateGameObjectWithHideFlags("Map", HideFlags.HideAndDontSave);
                SceneManager.MoveGameObjectToScene(root, scene);
                root.hideFlags = HideFlags.None;
                return root;
            }
            catch
            {
                EditorSceneManager.ClosePreviewScene(scene);
                throw;
            }
        }

        public static bool IsWorkspaceRoot(GameObject root)
        {
            return root != null && !EditorUtility.IsPersistent(root) && root.transform.parent == null &&
                root.scene.IsValid() && root.scene.isLoaded && EditorSceneManager.IsPreviewScene(root.scene) &&
                PrefabStageUtility.GetPrefabStage(root) == null;
        }

        public static GameObject AddFbx(GameObject root, GameObject model, Vector3 position, Transform layer = null)
        {
            return AddAsset(root, FbxPlacement.GetModelAsset(model), position, layer);
        }

        public static GameObject AddAsset(GameObject root, GameObject asset, Vector3 position, Transform layer = null)
        {
            ValidateRoot(root);
            Transform parent = FbxPlacementLayers.Resolve(root, layer);
            Vector3 localPosition = parent.InverseTransformPoint(root.transform.TransformPoint(position));
            return FbxPlacement.PlaceInPreview(asset, parent, localPosition);
        }

        public static GameObject AddFbxOnSurface(GameObject root, GameObject model, Vector3 point,
            Vector3 normal, bool alignToNormal, float offset, Transform layer = null)
        {
            return AddAssetOnSurface(root, FbxPlacement.GetModelAsset(model), point, normal, alignToNormal, offset, layer);
        }

        public static GameObject AddAssetOnSurface(GameObject root, GameObject asset, Vector3 point,
            Vector3 normal, bool alignToNormal, float offset, Transform layer = null)
        {
            ValidateRoot(root);
            FbxPlacementLayers.Resolve(root, layer);
            FbxPlacementSurface.Validate(point, normal, offset);
            const string undoName = "맵 에셋 표면 배치";
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            try
            {
                GameObject instance = AddAsset(root, asset, root.transform.InverseTransformPoint(point), layer);
                Undo.RecordObject(instance.transform, undoName);
                FbxPlacementSurface.Seat(instance.transform, point, normal, alignToNormal, offset);
                Undo.FlushUndoRecordObjects();
                Undo.CollapseUndoOperations(group);
                Undo.IncrementCurrentGroup();
                return instance;
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        public static GameObject OpenPrefab(GameObject prefab)
        {
            if (prefab == null || !EditorUtility.IsPersistent(prefab) ||
                !string.Equals(Path.GetExtension(AssetDatabase.GetAssetPath(prefab)), ".prefab", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Project 창의 맵 프리팹을 지정하세요.", nameof(prefab));

            GameObject root = CreateRoot();
            Scene scene = root.scene;
            try
            {
                GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                PrefabUtility.UnpackPrefabInstance(copy, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                UnityEngine.Object.DestroyImmediate(root);
                copy.GetComponent<ProjectL.Tool.Runtime.MapBakedLighting>()?.Apply();
                return copy;
            }
            catch
            {
                EditorSceneManager.ClosePreviewScene(scene);
                throw;
            }
        }

        public static GameObject SavePrefab(GameObject root, string path)
        {
            ValidateRoot(root);
            string normalized = (path ?? "").Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) || normalized.Contains("/../") ||
                normalized.Contains("/./") || !normalized.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ||
                !AssetDatabase.IsValidFolder(Path.GetDirectoryName(normalized)?.Replace('\\', '/')))
                throw new ArgumentException("Assets 폴더 안의 .prefab 경로를 지정하세요.", nameof(path));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, normalized, out bool success);
            if (!success || prefab == null)
                throw new InvalidOperationException("맵 프리팹을 저장하지 못했습니다: " + normalized);
            return prefab;
        }

        public static void Close(GameObject root)
        {
            if (!IsWorkspaceRoot(root)) return;
            if (Selection.activeGameObject != null && Selection.activeGameObject.scene == root.scene)
                Selection.activeObject = null;
            EditorSceneManager.ClosePreviewScene(root.scene);
        }

        private static void ValidateRoot(GameObject root)
        {
            if (!IsWorkspaceRoot(root))
                throw new ArgumentException("배치 툴이 소유하는 가상 씬의 맵 루트가 필요합니다.", nameof(root));
        }
    }
}
