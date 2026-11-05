using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 투사체 완급 곡선입니다(투사체 완급 설계 EZ1·EZ3). 시간 비율(0~1)을 같은 경로 위의 진행률(0~1)로 바꿉니다.
    /// 경로 모양과 비행 시간은 바꾸지 않고, 같은 길을 가는 빠르기만 나눕니다.
    /// </summary>
    /// <remarks>
    /// AnimationCurve는 바뀔 수 있는 참조 형식이라 만들 때 키를 복사해 감추고 <see cref="Evaluate"/>만 내줍니다.
    /// <c>default</c>는 등속입니다. 기본 곡선(0,0 → 1,1 직선)도 등속으로 읽어, 기본값이면 옛 등속과 같은 값을 냅니다.
    /// </remarks>
    internal readonly struct CombatProjectileEasing
    {
        // 곡선 검사 표본 수입니다(설계 Q-EZ3 A). 키 사이 접선이 표본 사이에서 조금 넘는 값은 실행 중 자르기가 맡습니다(설계 P3).
        private const int ValidationSamples = 64;
        // 끝 키·감소·범위 검사의 허용 오차입니다. 곡선 편집기에서 손으로 놓은 키를 받기 위한 값입니다.
        private const float Tolerance = .0001f;

        private readonly AnimationCurve _curve;

        private CombatProjectileEasing(AnimationCurve curve)
        {
            _curve = curve;
        }

        /// <summary>등속입니다.</summary>
        internal static CombatProjectileEasing Linear => default;

        /// <summary>
        /// 곡선을 검사하고 키를 복사해 만듭니다. 곡선 규칙의 원본은 이 입구 하나입니다(설계 Q-EZ3 A).
        /// 첫 키는 (0,0)·끝 키는 (1,1)이고, 값은 0~1 안에서 줄어들지 않아야 합니다. 맞는 순간이 "진행률이 접촉 지점을 한 번 넘는 순간"이기 때문입니다.
        /// 기본 곡선(0,0 → 1,1 직선)은 등속(<see cref="Linear"/>)으로 바꿉니다.
        /// </summary>
        /// <param name="error">규칙에 맞지 않을 때 그 이유입니다. 맞으면 null입니다.</param>
        /// <returns>규칙에 맞으면 true입니다. 키 배열은 한 번만 읽습니다.</returns>
        internal static bool TryFrom(AnimationCurve curve, out CombatProjectileEasing easing, out string error)
        {
            easing = Linear;
            if (curve == null)
            {
                error = "완급 곡선이 없습니다";
                return false;
            }
            Keyframe[] keys = curve.keys;
            error = FindError(curve, keys);
            if (error != null)
                return false;
            if (!IsIdentity(keys))
                easing = new CombatProjectileEasing(new AnimationCurve(keys));
            return true;
        }

        /// <summary>
        /// 시간 비율 <paramref name="t"/>의 경로 진행률입니다. 0 이하는 0, 1 이상은 정확히 1이라 곡사가 늘 목표에 떨어집니다.
        /// 사이 값은 0~1로 자릅니다(설계 P3).
        /// </summary>
        internal float Evaluate(float t)
        {
            if (t <= 0f)
                return 0f;
            if (t >= 1f)
                return 1f;
            return _curve == null ? t : Mathf.Clamp01(_curve.Evaluate(t));
        }

        private static string FindError(AnimationCurve curve, Keyframe[] keys)
        {
            if (keys.Length < 2)
                return "완급 곡선에는 키가 둘 이상 있어야 합니다";
            for (int i = 0; i < keys.Length; i++)
            {
                if (!IsFinite(keys[i].time) || !IsFinite(keys[i].value))
                    return $"완급 곡선 키 {i}의 값이 유한하지 않습니다";
            }
            if (!IsNear(keys[0].time, 0f) || !IsNear(keys[0].value, 0f))
                return $"완급 곡선의 첫 키는 (0, 0)이어야 합니다: ({keys[0].time}, {keys[0].value})";
            Keyframe last = keys[keys.Length - 1];
            if (!IsNear(last.time, 1f) || !IsNear(last.value, 1f))
                return $"완급 곡선의 끝 키는 (1, 1)이어야 합니다: ({last.time}, {last.value})";

            float previous = 0f;
            for (int i = 0; i <= ValidationSamples; i++)
            {
                float t = (float)i / ValidationSamples;
                float value = curve.Evaluate(t);
                if (!IsFinite(value))
                    return $"완급 곡선 값이 유한하지 않습니다(시간 {t})";
                if (value < -Tolerance || value > 1f + Tolerance)
                    return $"완급 곡선 값은 0~1 안이어야 합니다(시간 {t}, 값 {value})";
                if (value < previous - Tolerance)
                    return $"완급 곡선 값이 줄어들면 안 됩니다(시간 {t}, 값 {value} < {previous})";
                previous = Math.Max(previous, value);
            }
            return null;
        }

        // 기본 곡선(AnimationCurve.Linear(0, 0, 1, 1))인지입니다. 키가 정확히 그 값이면 등속으로 읽어 옛 값과 비트 단위로 같게 합니다.
        private static bool IsIdentity(Keyframe[] keys)
        {
            if (keys.Length != 2)
                return false;
            Keyframe first = keys[0];
            Keyframe last = keys[1];
            return first.time == 0f && first.value == 0f && first.outTangent == 1f && (first.weightedMode & WeightedMode.Out) == 0
                && last.time == 1f && last.value == 1f && last.inTangent == 1f && (last.weightedMode & WeightedMode.In) == 0;
        }

        private static bool IsNear(float value, float expected) => Math.Abs(value - expected) <= Tolerance;

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
