using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 플래시 한 회차의 세기 모양입니다(자폭 깜빡임 설계 SD3). 회차 안의 시간 비율(0~1)을 세기(0~1)로 바꿉니다. 1이면 플래시 색, 0이면 원래 색입니다.
    /// </summary>
    /// <remarks>
    /// AnimationCurve는 바뀔 수 있는 참조 형식이라 만들 때 키를 복사해 감추고 <see cref="Evaluate"/>만 내줍니다(투사체 완급 곡선 <see cref="CombatProjectileEasing"/>과 같은 꼴).
    /// <c>default</c>는 직선(바로 1 → 일정하게 0)이고, 기본 곡선(1 → 0 직선)도 직선으로 읽어 옛 피격 플래시와 같은 값을 냅니다.
    /// </remarks>
    internal readonly struct CombatFlashPulseShape
    {
        // 곡선 검사 표본 수입니다. 키 사이 접선이 표본 사이에서 조금 넘는 값은 실행 중 자르기가 맡습니다.
        private const int ValidationSamples = 64;
        // 범위 검사의 허용 오차입니다. 곡선 편집기에서 손으로 놓은 키를 받기 위한 값입니다.
        private const float Tolerance = .0001f;

        private readonly AnimationCurve _curve;

        private CombatFlashPulseShape(AnimationCurve curve)
        {
            _curve = curve;
        }

        /// <summary>직선입니다: 회차 시작에 1(플래시 색), 끝으로 갈수록 일정하게 0(원래 색).</summary>
        internal static CombatFlashPulseShape Linear => default;

        /// <summary>
        /// 곡선을 검사하고 키를 복사해 만듭니다. 곡선 규칙의 원본은 이 입구 하나입니다. 키는 둘 이상, 값은 0~1 안이어야 합니다.
        /// 기본 곡선(1,0 시간에 값 1 → 1 시간에 값 0 직선)은 직선(<see cref="Linear"/>)으로 바꿉니다.
        /// </summary>
        /// <param name="error">규칙에 맞지 않을 때 그 이유입니다. 맞으면 null입니다.</param>
        internal static bool TryFrom(AnimationCurve curve, out CombatFlashPulseShape shape, out string error)
        {
            shape = Linear;
            if (curve == null)
            {
                error = "회차 곡선이 없습니다";
                return false;
            }
            Keyframe[] keys = curve.keys;
            error = FindError(curve, keys);
            if (error != null)
                return false;
            if (!IsDefault(keys))
                shape = new CombatFlashPulseShape(new AnimationCurve(keys));
            return true;
        }

        /// <summary>회차 안 시간 비율 <paramref name="u"/>(0~1로 자름)의 세기(0~1)입니다. 직선이면 정확히 1 − u입니다.</summary>
        internal float Evaluate(float u)
        {
            u = Mathf.Clamp01(u);
            return _curve == null ? 1f - u : Mathf.Clamp01(_curve.Evaluate(u));
        }

        private static string FindError(AnimationCurve curve, Keyframe[] keys)
        {
            if (keys.Length < 2)
                return "회차 곡선에는 키가 둘 이상 있어야 합니다";
            for (int i = 0; i < keys.Length; i++)
            {
                if (!IsFinite(keys[i].time) || !IsFinite(keys[i].value))
                    return $"회차 곡선 키 {i}의 값이 유한하지 않습니다";
            }
            for (int i = 0; i <= ValidationSamples; i++)
            {
                float t = (float)i / ValidationSamples;
                float value = curve.Evaluate(t);
                if (!IsFinite(value))
                    return $"회차 곡선 값이 유한하지 않습니다(시간 {t})";
                if (value < -Tolerance || value > 1f + Tolerance)
                    return $"회차 곡선 값은 0~1 안이어야 합니다(시간 {t}, 값 {value})";
            }
            return null;
        }

        // 기본 곡선(AnimationCurve.Linear(0, 1, 1, 0))인지입니다. 키가 정확히 그 값이면 직선으로 읽어 옛 값과 비트 단위로 같게 합니다.
        private static bool IsDefault(Keyframe[] keys)
        {
            if (keys.Length != 2)
                return false;
            Keyframe first = keys[0];
            Keyframe last = keys[1];
            return first.time == 0f && first.value == 1f && first.outTangent == -1f && (first.weightedMode & WeightedMode.Out) == 0
                && last.time == 1f && last.value == 0f && last.inTangent == -1f && (last.weightedMode & WeightedMode.In) == 0;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
