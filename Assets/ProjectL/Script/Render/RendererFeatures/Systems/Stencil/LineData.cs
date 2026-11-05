using System;
using UnityEngine;

namespace ProjectL.Render.RendererFeatures.Systems.Stencil
{
    /// <summary>스텐실 경계선의 표현 프리셋이다.</summary>
    [Serializable]
    public struct LineData
    {
        [SerializeField, Min(0f), Tooltip("텍스처를 표시하는 외곽 띠의 월드 두께다.")]
        private float lineThickness;
        [SerializeField, ColorUsage(true, true)] private Color lineColor;
        [SerializeField] private Texture2D lineNoiseTexture;
        [SerializeField]
        [Tooltip("왜곡 0에서는 일반 2D UV에 X/Y를 그대로 곱한다. 한 축을 0으로 두면 텍스처가 그 축의 한 행/열로 줄어든다. 원형 UV의 X는 원 둘레 반복 횟수(최소 1의 정수), Y는 반경의 월드 단위당 반복 횟수이며 절댓값을 사용한다.")]
        private Vector2 lineNoiseTiling;
        [SerializeField]
        [Tooltip("원형 UV의 초당 노이즈 이동량이다. 양수는 바깥으로, 음수는 안으로 흐르고 0은 정지한다. 왜곡 0은 원래 UV를 그대로 읽는다.")]
        private float lineNoiseSpeed;
        [SerializeField, Min(0f)]
        [Tooltip("0은 일반 2D UV와 X/Y 타일링으로 텍스처를 읽고, 1은 기존 원형 UV와 경계 변형을 적용한다. 더 큰 값은 경계 변형을 증폭한다.")]
        private float lineDistortion;

        public float LineThickness => lineThickness;
        public Color LineColor => lineColor;
        public Texture2D LineNoiseTexture => lineNoiseTexture;
        public Vector2 LineNoiseTiling => lineNoiseTiling;
        public float LineNoiseSpeed => lineNoiseSpeed;
        public float LineDistortion => lineDistortion;

        public LineData(float thickness, Color color, Texture2D noiseTexture, Vector2 noiseTiling, float noiseSpeed, float distortion = 1f)
        {
            lineThickness = thickness;
            lineColor = color;
            lineNoiseTexture = noiseTexture;
            lineNoiseTiling = noiseTiling;
            lineNoiseSpeed = noiseSpeed;
            lineDistortion = distortion;
        }
    }
}
