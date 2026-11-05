// 해골 눈 불꽃 전용 셰이더. 불꽃 메시(파티클·MeshRenderer)를 물결치고 일렁이게 흔든다.
// 색 계산은 LowPolyFlash와 같다: 법선과 시선의 각도로 열(heat)을 정해 정면은 밝은 노랑, 비스듬한 면은 주황~붉은빛,
// 끝(UV.x = 1)으로 갈수록 식는다.
// 정점 단계에서 뿌리(UV.x = 0)는 고정하고 끝으로 갈수록 크게 옆으로 흔들며, 높이에 따라 위상이 밀려
// 물결이 불꽃을 따라 위로 타고 오른다. 불꽃 위치마다 위상이 달라 여러 혀가 똑같이 흔들리지 않는다.
// _FlickerAmount를 주면 끝이 위아래로 불규칙하게 솟았다 내려앉아 이글거린다(0이면 꺼짐).
// 메시 UV: x = 길이 방향 위치(0 뿌리 → 1 끝), y = 면마다 다른 난수.
Shader "ProjectL/VFX/WavyFlame"
{
    Properties
    {
        _CoreColor ("Core Color", Color) = (1, 0.88, 0.42, 1)
        _MidColor ("Mid Color", Color) = (1, 0.52, 0.07, 1)
        _OuterColor ("Outer Color", Color) = (0.82, 0.18, 0.04, 1)
        _Intensity ("Intensity", Range(0, 4)) = 1
        _FacingPower ("Facing Power", Range(0.25, 4)) = 1.1
        _TipCooling ("Tip Cooling", Range(0, 1)) = 0.55
        _FacetVariation ("Facet Variation", Range(0, 0.5)) = 0.12
        _Additive ("Additive", Range(0, 1)) = 0.25
        _Opacity ("Opacity", Range(0, 1)) = 1
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.1
        _WaveAmplitude ("Wave Amplitude", Float) = 0.08
        _WaveLength ("Wave Length", Float) = 1
        _WaveSpeed ("Wave Speed", Float) = 6
        _FlickerAmount ("Flicker Amount", Float) = 0
        _FlickerSpeed ("Flicker Speed", Float) = 9
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
            Name "WavyFlame"

            // 사전 곱 알파: _Additive가 0이면 일반 반투명, 1이면 가산.
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _MidColor;
                half4 _OuterColor;
                half _Intensity;
                half _FacingPower;
                half _TipCooling;
                half _FacetVariation;
                half _Additive;
                half _Opacity;
                half _EdgeSoftness;
                float _WaveAmplitude;
                float _WaveLength;
                float _WaveSpeed;
                float _FlickerAmount;
                float _FlickerSpeed;
            CBUFFER_END

            static const float TwoPi = 6.2831853;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                float3 positionOS = input.positionOS.xyz;

                // 뿌리는 고정하고 끝으로 갈수록 크게 흔든다.
                float along = saturate(input.uv.x);
                float weight = along * sqrt(along);

                // 높이에 따라 위상을 밀어 물결이 위로 타고 오르게 하고, 가로 위치로 혀마다 위상을 다르게 한다.
                float waveLength = max(_WaveLength, 1e-3);
                float phase = _Time.y * _WaveSpeed
                    - positionOS.y * (TwoPi / waveLength)
                    + (positionOS.x * 1.7 + positionOS.z * 2.3) * (TwoPi / waveLength);
                positionOS.x += sin(phase) * _WaveAmplitude * weight;
                positionOS.z += sin(phase * 0.8 + 1.7) * _WaveAmplitude * 0.6 * weight;

                // 주기가 맞지 않는 세 사인파로 끝을 위아래로 불규칙하게 솟게 한다. 가로 위치로 혀마다 어긋난다.
                float flickerTime = _Time.y * _FlickerSpeed
                    + (positionOS.x * 1.3 + positionOS.z * 0.9) * (TwoPi / waveLength) * 0.5;
                float flicker = 0.5 * sin(flickerTime)
                    + 0.3 * sin(flickerTime * 2.31 + 1.7)
                    + 0.2 * sin(flickerTime * 3.7 + 0.4);
                positionOS.y += flicker * _FlickerAmount * weight;

                Varyings output;
                output.positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half facing = saturate(dot(normalWS, viewWS));

                half heat = pow(facing, _FacingPower) * (1.0h - _TipCooling * input.uv.x);
                heat = saturate(heat + (input.uv.y - 0.5h) * _FacetVariation);

                half3 color = lerp(_OuterColor.rgb, _MidColor.rgb, saturate(heat * 2.0h));
                color = lerp(color, _CoreColor.rgb, saturate(heat * 2.0h - 1.0h));
                color *= input.color.rgb * _Intensity;

                half alpha = saturate(input.color.a * _Opacity);
                alpha *= smoothstep(0.0h, max(_EdgeSoftness, 1e-3h), facing);
                return half4(color * alpha, alpha * (1.0h - _Additive));
            }
            ENDHLSL
        }
    }
}
