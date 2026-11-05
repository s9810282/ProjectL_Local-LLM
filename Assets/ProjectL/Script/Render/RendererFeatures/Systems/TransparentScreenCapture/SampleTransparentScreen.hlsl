#ifndef PROJECTL_SAMPLE_TRANSPARENT_SCREEN_INCLUDED
#define PROJECTL_SAMPLE_TRANSPARENT_SCREEN_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D_X(_ProjectLTransparentScreenTexture);

// 현재 카메라의 캡처 뒤에 실행되는 왜곡 패스에서만 사용한다.
void ProjectLTransparentScreen_float(float2 UV, out float3 Color)
{
#if defined(SHADERGRAPH_PREVIEW)
    Color = 0;
#else
    Color = SAMPLE_TEXTURE2D_X(_ProjectLTransparentScreenTexture, sampler_LinearClamp,
        UnityStereoTransformScreenSpaceTex(saturate(UV))).rgb;
#endif
}

void ProjectLTransparentScreen_half(half2 UV, out half3 Color)
{
    float3 sampled;
    ProjectLTransparentScreen_float(UV, sampled);
    Color = sampled;
}

#endif
