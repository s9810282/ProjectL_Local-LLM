using System;
using System.Reflection;
using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>작업실 메시의 표면 조회와 모델 외곽을 기준으로 한 접지 보정을 담당합니다.</summary>
    public static class FbxPlacementSurface
    {
        private delegate bool IntersectMesh(Ray ray, Mesh mesh, Matrix4x4 matrix, out RaycastHit hit);
        private static IntersectMesh _intersectMesh;

        public static bool TryRaycast(GameObject root, Ray ray, out RaycastHit hit, Transform ignore = null, Transform layer = null)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root))
                throw new ArgumentException("표면을 조회할 가상 씬의 맵 루트가 필요합니다.", nameof(root));
            if (layer != null) FbxPlacementLayers.Resolve(root, layer);
            if (_intersectMesh == null)
            {
                // Unity 에디터의 메시 교차 판정은 콜라이더와 Read/Write 활성화 없이 사용할 수 있습니다.
                // 내부 API 의존은 이 어댑터 한 곳에 제한합니다.
                MethodInfo method = typeof(HandleUtility).GetMethod("IntersectRayMesh",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new[] { typeof(Ray), typeof(Mesh), typeof(Matrix4x4), typeof(RaycastHit).MakeByRefType() }, null);
                if (method == null) throw new NotSupportedException("현재 Unity 버전에서 표면 배치용 메시 교차 API를 찾을 수 없습니다.");
                _intersectMesh = (IntersectMesh)Delegate.CreateDelegate(typeof(IntersectMesh), method);
            }

            hit = default;
            float nearest = float.PositiveInfinity;
            bool found = false;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    (layer != null && FbxPlacementLayers.GetContainingLayer(root, renderer.transform) != layer) ||
                    (ignore != null && (renderer.transform == ignore || renderer.transform.IsChildOf(ignore))) ||
                    !renderer.bounds.IntersectRay(ray, out float boundsDistance) || boundsDistance > nearest) continue;

                Mesh baked = null;
                try
                {
                    Mesh mesh = null;
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        baked = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                        skinned.BakeMesh(baked);
                        mesh = baked;
                    }
                    else if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter))
                        mesh = filter.sharedMesh;

                    if (mesh == null || !_intersectMesh(ray, mesh, renderer.localToWorldMatrix, out RaycastHit candidate) ||
                        candidate.distance < 0 || candidate.distance >= nearest) continue;
                    nearest = candidate.distance;
                    hit = candidate;
                    if (Vector3.Dot(hit.normal, ray.direction) > 0) hit.normal = -hit.normal;
                    found = true;
                }
                finally
                {
                    if (baked != null) UnityEngine.Object.DestroyImmediate(baked);
                }
            }
            return found;
        }

        /// <summary>실제 메시의 지지점을 접지하고, 피벗이 벗어나 있어도 모델 중심을 배치 지점에 맞춥니다.</summary>
        public static void Seat(Transform target, Vector3 point, Vector3 normal, bool alignToNormal, float offset)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            Validate(point, normal, offset);
            normal.Normalize();
            target.position = point;
            if (alignToNormal) target.rotation = Quaternion.FromToRotation(target.up, normal) * target.rotation;

            float minimum = float.PositiveInfinity;
            Bounds worldBounds = default;
            bool hasBounds = false;
            foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                Mesh baked = null;
                try
                {
                    Mesh mesh = null;
                    if (renderer is SkinnedMeshRenderer skinned)
                    {
                        baked = new Mesh { hideFlags = HideFlags.HideAndDontSave };
                        skinned.BakeMesh(baked);
                        mesh = baked;
                    }
                    else if (renderer is MeshRenderer && renderer.TryGetComponent(out MeshFilter filter))
                        mesh = filter.sharedMesh;
                    if (mesh == null) continue;

                    // 에디터 전용 읽기 API로 원본 Read/Write 설정을 바꾸지 않습니다.
                    using (Mesh.MeshDataArray data = MeshUtility.AcquireReadOnlyMeshData(mesh))
                    using (NativeArray<Vector3> vertices = new NativeArray<Vector3>(data[0].vertexCount, Allocator.Temp))
                    {
                        data[0].GetVertices(vertices);
                        Matrix4x4 matrix = renderer.localToWorldMatrix;
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            Vector3 vertex = matrix.MultiplyPoint3x4(vertices[i]);
                            minimum = Mathf.Min(minimum, Vector3.Dot(normal, vertex));
                            if (hasBounds) worldBounds.Encapsulate(vertex);
                            else { worldBounds = new Bounds(vertex, Vector3.zero); hasBounds = true; }
                        }
                    }
                }
                finally
                {
                    if (baked != null) UnityEngine.Object.DestroyImmediate(baked);
                }
            }
            if (!hasBounds) { target.position = point + normal * offset; return; }

            // 회전을 유지하는 지면 배치는 위아래로만 보정해 마우스의 X/Z 위치가 밀리지 않게 합니다.
            Vector3 axis = !alignToNormal && Vector3.Dot(normal, Vector3.up) > .05f ? Vector3.up : normal;
            Vector3 centerOffset = Vector3.ProjectOnPlane(point - worldBounds.center, axis);
            minimum += Vector3.Dot(normal, centerOffset);
            float lift = (Vector3.Dot(normal, point) - minimum + offset) / Vector3.Dot(normal, axis);
            target.position += centerOffset + axis * lift;
        }

        internal static void Validate(Vector3 point, Vector3 normal, float offset)
        {
            if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z) ||
                !Finite(normal.x) || !Finite(normal.y) || !Finite(normal.z) || normal.sqrMagnitude < .000001f || !Finite(offset))
                throw new ArgumentException("표면 위치와 간격은 유한한 값이어야 하며 표면 법선은 0일 수 없습니다.");
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
