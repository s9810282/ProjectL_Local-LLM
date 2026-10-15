#ifndef COMMON_UTILITY_Color_HLSLI
#define COMMON_UTILITY_Color_HLSLI

// 1. 그레이스케일(흑백) 변환
// 설명: RGB 컬러를 ITU-R 601-2 표준 가중치로 흑백 명도 값으로 바꿉니다.
float GetGrayscale(float3 color)
{
    return dot(color, float3(0.299, 0.587, 0.114));
}

// 2. 채도(Saturation) 조절
// 설명: 흑백(Grayscale) 이미지와 원본 컬러 사이의 비율을 조절해 채도를 높이거나 낮춥니다.
float3 AdjustSaturation(float3 color, float saturation)
{
    float grayscale = GetGrayscale(color);
    return lerp(float3(grayscale, grayscale, grayscale), color, saturation);
}

// 3. 감마 보정 (Gamma to Linear / Linear to Gamma)
// 설명: sRGB 컬러 공간과 선형(Linear) 컬러 공간 사이를 변환합니다.
float3 GammaToLinear(float3 color)
{
    return pow(max(color, 0.0), 2.2);
}

float3 LinearToGamma(float3 color)
{
    return pow(max(color, 0.0), 1.0 / 2.2);
}

// 4. 색상 반전 (Invert Color)
// 설명: 컬러 값을 반전시킵니다. (UI 효과나 특정 하이라이트에 쓰임)
float3 InvertColor(float3 color)
{
    return 1.0 - color;
}

#endif