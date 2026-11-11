using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>화면 픽셀 영역을 Overlay Canvas 좌표로 바꾸고 Canvas 단위 여백을 더합니다.</summary>
    internal static class TutorialFocusGeometry
    {
        internal static Rect ToLocal(RectTransform canvasRect, Rect screenRect, float padding)
        {
            if (canvasRect == null)
                throw new ArgumentNullException(nameof(canvasRect));

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenRect.min, null, out Vector2 min);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenRect.max, null, out Vector2 max);
            float margin = Mathf.Max(0f, padding);
            return Rect.MinMaxRect(min.x - margin, min.y - margin, max.x + margin, max.y + margin);
        }
    }
}
