using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.View.DrawPointLineSystem
{
    /// <summary>받은 순서대로 격자 셀 중심을 연결하는 표시 데이터입니다.</summary>
    public readonly struct DrawPointLineData
    {
        public string Name { get; }
        public Vector2Int[] Cells { get; }
        public Vector3 GridOrigin { get; }
        public Vector2 CellSize { get; }
        public float YOffset { get; }
        public float Thickness { get; }
        public float UvScale { get; }
        public Color Color { get; }
        public EffectType EndPointEffect { get; }

        public DrawPointLineData(
            string name,
            Vector2Int[] cells,
            Vector3 gridOrigin,
            Vector2 cellSize,
            float yOffset,
            float thickness,
            Color color,
            EffectType endPointEffect = EffectType.Empty,
            float uvScale = 1f)
        {
            Name = name;
            Cells = cells;
            GridOrigin = gridOrigin;
            CellSize = cellSize;
            YOffset = yOffset;
            Thickness = thickness;
            Color = color;
            EndPointEffect = endPointEffect;
            UvScale = uvScale;
        }
    }
}
