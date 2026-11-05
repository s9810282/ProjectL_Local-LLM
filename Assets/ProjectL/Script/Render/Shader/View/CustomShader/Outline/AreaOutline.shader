Shader "ProjectL/Outline/AreaOutline"
{
    Properties
    {
        [Header(Appearance)]
        [HDR] _BaseColor ("Outline Color", Color) = (0.0, 1.0, 4.0, 1.0)

        [Header(Dash)]
        [Toggle] _DashEnabled ("Dashed", Float) = 1.0
        _DashCount ("Dashes Per Loop", Range(1.0, 128.0)) = 4.0
        _DashFill ("Dash Fill", Range(0.05, 1.0)) = 0.55

        [Header(Flow)]
        [Enum(Clockwise, 1, Counterclockwise, 0)] _FlowDirection ("Flow Direction", Float) = 1.0
        _FlowSpeed ("Flow Speed", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "AreaOutline"
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _DashEnabled;
                float _DashCount;
                float _DashFill;
                float _FlowDirection;
                float _FlowSpeed;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // U spans one complete boundary loop. Whole repeats keep the
                // animated pattern continuous across its closing UV seam.
                float direction = _FlowDirection > 0.5 ? 1.0 : -1.0;
                float dashCount = max(floor(_DashCount + 0.5), 1.0);
                float phase = input.uv.x * dashCount - _Time.y * _FlowSpeed * direction;
                // Measure derivatives before wrapping to avoid a seam at each repeat.
                float transitionWidth = max(fwidth(phase), 0.0001);
                float distanceToCenter = abs(frac(phase + 0.5) - 0.5);
                float fill = saturate(_DashFill);
                float dashMask = 1.0 - smoothstep(
                    fill * 0.5 - transitionWidth * 0.5,
                    fill * 0.5 + transitionWidth * 0.5,
                    distanceToCenter);
                dashMask = fill >= 1.0 ? 1.0 : (fill <= 0.0 ? 0.0 : dashMask);
                float visible = lerp(1.0, dashMask, saturate(_DashEnabled));

                // Gaps have zero alpha; the color alpha controls outline opacity.
                return half4(_BaseColor.rgb, _BaseColor.a * visible);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
