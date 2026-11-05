# BaseColor 명암 단계와 면 노멀

2026-10-14 사용자 요청: BaseColor 조명만 변경하고 각진 면의 명암 대비를 강하게 만든다.

## 설계와 단일 Phase

1. 기본/디졸브 셰이더의 diffuse만 두 단계로 변경한다.
2. 월드 위치의 화면 미분으로 삼각형 면 노멀을 구해 diffuse에 사용한다. 스펙큘러와 림에는 기존 보간 노멀과 계산을 사용한다.
3. 환경광의 기본 강도를 낮춰 어두운 명암이 밝아지는 것을 줄인다.
4. 두 셰이더를 컴파일하고 Unity 머티리얼 Inspector에서 결과를 확인한다.

명암 함수와 면 노멀 함수는 `BaseShader/ToonDiffuse.hlsl`에서 공유한다(SSOT). 셰이더 표현만 변경하며 Framework 서비스·Scope·DB·Core의 경계와 FW 계약은 변경하지 않는다.

## 기본값

| Inspector 항목 | 값 | 의미 |
|---|---:|---|
| Diffuse Shading Steps | 2 | 밝은 면/어두운 면 두 단계 |
| Diffuse Dark Brightness | 0.05 | 어두운 단계의 직접광 밝기 |
| Diffuse Edge Softness | 0 | 명암 경계에서 보간하지 않음 |
| Flat Face Diffuse | On | diffuse에 삼각형 면 노멀 사용 |
| Diffuse Ambient Strength | 0.15 | 기존 환경광을 15%로 적용 |

기본값의 주광원 diffuse는 `N·L < 0.5`일 때 0.05, 그 이상이면 1이다. 방향광과 일정한 조명 조건에서는 평면 삼각형 안의 diffuse 명암 단계가 일정하다. 실제 그림자·SSAO·점광원의 거리 감쇠, 텍스처, 스펙큘러와 림은 계속 적용되므로 최종 픽셀 색까지 삼각형 전체에서 고정되는 것은 아니다.

## 검증

- 두 셰이더의 Forward, 그림자·추가광·SSAO·쿠키, Forward+, 정점광, 정점 변환, 그림자 정점/픽셀, DepthOnly, DepthNormals(옥타헤드럴), NormalOnly: D3D11 HLSL 20개 조합 모두 컴파일 성공.
- Unity의 `Custom_ProjectLBaseLit` 머티리얼 Inspector에서 기본값과 평면별 명암 미리보기 확인. Console 오류/경고 0.
- 컴파일 확인 도구와 산출물은 Git에서 제외된 `Library/ToonVerification`에 있다. FXC의 half 호환 옵션 `/Gec`를 사용했고, 빈 `PopMarker` 매크로는 검증용 패키지 복사본에서만 조정했다.
