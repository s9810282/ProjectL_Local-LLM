#ifndef COMMON_UTILITY_Cartoon_HLSLI
#define COMMON_UTILITY_Cartoon_HLSLI

float3 ApplyCartoonShading(float3 color, float3 normal, float3 lightDir, float threshold)
{
    float intensity = dot(normal, lightDir);
    if (intensity > threshold)
    {
        return color;
    }
    else
    {
        return color * 0.5; // Darken the color for shadowed areas
    }
}

#endif