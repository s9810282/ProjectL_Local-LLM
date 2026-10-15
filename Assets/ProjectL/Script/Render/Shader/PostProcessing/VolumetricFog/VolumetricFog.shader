Shader "ProjectL/HeightVolumeFog"
{
    Properties
    {
        [Header(Volume)]
        [HDR] _FogColor ("안개 색상", Color) = (0.65, 0.75, 0.85, 1)
        _Density ("안개 밀도", Range(0, 12)) = 2.0
        _StepCount ("레이마칭 스텝 수", Range(8, 128)) = 64
        _Jitter ("스텝 지터", Range(0, 1)) = 0.75
        _EdgeFade ("큐브 가장자리 페이드", Range(0.001, 0.25)) = 0.04

        [Header(Height Map)]
        _HeightMap ("하이트 맵", 2D) = "gray" {}
        _HeightScale ("하이트 높이 배율", Range(0, 2)) = 1.0
        _HeightOffset ("하이트 높이 오프셋", Range(-1, 1)) = 0.0
        _HeightSoftness ("하이트 경계 부드러움", Range(0.001, 0.35)) = 0.06

        [Header(Parallax)]
        _ParallaxStrength ("패럴럭스 강도", Range(0, 0.5)) = 0.08
        _ParallaxViewClamp ("패럴럭스 시야 제한", Range(0.05, 1.0)) = 0.25

        [Header(Noise)]
        _NoiseTex ("노이즈 텍스처", 2D) = "white" {}
        _NoiseScale ("노이즈 크기", Float) = 3.0
        _NoiseStrength ("노이즈 강도", Range(0, 1)) = 0.35
        _NoiseScroll ("노이즈 이동 속도 XY", Vector) = (0.03, 0.01, 0, 0)

        [Header(Depth)]
        _DepthBias ("씬 깊이 보정", Range(0, 0.1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "HeightVolumeFog"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // 큐브의 뒷면을 그린다.
            // 카메라가 큐브 안에 들어가도 볼륨을 계속 볼 수 있다.
            Cull Front

            // 불투명 지오메트리의 Depth는 셰이더 안에서 직접 잘라낸다.
            ZWrite Off
            ZTest Always

            // 아래 Frag는 premultiplied color를 반환한다.
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_HeightMap);
            SAMPLER(sampler_HeightMap);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;

                float _Density;
                float _StepCount;
                float _Jitter;
                float _EdgeFade;

                float4 _HeightMap_ST;
                float _HeightScale;
                float _HeightOffset;
                float _HeightSoftness;

                float _ParallaxStrength;
                float _ParallaxViewClamp;

                float4 _NoiseTex_ST;
                float _NoiseScale;
                float _NoiseStrength;
                float4 _NoiseScroll;

                float _DepthBias;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs pos =
                    GetVertexPositionInputs(input.positionOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;

                return output;
            }

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // ------------------------------------------------------------
            // Object Space unit cube [-0.5, +0.5] ray intersection.
            //
            // IMPORTANT:
            // rayDirOS를 normalize하지 않는다.
            // world ray direction을 unity_WorldToObject의 3x3로 변환하면
            // t는 그대로 world-space distance 단위로 사용할 수 있다.
            // 따라서 non-uniform scale에서도 scene depth와 같은 t로 비교 가능.
            // ------------------------------------------------------------
            bool RayBoxIntersection(
                float3 rayOriginOS,
                float3 rayDirOS,
                out float tNear,
                out float tFar)
            {
                float3 signDir =
                    step(0.0, rayDirOS) * 2.0 - 1.0;

                float3 safeDir =
                    rayDirOS + signDir * 1e-7;

                float3 t0 =
                    (-0.5 - rayOriginOS) / safeDir;

                float3 t1 =
                    ( 0.5 - rayOriginOS) / safeDir;

                float3 tSmall = min(t0, t1);
                float3 tLarge = max(t0, t1);

                tNear =
                    max(
                        max(tSmall.x, tSmall.y),
                        tSmall.z
                    );

                tFar =
                    min(
                        min(tLarge.x, tLarge.y),
                        tLarge.z
                    );

                return tFar > max(tNear, 0.0);
            }

            float SampleHeight(float3 positionOS, float3 rayDirOS)
            {
                // Unity 기본 Cube는 local [-0.5, +0.5]
                float3 p01 = positionOS + 0.5;

                // --------------------------------------------------------
                // Height-map parallax
                //
                // 레이마칭 자체로 이미 실제 view parallax가 생긴다.
                // 아래 오프셋은 각 Y layer의 height-map UV를 추가로 이동시켜
                // 2D height map에서 내부 깊이감을 더 강하게 만든다.
                // --------------------------------------------------------
                float3 viewDirOS =
                    normalize(-rayDirOS);

                float safeViewY =
                    max(
                        abs(viewDirOS.y),
                        _ParallaxViewClamp
                    );

                float2 parallaxDir =
                    viewDirOS.xz / safeViewY;

                float layerOffset =
                    (0.5 - p01.y) *
                    _ParallaxStrength;

                float2 heightUV =
                    p01.xz +
                    parallaxDir * layerOffset;

                heightUV =
                    heightUV * _HeightMap_ST.xy +
                    _HeightMap_ST.zw;

                float heightValue =
                    SAMPLE_TEXTURE2D_LOD(
                        _HeightMap,
                        sampler_HeightMap,
                        heightUV,
                        0
                    ).r;

                return saturate(
                    heightValue * _HeightScale +
                    _HeightOffset
                );
            }

            float SampleNoise(float3 positionOS)
            {
                float3 p01 = positionOS + 0.5;

                // 2D texture를 Y에 따라 비틀어 pseudo-3D처럼 사용한다.
                float2 uv =
                    p01.xz * _NoiseScale;

                uv +=
                    p01.y *
                    float2(0.37, 0.73) *
                    _NoiseScale;

                uv +=
                    _Time.y *
                    _NoiseScroll.xy;

                uv =
                    uv * _NoiseTex_ST.xy +
                    _NoiseTex_ST.zw;

                float n =
                    SAMPLE_TEXTURE2D_LOD(
                        _NoiseTex,
                        sampler_NoiseTex,
                        uv,
                        0
                    ).r;

                return lerp(
                    1.0,
                    saturate(n * 1.35),
                    _NoiseStrength
                );
            }

            float SampleDensity(
                float3 positionOS,
                float3 rayDirOS)
            {
                float3 p01 = positionOS + 0.5;

                // ray-box 오차 방지
                if (any(p01 < 0.0) || any(p01 > 1.0))
                    return 0.0;

                float height =
                    SampleHeight(
                        positionOS,
                        rayDirOS
                    );

                // Height map 아래쪽을 fog로 채운다.
                //
                // p01.y < height : fog
                // p01.y > height : empty
                float heightMask =
                    1.0 -
                    smoothstep(
                        height,
                        height + _HeightSoftness,
                        p01.y
                    );

                // 큐브 경계에서 딱 잘리는 느낌 완화
                float3 edgeDistance =
                    min(
                        p01,
                        1.0 - p01
                    );

                float nearestEdge =
                    min(
                        min(
                            edgeDistance.x,
                            edgeDistance.y
                        ),
                        edgeDistance.z
                    );

                float edgeMask =
                    smoothstep(
                        0.0,
                        max(_EdgeFade, 1e-5),
                        nearestEdge
                    );

                float noise =
                    SampleNoise(positionOS);

                return
                    heightMask *
                    edgeMask *
                    noise *
                    _Density;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 cameraWS =
                    GetCameraPositionWS();

                // 이 fragment는 cube back-face 위에 있으므로
                // camera -> back face 방향이 해당 pixel의 world ray.
                float3 rayDirWS =
                    normalize(
                        input.positionWS -
                        cameraWS
                    );

                // --------------------------------------------------------
                // World ray -> Object ray.
                // normalize하지 않아 t를 world distance로 유지한다.
                // --------------------------------------------------------
                float3 rayOriginOS =
                    TransformWorldToObject(
                        cameraWS
                    );

                float3 rayDirOS =
                    mul(
                        (float3x3)unity_WorldToObject,
                        rayDirWS
                    );

                float tNear;
                float tFar;

                if (!RayBoxIntersection(
                    rayOriginOS,
                    rayDirOS,
                    tNear,
                    tFar))
                {
                    discard;
                }

                float tStart =
                    max(tNear, 0.0);

                float tEnd =
                    tFar;

                // --------------------------------------------------------
                // Scene depth로 opaque surface 뒤쪽 volume 제거
                // --------------------------------------------------------
                float2 screenUV =
                    GetNormalizedScreenSpaceUV(
                        input.positionCS
                    );

                float rawDepth =
                    SampleSceneDepth(screenUV);

                bool hasSceneDepth;

                #if UNITY_REVERSED_Z
                    hasSceneDepth =
                        rawDepth > 0.00001;
                #else
                    hasSceneDepth =
                        rawDepth < 0.99999;
                #endif

                if (hasSceneDepth)
                {
                    float3 scenePosWS =
                        ComputeWorldSpacePosition(
                            screenUV,
                            rawDepth,
                            UNITY_MATRIX_I_VP
                        );

                    float sceneT =
                        dot(
                            scenePosWS - cameraWS,
                            rayDirWS
                        );

                    tEnd =
                        min(
                            tEnd,
                            sceneT - _DepthBias
                        );
                }

                if (tEnd <= tStart)
                    discard;

                int stepCount =
                    (int)clamp(
                        round(_StepCount),
                        8.0,
                        128.0
                    );

                float segmentLength =
                    tEnd - tStart;

                float stepLength =
                    segmentLength /
                    max((float)stepCount, 1.0);

                float jitter =
                    Hash12(
                        input.positionCS.xy
                    ) * _Jitter;

                float t =
                    tStart +
                    stepLength * jitter;

                float3 accumulatedColor =
                    0.0;

                float accumulatedAlpha =
                    0.0;

                [loop]
                for (int i = 0; i < 128; i++)
                {
                    if (i >= stepCount)
                        break;

                    if (t > tEnd)
                        break;

                    if (accumulatedAlpha >= 0.995)
                        break;

                    float3 samplePosOS =
                        rayOriginOS +
                        rayDirOS * t;

                    float density =
                        SampleDensity(
                            samplePosOS,
                            rayDirOS
                        );

                    // Beer-Lambert 형태의 step alpha.
                    float sampleAlpha =
                        1.0 -
                        exp(
                            -density *
                            stepLength
                        );

                    sampleAlpha *=
                        _FogColor.a;

                    sampleAlpha =
                        saturate(sampleAlpha);

                    // front-to-back premultiplied accumulation
                    float visibility =
                        1.0 -
                        accumulatedAlpha;

                    accumulatedColor +=
                        visibility *
                        _FogColor.rgb *
                        sampleAlpha;

                    accumulatedAlpha +=
                        visibility *
                        sampleAlpha;

                    t += stepLength;
                }

                return half4(
                    accumulatedColor,
                    accumulatedAlpha
                );
            }

            ENDHLSL
        }
    }
}
