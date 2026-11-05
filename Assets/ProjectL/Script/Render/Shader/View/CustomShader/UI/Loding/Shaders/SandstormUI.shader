Shader "ProjectL/UI/Sandstorm"
{
    Properties
    {
        [PerRendererData] _MainTex ("UI Texture", 2D) = "white" {}
        _NoiseTex ("Wind Noise", 2D) = "gray" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _SandColor ("Sand Color", Vector) = (0.702,0.5255,0.3373,1)
        _LightColor ("Grain Color", Vector) = (0.969,0.843,0.624,1)
        _Turbulence ("Turbulence", Range(0,2)) = 1
        _WindSpeed ("Wind Speed", Range(0.1,3)) = 1
        _GrainStrength ("Grain Strength", Range(0,1)) = 0.35
        _Phase ("Phase", Float) = 0
        _Progress ("Progress", Range(0,1)) = 0
        _PresentationTime ("Unscaled Presentation Time", Float) = 0
        _Aspect ("Aspect", Float) = 1.77778
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct Attributes { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 vertex : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 localPosition : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _NoiseTex;
            fixed4 _Color;
            float4 _SandColor, _LightColor, _ClipRect;
            float _Phase, _Progress, _PresentationTime, _Aspect, _Turbulence, _WindSpeed, _GrainStrength;

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = float2(input.uv.x, 1.0 - input.uv.y);
                output.color = input.color * _Color;
                return output;
            }

            float Noise(float2 p)
            {
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return tex2Dlod(_NoiseTex, float4((floor(p) + f + 0.5) / 64.0, 0, 0)).r;
            }

            float2 Eddy(int index, float time)
            {
                return float2(frac(0.08 + time * 0.145 + index * 0.37) * 1.5 - 0.25,
                    0.18 + index * 0.3 + 0.075 * sin(time * 1.4 + index * 2.0));
            }

            float2 Curl(float2 uv, float time)
            {
                float2 field = 0;
                [unroll] for (int i = 0; i < 3; i++)
                {
                    float2 d = uv - Eddy(i, time);
                    d.y *= 0.72;
                    float falloff = exp(-dot(d, d) / 0.027) * (i == 1 ? -1.8 : 1.8);
                    field += float2(-d.y, d.x) * falloff;
                }
                return field * _Turbulence;
            }

            float Coverage(float2 uv, float turbulence, float time)
            {
                if (_Phase < 0.5) return 0;
                // 대기와 완료 페이드는 전체 CanvasGroup의 농도를 사용한다.
                if (_Phase > 1.5) return 1;
                float front = -0.4 + 1.8 * saturate(_Progress) + (uv.y - 0.5) * 0.18 +
                    0.055 * sin(uv.y * 12.0 - time * 2.0) + 0.03 * sin(uv.y * 25.0 + time * 3.0);
                float density = smoothstep(-0.16, 0.16, front - uv.x + turbulence * 0.13);
                return density;
            }

            float Hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453);
            }

            float Grains(float2 uv, float time)
            {
                float2 scale = float2(max(_Aspect, 0.1) * 16.0, 17.0);
                float2 p = (uv + Curl(uv, time) * 0.7) * scale - float2(time * 16.0, -time * 1.6);
                float2 cell = floor(p);
                float2 random = float2(Hash(cell), Hash(cell + 19.27));
                float2 d = frac(p) - (0.15 + random * 0.7);
                float width = max(fwidth(p.y), 0.008);
                float grainMask = (1.0 - smoothstep(0.015, 0.015 + width, abs(d.y + d.x * 0.15))) *
                    (1.0 - smoothstep(0.12, 0.22, abs(d.x)));
                return grainMask * random.x;
            }

            fixed4 Frag(Varyings input) : SV_Target
            {
                float time = _PresentationTime * _WindSpeed;
                float2 uv = input.uv;
                float2 warped = uv + Curl(uv, time);
                float n1 = Noise(warped * float2(3.4,5.5) + float2(-time * 1.15, time * 0.17));
                float n2 = Noise(warped * float2(8,14) + float2(-time * 2.8 + n1 * 0.9, time * 0.38));
                float n3 = Noise(warped * float2(17,33) + float2(-time * 5.2, time * 0.62));
                float turbulence = (n1 - 0.5) * 1.2 + (n2 - 0.5) * 0.55;
                float alpha = Coverage(uv, turbulence, time) * input.color.a;
                float light = (n1 * 0.65 + n2 * 0.27 + n3 * 0.08) * (76.0 / 255.0) - 38.0 / 255.0;
                float3 rgb = _SandColor.rgb + light * float3(1,0.84,0.64);
                rgb = lerp(rgb, _LightColor.rgb, saturate(Grains(uv, time) * _GrainStrength));
                #ifndef UNITY_COLORSPACE_GAMMA
                    rgb = GammaToLinearSpace(rgb);
                #endif
                fixed4 value = fixed4(rgb * input.color.rgb, alpha);
                #ifdef UNITY_UI_CLIP_RECT
                    value.a *= UnityGet2DClipping(input.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(value.a - 0.001);
                #endif
                return value;
            }
            ENDCG
        }
    }
}
