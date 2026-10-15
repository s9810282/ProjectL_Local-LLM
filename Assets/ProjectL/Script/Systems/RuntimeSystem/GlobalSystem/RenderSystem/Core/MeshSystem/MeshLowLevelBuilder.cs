using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem
{
    /// <summary>
    /// ����(State)�� ������ �ʰ�, �޽��� �׸��� ���� ���� ������ ��ġ, ���, UV������ ����Ͽ� ��ȯ�ϴ� ������ ��ƿ��Ƽ Ŭ�����Դϴ�.
    /// �迭 �Ҵ�(GC)�� �������� ������ ������ �������� ��ǰó�� ������ ����մϴ�.
    /// </summary>
    internal static class MeshLowLevelBuilder
    {
        public static Vector3 CalcNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            return Vector3.Cross(b - a, c - a).normalized;
        }

        public static void CalcCubeCorners(Vector3 center, Vector3 size, out Vector3[] corners)
        {
            corners = new Vector3[8];
            Vector3 ext = size * 0.5f;

            corners[0] = center + new Vector3(-ext.x, -ext.y, -ext.z);
            corners[1] = center + new Vector3(-ext.x, ext.y, -ext.z);
            corners[2] = center + new Vector3(ext.x, ext.y, -ext.z);
            corners[3] = center + new Vector3(ext.x, -ext.y, -ext.z);
            corners[4] = center + new Vector3(-ext.x, -ext.y, ext.z);
            corners[5] = center + new Vector3(-ext.x, ext.y, ext.z);
            corners[6] = center + new Vector3(ext.x, ext.y, ext.z);
            corners[7] = center + new Vector3(ext.x, -ext.y, ext.z);
        }

        public static void CalcLineCorners(Vector3 start, Vector3 end, float thickness, out Vector3 v0, out Vector3 v1, out Vector3 v2, out Vector3 v3)
        {
            Vector3 dir = (end - start).normalized;
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.001f) right = Vector3.Cross(dir, Vector3.forward).normalized;

            Vector3 offset = right * (thickness * 0.5f);

            // 0(���ϴ�), 1(�»��), 2(����), 3(���ϴ�) �ð���� �԰� ����
            v0 = start - offset;
            v1 = end - offset;
            v2 = end + offset;
            v3 = start + offset;
        }

        public static Vector3 CalcCirclePoint(Vector3 center, float radius, int index, int segments, bool isYAxis = true)
        {
            float angle = ((float)index / segments) * Mathf.PI * 2f;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;

            return isYAxis ? center + new Vector3(x, 0, z) : center + new Vector3(x, z, 0);
        }

        public static Vector2[] CalcQuadUVs() => new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };

        public static void CalcLineUVs(float startV, float endV, out Vector2 uv0, out Vector2 uv1, out Vector2 uv2, out Vector2 uv3)
        {
            uv0 = new Vector2(0f, startV); uv1 = new Vector2(1f, startV);
            uv2 = new Vector2(1f, endV); uv3 = new Vector2(0f, endV);
        }
    }
}