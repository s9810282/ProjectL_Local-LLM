#ifndef PROJECTL_TOON_DIFFUSE_INCLUDED
#define PROJECTL_TOON_DIFFUSE_INCLUDED

// 조립기 Toon 모델의 단계 범위와 맞춘 GPU 계산 상한이다.
#define PROJECTL_TOON_MAX_STEPS 20

// 기본은 모델의 노멀을 유지한다. 옵션을 켠 경우에만 삼각형 노멀을 재계산한다.
float3 ProjectLFlatDiffuseNormal(float3 positionWS, float3 normalWS, float flatAmount)
{
    if (flatAmount <= 0.0)
    {
        return normalize(normalWS);
    }
    float3 faceNormal = cross(ddx(positionWS), ddy(positionWS));
    float lengthSquared = dot(faceNormal, faceNormal);
    if (lengthSquared <= 0.000000000001)
    {
        return normalWS;
    }
    faceNormal *= rsqrt(lengthSquared);
    faceNormal *= dot(faceNormal, normalWS) < 0.0 ? -1.0 : 1.0;
    return normalize(lerp(normalWS, faceNormal, saturate(flatAmount)));
}

// 노멀과 광원의 내적 차이를 단계 판정 전에 배율만큼 확대한다.
float ProjectLNormalLightAmount(float NdotL, float multiplier)
{
    return saturate(NdotL * max(multiplier, 0.0));
}

// 부드러움이 0이면 명암 단계 사이의 중간색을 만들지 않는다.
half ProjectLToonDiffuse(float NdotL, float steps, float darkStrength, float softness)
{
    float count = clamp(floor(steps + 0.5), 2.0, float(PROJECTL_TOON_MAX_STEPS));
    float band = 0.0;
    float value = saturate(NdotL);
    if (softness <= 0.0)
    {
        // 면 노멀의 미분 오차가 같은 면을 두 단계로 나누지 않게 한다.
        // 모든 단계 경계를 격자 중심에 맞추고, 각 구간을 1024칸으로 나눈다.
        float precision = count * 1024.0;
        value = floor(value * precision + 0.5) / precision;
    }
    [unroll]
    for (int index = 1; index < PROJECTL_TOON_MAX_STEPS; ++index)
    {
        if (index < count)
        {
            float threshold = index / count;
            if (softness <= 0.0)
            {
                band += step(threshold, value);
            }
            else
            {
                float width = max(softness, 0.0001);
                band += smoothstep(threshold - width, threshold + width, value);
            }
        }
    }
    return lerp(saturate(darkStrength), 1.0h, band / (count - 1.0h));
}
#endif
