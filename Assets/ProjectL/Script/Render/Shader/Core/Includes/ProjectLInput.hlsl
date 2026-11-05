#ifndef PROJECTL_SHADER_INPUT_INCLUDED
#define PROJECTL_SHADER_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_DissolveTex);
SAMPLER(sampler_DissolveTex);

// 모든 조립용 Pass에서 필드 순서와 타입을 동일하게 유지한다.
CBUFFER_START(UnityPerMaterial)
    float4 _Color;
    float4 _MainTex_ST;
    float4 _DissolveTex_ST;
    float4 _DissolveEdgeColor;
    float _DissolveAmount;
    float _DissolveEdgeWidth;
    float _DissolveEdgeIntensity;
    half _Metallic;
    half _Smoothness;
    half _DiffuseSteps;
    half _DiffuseDarkStrength;
    half _DiffuseSoftness;
    half _DiffuseNormalMultiplier;
    half _FlatDiffuseNormals;
    half _DiffuseAmbientStrength;
    half4 _SpecularColor;
    half _SpecularIntensity;
    half4 _RimColor;
    half _RimIntensity;
    half _RimPower;
CBUFFER_END

struct ProjectLAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
#endif
