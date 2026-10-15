using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace ProjectL.Render.Shader.PostProcessing
{
    internal static class RenderFeatureCameraFilter
    {
        public static bool ShouldRender(Camera camera)
        {
            if (camera == null)
                return false;

            CameraType cameraType = camera.cameraType;
            if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
                return false;

    #if UNITY_EDITOR
            // Prefab and other preview scenes can also contain Game cameras.
            if (EditorSceneManager.IsPreviewSceneObject(camera.gameObject))
                return false;

            // Prefab Mode uses a SceneView camera, so CameraType alone is insufficient.
            if (cameraType == CameraType.SceneView &&
                PrefabStageUtility.GetCurrentPrefabStage() != null)
                return false;
    #endif

            return true;
        }
    }
}
