using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>터지기 전 깜빡임을 고르는 선택 입력입니다(자폭 깜빡임 설계 SD2). 꺼져 있으면 값이 없습니다.</summary>
    [Serializable]
    internal sealed class CombatDeathBlinkVisualInput
    {
        [Tooltip("자폭(SelfDestruct)의 SelfDestructExploded 줄 전용입니다. 켜면 터질 유닛이 사망 반응(쓰러짐·디졸브) 대신 이 색으로 깜빡이고, 자기 폭발 순간에 사라집니다.")]
        [SerializeField]
        private bool enabled;
        [Tooltip("모델이 이 색 쪽으로 바뀌었다가 원래 색으로 돌아옵니다(ColorSystem 손잡이, 텍스처·라이팅은 그대로). 알파는 쓰지 않습니다.")]
        [SerializeField]
        private Color color = Color.red;
        [Tooltip("깜빡이는 횟수입니다. 1 이상입니다.")]
        [SerializeField, Min(1)]
        private int pulses = 3;
        [Tooltip("깜빡임 전체 시간(초)입니다. 0보다 커야 합니다. 이 시간만큼 그 공격 뒤의 사건(폭발·주변 피해)이 늦어집니다.")]
        [SerializeField, Min(0f)]
        private float seconds = .9f;
        [Tooltip("회차마다 가장 붉을 때의 세기입니다. 1이면 이 색 그대로, 작을수록 원래 색이 섞여 옅게 붉어집니다.")]
        [SerializeField, Range(CombatDeathBlinkVisualData.MinPeakStrength, 1f)]
        private float peakStrength = 1f;
        [Tooltip("점점 빨라지기: 끝 회차 길이 ÷ 첫 회차 길이입니다. 1이면 같은 간격, 0.3이면 끝 회차가 첫 회차의 30% 길이입니다(전체 시간은 그대로).")]
        [SerializeField, Range(CombatDeathBlinkVisualData.MinLastPulseRatio, 1f)]
        private float lastPulseRatio = 1f;
        [Tooltip("한 회차의 세기 모양입니다. 가로 = 회차 안 시간(0~1), 세로 = 세기(0~1, 1 = 가장 붉음). 기본은 바로 붉어졌다가 일정하게 돌아오는 직선(1 → 0)이고, "
            + "예를 들어 0 → 1 → 0으로 그리면 서서히 붉어졌다가 돌아옵니다. 값은 0~1 안이어야 합니다.")]
        [SerializeField]
        private AnimationCurve pulseCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        /// <exception cref="ArgumentException">켜져 있는데 값·곡선이 맞지 않을 때입니다.</exception>
        internal CombatDeathBlinkVisualData ToData()
        {
            if (!enabled)
                return null;
            if (!CombatFlashPulseShape.TryFrom(pulseCurve, out CombatFlashPulseShape shape, out string error))
                throw new ArgumentException(error, nameof(pulseCurve));
            return new CombatDeathBlinkVisualData(color, pulses, seconds, peakStrength, lastPulseRatio, shape);
        }
    }
}
