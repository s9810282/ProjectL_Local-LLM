using System;
using Cinemachine;
using Cinemachine.PostFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectL.Tool.Editor
{
    /// <summary>맵 프리팹에 저장할 Cinemachine 버추얼 카메라를 생성하고 조회합니다.</summary>
    public static class FbxPlacementCameras
    {
        public static CinemachineVirtualCamera[] GetCameras(GameObject root)
        {
            ValidateRoot(root);
            return root.GetComponentsInChildren<CinemachineVirtualCamera>(true);
        }

        /// <summary>위치는 맵 루트 기준이며 회전은 월드 회전입니다.</summary>
        public static CinemachineVirtualCamera Add(GameObject root, Vector3 position, Quaternion rotation)
        {
            ValidateRoot(root);
            if (!IsFinite(position) || !IsFinite(rotation))
                throw new ArgumentException("카메라 위치와 회전은 유한한 값이어야 합니다.");
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("재생 또는 컴파일 중에는 카메라를 설치할 수 없습니다.");

            const string undoName = "맵 버추얼 카메라 설치";
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            GameObject instance = null;
            try
            {
                instance = EditorUtility.CreateGameObjectWithHideFlags(GetUniqueName(root), HideFlags.HideAndDontSave,
                    typeof(CinemachineVirtualCamera), typeof(CinemachineImpulseListener), typeof(CinemachineVolumeSettings));
                SceneManager.MoveGameObjectToScene(instance, root.scene);
                instance.hideFlags = HideFlags.None;
                instance.transform.SetParent(root.transform, false);
                instance.transform.position = root.transform.TransformPoint(position);
                instance.transform.rotation = rotation;

                CinemachineVirtualCamera camera = instance.GetComponent<CinemachineVirtualCamera>();
                LensSettings lens = camera.m_Lens;
                lens.Orthographic = true;
                lens.OrthographicSize = 6f;
                lens.FieldOfView = 40f;
                lens.NearClipPlane = .1f;
                lens.FarClipPlane = 500f;
                camera.m_Lens = lens;
                camera.Priority = 10;
                instance.GetComponent<CinemachineImpulseListener>().m_ChannelMask = 1;

                Undo.RegisterCreatedObjectUndo(instance, undoName);
                Undo.CollapseUndoOperations(group);
                Undo.IncrementCurrentGroup();
                return camera;
            }
            catch
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static string GetUniqueName(GameObject root)
        {
            const string baseName = "Map Camera";
            CinemachineVirtualCamera[] cameras = root.GetComponentsInChildren<CinemachineVirtualCamera>(true);
            for (int suffix = 1; ; suffix++)
            {
                string candidate = suffix == 1 ? baseName : baseName + " " + suffix;
                bool exists = false;
                for (int i = 0; i < cameras.Length; i++)
                {
                    if (string.Equals(cameras[i].gameObject.name, candidate, StringComparison.Ordinal))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists) return candidate;
            }
        }

        private static void ValidateRoot(GameObject root)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root))
                throw new ArgumentException("가상 씬의 맵 루트가 필요합니다.", nameof(root));
        }

        private static bool IsFinite(Vector3 value) => float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);

        private static bool IsFinite(Quaternion value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z) && float.IsFinite(value.w);
    }
}
