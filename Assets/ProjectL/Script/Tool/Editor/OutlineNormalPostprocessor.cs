using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{

    /// <summary>
    /// FBX Model Import 단계에서 Outline용 평균 Normal을 생성한다.
    ///
    /// TEXCOORD3 = Outline Normal
    ///
    /// 기존 Mesh.normal은 수정하지 않는다.
    /// </summary>
    public sealed class OutlineNormalPostprocessor : AssetPostprocessor
    {
        // Unity Shader 기준:
        // TEXCOORD0 = UV0
        // TEXCOORD1 = UV1
        // TEXCOORD2 = UV2
        // TEXCOORD3 = UV3
        private const int OutlineNormalChannel = 3;

        // 위치 비교 허용 오차.
        // 0.0001 단위까지 같은 위치로 취급.
        private const float PositionTolerance = 0.0001f;

        private void OnPostprocessModel(GameObject root)
        {
            MeshFilter[] meshFilters =
                root.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;

                if (mesh == null)
                    continue;

                GenerateOutlineNormals(mesh);
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
            {
                Mesh mesh = renderer.sharedMesh;

                if (mesh == null)
                    continue;

                GenerateOutlineNormals(mesh);
            }
        }

        private static void GenerateOutlineNormals(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            if (vertices == null || normals == null)
                return;

            if (vertices.Length == 0)
                return;

            if (vertices.Length != normals.Length)
            {
                Debug.LogWarning(
                    $"[OutlineNormal] Vertex/Normal count mismatch: {mesh.name}",
                    mesh);

                return;
            }

            // 같은 위치의 Vertex들을 그룹화한다.
            Dictionary<Vector3Int, List<int>> vertexGroups =
                new Dictionary<Vector3Int, List<int>>();

            for (int i = 0; i < vertices.Length; ++i)
            {
                Vector3Int key = QuantizePosition(vertices[i]);

                if (!vertexGroups.TryGetValue(key, out List<int> indices))
                {
                    indices = new List<int>();
                    vertexGroups.Add(key, indices);
                }

                indices.Add(i);
            }

            Vector3[] outlineNormals = new Vector3[vertices.Length];

            foreach (List<int> indices in vertexGroups.Values)
            {
                Vector3 averageNormal = Vector3.zero;

                foreach (int index in indices)
                {
                    averageNormal += normals[index];
                }

                // 평균 Normal 정규화
                if (averageNormal.sqrMagnitude > Mathf.Epsilon)
                {
                    averageNormal.Normalize();
                }
                else
                {
                    averageNormal = Vector3.up;
                }

                // 같은 위치의 모든 Vertex에
                // 동일한 Outline Normal을 기록한다.
                foreach (int index in indices)
                {
                    outlineNormals[index] = averageNormal;
                }
            }

            // TEXCOORD3에 Vector3 형태로 저장.
            mesh.SetUVs(OutlineNormalChannel, outlineNormals);
        }

        private static Vector3Int QuantizePosition(Vector3 position)
        {
            return new Vector3Int(
                Mathf.RoundToInt(position.x / PositionTolerance),
                Mathf.RoundToInt(position.y / PositionTolerance),
                Mathf.RoundToInt(position.z / PositionTolerance)
            );
        }
    }
}
