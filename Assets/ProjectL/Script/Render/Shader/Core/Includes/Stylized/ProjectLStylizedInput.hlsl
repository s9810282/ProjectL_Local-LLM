#ifndef PROJECTL_STYLIZED_INPUT_INCLUDED
#define PROJECTL_STYLIZED_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
#if defined(PROJECTL_STYLIZED_DISSOLVE)
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Maske/DissolveUtility.hlsl"
#endif

// 모든 Pass가 동일한 머티리얼 입력을 공유한다. 텍스처는 기본 UV 변환을 함께 쓴다.
TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
TEXTURE2D(_SpecularMap); SAMPLER(sampler_SpecularMap);
#if defined(PROJECTL_STYLIZED_DISSOLVE)
TEXTURE2D(_DissolveTex); SAMPLER(sampler_DissolveTex);
#endif

CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half4 _ShadeColor;
    half4 _SpecularColor;
    half4 _RimColor;
    half4 _EmissionColor;
    half4 _OutlineColor;
    float _BumpScale;
    float _OcclusionStrength;
    float _EmissionStrength;
    float _AlphaClip;
    float _Cutoff;
    float _Cull;
    float _ReceiveShadows;
    float _AmbientStrength;
    float _DirectStrength;
    float _ShadingSteps;
    float _ShadeThreshold;
    float _ShadeSoftness;
    float _HalfLambertPower;
    float _SpecularIntensity;
    float _SpecularPower;
    float _SpecularThreshold;
    float _SpecularSoftness;
    float _RimIntensity;
    float _RimWidth;
    float _RimSoftness;
    float _RimPower;
    float _RimLightThreshold;
    float _RimLightSoftness;
    float _RimLightPower;
    float _OutlineWidth;
#if defined(PROJECTL_STYLIZED_DISSOLVE)
    half4 _DissolveEdgeColor;
    float _DissolveAmount;
    float _DissolveEdgeWidth;
    float _DissolveEdgeIntensity;
#endif
#if defined(PROJECTL_STYLIZED_SURFACE_ALPHA)
    float _Surface;
#endif
CBUFFER_END

half4 ProjectLStylizedBase(float2 uv)
{
    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;
    return color;
}

#if defined(PROJECTL_STYLIZED_DISSOLVE)
// 디졸브 텍스처는 기본 텍스처와 같은 UV 변환을 사용해 모든 Pass의 경계를 일치시킨다.
float2 ProjectLStylizedDissolveMask(float2 uv)
{
    float noise = 1.0;
    UNITY_BRANCH
    if (_DissolveAmount > 0.0 && _DissolveAmount < 1.0)
        noise = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, uv).r;
    return ProjectLDissolveMask(noise, _DissolveAmount, _DissolveEdgeWidth);
}
#endif

float ProjectLStylizedClip(float2 uv)
{
    if (_AlphaClip > 0.5) clip(ProjectLStylizedBase(uv).a - _Cutoff);
#if defined(PROJECTL_STYLIZED_DISSOLVE)
    float2 mask = ProjectLStylizedDissolveMask(uv);
    clip(mask.x - 0.5);
    return mask.y;
#else
    return 0.0;
#endif
}

half3 ProjectLStylizedEmission(float2 uv)
{
    return SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uv).rgb *
        _EmissionColor.rgb * _EmissionStrength;
}

half3 ProjectLStylizedNormal(float2 uv, float3 normalWS, float4 tangentWS)
{
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uv), _BumpScale);
    float3 n = normalize(normalWS);
    float3 t = SafeNormalize(tangentWS.xyz);
    float3 b = cross(n, t) * tangentWS.w;
    return SafeNormalize(t * normalTS.x + b * normalTS.y + n * normalTS.z);
}

struct ProjectLStylizedSurface
{
    half3 albedo;
    half3 normalWS;
    half3 viewWS;
    half3 specularMask;
};

// 부드러움 0은 단계 경계를 유지한다.
float ProjectLStylizedStep(float threshold, float width, float value)
{
    if (width <= 0) return step(threshold, value);
    return smoothstep(threshold - width, threshold + width, value);
}
#endif
