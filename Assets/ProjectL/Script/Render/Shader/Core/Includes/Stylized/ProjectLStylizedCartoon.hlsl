#ifndef PROJECTL_STYLIZED_CARTOON_INCLUDED
#define PROJECTL_STYLIZED_CARTOON_INCLUDED
#include "Assets/ProjectL/Script/Render/Shader/Core/Includes/Lightng/ToonLighting.hlsl"

// 명암 단계 수와 경계 이동은 카툰 모델만 소유한다.
float ProjectLStylizedDiffuseAmount(float normalLight)
{
    float shifted = saturate(normalLight + 0.5 - _ShadeThreshold);
    return ProjectLToonDiffuse(shifted, _ShadingSteps, 0, _ShadeSoftness);
}
#endif
