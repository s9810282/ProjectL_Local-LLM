#ifndef PROJECTL_STYLIZED_PASSES_INCLUDED
#define PROJECTL_STYLIZED_PASSES_INCLUDED
#include "ProjectLStylizedLighting.hlsl"
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/OutlineNormalUtility.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct ProjectLStylizedAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 uv : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    float3 averagedNormalOS : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ProjectLStylizedVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float4 tangentWS : TEXCOORD3;
    half fog : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
#if defined(DYNAMICLIGHTMAP_ON)
    float2 dynamicLightmapUV : TEXCOORD6;
#endif
#if defined(EDITOR_VISUALIZATION)
    float2 vizUV : TEXCOORD7;
    float4 lightCoord : TEXCOORD8;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

ProjectLStylizedVaryings ProjectLStylizedVert(ProjectLStylizedAttributes input)
{
    ProjectLStylizedVaryings output = (ProjectLStylizedVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normal = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.positionCS = position.positionCS;
    output.positionWS = position.positionWS;
    output.normalWS = normal.normalWS;
    output.tangentWS = float4(normal.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.fog = ComputeFogFactor(position.positionCS.z);
    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS, output.vertexSH);
#if defined(DYNAMICLIGHTMAP_ON)
    output.dynamicLightmapUV = input.dynamicLightmapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    return output;
}

void ProjectLStylizedForwardFrag(ProjectLStylizedVaryings input, out half4 color : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out float4 layers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float dissolveEdge = ProjectLStylizedClip(input.uv);
    ProjectLStylizedSurface surface;
    surface.albedo = ProjectLStylizedBase(input.uv).rgb;
    surface.normalWS = ProjectLStylizedNormal(input.uv, input.normalWS, input.tangentWS);
    surface.viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    surface.specularMask = SAMPLE_TEXTURE2D(_SpecularMap, sampler_SpecularMap, input.uv).rgb;
    InputData data = (InputData)0;
    data.positionWS = input.positionWS;
    data.normalWS = surface.normalWS;
    data.viewDirectionWS = surface.viewWS;
    data.shadowCoord = ProjectLReceiverShadowCoord(input.positionWS);
    data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
#if defined(DYNAMICLIGHTMAP_ON)
    data.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, surface.normalWS);
#else
    data.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, surface.normalWS);
#endif
    data.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    half occlusion = lerp(1, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g, _OcclusionStrength);
    half3 shaded = ProjectLStylizedLighting(data, surface, occlusion) + ProjectLStylizedEmission(input.uv);
#if defined(PROJECTL_STYLIZED_DISSOLVE)
    shaded = lerp(shaded, _DissolveEdgeColor.rgb * _DissolveEdgeIntensity, dissolveEdge);
#endif
    // 직교 카메라의 clip Z는 거리와 같지 않다. URP의 시점 공간 Fog 경로를 사용한다.
    float fog = InitializeInputDataFog(float4(input.positionWS, 1), input.fog);
    half alpha = 1;
#if defined(PROJECTL_STYLIZED_SURFACE_ALPHA)
    alpha = _Surface > 0.5 ? saturate(ProjectLStylizedBase(input.uv).a) : 1;
#endif
    color = half4(MixFog(shaded, fog), alpha);
#if defined(_WRITE_RENDERING_LAYERS)
    layers = float4(EncodeMeshRenderingLayer(GetMeshRenderingLayer()), 0, 0, 0);
#endif
}

float3 _LightDirection;
float3 _LightPosition;
ProjectLStylizedVaryings ProjectLStylizedShadowVert(ProjectLStylizedAttributes input)
{
    ProjectLStylizedVaryings output = ProjectLStylizedVert(input);
    output.positionCS = ProjectLCasterShadowPosition(output.positionWS, normalize(output.normalWS),
        ProjectLCasterLightDirection(output.positionWS, _LightDirection, _LightPosition));
    return output;
}

half4 ProjectLStylizedShadowFrag(ProjectLStylizedVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ProjectLStylizedClip(input.uv);
    return 0;
}

half ProjectLStylizedDepthFrag(ProjectLStylizedVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ProjectLStylizedClip(input.uv);
    return input.positionCS.z;
}

void ProjectLStylizedDepthNormalsFrag(ProjectLStylizedVaryings input, out half4 normal : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out float4 layers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ProjectLStylizedClip(input.uv);
    half3 n = ProjectLStylizedNormal(input.uv, input.normalWS, input.tangentWS);
#if defined(_GBUFFER_NORMALS_OCT)
    normal = half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(n) * 0.5 + 0.5)), 0);
#else
    normal = half4(n, 0);
#endif
#if defined(_WRITE_RENDERING_LAYERS)
    layers = float4(EncodeMeshRenderingLayer(GetMeshRenderingLayer()), 0, 0, 0);
#endif
}

half4 ProjectLStylizedNormalFrag(ProjectLStylizedVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ProjectLStylizedClip(input.uv);
    return half4(ProjectLStylizedNormal(input.uv, input.normalWS, input.tangentWS) * 0.5 + 0.5, 1);
}

ProjectLStylizedVaryings ProjectLStylizedMetaVert(ProjectLStylizedAttributes input)
{
    ProjectLStylizedVaryings output = ProjectLStylizedVert(input);
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.staticLightmapUV,
        input.dynamicLightmapUV, unity_LightmapST, unity_DynamicLightmapST);
#if defined(EDITOR_VISUALIZATION)
    UnityEditorVizData(input.positionOS.xyz, input.uv, input.staticLightmapUV, input.dynamicLightmapUV,
        output.vizUV, output.lightCoord);
#endif
    return output;
}

half4 ProjectLStylizedMetaFrag(ProjectLStylizedVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float dissolveEdge = ProjectLStylizedClip(input.uv);
    MetaInput meta = (MetaInput)0;
    meta.Albedo = ProjectLStylizedBase(input.uv).rgb;
    meta.Emission = ProjectLStylizedEmission(input.uv);
#if defined(PROJECTL_STYLIZED_DISSOLVE)
    meta.Albedo *= 1.0 - dissolveEdge;
    meta.Emission += _DissolveEdgeColor.rgb * _DissolveEdgeIntensity * dissolveEdge;
#endif
#if defined(EDITOR_VISUALIZATION)
    meta.VizUV = input.vizUV;
    meta.LightCoord = input.lightCoord;
#endif
    return UnityMetaFragment(meta);
}

// 반전 헐. 폭은 월드 단위이고 실제 그림자와 베이크에는 포함하지 않는다.
ProjectLStylizedVaryings ProjectLStylizedOutlineVert(ProjectLStylizedAttributes input)
{
    ProjectLStylizedVaryings output = ProjectLStylizedVert(input);
    float3 outlineNormalOS = ProjectLOutlineNormalOS(input.averagedNormalOS, input.normalOS);
    float3 outlineNormalWS = TransformObjectToWorldNormal(outlineNormalOS);
    output.positionCS = TransformWorldToHClip(output.positionWS + outlineNormalWS * _OutlineWidth);
    return output;
}

half4 ProjectLStylizedOutlineFrag(ProjectLStylizedVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    clip(_OutlineWidth - 0.000001);
    ProjectLStylizedClip(input.uv);
    float fog = InitializeInputDataFog(float4(input.positionWS, 1), input.fog);
    return half4(MixFog(_OutlineColor.rgb, fog), 1);
}
#endif
