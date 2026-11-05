#ifndef PROJECTL_DEPTH_HALF_LAMBERT_LIGHTING_INCLUDED
#define PROJECTL_DEPTH_HALF_LAMBERT_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// 접평면에서 벗어난 정도만 사용해 경사진 평면 자체는 보정하지 않는다.
float2 ProjectLDepthPaletteResidual(float3 center, float3 faceNormal, float3 neighbor, float limit)
{
    float3 difference = neighbor - center;
    if (dot(difference, difference) > limit * limit)
    {
        return 0;
    }
    float normalDistance = dot(difference, faceNormal);
    float tangentDistance = length(difference - faceNormal * normalDistance);
    if (tangentDistance <= 0.00001)
    {
        return 0;
    }
    // 주변보다 솟은 중심은 양수, 들어간 중심은 음수가 된다.
    float residual = abs(normalDistance) <= max(0.00001, tangentDistance * 0.001)
        ? 0.0 : -normalDistance / tangentDistance;
    return float2(clamp(residual, -1.0, 1.0), 1.0);
}

bool ProjectLDepthPalettePosition(float2 uv, out float3 positionWS)
{
    positionWS = 0;
    if (any(uv < 0.0) || any(uv > 1.0))
    {
        return false;
    }
    float depth = SampleSceneDepth(uv);
#if UNITY_REVERSED_Z
    if (depth <= 0.0)
    {
        return false;
    }
#else
    if (depth >= 1.0)
    {
        return false;
    }
    depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, depth);
#endif
    // URP의 RT 경로에서 VP와 역 VP의 Y 반전이 다를 수 있어 행렬 곱으로 맞춘다.
    float4 inverseYColumn = float4(UNITY_MATRIX_I_VP._m01, UNITY_MATRIX_I_VP._m11,
        UNITY_MATRIX_I_VP._m21, UNITY_MATRIX_I_VP._m31);
    float2 reconstructionUV = uv;
    if (dot(UNITY_MATRIX_VP[1], inverseYColumn) < 0.0)
    {
        reconstructionUV.y = 1.0 - reconstructionUV.y;
    }
    // 역 VP 복원으로 직교와 원근 투영을 같은 경로에서 처리한다.
    positionWS = ComputeWorldSpacePosition(reconstructionUV, depth, UNITY_MATRIX_I_VP);
    return all(abs(positionWS) < 1.0e20);
}

float ProjectLDepthPaletteCorrection(ProjectLComposerContext context, float radiusPixels, float distanceLimit)
{
    // 미분은 깊이 유효성에 따른 픽셀별 분기 전에 평가한다.
    float3 faceNormal = ProjectLFlatDiffuseNormal(context.positionWS, context.normalWS, 1.0);
    float4 clipPosition = TransformWorldToHClip(context.positionWS);
    if (clipPosition.w <= 0.0 || any(_CameraDepthTexture_TexelSize.zw <= 1.0))
    {
        return 0;
    }
    float2 uv = clipPosition.xy / clipPosition.w * 0.5 + 0.5;
#if UNITY_UV_STARTS_AT_TOP
    uv.y = 1.0 - uv.y;
#endif
    float3 center;
    float limit = max(distanceLimit, 0.0001);
    if (!ProjectLDepthPalettePosition(uv, center))
    {
        return 0;
    }
    // 현재 표면이 없는 깊이(미생성/다른 카메라 등)는 보정에 사용하지 않는다.
    float centerTolerance = max(0.001, limit * 0.02);
    if (distance(center, context.positionWS) > centerTolerance)
    {
        return 0;
    }

    const float2 directions[8] =
    {
        float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1),
        float2(0.70710678, 0.70710678), float2(-0.70710678, 0.70710678),
        float2(0.70710678, -0.70710678), float2(-0.70710678, -0.70710678)
    };
    float2 accumulated = 0;
    float2 radius = _CameraDepthTexture_TexelSize.xy * clamp(radiusPixels, 1.0, 16.0);
    [unroll]
    for (int index = 0; index < 8; ++index)
    {
        float3 neighbor;
        if (ProjectLDepthPalettePosition(uv + directions[index] * radius, neighbor))
        {
            accumulated += ProjectLDepthPaletteResidual(context.positionWS, faceNormal, neighbor, limit);
        }
    }
    return accumulated.x / max(accumulated.y, 1.0);
}

float ProjectLDepthPaletteAmount(float normalLight, float power, float correction, float strength, float steps)
{
    float halfLambert = pow(saturate(normalLight * 0.5 + 0.5), max(power, 0.0001));
    float value = saturate(halfLambert + correction * strength);
    float intervals = clamp(floor(steps + 0.5), 2.0, 20.0) - 1.0;
    return floor(value * intervals + 0.5) / intervals;
}

struct ProjectLDepthPaletteSettings
{
    float power;
    float correction;
    float strength;
    float steps;
    half3 shade;
    half3 lower;
    half3 middle;
    half3 upper;
    half3 light;
};

half3 ProjectLDepthPaletteColor(float value, ProjectLDepthPaletteSettings settings)
{
    half3 color = lerp(settings.shade, settings.lower, smoothstep(0.0, 0.25, value));
    color = lerp(color, settings.middle, smoothstep(0.25, 0.5, value));
    color = lerp(color, settings.upper, smoothstep(0.5, 0.75, value));
    return lerp(color, settings.light, smoothstep(0.75, 1.0, value));
}

half3 ProjectLDepthPaletteDirectLight(Light light, float3 normalWS, half3 albedo,
    ProjectLDepthPaletteSettings settings)
{
    float value = ProjectLDepthPaletteAmount(dot(normalWS, light.direction), settings.power,
        settings.correction, settings.strength, settings.steps);
    return albedo * ProjectLDepthPaletteColor(value, settings) * light.color *
        (light.distanceAttenuation * light.shadowAttenuation);
}

bool ProjectLDepthPaletteMatchesLayer(Light light, uint meshLayers)
{
#if defined(_LIGHT_LAYERS)
    return IsMatchingLightLayer(light.layerMask, meshLayers);
#else
    return true;
#endif
}

half3 ProjectLComposerDepthHalfLambert(ProjectLComposerContext context, half3 albedo,
    float power, half ambientStrength, float steps, float radiusPixels, float depthStrength,
    float distanceLimit, half3 shade, half3 lower, half3 middle, half3 upper, half3 lightColor,
    float normalSmoothness)
{
    ProjectLDepthPaletteSettings settings;
    settings.power = power;
    settings.correction = 0;
    if (depthStrength > 0.0)
    {
        settings.correction = ProjectLDepthPaletteCorrection(context, radiusPixels, distanceLimit);
    }
    settings.strength = depthStrength;
    settings.steps = steps;
    settings.shade = shade;
    settings.lower = lower;
    settings.middle = middle;
    settings.upper = upper;
    settings.light = lightColor;

    // 깊이 보정의 접평면은 유지하고 이 모델의 라이팅 노멀만 부드럽게 혼합한다.
    context.normalWS = ProjectLFlatDiffuseNormal(context.positionWS, context.normalWS,
        1.0 - saturate(normalSmoothness));
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)
    // 라이트맵이 없으면 환경광도 같은 라이팅 노멀로 평가한다.
    context.bakedGI = SampleSH(context.normalWS);
#endif
    InputData inputData = ProjectLComposerInputData(context);
    Light mainLight = GetMainLight(inputData.shadowCoord, context.positionWS, context.shadowMask);
    half3 bakedGI = context.bakedGI;
    MixRealtimeAndBakedGI(mainLight, context.normalWS, bakedGI);
    uint meshLayers = GetMeshRenderingLayer();
    half3 direct = 0;
    if (ProjectLDepthPaletteMatchesLayer(mainLight, meshLayers))
    {
        direct += ProjectLDepthPaletteDirectLight(mainLight, context.normalWS, albedo, settings);
    }
#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_FORWARD_PLUS
#if USE_FORWARD_PLUS
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (ProjectLDepthPaletteMatchesLayer(light, meshLayers))
        {
            direct += ProjectLDepthPaletteDirectLight(light, context.normalWS, albedo, settings);
        }
    }
#endif
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (ProjectLDepthPaletteMatchesLayer(light, meshLayers))
        {
            direct += ProjectLDepthPaletteDirectLight(light, context.normalWS, albedo, settings);
        }
    LIGHT_LOOP_END
#endif
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(context.screenUV);
    return direct * ao.directAmbientOcclusion +
        albedo * bakedGI * ambientStrength * ao.indirectAmbientOcclusion;
}

// 스무스 입력이 없던 기존 생성 Shader는 면 노멀 기본값을 유지한다.
half3 ProjectLComposerDepthHalfLambert(ProjectLComposerContext context, half3 albedo,
    float power, half ambientStrength, float steps, float radiusPixels, float depthStrength,
    float distanceLimit, half3 shade, half3 lower, half3 middle, half3 upper, half3 lightColor)
{
    return ProjectLComposerDepthHalfLambert(context, albedo, power, ambientStrength, steps,
        radiusPixels, depthStrength, distanceLimit, shade, lower, middle, upper, lightColor, 0.0);
}

#endif
