using System;
using UnityEngine;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Runtime
{
    /// <summary>그리기 대상에 의존하지 않는 연속 걷기 계산.</summary>
    public static class JourneyMotionMath
    {
        public readonly struct HoofPose
        {
            public HoofPose(float offset, float lift, float roll)
            {
                Offset = offset;
                Lift = lift;
                Roll = roll;
            }

            public float Offset { get; }
            public float Lift { get; }
            public float Roll { get; }
        }

        public static float Fraction(double value) => (float)(value - Math.Floor(value));
        public static float Phase(JourneyMotionSettings settings, double time) => Fraction(time / settings.StrideDuration) * Mathf.PI * 2f;

        public static float Smooth(float start, float end, float value)
        {
            float t = Mathf.Clamp01((value - start) / (end - start));
            return t * t * (3f - 2f * t);
        }

        /// <summary>접지와 내딛기의 위치·속도가 연속인 발끝 궤적.</summary>
        public static HoofPose SampleHoof(JourneyMotionSettings settings, double time, float phaseOffset)
        {
            float p = Fraction(time / settings.StrideDuration + phaseOffset);
            float reach = settings.StrideReach;
            float stance = settings.StanceFraction;
            if (p < stance)
            {
                return new HoofPose(reach * (1f - 2f * p / stance), 0f, 0f);
            }
            float q = (p - stance) / (1f - stance);
            float ease = q * q * (3f - 2f * q);
            float tangent = -2f * reach * (1f - stance) / stance;
            float arc = Mathf.Sin(Mathf.PI * q);
            return new HoofPose(
                Mathf.Lerp(-reach, reach, ease) + tangent * q * (1f - q) * (1f - 2f * q),
                Mathf.Pow(Mathf.Max(arc, 0f), 1.5f) * settings.HoofLift,
                -arc * 0.11f);
        }
    }
}
