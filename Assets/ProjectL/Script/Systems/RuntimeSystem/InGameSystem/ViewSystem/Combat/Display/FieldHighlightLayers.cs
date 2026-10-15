using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>칸 하나에 겹칠 수 있는 강조 층입니다. 비트로 함께 표시합니다.</summary>
    [Flags]
    internal enum FieldHighlightLayer : byte
    {
        None = 0,
        /// <summary>Core의 현재 행위자가 점유한 칸입니다.</summary>
        ActiveActor = 1 << 0,
        /// <summary>이동·공격 범위 응답의 칸입니다.</summary>
        Range = 1 << 1,
        /// <summary>호버한 공격 조준의 영향 칸입니다.</summary>
        Affected = 1 << 2,
        /// <summary>드래그로 선택한 이동 예정 경로입니다. Core 승인 여부와는 별개입니다.</summary>
        MovementDraft = 1 << 3,
        /// <summary>현재 조준의 전달 또는 파급을 막는 벽이다.</summary>
        BlockedWall = 1 << 4
    }

    /// <summary>칸에 실제로 칠할 색의 종류입니다.</summary>
    internal enum FieldHighlightColor
    {
        /// <summary>강조 없음(셀 기본 색).</summary>
        None = 0,
        Static = 1,
        ActiveActor = 2,
        Range = 3,
        Affected = 4,
        HoverNeutral = 5,
        HoverValid = 6,
        HoverInvalid = 7,
        MovementDraft = 8
    }

    /// <summary>
    /// 칸 강조 층의 우선순위입니다(설계 §21.1): 호버 > 차단 벽 > 영향 > 예정 경로 > 범위 > 현재 행위자 > 정적(구역·레일).
    /// 높은 층이 지워지면 다음 층 색으로 돌아갑니다. 상태를 갖지 않는 순수 계산입니다.
    /// </summary>
    internal static class FieldHighlightLayers
    {
        /// <summary>
        /// 호버·정적을 뺀 층의 우선순위입니다(낮은 것부터). 셀 색 선택과 아웃라인을 쌓는 높이가 이 순서 하나를 따릅니다.
        /// </summary>
        internal static IReadOnlyList<FieldHighlightLayer> DrawOrder { get; } = new[]
        {
            FieldHighlightLayer.ActiveActor,
            FieldHighlightLayer.Range,
            FieldHighlightLayer.MovementDraft,
            FieldHighlightLayer.Affected,
            FieldHighlightLayer.BlockedWall
        };

        internal static FieldHighlightColor Resolve(
            bool isHovered,
            CombatHoverState hoverState,
            FieldHighlightLayer layers,
            bool hasStaticColor)
        {
            if (isHovered)
                return HoverColor(hoverState);

            for (int i = DrawOrder.Count - 1; i >= 0; i--)
            {
                if ((layers & DrawOrder[i]) != 0)
                    return ColorOf(DrawOrder[i]);
            }

            return hasStaticColor ? FieldHighlightColor.Static : FieldHighlightColor.None;
        }

        internal static FieldHighlightColor HoverColor(CombatHoverState hoverState)
        {
            switch (hoverState)
            {
                case CombatHoverState.Valid:
                    return FieldHighlightColor.HoverValid;
                case CombatHoverState.Invalid:
                    return FieldHighlightColor.HoverInvalid;
                default:
                    return FieldHighlightColor.HoverNeutral;
            }
        }

        /// <summary>층 하나의 색 종류입니다. 차단 벽은 호버 불가와 같은 색을 씁니다.</summary>
        internal static FieldHighlightColor ColorOf(FieldHighlightLayer layer)
        {
            switch (layer)
            {
                case FieldHighlightLayer.BlockedWall: return FieldHighlightColor.HoverInvalid;
                case FieldHighlightLayer.Affected: return FieldHighlightColor.Affected;
                case FieldHighlightLayer.MovementDraft: return FieldHighlightColor.MovementDraft;
                case FieldHighlightLayer.Range: return FieldHighlightColor.Range;
                case FieldHighlightLayer.ActiveActor: return FieldHighlightColor.ActiveActor;
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, "단일 강조 층이 아닙니다.");
            }
        }
    }
}
