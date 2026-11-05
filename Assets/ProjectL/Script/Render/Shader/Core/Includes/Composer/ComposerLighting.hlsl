#ifndef PROJECTL_COMPOSER_LIGHTING_INCLUDED
#define PROJECTL_COMPOSER_LIGHTING_INCLUDED

#include "ComposerTypes.hlsl"
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Lightng/LitLighting.hlsl"
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Shadow/ShadowUtility.hlsl"

InputData ProjectLComposerInputData(ProjectLComposerContext context)
{
    InputData data = (InputData)0;
    data.positionWS = context.positionWS;
    data.normalWS = context.normalWS;
    data.viewDirectionWS = context.viewWS;
    data.normalizedScreenSpaceUV = context.screenUV;
    data.shadowCoord = ProjectLReceiverShadowCoord(context.positionWS);
    data.bakedGI = context.bakedGI;
    data.vertexLighting = context.vertexLighting;
    data.shadowMask = context.shadowMask;
    return data;
}

#include "NearestStepLighting.hlsl"

#if defined(PROJECTL_COMPOSER_SCENE_DEPTH)
#include "DepthHalfLambertLighting.hlsl"
#endif

// 실제 광원의 색과 강도를 쓰며 환경광과 재질의 색은 마스크에 포함하지 않는다.
float ProjectLComposerLightBrightness(Light light, float3 normalWS)
{
    float luminance = dot(max(light.color, 0.0), float3(0.2126, 0.7152, 0.0722));
    return luminance * saturate(dot(normalWS, light.direction)) *
        light.distanceAttenuation * light.shadowAttenuation;
}

float ProjectLComposerDirectBrightness(ProjectLComposerContext context)
{
    InputData inputData = ProjectLComposerInputData(context);
    uint meshLayers = GetMeshRenderingLayer();
    Light mainLight = GetMainLight(inputData.shadowCoord, context.positionWS, context.shadowMask);
    float brightness = 0;
    if (IsMatchingLightLayer(mainLight.layerMask, meshLayers))
        brightness += ProjectLComposerLightBrightness(mainLight, context.normalWS);

#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_FORWARD_PLUS
#if USE_FORWARD_PLUS
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (IsMatchingLightLayer(light.layerMask, meshLayers))
            brightness += ProjectLComposerLightBrightness(light, context.normalWS);
    }
#endif
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (IsMatchingLightLayer(light.layerMask, meshLayers))
            brightness += ProjectLComposerLightBrightness(light, context.normalWS);
    LIGHT_LOOP_END
#endif
    return brightness;
}

half3 ProjectLComposerHalfLambertLight(Light light, float3 normalWS, half power)
{
    half diffuse = pow(saturate(dot(normalWS, light.direction) * 0.5 + 0.5), max(power, 0.0001h));
    return light.color * (diffuse * light.distanceAttenuation * light.shadowAttenuation);
}

half3 ProjectLComposerHalfLambert(ProjectLComposerContext context, half3 albedo, half power, half ambientStrength)
{
    InputData inputData = ProjectLComposerInputData(context);
    Light mainLight = GetMainLight(inputData.shadowCoord, context.positionWS, context.shadowMask);
    half3 bakedGI = context.bakedGI;
    MixRealtimeAndBakedGI(mainLight, context.normalWS, bakedGI);
    uint meshLayers = GetMeshRenderingLayer();
    half3 direct = 0;
    if (IsMatchingLightLayer(mainLight.layerMask, meshLayers))
        direct += ProjectLComposerHalfLambertLight(mainLight, context.normalWS, power);

#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_FORWARD_PLUS
#if USE_FORWARD_PLUS
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (IsMatchingLightLayer(light.layerMask, meshLayers))
            direct += ProjectLComposerHalfLambertLight(light, context.normalWS, power);
    }
#endif
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (IsMatchingLightLayer(light.layerMask, meshLayers))
            direct += ProjectLComposerHalfLambertLight(light, context.normalWS, power);
    LIGHT_LOOP_END
#endif
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(context.screenUV);
    return albedo * (direct * ao.directAmbientOcclusion + bakedGI * ambientStrength * ao.indirectAmbientOcclusion);
}

half3 ProjectLComposerLit(ProjectLComposerContext context, half3 albedo, half metallic, half smoothness)
{
    SurfaceData surface = (SurfaceData)0;
    surface.albedo = albedo;
    surface.metallic = metallic;
    surface.smoothness = smoothness;
    surface.normalTS = half3(0, 0, 1);
    surface.occlusion = 1;
    surface.alpha = 1;
    InputData inputData = ProjectLComposerInputData(context);
    return UniversalFragmentPBR(inputData, surface).rgb;
}

#include "PbrStepLighting.hlsl"

half3 ProjectLComposerToonDirect(ProjectLComposerContext context, Light mainLight,
    float3 diffuseNormalWS, half3 albedo, half3 F0, half power,
    half3 specularColor, half specularIntensity, half3 rimColor, half4 settings, bool useLightLayers)
{
    InputData inputData = ProjectLComposerInputData(context);
    half3 color = 0;
    uint meshLayers = GetMeshRenderingLayer();
    if (!useLightLayers || IsMatchingLightLayer(mainLight.layerMask, meshLayers))
        color += ProjectLToonDirectLight(mainLight, context.normalWS, diffuseNormalWS,
            context.viewWS, albedo, F0, power, specularColor, specularIntensity, rimColor, settings);

#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_FORWARD_PLUS
#if USE_FORWARD_PLUS
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (!useLightLayers || IsMatchingLightLayer(light.layerMask, meshLayers))
            color += ProjectLToonDirectLight(light, context.normalWS, diffuseNormalWS,
                context.viewWS, albedo, F0, power, specularColor, specularIntensity, rimColor, settings);
    }
#endif
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (!useLightLayers || IsMatchingLightLayer(light.layerMask, meshLayers))
            color += ProjectLToonDirectLight(light, context.normalWS, diffuseNormalWS,
                context.viewWS, albedo, F0, power, specularColor, specularIntensity, rimColor, settings);
    LIGHT_LOOP_END
#endif
    return color;
}

half3 ProjectLComposerToon(ProjectLComposerContext context, half3 albedo,
    half metallic, half smoothness, half3 specularColor, half specularIntensity,
    half steps, half darkStrength, half softness, half normalMultiplier, half ambientStrength)
{
    InputData inputData = ProjectLComposerInputData(context);
    Light mainLight = GetMainLight(inputData.shadowCoord, context.positionWS, context.shadowMask);
    half3 bakedGI = context.bakedGI;
    MixRealtimeAndBakedGI(mainLight, context.normalWS, bakedGI);
    half3 color = ProjectLComposerToonDirect(context, mainLight, context.normalWS, albedo,
        lerp(0.04h.xxx, albedo, metallic), ProjectLSpecularPower(smoothness), specularColor,
        specularIntensity, 0.0h.xxx, half4(steps, darkStrength, softness, normalMultiplier), true);
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(context.screenUV);
    return color * ao.directAmbientOcclusion + bakedGI * albedo * ambientStrength * ao.indirectAmbientOcclusion;
}

// 옛 BaseShader의 평면 명암과 SH·직접광 림 계약을 보존한다.
half3 ProjectLComposerBaseLit(ProjectLComposerContext context, half3 albedo,
    half metallic, half smoothness, half3 specularColor, half specularIntensity,
    half steps, half darkStrength, half softness, half flatNormals, half ambientStrength,
    half3 rimColor, half rimIntensity, half rimPower)
{
    float3 diffuseNormalWS = ProjectLFlatDiffuseNormal(context.positionWS, context.normalWS, flatNormals);
    Light mainLight = GetMainLight(ProjectLReceiverShadowCoord(context.positionWS));
    half3 rim = ProjectLLitRim(context.normalWS, context.viewWS, rimColor, rimIntensity, rimPower);
    half3 color = ProjectLComposerToonDirect(context, mainLight, diffuseNormalWS, albedo,
        lerp(0.04h.xxx, albedo, metallic), ProjectLSpecularPower(smoothness), specularColor,
        specularIntensity, rim, half4(steps, darkStrength, softness, 1), false);
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(context.screenUV);
    return color * ao.directAmbientOcclusion +
        SampleSH(diffuseNormalWS) * albedo * ambientStrength * ao.indirectAmbientOcclusion;
}

#endif
