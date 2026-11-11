using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 플래시의 회차 시간표입니다(자폭 깜빡임 설계 SD3). 전체 시간·횟수·끝 회차 길이 비율로 회차 길이를 나누고, 지금 시각의 세기(0~1)를 답합니다.
    /// 피격 플래시는 한 회차·직선·최대 1(<see cref="Single"/>)이고, 터지기 전 깜빡임은 깜빡임 값(<see cref="From"/>)입니다. 세기 계산의 원본은 이곳 하나입니다.
    /// </summary>
    /// <remarks>
    /// 회차 길이는 첫 회차에서 끝 회차까지 같은 비율로 줄어듭니다(끝 회차 = 첫 회차 × 끝 회차 길이 비율, 합 = 전체 시간). 비율 1이면 같은 간격입니다.
    /// 끝 직전 부동소수 경계에서 다음 회차(세기 최대)로 넘어가지 않게 회차 번호를 마지막 회차로 묶습니다(묶음 끝 검토 F4). 호출마다 할당하지 않습니다.
    /// </remarks>
    internal readonly struct CombatFlashPulses
    {
        private readonly float _seconds;
        private readonly int _pulses;
        private readonly float _lastPulseRatio;
        private readonly float _peak;
        private readonly CombatFlashPulseShape _shape;

        /// <exception cref="ArgumentOutOfRangeException">시간이 유한한 0 이상이 아니거나, 횟수가 1보다 작거나, 비율·세기가 (0, 1] 밖일 때입니다.</exception>
        internal CombatFlashPulses(float seconds, int pulses, float lastPulseRatio, float peak, CombatFlashPulseShape shape)
        {
            // 시간 조건은 설정 Data 한 곳을 씁니다(연출 조립 설계 §9 I-5). 0이면 시작 안에서 끝납니다(보이지 않음).
            CombatPresentationSettingsData.ValidateSeconds(seconds, nameof(seconds));
            if (pulses < 1)
                throw new ArgumentOutOfRangeException(nameof(pulses), pulses, "플래시 횟수는 1 이상이어야 합니다.");
            if (float.IsNaN(lastPulseRatio) || lastPulseRatio <= 0f || lastPulseRatio > 1f)
                throw new ArgumentOutOfRangeException(nameof(lastPulseRatio), lastPulseRatio, "끝 회차 길이 비율은 0보다 크고 1 이하여야 합니다.");
            if (float.IsNaN(peak) || peak <= 0f || peak > 1f)
                throw new ArgumentOutOfRangeException(nameof(peak), peak, "최대 세기는 0보다 크고 1 이하여야 합니다.");
            _seconds = seconds;
            _pulses = pulses;
            _lastPulseRatio = lastPulseRatio;
            _peak = peak;
            _shape = shape;
        }

        /// <summary>피격 플래시입니다: 한 회차, 바로 플래시 색 → 일정하게 원래 색, 최대 1(옛 피격 플래시와 같은 값).</summary>
        internal static CombatFlashPulses Single(float seconds) => new(seconds, 1, 1f, 1f, CombatFlashPulseShape.Linear);

        /// <summary>터지기 전 깜빡임입니다. 값 검사는 깜빡임 값 생성자가 이미 했습니다.</summary>
        internal static CombatFlashPulses From(CombatDeathBlinkVisualData blink)
        {
            if (blink == null) throw new ArgumentNullException(nameof(blink));
            return new CombatFlashPulses(blink.Seconds, blink.Pulses, blink.LastPulseRatio, blink.PeakStrength, blink.Shape);
        }

        /// <summary>전체 시간(초)입니다.</summary>
        internal float Duration => _seconds;

        /// <summary>시작부터 <paramref name="elapsed"/>초 지난 순간의 세기(0~1)입니다. 전체 시간이 지났으면 0(플래시 층 지움)입니다.</summary>
        internal float StrengthAt(float elapsed)
        {
            if (_seconds <= 0f || elapsed >= _seconds)
                return 0f;
            return _peak * _shape.Evaluate(PulseTime(Mathf.Max(0f, elapsed)));
        }

        // 지금 회차 안의 시간 비율(0~1)입니다.
        private float PulseTime(float elapsed)
        {
            if (_lastPulseRatio == 1f || _pulses == 1)
            {
                float pulse = _seconds / _pulses;
                int index = Mathf.Min((int)(elapsed / pulse), _pulses - 1);
                return Mathf.Clamp01((elapsed - index * pulse) / pulse);
            }
            // 회차마다 길이가 q배가 되는 등비 나눔입니다. q^(횟수 − 1) = 끝 회차 길이 비율.
            float q = Mathf.Pow(_lastPulseRatio, 1f / (_pulses - 1));
            float length = _seconds * (1f - q) / (1f - Mathf.Pow(q, _pulses));
            float start = 0f;
            for (int i = 0; i < _pulses - 1; i++)
            {
                if (elapsed < start + length)
                    return Mathf.Clamp01((elapsed - start) / length);
                start += length;
                length *= q;
            }
            return Mathf.Clamp01((elapsed - start) / length);
        }
    }
}
