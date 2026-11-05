#ifndef PROJECTL_SHADOW_CASTER_PASS_INCLUDED
#define PROJECTL_SHADOW_CASTER_PASS_INCLUDED

#include "Assets/ProjectL/Script/Render/Shader/Core/Passes/SurfaceInput.hlsl"
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Shadow/ShadowUtility.hlsl"

// URP가 현재 그림자를 그리는 광원마다 설정한다.
float3 _LightDirection;
float3 _LightPosition;

struct ProjectLShadowVaryings
{
    float4 positionCS : SV_POSITION;
#if defined(PROJECTL_DISSOLVE)
    float2 dissolveUV : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

ProjectLShadowVaryings ProjectLShadowVert(ProjectLAttributes input)
{
    ProjectLShadowVaryings output = (ProjectLShadowVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
    float3 directionWS = ProjectLCasterLightDirection(positionWS, _LightDirection, _LightPosition);
    output.positionCS = ProjectLCasterShadowPosition(positionWS, normalWS, directionWS);
#if defined(PROJECTL_DISSOLVE)
    output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);
#endif
    return output;
}

half4 ProjectLShadowFrag(ProjectLShadowVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
#if defined(PROJECTL_DISSOLVE)
    ProjectLApplySurfaceClip(input.dissolveUV);
#endif
    return 0;
}
#endif
