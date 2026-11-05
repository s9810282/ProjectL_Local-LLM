#ifndef PROJECTL_PBR_STEP_LIGHTING_INCLUDED
#define PROJECTL_PBR_STEP_LIGHTING_INCLUDED

// PBR 출력의 선형 휘도만 단계화하고 RGB 비율과 HDR 범위를 유지한다.
float3 ProjectLPbrStepColor(float3 color, float steps, float brightnessRange)
{
    color = max(color, 0.0);
    float brightness = dot(color, float3(0.2126, 0.7152, 0.0722));
    if (brightness <= 0.00001)
    {
        return 0;
    }

    float intervals = clamp(floor(steps + 0.5), 2.0, 20.0) - 1.0;
    float spacing = max(brightnessRange, 0.0001) / intervals;
    // 기준 범위는 간격을 정한다. 이를 넘는 밝은 반사광은 자르지 않는다.
    float stepped = floor(brightness / spacing + 0.5) * spacing;
    return color * (stepped / brightness);
}

// ComposerLighting의 공통 입력 변환과 PBR 함수를 연결한 뒤 포함한다.
half3 ProjectLComposerPbrStep(ProjectLComposerContext context, half3 albedo,
    half metallic, half smoothness, float steps, float brightnessRange)
{
    // 문맥은 값으로 전달되므로 함께 혼합하는 다른 모델의 노멀은 유지된다.
    context.normalWS = ProjectLFlatDiffuseNormal(context.positionWS, context.normalWS, 1.0);
#if !defined(LIGHTMAP_ON) && !defined(DYNAMICLIGHTMAP_ON)
    context.bakedGI = SampleSH(context.normalWS);
#endif
    context.vertexLighting = VertexLighting(context.positionWS, context.normalWS);
    float3 color = ProjectLComposerLit(context, albedo, metallic, smoothness);
    return ProjectLPbrStepColor(color, steps, brightnessRange);
}

#endif
