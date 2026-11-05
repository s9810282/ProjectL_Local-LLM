// 불씨 꼬리(파티클 Trails) 셰이더. 꼬리 폭 방향(UV.y)으로 가장자리가 또렷한 띠를 그리고,
// 색·투명도는 파티클 색과 Trails 모듈의 Color/Width over Trail을 그대로 따른다.
Shader "ProjectL/VFX/EmberTrail"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.7, 0.25, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1.3
        _Additive ("Additive", Range(0, 1)) = 0.7
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "EmberTrail"

            // 사전 곱 알파: _Additive가 0이면 일반 반투명, 1이면 가산.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Additive;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 폭 방향 중심 0 → 가장자리 1.
                half across = abs(input.uv.y - 0.5h) * 2.0h;
                half edgeWidth = max(fwidth(across), 1e-4h) * 1.5h;
                half mask = 1.0h - smoothstep(1.0h - edgeWidth, 1.0h, across);

                half3 color = _Color.rgb * input.color.rgb * _Intensity;
                half alpha = saturate(mask * input.color.a);
                return half4(color * alpha, alpha * (1.0h - _Additive));
            }
            ENDHLSL
        }
    }
}
