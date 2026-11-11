Shader "ProjectL/View/Lava/Flow"
{
    Properties
    {
        [Header(Lava)]
        [MainTexture] _LavaTexture ("Lava Texture (RGBA)", 2D) = "white" {}
        _LavaUVSpeed ("Lava UV Speed (XY)", Vector) = (0.05, 0.02, 0, 0)
        [HDR] _LavaTint ("Lava Tint", Color) = (1, 1, 1, 1)
        _EmissionMaskTexture ("Emission Mask (R)", 2D) = "white" {}
        _EmissionMaskUVSpeed ("Emission Mask UV Speed (XY)", Vector) = (0.05, 0.02, 0, 0)
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _LavaEmissionStrength ("Lava Emission Strength", Range(0, 10)) = 1

        [Header(Vertex Motion)]
        _VertexMotionTexture ("Vertex Motion Texture (R = Intensity, G = Phase)", 2D) = "black" {}
        _VertexMotionUVSpeed ("Vertex Motion UV Speed (XY)", Vector) = (0, 0, 0, 0)
        _VertexMotionAmplitude ("Vertex Motion Amplitude", Range(0, 2)) = 0
        _VertexMotionSpeed ("Vertex Motion Speed", Range(0, 10)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "LavaFlow"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_LavaTexture);
            SAMPLER(sampler_LavaTexture);
            TEXTURE2D(_EmissionMaskTexture);
            SAMPLER(sampler_EmissionMaskTexture);
            TEXTURE2D(_VertexMotionTexture);
            SAMPLER(sampler_VertexMotionTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _LavaTexture_ST;
                float4 _LavaUVSpeed;
                float4 _LavaTint;
                float4 _EmissionMaskTexture_ST;
                float4 _EmissionMaskUVSpeed;
                float4 _EmissionColor;
                float _LavaEmissionStrength;
                float4 _VertexMotionTexture_ST;
                float4 _VertexMotionUVSpeed;
                float _VertexMotionAmplitude;
                float _VertexMotionSpeed;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float2 motionUV = TRANSFORM_TEX(input.uv, _VertexMotionTexture);
                motionUV += _VertexMotionUVSpeed.xy * _Time.y;

                // R은 정점별 변위 세기, G는 왕복 운동의 위상이다. 검은 텍스처는 변위를 만들지 않는다.
                float2 motion = SAMPLE_TEXTURE2D_LOD(_VertexMotionTexture, sampler_VertexMotionTexture, motionUV, 0).rg;
                float verticalOffset = motion.r * sin(_Time.y * _VertexMotionSpeed + motion.g * 6.28318530718);
                float3 positionOS = input.positionOS.xyz;
                positionOS.y += verticalOffset * _VertexMotionAmplitude;

                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 lavaUV = TRANSFORM_TEX(input.uv, _LavaTexture);
                lavaUV += _LavaUVSpeed.xy * _Time.y;
                half3 lava = SAMPLE_TEXTURE2D(_LavaTexture, sampler_LavaTexture, lavaUV).rgb * _LavaTint.rgb;

                float2 emissionMaskUV = TRANSFORM_TEX(input.uv, _EmissionMaskTexture);
                emissionMaskUV += _EmissionMaskUVSpeed.xy * _Time.y;
                half emissionMask = SAMPLE_TEXTURE2D(_EmissionMaskTexture, sampler_EmissionMaskTexture, emissionMaskUV).r;
                half3 emission = lava * _EmissionColor.rgb * emissionMask * _LavaEmissionStrength;

                return half4(lava + emission, 1.0);
            }
            ENDHLSL
        }
    }
}
