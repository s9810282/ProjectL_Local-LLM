using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{

    /// <summary>
    /// FBX Model Import �ܰ迡�� Outline�� ��� Normal�� �����Ѵ�.
    ///
    /// TEXCOORD3 = Outline Normal
    ///
    /// ���� Mesh.normal�� �������� �ʴ´�.
    /// </summary>
    public sealed class OutlineNormalPostprocessor : AssetPostprocessor
    {
        // Unity Shader ����:
        // TEXCOORD0 = UV0
        // TEXCOORD1 = UV1
        // TEXCOORD2 = UV2
        // TEXCOORD3 = UV3
        private const int OutlineNormalChannel = 3;

        // ��ġ �� ��� ����.
        // 0.0001 �������� ���� ��ġ�� ���.
        private const float PositionTolerance = 0.0001f;

        // 기존 임포트 캐시에도 평균 노멀 정규화 보완을 반영한다.
        public override uint GetVersion()
        {
            return 1;
        }

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

            // ���� ��ġ�� Vertex���� �׷�ȭ�Ѵ�.
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

                // ��� Normal ����ȭ
                // Normalize가 영벡터로 만드는 미세 잔차는 기본 방향으로 처리한다.
                if (averageNormal.sqrMagnitude > Vector3.kEpsilon * Vector3.kEpsilon)
                {
                    averageNormal.Normalize();
                }
                else
                {
                    averageNormal = Vector3.up;
                }

                // ���� ��ġ�� ��� Vertex��
                // ������ Outline Normal�� ����Ѵ�.
                foreach (int index in indices)
                {
                    outlineNormals[index] = averageNormal;
                }
            }

            // TEXCOORD3�� Vector3 ���·� ����.
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
