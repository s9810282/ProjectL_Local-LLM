#ifndef PROJECTL_LIT_LIGHTING_INCLUDED
#define PROJECTL_LIT_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "ToonLighting.hlsl"

half ProjectLSpecularPower(half smoothness)
{
    half roughness = (1.0h - smoothness) * (1.0h - smoothness);
    return max(1.0h, 2.0h / max(roughness * roughness, 0.002h) - 2.0h);
}

half3 ProjectLLitRim(half3 normalWS, half3 viewWS, half3 color, half intensity, half power)
{
    half rim = pow(1.0h - saturate(dot(normalWS, viewWS)), max(power, 0.001h));
    return color * intensity * rim;
}

// 주광원과 추가광이 같은 직접광 계산을 사용한다.
// diffuseSettings는 명암 단계 수, 어두운 밝기, 경계 부드러움, 노멀 내적 배율 순서다.
half3 ProjectLToonDirectLight(Light light, half3 normalWS, float3 diffuseNormalWS,
    half3 viewWS, half3 albedo, half3 F0, half specularPower,
    half3 specularTint, half specularIntensity, half3 rimColor, half4 diffuseSettings)
{
    float normalLight = ProjectLNormalLightAmount(dot(diffuseNormalWS, light.direction), diffuseSettings.w);
    half diffuse = ProjectLToonDiffuse(normalLight,
        diffuseSettings.x, diffuseSettings.y, diffuseSettings.z);
    half3 halfDirection = SafeNormalize(light.direction + viewWS);
    half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower);
    half3 fresnel = F0 + (1.0h - F0) *
        pow(1.0h - saturate(dot(halfDirection, viewWS)), 5.0h);
    half litMask = saturate(dot(normalWS, light.direction));
    half3 reflection = specular * fresnel * litMask * specularTint * specularIntensity;
    return (albedo * diffuse + reflection + rimColor * litMask) * light.color *
        (light.distanceAttenuation * light.shadowAttenuation);
}
#endif
