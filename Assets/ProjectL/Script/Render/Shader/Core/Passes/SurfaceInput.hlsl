#ifndef PROJECTL_PASS_SURFACE_INCLUDED
#define PROJECTL_PASS_SURFACE_INCLUDED

#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/ProjectLInput.hlsl"
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Maske/DissolveUtility.hlsl"

half4 ProjectLReadAlbedo(float2 uv)
{
    return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Color;
}

// 모든 디졸브 Pass가 동일한 샘플링과 폐기 조건을 사용한다.
float ProjectLApplySurfaceClip(float2 dissolveUV)
{
#if defined(PROJECTL_DISSOLVE)
    if (_DissolveAmount <= 0.0)
    {
        return 0.0;
    }
    if (_DissolveAmount >= 1.0)
    {
        clip(-1.0);
        return 0.0;
    }
    float noise = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, dissolveUV).r;
    float2 mask = ProjectLDissolveMask(noise, _DissolveAmount, _DissolveEdgeWidth);
    clip(mask.x - 0.5);
    return mask.y;
#else
    return 0.0;
#endif
}
#endif
