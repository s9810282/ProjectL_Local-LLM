using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;
using UnityEngine.Serialization;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>튜토리얼 UI 한 곳에서 소유하는 점선·모델 강조 표현 값입니다.</summary>
    [Serializable]
    internal sealed class TutorialHighlightStyle
    {
        [Tooltip("튜토리얼이 보이게 둔 UI 영역 바깥의 기존 점선 색입니다.")]
        [FormerlySerializedAs("color")]
        [SerializeField] private Color uiBorderColor = Color.white;
        [Tooltip("각 카드·버튼 자체의 가장자리에 추가하는 점선 색입니다.")]
        [SerializeField] private Color targetBorderColor = new Color(1f, 0.8f, 0f, 1f);
        [SerializeField, Min(0.1f)] private float targetBorderThickness = 5f;
        [Tooltip("실습 중의 노란 대상 점선만 부드럽게 깜빡입니다.")]
        [SerializeField] private bool blinkEnabled = true;
        [SerializeField, Min(0.1f)] private float blinkPeriod = 1.2f;
        [SerializeField, Range(0.01f, 1f)] private float blinkMinAlpha = 0.35f;
        [Tooltip("강조 대상 아군과 메인 캐릭터의 아웃라인 색입니다.")]
        [SerializeField] private Color allyOutlineColor = new Color(57f / 255f, 197f / 255f, 1f, 1f);
        [Tooltip("강조 대상 적과 적 넥서스의 아웃라인 색입니다.")]
        [SerializeField] private Color enemyOutlineColor = new Color(1f, 90f / 255f, 79f / 255f, 1f);
        [SerializeField, Min(0.1f)] private float borderThickness = 4f;
        [SerializeField, Min(0.1f)] private float dashLength = 16f;
        [SerializeField, Min(0.1f)] private float dashGap = 8f;
        [SerializeField, Min(0.0001f)] private float modelWidth = 0.04f;

        internal Color UiBorderColor => uiBorderColor;
        internal Color TargetBorderColor => targetBorderColor;
        internal float TargetBorderThickness => targetBorderThickness;
        internal bool BlinkEnabled => blinkEnabled;
        internal float BlinkPeriod => blinkPeriod;
        internal float BlinkMinAlpha => blinkMinAlpha;
        internal Color AllyOutlineColor => allyOutlineColor;
        internal Color EnemyOutlineColor => enemyOutlineColor;
        internal float BorderThickness => borderThickness;
        internal float DashLength => dashLength;
        internal float DashGap => dashGap;
        internal float ModelWidth => modelWidth;

        internal Color GetModelColor(Owner owner)
        {
            return owner switch
            {
                Owner.Player => AllyOutlineColor,
                Owner.Enemy => EnemyOutlineColor,
                _ => throw new InvalidOperationException($"[UIId.Tutorial] 모델 강조를 지원하지 않는 진영입니다: {owner}")
            };
        }

        internal void Validate()
        {
            ValidateColor(uiBorderColor, nameof(uiBorderColor));
            ValidateColor(targetBorderColor, nameof(targetBorderColor));
            ValidatePositive(targetBorderThickness, nameof(targetBorderThickness));
            ValidatePositive(blinkPeriod, nameof(blinkPeriod));
            if (!IsFinite(blinkMinAlpha) || blinkMinAlpha <= 0f || blinkMinAlpha > 1f)
                throw new InvalidOperationException("[UIId.Tutorial] highlightStyle.blinkMinAlpha는 0 초과 1 이하의 유한한 값이어야 합니다.");
            ValidateColor(allyOutlineColor, nameof(allyOutlineColor));
            ValidateColor(enemyOutlineColor, nameof(enemyOutlineColor));
            ValidatePositive(borderThickness, nameof(borderThickness));
            ValidatePositive(dashLength, nameof(dashLength));
            ValidatePositive(dashGap, nameof(dashGap));
            ValidatePositive(dashLength + dashGap, "dashLength + dashGap");
            ValidatePositive(modelWidth, nameof(modelWidth));
        }

        private static void ValidateColor(Color color, string field)
        {
            if (!IsFinite(color.r) || !IsFinite(color.g) || !IsFinite(color.b)
                || !IsFinite(color.a) || color.a <= 0f || color.a > 1f)
                throw new InvalidOperationException($"[UIId.Tutorial] highlightStyle.{field}는 유효한 색과 0 초과 1 이하 알파가 필요합니다.");
        }

        private static void ValidatePositive(float value, string field)
        {
            if (!IsFinite(value) || value <= 0f)
                throw new InvalidOperationException($"[UIId.Tutorial] highlightStyle.{field}는 유한한 양수여야 합니다.");
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
