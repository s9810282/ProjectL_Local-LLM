Shader "Hidden/ProjectL/EffectOcclusionOutline"
{
    Properties
    {
        _WidthPixels ("Width Pixels", Float) = 3
        _UseBakedNormals ("Use Baked Normals", Float) = 0
        _OutlineColor ("Outline Color", Color) = (1,0.8,0.4,0.9)
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Assets/ProjectL/Script/Render/Shader/Core/Includes/OutlineNormalUtility.hlsl"
        #include "EffectOcclusionDepth.hlsl"
        TEXTURE2D_X_FLOAT(_ProjectLEOOOriginalDepth);
        TEXTURE2D_X(_ProjectLEOOCoverage);
        CBUFFER_START(UnityPerMaterial)
        float _WidthPixels;
        float _UseBakedNormals;
        half4 _OutlineColor;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float3 averagedNormalOS : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        Varyings HullVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            float3 normalOS = _UseBakedNormals > 0.5 ? ProjectLOutlineNormalOS(input.averagedNormalOS, input.normalOS) : input.normalOS;
            float3 normal = TransformObjectToWorldNormal(normalOS);
            float4 directionCS = mul(UNITY_MATRIX_VP, float4(normal, 0));
            // 원근 나눗셈에 따른 방향 변화도 반영한다. 두께에 비례하는 텍셀 반복 검사는 없다.
            float2 direction = (directionCS.xy - output.positionCS.xy / output.positionCS.w * directionCS.w) * _ScaledScreenParams.xy;
            direction *= rsqrt(max(dot(direction, direction), 0.000001));
            output.positionCS.xy += direction * (2.0 * _WidthPixels / _ScaledScreenParams.xy) * output.positionCS.w;
            return output;
        }
        half4 DepthFragment(Varyings input) : SV_Target
        {
            return 0;
        }
        half4 OutlineFragment(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
            float original = SAMPLE_TEXTURE2D_X(_ProjectLEOOOriginalDepth, sampler_PointClamp, uv).r;
            clip(ProjectLEOOHasDepth(original) ? -1 : 1);
            float hull = SAMPLE_TEXTURE2D_X(_ProjectLEOOHullDepth, sampler_PointClamp, uv).r;
            clip(ProjectLEOOHasDepth(hull) ? 1 : -1);
            float eye = ProjectLEOOEyeDepth(input.positionCS.z);
            clip(_ProjectLEOOParameters.z - abs(eye - ProjectLEOOEyeDepth(hull)));
            float world = SAMPLE_TEXTURE2D_X(_ProjectLEOOWorldDepth, sampler_PointClamp, uv).r;
            clip(ProjectLEOOBlocked(world, eye) ? -1 : 1);
            half coverage = SAMPLE_TEXTURE2D_X(_ProjectLEOOCoverage, sampler_PointClamp, uv).r;
            half visibility = smoothstep(_ProjectLEOOParameters.x, _ProjectLEOOParameters.x + _ProjectLEOOParameters.y, coverage);
            clip(visibility - 0.0001);
            return half4(_OutlineColor.rgb, _OutlineColor.a * visibility);
        }
        ENDHLSL
        Pass
        {
            Name "HullDepth"
            Cull Off ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex HullVertex
            #pragma fragment DepthFragment
            ENDHLSL
        }
        Pass
        {
            Name "Outline"
            Cull Off ZWrite Off ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma vertex HullVertex
            #pragma fragment OutlineFragment
            ENDHLSL
        }
    }
}
