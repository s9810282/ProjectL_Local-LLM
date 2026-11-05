#ifndef PROJECTL_META_PASS_INCLUDED
#define PROJECTL_META_PASS_INCLUDED

#include "Assets/ProjectL/Script/Render/Shader/Core/Passes/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct ProjectLMetaVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
#if defined(PROJECTL_DISSOLVE)
    float2 dissolveUV : TEXCOORD1;
#endif
};

ProjectLMetaVaryings ProjectLMetaVert(ProjectLAttributes input)
{
    ProjectLMetaVaryings output = (ProjectLMetaVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    output.positionCS = MetaVertexPosition(input.positionOS,
        input.lightmapUV, input.dynamicLightmapUV, unity_LightmapST, unity_DynamicLightmapST);
    output.uv = TRANSFORM_TEX(input.uv, _MainTex);
#if defined(PROJECTL_DISSOLVE)
    output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);
#endif
    return output;
}

half4 ProjectLMetaFrag(ProjectLMetaVaryings input) : SV_Target
{
    float edge = 0.0;
#if defined(PROJECTL_DISSOLVE)
    edge = ProjectLApplySurfaceClip(input.dissolveUV);
#endif
    MetaInput meta = (MetaInput)0;
    meta.Albedo = ProjectLReadAlbedo(input.uv).rgb;
#if defined(PROJECTL_DISSOLVE)
    meta.Emission = _DissolveEdgeColor.rgb * (_DissolveEdgeIntensity * edge);
#endif
    return MetaFragment(meta);
}
#endif
