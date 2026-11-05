#ifndef PROJECTL_COMPOSER_TYPES_INCLUDED
#define PROJECTL_COMPOSER_TYPES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Maske/DissolveUtility.hlsl"
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Maske/MaskUtility.hlsl"

// 모든 모델과 효과가 같은 표면 문맥을 받는다. 머티리얼 변수는 생성 Shader가 소유한다.
struct ProjectLComposerContext
{
    float3 positionWS;
    float3 positionOS;
    float3 normalWS;
    float3 viewWS;
    float2 uv;
    float2 screenUV;
    half3 bakedGI;
    half3 vertexLighting;
    half4 shadowMask;
    float lightBrightness;
    float fog;
};

float ProjectLComposerHash(float2 position)
{
    return frac(sin(dot(position, float2(127.1, 311.7))) * 43758.5453);
}

float ProjectLComposerNoise(float2 uv)
{
    float2 cell = floor(uv);
    float2 f = frac(uv);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(ProjectLComposerHash(cell), ProjectLComposerHash(cell + float2(1, 0)), f.x),
        lerp(ProjectLComposerHash(cell + float2(0, 1)), ProjectLComposerHash(cell + float2(1, 1)), f.x), f.y);
}

float ProjectLComposerHeight(float height, float bottom, float top)
{
    return saturate((height - bottom) / max(top - bottom, 0.00001));
}

#endif
