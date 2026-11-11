using System;
using Cinemachine;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>편집 씬의 공용 카메라 바인딩을 찾아 설정 SO 기반 시작 상태 확장만 연결합니다.</summary>
    internal static class CombatMapAuthoringCameraSetup
    {
        private const string BindingTypeName =
            "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Cameras.CombatSceneCameraBinding";

        internal static void Ensure(CombatMapAuthoringRoot root)
        {
            Find(root, out CinemachineVirtualCamera camera, out ScriptableObject settings);
            FbxCombatCameraSource.ReadCamera(camera, settings);
            CombatMapAuthoringCameraPose pose = camera.GetComponent<CombatMapAuthoringCameraPose>();
            if (pose == null) pose = Undo.AddComponent<CombatMapAuthoringCameraPose>(camera.gameObject);
            if (pose.Settings != settings)
            {
                Undo.RecordObject(pose, "전투 카메라 시작 구도");
                pose.Configure(settings);
                EditorUtility.SetDirty(pose);
            }
            else pose.Refresh();
            if (!string.IsNullOrEmpty(pose.Problem))
                throw new InvalidOperationException(pose.Problem);
        }

        internal static CombatMapAuthoringCameraPose FindPose(CombatMapAuthoringRoot root)
        {
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects())
            {
                if (sceneRoot == root.gameObject) continue;
                foreach (CombatMapAuthoringCameraPose pose in sceneRoot.GetComponentsInChildren<CombatMapAuthoringCameraPose>(true))
                    return pose;
            }
            return null;
        }

        private static void Find(CombatMapAuthoringRoot root, out CinemachineVirtualCamera camera,
            out ScriptableObject settings)
        {
            MonoBehaviour binding = null;
            int mainCameras = 0;
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects())
            {
                if (sceneRoot == root.gameObject) continue; // 배경 프리팹의 옛 카메라는 표시 대상 밖입니다.
                foreach (Camera candidate in sceneRoot.GetComponentsInChildren<Camera>(true))
                    if (candidate.gameObject.CompareTag("MainCamera")) mainCameras++;
                foreach (MonoBehaviour candidate in sceneRoot.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (candidate == null || candidate.GetType().FullName != BindingTypeName) continue;
                    if (binding != null) throw new InvalidOperationException("편집 씬에 공용 카메라 Binding이 둘 이상입니다.");
                    binding = candidate;
                }
            }
            if (mainCameras != 1 || binding == null)
                throw new InvalidOperationException("편집 씬에는 Main Camera와 공용 카메라 Binding이 각각 하나여야 합니다.");
            var values = new SerializedObject(binding);
            camera = values.FindProperty("virtualCamera")?.objectReferenceValue as CinemachineVirtualCamera;
            settings = values.FindProperty("settings")?.objectReferenceValue as ScriptableObject;
            if (camera == null || settings == null || camera.gameObject.scene != root.gameObject.scene)
                throw new InvalidOperationException("편집 씬의 공용 카메라 또는 설정 SO 참조가 잘못되었습니다.");
        }
    }
}
