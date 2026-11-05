Shader "ProjectL/View/ToonLit"
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
        // 기존 머티리얼/공통 CBUFFER 호환용이다. 새 Forward는 모델 노멀을 사용한다.
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
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        LOD 300

        UsePass "Hidden/ProjectL/Core/LightingPass/FORWARD"
        UsePass "Hidden/ProjectL/Core/ShadowPass/SHADOWCASTER"
        UsePass "Hidden/ProjectL/Core/URPPass/DEPTHONLY"
        UsePass "Hidden/ProjectL/Core/URPPass/DEPTHNORMALS"
        UsePass "Hidden/ProjectL/Core/URPPass/NORMALONLY"
        UsePass "Hidden/ProjectL/Core/URPPass/META"
    }
}
