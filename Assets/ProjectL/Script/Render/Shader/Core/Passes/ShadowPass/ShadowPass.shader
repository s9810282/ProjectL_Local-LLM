Shader "Hidden/ProjectL/Core/ShadowPass"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        [Header(Base Color Lighting)]
        _DiffuseSteps ("Diffuse Shading Steps", Range(2,6)) = 2
        _DiffuseDarkStrength ("Diffuse Dark Brightness", Range(0,1)) = 0.05
        _DiffuseSoftness ("Diffuse Edge Softness", Range(0,0.1)) = 0
        _DiffuseNormalMultiplier ("Normal Light Multiplier", Range(0,8)) = 1
        [HideInInspector] _FlatDiffuseNormals ("Legacy Triangle Normal Mode", Float) = 0
        _DiffuseAmbientStrength ("Diffuse Ambient Strength", Range(0,1)) = 0.15

        [Header(Specular)]
        [HDR] _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularIntensity ("Specular Intensity", Range(0,10)) = 1

        [Header(Lit Rim)]
        [HDR] _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimIntensity ("Rim Intensity", Range(0,5)) = 1
        _RimPower ("Rim Power", Range(0.5,8)) = 4

        // 공통 CBUFFER와 맞추기 위한 비표시 속성이다.
        [HideInInspector] _DissolveTex ("Dissolve Texture (R)", 2D) = "white" {}
        [HideInInspector] _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        [HideInInspector] [HDR] _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1,0.3,0.03,1)
        [HideInInspector] _DissolveEdgeWidth ("Dissolve Edge Width", Range(0,0.5)) = 0.05
        [HideInInspector] _DissolveEdgeIntensity ("Dissolve Edge Intensity", Range(0,10)) = 2
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma vertex ProjectLShadowVert
            #pragma fragment ProjectLShadowFrag
            #include "Assets/ProjectL/Script/Render/Shader/Core/Passes/ShadowPass/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DISSOLVE_SHADOWCASTER"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma vertex ProjectLShadowVert
            #pragma fragment ProjectLShadowFrag
            #define PROJECTL_DISSOLVE 1
            #include "Assets/ProjectL/Script/Render/Shader/Core/Passes/ShadowPass/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
