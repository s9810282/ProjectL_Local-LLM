using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem
{
    /// <summary>
    /// ���� �޽� ���� ��û�� ���� ��Ŀ �������̽�.
    /// ��� ����� ���� �޽� ������ ����ü�� �� �������̽��� ��ӹ޾ƾ� �մϴ�.
    /// </summary>
    public interface IMeshUpdateData { }

    /// <summary>
    /// �����ӿ�ũ ���ο��� ������ ��忡 �����ϱ� ���� ���������� ������ ���� �޽� �������Դϴ�.
    /// </summary>
    internal struct MeshGeometryData
    {
        public string Name;
        public List<Vector3> Vertices;
        public List<int> Indices;
        public List<Vector2> UVs;
        public List<Vector3> Normals;
        public List<Color> Colors;

        public MeshGeometryData(string name, List<Vector3> vertices, List<int> indices, List<Vector2> uvs, List<Vector3> normals, List<Color> colors)
        {
            Name = name;
            Vertices = vertices;
            Indices = indices;
            UVs = uvs;
            Normals = normals;
            Colors = colors;
        }
    }

    /// <summary>
    /// ���� ������ü(Cube)�� �����ϱ� ���� ������.
    /// �ϵ� ������ ����� 24���� ���ؽ��� �����մϴ�.
    /// </summary>
    public struct CubeCreateData : IMeshUpdateData
    {
        public string Name;
        public Vector3 Center; // ������ü�� ���� �߽���
        public Vector3 Size;   // X, Y, Z ũ��
        public Color Color;    // ���ؽ� �÷�

        public CubeCreateData(string name, Vector3 center, Vector3 size, Color color)
        {
            Name = name;
            Center = center;
            Size = size;
            Color = color;
        }
    }

    /// <summary>
    /// 2D ��� ����(Circle)�� �����ϱ� ���� ������.
    /// </summary>
    public struct CircleCreateData : IMeshUpdateData
    {
        public string Name;
        public Vector3 Center;
        public float Radius;   // ������
        public int Segments;   // �ٰ����� ���� �� (�������� �ε巯�� ������ ��)
        public Color Color;

        public CircleCreateData(string name, Vector3 center, float radius, int segments, Color color)
        {
            Name = name;
            Center = center;
            Radius = radius;
            Segments = segments;
            Color = color;
        }
    }

    /// <summary>
    /// ��ü �����(Cylinder) ������ �����ϱ� ���� ������.
    /// </summary>
    public struct CylinderCreateData : IMeshUpdateData
    {
        public string Name;
        public Vector3 Center;
        public float Radius;
        public float Height;   // ������� ����
        public int Segments;
        public Color Color;

        public CylinderCreateData(string name, Vector3 center, float radius, float height, int segments, Color color)
        {
            Name = name;
            Center = center;
            Radius = radius;
            Height = height;
            Segments = segments;
            Color = color;
        }
    }

    /// <summary>
    /// �β��� ���� ���� ��(Line)�� �簢��(Quad) ���·� �����ϱ� ���� ������.
    /// </summary>
    public struct SingleLineCreateData : IMeshUpdateData
    {
        public string Name;
        public Vector3 StartPos; // ���� ���� ���� ��ǥ
        public Vector3 EndPos;   // ���� �� ���� ��ǥ
        public float Thickness;  // ���� �β�
        public Color Color;

        public SingleLineCreateData(string name, Vector3 startPos, Vector3 endPos, float thickness, Color color)
        {
            Name = name;
            StartPos = startPos;
            EndPos = endPos;
            Thickness = thickness;
            Color = color;
        }
    }

    /// <summary>
    /// ���� ���� ���� �̾� ���� �޽��� �������ϴ� ���� ��(Multi-Line) ������.
    /// �ؽ�ó�� ������ �ʵ��� ���� �Ÿ��� ����Ͽ� UV�� �����˴ϴ�.
    /// </summary>
    public struct MultiLineCreateData : IMeshUpdateData
    {
        public string Name;
        public Vector3[] Points; // �̾����� ������ ������ �迭
        public float Thickness;
        public float UvScale;    // 1 ���� �Ÿ��� UV �ؽ�ó�� �ݺ��Ǵ� ����
        public Color Color;

        public MultiLineCreateData(string name, Vector3[] points, float thickness, float uvScale, Color color)
        {
            Name = name;
            Points = points;
            Thickness = thickness;
            UvScale = uvScale;
            Color = color;
        }
    }

    /// <summary>
    /// �ټ��� ť�긦 1ȸ�� ��ο��ݷ� �׸��� ���� �׸��� ť�� ������.
    /// </summary>
    public struct GridCubeCreateData : IMeshUpdateData
    {
        public string Name;
        public Vector3[] Positions; // ������ ť����� ���� ��ǥ �迭
        public Vector3 CellSize;    // ť�� �ϳ��� �⺻ ũ��
        public Vector3 Padding;     // ť�� ������ ���� (���� ũ�� = CellSize - Padding)
        public Color Color;

        public GridCubeCreateData(string name, Vector3[] positions, Vector3 cellSize, Vector3 padding, Color color)
        {
            Name = name;
            Positions = positions;
            CellSize = cellSize;
            Padding = padding;
            Color = color;
        }
    }

    /// <summary>
    /// XZ 격자 셀 집합의 바깥 경계만 선으로 만드는 데이터입니다.
    /// Cells의 (0, 0)은 GridOrigin을 중심으로 하는 셀이고, 서로 맞닿은 셀의 내부 변은 생성하지 않습니다.
    /// </summary>
    public struct GridOutlineCreateData : IMeshUpdateData
    {
        public string Name;
        public Vector2Int[] Cells;
        public Vector3 GridOrigin;
        public Vector2 CellSize;
        public float YOffset;
        public float Thickness;
        public Color Color;
        /// <summary>
        /// 0보다 크면 UV U를 루프마다 0 → round(둘레 ÷ DashLength)로 늘립니다(최소 1). 셰이더의 반복 수를 1로 두면
        /// 루프 길이와 상관없이 같은 길이의 점선이 됩니다. 0이면 U는 루프마다 0 → 1입니다.
        /// </summary>
        public float DashLength;
        /// <summary>
        /// 0보다 크면 고리의 바깥으로 튀어나온(볼록) 꺾인 점마다 이 반지름(월드 길이)의 사분원으로 잇습니다.
        /// 안으로 꺾인(오목) 점은 직각 그대로입니다. 꺾인 점 앞뒤 변 길이의 반을 넘으면 그 반으로 자릅니다. 0 이하(또는 NaN)면 모두 직각입니다.
        /// </summary>
        public float CornerRadius;
        /// <summary>
        /// 0보다 크면 맞닿은 셀을 묶은 덩어리 경계의 변마다 안쪽으로 이 거리(월드 길이)만큼 옮긴 고리를 만듭니다.
        /// x는 세로(Z 방향) 변을 X로 옮기는 거리, y는 가로(X 방향) 변을 Z로 옮기는 거리입니다. 0이면 셀 경계 그대로입니다.
        /// 음수·NaN 축은 0으로 봅니다. 셀 크기의 반 이상이면 고리가 뒤집히므로 그보다 작아야 합니다(검사하지 않음).
        /// </summary>
        public Vector2 LoopInset;
        /// <summary>
        /// 참이면 셀을 덩어리로 묶지 않고 셀마다 네 변을 모두 그립니다. <see cref="LoopInset"/>·<see cref="CornerRadius"/>가 모두 0이면
        /// 맞닿은 두 셀이 함께 가진 변은 한 번만 그리고, 아니면 셀마다 고리 하나를 그 셀 경계에서 안쪽으로 줄이고 네 모서리를 둥글게 해 따로 그립니다.
        /// 거짓(기본)이면 맞닿은 셀을 묶은 덩어리의 바깥 경계만 그립니다.
        /// </summary>
        public bool EachCell;

        /// <summary>지정한 바깥 격자 변만 생략합니다. 기본값은 닫힌 고리입니다.</summary>
        public GridOutlineOpening Opening;

        public GridOutlineCreateData(
            string name,
            Vector2Int[] cells,
            Vector3 gridOrigin,
            Vector2 cellSize,
            float yOffset,
            float thickness,
            Color color,
            float dashLength = 0f,
            float cornerRadius = 0f,
            Vector2 loopInset = default,
            bool eachCell = false,
            GridOutlineOpening opening = default)
        {
            Name = name;
            Cells = cells;
            GridOrigin = gridOrigin;
            CellSize = cellSize;
            YOffset = yOffset;
            Thickness = thickness;
            Color = color;
            DashLength = dashLength;
            CornerRadius = cornerRadius;
            LoopInset = loopInset;
            EachCell = eachCell;
            Opening = opening;
        }
    }
}
