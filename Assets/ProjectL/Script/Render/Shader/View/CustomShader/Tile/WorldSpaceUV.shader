Shader "ProjectL/Tile/WorldSpaceUV"
{
    Properties
    {
        [MainTexture] _BaseMap ("World XZ Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Top Tint", Color) = (1, 1, 1, 1)
        _TopNormalThreshold ("Top Face Normal Y Threshold", Range(0.01, 1)) = 0.7

        [Header(Side Gradient)]
        _SideBottomColor ("Side Bottom Color", Color) = (0.12, 0.14, 0.18, 1)
        _SideTopColor ("Side Top Color", Color) = (0.38, 0.42, 0.48, 1)

        [Header(Top Surface Edge Mask)]
        _TopEdgeColor ("Edge Mask Color (Alpha = Opacity)", Color) = (0.05, 0.06, 0.08, 1)
        _TopEdgeWidth ("Edge Mask Width (UV)", Range(0, 0.5)) = 0.03
        _TopEdgeSoftness ("Edge Mask Softness (UV)", Range(0, 0.25)) = 0.003
        [ToggleUI] _ShowTopEdgeMask ("Preview Edge Mask", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "UniversalMaterialType" = "Unlit"
        }

        Cull Back
        ZWrite On
        ZTest LEqual

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _SideBottomColor;
            half4 _SideTopColor;
            half4 _TopEdgeColor;
            float _TopNormalThreshold;
            float _TopEdgeWidth;
            float _TopEdgeSoftness;
            float _ShowTopEdgeMask;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 worldUV : TEXCOORD0;
            float heightWS : TEXCOORD1;
            float3 normalWS : TEXCOORD2;
            float2 meshUV : TEXCOORD3;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vert(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
            output.positionCS = positions.positionCS;

            // 메시 UV나 오브젝트 원점 대신 표면의 월드 XZ를 사용해 타일 사이 무늬를 잇는다.
            // Tiling은 월드 1단위당 반복 횟수이며 Offset은 UV 단위다.
            output.worldUV = positions.positionWS.xz * _BaseMap_ST.xy + _BaseMap_ST.zw;
            output.heightWS = positions.positionWS.y;
            output.normalWS = TransformObjectToWorldNormal(input.normalOS);
            output.meshUV = input.uv;
            return output;
        }

        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            // 원본 텍스처의 Wrap 설정을 바꾸지 않고 음수 좌표에서도 반복한다.
            half3 topColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_TrilinearRepeat, input.worldUV).rgb * _BaseColor.rgb;

            // Renderer 경계를 원본으로 사용해 피벗·이동·크기가 달라도 높이를 따로 입력하지 않는다.
            float bottomHeight = unity_RendererBounds_Min.y;
            float topHeight = unity_RendererBounds_Max.y;
            float heightRange = max(topHeight - bottomHeight, 0.00001);
            float heightRatio = saturate((input.heightWS - bottomHeight) / heightRange);
            half3 sideColor = lerp(_SideBottomColor.rgb, _SideTopColor.rgb, heightRatio);

            // 윗면이 UV 0~1 사각형을 채우는 메시의 네 변을 안쪽으로 마스킹한다.
            // 원래 메시 UV를 쓰므로 월드 텍스처의 Tiling/Offset이 경계선을 움직이지 않는다.
            float2 edgeDistances = min(input.meshUV, 1.0 - input.meshUV);
            float distanceToEdge = min(edgeDistances.x, edgeDistances.y);
            float edgeWidth = clamp(_TopEdgeWidth, 0.0, 0.5);
            float softness = max(max(_TopEdgeSoftness, fwidth(distanceToEdge)), 0.00001);
            float edgeMask = 1.0 - smoothstep(edgeWidth - softness, edgeWidth + softness, distanceToEdge);
            edgeMask *= edgeWidth > 0.0 ? 1.0 : 0.0;
            topColor = lerp(topColor, _TopEdgeColor.rgb, edgeMask * saturate(_TopEdgeColor.a));
            topColor = lerp(topColor, edgeMask.xxx, step(0.5, _ShowTopEdgeMask));

            float topMask = step(_TopNormalThreshold, normalize(input.normalWS).y);
            return half4(lerp(sideColor, topColor, topMask), 1.0h);
        }

        half4 DepthFrag(Varyings input) : SV_Target
        {
            return 0;
        }
        ENDHLSL

        Pass
        {
            Name "WorldSpaceUV"
            Tags { "LightMode" = "UniversalForwardOnly" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            // Renderer별 경계를 유지하기 위해 일반 GPU 인스턴싱 대신 SRP Batcher를 사용한다.
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
