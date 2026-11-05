#ifndef PROJECTL_NEAREST_STEP_LIGHTING_INCLUDED
#define PROJECTL_NEAREST_STEP_LIGHTING_INCLUDED

// ComposerLighting의 공통 문맥/입력 변환을 연결한 뒤 포함한다.
// 0과 1을 포함하는 균등 표본 중 실제 내적에 가장 가까운 값을 고른다.
float ProjectLNearestStepAmount(float normalLight, float steps)
{
    float intervals = clamp(floor(steps + 0.5), 2.0, 20.0) - 1.0;
    return floor(saturate(normalLight) * intervals + 0.5) / intervals;
}

half3 ProjectLNearestStepDirectLight(Light light, float3 normalWS, half3 albedo, float steps)
{
    float amount = ProjectLNearestStepAmount(dot(normalWS, light.direction), steps);
    return albedo * light.color * amount *
        (light.distanceAttenuation * light.shadowAttenuation);
}

half3 ProjectLComposerNearestStep(ProjectLComposerContext context, half3 albedo,
    float steps, half ambientStrength)
{
    // 이 모델의 문맥 사본만 면 노멀로 바꿔 혼합하는 다른 모델에는 영향을 주지 않는다.
    context.normalWS = ProjectLFlatDiffuseNormal(context.positionWS, context.normalWS, 1.0);
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)
    // 보간된 정점 SH 대신 같은 면 노멀로 환경광도 다시 평가한다.
    context.bakedGI = SampleSH(context.normalWS);
#endif
    InputData inputData = ProjectLComposerInputData(context);
    Light mainLight = GetMainLight(inputData.shadowCoord, context.positionWS, context.shadowMask);
    half3 bakedGI = context.bakedGI;
    MixRealtimeAndBakedGI(mainLight, context.normalWS, bakedGI);
    uint meshLayers = GetMeshRenderingLayer();
    half3 direct = 0;
    if (IsMatchingLightLayer(mainLight.layerMask, meshLayers))
    {
        direct += ProjectLNearestStepDirectLight(mainLight, context.normalWS, albedo, steps);
    }

#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_FORWARD_PLUS
#if USE_FORWARD_PLUS
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (IsMatchingLightLayer(light.layerMask, meshLayers))
        {
            direct += ProjectLNearestStepDirectLight(light, context.normalWS, albedo, steps);
        }
    }
#endif
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, context.positionWS, context.shadowMask);
        if (IsMatchingLightLayer(light.layerMask, meshLayers))
        {
            direct += ProjectLNearestStepDirectLight(light, context.normalWS, albedo, steps);
        }
    LIGHT_LOOP_END
#endif

    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(context.screenUV);
    return direct * ao.directAmbientOcclusion +
        albedo * bakedGI * ambientStrength * ao.indirectAmbientOcclusion;
}

#endif
