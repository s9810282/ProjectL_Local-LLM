using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item
{
    /// <summary>노드 호버의 색상·두께·전환 시간입니다. 런타임 호버 상태는 보관하지 않습니다.</summary>
    internal sealed class StageNodeHoverStyleData
    {
        internal StageNodeHoverStyleData(Color availableOutlineColor, float availableOutlineWidth,
            Color unavailableOutlineColor, float unavailableOutlineWidth,
            Color connectionColor, float connectionWidth, float transitionDuration,
            Color idleOutlineColor, float idleOutlineWidth)
        {
            RequireColor(availableOutlineColor, nameof(availableOutlineColor));
            RequireColor(unavailableOutlineColor, nameof(unavailableOutlineColor));
            RequireColor(connectionColor, nameof(connectionColor));
            RequireColor(idleOutlineColor, nameof(idleOutlineColor));
            RequireNonnegative(idleOutlineWidth, nameof(idleOutlineWidth));
            RequireNonnegative(availableOutlineWidth, nameof(availableOutlineWidth));
            RequireNonnegative(unavailableOutlineWidth, nameof(unavailableOutlineWidth));
            RequireNonnegative(connectionWidth, nameof(connectionWidth));
            RequireNonnegative(transitionDuration, nameof(transitionDuration));
            if (connectionWidth == 0f)
                throw new InvalidOperationException("[StageNodeHoverStyle] Connection Width는 0보다 커야 합니다.");
            AvailableOutlineColor = availableOutlineColor;
            AvailableOutlineWidth = availableOutlineWidth;
            UnavailableOutlineColor = unavailableOutlineColor;
            UnavailableOutlineWidth = unavailableOutlineWidth;
            ConnectionColor = connectionColor;
            ConnectionWidth = connectionWidth;
            TransitionDuration = transitionDuration;
            IdleOutlineColor = idleOutlineColor;
            IdleOutlineWidth = idleOutlineWidth;
        }

        internal Color AvailableOutlineColor { get; }
        internal float AvailableOutlineWidth { get; }
        internal Color UnavailableOutlineColor { get; }
        internal float UnavailableOutlineWidth { get; }
        internal Color ConnectionColor { get; }
        internal float ConnectionWidth { get; }
        internal float TransitionDuration { get; }
        internal Color IdleOutlineColor { get; }
        internal float IdleOutlineWidth { get; }

        private static void RequireNonnegative(float value, string field)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new InvalidOperationException($"[StageNodeHoverStyle] {field}는 0 이상의 유한한 값이어야 합니다.");
        }

        private static void RequireColor(Color value, string field)
        {
            if (!float.IsFinite(value.r) || !float.IsFinite(value.g) ||
                !float.IsFinite(value.b) || !float.IsFinite(value.a))
                throw new InvalidOperationException($"[StageNodeHoverStyle] {field} 색상이 올바르지 않습니다.");
        }
    }
}
