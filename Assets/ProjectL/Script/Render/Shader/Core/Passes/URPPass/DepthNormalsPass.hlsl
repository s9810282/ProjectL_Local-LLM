#ifndef PROJECTL_DEPTH_NORMALS_PASS_INCLUDED
#define PROJECTL_DEPTH_NORMALS_PASS_INCLUDED

#include "Assets/ProjectL/Script/Render/Shader/Core/Passes/SurfaceInput.hlsl"

struct ProjectLNormalVaryings
{
    float4 positionCS : SV_POSITION;
    half3 normalWS : TEXCOORD0;
#if defined(PROJECTL_DISSOLVE)
    float2 dissolveUV : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

ProjectLNormalVaryings ProjectLNormalVert(ProjectLAttributes input)
{
    ProjectLNormalVaryings output = (ProjectLNormalVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.normalWS = NormalizeNormalPerVertex(TransformObjectToWorldNormal(input.normalOS));
#if defined(PROJECTL_DISSOLVE)
    output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);
#endif
    return output;
}

void ProjectLPrepareNormalFragment(ProjectLNormalVaryings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#if defined(PROJECTL_DISSOLVE)
    ProjectLApplySurfaceClip(input.dissolveUV);
#endif
}

// URP 카메라 노멀은 부호 있는 월드 노멀 또는 옥타헤드럴 패킹을 사용한다.
void ProjectLDepthNormalsFrag(ProjectLNormalVaryings input,
    out half4 outNormalWS : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    ProjectLPrepareNormalFragment(input);
    float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormal = PackNormalOctQuadEncode(normalWS);
    outNormalWS = half4(PackFloat2To888(saturate(octNormal * 0.5 + 0.5)), 0.0);
#else
    outNormalWS = half4(normalWS, 0.0);
#endif
#if defined(_WRITE_RENDERING_LAYERS)
    outRenderingLayers = float4(EncodeMeshRenderingLayer(GetMeshRenderingLayer()), 0, 0, 0);
#endif
}

// 프로젝트의 NormalOnly 소비자는 [0, 1] 월드 노멀을 사용한다.
half4 ProjectLNormalOnlyFrag(ProjectLNormalVaryings input) : SV_Target
{
    ProjectLPrepareNormalFragment(input);
    return half4(normalize(input.normalWS) * 0.5h + 0.5h, 1.0h);
}
#endif
