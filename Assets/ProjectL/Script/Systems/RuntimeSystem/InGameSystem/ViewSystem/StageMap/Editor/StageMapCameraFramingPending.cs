#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Editor
{
    /// <summary>Play 중 확정한 구도를 편집 씬으로 전달합니다. 씬 저장은 사용자가 수행합니다.</summary>
    [InitializeOnLoad]
    internal static class StageMapCameraFramingPending
    {
        private const string SessionKey = "ProjectL.StageCamera.PendingConfirmations";
        private static readonly PendingList _pending;

        static StageMapCameraFramingPending()
        {
            _pending = JsonUtility.FromJson<PendingList>(SessionState.GetString(SessionKey, "{}")) ?? new PendingList();
            _pending.items ??= new List<Confirmation>();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.delayCall += ApplyPending;
        }

        [Serializable]
        internal sealed class Confirmation
        {
            public string key;
            public string scenePath;
            public Vector3 position;
            public Vector3 angles;
            public float size;
        }

        [Serializable]
        private sealed class PendingList
        {
            public List<Confirmation> items = new();
        }

        internal static Confirmation Find(string key)
        {
            foreach (Confirmation item in _pending.items)
            {
                if (item.key == key) return item;
            }
            return null;
        }

        internal static void Store(StageMapSceneRoot root, Vector3 position, Vector3 angles, float size)
        {
            if (string.IsNullOrEmpty(root.gameObject.scene.path))
                throw new InvalidOperationException("[StageMap] 구도 확정에는 저장된 씬이 필요합니다.");
            var item = new Confirmation
            {
                key = root.CameraFramingKey,
                scenePath = root.gameObject.scene.path,
                position = position,
                angles = angles,
                size = size,
            };
            _pending.items.RemoveAll(existing => existing.key == item.key);
            _pending.items.Add(item);
            SavePending();
        }

        internal static void Apply(CinemachineVirtualCamera camera, Vector3 position, Vector3 angles, float size)
        {
            Undo.RecordObjects(new UnityEngine.Object[] { camera.transform, camera }, "StageSelect 카메라 구도 확정");
            camera.transform.SetPositionAndRotation(position, Quaternion.Euler(angles));
            var lens = camera.m_Lens;
            lens.OrthographicSize = size;
            camera.m_Lens = lens;
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(camera);
            EditorSceneManager.MarkSceneDirty(camera.gameObject.scene);
        }

        internal static void Remove(string key)
        {
            _pending.items.RemoveAll(item => item.key == key);
            SavePending();
        }

        private static void SavePending() => SessionState.SetString(SessionKey, JsonUtility.ToJson(_pending));

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall -= ApplyPending;
                EditorApplication.delayCall += ApplyPending;
            }
        }

        private static void ApplyPending()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (Confirmation item in _pending.items.ToArray())
            {
                try
                {
                    Scene scene = SceneManager.GetSceneByPath(item.scenePath);
                    if (!scene.IsValid() || !scene.isLoaded)
                        scene = EditorSceneManager.OpenScene(item.scenePath, OpenSceneMode.Additive);
                    StageMapSceneRoot root = FindRoot(scene, item.key);
                    var settings = new StageMapCameraFramingSettings(root);
                    if (!settings.IsValid(item.position, item.angles) || !StageMapCameraFramingSettings.IsValidSize(item.size))
                        throw new InvalidOperationException($"[StageMap] 확정 구도가 편집 씬의 맵 평면에 유효하지 않습니다: {item.key}");
                    Apply(root.MapCamera, item.position, item.angles, item.size);
                    Remove(item.key);
                    Debug.Log($"[StageMap] Play 중 확정한 카메라 구도를 반영했습니다. 씬을 저장하세요: {item.scenePath}", root);
                }
                catch (Exception exception)
                {
                    // 실패한 확정값은 세션에 남겨 재확정 또는 다음 편집 모드 복귀 때 복구할 수 있게 합니다.
                    Debug.LogError($"[StageMap] 카메라 구도 반영 실패. 확정값을 유지합니다: {item.key}\n{exception}");
                }
            }
        }

        private static StageMapSceneRoot FindRoot(Scene scene, string key)
        {
            StageMapSceneRoot match = null;
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (StageMapSceneRoot root in sceneRoot.GetComponentsInChildren<StageMapSceneRoot>(true))
                {
                    if (root.MapCamera == null || root.CameraFramingKey != key) continue;
                    if (match != null)
                        throw new InvalidOperationException($"[StageMap] 카메라 구도 대상이 중복됩니다: {key}");
                    match = root;
                }
            }
            return match != null ? match : throw new InvalidOperationException($"[StageMap] 카메라 구도 대상을 찾을 수 없습니다: {key}");
        }
    }
}
#endif
