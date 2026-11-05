#ifndef PROJECTL_MASK_UTILITY_INCLUDED
#define PROJECTL_MASK_UTILITY_INCLUDED

// UV 중심을 지나는 직선 또는 평행 줄무늬. 0도는 수평, 90도는 수직이다.
// 폭은 전체 폭이며 간격은 선 중심 사이 거리다. 부드러움은 선 바깥의 페이드 폭이다.
float ProjectLLineMask(float2 uv, float mode, float angleDegrees, float offset,
    float width, float spacing, float softness)
{
    float sine, cosine;
    sincos(radians(angleDegrees), sine, cosine);
    float coordinate = dot(uv - 0.5, float2(-sine, cosine)) - offset;
    // frac 뒤에서 미분하면 반복 경계에서 과도하게 흐려지므로 원래 좌표를 사용한다.
    float antialias = max(fwidth(coordinate) * 0.5, 0.00001);
    float period = max(spacing, 0.0001);
    if (width <= 0.0) return 0.0;
    if (mode < 0.5)
    {
        if (width >= period) return 1.0;
        coordinate = (frac(coordinate / period + 0.5) - 0.5) * period;
    }
    float halfWidth = width * 0.5;
    return 1.0 - smoothstep(halfWidth - antialias,
        halfWidth + max(softness, 0.0) + antialias, abs(coordinate));
}

// 기준 이하의 빛은 차단한다. 기준 0에서도 무광 영역은 유지하지 않는다.
float ProjectLLightMask(float brightness, float threshold, float softness)
{
    threshold = max(threshold, 0.0);
    if (brightness <= threshold) return 0.0;
    if (softness <= 0.0) return 1.0;
    return smoothstep(threshold, threshold + max(softness, 0.00001), brightness);
}

// 노이즈를 유지 마스크로 바꾼다. 부드러움 0은 Step과 같다.
float ProjectLNoiseMask(float noise, float threshold, float softness)
{
    if (softness <= 0.0)
    {
        return step(threshold, noise);
    }
    float width = max(softness, 0.00001);
    return smoothstep(threshold - width, threshold + width, noise);
}

// 중앙 구간은 1이며 각 외곽에서 원하는 알파까지 선형 보간한다.
// 두께와 페이드 폭은 coordinate와 같은 단위를 사용한다.
float ProjectLLinearBandMask(float coordinate, float center, float thickness,
    float lowerFadeWidth, float upperFadeWidth, float lowerAlpha, float upperAlpha)
{
    float halfWidth = max(thickness, 0.0) * 0.5;
    float lowerDistance = center - halfWidth - coordinate;
    float upperDistance = coordinate - (center + halfWidth);
    float lowerT = lowerFadeWidth > 0.0
        ? saturate(lowerDistance / max(lowerFadeWidth, 0.00001))
        : (lowerDistance > 0.0 ? 1.0 : 0.0);
    float upperT = upperFadeWidth > 0.0
        ? saturate(upperDistance / max(upperFadeWidth, 0.00001))
        : (upperDistance > 0.0 ? 1.0 : 0.0);
    return min(lerp(1.0, saturate(lowerAlpha), lowerT),
        lerp(1.0, saturate(upperAlpha), upperT));
}
#endif
