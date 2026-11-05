#ifndef PROJECTL_CARTOON_EFFECT_OCCLUSION_INCLUDED
#define PROJECTL_CARTOON_EFFECT_OCCLUSION_INCLUDED
#include "EffectOcclusionDepth.hlsl"

// Forward와 동일한 기본 텍스처/색/컷아웃을 사용한다. Opaque의 알파는 투과율이 아니다.
half4 ProjectLCartoonEffectOcclusionFrag(ProjectLStylizedVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    ProjectLStylizedClip(input.uv);
    float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
    float hull = SAMPLE_TEXTURE2D_X(_ProjectLEOOHullDepth, sampler_PointClamp, uv).r;
    clip(ProjectLEOOHasDepth(hull) ? 1 : -1);
    float eye = ProjectLEOOEyeDepth(input.positionCS.z);
    clip(ProjectLEOOEyeDepth(hull) - eye - _ProjectLEOOParameters.z);
    float world = SAMPLE_TEXTURE2D_X(_ProjectLEOOWorldDepth, sampler_PointClamp, uv).r;
    clip(ProjectLEOOBlocked(world, eye) ? -1 : 1);
    half alpha = _Surface > 0.5 ? saturate(ProjectLStylizedBase(input.uv).a) : 1;
    return half4(alpha, alpha, alpha, alpha);
}
#endif
