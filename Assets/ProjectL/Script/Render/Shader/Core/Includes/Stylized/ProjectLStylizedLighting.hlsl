#ifndef PROJECTL_STYLIZED_LIGHTING_INCLUDED
#define PROJECTL_STYLIZED_LIGHTING_INCLUDED
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Shadow/ShadowUtility.hlsl"

bool ProjectLStylizedMatchesLightLayer(Light light, uint meshLayers)
{
#if defined(_LIGHT_LAYERS)
    return IsMatchingLightLayer(light.layerMask, meshLayers);
#else
    return true;
#endif
}

half3 ProjectLStylizedDirect(Light light, ProjectLStylizedSurface surface)
{
    float normalLight = dot(surface.normalWS, light.direction);
    float diffuse = ProjectLStylizedDiffuseAmount(normalLight);
    half3 diffuseColor = surface.albedo * lerp(_ShadeColor.rgb, half3(1, 1, 1), diffuse);
    half3 halfDirection = SafeNormalize(light.direction + surface.viewWS);
    float specular = pow(saturate(dot(surface.normalWS, halfDirection)), max(_SpecularPower, 1));
    specular = ProjectLStylizedStep(_SpecularThreshold, _SpecularSoftness, specular) * saturate(normalLight);

    float viewEdge = pow(1.0 - saturate(dot(surface.normalWS, surface.viewWS)), max(_RimPower, 0.01));
    float rim = ProjectLStylizedStep(1.0 - _RimWidth, _RimSoftness, viewEdge);
    // 시작점/부드러움 조절로도 음의 광원 반구에 림이 새지 않게 한다.
    float litSide = normalLight > 0 ? ProjectLStylizedStep(_RimLightThreshold, _RimLightSoftness, normalLight) : 0;
    litSide = pow(saturate(litSide * saturate(normalLight)), max(_RimLightPower, 0.01));
    half3 highlights = surface.specularMask * _SpecularColor.rgb * (_SpecularIntensity * specular) +
        _RimColor.rgb * (_RimIntensity * rim * litSide);
    float shadow = lerp(1.0, light.shadowAttenuation, saturate(_ReceiveShadows));
    return (diffuseColor * _DirectStrength + highlights) * light.color * (light.distanceAttenuation * shadow);
}

half3 ProjectLStylizedLighting(InputData inputData, ProjectLStylizedSurface surface, half occlusion)
{
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask);
    half3 bakedGI = inputData.bakedGI;
    MixRealtimeAndBakedGI(mainLight, surface.normalWS, bakedGI);
    uint meshLayers = GetMeshRenderingLayer();
    half3 direct = 0;
    if (ProjectLStylizedMatchesLightLayer(mainLight, meshLayers)) direct += ProjectLStylizedDirect(mainLight, surface);
#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_FORWARD_PLUS
#if USE_FORWARD_PLUS
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
        if (ProjectLStylizedMatchesLightLayer(light, meshLayers)) direct += ProjectLStylizedDirect(light, surface);
    }
#endif
    // 카툰 단계와 광원 방향 림을 유지하려고 정점광 설정에서도 개별 광원을 평가한다.
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, inputData.positionWS, inputData.shadowMask);
        if (ProjectLStylizedMatchesLightLayer(light, meshLayers)) direct += ProjectLStylizedDirect(light, surface);
    LIGHT_LOOP_END
#endif
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
    return direct * ao.directAmbientOcclusion + surface.albedo * bakedGI *
        (_AmbientStrength * occlusion * ao.indirectAmbientOcclusion);
}
#endif
