#ifndef COMMON_UTILITY_Vertex_HLSLI
#define COMMON_UTILITY_Vertex_HLSLI

// --- 기존 함수들 ---
float3 GetVertexNormal(float4 vertexData)
{
    return normalize(vertexData.xyz);
}

float3 GetVertexTangent(float4 vertexData)
{
    return normalize(vertexData.w * float3(1, 0, 0) + (1 - vertexData.w) * float3(0, 1, 0));
}

float3 GetVertexBitangent(float4 vertexData)
{
    return normalize(cross(GetVertexNormal(vertexData), GetVertexTangent(vertexData)));
}

float3 GetVertexPosition(float4 vertexData)
{
    return vertexData.xyz;
}


// ==========================================
// --- 아웃라인 및 추가 유틸리티 함수 영역 ---
// ==========================================

// 1. 아웃라인용 버텍스 밀어내기 함수
// 설명: 버텍스 위치를 노멀 방향으로 지정한 두께(thickness)만큼 바깥쪽으로 확장합니다.
float3 CalculateOutlinePosition(float3 position, float3 normal, float thickness)
{
    return position + (normal * thickness);
}

// 2. 밸류 리매프(Remap) 함수
// 설명: 특정 범위의 값을 다른 범위로 선형 변환합니다. (카툰 톤이나 마스크 조절용으로 꿀입니다)
float RemapValue(float value, float inMin, float inMax, float outMin, float outMax)
{
    return outMin + (value - inMin) * (outMax - outMin) / (inMax - inMin);
}

// 3. 포화(Saturate) 대용 안전 장치 혹은 커스텀 클램프
// 설명: 0과 1 사이로 값을 강제 고정합니다. (HLSL 기본 saturate와 동일하지만 유틸용으로 구비)
float Clamp01(float value)
{
    return clamp(value, 0.0, 1.0);
}

#endif