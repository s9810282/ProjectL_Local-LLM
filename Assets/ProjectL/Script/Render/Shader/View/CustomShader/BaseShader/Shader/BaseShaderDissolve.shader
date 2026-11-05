Shader "Custom/ProjectLBaseLitDissolve"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}

        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5

        [Header(Base Color Lighting)]
        _DiffuseSteps ("Diffuse Shading Steps", Range(2,6)) = 2
        _DiffuseDarkStrength ("Diffuse Dark Brightness", Range(0,1)) = 0.05
        _DiffuseSoftness ("Diffuse Edge Softness", Range(0,0.1)) = 0
        [Toggle] _FlatDiffuseNormals ("Flat Face Diffuse", Float) = 1
        _DiffuseAmbientStrength ("Diffuse Ambient Strength", Range(0,1)) = 0.15

        [Header(Dissolve)]
        _DissolveTex ("Dissolve Texture (R)", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0
        [HDR] _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1,0.3,0.03,1)
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0,0.5)) = 0.05
        _DissolveEdgeIntensity ("Dissolve Edge Intensity", Range(0,10)) = 2

        [Header(Specular)]
        [HDR] _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularIntensity ("Specular Intensity", Range(0,10)) = 1

        [Header(Lit Rim)]
        [HDR] _RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimIntensity ("Rim Intensity", Range(0,5)) = 1
        _RimPower ("Rim Power", Range(0.5,8)) = 4

    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        LOD 300

        // ================================================================
        // Common HLSL
        // ================================================================

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
        #include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Lightng/ToonLighting.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_DissolveTex);
        SAMPLER(sampler_DissolveTex);

        CBUFFER_START(UnityPerMaterial)

            float4 _Color;
            float4 _MainTex_ST;
            float4 _DissolveTex_ST;
            float4 _DissolveEdgeColor;
            float _DissolveAmount;
            float _DissolveEdgeWidth;
            float _DissolveEdgeIntensity;

            half _Metallic;
            half _Smoothness;
            half _DiffuseSteps;
            half _DiffuseDarkStrength;
            half _DiffuseSoftness;
            half _FlatDiffuseNormals;
            half _DiffuseAmbientStrength;
            half4 _SpecularColor;
            half _SpecularIntensity;
            half4 _RimColor;
            half _RimIntensity;
            half _RimPower;


        CBUFFER_END

        // All geometry passes share the same UVs, threshold and endpoint behavior.
        // Black texels disappear first. 0 keeps every texel; 1 removes every texel.
        float ApplyDissolve(float2 dissolveUV)
        {
            if (_DissolveAmount <= 0.0)
                return 0.0;
            if (_DissolveAmount >= 1.0)
            {
                clip(-1.0);
                return 0.0;
            }

            float mask = saturate(SAMPLE_TEXTURE2D(
                _DissolveTex, sampler_DissolveTex, dissolveUV).r);
            float remaining = mask - _DissolveAmount;
            clip(remaining);

            // Width is measured in mask values, not world units.
            return _DissolveEdgeWidth > 0.0
                ? 1.0 - smoothstep(0.0, max(_DissolveEdgeWidth, 0.00001), remaining)
                : 0.0;
        }


        // ================================================================
        // Vertex
        // ================================================================

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float2 uv         : TEXCOORD0;

            UNITY_VERTEX_INPUT_INSTANCE_ID
        };


        struct Varyings
        {
            float4 positionCS : SV_POSITION;

            float3 positionWS : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
            float2 uv         : TEXCOORD2;

            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };


        Varyings Vert(Attributes input)
        {
            Varyings output;

            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            VertexPositionInputs positionInputs =
                GetVertexPositionInputs(input.positionOS.xyz);

            VertexNormalInputs normalInputs =
                GetVertexNormalInputs(input.normalOS);

            output.positionCS =
                positionInputs.positionCS;

            output.positionWS =
                positionInputs.positionWS;

            output.normalWS =
                NormalizeNormalPerVertex(
                    normalInputs.normalWS
                );

            output.uv =
                TRANSFORM_TEX(input.uv, _MainTex);

            return output;
        }


        // ================================================================
        // Universal Forward
        //
        // Color�� Diffuse�� Toon
        // Specular / Metallic / Smoothness / GI / Shadow ���� Lit
        // ================================================================

        struct ForwardVaryings
        {
            float4 positionCS : SV_POSITION;

            float3 positionWS : TEXCOORD0;
            float3 normalWS   : TEXCOORD1;
            float2 uv         : TEXCOORD2;

            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
            float2 dissolveUV : TEXCOORD3;
        };


        ForwardVaryings ForwardVert(Attributes input)
        {
            ForwardVaryings output;

            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            VertexPositionInputs positionInputs =
                GetVertexPositionInputs(input.positionOS.xyz);

            VertexNormalInputs normalInputs =
                GetVertexNormalInputs(input.normalOS);

            output.positionCS =
                positionInputs.positionCS;

            output.positionWS =
                positionInputs.positionWS;

            output.normalWS =
                NormalizeNormalPerVertex(
                    normalInputs.normalWS
                );

            output.uv =
                TRANSFORM_TEX(input.uv, _MainTex);

            output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);

            return output;
        }


        half3 EvaluateAdditionalToonDiffuseLight(Light light, half3 normalWS, half3 diffuseNormalWS,
            half3 viewDirectionWS, half3 baseColor, half3 F0, half specularPower, half3 rimColor)
        {
            half toonDiffuse = ProjectLToonDiffuse(dot(diffuseNormalWS, light.direction),
                _DiffuseSteps, _DiffuseDarkStrength, _DiffuseSoftness);

            half3 halfDirection = SafeNormalize(light.direction + viewDirectionWS);
            half specular = pow(saturate(dot(normalWS, halfDirection)), specularPower);
            half3 fresnel = F0 + (1.0h - F0) *
                pow(1.0h - saturate(dot(halfDirection, viewDirectionWS)), 5.0h);
            half3 specularColor = specular * fresnel * saturate(dot(normalWS, light.direction)) *
                _SpecularColor.rgb * _SpecularIntensity;

            // 림은 기존 보간 노멀에서 빛을 받는 면에만 적용한다.
            half litMask = saturate(dot(normalWS, light.direction));
            return (baseColor * toonDiffuse + specularColor + rimColor * litMask) * light.color *
                (light.distanceAttenuation * light.shadowAttenuation);
        }

        half4 ForwardFrag(ForwardVaryings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float dissolveEdge = ApplyDissolve(input.dissolveUV);

            // ------------------------------------------------------------
            // Texture Color
            // ------------------------------------------------------------

            half4 texColor =
                SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv
                );

            half3 baseColor =
                texColor.rgb * _Color.rgb;

            half alpha =
                texColor.a * _Color.a;


            // ------------------------------------------------------------
            // Normal
            // ------------------------------------------------------------

            half3 normalWS =
                normalize(input.normalWS);
            half3 diffuseNormalWS = ProjectLFlatDiffuseNormal(input.positionWS,
                normalWS, _FlatDiffuseNormals);


            // ------------------------------------------------------------
            // Main Light
            // ------------------------------------------------------------

            #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(input.positionWS));
            #else
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #endif

            Light mainLight =
                GetMainLight(shadowCoord);


            // ============================================================
            // 기본색의 명암은 면 노멀을 기준으로 고정된 단계만 사용한다.
            // ============================================================

            half NdotL =
                saturate(
                    dot(
                        normalWS,
                        mainLight.direction
                    )
                );

            half toonDiffuse = ProjectLToonDiffuse(dot(diffuseNormalWS, mainLight.direction),
                _DiffuseSteps, _DiffuseDarkStrength, _DiffuseSoftness);
            half attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;


            // ============================================================
            // Lit Specular
            //
            // Specular�� Toonȭ���� ����
            // ============================================================

            half3 viewDirectionWS =
                GetWorldSpaceNormalizeViewDir(
                    input.positionWS
                );

            // View-dependent silhouette; direct light determines where it is visible.
            half rim = pow(1.0h - saturate(dot(normalWS, viewDirectionWS)), max(_RimPower, 0.001h));
            half3 rimColor = _RimColor.rgb * (_RimIntensity * rim);

            half3 halfDirection =
                SafeNormalize(
                    mainLight.direction +
                    viewDirectionWS
                );

            half NdotH =
                saturate(
                    dot(
                        normalWS,
                        halfDirection
                    )
                );

            half smoothness =
                _Smoothness;

            half perceptualRoughness =
                1.0h - smoothness;

            half roughness =
                perceptualRoughness *
                perceptualRoughness;

            half roughness2 =
                max(
                    roughness * roughness,
                    0.002h
                );

            half specularPower =
                max(
                    1.0h,
                    (2.0h / roughness2) - 2.0h
                );

            half specular =
                pow(
                    NdotH,
                    specularPower
                );


            // Metallic�� ���� F0
            half3 dielectricSpecular =
                0.04h.xxx;

            half3 F0 =
                lerp(
                    dielectricSpecular,
                    baseColor,
                    _Metallic
                );

            half3 fresnel =
                F0 +
                (1.0h - F0) *
                pow(
                    1.0h - saturate(dot(
                        halfDirection,
                        viewDirectionWS
                    )),
                    5.0h
                );

            half3 specularColor =
                specular *
                fresnel *
                mainLight.color *
                NdotL *
                _SpecularColor.rgb *
                _SpecularIntensity;


            // ============================================================
            // GI / Ambient
            // ============================================================

            float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            // URP returns neutral factors when SSAO is disabled (including After Opaque).
            AmbientOcclusionFactor ao = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);

            half3 indirectDiffuse =
                SampleSH(diffuseNormalWS);

            half3 ambient =
                indirectDiffuse *
                baseColor * _DiffuseAmbientStrength;


            // ============================================================
            // Final
            // ============================================================

            half3 finalColor =
                baseColor *
                mainLight.color *
                toonDiffuse * attenuation;

            finalColor += specularColor * attenuation;
            finalColor += rimColor * NdotL * mainLight.color * attenuation;

            // 정점광 모드도 픽셀에서 평가해 기본색 명암 단계와 기존 반사광을 유지한다.
            #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX) || USE_FORWARD_PLUS
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = normalizedScreenSpaceUV;
                half4 shadowMask = half4(1, 1, 1, 1);

                #if USE_FORWARD_PLUS
                    UNITY_LOOP for (uint lightIndex = 0;
                        lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); ++lightIndex)
                    {
                        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                        Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                        finalColor += EvaluateAdditionalToonDiffuseLight(light, normalWS, diffuseNormalWS,
                            viewDirectionWS, baseColor, F0, specularPower, rimColor);
                    }
                #endif

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light light = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                    finalColor += EvaluateAdditionalToonDiffuseLight(light, normalWS, diffuseNormalWS,
                        viewDirectionWS, baseColor, F0, specularPower, rimColor);
                LIGHT_LOOP_END
            #endif

            // Apply Direct Lighting Strength to all direct lights, including lit rim/specular.
            finalColor *= ao.directAmbientOcclusion;
            finalColor += ambient * ao.indirectAmbientOcclusion;
            // An emissive border remains visible even in shadow.
            finalColor = lerp(finalColor,
                _DissolveEdgeColor.rgb * _DissolveEdgeIntensity, dissolveEdge);

            return half4(
                finalColor,
                alpha
            );
        }


        // ================================================================
        // ShadowCaster
        // ================================================================

        // Set by URP for each shadow-casting light (not the camera's main light).
        float3 _LightDirection;
        float3 _LightPosition;

        struct ShadowVaryings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            float2 dissolveUV : TEXCOORD0;
        };


        ShadowVaryings ShadowVert(Attributes input)
        {
            ShadowVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);

            float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
            float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

            #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

            output.positionCS = TransformWorldToHClip(
                ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            // Preserve directional-light casters crossing the shadow near plane.
            output.positionCS = ApplyShadowClamping(output.positionCS);

            output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);

            return output;
        }


        half4 ShadowFrag(ShadowVaryings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            ApplyDissolve(input.dissolveUV);
            return 0;
        }


        // ================================================================
        // DepthOnly
        // ================================================================

        struct DepthVaryings
        {
            float4 positionCS : SV_POSITION;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
            float2 dissolveUV : TEXCOORD0;
        };


        DepthVaryings DepthVert(Attributes input)
        {
            DepthVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            VertexPositionInputs positionInputs =
                GetVertexPositionInputs(
                    input.positionOS.xyz
                );

            output.positionCS =
                positionInputs.positionCS;

            output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);

            return output;
        }


        half DepthFrag(DepthVaryings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            ApplyDissolve(input.dissolveUV);
            return input.positionCS.z;
        }


        // ================================================================
        // NormalOnly
        //
        // Sobel ����
        //
        // Color �� World Normal
        // Depth �� ZWrite
        // ================================================================

        struct NormalVaryings
        {
            float4 positionCS : SV_POSITION;
            float3 normalWS   : TEXCOORD0;

            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
            float2 dissolveUV : TEXCOORD1;
        };


        NormalVaryings NormalVert(Attributes input)
        {
            NormalVaryings output;

            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            VertexPositionInputs positionInputs =
                GetVertexPositionInputs(
                    input.positionOS.xyz
                );

            VertexNormalInputs normalInputs =
                GetVertexNormalInputs(
                    input.normalOS
                );

            output.positionCS =
                positionInputs.positionCS;

            output.normalWS =
                NormalizeNormalPerVertex(
                    normalInputs.normalWS
                );

            output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);

            return output;
        }


        half4 NormalFrag(NormalVaryings input) : SV_Target
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            ApplyDissolve(input.dissolveUV);

            half3 normalWS =
                normalize(input.normalWS);

            // World Normal
            // [-1, 1] �� [0, 1]

            half3 encodedNormal =
                normalWS * 0.5h + 0.5h;

            return half4(
                encodedNormal,
                1.0h
            );
        }

        // URP camera normals use signed world normals or octahedral packing,
        // unlike the custom NormalOnly buffer's [0, 1] encoding.
        void DepthNormalsFrag(NormalVaryings input,
            out half4 outNormalWS : SV_Target0
            #if defined(_WRITE_RENDERING_LAYERS)
                , out float4 outRenderingLayers : SV_Target1
            #endif
        )
        {
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            ApplyDissolve(input.dissolveUV);
            float3 normalWS = NormalizeNormalPerPixel(input.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 octNormal = PackNormalOctQuadEncode(normalWS);
                half3 packedNormal = PackFloat2To888(saturate(octNormal * 0.5 + 0.5));
                outNormalWS = half4(packedNormal, 0.0);
            #else
                outNormalWS = half4(normalWS, 0.0);
            #endif
            #if defined(_WRITE_RENDERING_LAYERS)
                outRenderingLayers = float4(EncodeMeshRenderingLayer(GetMeshRenderingLayer()), 0, 0, 0);
            #endif
        }


        // ================================================================
        // Meta
        // ================================================================

        struct MetaVaryings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float2 dissolveUV : TEXCOORD1;
        };


        MetaVaryings MetaVert(Attributes input)
        {
            MetaVaryings output;

            VertexPositionInputs positionInputs =
                GetVertexPositionInputs(
                    input.positionOS.xyz
                );

            output.positionCS =
                positionInputs.positionCS;

            output.uv =
                TRANSFORM_TEX(
                    input.uv,
                    _MainTex
                );

            output.dissolveUV = TRANSFORM_TEX(input.uv, _DissolveTex);

            return output;
        }


        half4 MetaFrag(MetaVaryings input) : SV_Target
        {
            ApplyDissolve(input.dissolveUV);
            half4 texColor =
                SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    input.uv
                );

            half3 albedo =
                texColor.rgb *
                _Color.rgb;

            return half4(
                albedo,
                1.0h
            );
        }

        ENDHLSL


        // ================================================================
        // 1. Universal Forward
        // ================================================================

        Pass
        {
            Name "UniversalForward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex ForwardVert
            #pragma fragment ForwardFrag

            #pragma multi_compile_instancing

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _SHADOWS_SOFT

            ENDHLSL
        }


        // ================================================================
        // 2. ShadowCaster
        // ================================================================

        Pass
        {
            Name "ShadowCaster"

            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            ENDHLSL
        }


        // ================================================================
        // 3. DepthOnly
        // ================================================================

        Pass
        {
            Name "DepthOnly"

            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ZTest LEqual
            ColorMask R

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex DepthVert
            #pragma fragment DepthFrag

            #pragma multi_compile_instancing

            ENDHLSL
        }


        // ================================================================
        // URP depth/normal prepass (_CameraNormalsTexture).
        // ================================================================

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex NormalVert
            #pragma fragment DepthNormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            ENDHLSL
        }

        // ================================================================
        // 4. NormalOnly (custom consumers)
        // ================================================================

        Pass
        {
            Name "NormalOnly"

            Tags
            {
                "LightMode" = "NormalOnly"
            }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex NormalVert
            #pragma fragment NormalFrag

            #pragma multi_compile_instancing

            ENDHLSL
        }


        // ================================================================
        // 5. Meta
        // ================================================================

        Pass
        {
            Name "Meta"

            Tags
            {
                "LightMode" = "Meta"
            }

            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex MetaVert
            #pragma fragment MetaFrag

            ENDHLSL
        }
    }
}
