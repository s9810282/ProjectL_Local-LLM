Shader "ProjectL/FakeGI"
{
    Properties
    {
        // ================================================================
        // 빛 설정
        // ================================================================

        [Header(Light Settings)]

        [HDR]
        _LightTint ("빛 색상", Color) = (1, 1, 1, 1)

        _LightIntensity ("빛 세기", Float) = 1.0

        _LightRadius ("빛 반경", Float) = 5.0

        _LightSoftness ("빛 가장자리 부드러움", Range(0, 1)) = 0.5

        _LightFalloff ("빛 감쇠", Range(0.1, 8.0)) = 2.0

        _LightPosterize ("빛 단계화", Range(0, 1)) = 0.0

        _GradientTex ("빛 색상 그라디언트", 2D) = "white" {}


        // ================================================================
        // Directional Light 연동
        // ================================================================

        [Header(Directional Light)]

        [Toggle]
        _UseDirectionalLightAttenuation
        (
            "방향광에 따른 감쇠 사용",
            Float
        ) = 1

        _DirectionalLightThreshold
        (
            "방향광 감쇠 시작점",
            Range(0, 1)
        ) = 0.25


        // ================================================================
        // 표면 음영
        // ================================================================

        [Header(Shading)]

        [Toggle]
        _EnableShading ("표면 음영 사용", Float) = 0

        _ShadingBlend ("음영 혼합", Range(0, 1)) = 0.0

        _ShadingSoftness ("음영 부드러움", Range(0, 1)) = 0.5


        // ================================================================
        // 후광
        // ================================================================

        [Header(Halo)]

        [Toggle]
        _EnableHalo ("후광 사용", Float) = 1

        [HDR]
        _HaloTint ("후광 색상", Color) = (1, 1, 1, 1)

        _HaloSize ("후광 크기", Range(0, 1)) = 0.5

        _HaloPosterize ("후광 단계화", Range(0, 1)) = 0.0

        _HaloDepthFade ("후광 깊이 감쇠", Range(0, 1)) = 0.5


        // ================================================================
        // 거리 페이드
        // ================================================================

        [Header(Distance Fade)]

        _CloseFade ("근거리 페이드 시작", Float) = 0.0

        _CloseTransition ("근거리 페이드 구간", Float) = 0.5

        _FarFade ("원거리 페이드 시작", Float) = 5.0

        _FarTransition ("원거리 페이드 구간", Float) = 1.0


        // ================================================================
        // 깜빡임
        // ================================================================

        [Header(Flicker)]

        [Toggle]
        _EnableFlicker ("깜빡임 사용", Float) = 0

        _FlickerIntensity ("깜빡임 세기", Range(0, 1)) = 0.1

        [HDR]
        _FlickerHue ("깜빡임 색상", Color) = (1, 0.8, 0.5, 1)

        _FlickerSpeed ("깜빡임 속도", Float) = 5.0

        _FlickerSoftness ("깜빡임 부드러움", Range(0, 1)) = 0.5

        _SizeFlickering ("크기 깜빡임", Range(0, 1)) = 0.0


        // ================================================================
        // 노이즈
        // ================================================================

        [Header(Noise)]

        [Toggle]
        _EnableNoise ("노이즈 사용", Float) = 0

        _NoiseTex ("노이즈 텍스처", 2D) = "white" {}

        _Noisiness ("노이즈 세기", Range(0, 1)) = 0.2

        _NoiseScale ("노이즈 크기", Float) = 1.0

        _NoiseMovement ("노이즈 이동 속도", Float) = 0.0


        // ================================================================
        // 디더링
        // ================================================================

        [Header(Dithering)]

        [Toggle]
        _EnableDither ("디더링 사용", Float) = 0

        _DitherIntensity ("디더링 세기", Range(0, 1)) = 0.1


        // ================================================================
        // 반사광
        // ================================================================

        [Header(Specular)]

        [Toggle]
        _EnableSpecular ("반사광 사용", Float) = 0

        _SpecularIntensity ("반사광 세기", Range(0, 1)) = 0.25

        _SpecularPower ("반사광 집중도", Range(1, 128)) = 32


        // ================================================================
        // 기타
        // ================================================================

        [Header(Extra)]

        [Toggle]
        _AccurateColors ("실제 색상 기반 조명", Float) = 1


        // ================================================================
        // 중앙 발광 구체
        // ================================================================

        [Header(Center Core)]

        [Toggle]
        _CoreEnabled ("중앙 구체 표시", Float) = 1

        _CoreRadius ("중앙 구체 크기", Range(0.001, 2.0)) = 0.2

        [HDR]
        _CoreColor ("중앙 구체 색상", Color) = (1, 1, 1, 1)

        _CoreIntensity ("중앙 구체 밝기", Range(0, 50)) = 8.0

        _CoreSoftness ("중앙 구체 부드러움", Range(0, 1)) = 0.5

        _CoreRim ("중앙 구체 가장자리", Range(0, 1)) = 0.25

        _CoreRimPower ("중앙 구체 가장자리 강도", Range(0.1, 8)) = 2.0
    }


    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }


        // ====================================================================
        // PASS 1 : FakeGI
        // ====================================================================

        Pass
        {
            Name "FakeGI"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Off
            ZWrite Off
            ZTest Always

            Blend One One
            ColorMask RGB


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"


            // ============================================================
            // 공유 텍스처
            // ============================================================

            TEXTURE2D(_GradientTex);
            SAMPLER(sampler_GradientTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);


            CBUFFER_START(UnityPerMaterial)

                float4 _GradientTex_ST;
                float4 _NoiseTex_ST;

            CBUFFER_END


            // ============================================================
            // 인스턴스 데이터
            // ============================================================

            UNITY_INSTANCING_BUFFER_START(FakeGIProps)

                UNITY_DEFINE_INSTANCED_PROP(float4, _LightTint)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightRadius)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightSoftness)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightFalloff)
                UNITY_DEFINE_INSTANCED_PROP(float, _LightPosterize)

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _UseDirectionalLightAttenuation
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _DirectionalLightThreshold
                )

                UNITY_DEFINE_INSTANCED_PROP(float, _EnableShading)
                UNITY_DEFINE_INSTANCED_PROP(float, _ShadingBlend)
                UNITY_DEFINE_INSTANCED_PROP(float, _ShadingSoftness)

                UNITY_DEFINE_INSTANCED_PROP(float, _EnableHalo)
                UNITY_DEFINE_INSTANCED_PROP(float4, _HaloTint)
                UNITY_DEFINE_INSTANCED_PROP(float, _HaloSize)
                UNITY_DEFINE_INSTANCED_PROP(float, _HaloPosterize)
                UNITY_DEFINE_INSTANCED_PROP(float, _HaloDepthFade)

                UNITY_DEFINE_INSTANCED_PROP(float, _CloseFade)
                UNITY_DEFINE_INSTANCED_PROP(float, _CloseTransition)
                UNITY_DEFINE_INSTANCED_PROP(float, _FarFade)
                UNITY_DEFINE_INSTANCED_PROP(float, _FarTransition)

                UNITY_DEFINE_INSTANCED_PROP(float, _EnableFlicker)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlickerIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float4, _FlickerHue)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlickerSpeed)
                UNITY_DEFINE_INSTANCED_PROP(float, _FlickerSoftness)
                UNITY_DEFINE_INSTANCED_PROP(float, _SizeFlickering)

                UNITY_DEFINE_INSTANCED_PROP(float, _EnableNoise)
                UNITY_DEFINE_INSTANCED_PROP(float, _Noisiness)
                UNITY_DEFINE_INSTANCED_PROP(float, _NoiseScale)
                UNITY_DEFINE_INSTANCED_PROP(float, _NoiseMovement)

                UNITY_DEFINE_INSTANCED_PROP(float, _EnableDither)
                UNITY_DEFINE_INSTANCED_PROP(float, _DitherIntensity)

                UNITY_DEFINE_INSTANCED_PROP(float, _EnableSpecular)
                UNITY_DEFINE_INSTANCED_PROP(float, _SpecularIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _SpecularPower)

                UNITY_DEFINE_INSTANCED_PROP(float, _AccurateColors)

                // 중앙 구체
                UNITY_DEFINE_INSTANCED_PROP(float, _CoreEnabled)
                UNITY_DEFINE_INSTANCED_PROP(float, _CoreRadius)
                UNITY_DEFINE_INSTANCED_PROP(float4, _CoreColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _CoreIntensity)
                UNITY_DEFINE_INSTANCED_PROP(float, _CoreSoftness)
                UNITY_DEFINE_INSTANCED_PROP(float, _CoreRim)
                UNITY_DEFINE_INSTANCED_PROP(float, _CoreRimPower)

            UNITY_INSTANCING_BUFFER_END(FakeGIProps)


            struct Attributes
            {
                float3 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            Varyings Vert(
                Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(
                    input,
                    output
                );

                output.positionCS =
                    TransformObjectToHClip(
                        input.positionOS
                    );

                return output;
            }


            float Posterize(
                float value,
                float amount)
            {
                const float steps = 6.0;

                float posterized =
                    floor(
                        value * steps
                    ) / steps;

                return lerp(
                    value,
                    posterized,
                    amount
                );
            }


            float Hash21(float2 p)
            {
                p =
                    frac(
                        p *
                        float2(
                            123.34,
                            345.45
                        )
                    );

                p +=
                    dot(
                        p,
                        p + 34.345
                    );

                return frac(
                    p.x * p.y
                );
            }


            float CalculateFade(
                float distanceWS,
                float closeFade,
                float closeTransition,
                float farFade,
                float farTransition)
            {
                float fade = 1.0;


                if (closeTransition > 0.0001)
                {
                    float closeValue =
                        saturate(
                            (distanceWS - closeFade) /
                            closeTransition
                        );

                    fade *=
                        closeValue;
                }


                if (farTransition > 0.0001)
                {
                    float farValue =
                        1.0 -
                        saturate(
                            (distanceWS - farFade) /
                            farTransition
                        );

                    fade *=
                        farValue;
                }


                return fade;
            }


            // ============================================================
            // Directional Light 방향 기반 감쇠
            //
            // Unity Light의 transform.forward 기준으로:
            //
            // -1 = 아래
            //  0 = 수평
            // +1 = 위
            //
            // 아래에서는 0
            // 위로 갈수록 1
            // ============================================================

            float CalculateDirectionalFactor(
                float mainLightDirectionY,
                float threshold)
            {
                // URP의 mainLight.direction은
                // 광원 -> 씬 방향이 아니라
                // 표면 -> 광원 방향이다.
                //
                // 따라서 Unity Light의 실제 forward 방향 기준
                // 위쪽 정도를 얻으려면 부호를 반전한다.

                float upward =
                    saturate(
                        -mainLightDirectionY
                    );


                threshold =
                    saturate(
                        threshold
                    );


                // threshold 이하에서는 FakeGI가 보이지 않는다.
                // threshold를 넘어 위로 향할수록
                // 원본값에 접근한다.

                return smoothstep(
                    threshold,
                    1.0,
                    upward
                );
            }


            half4 Frag(
                Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


                // ========================================================
                // 기본 설정
                // ========================================================

                float4 lightTint =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _LightTint
                    );


                float baseIntensity =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _LightIntensity
                    );


                float baseRadius =
                    max(
                        UNITY_ACCESS_INSTANCED_PROP(
                            FakeGIProps,
                            _LightRadius
                        ),
                        0.001
                    );


                float softness =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _LightSoftness
                    );


                float falloff =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _LightFalloff
                    );


                float posterize =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _LightPosterize
                    );


                // ========================================================
                // Directional Light 설정
                // ========================================================

                float useDirectional =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _UseDirectionalLightAttenuation
                    );


                float directionalThreshold =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _DirectionalLightThreshold
                    );


                // ========================================================
                // 기본값
                //
                // 감쇠 사용 OFF이면 무조건 원본값.
                // ========================================================

                float directionalFactor = 1.0;


                if (useDirectional > 0.5)
                {
                    Light mainLight =
                        GetMainLight();


                    directionalFactor =
                        CalculateDirectionalFactor(
                            mainLight.direction.y,
                            directionalThreshold
                        );
                }


                // ========================================================
                // 방향광에 의한 반경 / 밝기 감쇠
                // ========================================================

                float effectiveRadius =
                    baseRadius *
                    directionalFactor;


                float intensity =
                    baseIntensity *
                    directionalFactor;


                if (effectiveRadius <= 0.0001)
                    discard;


                if (intensity <= 0.0001)
                    discard;


                // ========================================================
                // 기타 설정
                // ========================================================

                float enableShading =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _EnableShading
                    );


                float shadingBlend =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _ShadingBlend
                    );


                float shadingSoftness =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _ShadingSoftness
                    );


                float enableHalo =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _EnableHalo
                    );


                float4 haloTint =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _HaloTint
                    );


                float haloSize =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _HaloSize
                    );


                float haloPosterize =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _HaloPosterize
                    );


                float haloDepthFade =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _HaloDepthFade
                    );


                float closeFade =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _CloseFade
                    );


                float closeTransition =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _CloseTransition
                    );


                float farFade =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _FarFade
                    );


                float farTransition =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _FarTransition
                    );


                float enableFlicker =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _EnableFlicker
                    );


                float flickerIntensity =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _FlickerIntensity
                    );


                float4 flickerHue =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _FlickerHue
                    );


                float flickerSpeed =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _FlickerSpeed
                    );


                float flickerSoftness =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _FlickerSoftness
                    );


                float sizeFlickering =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _SizeFlickering
                    );


                float enableNoise =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _EnableNoise
                    );


                float noisiness =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _Noisiness
                    );


                float noiseScale =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _NoiseScale
                    );


                float noiseMovement =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _NoiseMovement
                    );


                float enableDither =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _EnableDither
                    );


                float ditherIntensity =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _DitherIntensity
                    );


                float enableSpecular =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _EnableSpecular
                    );


                float specularIntensity =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _SpecularIntensity
                    );


                float specularPower =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _SpecularPower
                    );


                float accurateColors =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _AccurateColors
                    );


                // ========================================================
                // 화면 UV
                // ========================================================

                float2 uv =
                    GetNormalizedScreenSpaceUV(
                        input.positionCS
                    );


                // ========================================================
                // Depth
                // ========================================================

                float rawDepth =
                    SampleSceneDepth(
                        uv
                    );


                #if UNITY_REVERSED_Z

                    if (rawDepth <= 0.00001)
                        discard;

                #else

                    if (rawDepth >= 0.99999)
                        discard;

                #endif


                // ========================================================
                // Depth → World Position
                // ========================================================

                float3 surfacePosWS =
                    ComputeWorldSpacePosition(
                        uv,
                        rawDepth,
                        UNITY_MATRIX_I_VP
                    );


                // ========================================================
                // FakeGI 중심
                // ========================================================

                float3 lightPosWS =
                    TransformObjectToWorld(
                        float3(
                            0.0,
                            0.0,
                            0.0
                        )
                    );


                // ========================================================
                // 거리 / 방향
                // ========================================================

                float3 lightVector =
                    lightPosWS -
                    surfacePosWS;


                float distanceWS =
                    length(
                        lightVector
                    );


                if (distanceWS >= effectiveRadius)
                    discard;


                float3 lightDir =
                    lightVector /
                    max(
                        distanceWS,
                        0.0001
                    );


                // ========================================================
                // Flicker
                // ========================================================

                float flickerValue = 0.0;


                if (enableFlicker > 0.5)
                {
                    float wave =
                        sin(
                            _Time.y *
                            flickerSpeed
                        );


                    wave =
                        wave * 0.5 +
                        0.5;


                    wave =
                        smoothstep(
                            0.0,
                            max(
                                flickerSoftness,
                                0.001
                            ),
                            wave
                        );


                    flickerValue =
                        wave *
                        flickerIntensity;


                    intensity *=
                        1.0 +
                        flickerValue;


                    lightTint.rgb =
                        lerp(
                            lightTint.rgb,
                            flickerHue.rgb,
                            flickerValue
                        );
                }


                float flickerRadius =
                    effectiveRadius;


                if (enableFlicker > 0.5)
                {
                    flickerRadius *=
                        1.0 +
                        flickerValue *
                        sizeFlickering;
                }


                float normalizedDistance =
                    saturate(
                        distanceWS /
                        max(
                            flickerRadius,
                            0.0001
                        )
                    );


                // ========================================================
                // 거리 감쇠
                // ========================================================

                float attenuation =
                    saturate(
                        1.0 -
                        normalizedDistance
                    );


                attenuation =
                    pow(
                        attenuation,
                        max(
                            falloff,
                            0.001
                        )
                    );


                // ========================================================
                // 가장자리 부드러움
                // ========================================================

                if (softness > 0.0001)
                {
                    float edge =
                        saturate(
                            (1.0 -
                            normalizedDistance) /
                            softness
                        );


                    edge =
                        smoothstep(
                            0.0,
                            1.0,
                            edge
                        );


                    attenuation *=
                        edge;
                }


                // ========================================================
                // 거리 Fade
                // ========================================================

                attenuation *=
                    CalculateFade(
                        distanceWS,
                        closeFade,
                        closeTransition,
                        farFade,
                        farTransition
                    );


                // ========================================================
                // Gradient
                // ========================================================

                float3 gradientColor =
                    SAMPLE_TEXTURE2D(
                        _GradientTex,
                        sampler_GradientTex,
                        float2(
                            normalizedDistance,
                            0.5
                        )
                    ).rgb;


                lightTint.rgb *=
                    gradientColor;


                // ========================================================
                // Noise
                // ========================================================

                if (enableNoise > 0.5)
                {
                    float2 noiseUV =
                        uv *
                        noiseScale;


                    noiseUV.x +=
                        _Time.y *
                        noiseMovement;


                    float noise =
                        SAMPLE_TEXTURE2D(
                            _NoiseTex,
                            sampler_NoiseTex,
                            noiseUV
                        ).r;


                    noise =
                        lerp(
                            1.0,
                            noise,
                            noisiness
                        );


                    attenuation *=
                        noise;
                }


                // ========================================================
                // 표면 음영
                // ========================================================

                float shading = 1.0;


                if (enableShading > 0.5)
                {
                    float3 normalWS =
                        normalize(
                            SampleSceneNormals(uv)
                        );


                    float NdotL =
                        saturate(
                            dot(
                                normalWS,
                                lightDir
                            )
                        );


                    if (shadingSoftness > 0.0001)
                    {
                        NdotL =
                            smoothstep(
                                0.0,
                                shadingSoftness,
                                NdotL
                            );
                    }


                    shading =
                        lerp(
                            NdotL,
                            1.0,
                            shadingBlend
                        );
                }


                // ========================================================
                // 메인 FakeGI
                // ========================================================

                float mainIntensity =
                    attenuation *
                    shading *
                    intensity;


                mainIntensity =
                    Posterize(
                        mainIntensity,
                        posterize
                    );


                float3 mainLight =
                    lightTint.rgb *
                    mainIntensity;


                // ========================================================
                // Halo
                // ========================================================

                float halo = 0.0;


                if (enableHalo > 0.5)
                {
                    float haloRadius =
                        lerp(
                            0.15,
                            1.0,
                            haloSize
                        );


                    halo =
                        1.0 -
                        smoothstep(
                            0.0,
                            haloRadius,
                            normalizedDistance
                        );


                    halo =
                        Posterize(
                            halo,
                            haloPosterize
                        );


                    if (haloDepthFade > 0.0001)
                    {
                        float3 lightView =
                            TransformWorldToView(
                                lightPosWS
                            );


                        float3 surfaceView =
                            TransformWorldToView(
                                surfacePosWS
                            );


                        float depthDifference =
                            abs(
                                lightView.z -
                                surfaceView.z
                            );


                        float depthFade =
                            1.0 -
                            saturate(
                                depthDifference /
                                max(
                                    effectiveRadius *
                                    haloDepthFade,
                                    0.0001
                                )
                            );


                        halo *=
                            depthFade;
                    }
                }


                float3 haloLight =
                    haloTint.rgb *
                    halo;


                // ========================================================
                // Specular
                // ========================================================

                float3 specularColor =
                    0.0;


                if (enableSpecular > 0.5 &&
                    enableShading > 0.5)
                {
                    float3 normalWS =
                        normalize(
                            SampleSceneNormals(uv)
                        );


                    float3 viewDirWS =
                        normalize(
                            GetWorldSpaceViewDir(
                                surfacePosWS
                            )
                        );


                    float3 reflected =
                        reflect(
                            -lightDir,
                            normalWS
                        );


                    float spec =
                        pow(
                            saturate(
                                dot(
                                    reflected,
                                    viewDirWS
                                )
                            ),
                            specularPower
                        );


                    specularColor =
                        lightTint.rgb *
                        spec *
                        specularIntensity;
                }


                // ========================================================
                // 최종 FakeGI
                // ========================================================

                float3 fakeLight =
                    mainLight +
                    haloLight +
                    specularColor;


                // ========================================================
                // 실제 씬 색상 기반
                // ========================================================

                float3 contribution;


                if (accurateColors > 0.5)
                {
                    half3 sceneColor =
                        SampleSceneColor(
                            uv
                        );


                    contribution =
                        sceneColor *
                        fakeLight;
                }
                else
                {
                    contribution =
                        fakeLight;
                }


                // ========================================================
                // Dither
                // ========================================================

                if (enableDither > 0.5)
                {
                    float dither =
                        Hash21(
                            input.positionCS.xy
                        );


                    float luminance =
                        dot(
                            contribution,
                            float3(
                                0.2126,
                                0.7152,
                                0.0722
                            )
                        );


                    float threshold =
                        lerp(
                            1.0,
                            dither,
                            ditherIntensity
                        );


                    if (luminance <= threshold)
                        discard;
                }


                return half4(
                    contribution,
                    1.0
                );
            }

            ENDHLSL
        }


        // ====================================================================
        // PASS 2 : 중앙 발광 구체
        // ====================================================================

        Pass
        {
            Name "CenterCore"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit"
            }

            Cull Back
            ZWrite Off
            ZTest LEqual

            Blend One One
            ColorMask RGB


            HLSLPROGRAM

            #pragma target 4.5

            #pragma vertex CoreVert
            #pragma fragment CoreFrag

            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"


            // ============================================================
            // 인스턴스 데이터
            // ============================================================

            UNITY_INSTANCING_BUFFER_START(FakeGIProps)

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _LightRadius
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _UseDirectionalLightAttenuation
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _DirectionalLightThreshold
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _CoreEnabled
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _CoreRadius
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float4,
                    _CoreColor
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _CoreIntensity
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _CoreSoftness
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _CoreRim
                )

                UNITY_DEFINE_INSTANCED_PROP(
                    float,
                    _CoreRimPower
                )

            UNITY_INSTANCING_BUFFER_END(FakeGIProps)


            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            // ============================================================
            // Directional Light 기반 감쇠
            // ============================================================

            float CalculateDirectionalFactor(
                float mainLightDirectionY,
                float threshold)
            {
                float upward =
                    saturate(
                        -mainLightDirectionY
                    );


                threshold =
                    saturate(
                        threshold
                    );


                return smoothstep(
                    threshold,
                    1.0,
                    upward
                );
            }


            // ============================================================
            // Vertex
            // ============================================================

            Varyings CoreVert(
                Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);

                UNITY_TRANSFER_INSTANCE_ID(
                    input,
                    output
                );


                float enabled =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _CoreEnabled
                    );


                if (enabled <= 0.5)
                {
                    output.positionCS =
                        float4(
                            0.0,
                            0.0,
                            -2.0,
                            1.0
                        );

                    return output;
                }


                // ========================================================
                // FakeGI 기본 반경
                // ========================================================

                float lightRadius =
                    max(
                        UNITY_ACCESS_INSTANCED_PROP(
                            FakeGIProps,
                            _LightRadius
                        ),
                        0.001
                    );


                float coreRadius =
                    max(
                        UNITY_ACCESS_INSTANCED_PROP(
                            FakeGIProps,
                            _CoreRadius
                        ),
                        0.001
                    );


                // ========================================================
                // Directional Light
                // ========================================================

                float useDirectional =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _UseDirectionalLightAttenuation
                    );


                float directionalFactor =
                    1.0;


                if (useDirectional > 0.5)
                {
                    Light mainLight =
                        GetMainLight();


                    float threshold =
                        UNITY_ACCESS_INSTANCED_PROP(
                            FakeGIProps,
                            _DirectionalLightThreshold
                        );


                    directionalFactor =
                        CalculateDirectionalFactor(
                            mainLight.direction.y,
                            threshold
                        );
                }


                // ========================================================
                // 중앙 구체 크기
                // ========================================================

                float effectiveCoreRadius =
                    coreRadius *
                    directionalFactor;


                if (effectiveCoreRadius <= 0.0001)
                {
                    output.positionCS =
                        float4(
                            0.0,
                            0.0,
                            -2.0,
                            1.0
                        );

                    return output;
                }


                // ========================================================
                // 큰 IcoSphere → 중앙 구체 크기로 축소
                // ========================================================

                float coreScale =
                    effectiveCoreRadius /
                    lightRadius;


                float3 positionOS =
                    input.positionOS *
                    coreScale;


                float3 positionWS =
                    TransformObjectToWorld(
                        positionOS
                    );


                output.positionCS =
                    TransformWorldToHClip(
                        positionWS
                    );


                output.normalWS =
                    TransformObjectToWorldNormal(
                        input.normalOS
                    );


                output.viewDirWS =
                    GetWorldSpaceViewDir(
                        positionWS
                    );


                return output;
            }


            // ============================================================
            // Fragment
            // ============================================================

            half4 CoreFrag(
                Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);


                float enabled =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _CoreEnabled
                    );


                if (enabled <= 0.5)
                    discard;


                // ========================================================
                // Directional Light 감쇠율
                // ========================================================

                float useDirectional =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _UseDirectionalLightAttenuation
                    );


                float directionalFactor =
                    1.0;


                if (useDirectional > 0.5)
                {
                    Light mainLight =
                        GetMainLight();


                    float threshold =
                        UNITY_ACCESS_INSTANCED_PROP(
                            FakeGIProps,
                            _DirectionalLightThreshold
                        );


                    directionalFactor =
                        CalculateDirectionalFactor(
                            mainLight.direction.y,
                            threshold
                        );
                }


                if (directionalFactor <= 0.0001)
                    discard;


                // ========================================================
                // Core 설정
                // ========================================================

                float4 color =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _CoreColor
                    );


                float baseIntensity =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _CoreIntensity
                    );


                float softness =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _CoreSoftness
                    );


                float rimAmount =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _CoreRim
                    );


                float rimPower =
                    UNITY_ACCESS_INSTANCED_PROP(
                        FakeGIProps,
                        _CoreRimPower
                    );


                // ========================================================
                // Directional Light에 따른 Core 밝기
                // ========================================================

                float intensity =
                    baseIntensity *
                    directionalFactor;


                // ========================================================
                // Core 형태
                // ========================================================

                float3 normalWS =
                    normalize(
                        input.normalWS
                    );


                float3 viewDirWS =
                    normalize(
                        input.viewDirWS
                    );


                float NdotV =
                    saturate(
                        dot(
                            normalWS,
                            viewDirWS
                        )
                    );


                // 중앙
                float center =
                    smoothstep(
                        0.0,
                        max(
                            softness,
                            0.001
                        ),
                        NdotV
                    );


                // 가장자리
                float rim =
                    1.0 -
                    NdotV;


                rim =
                    pow(
                        saturate(rim),
                        max(
                            rimPower,
                            0.001
                        )
                    );


                rim *=
                    rimAmount;


                float finalIntensity =
                    center +
                    rim;


                return half4(
                    color.rgb *
                    intensity *
                    finalIntensity,
                    1.0
                );
            }

            ENDHLSL
        }
    }
}