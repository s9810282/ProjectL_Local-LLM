using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 덮개 영역에서 구멍(보일 대상)을 뺀 나머지를 겹치지 않는 사각형들로 나눕니다(튜토리얼 설계 §15.4). 셰이더 없이 UI 메시로 그리기 위한 계산입니다.
    /// </summary>
    /// <remarks>
    /// 영역과 구멍의 모서리로 격자를 만들고, 어느 구멍에도 들지 않는 칸만 남긴 뒤 같은 줄의 이웃 칸을 합칩니다. 구멍은 영역 안으로 잘라 씁니다.
    /// </remarks>
    internal static class TutorialViewOverlayCells
    {
        /// <summary><paramref name="cells"/>를 비우고 덮을 칸을 채웁니다. 크기가 없는 구멍은 무시합니다.</summary>
        internal static void Build(Rect area, IReadOnlyList<Rect> holes, List<Rect> cells)
        {
            if (holes == null)
                throw new ArgumentNullException(nameof(holes));
            if (cells == null)
                throw new ArgumentNullException(nameof(cells));

            cells.Clear();
            if (area.width <= 0f || area.height <= 0f)
                return;

            var clipped = new List<Rect>(holes.Count);
            var xs = new List<float> { area.xMin, area.xMax };
            var ys = new List<float> { area.yMin, area.yMax };
            for (int i = 0; i < holes.Count; i++)
            {
                Rect hole = Clip(holes[i], area);
                if (hole.width <= 0f || hole.height <= 0f)
                    continue;
                clipped.Add(hole);
                xs.Add(hole.xMin);
                xs.Add(hole.xMax);
                ys.Add(hole.yMin);
                ys.Add(hole.yMax);
            }

            if (clipped.Count == 0)
            {
                cells.Add(area);
                return;
            }

            SortUnique(xs);
            SortUnique(ys);
            for (int y = 0; y < ys.Count - 1; y++)
            {
                float yMin = ys[y];
                float yMax = ys[y + 1];
                int runStart = -1;
                for (int x = 0; x < xs.Count - 1; x++)
                {
                    var center = new Vector2((xs[x] + xs[x + 1]) * 0.5f, (yMin + yMax) * 0.5f);
                    bool covered = !IsInsideAny(clipped, center);
                    if (covered && runStart < 0)
                        runStart = x;
                    if (!covered && runStart >= 0)
                    {
                        cells.Add(Rect.MinMaxRect(xs[runStart], yMin, xs[x], yMax));
                        runStart = -1;
                    }
                }

                if (runStart >= 0)
                    cells.Add(Rect.MinMaxRect(xs[runStart], yMin, xs[xs.Count - 1], yMax));
            }
        }

        private static Rect Clip(Rect rect, Rect area)
        {
            float xMin = Mathf.Max(rect.xMin, area.xMin);
            float yMin = Mathf.Max(rect.yMin, area.yMin);
            float xMax = Mathf.Min(rect.xMax, area.xMax);
            float yMax = Mathf.Min(rect.yMax, area.yMax);
            return xMax > xMin && yMax > yMin ? Rect.MinMaxRect(xMin, yMin, xMax, yMax) : Rect.zero;
        }

        private static bool IsInsideAny(List<Rect> holes, Vector2 point)
        {
            for (int i = 0; i < holes.Count; i++)
            {
                if (holes[i].Contains(point))
                    return true;
            }

            return false;
        }

        private static void SortUnique(List<float> values)
        {
            values.Sort();
            int write = 0;
            for (int read = 0; read < values.Count; read++)
            {
                if (write > 0 && Mathf.Approximately(values[write - 1], values[read]))
                    continue;
                values[write++] = values[read];
            }

            values.RemoveRange(write, values.Count - write);
        }
    }
}
