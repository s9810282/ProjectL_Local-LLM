#ifndef PROJECTL_DISSOLVE_UTILITY_INCLUDED
#define PROJECTL_DISSOLVE_UTILITY_INCLUDED

// x는 유지 마스크, y는 테두리 마스크다. 픽셀 폐기는 호출하는 Pass가 담당한다.
// amount 0은 모든 픽셀 유지, 1은 모든 픽셀 제거를 보장한다.
float2 ProjectLDissolveMask(float textureMask, float amount, float edgeWidth)
{
    if (amount <= 0.0)
    {
        return float2(1.0, 0.0);
    }
    if (amount >= 1.0)
    {
        return float2(0.0, 0.0);
    }
    float remaining = saturate(textureMask) - amount;
    float keep = step(0.0, remaining);
    float edge = edgeWidth > 0.0
        ? 1.0 - smoothstep(0.0, max(edgeWidth, 0.00001), remaining)
        : 0.0;
    return float2(keep, edge * keep);
}
#endif
