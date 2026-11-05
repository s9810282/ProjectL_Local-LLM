#ifndef PROJECTL_TOON_FORWARD_PASS_INCLUDED
#define PROJECTL_TOON_FORWARD_PASS_INCLUDED

#include "Assets/ProjectL/Script/Render/Shader/Core/Passes/SurfaceInput.hlsl"
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Lightng/LitLighting.hlsl"
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Shadow/ShadowUtility.hlsl"

struct ProjectLForwardVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float2 uv : TEXCOORD2;
#if defined(PROJECTL_DISSOLVE)
    float2 dissolveUV : TEXCOORD3;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

ProjectLForwardVaryings ProjectLForwardVert(ProjectLAttributes input)
{
    ProjectLForwardVaryings output = (ProjectLForwardVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = position.positionCS;
    output.positionWS = position.positionWS;
    output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
#if defined(PROJECTL_DISSOLVE)
    output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);
#endif
    return output;
}

half4 ProjectLForwardFrag(ProjectLForwardVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float edge = 0.0;
#if defined(PROJECTL_DISSOLVE)
    edge = ProjectLApplySurfaceClip(input.dissolveUV);
#endif
    half4 albedo = ProjectLReadAlbedo(input.uv);
    // FBX에 기록된 Sharp 경계의 분리 노멀과 Smooth 영역의 보간을 유지한다.
    float3 normalWS = normalize(input.normalWS);
    float3 diffuseNormalWS = normalWS;
    half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 rimColor = ProjectLLitRim(normalWS, viewWS, _RimColor.rgb, _RimIntensity, _RimPower);
    half3 F0 = lerp(0.04h.xxx, albedo.rgb, _Metallic);
    half power = ProjectLSpecularPower(_Smoothness);
    half4 settings = half4(_DiffuseSteps, _DiffuseDarkStrength, _DiffuseSoftness, _DiffuseNormalMultiplier);
    Light mainLight = GetMainLight(ProjectLReceiverShadowCoord(input.positionWS));
    half3 color = ProjectLToonDirectLight(mainLight, normalWS, diffuseNormalWS,
        viewWS, albedo.rgb, F0, power, _SpecularColor.rgb, _SpecularIntensity, rimColor, settings);
    float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);

#if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_FORWARD_PLUS
    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalizedScreenSpaceUV = screenUV;
    half4 shadowMask = half4(1, 1, 1, 1);
#if USE_FORWARD_PLUS
    UNITY_LOOP for (uint lightIndex = 0;
        lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
    {
        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
        color += ProjectLToonDirectLight(light, normalWS, diffuseNormalWS,
            viewWS, albedo.rgb, F0, power, _SpecularColor.rgb, _SpecularIntensity, rimColor, settings);
    }
#endif
    uint pixelLightCount = GetAdditionalLightsCount();
    LIGHT_LOOP_BEGIN(pixelLightCount)
        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
        color += ProjectLToonDirectLight(light, normalWS, diffuseNormalWS,
            viewWS, albedo.rgb, F0, power, _SpecularColor.rgb, _SpecularIntensity, rimColor, settings);
    LIGHT_LOOP_END
#endif
    AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(screenUV);
    color *= ao.directAmbientOcclusion;
    color += SampleSH(diffuseNormalWS) * albedo.rgb *
        (_DiffuseAmbientStrength * ao.indirectAmbientOcclusion);
#if defined(PROJECTL_DISSOLVE)
    color = lerp(color, _DissolveEdgeColor.rgb * _DissolveEdgeIntensity, edge);
#endif
    return half4(color, albedo.a);
}
#endif
