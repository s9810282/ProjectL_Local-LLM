using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem
{
    /// <summary>
    /// ������ ����(MeshLowLevelBuilder)�� ���� ������ ���� ����ó�� �����Ͽ�,
    /// �ε��� ��Ģ(CW)�� ����Ʈ ������ ó���� �������� ������ 3D �޽� �����͸� �����ϴ� ������ ����(Factory) Ŭ�����Դϴ�.
    /// </summary>
    internal static class MeshHightLevelBuilder
    {
        // �Ϻ��ϰ� �ܺθ� ���ϴ� �ð����(CW)���� ������ �ε���
        // [����] ������(-X)/������(+X)�� ���� ������ �ڹٲ�� �־
        // ����� ������ ���ϴ� ������ ������ (���� ������ �ݴ�� ����)
        private static readonly int[][] CubeFaceIndices = new int[][]
        {
            new int[] { 7, 6, 5, 4 }, // �ո� (+Z)
            new int[] { 0, 1, 2, 3 }, // �޸� (-Z)
            new int[] { 1, 5, 6, 2 }, // ���� (+Y)
            new int[] { 3, 7, 4, 0 }, // �Ʒ��� (-Y)
            new int[] { 4, 5, 1, 0 }, // ������ (-X) - ���� {0,1,5,4}�� ����� +X(����)�� ������
            new int[] { 3, 2, 6, 7 }  // ������ (+X) - ���� {7,6,2,3}�� ����� -X(����)�� ������
        };

        public static MeshGeometryData CreateCube(CubeCreateData data)
        {
            var vertices = new List<Vector3>(24); var indices = new List<int>(36);
            var uvs = new List<Vector2>(24); var normals = new List<Vector3>(24); var colors = new List<Color>(24);

            MeshLowLevelBuilder.CalcCubeCorners(data.Center, data.Size, out Vector3[] c);
            Vector2[] quadUVs = MeshLowLevelBuilder.CalcQuadUVs();

            for (int i = 0; i < 6; i++)
            {
                int vIndex = vertices.Count;
                int[] face = CubeFaceIndices[i];

                Vector3 v0 = c[face[0]], v1 = c[face[1]], v2 = c[face[2]], v3 = c[face[3]];
                Vector3 normal = MeshLowLevelBuilder.CalcNormal(v0, v1, v2);

                vertices.Add(v0); vertices.Add(v1); vertices.Add(v2); vertices.Add(v3);
                uvs.Add(quadUVs[0]); uvs.Add(quadUVs[1]); uvs.Add(quadUVs[2]); uvs.Add(quadUVs[3]);

                for (int j = 0; j < 4; j++) { normals.Add(normal); colors.Add(data.Color); }

                indices.Add(vIndex + 0); indices.Add(vIndex + 1); indices.Add(vIndex + 2);
                indices.Add(vIndex + 0); indices.Add(vIndex + 2); indices.Add(vIndex + 3);
            }

            return new MeshGeometryData(data.Name, vertices, indices, uvs, normals, colors);
        }

        public static MeshGeometryData CreateCircle(CircleCreateData data)
        {
            int vCount = data.Segments + 1;
            var vertices = new List<Vector3>(vCount); var indices = new List<int>(data.Segments * 3);
            var uvs = new List<Vector2>(vCount); var normals = new List<Vector3>(vCount); var colors = new List<Color>(vCount);

            vertices.Add(data.Center);
            uvs.Add(new Vector2(0.5f, 0.5f));
            normals.Add(Vector3.up);
            colors.Add(data.Color);

            for (int i = 0; i < data.Segments; i++)
            {
                Vector3 pt = MeshLowLevelBuilder.CalcCirclePoint(data.Center, data.Radius, i, data.Segments);
                vertices.Add(pt);
                uvs.Add(new Vector2(pt.x / data.Radius * 0.5f + 0.5f, pt.z / data.Radius * 0.5f + 0.5f));
                normals.Add(Vector3.up);
                colors.Add(data.Color);

                int nextIndex = (i + 1) % data.Segments;
                indices.Add(0); indices.Add(i + 1); indices.Add(nextIndex + 1);
            }

            return new MeshGeometryData(data.Name, vertices, indices, uvs, normals, colors);
        }

        public static MeshGeometryData CreateCylinder(CylinderCreateData data)
        {
            var vertices = new List<Vector3>(); var indices = new List<int>();
            var uvs = new List<Vector2>(); var normals = new List<Vector3>(); var colors = new List<Color>();

            Vector3 topCenter = data.Center + new Vector3(0, data.Height * 0.5f, 0);
            Vector3 bottomCenter = data.Center - new Vector3(0, data.Height * 0.5f, 0);
            Vector2[] quadUVs = MeshLowLevelBuilder.CalcQuadUVs();

            for (int i = 0; i < data.Segments; i++)
            {
                int nextI = (i + 1) % data.Segments;
                Vector3 top1 = MeshLowLevelBuilder.CalcCirclePoint(topCenter, data.Radius, i, data.Segments);
                Vector3 top2 = MeshLowLevelBuilder.CalcCirclePoint(topCenter, data.Radius, nextI, data.Segments);
                Vector3 bot1 = MeshLowLevelBuilder.CalcCirclePoint(bottomCenter, data.Radius, i, data.Segments);
                Vector3 bot2 = MeshLowLevelBuilder.CalcCirclePoint(bottomCenter, data.Radius, nextI, data.Segments);

                int vIndex = vertices.Count;

                // �Ǹ��� ������ �ٱ��� ������ �ð���� �Է� ���� ����
                Vector3 normal = MeshLowLevelBuilder.CalcNormal(bot2, top2, top1);

                vertices.Add(bot2); vertices.Add(top2); vertices.Add(top1); vertices.Add(bot1);
                uvs.Add(quadUVs[0]); uvs.Add(quadUVs[1]); uvs.Add(quadUVs[2]); uvs.Add(quadUVs[3]);

                for (int j = 0; j < 4; j++) { normals.Add(normal); colors.Add(data.Color); }

                indices.Add(vIndex + 0); indices.Add(vIndex + 1); indices.Add(vIndex + 2);
                indices.Add(vIndex + 0); indices.Add(vIndex + 2); indices.Add(vIndex + 3);
            }

            // [����] ��/�Ʒ� �Ѳ�(Cap)�� ��� �Ǹ��� ���� �շ� ���̴� ���� ����.
            // ��(CreateCircle)�� ���� ����� �ﰢ��(triangle fan)�� ��/�Ʒ��� ���� �߰���.

            // ���� �Ѳ� (+Y �ٱ��� ���ؾ� �ϹǷ� CreateCircle�� ������ ���� ���� ���)
            int topCenterIndex = vertices.Count;
            vertices.Add(topCenter);
            uvs.Add(new Vector2(0.5f, 0.5f));
            normals.Add(Vector3.up);
            colors.Add(data.Color);

            for (int i = 0; i < data.Segments; i++)
            {
                Vector3 pt = MeshLowLevelBuilder.CalcCirclePoint(topCenter, data.Radius, i, data.Segments);
                vertices.Add(pt);
                uvs.Add(new Vector2(pt.x / data.Radius * 0.5f + 0.5f, pt.z / data.Radius * 0.5f + 0.5f));
                normals.Add(Vector3.up);
                colors.Add(data.Color);

                int nextIndex = (i + 1) % data.Segments;
                indices.Add(topCenterIndex); indices.Add(topCenterIndex + 1 + i); indices.Add(topCenterIndex + 1 + nextIndex);
            }

            // �Ʒ��� �Ѳ� (-Y �ٱ��� ���ؾ� �ϹǷ� ����� �ݴ�� ���� ������ ������)
            int bottomCenterIndex = vertices.Count;
            vertices.Add(bottomCenter);
            uvs.Add(new Vector2(0.5f, 0.5f));
            normals.Add(Vector3.down);
            colors.Add(data.Color);

            for (int i = 0; i < data.Segments; i++)
            {
                Vector3 pt = MeshLowLevelBuilder.CalcCirclePoint(bottomCenter, data.Radius, i, data.Segments);
                vertices.Add(pt);
                uvs.Add(new Vector2(pt.x / data.Radius * 0.5f + 0.5f, pt.z / data.Radius * 0.5f + 0.5f));
                normals.Add(Vector3.down);
                colors.Add(data.Color);

                int nextIndex = (i + 1) % data.Segments;
                // ����� �ݴ� ����(������ �� �ε����� ����)�� ���Ƽ� ����� -Y�� ���ϰ� ��
                indices.Add(bottomCenterIndex); indices.Add(bottomCenterIndex + 1 + nextIndex); indices.Add(bottomCenterIndex + 1 + i);
            }

            return new MeshGeometryData(data.Name, vertices, indices, uvs, normals, colors);
        }

        public static MeshGeometryData CreateSingleLine(SingleLineCreateData data)
        {
            var vertices = new List<Vector3>(4); var indices = new List<int>(6);
            var uvs = new List<Vector2>(4); var normals = new List<Vector3>(4); var colors = new List<Color>(4);

            MeshLowLevelBuilder.CalcLineCorners(data.StartPos, data.EndPos, data.Thickness, out Vector3 v0, out Vector3 v1, out Vector3 v2, out Vector3 v3);
            Vector2[] quadUVs = MeshLowLevelBuilder.CalcQuadUVs();

            vertices.Add(v0); vertices.Add(v1); vertices.Add(v2); vertices.Add(v3);
            uvs.AddRange(quadUVs);
            Vector3 normal = Vector3.up;
            for (int i = 0; i < 4; i++) { normals.Add(normal); colors.Add(data.Color); }

            // [����] ���� (0,1,2)/(0,2,3) ������ ������ -Y(�Ʒ�)�� ���ϴ� �����̶�
            // normal�� up���� �����ص� ������ ���� �޸�(�ø�)�̶� �� ������.
            // ������ �� �ε����� ���� �ٲ㼭 +Y(��)�� ���ϵ��� ���� ������ ������.
            indices.Add(0); indices.Add(2); indices.Add(1);
            indices.Add(0); indices.Add(3); indices.Add(2);

            return new MeshGeometryData(data.Name, vertices, indices, uvs, normals, colors);
        }

        public static MeshGeometryData CreateMultiLine(MultiLineCreateData data)
        {
            if (data.Points == null || data.Points.Length < 2)
                return new MeshGeometryData(data.Name, new List<Vector3>(), new List<int>(), new List<Vector2>(), new List<Vector3>(), new List<Color>());

            int segCount = data.Points.Length - 1;
            var vertices = new List<Vector3>(segCount * 4); var indices = new List<int>(segCount * 6);
            var uvs = new List<Vector2>(segCount * 4); var normals = new List<Vector3>(segCount * 4); var colors = new List<Color>(segCount * 4);

            float currentDist = 0f;
            Vector3 normal = Vector3.up;

            for (int i = 0; i < segCount; i++)
            {
                Vector3 pStart = data.Points[i];
                Vector3 pEnd = data.Points[i + 1];
                float nextDist = currentDist + (Vector3.Distance(pStart, pEnd) * data.UvScale);

                MeshLowLevelBuilder.CalcLineCorners(pStart, pEnd, data.Thickness, out Vector3 v0, out Vector3 v1, out Vector3 v2, out Vector3 v3);
                MeshLowLevelBuilder.CalcLineUVs(currentDist, nextDist, out Vector2 uv0, out Vector2 uv1, out Vector2 uv2, out Vector2 uv3);

                int vIndex = vertices.Count;
                vertices.Add(v0); vertices.Add(v1); vertices.Add(v2); vertices.Add(v3);
                uvs.Add(uv0); uvs.Add(uv1); uvs.Add(uv2); uvs.Add(uv3);

                for (int c = 0; c < 4; c++) { normals.Add(normal); colors.Add(data.Color); }

                // [����] SingleLine�� ������ ������ ���� ������ ������ +Y�� ���ϰ� ��
                indices.Add(vIndex + 0); indices.Add(vIndex + 2); indices.Add(vIndex + 1);
                indices.Add(vIndex + 0); indices.Add(vIndex + 3); indices.Add(vIndex + 2);

                currentDist = nextDist;
            }

            return new MeshGeometryData(data.Name, vertices, indices, uvs, normals, colors);
        }

        public static MeshGeometryData CreateGridCubes(GridCubeCreateData data)
        {
            if (data.Positions == null || data.Positions.Length == 0)
                return new MeshGeometryData(data.Name, new List<Vector3>(), new List<int>(), new List<Vector2>(), new List<Vector3>(), new List<Color>());

            int cubeCount = data.Positions.Length;
            var vertices = new List<Vector3>(cubeCount * 24);
            var indices = new List<int>(cubeCount * 36);
            var uvs = new List<Vector2>(cubeCount * 24);
            var normals = new List<Vector3>(cubeCount * 24);
            var colors = new List<Color>(cubeCount * 24);

            Vector2[] quadUVs = MeshLowLevelBuilder.CalcQuadUVs();
            Vector3 targetSize = data.CellSize - data.Padding;
            targetSize.x = Mathf.Max(0, targetSize.x);
            targetSize.y = Mathf.Max(0, targetSize.y);
            targetSize.z = Mathf.Max(0, targetSize.z);

            for (int p = 0; p < data.Positions.Length; p++)
            {
                Vector3 cubeCenter = data.Positions[p];
                MeshLowLevelBuilder.CalcCubeCorners(cubeCenter, targetSize, out Vector3[] c);

                for (int i = 0; i < 6; i++)
                {
                    int vIndex = vertices.Count;
                    int[] face = CubeFaceIndices[i];

                    Vector3 v0 = c[face[0]], v1 = c[face[1]], v2 = c[face[2]], v3 = c[face[3]];
                    Vector3 normal = MeshLowLevelBuilder.CalcNormal(v0, v1, v2);

                    vertices.Add(v0); vertices.Add(v1); vertices.Add(v2); vertices.Add(v3);
                    uvs.Add(quadUVs[0]); uvs.Add(quadUVs[1]); uvs.Add(quadUVs[2]); uvs.Add(quadUVs[3]);

                    for (int j = 0; j < 4; j++)
                    {
                        normals.Add(normal);
                        colors.Add(data.Color);
                    }

                    indices.Add(vIndex + 0); indices.Add(vIndex + 1); indices.Add(vIndex + 2);
                    indices.Add(vIndex + 0); indices.Add(vIndex + 2); indices.Add(vIndex + 3);
                }
            }

            return new MeshGeometryData(data.Name, vertices, indices, uvs, normals, colors);
        }

        public static MeshGeometryData CreateGridOutline(GridOutlineCreateData data)
        {
            if (data.Cells == null || data.Cells.Length == 0 ||
                data.CellSize.x <= 0f || data.CellSize.y <= 0f || data.Thickness <= 0f)
            {
                return new MeshGeometryData(data.Name, new List<Vector3>(), new List<int>(), new List<Vector2>(), new List<Vector3>(), new List<Color>());
            }

            var cells = new HashSet<Vector2Int>(data.Cells);
            var vertices = new List<Vector3>(cells.Count * 16);
            var indices = new List<int>(cells.Count * 24);
            var uvs = new List<Vector2>(cells.Count * 16);
            var normals = new List<Vector3>(cells.Count * 16);
            var colors = new List<Color>(cells.Count * 16);
            var edges = new List<(Vector2Int start, Vector2Int end)>();
            var outgoing = new Dictionary<Vector2Int, List<int>>();
            // Stable ordering keeps the UV seam independent of the input cell order.
            var sortedCells = new List<Vector2Int>(cells);
            sortedCells.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            foreach (var cell in sortedCells)
            {
                if (!cells.Contains(cell + Vector2Int.up))
                    AddBoundaryEdge(cell + Vector2Int.up, cell + Vector2Int.one, edges, outgoing);
                if (!cells.Contains(cell + Vector2Int.right))
                    AddBoundaryEdge(cell + Vector2Int.one, cell + Vector2Int.right, edges, outgoing);
                if (!cells.Contains(cell + Vector2Int.down))
                    AddBoundaryEdge(cell + Vector2Int.right, cell, edges, outgoing);
                if (!cells.Contains(cell + Vector2Int.left))
                    AddBoundaryEdge(cell, cell + Vector2Int.up, edges, outgoing);
            }

            var visited = new bool[edges.Count];
            for (int first = 0; first < edges.Count; first++)
            {
                if (visited[first])
                    continue;

                var loop = new List<Vector2Int>();
                int current = first;
                while (true)
                {
                    var edge = edges[current];
                    visited[current] = true;
                    loop.Add(edge.start);
                    if (edge.end == edges[first].start)
                        break;

                    // At a corner shared only diagonally, turn right to keep the
                    // two touching islands in separate loops instead of a figure eight.
                    Vector2Int direction = edge.end - edge.start;
                    int next = -1;
                    int bestRank = int.MaxValue;
                    foreach (int candidate in outgoing[edge.end])
                    {
                        if (visited[candidate])
                            continue;
                        Vector2Int nextDirection = edges[candidate].end - edges[candidate].start;
                        int cross = direction.x * nextDirection.y - direction.y * nextDirection.x;
                        int rank = cross < 0 ? 0 : (cross == 0 ? 1 : 2);
                        if (rank < bestRank)
                        {
                            bestRank = rank;
                            next = candidate;
                        }
                    }

                    if (next < 0)
                        throw new System.InvalidOperationException("Grid outline boundary is not closed.");
                    current = next;
                }

                AddOutlineLoop(loop, data, vertices, indices, uvs, normals, colors);
            }

            return new MeshGeometryData(data.Name, vertices, indices, uvs, normals, colors);
        }

        private static void AddBoundaryEdge(
            Vector2Int start,
            Vector2Int end,
            List<(Vector2Int start, Vector2Int end)> edges,
            Dictionary<Vector2Int, List<int>> outgoing)
        {
            if (!outgoing.TryGetValue(start, out List<int> candidates))
            {
                candidates = new List<int>(2);
                outgoing.Add(start, candidates);
            }
            candidates.Add(edges.Count);
            edges.Add((start, end));
        }

        private static void AddOutlineLoop(
            List<Vector2Int> loop,
            GridOutlineCreateData data,
            List<Vector3> vertices,
            List<int> indices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<Color> colors)
        {
            int count = loop.Count;
            var points = new Vector3[count];
            var offsets = new Vector3[count];
            var distances = new float[count + 1];
            for (int i = 0; i < count; i++)
            {
                points[i] = data.GridOrigin + new Vector3(
                    (loop[i].x - 0.5f) * data.CellSize.x,
                    data.YOffset,
                    (loop[i].y - 0.5f) * data.CellSize.y);
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 incoming = (points[i] - points[(i + count - 1) % count]).normalized;
                Vector3 outgoing = (points[(i + 1) % count] - points[i]).normalized;
                Vector3 previousNormal = Vector3.Cross(incoming, Vector3.up);
                Vector3 nextNormal = Vector3.Cross(outgoing, Vector3.up);
                // Intersection of the two offset edges: a shared diagonal miter
                // at both convex and concave corners, with no overlapping caps.
                offsets[i] = (previousNormal + nextNormal) *
                    (data.Thickness * 0.5f / (1f + Vector3.Dot(previousNormal, nextNormal)));
                distances[i + 1] = distances[i] + Vector3.Distance(points[i], points[(i + 1) % count]);
            }

            int firstVertex = vertices.Count;
            float perimeter = distances[count];
            // 점선 길이가 주어지면 루프마다 정수 반복 수로 U를 늘린다. 정수라 닫히는 이음매에서도 무늬가 이어진다.
            float repeats = data.DashLength > 0f ? Mathf.Max(1f, Mathf.Round(perimeter / data.DashLength)) : 1f;
            // One paired vertex section per boundary point. Duplicate only the
            // closing section so U can run continuously from 0 to 1 at the seam.
            for (int i = 0; i <= count; i++)
            {
                int point = i % count;
                vertices.Add(points[point] - offsets[point]);
                vertices.Add(points[point] + offsets[point]);
                float u = distances[i] / perimeter * repeats;
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));
                normals.Add(Vector3.up); normals.Add(Vector3.up);
                colors.Add(data.Color); colors.Add(data.Color);
                if (i == count)
                    continue;
                int index = firstVertex + i * 2;
                indices.Add(index); indices.Add(index + 3); indices.Add(index + 2);
                indices.Add(index); indices.Add(index + 1); indices.Add(index + 3);
            }
        }
    }
}
