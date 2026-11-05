# BaseShader 카툰·하프램버트 복원

## 설계

2026-10-29 사용자 요청으로 새 작업 브랜치 `feature/JMH/Effect`에 노멀 임포트 기능과 `Cartoon`, `HarfRambert` 셰이더를 추가한다. 파일은 기존 구조에 맞춰 `Assets/ProjectL/Script/Render/Shader/View/CustomShader/BaseShader/Shader/`에 둔다. 표시 이름도 요청한 `HarfRambert`를 사용하며 수식과 파라미터는 Half Lambert다.

- 기존 `OutlineNormalPostprocessor`를 단일 생성 주체로 유지한다. 원본 NORMAL을 보존하고 같은 양자화 위치(0.0001)의 평균 단위 노멀을 UV3/TEXCOORD3에 기록한다. 거의 상쇄된 합의 정규화 예외를 보완하고 후처리 버전을 올려 캐시에도 변경을 반영한다.
- 두 셰이더의 공통 표면 입력·URP 패스·광원/림 처리는 Core/Includes/Stylized에 공유한다. 확산광 모델만 각각 분리한다. 텍스처가 없으면 흰 기본 텍스처에 BaseColor를 곱해 컬러를 표시한다.
- 카툰 단계·경계·부드러움, 하프램버트 곡선, 스펙큘러·노멀·AO·발광·컷아웃, 광원 방향에 제한된 림을 재질에서 조절한다. Outline은 TEXCOORD3를 사용하며 폭 0으로 끌 수 있다. UV3 영벡터는 NORMAL로 대체하되 채널 자체가 없는 메시에는 올바른 아웃라인을 보장하지 않는다.
- Outline, Forward, ShadowCaster, DepthOnly, DepthNormals, 프로젝트 NormalOnly, Meta의 7개 패스를 포함한다. Fog·라이트맵·추가광원·Forward+·인스턴싱을 유지한다.

## Task와 Phase

이번 추가를 하나의 복원 Phase로 진행하고 결과를 보고한 뒤 멈춘다.

1. 기존 임포터의 정규화 예외 수정, 버전 갱신, 회귀 테스트 복원.
2. 기존에 검증한 표면/조명/패스 구현을 새 위치와 이름으로 정리해 셰이더 2종 및 공유 HLSL·meta 추가. 과거 Pixel/LastTest 실험 의존성은 포함하지 않는다.
3. 새 경로의 실제 소스로 D3D11 변형 컴파일, 격리 Unity GPU의 모델/림/컬러/텍스처/아웃라인·Fog 검사와 노멀 테스트를 수행한다. 원본 Unity 임포트/컴파일도 확인한다.

씬·머티리얼 일괄 변환과 픽셀화는 이번 범위 밖이다. 현재 브랜치의 기존 수정·삭제 에셋을 유지하고 커밋·push하지 않는다.

## 구조 점검

SSOT: 노멀 생성은 기존 임포터 한 곳, 표면 입력과 공통 패스는 공유 include 한 곳에서 소유한다. SRP/OCP: 모델 수식과 패스를 분리해 다른 모델 변경 없이 확산광을 바꿀 수 있다. LSP/ISP/DIP: 새 서비스 상속·계약·등록을 추가하지 않고 기존 URP 입력/패스 계약에 맞춘다. 조립은 `.shader`, 기능은 공통/모델 HLSL에 분리한다. Core 게임 로직·FieldCamera·Architecture와 FW 서비스 조립 경로를 수정하거나 관련 게임 기능까지 검증할 필요가 없다.

## Phase 완료 기록

- 기존 후처리는 이미 활성 상태여서 중복 임포터를 추가하지 않았다. 같은 GUID를 유지한 채 정규화 예외와 GetVersion=1을 반영했다. 새 FBX뿐 아니라 다음 기존 모델 임포트에서도 갱신된 버전을 사용한다. 이번 요청에서 전체 FBX 강제 재임포트 메뉴나 씬 일괄 변환은 추가하지 않았다.
- Shader 메뉴 이름: `ProjectL/BaseShader/Cartoon`, `ProjectL/BaseShader/HarfRambert`. 파일 이름은 각각 `Cartoon.shader`, `HarfRambert.shader`다. 공통 입력·패스 3개와 확산광 모델 2개는 `Core/Includes/Stylized`에 있다. 새 파일·폴더의 meta도 추가했다.
- D3D11 60개 Pass/변형 컴파일 통과: 인스턴싱, 추가광원, 그림자/SSAO/쿠키/레이어, Forward+, 정적·동적 라이트맵, Fog, punctual ShadowCaster, Oct DepthNormals, Meta 시각화.
- 격리 Unity 6000.0.73f1, 실제 D3D11 GPU에서 새 경로의 셰이더 2종 각각 7개 패스 컴파일·명암 조절·무텍스처 BaseColor·텍스처/컷아웃/발광·아웃라인 출력 확인. 각 모델의 빛 반대쪽 NdotL 음수 샘플 3,312개에서 림 누출 0. 렌더 후 SRP Batcher 호환 코드 0.
- 회전·비균일 스케일 Cube에서 UV3 영벡터 대체, UV3 길이 정규화, 평균 노멀에 따른 외곽선 변화(각 1,832픽셀), Forward/DepthNormals/ShadowCaster의 입력 변화 차이 0을 확인했다. 이 검사는 명시적 UV3 스트림의 영벡터/평균값 비교이며 UV3 채널 미보유 메시를 보증하지 않는다.
- Fog 9조합(직교 양/음 near·원근 × Linear/Exp/Exp2)을 설치 URP 수식과 대조해 최대 채널 오차 0.000388 미만으로 통과했다.
- 동일 임포터 소스의 격리 Unity EditMode 회귀 테스트 3/3 통과: 정확한/미세 잔차 상쇄, 하드 심 공유·위치 독립 및 원래 NORMAL 보존. 원본 프로젝트 전체 테스트 결과와는 구분한다.
- 원본 Unity의 BaseShader/Shader 폴더에서 새 셰이더 2개 임포트를 확인했고 Console 오류·경고 0이었다. 기존 씬·재질을 연결하거나 저장하지 않았다.

검증 자료: `Library/BaseStylizedVerification/Compile.ps1`, `unity-report.txt`, `outline-channel-report.txt`, `editmode-results.xml`, `gpu.log`, `editmode.log`. 기존 브랜치의 사용자 수정·삭제는 그대로 두었다. 구현 Phase 완료, 이후 재질 적용은 별도 작업이다.
