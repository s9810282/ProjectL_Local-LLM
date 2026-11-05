// 해골 눈 구체 셰이더. 스스로 빛나는 둥근 구체를 표현한다.
// 법선과 시선의 각도로 색을 정해 정면 중심은 아이보리, 바깥으로 갈수록 주황, 실루엣 근처는 붉은빛이 된다.
// 붉은 가장자리는 카메라를 향한 평면 고리가 아니라 구의 입체 음영이라 어느 각도에서 봐도 구의 윤곽을 따른다.
// 밝기는 시간에 따라 두 사인파를 섞어 아주 약하게 부드럽게 흔들린다(깜빡임 없음).
// 투명 큐 앞쪽(Transparent-10)에서 깊이를 써서, 뒤에 있는 불꽃·연기 파티클이 구체 위에 겹쳐 그려지지 않게 한다.
Shader "ProjectL/VFX/GlowOrb"
{
    Properties
    {
        _CoreColor ("Core Color", Color) = (1, 0.95, 0.78, 1)
        _MidColor ("Mid Color", Color) = (1, 0.6, 0.18, 1)
        _RimColor ("Rim Color", Color) = (0.8, 0.16, 0.06, 1)
        _CoreStart ("Core Start", Range(0, 1)) = 0.5
        _CoreSoftness ("Core Softness", Range(0.01, 1)) = 0.4
        _RimWidth ("Rim Width", Range(0.01, 1)) = 0.5
        _Intensity ("Intensity", Range(0, 4)) = 1
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.06
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1.6
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GlowOrb"

            Blend Off
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _MidColor;
                half4 _RimColor;
                half _CoreStart;
                half _CoreSoftness;
                half _RimWidth;
                half _Intensity;
                half _PulseAmount;
                half _PulseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                half3 viewWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half facing = saturate(dot(normalWS, viewWS));

                // 정면(facing 1)은 아이보리, 중간은 주황, 실루엣(facing 0) 쪽은 붉은빛.
                half coreMix = smoothstep(_CoreStart, min(_CoreStart + _CoreSoftness, 1.0h), facing);
                half rimMix = 1.0h - smoothstep(0.0h, _RimWidth, facing);
                half3 color = lerp(_MidColor.rgb, _CoreColor.rgb, coreMix);
                color = lerp(color, _RimColor.rgb, rimMix);

                // 주기가 맞지 않는 두 사인파를 섞어 규칙적인 경고등처럼 보이지 않게 한다.
                float time = _Time.y * _PulseSpeed;
                half pulse = 1.0h + _PulseAmount * (half)(0.6 * sin(time) + 0.4 * sin(time * 1.73 + 1.3));

                return half4(color * _Intensity * pulse, 1.0h);
            }
            ENDHLSL
        }
    }
}
