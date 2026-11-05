using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ProjectL.Tool.Editor
{
    /// <summary>맵에 저장되는 Unity Light의 생성과 조회를 담당합니다.</summary>
    public static class FbxPlacementLights
    {
        public static readonly LightType[] Types =
            { LightType.Directional, LightType.Point, LightType.Spot, LightType.Rectangle, LightType.Disc };

        public static string GetName(LightType type) => type == LightType.Rectangle ? "Area (Rectangle)" :
            type == LightType.Disc ? "Area (Disc)" : type.ToString();

        public static bool IsArea(LightType type) => type == LightType.Rectangle || type == LightType.Disc;

        public static Light[] GetLights(GameObject root)
        {
            ValidateRoot(root);
            return root.GetComponentsInChildren<Light>(true);
        }

        /// <summary>위치는 맵 루트 기준입니다. 배치 레이어가 바뀌어도 월드 위치를 유지합니다.</summary>
        public static Light Add(GameObject root, LightType type, Vector3 position, Transform layer = null)
        {
            ValidateRoot(root);
            Transform parent = FbxPlacementLayers.Resolve(root, layer);
            if (Array.IndexOf(Types, type) < 0) throw new ArgumentException("지원하지 않는 라이트 종류입니다.", nameof(type));
            if (!float.IsFinite(position.x) || !float.IsFinite(position.y) || !float.IsFinite(position.z))
                throw new ArgumentException("라이트 위치는 유한한 수여야 합니다.", nameof(position));
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("재생 또는 컴파일 중에는 라이트를 추가할 수 없습니다.");

            const string undoName = "맵 라이트 추가";
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            GameObject instance = null;
            try
            {
                instance = EditorUtility.CreateGameObjectWithHideFlags(GetName(type) + " Light", HideFlags.HideAndDontSave, typeof(Light));
                SceneManager.MoveGameObjectToScene(instance, root.scene);
                instance.hideFlags = HideFlags.None;
                instance.transform.SetParent(parent, false);
                instance.transform.position = root.transform.TransformPoint(position);
                instance.transform.rotation = Quaternion.Euler(type == LightType.Directional ? new Vector3(50, -30, 0) : new Vector3(90, 0, 0));
                Light light = instance.GetComponent<Light>();
                light.type = type;
                light.color = Color.white;
                light.intensity = 1;
                light.range = 10;
                light.spotAngle = 45;
                light.innerSpotAngle = 25;
                light.lightmapBakeType = IsArea(type) ? LightmapBakeType.Baked : LightmapBakeType.Realtime;
                light.shadows = LightShadows.Soft;
                light.renderMode = LightRenderMode.ForcePixel;
                light.cullingMask = ~0;
                light.GetUniversalAdditionalLightData();
                Undo.RegisterCreatedObjectUndo(instance, undoName);
                Undo.CollapseUndoOperations(group);
                Undo.IncrementCurrentGroup();
                return light;
            }
            catch
            {
                if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }

        private static void ValidateRoot(GameObject root)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root))
                throw new ArgumentException("가상 씬의 맵 루트가 필요합니다.", nameof(root));
        }
    }
}
