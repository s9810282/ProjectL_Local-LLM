using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.MeshSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 범위 원점의 인접 경계와, 원점이 바깥이면 그쪽 한 변을 엽니다. 어느 층이 열고 원점이 어디인지는 층 표의 "원점 열기"가 정합니다(평가 C1-21).
    /// 호버 갱신마다 불려 인터페이스 열거자를 만들지 않습니다(평가 C11-07).
    /// </summary>
    internal static class FieldAttackOutlineOpening
    {
        internal static GridOutlineOpening Of(IFieldHighlightStateReader state, FieldHighlightLayer layer, Vector2 cellSize)
        {
            if (layer == FieldHighlightLayer.Affected && state.IsAffectedOutlineClosed)
                return default;

            bool hoverRange = FieldHighlightLayers.IsHoverRange(layer);
            if (!FieldHighlightLayers.OpensAtSource(layer))
                return default;
            // 선택 유닛 테두리는 선택 유닛 칸이 원점인 다른 범위 층이 칠해져 있거나, 호버 범위 원점이 선택 유닛일 때만 엽니다.
            FieldHighlightLayer actorRanges =
                FieldHighlightLayers.OpensAtSourceLayers & ~(FieldHighlightLayers.HoverRangeLayers | FieldHighlightLayer.ActiveActor);
            if (layer == FieldHighlightLayer.ActiveActor && !state.HasAnyCell(actorRanges))
            {
                bool sourceHovered = false;
                var sources = state.HoverRangeSourceCells;
                for (int i = 0; i < sources.Count; i++)
                    if (state.TryGetIndex(sources[i], out int index) && (state.LayersAt(index) & FieldHighlightLayer.ActiveActor) != 0)
                        sourceHovered = true;
                if (!sourceHovered)
                    return default;
            }

            Vector2Int sourceMin = new(int.MaxValue, int.MaxValue);
            Vector2Int sourceMax = new(int.MinValue, int.MinValue);
            Vector2Int min = sourceMin;
            Vector2Int max = sourceMax;
            if (hoverRange)
            {
                var sources = state.HoverRangeSourceCells;
                for (int i = 0; i < sources.Count; i++)
                {
                    Vector2Int point = new(sources[i].X, sources[i].Y);
                    sourceMin = Vector2Int.Min(sourceMin, point);
                    sourceMax = Vector2Int.Max(sourceMax, point);
                }
            }
            for (int i = 0; i < state.CellCount; i++)
            {
                FieldHighlightLayer layers = state.LayersAt(i);
                if ((layers & (layer | FieldHighlightLayer.ActiveActor)) == 0)
                    continue;
                Vector2Int cell = new(i % state.ColumnCount, i / state.ColumnCount);
                if (!hoverRange && (layers & FieldHighlightLayer.ActiveActor) != 0)
                {
                    sourceMin = Vector2Int.Min(sourceMin, cell);
                    sourceMax = Vector2Int.Max(sourceMax, cell);
                }
                if ((layers & layer) != 0)
                {
                    min = Vector2Int.Min(min, cell);
                    max = Vector2Int.Max(max, cell);
                }
            }
            if (sourceMin.x == int.MaxValue || min.x == int.MaxValue)
                return default;

            RectInt clearArea = new(sourceMin, sourceMax - sourceMin + Vector2Int.one);
            int gapX = sourceMax.x < min.x ? min.x - sourceMax.x : sourceMin.x > max.x ? sourceMin.x - max.x : 0;
            int gapZ = sourceMax.y < min.y ? min.y - sourceMax.y : sourceMin.y > max.y ? sourceMin.y - max.y : 0;
            if (gapX == 0 && gapZ == 0)
                return new GridOutlineOpening(GridOutlineSide.None, 0, clearArea);

            if (gapX * cellSize.x >= gapZ * cellSize.y)
                return sourceMax.x < min.x
                    ? new GridOutlineOpening(GridOutlineSide.Left, min.x, clearArea)
                    : new GridOutlineOpening(GridOutlineSide.Right, max.x + 1, clearArea);
            return sourceMax.y < min.y
                ? new GridOutlineOpening(GridOutlineSide.Down, min.y, clearArea)
                : new GridOutlineOpening(GridOutlineSide.Up, max.y + 1, clearArea);
        }

        internal static GridOutlineOpening ForHover(IFieldHighlightStateReader state, FieldHighlightLayer mergedLayer, Vector2 cellSize)
        {
            if (mergedLayer != FieldHighlightLayer.None)
                return Of(state, mergedLayer, cellSize);
            if (state.HoverIndex < 0 || !state.IsActing)
                return default;
            var sources = state.HoverRangeSourceCells;
            var order = FieldHighlightLayers.DrawOrder;
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i].Y * state.ColumnCount + sources[i].X != state.HoverIndex)
                    continue;
                for (int j = 0; j < order.Count; j++)
                    if (FieldHighlightLayers.IsHoverRange(order[j]) && state.HasAnyCell(order[j]))
                        return Of(state, order[j], cellSize);
            }
            return (state.LayersAt(state.HoverIndex) & FieldHighlightLayer.ActiveActor) != 0
                ? Of(state, FieldHighlightLayer.ActiveActor, cellSize) : default;
        }
    }
}
