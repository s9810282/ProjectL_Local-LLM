# 볼륨메트릭 라이트 — 투과율·산란광 분리 설계 (2026-11-07)

작성: Codex / View 작업. 사용자 요청: 기존 효과의 색감 변화 원인을 개선하는 볼륨메트릭 라이트 구현.

## 1. 현황과 목표

- `VolmetrickLight.compute`는 메인 라이트 그림자 여부에 따라 무채색 누적값을 더하거나 빼고 있다. 광원 색·세기·산란 방향·공기 밀도가 분리되어 있지 않다.
- `ShadowSteps`, `ShadowsColor`는 CPU에서 전달하지만 셰이더에서는 사용하지 않는다.
- PC Renderer에는 기능이 등록되어 있지만 `IsAddRenderPass = 0`이다. `RenderFeatureSystem.Initialize`도 관리 기능을 끄며, 현재 볼륨메트릭 사용 Scope 소비자는 없다.
- 색 변화가 전혀 없는 효과를 보장하지 않는다. 밀도와 별도 산란 강도로 공기층과 빛줄기를 조절할 수 있게 한다.

### 1.1 사용자 참조에 따른 표시 목표 (2026-11-07)

사용자가 첨부한 `codex-clipboard-8d3546d7-17dc-4456-b59e-b0552ca103e0.png`의 off/on 비교를 표시 목표로 삼는다. 특정 엔진이나 시간대는 이미지로 확정하지 않는다.

- 밝은 주간 장면에서도 지붕 구조물 등에 가려진 넓은 빛줄기와 공기층이 보여야 한다.
- on에서는 벽·원경·하늘의 대비가 완만해지고 옅은 산란광이 보인다. 모든 배경색을 동일하게 보존하는 것이 인수 조건은 아니다.
- 햇빛의 가림에 따라 밝은 공기와 어두운 공기가 나뉘어야 하며, 전체 화면을 밝히는 것만으로 빛줄기 가시성을 완료 처리하지 않는다.
- 지상 표면과 유닛의 구분은 유지해야 한다. 이미지와 같은 결과인지는 실제 게임의 같은 구도에서 효과 on/off로 확인한다.

이 목표는 현재 단일 산란식의 설정 튜닝으로 먼저 접근한다. 이번 후속에서 새 광원·매질·합성 모드나 다른 기능의 수명을 추가하지 않는다. 현재 게임 화면과의 대조가 없으므로 프리셋 변경만으로 참조 일치를 판정하지 않는다.

## 2. 이번 구현의 경계

Phase 1은 기존 화면별 Compute 레이마칭을 유지하면서 단일 산란 근사를 구현한다. 범위는 기존 Feature·Settings·Compute, PC Renderer의 해당 설정, 이 설계와 계획 문서다.

- 포함: 메인 Directional Light의 URP 최종 색·세기·방향, 기존 1~4 cascade 그림자, 투과율, 비음수 산란광, 방향성, 시작 거리, 높이별 밀도.
- 별도 후속: 운영 활성화 Scope의 수명 연결과 Unity 화면 튜닝(Phase 2).
- 이번 방식은 Unreal/Frostbite의 3D 격자 전체 재현이 아니다. 추가 광원, 국소 볼륨, 다중 산란, 매질 자체 그림자, 반해상도 및 시간 누적은 포함하지 않는다.
- 기존 `BaseRenderFeature`, `RenderFeatureSystem`, Factory/Group, Core, FieldCamera, Architecture 및 씬은 Phase 1에서 수정하지 않는다.
- 기존 클래스·네임스페이스·에셋 GUID는 유지한다. 경로와 기존 네임스페이스 차이의 일괄 정리는 이 기능 변경에 섞지 않는다.

## 3. 계산과 설정의 원본

렌더러에 직렬화된 `VolmetrickLightSettings`가 효과 설정의 원본이다. `PassData`는 그 카메라의 렌더 명령에 필요한 값만 복사하며 별도 동기화 상태를 소유하지 않는다. 메인 광원은 `UniversalLightData`에서 매 카메라 읽고, 카메라 투영은 `UniversalCameraData`에서 읽는다.

### 3.1 매질과 합성

```text
sigmaT(y) = Density * exp(-max(y - BaseHeight, 0) * HeightFalloff)
segmentT = exp(-sigmaT * segmentLength)
segmentScatter = ScatteringAlbedo * (1 - segmentT)
scatteredLight += T * segmentScatter * lightColor * phase * visibility * ScatteringIntensity
T *= segmentT
finalRGB = originalRGB * T + scatteredLight
finalAlpha = originalAlpha
```

- `Density`는 기준 높이 이하의 단위 거리당 소멸 계수다. 밀도가 0이면 원본을 그대로 유지한다.
- `ScatteringAlbedo`는 0~1의 무채색 산란 비율이다. 흡수에 의한 색 편향은 모델링하지 않는다.
- `ScatteringIntensity`는 광원 기여도 조절값이다. 밀도와 분리해 아트 조절을 허용하므로 완전한 에너지 보존을 보장하지 않는다.
- 위상 함수는 설치된 Core RP의 `HenyeyGreensteinPhaseFunction`을 재사용한다. 시선 광선과 광원을 향한 방향의 내적을 사용해 양의 방향성에서 광원을 바라볼 때 강해진다.
- 그림자는 산란광의 기여도만 줄인다. 원본 RGB에 음수 값을 더하지 않는다. 매질의 투과율에 의한 배경 감쇠는 그림자와 독립적이다.
- 직교 카메라의 픽셀별 near-plane 광선 원점을 유지한다. 시작 거리와 최대 범위는 이 원점을 기준으로 한다. 불투명 표면 뒤로 적분하지 않는다.
- GPU 투영은 `GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), true)`로 계산한다. 이 패스는 중간 텍스처를 요구하고 backbuffer 경로는 제외한다. 설치된 URP 17의 인자 없는 `cameraData.GetGPUProjectionMatrix()`는 호환 모드의 `cameraColorTargetHandle`을 조회하므로 RenderGraph 기록에서 사용하지 않는다(사용자 실행 로그에 따른 2026-11-07 정정).
- 높이 변화와 그림자는 각 구간의 지터 위치에서 샘플링하고 해당 구간을 상수로 근사한다. 단색·균일 매질의 구간 투과율은 지수식으로 적분해 샘플 수에 따른 밝기 누적 차이를 줄인다.

### 3.2 설정

| 설정 | 초기값 | 의미 |
|---|---:|---|
| LightingRange | 60 | near-plane 기준 최대 거리 |
| LightingSteps | 60 | 구간 수, 1~128 |
| StartDistance | 2 | 근거리 효과 제외 |
| Density | 0.006 | 주간 비교용 기본 밀도 |
| BaseHeight | 0 | 최대 밀도 기준 월드 Y |
| HeightFalloff | 0.02 | 기준 높이 위의 밀도 감소, 0이면 균일 |
| ScatteringAlbedo | 0.9 | 산란 비율 |
| ScatteringIntensity | 12 | 메인 광원 볼륨 기여도 |
| Anisotropy | 0 | 주간 비교용 등방 산란, 허용 범위 -0.9~0.9 |

수치는 Unity 튜닝 전 시작값이다. `LightingRange`·`LightingSteps`는 private 직렬화 필드로 바꾸면서 `FormerlySerializedAs`로 기존 에셋 이름을 인수한다. PC 에셋은 새 직렬화 이름으로 명시 갱신한다.

2026-11-07 사용자 표시 피드백 후 산란 강도 초기값을 1→8, 방향성을 0.6→0.2로 조정했다. 당시 PC 에셋의 방향성은 사용자 조정으로 0.9였다. 높은 양의 방향성은 광원을 바라보는 좁은 각도에서만 강하므로 탑다운 시점의 가시성에 불리할 수 있다. 기존 밀도는 유지하고 산란 기여도만 올리는 튜닝이다. 사용자가 변경한 PC의 Albedo=1 및 활성화 값은 보존했다. 새 기본 프리셋의 실제 화면은 Unity 재확인이 필요하다.

같은 날 참조 이미지로 목표가 구체화되어 위 표의 주간 비교용 프리셋으로 다시 조정했다. Density=0.006으로 공기층을 더 드러내고 HeightFalloff=0.02로 높은 위치의 밀도가 너무 빨리 사라지지 않게 한다. ScatteringIntensity=12, Anisotropy=0은 시선 방향에 따른 약화를 제외하고 현재 그림자 분포가 빛줄기를 만드는지 비교하기 위한 시작값이다. 등방 산란이어도 빛줄기의 공간 방향은 메인 광원의 그림자에 따라 정해진다. 실제 장면에서 확인 후 방향성을 다시 조절할 수 있다. PC의 사용자 변경 Albedo=1 및 활성화 값은 계속 보존한다.

무채색 가산/감산 모델의 `LightingIntensity`, `LightingIntensityMax`, `ShadowSteps`, `ShadowIntensity`, `ShadowIntensityMax`, `ShadowsColor` 및 대응 setter는 제거한다. 저장소 검색에서 정의와 Feature 내부 이외의 호출자는 없다. 이전 강도를 새 물리량으로 자동 변환하지 않는다. `IVolmetrickLightSettingService`와 서비스 키의 타입·식별자는 유지하고 메서드를 새 설정에 맞춘다. 외부 미병합 브랜치의 기존 호출자는 이 계약에 맞춰 수정해야 한다.

Inspector와 서비스 경로 모두 유한값·유효 범위를 검사한다. 잘못된 구성은 대상 필드 이름을 포함한 예외로 드러낸다. 시작 거리가 범위 이상인 경우와 밀도 0은 의도적인 효과 없음으로 취급한다.

### 3.3 광원과 그림자

- `UniversalLightData.mainLightIndex`의 `VisibleLight.finalColor`를 사용한다. URP가 이미 활성 색 공간에 맞춘 값을 다시 `.linear` 변환하지 않는다.
- 광원 방향은 URP와 동일하게 `-localToWorldMatrix.GetColumn(2)`다. 광원이 없으면 기여색은 0으로 초기화한다. 이 경우에도 밀도가 있으면 투과율에 의한 감쇠는 남는다(환경광 미지원의 한계).
- 그림자맵이 없으면 명시적인 흰색 기본 텍스처와 cascade 수 0을 넘긴다. 카메라 사이의 이전 광원·그림자 값을 새 광원 값으로 재사용하지 않는다.
- 기존 cascade 선택·atlas 경계 제한을 유지하고, 깊이는 point 샘플 후 비교한다. URP 그림자 강도와 거리 fade를 반영한다.
- 유효한 cascade 인덱스 0~3의 atlas 좌표는 `uint` 비트 AND/시프트로 구한다. 음수(그림자 없음)는 그 전에 반환하며 signed 정수 나눗셈·나머지 연산을 사용하지 않는다.
- 기존 Game/SceneView 필터와 `BeforeRenderingTransparents` 순서를 유지한다. 투명 오브젝트는 이 Compute 결과에 포함되지 않으므로 별도 fog 처리는 후속 범위다.

## 4. 조립·서비스·설계 검토

- SSOT: 설정은 기존 Settings 하나, 광원은 URP, 자원은 RenderGraph가 소유한다. 게임 Buffer와 규칙을 복제하지 않는다.
- S: Settings는 설정/검증, Feature는 카메라별 데이터와 패스 연결, Compute는 적분을 담당한다.
- O: 기존 기능 내부 알고리즘 교체이며 공용 렌더 기능 분기나 다른 기능 구현에 조건을 추가하지 않는다.
- L: 새 setter의 유효 범위를 명시한다. 기존 서비스 타입·Scope 수명은 유지한다.
- I: 사용하지 않는 그림자 설정 명령을 제거하고 이 효과가 실제 사용하는 설정만 제공한다.
- D: 광원은 씬 검색 대신 URP 프레임 데이터에서 받는다. 기존 서비스 조립 경계를 우회하지 않는다.
- JMH/FW: 생성은 기존 Renderer Feature/RenderSystemFactory, 내부 등록은 `RenderFeatureSystem`, Export는 `RenderCoreGroup`, 소비 경로는 `Global.Render.CoreGroup.RenderFeatureSystemServices`의 Scope다. Phase 1은 등록을 추가하지 않으므로 FW-001~003의 Builder 변경이 없다. FW-004 내부 우회를 만들지 않는다. FW-010/012의 Mesh·DB 대여는 사용하지 않는다. RenderGraph 임시 텍스처의 수명은 RenderGraph에 남긴다.
- 기능 독립성(§4.6): 산란 계산 변경은 세 기능 파일과 PC의 해당 설정만 수정/이해/검증한다. 활성 구간 변경은 후속 Scope 소유자와 조립 지점에서 처리한다. 공용 `BaseRenderFeature`를 고쳐 모든 기능을 재검증해야 하는 구조를 만들지 않는다. 남는 공통 경계는 카메라 필터·RenderGraph 자원·RenderFeature Scope 계약이다.

## 5. 검증과 자료

- Test 코드를 작성하지 않는다. MSBuild·Unity 실행은 수행하지 않는다. 보고 상태는 **컴파일 미확인**이다.
- 정적 점검: 기존 호출자 잔여, CPU/HLSL 인자 대응, 에셋 GUID/다른 기능 불변, LF/UTF-8, diff 공백 오류.
- 사용자 Unity 확인: 밀도 0 원본 보존, 메인 광원 색/세기 0, 그림자 강도 0/1, 1/4 cascade, 직교/원근, 근거리 제외, 높이 변화, 샘플 수 변경, 기존 높이 안개와 중첩, SceneView/GameView 방향 일치.
- 현재 Phase 1은 운영 Scope 소비자를 추가하지 않는다. 저장된 `IsAddRenderPass=0` 및 초기화의 비활성화는 유지된다. Unity 계산 비교는 재생 초기화가 끝난 뒤 Inspector의 `Is Add Render Pass`를 잠시 켜서 확인할 수 있다. 운영 활성화는 Phase 2에서 기존 Scope를 통해 연결한다.

근거:

- 설치된 URP `UniversalLightData.cs`, `UniversalRenderPipelineCore.InitializeLightConstants_Common`, `UniversalCameraData.GetProjectionMatrix` 및 `GetGPUProjectionMatrix`의 호환 모드 타깃 조회 경로, `Shadows.hlsl`.
- 설치된 Core RP `ShaderLibrary/VolumeRendering.hlsl`: Beer–Lambert 투과율, 균일 구간 산란 적분, 높이 밀도, Henyey–Greenstein 함수.
- [Unreal Volumetric Fog: 낮은 전체 밀도와 광원별 산란 기여도 조절](https://dev.epicgames.com/documentation/en-us/unreal-engine/volumetric-fog-in-unreal-engine).
