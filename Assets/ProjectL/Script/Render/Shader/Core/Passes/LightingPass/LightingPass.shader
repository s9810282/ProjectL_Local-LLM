Shader "Hidden/ProjectL/Core/LightingPass"
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
            Name "FORWARD"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma vertex ProjectLForwardVert
            #pragma fragment ProjectLForwardFrag
            #include "Assets/ProjectL/Script/Render/Shader/Core/Passes/LightingPass/ToonForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DISSOLVE_FORWARD"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma vertex ProjectLForwardVert
            #pragma fragment ProjectLForwardFrag
            #define PROJECTL_DISSOLVE 1
            #include "Assets/ProjectL/Script/Render/Shader/Core/Passes/LightingPass/ToonForwardPass.hlsl"
            ENDHLSL
        }
    }
}
