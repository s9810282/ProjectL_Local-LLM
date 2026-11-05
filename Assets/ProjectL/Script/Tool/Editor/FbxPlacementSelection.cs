using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>가상 씬의 사각 영역 안에 있는 배치 오브젝트와 선택 집합을 계산합니다.</summary>
    public static class FbxPlacementSelection
    {
        public enum Mode { Replace, Add, Toggle, Remove }

        public static Transform GetPlacedObject(GameObject root, Transform target)
        {
            if (root == null || target == null || target == root.transform || !target.IsChildOf(root.transform) ||
                FbxPlacementLayers.IsLayer(root, target)) return null;
            while (target.parent != root.transform && !FbxPlacementLayers.IsLayer(root, target.parent)) target = target.parent;
            return target;
        }

        /// <summary>카메라 뷰포트의 0~1 사각형과 겹치는 FBX 루트를 중복 없이 반환합니다.</summary>
        public static List<Transform> FindInRect(GameObject root, Camera camera, Rect viewportRect)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root)) throw new ArgumentException("가상 씬의 맵 루트가 필요합니다.", nameof(root));
            if (camera == null) throw new ArgumentNullException(nameof(camera));
            float minX = Mathf.Clamp01(Mathf.Min(viewportRect.xMin, viewportRect.xMax));
            float maxX = Mathf.Clamp01(Mathf.Max(viewportRect.xMin, viewportRect.xMax));
            float minY = Mathf.Clamp01(Mathf.Min(viewportRect.yMin, viewportRect.yMax));
            float maxY = Mathf.Clamp01(Mathf.Max(viewportRect.yMin, viewportRect.yMax));
            List<Transform> result = new List<Transform>();
            if (maxX - minX < .000001f || maxY - minY < .000001f) return result;

            Rect rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
            Matrix4x4 crop = Matrix4x4.identity;
            crop.m00 = 1 / rect.width;
            crop.m11 = 1 / rect.height;
            crop.m03 = (1 - 2 * rect.center.x) / rect.width;
            crop.m13 = (1 - 2 * rect.center.y) / rect.height;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(crop * camera.projectionMatrix * camera.worldToCameraMatrix);
            HashSet<Transform> seen = new HashSet<Transform>();
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    !GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) continue;
                Transform placed = GetPlacedObject(root, renderer.transform);
                if (placed != null && seen.Add(placed)) result.Add(placed);
            }
            foreach (Light light in FbxPlacementLights.GetLights(root))
            {
                if (!light.gameObject.activeInHierarchy ||
                    !GeometryUtility.TestPlanesAABB(planes, new Bounds(light.transform.position, Vector3.one * .1f))) continue;
                Transform placed = GetPlacedObject(root, light.transform);
                if (placed != null && seen.Add(placed)) result.Add(placed);
            }
            return result;
        }

        public static List<Transform> Combine(GameObject root, IEnumerable<Transform> previous,
            IEnumerable<Transform> hits, Mode mode)
        {
            List<Transform> result = new List<Transform>();
            HashSet<Transform> seen = new HashSet<Transform>();
            if (mode != Mode.Replace)
                foreach (Transform target in previous)
                {
                    Transform placed = GetPlacedObject(root, target);
                    if (placed != null && seen.Add(placed)) result.Add(placed);
                }
            HashSet<Transform> processed = new HashSet<Transform>();
            foreach (Transform target in hits)
            {
                Transform placed = GetPlacedObject(root, target);
                if (placed == null || !processed.Add(placed)) continue;
                if (mode == Mode.Remove || (mode == Mode.Toggle && seen.Contains(placed)))
                {
                    seen.Remove(placed);
                    result.Remove(placed);
                }
                else if (seen.Add(placed)) result.Add(placed);
            }
            return result;
        }
    }
}
