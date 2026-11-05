#ifndef PROJECTL_SHADOW_UTILITY_INCLUDED
#define PROJECTL_SHADOW_UTILITY_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Forward에서 그림자를 받을 때 사용할 좌표다.
float4 ProjectLReceiverShadowCoord(float3 positionWS)
{
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    return ComputeScreenPos(TransformWorldToHClip(positionWS));
#else
    return TransformWorldToShadowCoord(positionWS);
#endif
}

// 실제 그림자 광원의 방향을 사용한다. 카메라의 주광원과 구분한다.
float3 ProjectLCasterLightDirection(float3 positionWS, float3 directionWS, float3 lightPositionWS)
{
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    return normalize(lightPositionWS - positionWS);
#else
    return directionWS;
#endif
}

float4 ProjectLCasterShadowPosition(float3 positionWS, float3 normalWS, float3 lightDirectionWS)
{
    float4 positionCS = TransformWorldToHClip(
        ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    return ApplyShadowClamping(positionCS);
}
#endif
