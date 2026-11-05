using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 셀 중심 사이 거리입니다. 셀 프리팹 크기에 작은 틈과 DB 간격(StageFieldView의 cellPadding)을 더합니다
    /// (옛 FieldCellFootprint·FieldCellSpacing·FieldCellSpacingPolicy 이식).
    /// </summary>
    internal readonly struct FieldCellSpacing
    {
        // 셀이 맞닿아 보이지 않도록 작은 변의 1%를 틈으로 둔다(옛 값 그대로).
        private const float GapRatio = 0.01f;

        private FieldCellSpacing(float width, float length)
        {
            Width = width;
            Length = length;
        }

        internal float Width { get; }

        internal float Length { get; }

        /// <param name="footprintWidth">셀 프리팹의 X 크기</param>
        /// <param name="footprintLength">셀 프리팹의 Z 크기</param>
        internal static FieldCellSpacing Calculate(float footprintWidth, float footprintLength, float paddingX, float paddingY)
        {
            RequirePositive(footprintWidth, nameof(footprintWidth));
            RequirePositive(footprintLength, nameof(footprintLength));
            if (!float.IsFinite(paddingX) || paddingX < 0f)
                throw new ArgumentOutOfRangeException(nameof(paddingX));
            if (!float.IsFinite(paddingY) || paddingY < 0f)
                throw new ArgumentOutOfRangeException(nameof(paddingY));

            float gap = Math.Min(footprintWidth, footprintLength) * GapRatio;
            return new FieldCellSpacing(footprintWidth + gap + paddingX, footprintLength + gap + paddingY);
        }

        /// <summary>칸 중심 간격을 그대로 씁니다(GridOutline 모드, View DB의 outlineCellSpacing).</summary>
        internal static FieldCellSpacing FromCenterSpacing(float width, float length)
        {
            RequirePositive(width, nameof(width));
            RequirePositive(length, nameof(length));
            return new FieldCellSpacing(width, length);
        }

        /// <summary>
        /// GridOutline 모드 설정의 칸 간격입니다. 이 모드에서는 View DB의 칸 중심 간격이 곧 칸 간격이고, 그 규칙은 여기 한 곳입니다.
        /// 격자 아웃라인 표시기와 배경 바닥 타일 맞춤(필드 구성 전, 씬 로드 때)이 함께 부릅니다(타일 맞춤 계획 P10).
        /// </summary>
        /// <exception cref="InvalidOperationException">칸 프리팹 모드처럼 칸 프리팹을 재야 간격이 나오는 설정일 때입니다.</exception>
        internal static FieldCellSpacing FromGridOutline(StageFieldViewData config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (config.DisplayMode != FieldDisplayMode.GridOutline)
            {
                throw new InvalidOperationException(
                    $"[FieldCellSpacing] 설정만으로 칸 간격을 정할 수 있는 것은 GridOutline 모드뿐입니다: {config.DisplayMode}");
            }
            return FromCenterSpacing(config.OutlineCellSpacing.x, config.OutlineCellSpacing.y);
        }

        private static void RequirePositive(float value, string name)
        {
            if (!float.IsFinite(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
