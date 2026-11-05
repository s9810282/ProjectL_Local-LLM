#ifndef PROJECTL_OUTLINE_NORMAL_UTILITY_INCLUDED
#define PROJECTL_OUTLINE_NORMAL_UTILITY_INCLUDED

// FBX 후처리가 UV3(TEXCOORD3)에 저장한 오브젝트 공간의 부호 있는 평균 노멀이다.
float3 ProjectLOutlineNormalOS(float3 averagedNormalOS, float3 normalOS)
{
    // UV3에 영벡터가 들어온 정점은 원래 노멀로 외곽선을 확장한다.
    float lengthSquared = dot(averagedNormalOS, averagedNormalOS);
    if (lengthSquared > 1e-10)
    {
        return averagedNormalOS * rsqrt(lengthSquared);
    }

    return SafeNormalize(normalOS);
}

#endif
