#ifndef PROJECTL_STYLIZED_HALFLAMBERT_INCLUDED
#define PROJECTL_STYLIZED_HALFLAMBERT_INCLUDED

// 림의 광원 방향 마스크는 이 확산광과 별도로 원래 N dot L을 사용한다.
float ProjectLStylizedDiffuseAmount(float normalLight)
{
    return pow(saturate(normalLight * 0.5 + 0.5), max(_HalfLambertPower, 0.01));
}
#endif
