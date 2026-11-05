#ifndef PROJECTL_COMPOSER_PASSES_INCLUDED
#define PROJECTL_COMPOSER_PASSES_INCLUDED

#include "ComposerLighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

// 생성 Shader가 먼저 표면 읽기/마스크/효과/라이팅 함수를 정의한다.
struct ProjectLComposerAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ProjectLComposerVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 normalWS : TEXCOORD2;
    float3 positionOS : TEXCOORD3;
    half fog : TEXCOORD4;
    half3 vertexLighting : TEXCOORD5;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
#if defined(DYNAMICLIGHTMAP_ON)
    float2 dynamicLightmapUV : TEXCOORD7;
#endif
#if defined(EDITOR_VISUALIZATION)
    float2 vizUV : TEXCOORD8;
    float4 lightCoord : TEXCOORD9;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

ProjectLComposerVaryings ProjectLComposerVert(ProjectLComposerAttributes input)
{
    ProjectLComposerVaryings output = (ProjectLComposerVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = position.positionCS;
    output.positionWS = position.positionWS;
    output.positionOS = input.positionOS.xyz;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = input.uv;
    output.fog = ComputeFogFactor(position.positionCS.z);
    output.vertexLighting = VertexLighting(position.positionWS, output.normalWS);
    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_SH(output.normalWS, output.vertexSH);
#if defined(DYNAMICLIGHTMAP_ON)
    output.dynamicLightmapUV = input.dynamicLightmapUV * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    return output;
}

ProjectLComposerContext ProjectLComposerMakeContext(ProjectLComposerVaryings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ProjectLComposerContext context = (ProjectLComposerContext)0;
    context.positionWS = input.positionWS;
    context.positionOS = input.positionOS;
    context.normalWS = normalize(input.normalWS);
    context.viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    context.uv = input.uv;
    context.screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
    context.vertexLighting = input.vertexLighting;
#if defined(DYNAMICLIGHTMAP_ON)
    context.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, context.normalWS);
#else
    context.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, context.normalWS);
#endif
    context.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    context.fog = input.fog;
    return context;
}

half4 ProjectLComposerForwardFrag(ProjectLComposerVaryings input) : SV_Target
{
    ProjectLComposerContext context = ProjectLComposerMakeContext(input);
#if defined(PROJECTL_COMPOSER_LIGHT_MASK)
    context.lightBrightness = ProjectLComposerDirectBrightness(context);
#endif
    ProjectLComposerMasks masks = ProjectLComposerEvaluateMasks(context);
    ProjectLComposerClip(masks);
    half4 albedo = ProjectLComposerReadBase(context);
    half3 color = ProjectLComposerShade(context, albedo.rgb);
    color = ProjectLComposerApplyEffects(context, masks, color);
#if !defined(PROJECTL_COMPOSER_DISABLE_FOG)
    color = MixFog(color, context.fog);
#endif
    return half4(color, albedo.a);
}

float3 _LightDirection;
float3 _LightPosition;

ProjectLComposerVaryings ProjectLComposerShadowVert(ProjectLComposerAttributes input)
{
    ProjectLComposerVaryings output = ProjectLComposerVert(input);
    output.positionCS = ProjectLCasterShadowPosition(output.positionWS, output.normalWS,
        ProjectLCasterLightDirection(output.positionWS, _LightDirection, _LightPosition));
    return output;
}

half4 ProjectLComposerShadowFrag(ProjectLComposerVaryings input) : SV_Target
{
    ProjectLComposerContext context = ProjectLComposerMakeContext(input);
    ProjectLComposerClip(ProjectLComposerEvaluateMasks(context));
    return 0;
}

half ProjectLComposerDepthFrag(ProjectLComposerVaryings input) : SV_Target
{
    ProjectLComposerContext context = ProjectLComposerMakeContext(input);
    ProjectLComposerClip(ProjectLComposerEvaluateMasks(context));
    return input.positionCS.z;
}

void ProjectLComposerDepthNormalsFrag(ProjectLComposerVaryings input,
    out half4 outNormal : SV_Target0
#if defined(_WRITE_RENDERING_LAYERS)
    , out float4 outLayers : SV_Target1
#endif
)
{
    ProjectLComposerContext context = ProjectLComposerMakeContext(input);
    ProjectLComposerClip(ProjectLComposerEvaluateMasks(context));
#if defined(_GBUFFER_NORMALS_OCT)
    float2 packed = PackNormalOctQuadEncode(context.normalWS);
    outNormal = half4(PackFloat2To888(saturate(packed * 0.5 + 0.5)), 0);
#else
    outNormal = half4(context.normalWS, 0);
#endif
#if defined(_WRITE_RENDERING_LAYERS)
    outLayers = float4(EncodeMeshRenderingLayer(GetMeshRenderingLayer()), 0, 0, 0);
#endif
}

half4 ProjectLComposerNormalFrag(ProjectLComposerVaryings input) : SV_Target
{
    ProjectLComposerContext context = ProjectLComposerMakeContext(input);
    ProjectLComposerClip(ProjectLComposerEvaluateMasks(context));
    return half4(context.normalWS * 0.5 + 0.5, 1);
}

ProjectLComposerVaryings ProjectLComposerMetaVert(ProjectLComposerAttributes input)
{
    ProjectLComposerVaryings output = ProjectLComposerVert(input);
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz,
        input.staticLightmapUV, input.dynamicLightmapUV, unity_LightmapST, unity_DynamicLightmapST);
#if defined(EDITOR_VISUALIZATION)
    UnityEditorVizData(input.positionOS.xyz, input.uv, input.staticLightmapUV, input.dynamicLightmapUV,
        output.vizUV, output.lightCoord);
#endif
    return output;
}

half4 ProjectLComposerMetaFrag(ProjectLComposerVaryings input) : SV_Target
{
    ProjectLComposerContext context = ProjectLComposerMakeContext(input);
    context.viewWS = context.normalWS;
    ProjectLComposerMasks masks = ProjectLComposerEvaluateMasks(context);
    ProjectLComposerClip(masks);
    MetaInput meta = (MetaInput)0;
    ProjectLComposerMetaSurface(context, masks, ProjectLComposerReadBase(context).rgb, meta.Albedo, meta.Emission);
#if defined(EDITOR_VISUALIZATION)
    meta.VizUV = input.vizUV;
    meta.LightCoord = input.lightCoord;
#endif
    return UnityMetaFragment(meta);
}

#endif
