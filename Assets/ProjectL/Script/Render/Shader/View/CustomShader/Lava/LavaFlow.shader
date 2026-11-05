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

        [Header(Crust)]
        _CrustTexture ("Crust Texture (RGB, A = Blend Mask)", 2D) = "black" {}
        _CrustUVSpeed ("Crust UV Speed (XY)", Vector) = (0, 0, 0, 0)
        _CrustBlendStrength ("Crust Blend Strength", Range(0, 1)) = 0
        _CrustWidthPixels ("Crust Width (Pixels)", Range(0, 32)) = 2
        _CrustDepthThreshold ("Crust Depth Threshold", Range(0, 50)) = 0.05
        _CrustDepthSoftness ("Crust Depth Edge Softness", Range(0.001, 10)) = 0.02

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

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
            TEXTURE2D(_CrustTexture);
            SAMPLER(sampler_CrustTexture);
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
                float4 _CrustTexture_ST;
                float4 _CrustUVSpeed;
                float _CrustBlendStrength;
                float _CrustWidthPixels;
                float _CrustDepthThreshold;
                float _CrustDepthSoftness;
                float4 _VertexMotionTexture_ST;
                float4 _VertexMotionUVSpeed;
                float _VertexMotionAmplitude;
                float _VertexMotionSpeed;
            CBUFFER_END

            static const float2 CrustSampleDirections[8] =
            {
                float2(1.0, 0.0), float2(-1.0, 0.0), float2(0.0, 1.0), float2(0.0, -1.0),
                float2(0.70710678, 0.70710678), float2(-0.70710678, 0.70710678),
                float2(0.70710678, -0.70710678), float2(-0.70710678, -0.70710678)
            };

            bool HasSceneDepth(float rawDepth)
            {
#if UNITY_REVERSED_Z
                return rawDepth > 0.000001;
#else
                return rawDepth < 0.999999;
#endif
            }

            float ToEyeDepth(float rawDepth)
            {
                if (unity_OrthoParams.w > 0.5)
                {
#if UNITY_REVERSED_Z
                    rawDepth = 1.0 - rawDepth;
#endif
                    return lerp(_ProjectionParams.y, _ProjectionParams.z, rawDepth);
                }

                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            // 보이는 용암 픽셀 주위에서, 용암보다 충분히 카메라 쪽인 오브젝트를 찾는다.
            // 해당 오브젝트 자체에는 깊이 테스트로 색을 쓰지 않아 크러스트가 바위 위로 올라가지 않는다.
            float CrustOverlapMask(float2 screenUV, float lavaEyeDepth)
            {
                if (_CrustWidthPixels <= 0.0 || _CrustBlendStrength <= 0.0)
                    return 0.0;

                float2 offset = _CrustWidthPixels / _ScaledScreenParams.xy;
                float overlap = 0.0;
                [unroll]
                for (int index = 0; index < 8; index++)
                {
                    float rawSceneDepth = SampleSceneDepth(screenUV + CrustSampleDirections[index] * offset);
                    if (!HasSceneDepth(rawSceneDepth))
                        continue;

                    float sceneEyeDepth = ToEyeDepth(rawSceneDepth);
                    // 두 표면의 시점 깊이 차는 방향과 무관하게 절댓값으로 비교한다.
                    // 이진 step 대신 전이 폭을 둬, 경계에서 크러스트가 부드럽게 사라진다.
                    float depthDifference = abs(lavaEyeDepth - sceneEyeDepth);
                    float depthMask = smoothstep(_CrustDepthThreshold,
                        _CrustDepthThreshold + _CrustDepthSoftness, depthDifference);
                    overlap = max(overlap, depthMask);
                }

                return overlap;
            }

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

                float2 crustUV = TRANSFORM_TEX(input.uv, _CrustTexture);
                crustUV += _CrustUVSpeed.xy * _Time.y;
                half4 crust = SAMPLE_TEXTURE2D(_CrustTexture, sampler_CrustTexture, crustUV);
                float lavaEyeDepth = ToEyeDepth(input.positionCS.z);
                float overlap = CrustOverlapMask(GetNormalizedScreenSpaceUV(input.positionCS), lavaEyeDepth);
                half crustMask = saturate(crust.a * _CrustBlendStrength * overlap);
                half3 surfaceColor = lerp(lava, crust.rgb, crustMask);
                float2 emissionMaskUV = TRANSFORM_TEX(input.uv, _EmissionMaskTexture);
                emissionMaskUV += _EmissionMaskUVSpeed.xy * _Time.y;
                half emissionMask = SAMPLE_TEXTURE2D(_EmissionMaskTexture, sampler_EmissionMaskTexture, emissionMaskUV).r;
                // 크러스트가 보이는 부분은 용암 발광도 함께 가려, 식은 껍질처럼 보이게 한다.
                half3 emission = lava * _EmissionColor.rgb * emissionMask * _LavaEmissionStrength * (1.0h - crustMask);

                return half4(surfaceColor + emission, 1.0);
            }
            ENDHLSL
        }
    }
}
