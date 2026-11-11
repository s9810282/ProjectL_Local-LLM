using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 터지기 전 깜빡임 값입니다(자폭 깜빡임 설계 SD2·SD3). 모델 색이 이 색 쪽으로 바뀌었다가 원래 색으로 돌아오기를 횟수만큼 반복합니다.
    /// 색은 피격 플래시와 같은 외형 색 층으로 바꾸고 알파는 쓰지 않습니다(모델의 원래 알파를 따름).
    /// </summary>
    internal sealed class CombatDeathBlinkVisualData
    {
        /// <summary>최대 세기의 아래 끝입니다(0이면 보이지 않음). 인스펙터 범위와 검사가 함께 씁니다.</summary>
        internal const float MinPeakStrength = .05f;

        /// <summary>끝 회차 길이 비율의 아래 끝입니다. 인스펙터 범위와 검사가 함께 씁니다.</summary>
        internal const float MinLastPulseRatio = .1f;

        /// <param name="pulses">깜빡이는 횟수입니다. 1 이상입니다.</param>
        /// <param name="seconds">깜빡임 전체 시간(초)입니다. 0보다 커야 합니다.</param>
        /// <param name="peakStrength">회차마다 가장 붉을 때의 세기입니다(1 = 이 색 그대로). <see cref="MinPeakStrength"/>~1입니다.</param>
        /// <param name="lastPulseRatio">끝 회차 길이 ÷ 첫 회차 길이입니다. 1이면 같은 간격, 작을수록 점점 빨라집니다. <see cref="MinLastPulseRatio"/>~1입니다.</param>
        /// <param name="shape">한 회차의 세기 모양입니다.</param>
        /// <exception cref="ArgumentOutOfRangeException">값이 범위 밖일 때입니다.</exception>
        internal CombatDeathBlinkVisualData(Color color, int pulses, float seconds, float peakStrength, float lastPulseRatio, CombatFlashPulseShape shape)
        {
            if (pulses < 1)
                throw new ArgumentOutOfRangeException(nameof(pulses), pulses, "깜빡임 횟수는 1 이상이어야 합니다.");
            CombatPresentationValueRules.RequirePositive(seconds, nameof(seconds));
            if (float.IsNaN(peakStrength) || peakStrength < MinPeakStrength || peakStrength > 1f)
                throw new ArgumentOutOfRangeException(nameof(peakStrength), peakStrength, $"최대 세기는 {MinPeakStrength}~1이어야 합니다.");
            if (float.IsNaN(lastPulseRatio) || lastPulseRatio < MinLastPulseRatio || lastPulseRatio > 1f)
                throw new ArgumentOutOfRangeException(nameof(lastPulseRatio), lastPulseRatio, $"끝 회차 길이 비율은 {MinLastPulseRatio}~1이어야 합니다.");
            Color = color;
            Pulses = pulses;
            Seconds = seconds;
            PeakStrength = peakStrength;
            LastPulseRatio = lastPulseRatio;
            Shape = shape;
        }

        internal Color Color { get; }

        internal int Pulses { get; }

        internal float Seconds { get; }

        internal float PeakStrength { get; }

        internal float LastPulseRatio { get; }

        internal CombatFlashPulseShape Shape { get; }
    }
}
