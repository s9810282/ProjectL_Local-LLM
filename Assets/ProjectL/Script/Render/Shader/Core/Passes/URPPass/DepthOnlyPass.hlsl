#ifndef PROJECTL_DEPTH_ONLY_PASS_INCLUDED
#define PROJECTL_DEPTH_ONLY_PASS_INCLUDED

#include "Assets/ProjectL/Script/Render/Shader/Core/Passes/SurfaceInput.hlsl"

struct ProjectLDepthVaryings
{
    float4 positionCS : SV_POSITION;
#if defined(PROJECTL_DISSOLVE)
    float2 dissolveUV : TEXCOORD0;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

ProjectLDepthVaryings ProjectLDepthVert(ProjectLAttributes input)
{
    ProjectLDepthVaryings output = (ProjectLDepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
#if defined(PROJECTL_DISSOLVE)
    output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);
#endif
    return output;
}

half ProjectLDepthFrag(ProjectLDepthVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#if defined(PROJECTL_DISSOLVE)
    ProjectLApplySurfaceClip(input.dissolveUV);
#endif
    return input.positionCS.z;
}
#endif
