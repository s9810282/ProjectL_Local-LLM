using System;
using Cinemachine;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Tool.Editor
{
    /// <summary>저장된 Combat 씬의 공용 가상 카메라 기준값만 읽고 PreviewScene을 즉시 닫습니다.</summary>
    internal static class FbxCombatCameraSource
    {
        private const string CombatScenePath = "Assets/Scenes/Combat.unity";
        private const string BindingTypeName =
            "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras.CombatSceneCameraBinding";
        private const string SceneRootTypeName =
            "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.CombatSceneRoot";

        internal readonly struct Frame
        {
            internal Frame(string name, Vector3 position, Quaternion rotation, float size, float near, float far,
                CombatCameraPreviewSettings settings, string settingsPath)
            {
                Name = name;
                Position = position;
                Rotation = rotation;
                OrthographicSize = size;
                NearClip = near;
                FarClip = far;
                UseZoomPose = settings.UseZoomPose;
                ZoomMax = settings.ZoomMax;
                InitialSize = settings.InitialSize(size);
                Pose start = settings.StartPoseAt(position, rotation);
                StartPosition = start.position;
                StartRotation = start.rotation;
                SettingsPath = settingsPath;
            }

            internal string Name { get; }
            internal Vector3 Position { get; }
            internal Quaternion Rotation { get; }
            internal float OrthographicSize { get; }
            internal float NearClip { get; }
            internal float FarClip { get; }
            internal bool UseZoomPose { get; }
            internal float ZoomMax { get; }
            internal float InitialSize { get; }
            internal Vector3 StartPosition { get; }
            internal Quaternion StartRotation { get; }
            internal string SettingsPath { get; }

        }

        internal static Frame Read()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CombatScenePath) == null)
                throw new InvalidOperationException($"Combat 씬 에셋을 찾을 수 없습니다: {CombatScenePath}");

            Scene scene = EditorSceneManager.OpenPreviewScene(CombatScenePath);
            try
            {
                MonoBehaviour binding = null;
                MonoBehaviour sceneRoot = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (MonoBehaviour candidate in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (candidate == null)
                            continue;
                        string typeName = candidate.GetType().FullName;
                        if (typeName == BindingTypeName)
                        {
                            if (binding != null)
                                throw new InvalidOperationException("저장된 Combat 씬에 공용 카메라 Binding이 둘 이상입니다.");
                            binding = candidate;
                        }
                        else if (typeName == SceneRootTypeName)
                        {
                            if (sceneRoot != null)
                                throw new InvalidOperationException("저장된 Combat 씬에 CombatSceneRoot가 둘 이상입니다.");
                            sceneRoot = candidate;
                        }
                    }
                }
                if (binding == null)
                    throw new InvalidOperationException("저장된 Combat 씬에 공용 카메라 Binding이 없습니다.");
                if (sceneRoot == null || new SerializedObject(sceneRoot).FindProperty("sceneCamera")?.objectReferenceValue != binding)
                    throw new InvalidOperationException("CombatSceneRoot.sceneCamera가 이 공용 카메라 Binding을 가리키지 않습니다.");
                var bindingValues = new SerializedObject(binding);
                SerializedProperty property = bindingValues.FindProperty("virtualCamera");
                var camera = property?.objectReferenceValue as CinemachineVirtualCamera;
                if (camera == null || camera.gameObject.scene != scene)
                    throw new InvalidOperationException("Combat 씬 Binding의 virtualCamera 참조가 비었거나 씬 밖을 가리킵니다.");
                var settings = bindingValues.FindProperty("settings")?.objectReferenceValue as ScriptableObject;
                if (settings == null)
                    throw new InvalidOperationException("Combat 씬 Binding에 공용 카메라 설정 SO가 없습니다.");
                return ReadCamera(camera, settings);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>저장된 Combat 씬과 편집 씬이 같은 원본 렌즈·설정 읽기 계약을 사용합니다.</summary>
        internal static Frame ReadCamera(CinemachineVirtualCamera camera, ScriptableObject settings)
        {
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            LensSettings lens = camera.m_Lens;
            if (!lens.Orthographic || !(lens.OrthographicSize > 0f))
                throw new InvalidOperationException("공용 카메라는 양의 크기를 가진 직교 시점이어야 합니다.");
            CombatCameraPreviewSettings preview = CombatCameraPreviewSettings.Read(settings);
            return new Frame(camera.name, camera.transform.position, camera.transform.rotation,
                lens.OrthographicSize, lens.NearClipPlane, lens.FarClipPlane, preview, AssetDatabase.GetAssetPath(settings));
        }
    }
}
