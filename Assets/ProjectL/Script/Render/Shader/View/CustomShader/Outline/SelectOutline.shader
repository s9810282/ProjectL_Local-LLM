Shader "ProjectL/Outline/Selection"
{
    Properties
    {
        [Header(Outline Settings)]
        [HDR] _OutlineColor ("Outline Color (Emission)", Color) = (0, 0, 0, 1)
        _OutlineThickness ("Base Outline Thickness", Range(0.0, 0.5)) = 0.005
        
        [Header(Pulse Animation)]
        _PulseSpeed ("Pulse Speed", Float) = 5.0
        _PulseGap ("Pulse Gap (Amplitude)", Range(0.0, 0.5)) = 0.015
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL; // ������ ���� ���
                float3 averagedNormalOS : TEXCOORD3; // �ε巴�� ������ �밢�� ���
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float _PulseSpeed;
                float _PulseGap;
            CBUFFER_END

            Varyings OutlineVert(Attributes input)
            {
                Varyings output;

                float3 hardNormal = normalize(input.normalOS);
                float3 smoothNormal = normalize(input.averagedNormalOS);
                
                float cosAngle = max(dot(hardNormal, smoothNormal), 0.1);
               
                float wave = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                float animatedThickness = _OutlineThickness + (wave * _PulseGap);
                float compensatedThickness = animatedThickness / cosAngle;
                float3 positionOS = input.positionOS.xyz + smoothNormal * compensatedThickness;

                output.positionCS = TransformObjectToHClip(positionOS);

                return output;
            }

            half4 OutlineFrag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}