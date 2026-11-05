using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// GridOutline 칸 채우기 판과 칸 테두리 고리 모양 식의 원본입니다(겹친 강조 2단계 4판 RC12, 5판 RC15).
    /// 깊이 d(칸에 쌓인 채우기를 아래부터 센 번호) 판의 칸 크기 비율은 밑 층 크기 비율 × <c>overlapInsetScale</c>^d입니다.
    /// 밑 층 크기 비율은 칸마다 방식(<see cref="FieldCellOutlineStyle.EachCell"/>)이면 <c>cellOutlineScale</c>, 덩어리 방식이면 1입니다.
    /// 판과 테두리는 같은 모양이라 칸 채우기 부품(<see cref="FieldCellFillStack"/>)과 표시기(<see cref="FieldOutlineRenderer"/>)가 이 값 하나를 함께 씁니다.
    /// 깊이 0은 cellOutlineStyle, 깊이 1 이상은 overlapCellOutlineStyle을 따릅니다. 덩어리 방식이면 같은 종류 칸 묶음(덩어리)의 둘레를,
    /// 칸마다 방식이면 칸 하나의 둘레를 이 거리만큼 줄이고 볼록 모서리를 둥글게 합니다. 시작 크기는 기본 방식으로 정하고 연결 방식만 깊이로 나눕니다.
    /// View DB 값은 만들 때 한 번 읽습니다. 범위의 원본은 View DB입니다(<see cref="StageFieldViewData"/>).
    /// </summary>
    internal sealed class FieldCellShape
    {
        private readonly float _baseScale;
        private readonly float _insetScale;
        private readonly FieldCellOutlineStyle _baseStyle;
        private readonly FieldCellOutlineStyle _overlapStyle;
        // 깊이 0 테두리·판(Merged는 테두리만)과 깊이 1 이상 판·테두리의 볼록 모서리 반지름 비율(크기 짧은 반변 대비)입니다.
        private readonly float _outlineCornerRatio;
        private readonly float _overlapCornerRatio;

        internal FieldCellShape(StageFieldViewData config, FieldCellSpacing spacing)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            _baseStyle = config.CellOutlineStyle;
            _overlapStyle = config.OverlapCellOutlineStyle;
            CellSize = new Vector2(spacing.Width, spacing.Length);
            _baseScale = EachCellAt(0) ? config.CellOutlineScale : 1f;
            _insetScale = config.OverlapInsetScale;
            _outlineCornerRatio = config.OutlineCornerRadius;
            _overlapCornerRatio = config.OverlapCornerRadius;
        }

        /// <summary>이 깊이에서 칸마다 네 변과 판을 따로 그리는지입니다. 거짓이면 같은 종류 칸을 덩어리로 잇습니다.</summary>
        internal bool EachCellAt(int depth) =>
            (depth == 0 ? _baseStyle : _overlapStyle) == FieldCellOutlineStyle.EachCell;

        /// <summary>칸 크기입니다(x = 칸 간격 X, y = 칸 간격 Z).</summary>
        internal Vector2 CellSize { get; }

        /// <summary>같은 방식·크기 비율·반지름 비율이면 이 모양을 다시 씁니다. 칸 간격은 표시기가 따로 비교합니다.</summary>
        internal bool Uses(StageFieldViewData config) =>
            config.CellOutlineStyle == _baseStyle
            && config.OverlapCellOutlineStyle == _overlapStyle
            && (!EachCellAt(0) || config.CellOutlineScale == _baseScale)
            && config.OverlapInsetScale == _insetScale
            && config.OutlineCornerRadius == _outlineCornerRatio
            && config.OverlapCornerRadius == _overlapCornerRatio;

        /// <summary>모서리 반지름 식입니다: 크기(판 또는 칸)의 짧은 반변 × 비율.</summary>
        internal static float CornerRadiusFor(Vector2 size, float ratio) => Mathf.Min(size.x, size.y) * 0.5f * ratio;

        /// <summary>
        /// 깊이 판의 변을 안쪽으로 옮기는 거리(월드 길이) = 칸 × (1 − 크기 비율) / 2입니다. x는 세로(Z 방향) 변을 X로,
        /// y는 가로(X 방향) 변을 Z로 옮기는 거리입니다. 덩어리 방식의 깊이 0은 0(칸 경계 그대로)입니다.
        /// </summary>
        internal Vector2 InsetOf(int depth) => CellSize * ((1f - ScaleOf(depth)) * 0.5f);

        /// <summary>
        /// 테두리 고리의 볼록 모서리 반지름(월드 길이)입니다. 깊이 0은 그 크기(칸 × 밑 층 비율)의 <c>outlineCornerRadius</c>,
        /// 깊이 1 이상은 판(칸 × 크기 비율)의 <c>overlapCornerRadius</c>입니다. 칸마다 방식은 칸 하나가 고리 하나라 네 모서리가 다 볼록입니다.
        /// </summary>
        internal float OutlineRadiusOf(int depth) =>
            CornerRadiusFor(CellSize * ScaleOf(depth), depth == 0 ? _outlineCornerRatio : _overlapCornerRatio);

        /// <summary>
        /// 판의 볼록 모서리 반지름(월드 길이)입니다. 테두리와 같고, 덩어리 방식의 깊이 0(칸 전체 채움)만 늘 네모라 0입니다(3판 Q-RC11).
        /// </summary>
        internal float PlateRadiusOf(int depth) => depth == 0 && !EachCellAt(depth) ? 0f : OutlineRadiusOf(depth);

        // 깊이 판의 칸 크기 비율입니다.
        private float ScaleOf(int depth) => _baseScale * Mathf.Pow(_insetScale, depth);
    }
}
