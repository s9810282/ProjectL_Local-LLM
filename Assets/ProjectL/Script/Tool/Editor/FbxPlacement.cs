using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Tool.Editor
{
    /// <summary>원본 FBX·프리팹을 수정하지 않고 배치한 뒤 중첩 프리팹 연결까지 해제합니다.</summary>
    public static class FbxPlacement
    {
        public static GameObject GetModelAsset(GameObject candidate)
        {
            GameObject model = GetPlacementAsset(candidate);
            if (PrefabUtility.GetPrefabAssetType(model) != PrefabAssetType.Model)
                throw new ArgumentException("Project 창의 FBX 모델 에셋을 선택하세요.", nameof(candidate));
            return model;
        }

        /// <summary>카탈로그와 배치 입력이 함께 사용하는 지원 파일 형식입니다.</summary>
        public static bool IsSupportedAssetPath(string path)
        {
            string extension = Path.GetExtension(path);
            return string.Equals(extension, ".fbx", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Project 에셋만 받으며 내부 자식을 선택해도 배치할 에셋 루트를 반환합니다.</summary>
        public static GameObject GetPlacementAsset(GameObject candidate)
        {
            if (candidate == null)
                throw new ArgumentNullException(nameof(candidate));

            string path = AssetDatabase.GetAssetPath(candidate.transform.root.gameObject);
            if (!EditorUtility.IsPersistent(candidate) || !IsSupportedAssetPath(path))
                throw new ArgumentException("Project 창의 FBX 또는 프리팹 에셋을 선택하세요.", nameof(candidate));

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            PrefabAssetType type = model != null ? PrefabUtility.GetPrefabAssetType(model) : PrefabAssetType.NotAPrefab;
            bool valid = string.Equals(Path.GetExtension(path), ".fbx", StringComparison.OrdinalIgnoreCase)
                ? type == PrefabAssetType.Model
                : type == PrefabAssetType.Regular || type == PrefabAssetType.Variant;
            if (!valid)
                throw new ArgumentException("배치할 FBX·프리팹의 루트를 찾을 수 없습니다: " + path, nameof(candidate));
            return model;
        }

        public static Scene GetDestinationScene(Transform parent)
        {
            if (parent != null && (EditorUtility.IsPersistent(parent) ||
                PrefabStageUtility.GetPrefabStage(parent.gameObject) != null))
                throw new ArgumentException("배치 부모는 일반 씬의 오브젝트여야 합니다.", nameof(parent));

            Scene scene = parent != null ? parent.gameObject.scene : SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene) ||
                (PrefabStageUtility.GetCurrentPrefabStage() != null &&
                 PrefabStageUtility.GetCurrentPrefabStage().scene == scene))
                throw new InvalidOperationException("에셋을 배치할 일반 씬을 열어 주세요.");
            return scene;
        }

        /// <summary>위치는 부모 기준이며 회전과 크기는 에셋 원래 Transform에 추가로 적용합니다.</summary>
        public static GameObject Place(GameObject candidate, Transform parent, Vector3 position,
            Vector3 rotationOffset, Vector3 scaleMultiplier)
        {
            return PlaceAsset(candidate, parent, GetDestinationScene(parent), position, rotationOffset, scaleMultiplier);
        }

        /// <summary>배치 툴이 소유하는 프리뷰 씬의 부모 아래에만 생성합니다.</summary>
        internal static GameObject PlaceInPreview(GameObject candidate, Transform parent, Vector3 position)
        {
            if (parent == null || !EditorSceneManager.IsPreviewScene(parent.gameObject.scene))
                throw new ArgumentException("가상 씬의 배치 부모가 필요합니다.", nameof(parent));
            return PlaceAsset(candidate, parent, parent.gameObject.scene, position, Vector3.zero, Vector3.one);
        }

        private static GameObject PlaceAsset(GameObject candidate, Transform parent, Scene scene, Vector3 position,
            Vector3 rotationOffset, Vector3 scaleMultiplier)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("재생 또는 컴파일 중에는 배치할 수 없습니다.");

            GameObject model = GetPlacementAsset(candidate);
            if (!IsFinite(position) || !IsFinite(rotationOffset) || !IsFinite(scaleMultiplier) ||
                scaleMultiplier.x == 0 || scaleMultiplier.y == 0 || scaleMultiplier.z == 0)
                throw new ArgumentException("위치·회전은 유한한 수여야 하며 크기 배율에는 0을 사용할 수 없습니다.");

            const string undoName = "맵 에셋 배치 및 Unpack";
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                if (instance == null)
                    throw new InvalidOperationException("배치 인스턴스를 생성하지 못했습니다.");

                Undo.RegisterCreatedObjectUndo(instance, undoName);
                if (parent != null)
                    Undo.SetTransformParent(instance.transform, parent, undoName);

                Undo.RecordObject(instance.transform, undoName);
                instance.transform.localPosition = position;
                instance.transform.localRotation = Quaternion.Euler(rotationOffset) * model.transform.localRotation;
                instance.transform.localScale = Vector3.Scale(model.transform.localScale, scaleMultiplier);

                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.UserAction);
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

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
