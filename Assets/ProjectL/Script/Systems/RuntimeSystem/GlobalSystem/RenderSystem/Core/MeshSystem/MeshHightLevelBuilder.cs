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

            if (data.EachCell)
            {
                AddEachCellOutline(sortedCells, data, vertices, indices, uvs, normals, colors);
                return new MeshGeometryData(data.Name, vertices, indices, uvs, normals, colors);
            }

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

                AddOutlineLoop(GridLoopPoints(loop, data), data, vertices, indices, uvs, normals, colors);
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

        // 격자 점 고리(셀 모서리 정수 좌표)를 월드 점으로 바꿉니다.
        private static Vector3[] GridLoopPoints(List<Vector2Int> loop, GridOutlineCreateData data)
        {
            var points = new Vector3[loop.Count];
            for (int i = 0; i < loop.Count; i++)
                points[i] = GridPoint(loop[i], data);

            return points;
        }

        // 격자 점(셀 모서리 정수 좌표)의 월드 자리입니다. 셀 (0, 0)의 가운데가 GridOrigin입니다.
        private static Vector3 GridPoint(Vector2Int point, GridOutlineCreateData data)
        {
            return data.GridOrigin + new Vector3(
                (point.x - 0.5f) * data.CellSize.x,
                data.YOffset,
                (point.y - 0.5f) * data.CellSize.y);
        }

        // 점선 길이가 주어지면 길이 ÷ 점선 길이(반올림, 최소 1)번, 아니면 1번 U가 반복됩니다. 고리와 곧은 줄이 같은 규칙입니다.
        private static float DashRepeats(float length, float dashLength) =>
            dashLength > 0f ? Mathf.Max(1f, Mathf.Round(length / dashLength)) : 1f;

        /// <summary>
        /// 셀마다 네 변을 모두 그립니다(<see cref="GridOutlineCreateData.EachCell"/>). 안쪽 거리·모서리 반지름이 모두 0이면 맞닿은 두 셀이 함께 가진
        /// 변은 한 번만 그리고(<see cref="AddCellGridLines"/>), 아니면 셀마다 고리 하나를 그 셀 경계에서 안쪽 거리만큼 줄이고 네 모서리를 둥글게 해
        /// 따로 그립니다(맞닿은 변 자리에는 고리 둘). 셀 하나짜리 고리는 꺾인 점 넷이 모두 볼록입니다.
        /// </summary>
        private static void AddEachCellOutline(
            List<Vector2Int> sortedCells,
            GridOutlineCreateData data,
            List<Vector3> vertices,
            List<int> indices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<Color> colors)
        {
            // 음수·NaN 안쪽 거리와 NaN 반지름은 0으로 봅니다(고리 식과 같음).
            Vector2 loopInset = Vector2.Max(data.LoopInset, Vector2.zero);
            if (loopInset.x <= 0f && loopInset.y <= 0f && !(data.CornerRadius > 0f))
            {
                AddCellGridLines(sortedCells, data, vertices, indices, uvs, normals, colors);
                return;
            }

            var loop = new List<Vector2Int>(4);
            foreach (var cell in sortedCells)
            {
                // 덩어리 경계 고리와 같은 방향입니다(위에서 보아 시계 방향, 진행 방향 오른쪽이 셀 안쪽).
                loop.Clear();
                loop.Add(cell + Vector2Int.up);
                loop.Add(cell + Vector2Int.one);
                loop.Add(cell + Vector2Int.right);
                loop.Add(cell);
                AddOutlineLoop(GridLoopPoints(loop, data), data, vertices, indices, uvs, normals, colors);
            }
        }

        /// <summary>
        /// 셀마다 네 변을 그리되 맞닿은 두 셀이 함께 가진 변은 한 번만 그립니다(EachCell, 안쪽 거리·반지름 0). 띠는 고리처럼 변 가운데에 걸칩니다.
        /// 가로(X 방향) 변은 같은 줄에 이어진 것끼리 띠 하나로 그리고 양 끝을 두께 반만큼 늘려 꺾인 자리·교차점을 덮습니다.
        /// 세로(Z 방향) 변은 변마다 양 끝을 두께 반만큼 줄여 가로 띠와 겹치지 않게 합니다(반투명 색이 교차점에서 두 번 섞이지 않음).
        /// 셀 꼭짓점마다 그 셀의 가로 변이 지나므로 세로 변 끝은 늘 가로 띠 안입니다. 세로 변의 점선 U는 같은 열에 이어진 줄 전체 길이로 매겨
        /// 교차점을 건너뛰어도 무늬가 이어집니다.
        /// </summary>
        private static void AddCellGridLines(
            List<Vector2Int> sortedCells,
            GridOutlineCreateData data,
            List<Vector3> vertices,
            List<int> indices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<Color> colors)
        {
            // 가로 변 (x, y)는 격자 점 (x, y) → (x + 1, y), 세로 변 (x, y)는 (x, y) → (x, y + 1)입니다. 맞닿은 셀이 함께 가진 변은 하나로 모입니다.
            var horizontal = new HashSet<Vector2Int>();
            var vertical = new HashSet<Vector2Int>();
            foreach (var cell in sortedCells)
            {
                // 셀 고리와 같은 진행 방향으로 검사합니다. 범위의 지정 경계선만 빠져 내부 공유 변은 유지됩니다.
                if (!data.Opening.IsEnabled || !GridOutlineOpeningClip.Cuts(GridPoint(cell + Vector2Int.right, data), GridPoint(cell, data), data, 0f))
                    horizontal.Add(cell);
                if (!data.Opening.IsEnabled || !GridOutlineOpeningClip.Cuts(GridPoint(cell + Vector2Int.up, data), GridPoint(cell + Vector2Int.one, data), data, 0f))
                    horizontal.Add(cell + Vector2Int.up);
                if (!data.Opening.IsEnabled || !GridOutlineOpeningClip.Cuts(GridPoint(cell, data), GridPoint(cell + Vector2Int.up, data), data, 0f))
                    vertical.Add(cell);
                if (!data.Opening.IsEnabled || !GridOutlineOpeningClip.Cuts(GridPoint(cell + Vector2Int.one, data), GridPoint(cell + Vector2Int.right, data), data, 0f))
                    vertical.Add(cell + Vector2Int.right);
            }

            // 같은 줄(y)은 x 순, 같은 열(x)은 y 순으로 늘어놓아 이어진 변을 한 줄로 묶습니다.
            var rows = new List<Vector2Int>(horizontal);
            rows.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
            var columns = new List<Vector2Int>(vertical);
            columns.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

            float halfThickness = data.Thickness * 0.5f;
            for (int start = 0; start < rows.Count;)
            {
                int end = RunEnd(rows, start, Vector2Int.right);
                Vector3 from = GridPoint(rows[start], data) - (Vector3.right * halfThickness);
                Vector3 to = GridPoint(rows[end - 1] + Vector2Int.right, data) + (Vector3.right * halfThickness);
                AddBand(from, to, 0f, DashRepeats(Vector3.Distance(from, to), data.DashLength), halfThickness, data.Color,
                    vertices, indices, uvs, normals, colors);
                start = end;
            }

            for (int start = 0; start < columns.Count;)
            {
                int end = RunEnd(columns, start, Vector2Int.up);
                // 줄 전체(양 끝을 두께 반만큼 늘린 길이)를 U 기준으로 삼습니다. 거리는 이 줄 시작점에서 잽니다.
                Vector3 runStart = GridPoint(columns[start], data) - (Vector3.forward * halfThickness);
                float runLength = ((end - start) * data.CellSize.y) + data.Thickness;
                float repeats = DashRepeats(runLength, data.DashLength);
                for (int i = start; i < end; i++)
                {
                    float near = ((i - start) * data.CellSize.y) + data.Thickness;
                    float far = (i - start + 1) * data.CellSize.y;
                    // 두께가 셀 길이 이상이면 가로 띠가 이 변을 다 덮습니다.
                    if (far <= near)
                        continue;
                    AddBand(runStart + (Vector3.forward * near), runStart + (Vector3.forward * far),
                        near / runLength * repeats, far / runLength * repeats, halfThickness, data.Color,
                        vertices, indices, uvs, normals, colors);
                }

                start = end;
            }
        }

        // sorted[start]부터 step 방향으로 하나씩 이어진 변 묶음의 끝(포함하지 않음)입니다.
        private static int RunEnd(List<Vector2Int> sorted, int start, Vector2Int step)
        {
            int end = start + 1;
            while (end < sorted.Count && sorted[end] == sorted[end - 1] + step)
                end++;
            return end;
        }

        // 곧은 띠 하나를 넣습니다. 띠 양쪽은 진행 방향 × 위('+' 쪽)로 두께 반씩이고, U는 from → to로 u0 → u1입니다(고리와 같은 꼭짓점·삼각형 순서).
        private static void AddBand(
            Vector3 from,
            Vector3 to,
            float u0,
            float u1,
            float halfThickness,
            Color color,
            List<Vector3> vertices,
            List<int> indices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<Color> colors)
        {
            Vector3 side = Vector3.Cross((to - from).normalized, Vector3.up) * halfThickness;
            int first = vertices.Count;
            vertices.Add(from - side);
            vertices.Add(from + side);
            vertices.Add(to - side);
            vertices.Add(to + side);
            uvs.Add(new Vector2(u0, 0f));
            uvs.Add(new Vector2(u0, 1f));
            uvs.Add(new Vector2(u1, 0f));
            uvs.Add(new Vector2(u1, 1f));
            for (int i = 0; i < 4; i++)
            {
                normals.Add(Vector3.up);
                colors.Add(color);
            }

            indices.Add(first); indices.Add(first + 3); indices.Add(first + 2);
            indices.Add(first); indices.Add(first + 1); indices.Add(first + 3);
        }

        // 둥근 꺾임 하나(사분원)를 이루는 직선 조각 수입니다.
        private const int CornerArcSegments = 6;

        // 격자 고리는 늘 진행 방향 오른쪽이 덩어리 안쪽입니다(위에서 보아 시계 방향, 구멍 둘레는 반대 방향).
        // 그래서 띠 '+' 쪽(진행 방향 × 위)은 늘 바깥이고, 오른쪽으로 꺾이는 점이 볼록입니다.
        private static void AddOutlineLoop(
            Vector3[] points,
            GridOutlineCreateData data,
            List<Vector3> vertices,
            List<int> indices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<Color> colors)
        {
            float halfThickness = data.Thickness * 0.5f;
            // 음수·NaN 축은 0으로 봅니다(Mathf.Max). 셀 반 이상이면 고리가 뒤집히므로 부르는 쪽이 그보다 작게 넘깁니다.
            Vector2 loopInset = Vector2.Max(data.LoopInset, Vector2.zero);
            if (loopInset.x > 0f || loopInset.y > 0f)
                points = InsetCorners(CornersOf(points), loopInset);
            Vector3[] minus;
            Vector3[] plus;
            if (data.CornerRadius > 0f)
                points = RoundCorners(points, data.CornerRadius, halfThickness, out minus, out plus);
            else
                MiterSides(points, halfThickness, out minus, out plus);

            int count = points.Length;
            var distances = new float[count + 1];
            for (int i = 0; i < count; i++)
                distances[i + 1] = distances[i] + Vector3.Distance(points[i], points[(i + 1) % count]);

            int firstVertex = vertices.Count;
            float perimeter = distances[count];
            // 점선 길이가 주어지면 루프마다 정수 반복 수로 U를 늘린다. 정수라 닫히는 이음매에서도 무늬가 이어진다.
            float repeats = DashRepeats(perimeter, data.DashLength);
            // One paired vertex section per boundary point. Duplicate only the
            // closing section so U can run continuously from 0 to 1 at the seam.
            for (int i = 0; i <= count; i++)
            {
                int point = i % count;
                vertices.Add(minus[point]);
                vertices.Add(plus[point]);
                float u = distances[i] / perimeter * repeats;
                uvs.Add(new Vector2(u, 0f));
                uvs.Add(new Vector2(u, 1f));
                normals.Add(Vector3.up); normals.Add(Vector3.up);
                colors.Add(data.Color); colors.Add(data.Color);
                if (i == count)
                    continue;
                // 고리를 만들 때 변을 빼면 닫힌 경계 탐색·둥근 모서리가 깨지므로 완성된 띠의 삼각형만 생략합니다.
                if (GridOutlineOpeningClip.Cuts(points[point], points[(point + 1) % count], data, data.CornerRadius))
                    continue;
                int index = firstVertex + i * 2;
                indices.Add(index); indices.Add(index + 3); indices.Add(index + 2);
                indices.Add(index); indices.Add(index + 1); indices.Add(index + 3);
            }
        }

        // 꺾인 점마다 띠 양쪽 점을 사선(두 변을 두께 반만큼 민 선의 교점)으로 둡니다. 볼록·오목 모두 같은 식이고 덧대는 끝이 없습니다.
        private static void MiterSides(Vector3[] points, float halfThickness, out Vector3[] minus, out Vector3[] plus)
        {
            int count = points.Length;
            minus = new Vector3[count];
            plus = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 incoming = (points[i] - points[(i + count - 1) % count]).normalized;
                Vector3 outgoing = (points[(i + 1) % count] - points[i]).normalized;
                Vector3 previousNormal = Vector3.Cross(incoming, Vector3.up);
                Vector3 nextNormal = Vector3.Cross(outgoing, Vector3.up);
                Vector3 offset = (previousNormal + nextNormal) *
                    (halfThickness / (1f + Vector3.Dot(previousNormal, nextNormal)));
                minus[i] = points[i] - offset;
                plus[i] = points[i] + offset;
            }
        }

        /// <summary>
        /// 고리의 볼록 꺾인 점을 사분원 조각 점으로 바꾸고 띠 양쪽 점을 함께 만듭니다. 격자 고리는 늘 직각으로 꺾입니다.
        /// 오목 꺾인 점은 사선 식 그대로 직각입니다. 곧은 변 가운데 점은 먼저 빼고, 반지름은 꺾인 점 앞뒤 변 길이의 반으로 자릅니다
        /// (이웃 꺾임의 원이 겹치지 않게). 띠 안쪽은 원 중심 쪽으로 반지름 − 두께 반이고, 반지름이 두께 반보다 작으면 두 변 안쪽 선의 교점에 모입니다.
        /// </summary>
        private static Vector3[] RoundCorners(Vector3[] loop, float radius, float halfThickness, out Vector3[] minus, out Vector3[] plus)
        {
            Vector3[] corners = CornersOf(loop);
            int cornerCount = corners.Length;
            if (cornerCount < 3)
            {
                // 꺾인 점이 없는 고리는 격자에서 생기지 않지만, 생기면 직각 식으로 그립니다.
                MiterSides(loop, halfThickness, out minus, out plus);
                return loop;
            }

            var points = new List<Vector3>(cornerCount * (CornerArcSegments + 1));
            var minusSide = new List<Vector3>(points.Capacity);
            var plusSide = new List<Vector3>(points.Capacity);
            for (int i = 0; i < cornerCount; i++)
            {
                Vector3 corner = corners[i];
                Vector3 toCorner = corner - corners[(i + cornerCount - 1) % cornerCount];
                Vector3 fromCorner = corners[(i + 1) % cornerCount] - corner;
                float lengthIn = toCorner.magnitude;
                float lengthOut = fromCorner.magnitude;
                Vector3 directionIn = toCorner / lengthIn;
                Vector3 directionOut = fromCorner / lengthOut;
                if (Vector3.Cross(directionIn, directionOut).y <= 0f)
                {
                    // 오목(왼쪽으로 꺾임)은 직각입니다. 두 변이 직각이라 사선 식이 (앞 변 바깥 + 뒤 변 바깥) × 두께 반입니다.
                    Vector3 offset = (Vector3.Cross(directionIn, Vector3.up) + Vector3.Cross(directionOut, Vector3.up)) * halfThickness;
                    points.Add(corner);
                    minusSide.Add(corner - offset);
                    plusSide.Add(corner + offset);
                    continue;
                }

                float cornerRadius = Mathf.Min(radius, lengthIn * 0.5f, lengthOut * 0.5f);
                // 두 변에 닿는 원의 중심입니다(덩어리 안쪽).
                Vector3 center = corner + ((directionOut - directionIn) * cornerRadius);
                Vector3 innerCorner = corner + ((directionOut - directionIn) * halfThickness);
                for (int k = 0; k <= CornerArcSegments; k++)
                {
                    float angle = k * (Mathf.PI * 0.5f) / CornerArcSegments;
                    // 원 시작(들어오는 변 위)에서 끝(나가는 변 위)까지의 중심 → 점 방향입니다.
                    Vector3 radial = (-directionOut * Mathf.Cos(angle)) + (directionIn * Mathf.Sin(angle));
                    points.Add(center + (radial * cornerRadius));
                    // 볼록 꺾임에서는 띠 '+' 쪽(바깥)이 원 바깥입니다.
                    plusSide.Add(center + (radial * (cornerRadius + halfThickness)));
                    minusSide.Add(cornerRadius > halfThickness
                        ? center + (radial * (cornerRadius - halfThickness))
                        : innerCorner);
                }
            }

            minus = minusSide.ToArray();
            plus = plusSide.ToArray();
            return points.ToArray();
        }

        // 곧은 변 가운데 점을 빼고 꺾인 점만 남깁니다.
        private static Vector3[] CornersOf(Vector3[] loop)
        {
            var corners = new List<Vector3>(loop.Length);
            int count = loop.Length;
            for (int i = 0; i < count; i++)
            {
                Vector3 incoming = (loop[i] - loop[(i + count - 1) % count]).normalized;
                Vector3 outgoing = (loop[(i + 1) % count] - loop[i]).normalized;
                if (Vector3.Cross(incoming, outgoing).sqrMagnitude > 1e-6f)
                    corners.Add(loop[i]);
            }

            return corners.ToArray();
        }

        /// <summary>
        /// 꺾인 점마다 앞뒤 변을 안쪽(진행 방향 오른쪽)으로 옮긴 두 선의 교점을 돌려줍니다. 가로(X 방향) 변은 Z로 <c>inset.y</c>,
        /// 세로(Z 방향) 변은 X로 <c>inset.x</c>만큼 옮깁니다. 앞뒤 변이 직각이라 교점은 볼록·오목 모두 두 이동을 더한 자리입니다.
        /// 맞닿은 셀 사이에는 처음부터 경계가 없어 줄어든 덩어리도 그대로 이어집니다.
        /// </summary>
        private static Vector3[] InsetCorners(Vector3[] corners, Vector2 inset)
        {
            int count = corners.Length;
            if (count < 3)
                return corners;

            var result = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 incoming = (corners[i] - corners[(i + count - 1) % count]).normalized;
                Vector3 outgoing = (corners[(i + 1) % count] - corners[i]).normalized;
                result[i] = corners[i] + InwardShift(incoming, inset) + InwardShift(outgoing, inset);
            }

            return result;
        }

        // 진행 방향 변을 안쪽(위 × 진행 방향 = 오른쪽)으로 옮기는 변위입니다.
        private static Vector3 InwardShift(Vector3 direction, Vector2 inset)
        {
            float distance = Mathf.Abs(direction.x) > Mathf.Abs(direction.z) ? inset.y : inset.x;
            return Vector3.Cross(Vector3.up, direction) * distance;
        }
    }
}
