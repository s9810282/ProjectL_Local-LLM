#ifndef PROJECTL_EFFECT_OCCLUSION_DEPTH_INCLUDED
#define PROJECTL_EFFECT_OCCLUSION_DEPTH_INCLUDED

TEXTURE2D_X_FLOAT(_ProjectLEOOWorldDepth);
TEXTURE2D_X_FLOAT(_ProjectLEOOHullDepth);
float4 _ProjectLEOOParameters;

bool ProjectLEOOHasDepth(float depth)
{
#if UNITY_REVERSED_Z
    return depth > 0.000001;
#else
    return depth < 0.999999;
#endif
}

float ProjectLEOOEyeDepth(float depth)
{
    if (unity_OrthoParams.w > 0.5)
    {
#if UNITY_REVERSED_Z
        depth = 1.0 - depth;
#endif
        return lerp(_ProjectionParams.y, _ProjectionParams.z, depth);
    }
    return LinearEyeDepth(depth, _ZBufferParams);
}

// 마스크와 합성이 같은 거리 기준을 사용한다. 값의 단위는 시점 공간 거리다.
bool ProjectLEOOBlocked(float rawWorld, float eyeDepth)
{
    return ProjectLEOOHasDepth(rawWorld) && ProjectLEOOEyeDepth(rawWorld) + _ProjectLEOOParameters.z < eyeDepth;
}
#endif
