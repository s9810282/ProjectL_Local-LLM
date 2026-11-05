using System;
using UnityEngine;

namespace ProjectL.Render.Shader.View.CustomShader.UI.Runtime
{
    /// <summary>足運びと路面・車輪の速度を共有する表示設定。</summary>
    [CreateAssetMenu(menuName = "ProjectL/Render/Journey Motion Settings")]
    public sealed class JourneyMotionSettings : ScriptableObject
    {
        [SerializeField, Min(0.1f)] private float strideDuration = 1.02f;
        [SerializeField, Range(1f, 65f)] private float strideReach = 36f;
        [SerializeField, Range(0.2f, 0.8f)] private float stanceFraction = 0.58f;
        [SerializeField, Range(0f, 60f)] private float hoofLift = 34f;
        [SerializeField, Range(0f, 12f)] private float suspension = 2.7f;
        [SerializeField, Range(0f, 0.06f)] private float driverLean = 0.018f;

        public float StrideDuration => strideDuration;
        public float StrideReach => strideReach;
        public float StanceFraction => stanceFraction;
        public float HoofLift => hoofLift;
        public float Suspension => suspension;
        public float DriverLean => driverLean;
        public float RoadSpeed => 2f * strideReach / stanceFraction / strideDuration * JourneySpriteLayout.HorseDisplayScale;

        /// <summary>잘못된 공유 설정을 재생 시작 전에 거부한다.</summary>
        public void ValidateConfiguration()
        {
            if (!Finite(strideDuration) || strideDuration < 0.1f || !Finite(strideReach) || strideReach <= 0f || strideReach > 65f ||
                !Finite(stanceFraction) || stanceFraction < 0.2f || stanceFraction > 0.8f ||
                !Finite(hoofLift) || hoofLift < 0f || hoofLift > 60f || !Finite(suspension) || suspension < 0f ||
                !Finite(driverLean) || driverLean < 0f)
            {
                throw new InvalidOperationException($"{name}: JourneyMotionSettings의 걷기 설정이 잘못됐다.");
            }
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
