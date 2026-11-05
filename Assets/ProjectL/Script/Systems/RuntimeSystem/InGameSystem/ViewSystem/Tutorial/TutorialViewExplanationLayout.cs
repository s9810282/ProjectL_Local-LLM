using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Tutorial.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>진행 상태를 읽지 않고 화면 영역과 설명 크기만으로 배치를 계산합니다.</summary>
    internal static class TutorialViewExplanationLayout
    {
        internal static Rect Place(Rect viewport, Vector2 size, Rect? anchor,
            TutorialViewExplanationPlacement preference, IReadOnlyList<Rect> obstacles, float gap, float margin)
        {
            if (obstacles == null)
                throw new ArgumentNullException(nameof(obstacles));
            margin = Mathf.Max(0f, margin);
            gap = Mathf.Max(0f, gap);
            float insetX = Mathf.Min(margin, Mathf.Max(0f, viewport.width) * 0.5f);
            float insetY = Mathf.Min(margin, Mathf.Max(0f, viewport.height) * 0.5f);
            var bounds = new Rect(viewport.xMin + insetX, viewport.yMin + insetY,
                Mathf.Max(0f, viewport.width - insetX * 2f), Mathf.Max(0f, viewport.height - insetY * 2f));
            size = new Vector2(Mathf.Clamp(size.x, 0f, bounds.width), Mathf.Clamp(size.y, 0f, bounds.height));
            Rect best = default;
            float bestOverlap = float.PositiveInfinity;
            int count = anchor.HasValue ? 4 : 9;
            for (int i = 0; i < count; i++)
            {
                Vector2 position;
                bool horizontal = false;
                if (anchor.HasValue)
                {
                    TutorialViewExplanationPlacement side = Side(preference, i);
                    horizontal = side == TutorialViewExplanationPlacement.Left || side == TutorialViewExplanationPlacement.Right;
                    Rect target = anchor.Value;
                    position = target.center - size * 0.5f;
                    switch (side)
                    {
                        case TutorialViewExplanationPlacement.Above: position.y = target.yMax + gap; break;
                        case TutorialViewExplanationPlacement.Below: position.y = target.yMin - gap - size.y; break;
                        case TutorialViewExplanationPlacement.Left: position.x = target.xMin - gap - size.x; break;
                        case TutorialViewExplanationPlacement.Right: position.x = target.xMax + gap; break;
                    }
                }
                else
                {
                    // 대상이 일시적으로 없으면 화면을 아홉 칸으로 나눠 빈 위치를 확인합니다.
                    int column = i % 3;
                    int row = i / 3;
                    position = new Vector2(bounds.xMin + (bounds.width - size.x) * column * 0.5f,
                        bounds.yMin + (bounds.height - size.y) * row * 0.5f);
                }
                var candidate = new Rect(Mathf.Clamp(position.x, bounds.xMin, bounds.xMax - size.x),
                    Mathf.Clamp(position.y, bounds.yMin, bounds.yMax - size.y), size.x, size.y);
                float overlap = anchor.HasValue ? IntersectionArea(candidate, anchor.Value) : 0f;
                for (int j = 0; j < obstacles.Count; j++)
                    overlap += IntersectionArea(candidate, obstacles[j]);
                if (overlap < bestOverlap)
                {
                    best = candidate;
                    bestOverlap = overlap;
                }
                // 옆 방향의 간격을 줄여야만 들어가는 후보는 정상 후보로 먼저 고르지 않습니다.
                bool sideFits = !anchor.HasValue || (horizontal ? candidate.x == position.x : candidate.y == position.y);
                if (overlap == 0f && sideFits)
                    return candidate;
            }
            return best;
        }

        private static TutorialViewExplanationPlacement Side(TutorialViewExplanationPlacement preferred, int index)
        {
            bool horizontal = preferred == TutorialViewExplanationPlacement.Left || preferred == TutorialViewExplanationPlacement.Right;
            if (preferred == TutorialViewExplanationPlacement.None)
                preferred = TutorialViewExplanationPlacement.Above;
            if (index == 0)
                return preferred;
            if (index == 1)
            {
                switch (preferred)
                {
                    case TutorialViewExplanationPlacement.Above: return TutorialViewExplanationPlacement.Below;
                    case TutorialViewExplanationPlacement.Below: return TutorialViewExplanationPlacement.Above;
                    case TutorialViewExplanationPlacement.Left: return TutorialViewExplanationPlacement.Right;
                    default: return TutorialViewExplanationPlacement.Left;
                }
            }
            if (horizontal)
                return index == 2 ? TutorialViewExplanationPlacement.Above : TutorialViewExplanationPlacement.Below;
            return index == 2 ? TutorialViewExplanationPlacement.Left : TutorialViewExplanationPlacement.Right;
        }

        private static float IntersectionArea(Rect first, Rect second)
        {
            return Mathf.Max(0f, Mathf.Min(first.xMax, second.xMax) - Mathf.Max(first.xMin, second.xMin)) *
                Mathf.Max(0f, Mathf.Min(first.yMax, second.yMax) - Mathf.Max(first.yMin, second.yMin));
        }
    }
}
