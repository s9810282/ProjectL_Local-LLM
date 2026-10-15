#ifndef COMMON_UTILITY_Light_HLSLI
#define COMMON_UTILITY_Light_HLSLI

// --- 기존 작성하신 함수들 ---
float3 GetLightDirection(float4 lightData)
{
    return normalize(lightData.xyz);
}

float ApplyLightIntensity(float3 lightDirection, float3 normal, float intensity)
{
    return max(dot(normal, lightDirection), 0) * intensity;
}


// ==========================================
// --- 추가 라이팅 유틸리티 함수 영역 ---
// ==========================================

// 1. 반사 벡터(Reflection Vector) 구하기
// 설명: 표면에서 빛이 튕겨 나가는 방향을 계산합니다. (스펙큘러나 환경 반사 림라이트 등에 사용)
float3 GetReflectionDirection(float3 lightDirection, float3 normal)
{
    return reflect(-lightDirection, normal);
}

// 2. 하프 벡터(Half Vector) 구하기
// 설명: 시선 방향과 빛 방향의 중간 벡터를 구합니다. (Blinn-Phong 스펙큘러 계산 시 필수)
float3 GetHalfVector(float3 lightDirection, float3 viewDirection)
{
    return normalize(lightDirection + viewDirection);
}

// 3. 림 라이트(Rim Light / 테두리 조명) 강도 계산
// 설명: 시선과 노멀의 각도를 이용해 오브젝트 테두리에 빛이 은은하게 걸치는 효과를 냅니다.
float CalculateRimLighting(float3 normal, float3 viewDirection, float rimPower)
{
    float rim = 1.0 - saturate(dot(normal, viewDirection));
    return pow(rim, rimPower);
}

#endif