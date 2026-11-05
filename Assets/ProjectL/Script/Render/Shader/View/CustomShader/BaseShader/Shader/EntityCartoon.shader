Shader "ProjectL/BaseShader/EntityCartoon"
{
    Properties
    {
        [Header(Surface)]
        [Enum(Opaque,0,Transparent,1)] _Surface ("표면 타입", Float) = 0
        [MainTexture] _BaseMap ("기본 텍스처", 2D) = "white" {}
        [MainColor] _BaseColor ("기본 색", Color) = (1,1,1,1)
        [Normal] _BumpMap ("노멀맵", 2D) = "bump" {}
        _BumpScale ("노멀 강도", Range(0,2)) = 1
        _OcclusionMap ("AO 텍스처 G", 2D) = "white" {}
        _OcclusionStrength ("AO 강도", Range(0,1)) = 1
        _EmissionMap ("발광 텍스처", 2D) = "white" {}
        [HDR] _EmissionColor ("발광 색", Color) = (0,0,0,1)
        _EmissionStrength ("발광 강도", Range(0,10)) = 1
        [Toggle] _AlphaClip ("알파 컷아웃", Float) = 0
        _Cutoff ("알파 기준", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("렌더링 면", Float) = 2

        [Header(Dissolve)]
        [NoScaleOffset] _DissolveTex ("디졸브 텍스처 (R)", 2D) = "white" {}
        _DissolveAmount ("디졸브 양", Range(0,1)) = 0
        [HDR] _DissolveEdgeColor ("디졸브 경계 색", Color) = (1,0.3,0.03,1)
        _DissolveEdgeWidth ("디졸브 경계 폭", Range(0,0.5)) = 0.05
        _DissolveEdgeIntensity ("디졸브 경계 발광", Range(0,10)) = 2

        [Header(Lighting)]
        _ShadeColor ("그늘 색 배율", Color) = (0.25,0.28,0.38,1)
        _AmbientStrength ("환경광 강도", Range(0,2)) = 1
        _DirectStrength ("직접광 강도", Range(0,3)) = 1
        [Toggle] _ReceiveShadows ("실시간 그림자 받기", Float) = 1

        [Header(Cartoon)]
        _ShadingSteps ("명암 단계", Range(2,20)) = 3
        _ShadeThreshold ("명암 경계 위치", Range(0,1)) = 0.5
        _ShadeSoftness ("명암 경계 부드러움", Range(0,0.2)) = 0
        [HideInInspector] _HalfLambertPower ("Unused Half Lambert Power", Float) = 2

        [Header(Specular)]
        _SpecularMap ("스펙큘러 마스크 RGB", 2D) = "white" {}
        [HDR] _SpecularColor ("스펙큘러 색", Color) = (1,1,1,1)
        _SpecularIntensity ("스펙큘러 강도", Range(0,5)) = 0.3
        _SpecularPower ("스펙큘러 집중도", Range(1,256)) = 48
        _SpecularThreshold ("스펙큘러 단계 기준", Range(0,1)) = 0.5
        _SpecularSoftness ("스펙큘러 경계", Range(0,0.5)) = 0.02

        [Header(Light Facing Rim)]
        [HDR] _RimColor ("림 색", Color) = (1,0.85,0.65,1)
        _RimIntensity ("림 강도", Range(0,5)) = 0.7
        _RimWidth ("림 폭", Range(0,1)) = 0.35
        _RimSoftness ("림 경계 부드러움", Range(0,0.5)) = 0.05
        _RimPower ("림 시선 곡선", Range(0.1,8)) = 1
        _RimLightThreshold ("림 광원 방향 시작점", Range(0,0.95)) = 0.1
        _RimLightSoftness ("림 광원 방향 경계", Range(0,0.5)) = 0.05
        _RimLightPower ("림 광원 방향 곡선", Range(0.1,8)) = 1

        [Header(Outline)]
        _OutlineColor ("외곽선 색", Color) = (0.035,0.025,0.05,1)
        _OutlineWidth ("외곽선 폭 월드 단위", Range(0,0.1)) = 0.015
        [HideInInspector] _SrcBlend ("Source Blend", Float) = 1
        [HideInInspector] _DstBlend ("Destination Blend", Float) = 0
        [HideInInspector] _SrcBlendAlpha ("Source Alpha Blend", Float) = 1
        [HideInInspector] _DstBlendAlpha ("Destination Alpha Blend", Float) = 0
        [HideInInspector] _ZWrite ("Depth Write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" "ProjectLMovementTransparency" = "True" }
        HLSLINCLUDE
        #define PROJECTL_STYLIZED_SURFACE_ALPHA 1
        #define PROJECTL_STYLIZED_DISSOLVE 1
        #include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Stylized/ProjectLStylizedInput.hlsl"
        #include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Stylized/ProjectLStylizedCartoon.hlsl"
        #include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Stylized/ProjectLStylizedPasses.hlsl"
        ENDHLSL

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex ProjectLStylizedOutlineVert
            #pragma fragment ProjectLStylizedOutlineFrag
            #pragma multi_compile_fog
            ENDHLSL
        }
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForwardOnly" }
            Cull [_Cull] ZWrite [_ZWrite] ZTest LEqual
            Blend [_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex ProjectLStylizedVert
            #pragma fragment ProjectLStylizedForwardFrag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ _SHADOWS_SHADOWMASK
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma multi_compile_fog
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull [_Cull] ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex ProjectLStylizedShadowVert
            #pragma fragment ProjectLStylizedShadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            Cull [_Cull] ZWrite On ZTest LEqual ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex ProjectLStylizedVert
            #pragma fragment ProjectLStylizedDepthFrag
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormalsOnly" }
            Cull [_Cull] ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex ProjectLStylizedVert
            #pragma fragment ProjectLStylizedDepthNormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "NormalOnly"
            Tags { "LightMode" = "NormalOnly" }
            Cull [_Cull] ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex ProjectLStylizedVert
            #pragma fragment ProjectLStylizedNormalFrag
            ENDHLSL
        }
        Pass
        {
            Name "EffectOcclusionMask"
            Tags { "LightMode" = "ProjectLEffectOcclusion" }
            Cull [_Cull] ZWrite Off ZTest Always
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex ProjectLStylizedVert
            #pragma fragment ProjectLCartoonEffectOcclusionFrag
            #include "Assets/ProjectL/Script/Render/RendererFeatures/Systems/EffectOcclusionOutline/CartoonEffectOcclusion.hlsl"
            ENDHLSL
        }
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }
            Cull Off ZWrite On ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex ProjectLStylizedMetaVert
            #pragma fragment ProjectLStylizedMetaFrag
            #pragma shader_feature EDITOR_VISUALIZATION
            ENDHLSL
        }
    }
    Fallback Off
    CustomEditor "ProjectL.Tool.Editor.CartoonMaterialGUI"
}
